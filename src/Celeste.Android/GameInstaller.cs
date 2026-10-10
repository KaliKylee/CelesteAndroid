using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Android.Content;
using Android.Database;
using Android.Graphics;
using Android.Provider;
using Android.Util;
using CelesteAndroid.Patcher;
using Microsoft.Win32.SafeHandles;
using Path = System.IO.Path;
using Uri = Android.Net.Uri;

namespace CelesteAndroid
{
	public sealed class GameInstaller
	{
		private const string GameAssetsRoot = "game";
		private const int BufferSize = 1 << 20;

		private readonly Context context;
		private readonly Action<string, float> progress;
		private readonly byte[] buffer = new byte[BufferSize];

		public GameInstaller(Context context, Action<string, float> progress)
		{
			this.context = context;
			this.progress = progress;
		}

		private static string Files(Context context) => context.FilesDir!.AbsolutePath;
		public static string GameDir(Context context) => Path.Combine(Files(context), "Celeste");
		public static string PatchedDll(Context context) => Path.Combine(Files(context), "patched", "Celeste.dll");
		public static string BackgroundPng(Context context) => Path.Combine(Files(context), "background.png");
		public static string UserDir(Context context) => Path.Combine(Files(context), "userdata");
		public static string TouchLayoutFile(Context context) => Path.Combine(Files(context), "touch_layout.txt");

		public static string CustomButtonsFile(Context context) => Path.Combine(Files(context), "custom_buttons.txt");

		public static string ButtonStyleFile(Context context) => Path.Combine(Files(context), "button_style.txt");

		private static string PatchStampFile(Context context) => Path.Combine(Files(context), "patched", "patch.stamp");

		// Impressão digital do módulo de patches embutido no APK. Muda a cada versão que altera os patches.
		private static string CurrentPatchStamp(Context context)
		{
			using Stream s = context.Assets!.Open("patcher/Celeste.Android.mm.dll");
			using var sha = System.Security.Cryptography.SHA256.Create();
			return Convert.ToHexString(sha.ComputeHash(s));
		}

