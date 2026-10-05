using System;
using System.Reflection;
using MonoMod;

namespace CelesteAndroid
{
	public static class HostConfig
	{
		public const string PlatformKey = "CelesteAndroid.Platform";
		public const string PrefPathKey = "CelesteAndroid.PrefPath";
		public const string BackgroundPathKey = "CelesteAndroid.BackgroundPath";
		public const string TouchLayoutPathKey = "CelesteAndroid.TouchLayoutPath";

		public const string ShowFpsKey = "CelesteAndroid.ShowFps";
		public const string HideTouchKey = "CelesteAndroid.HideTouch";
		public const string TouchOpacityKey = "CelesteAndroid.TouchOpacity";

		public static bool ShowFps => AppContext.GetData(ShowFpsKey) as string == "1";

		public static bool HideTouch => AppContext.GetData(HideTouchKey) as string == "1";

		public static int? TouchOpacityPercent =>
			int.TryParse(AppContext.GetData(TouchOpacityKey) as string, System.Globalization.NumberStyles.Integer,
				System.Globalization.CultureInfo.InvariantCulture, out int v) ? (int?)Math.Clamp(v, 0, 100) : null;

		public static string Platform => AppContext.GetData(PlatformKey) as string ?? "Android";

		public static string? BackgroundPath => AppContext.GetData(BackgroundPathKey) as string;

		public static string? TouchLayoutPath => AppContext.GetData(TouchLayoutPathKey) as string;

		public static string PrefPath => AppContext.GetData(PrefPathKey) as string
			?? throw new InvalidOperationException($"{PrefPathKey} não foi definido pelo host.");
	}

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
		[MonoModLinkFrom("System.Void System.Runtime.GCSettings::set_LatencyMode(System.Runtime.GCLatencyMode)")]
		public static void SetLatencyMode(System.Runtime.GCLatencyMode mode)
		{
			try
			{
				typeof(System.Runtime.GCSettings).GetProperty(nameof(System.Runtime.GCSettings.LatencyMode))!.SetValue(null, mode);
			}
			catch (TargetInvocationException e) when (e.InnerException is PlatformNotSupportedException)
			{
			}
		}
	}

	public static class ReflectionShim
	{
		[MonoModLinkFrom("System.Reflection.Assembly System.Reflection.Assembly::GetEntryAssembly()")]
		public static Assembly GetEntryAssembly() => typeof(ReflectionShim).Assembly;
	}
}
