using System;
using System.Collections.Generic;
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
	/// <summary>
	/// Importa os arquivos do jogo do usuário para o armazenamento interno do app e prepara tudo para rodar:
	/// cópia (pasta, .zip ou assets do APK pessoal) → patch com MonoMod → fundo das faixas laterais.
	/// </summary>
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
		/// <summary>Layout dos controles de toque salvo pelo editor (lido pelo jogo, que roda em outro processo).</summary>
		public static string TouchLayoutFile(Context context) => Path.Combine(Files(context), "touch_layout.txt");

		public static bool IsInstalled(Context context) =>
			File.Exists(PatchedDll(context)) && Directory.Exists(Path.Combine(GameDir(context), "Content"));

		/// <summary>APK pessoal: o jogo vem nos assets (build com -p:EmbedGame=true).</summary>
		public static bool HasEmbeddedGame(Context context) =>
			context.Assets!.List(GameAssetsRoot)?.Contains("Celeste.exe") == true;

		#region Importação

		/// <summary>Pasta escolhida pelo usuário (ACTION_OPEN_DOCUMENT_TREE).</summary>
		public void ImportFolder(Uri treeUri)
		{
			progress("Procurando por Celeste na pasta…", -1);
			ContentResolver resolver = context.ContentResolver!;
			string rootId = DocumentsContract.GetTreeDocumentId(treeUri)!;

			Doc root = FindGameRoot(treeUri, new Doc(rootId, "", true, 0), depth: 3)
				?? throw new InstallException("Não encontrei o Celeste.exe e a pasta Content aí.");
			List<Doc> top = ListChildren(treeUri, root.Id);
			CheckFnaBuild(top.Any(d => d.Name.Equals("FNA.dll", StringComparison.OrdinalIgnoreCase)));

			var files = new List<(Doc doc, string relative)>();
			files.Add((top.First(d => d.Name == "Celeste.exe"), "Celeste.exe"));
			CollectFiles(treeUri, top.First(d => d.Name == "Content" && d.IsDir), "Content", files);

			CopyAll(files.Select(f => (f.relative, f.doc.Size,
				(Func<Stream>)(() => resolver.OpenInputStream(DocumentsContract.BuildDocumentUriUsingTree(treeUri, f.doc.Id)!)!))));
		}

		/// <summary>.zip escolhido pelo usuário (ex.: o zip Linux do itch.io).</summary>
		public void ImportZip(Uri zipUri)
		{
			progress("Lendo o .zip…", -1);
			using var pfd = context.ContentResolver!.OpenFileDescriptor(zipUri, "r")
				?? throw new InstallException("Não consegui abrir o arquivo.");
			using var stream = new FileStream(new SafeFileHandle(pfd.DetachFd(), ownsHandle: true), FileAccess.Read);
			using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

			ZipArchiveEntry exe = zip.Entries
				.Where(e => e.Name.Equals("Celeste.exe", StringComparison.OrdinalIgnoreCase))
				.OrderBy(e => e.FullName.Length)
				.FirstOrDefault() ?? throw new InstallException("Este .zip não contém o Celeste.exe.");
			string prefix = exe.FullName[..^exe.Name.Length];
			CheckFnaBuild(zip.GetEntry(prefix + "FNA.dll") != null);

			var entries = zip.Entries
				.Where(e => e == exe || (e.FullName.StartsWith(prefix + "Content/", StringComparison.Ordinal) && e.Name.Length > 0))
				.ToList();
			CopyAll(entries.Select(e => (e.FullName[prefix.Length..], e.Length, (Func<Stream>)e.Open)));
		}

		/// <summary>APK pessoal: copia o jogo embutido nos assets.</summary>
		public void ImportEmbedded()
		{
			progress("Preparando o jogo incluído…", -1);
			var files = new List<string>();
			CollectAssets(GameAssetsRoot, files);
			CopyAll(files.Select(f => (f[(GameAssetsRoot.Length + 1)..], -1L, (Func<Stream>)(() => context.Assets!.Open(f)))));
		}

		/// <summary>
		/// Saves do PC (ou de outro aparelho): uma pasta com os .celeste, ou a pasta do jogo com Saves/ dentro.
		/// Os saves atuais vão para Backups/ antes de serem substituídos.
		/// </summary>
		public int ImportSaves(Uri treeUri)
		{
			progress("Procurando arquivos de save…", -1);
			string rootId = DocumentsContract.GetTreeDocumentId(treeUri)!;
			List<Doc> files = ListChildren(treeUri, rootId);
			Doc? savesDir = files.FirstOrDefault(d => d.IsDir && d.Name.Equals("Saves", StringComparison.OrdinalIgnoreCase));
			if (savesDir is Doc dir)
				files = ListChildren(treeUri, dir.Id);
			List<Doc> saves = files.Where(d => !d.IsDir && d.Name.EndsWith(".celeste", StringComparison.OrdinalIgnoreCase)).ToList();
			if (saves.Count == 0)
				throw new InstallException("Nenhum arquivo de save .celeste nessa pasta.");

			string target = Path.Combine(UserDir(context), "Celeste", "Saves");
			string backup = Path.Combine(UserDir(context), "Celeste", "Backups");
			Directory.CreateDirectory(target);
			Directory.CreateDirectory(backup);
			foreach (string existing in Directory.GetFiles(target, "*.celeste"))
				File.Copy(existing, Path.Combine(backup, Path.GetFileName(existing)), overwrite: true);

			foreach (Doc save in saves)
			{
				using Stream input = context.ContentResolver!.OpenInputStream(DocumentsContract.BuildDocumentUriUsingTree(treeUri, save.Id)!)!;
				using FileStream output = File.Create(Path.Combine(target, save.Name));
				input.CopyTo(output);
			}
			return saves.Count;
		}

		private void CheckFnaBuild(bool hasFna)
		{
			if (!hasFna)
			{
				throw new InstallException(
					"Esta é a versão XNA do Celeste. Na Steam, ative o beta \"opengl\" " +
					"(Propriedades → Betas) e copie a pasta de novo, ou use o .zip do Linux do itch.io.");
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
				// Sem tamanho conhecido (assets), o progresso segue a contagem de arquivos.
				float fraction = size >= 0 ? done / (float)total : (i + 1) / (float)files.Count;
				if (i % 8 == 0 || i == files.Count - 1)
					progress($"Copiando arquivos do jogo… {i + 1}/{files.Count}", fraction);
			}

			if (!File.Exists(Path.Combine(staging, "Celeste.exe")) || !Directory.Exists(Path.Combine(staging, "Content")))
				throw new InstallException("A cópia está incompleta.");

			string gameDir = GameDir(context);
			if (Directory.Exists(gameDir))
				Directory.Delete(gameDir, recursive: true);
			Directory.Move(staging, gameDir);
		}

		#endregion

		#region Patch e fundo

		/// <summary>Gera o Celeste.dll que roda no .NET do Android (MonoMod, no próprio aparelho).</summary>
		public void Patch()
		{
			progress("Adaptando o jogo para Android…", -1);
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
			// O MonoMod também grava símbolos de depuração (.mdb) que não usamos.
			foreach (string leftover in Directory.GetFiles(Path.GetDirectoryName(patched)!, "*.mdb"))
				File.Delete(leftover);
		}

		/// <summary>Versão desfocada e escurecida da key art do jogo, para as faixas laterais.</summary>
		public void PrepareBackground()
		{
			string art = Path.Combine(GameDir(context), "Content", "Graphics", "SplashScreen.png");
			if (!File.Exists(art))
				return;
			progress("Gerando o fundo…", -1);

			const int width = 192, height = 108;
			using Bitmap decoded = BitmapFactory.DecodeFile(art, new BitmapFactory.Options { InSampleSize = 8 })!;
			using Bitmap small = Bitmap.CreateScaledBitmap(decoded, width, height, true)!;
			int[] pixels = new int[width * height];
			small.GetPixels(pixels, 0, width, 0, 0, width, height);

			// Três passadas de box blur ≈ desfoque gaussiano; depois escurece para não competir com o jogo.
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

		/// <summary>Aceita a própria pasta do jogo ou uma pasta acima dela (ex.: "common" da Steam).</summary>
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
