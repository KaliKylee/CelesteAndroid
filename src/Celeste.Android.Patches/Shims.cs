using System;
using System.Reflection;
using MonoMod;

namespace CelesteAndroid
{
	/// <summary>
	/// Configuração passada pelo host (desktop ou Activity Android) antes do jogo iniciar.
	/// Usa AppContext para não exigir referência ao Celeste.dll patcheado.
	/// </summary>
	public static class HostConfig
	{
		public const string PlatformKey = "CelesteAndroid.Platform";
		public const string PrefPathKey = "CelesteAndroid.PrefPath";
		public const string BackgroundPathKey = "CelesteAndroid.BackgroundPath";

		public static string Platform => AppContext.GetData(PlatformKey) as string ?? "Android";

		/// <summary>Imagem para as faixas laterais em telas mais largas que 16:9 (opcional).</summary>
		public static string? BackgroundPath => AppContext.GetData(BackgroundPathKey) as string;

		public static string PrefPath => AppContext.GetData(PrefPathKey) as string
			?? throw new InvalidOperationException($"{PrefPathKey} não foi definido pelo host.");
	}

	/// <summary>
	/// O Celeste chama SDL2 diretamente, mas o FNA atual roda sobre SDL3.
	/// As chamadas são religadas para cá durante o patch.
	/// </summary>
	public static class SDLShim
	{
		[MonoModLinkFrom("System.String SDL2.SDL::SDL_GetPlatform()")]
		public static string SDL_GetPlatform() => HostConfig.Platform;

		[MonoModLinkFrom("System.String SDL2.SDL::SDL_GetPrefPath(System.String,System.String)")]
		public static string SDL_GetPrefPath(string org, string app)
		{
			string path = System.IO.Path.Combine(HostConfig.PrefPath, app);
			System.IO.Directory.CreateDirectory(path);
			return path;
		}
	}

	public static class GCShim
	{
		/// <summary>O runtime Mono do Android lança PlatformNotSupportedException ao mudar a latência do GC.</summary>
		[MonoModLinkFrom("System.Void System.Runtime.GCSettings::set_LatencyMode(System.Runtime.GCLatencyMode)")]
		public static void SetLatencyMode(System.Runtime.GCLatencyMode mode)
		{
			// Por reflexão: uma chamada direta também seria religada para cá (recursão infinita).
			try
			{
				typeof(System.Runtime.GCSettings).GetProperty(nameof(System.Runtime.GCSettings.LatencyMode))!.SetValue(null, mode);
			}
			catch (TargetInvocationException e) when (e.InnerException is PlatformNotSupportedException)
			{
			}
		}
	}

	/// <summary>
	/// O Celeste usa GetEntryAssembly() para achar os próprios tipos; com um loader, o "entry" não é ele.
	/// </summary>
	public static class ReflectionShim
	{
		[MonoModLinkFrom("System.Reflection.Assembly System.Reflection.Assembly::GetEntryAssembly()")]
		public static Assembly GetEntryAssembly() => typeof(ReflectionShim).Assembly;
	}
}