		// O Celeste.dll "patchado" é gerado no aparelho; depois de atualizar o app ele precisa ser refeito.
		public static bool NeedsRepatch(Context context)
		{
			try
			{
				if (!IsInstalled(context))
					return false;
				string stamp = PatchStampFile(context);
				return !File.Exists(stamp) || File.ReadAllText(stamp).Trim() != CurrentPatchStamp(context);
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static bool IsInstalled(Context context) =
			File.Exists(PatchedDll(context)) && Directory.Exists(Path.Combine(GameDir(context), "Content"));

		public static bool HasEmbeddedGame(Context context) =>
			context.Assets!.List(GameAssetsRoot)?.Contains("Celeste.exe") == true;

		#region Importação

		public void ImportFolder(Uri treeUri)
		{
			progress(L.Searching, -1);
			ContentResolver resolver = context.ContentResolver!;
			string rootId = DocumentsContract.GetTreeDocumentId(treeUri)!;

			Doc root = FindGameRoot(treeUri, new Doc(rootId, "", true, 0), depth: 3)
				?? throw new InstallException(L.NotFound);
			List<Doc> top = ListChildren(treeUri, root.Id);
			CheckFnaBuild(top.Any(d => d.Name.Equals("FNA.dll", StringComparison.OrdinalIgnoreCase)));

			var files = new List<(Doc doc, string relative)>();
			files.Add((top.First(d => d.Name == "Celeste.exe"), "Celeste.exe"));
			CollectFiles(treeUri, top.First(d => d.Name == "Content" && d.IsDir), "Content", files);

			CopyAll(files.Select(f => (f.relative, f.doc.Size,
				(Func<Stream>)(() => resolver.OpenInputStream(DocumentsContract.BuildDocumentUriUsingTree(treeUri, f.doc.Id)!)!))));
		}

		public void ImportZip(Uri zipUri)
		{
			progress(L.ReadingZip, -1);
			using var pfd = context.ContentResolver!.OpenFileDescriptor(zipUri, "r")
				?? throw new InstallException(L.CantOpenFile);
			using var stream = new FileStream(new SafeFileHandle(pfd.DetachFd(), ownsHandle: true), FileAccess.Read);
			using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

			ZipArchiveEntry exe = zip.Entries
				.Where(e => e.Name.Equals("Celeste.exe", StringComparison.OrdinalIgnoreCase))
				.OrderBy(e => e.FullName.Length)
				.FirstOrDefault() ?? throw new InstallException(L.ZipNoExe);
			string prefix = exe.FullName[..^exe.Name.Length];
			CheckFnaBuild(zip.GetEntry(prefix + "FNA.dll") != null);

			var entries = zip.Entries
				.Where(e => e == exe || (e.FullName.StartsWith(prefix + "Content/", StringComparison.Ordinal) && e.Name.Length > 0))
				.ToList();
			CopyAll(entries.Select(e => (e.FullName[prefix.Length..], e.Length, (Func<Stream>)e.Open)));
		}

		public void ImportEmbedded()
		{
			progress(L.PreparingEmbedded, -1);
			var files = new List<string>();
			CollectAssets(GameAssetsRoot, files);
			CopyAll(files.Select(f => (f[(GameAssetsRoot.Length + 1)..], -1L, (Func<Stream>)(() => context.Assets!.Open(f)))));
		}

		// ---------- Saves: importar (pasta ou .zip), exportar (.zip) e backups com data ----------

		private const int MaxBackups = 10;
		private const long MaxSaveBytes = 20L << 20;

		public sealed class SavePlan
		{
			public List<(string Name, byte[] Data, bool Replaces)> Files { get; } = new();
			public List<string> Skipped { get; } = new();
		}

		private static string SavesDir(Context c) => Path.Combine(UserDir(c), "Celeste", "Saves");

		private static string BackupsDir(Context c) => Path.Combine(UserDir(c), "Celeste", "Backups");

		private static byte[] ReadAll(Stream input)
		{
			using var ms = new MemoryStream();
			input.CopyTo(ms);
			return ms.ToArray();
		}

		// Um save válido é um XML bem formado (formato usado pelo Celeste).
		private static bool LooksLikeSave(byte[] data)
		{
			if (data.Length == 0 || data.Length > MaxSaveBytes)
				return false;
			try
			{
				using var ms = new MemoryStream(data);
				return System.Xml.Linq.XDocument.Load(ms).Root != null;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private SavePlan BuildPlan(IEnumerable<(string name, Func<byte[]> read)> candidates)
		{
			var plan = new SavePlan();
			string target = SavesDir(context);
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var (rawName, read) in candidates)
			{
				string name = Path.GetFileName(rawName);
				if (name.Length == 0 || name.Contains("..", StringComparison.Ordinal) || !seen.Add(name))
					continue;
				byte[] data;
				try
				{
					data = read();
				}
				catch (Exception)
				{
					plan.Skipped.Add(name);
					continue;
				}
				if (!LooksLikeSave(data))
				{
					plan.Skipped.Add(name);
					continue;
				}
				plan.Files.Add((name, data, File.Exists(Path.Combine(target, name))));
			}
			if (plan.Files.Count == 0)
				throw new InstallException(plan.Skipped.Count > 0 ? L.SavesSkipped(string.Join(", ", plan.Skipped)) : L.NoSaves);
			return plan;
		}

		public SavePlan ReadSavesFolder(Uri treeUri)
		{
			progress(L.SearchingSaves, -1);
			string rootId = DocumentsContract.GetTreeDocumentId(treeUri)!;
			List<Doc> files = ListChildren(treeUri, rootId);
			Doc? savesDir = files.FirstOrDefault(d => d.IsDir && d.Name.Equals("Saves", StringComparison.OrdinalIgnoreCase));
			if (savesDir is Doc dir)
				files = ListChildren(treeUri, dir.Id);
			List<Doc> saves = files.Where(d => !d.IsDir && d.Name.EndsWith(".celeste", StringComparison.OrdinalIgnoreCase)).ToList();
			if (saves.Count == 0)
				throw new InstallException(L.NoSaves);

			return BuildPlan(saves.Select(save => (save.Name, (Func<byte[]>)(() =>
			{
				if (save.Size > MaxSaveBytes)
					return Array.Empty<byte>();
				using Stream input = context.ContentResolver!.OpenInputStream(DocumentsContract.BuildDocumentUriUsingTree(treeUri, save.Id)!)!;
				return ReadAll(input);
			}))));
		}

		public SavePlan ReadSavesZip(Uri zipUri)
		{
			progress(L.SearchingSaves, -1);
			using var pfd = context.ContentResolver!.OpenFileDescriptor(zipUri, "r")
				?? throw new InstallException(L.CantOpenFile);
			using var stream = new FileStream(new SafeFileHandle(pfd.DetachFd(), ownsHandle: true), FileAccess.Read);
			using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

			List<ZipArchiveEntry> entries = zip.Entries
				.Where(e => e.Name.EndsWith(".celeste", StringComparison.OrdinalIgnoreCase))
				.ToList();
			if (entries.Count == 0)
				throw new InstallException(L.NoSaves);

			return BuildPlan(entries.Select(e => (e.Name, (Func<byte[]>)(() =>
			{
				if (e.Length > MaxSaveBytes)
					return Array.Empty<byte>();
				using Stream input = e.Open();
				return ReadAll(input);
			}))));
		}

		public int ApplySaves(SavePlan plan)
		{
			progress(L.SavesWorking, -1);
			Directory.CreateDirectory(SavesDir(context));
			BackupCurrentSaves();
			WriteSaves(plan.Files.Select(f => (f.Name, f.Data)).ToList());
			return plan.Files.Count;
		}

		private void WriteSaves(List<(string Name, byte[] Data)> files)
		{
			string target = SavesDir(context);
			Directory.CreateDirectory(target);
			foreach (var (name, data) in files)
			{
				string dest = Path.Combine(target, name);
				string tmp = dest + ".tmp";
				File.WriteAllBytes(tmp, data);
				File.Move(tmp, dest, overwrite: true);
			}
		}

		private string NewBackupDir(DateTime time)
		{
			string root = BackupsDir(context);
			Directory.CreateDirectory(root);
			string name = time.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
			string dir = Path.Combine(root, name);
			int n = 1;
			while (Directory.Exists(dir))
				dir = Path.Combine(root, name + "-" + n++);
			Directory.CreateDirectory(dir);
			return dir;
		}

		// Guarda os saves atuais numa pasta com data/hora e mantém só os últimos backups.
		private void BackupCurrentSaves()
		{
			string target = SavesDir(context);
			if (!Directory.Exists(target))
				return;
			string[] current = Directory.GetFiles(target, "*.celeste");
			if (current.Length == 0)
				return;
			string dir = NewBackupDir(DateTime.Now);
			foreach (string file in current)
				File.Copy(file, Path.Combine(dir, Path.GetFileName(file)), overwrite: true);

			string[] all = Directory.GetDirectories(BackupsDir(context));
			Array.Sort(all, StringComparer.Ordinal);
			for (int i = 0; i < all.Length - MaxBackups; i++)
				Directory.Delete(all[i], recursive: true);
		}

		public static List<(string Name, DateTime Time, int Count)> ListBackups(Context c)
		{
			var result = new List<(string, DateTime, int)>();
			string root = BackupsDir(c);
			if (!Directory.Exists(root))
				return result;

			// Backups antigos ficavam soltos na pasta: agrupa numa pasta datada.
			string[] flat = Directory.GetFiles(root, "*.celeste");
			if (flat.Length > 0)
			{
				DateTime when = flat.Max(f => File.GetLastWriteTime(f));
				string name = when.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
				string dir = Path.Combine(root, name);
				int n = 1;
				while (Directory.Exists(dir))
					dir = Path.Combine(root, name + "-" + n++);
				Directory.CreateDirectory(dir);
				foreach (string file in flat)
					File.Move(file, Path.Combine(dir, Path.GetFileName(file)));
			}

			string[] dirs = Directory.GetDirectories(root);
			Array.Sort(dirs, StringComparer.Ordinal);
			for (int i = dirs.Length - 1; i >= 0; i--)
			{
				int count = Directory.GetFiles(dirs[i], "*.celeste").Length;
				if (count > 0)
					result.Add((Path.GetFileName(dirs[i]), Directory.GetLastWriteTime(dirs[i]), count));
			}
			return result;
		}

		public int RestoreBackup(string name)
		{
			progress(L.SavesWorking, -1);
			string dir = Path.Combine(BackupsDir(context), Path.GetFileName(name));
			string[] files = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.celeste") : Array.Empty<string>();
			if (files.Length == 0)
				throw new InstallException(L.NoBackups);

			// Lê tudo antes: o backup dos saves atuais pode apagar o mais antigo da lista.
			var data = files.Select(f => (Path.GetFileName(f), File.ReadAllBytes(f))).ToList();
			BackupCurrentSaves();
			WriteSaves(data);
			return data.Count;
		}

		public int ExportSaves(Uri uri)
		{
			progress(L.SavesWorking, -1);
			string dir = SavesDir(context);
			string[] files = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.celeste") : Array.Empty<string>();
			if (files.Length == 0)
				throw new InstallException(L.NoLocalSaves);

			using Stream output = context.ContentResolver!.OpenOutputStream(uri, "wt")
				?? throw new InstallException(L.CantOpenFile);
			using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
			{
				foreach (string file in files)
				{
					ZipArchiveEntry entry = zip.CreateEntry("Saves/" + Path.GetFileName(file), CompressionLevel.Optimal);
					using Stream entryStream = entry.Open();
					using FileStream input = File.OpenRead(file);
					input.CopyTo(entryStream);
				}
			}
			output.Flush();
			return files.Length;
		}

		private void CheckFnaBuild(bool hasFna)
		{
			if (!hasFna)
			{
				throw new InstallException(L.XnaVersion);
			}
		}

		private void CopyAll(IEnumerable<(string relative, long size, Func<Stream> open)> source)
		{
			var files = source.ToList();
			long total = Math.Max(1, files.Sum(f => Math.Max(0, f.size)));
			long done = 0;

			string staging = GameDir(context) + ".importing";
			if (Directory.Exists(staging))
				Directory.Delete(staging, recursive: true);

			for (int i = 0; i < files.Count; i++)
			{
				var (relative, size, open) = files[i];
				string dest = Path.Combine(staging, relative.Replace('\\', '/'));
				Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
				using (Stream input = open())
				using (FileStream output = File.Create(dest))
				{
					int read;
					while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
					{
						output.Write(buffer, 0, read);
						done += read;
					}
				}
				float fraction = size >= 0 ? done / (float)total : (i + 1) / (float)files.Count;
				if (i % 8 == 0 || i == files.Count - 1)
					progress(L.Copying(i + 1, files.Count), fraction);
			}

			if (!File.Exists(Path.Combine(staging, "Celeste.exe")) || !Directory.Exists(Path.Combine(staging, "Content")))
				throw new InstallException(L.CopyIncomplete);

			string gameDir = GameDir(context);
			if (Directory.Exists(gameDir))
				Directory.Delete(gameDir, recursive: true);
			Directory.Move(staging, gameDir);
		}

		#endregion

		#region Patch e fundo

		public void Patch()
		{
			progress(L.Patching, -1);
			string patcherDir = Path.Combine(context.CacheDir!.AbsolutePath, "patcher");
			Directory.CreateDirectory(patcherDir);
			foreach (string asset in context.Assets!.List("patcher")!)
			{
				using Stream input = context.Assets.Open("patcher/" + asset);
				using FileStream output = File.Create(Path.Combine(patcherDir, asset));
				input.CopyTo(output);
			}

			string patched = PatchedDll(context);
			string staging = patched + ".tmp";
			CelestePatcher.Patch(
				Path.Combine(GameDir(context), "Celeste.exe"),
				Path.Combine(patcherDir, "Celeste.Android.mm.dll"),
				staging,
				new[] { patcherDir },
				msg => Log.Info(GameActivity.LogTag, msg)
			);
			File.Move(staging, patched, overwrite: true);
			File.WriteAllText(PatchStampFile(context), CurrentPatchStamp(context));
			foreach (string leftover in Directory.GetFiles(Path.GetDirectoryName(patched)!, "*.mdb"))
				File.Delete(leftover);
		}

		public void PrepareBackground()
		{
			string art = Path.Combine(GameDir(context), "Content", "Graphics", "SplashScreen.png");
			if (!File.Exists(art))
				return;
			progress(L.MakingBackground, -1);

			const int width = 192, height = 108;
			using Bitmap decoded = BitmapFactory.DecodeFile(art, new BitmapFactory.Options { InSampleSize = 8 })!;
			using Bitmap small = Bitmap.CreateScaledBitmap(decoded, width, height, true)!;
			int[] pixels = new int[width * height];
			small.GetPixels(pixels, 0, width, 0, 0, width, height);

			for (int pass = 0; pass < 3; pass++)
			{
				BoxBlur(pixels, width, height, radius: 7, horizontal: true);
				BoxBlur(pixels, width, height, radius: 7, horizontal: false);
			}
			for (int i = 0; i < pixels.Length; i++)
			{
				int p = pixels[i];
				pixels[i] = unchecked((int)0xFF000000)
					| (((p >> 16) & 0xFF) * 38 / 100) << 16
					| (((p >> 8) & 0xFF) * 38 / 100) << 8
					| ((p & 0xFF) * 38 / 100);
			}

			using Bitmap result = Bitmap.CreateBitmap(pixels, width, height, Bitmap.Config.Argb8888!)!;
			using FileStream output = File.Create(BackgroundPng(context));
			result.Compress(Bitmap.CompressFormat.Png!, 100, output);
		}

		private static void BoxBlur(int[] pixels, int width, int height, int radius, bool horizontal)
		{
			int lines = horizontal ? height : width;
			int length = horizontal ? width : height;
			int[] line = new int[length];
			for (int l = 0; l < lines; l++)
			{
				int Index(int i) => horizontal ? l * width + i : i * width + l;
				for (int i = 0; i < length; i++)
					line[i] = pixels[Index(i)];

				for (int i = 0; i < length; i++)
				{
					int r = 0, g = 0, b = 0, count = 0;
					for (int k = Math.Max(0, i - radius); k <= Math.Min(length - 1, i + radius); k++)
					{
						int p = line[k];
						r += (p >> 16) & 0xFF;
						g += (p >> 8) & 0xFF;
						b += p & 0xFF;
						count++;
					}
					pixels[Index(i)] = unchecked((int)0xFF000000) | (r / count) << 16 | (g / count) << 8 | (b / count);
				}
			}
		}

		#endregion

		#region SAF / assets

		private readonly record struct Doc(string Id, string Name, bool IsDir, long Size);

		private List<Doc> ListChildren(Uri treeUri, string parentId)
		{
			var result = new List<Doc>();
			Uri children = DocumentsContract.BuildChildDocumentsUriUsingTree(treeUri, parentId)!;
			string[] projection =
			{
				DocumentsContract.Document.ColumnDocumentId,
				DocumentsContract.Document.ColumnDisplayName,
				DocumentsContract.Document.ColumnMimeType,
				DocumentsContract.Document.ColumnSize,
			};
			using ICursor? cursor = context.ContentResolver!.Query(children, projection, null, null, null);
			while (cursor != null && cursor.MoveToNext())
			{
				result.Add(new Doc(
					cursor.GetString(0)!,
					cursor.GetString(1) ?? "",
					cursor.GetString(2) == DocumentsContract.Document.MimeTypeDir,
					cursor.IsNull(3) ? -1 : cursor.GetLong(3)
				));
			}
			return result;
		}

		private Doc? FindGameRoot(Uri treeUri, Doc dir, int depth)
		{
			List<Doc> children = ListChildren(treeUri, dir.Id);
			if (children.Any(c => c.Name == "Celeste.exe") && children.Any(c => c.Name == "Content" && c.IsDir))
				return dir;
			if (depth == 0)
				return null;
			foreach (Doc child in children.Where(c => c.IsDir))
			{
				Doc? found = FindGameRoot(treeUri, child, depth - 1);
				if (found != null)
					return found;
			}
			return null;
		}

		private void CollectFiles(Uri treeUri, Doc dir, string relative, List<(Doc, string)> files)
		{
			foreach (Doc child in ListChildren(treeUri, dir.Id))
			{
				string path = relative + "/" + child.Name;
				if (child.IsDir)
					CollectFiles(treeUri, child, path, files);
				else
					files.Add((child, path));
			}
		}

		private void CollectAssets(string path, List<string> files)
		{
			string[] children = context.Assets!.List(path) ?? Array.Empty<string>();
			if (children.Length == 0)
			{
				files.Add(path);
				return;
			}
			foreach (string child in children)
				CollectAssets(path + "/" + child, files);
		}

		#endregion
	}

	public sealed class InstallException(string message) : Exception(message);
}
