using System;
using System.Collections.Generic;
using System.IO;
using Android.Content;
using Android.OS;

namespace CelesteAndroid
{
	/// <summary>
	/// Escolha do driver gráfico (Vulkan ou OpenGL ES). O padrão do FNA3D aqui é Vulkan (SDL_GPU), testado só em Adreno;
	/// em aparelhos MediaTek (Mali/PowerVR) o driver Vulkan costuma travar ao iniciar. Então:
	///  1) sem escolha do usuário, aparelhos MediaTek começam em OpenGL ES;
	///  2) se uma partida iniciada com Vulkan não sobreviver aos primeiros segundos, a próxima troca para OpenGL ES sozinha.
	/// A preferência "driver" guarda "" (automático), "OpenGL" ou "Vulkan" (escolha explícita do usuário).
	/// </summary>
	public static class GraphicsDriver
	{
		public const string OpenGL = "OpenGL";
		public const string Vulkan = "Vulkan";
		private const string PrefDriver = "driver";

		/// <summary>O jogo roda em outro processo; este arquivo (na pasta do app) é o recado entre os dois.</summary>
		private static string MarkerPath(Context context) => Path.Combine(context.FilesDir!.AbsolutePath, "launch_pending.txt");

		public static bool IsMediaTek { get; } = DetectMediaTek();

		private static bool DetectMediaTek()
		{
			try
			{
				var fields = new List<string?> { Build.Hardware, Build.Board };
				if (OperatingSystem.IsAndroidVersionAtLeast(31))
				{
					fields.Add(Build.SocManufacturer);
					fields.Add(Build.SocModel);
				}
				foreach (string? field in fields)
				{
					string s = (field ?? "").ToLowerInvariant();
					// Hardware/Board dos MediaTek: "mt6893", "mt6877"...; Soc*: "Mediatek" / "MT6893".
					if (s.Contains("mediatek") || (s.Length > 2 && s.StartsWith("mt") && char.IsDigit(s[2])))
						return true;
				}
			}
			catch (Exception)
			{
			}
			return false;
		}

		/// <summary>Driver que será usado: a escolha do usuário, ou o automático (OpenGL ES no MediaTek, Vulkan no resto).</summary>
		public static string Effective(ISharedPreferences prefs)
		{
			string stored = prefs.GetString(PrefDriver, "") ?? "";
			if (stored == OpenGL || stored == Vulkan)
				return stored;
			return IsMediaTek ? OpenGL : Vulkan;
		}

		public static void Set(ISharedPreferences prefs, string driver) =>
			prefs.Edit()!.PutString(PrefDriver, driver)!.Apply();

		public static void Toggle(ISharedPreferences prefs) =>
			Set(prefs, Effective(prefs) == OpenGL ? Vulkan : OpenGL);

		/// <summary>
		/// Chamar antes de iniciar o jogo. Se a partida anterior (com Vulkan) nunca chegou a "sobreviver" (ver MarkLaunchOk),
		/// troca para OpenGL ES e devolve true, para o launcher avisar o usuário. Depois deixa o recado da partida atual.
		/// </summary>
		public static bool PrepareLaunch(Context context, ISharedPreferences prefs)
		{
			bool switched = false;
			try
			{
				string path = MarkerPath(context);
				if (File.Exists(path))
				{
					string previous = File.ReadAllText(path).Trim();
					File.Delete(path);
					if (previous == Vulkan && Effective(prefs) == Vulkan)
					{
						Set(prefs, OpenGL);
						switched = true;
					}
				}
				File.WriteAllText(path, Effective(prefs));
			}
			catch (Exception)
			{
			}
			return switched;
		}

		/// <summary>Chamado pelo processo do jogo quando a partida passou da fase de risco (iniciou e ficou viva).</summary>
		public static void MarkLaunchOk(Context context)
		{
			try
			{
				File.Delete(MarkerPath(context));
			}
			catch (Exception)
			{
			}
		}
	}
}
