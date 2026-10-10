using System;
using System.Reflection;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Microsoft.Xna.Framework;
using Org.Libsdl.App;

namespace CelesteAndroid
{
	[Activity(
		Name = "org.celesteandroid.celeste.GameActivity",
		Label = "Celeste",
		Process = ":game",
		Exported = false,
		Theme = "@style/Theme.Celeste",
		ScreenOrientation = ScreenOrientation.SensorLandscape,
		LaunchMode = LaunchMode.SingleTask,
		HardwareAccelerated = true,
		ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
			| ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation
			| ConfigChanges.UiMode | ConfigChanges.Density | ConfigChanges.SmallestScreenSize)]
	public class GameActivity : SDLActivity
	{
		public const string LogTag = "CelesteAndroid";
		public const string ExtraDriver = "driver";
		public const string ExtraShowFps = "show_fps";
		public const string ExtraHideTouch = "hide_touch";
		public const string ExtraTouchOpacity = "touch_opacity";

		private const int SurviveMs = 15000, QuickExitMs = 5000;
		private static System.Threading.Timer? surviveTimer;
		private readonly System.Diagnostics.Stopwatch alive = System.Diagnostics.Stopwatch.StartNew();

		protected override string[] GetLibraries() => new[] { "SDL3", "fmod", "fmodstudio" };

		public override ScreenOrientation RequestedOrientation
		{
			get => base.RequestedOrientation;
			set => base.RequestedOrientation = LandscapeLock.Coerce(value);
		}

		protected override void OnCreate(Bundle? savedInstanceState)
		{
			CultureFix.Apply();
			RequestedOrientation = LandscapeLock.Orientation;
			base.OnCreate(savedInstanceState);
			Org.Fmod.FMOD.Init(this);
		}

		protected override void OnDestroy()
		{
			Org.Fmod.FMOD.Close();
			if (alive.ElapsedMilliseconds > QuickExitMs)
				GraphicsDriver.MarkLaunchOk(this);
			base.OnDestroy();
			if (IsFinishing)
				Process.KillProcess(Process.MyPid());
		}

		protected override void Main()
		{
			CultureFix.Apply();
			surviveTimer = new System.Threading.Timer(_ => GraphicsDriver.MarkLaunchOk(this), null, SurviveMs, System.Threading.Timeout.Infinite);

			FNALoggerEXT.LogInfo = msg => Log.Info(LogTag, msg);
			FNALoggerEXT.LogWarn = msg => Log.Warn(LogTag, msg);
			FNALoggerEXT.LogError = msg => Log.Error(LogTag, msg);

			SDL3.SDL.SDL_SetHint("SDL_ORIENTATIONS", "LandscapeLeft LandscapeRight");

			string? driver = Intent?.GetStringExtra(ExtraDriver);
			if (!string.IsNullOrEmpty(driver))
				SDL3.SDL.SDL_SetHint("FNA3D_FORCE_DRIVER", driver);

			AppContext.SetData("CelesteAndroid.ShowFps", Intent?.GetBooleanExtra(ExtraShowFps, false) == true ? "1" : "0");
			AppContext.SetData("CelesteAndroid.HideTouch", Intent?.GetBooleanExtra(ExtraHideTouch, false) == true ? "1" : "0");
			AppContext.SetData("CelesteAndroid.TouchOpacity",
				(Intent?.GetIntExtra(ExtraTouchOpacity, GameOptions.DefaultOpacity) ?? GameOptions.DefaultOpacity).ToString(System.Globalization.CultureInfo.InvariantCulture));

			try
			{
				if (GameInstaller.IsInstalled(this))
				{
					CelesteLauncher.Run(this);
				}
				else
				{
					Log.Warn(LogTag, "Jogo não instalado; rodando HelloGame.");
					using HelloGame game = new();
					game.Run();
				}
			}
			catch (Exception e)
			{
				Log.Error(LogTag, (e is TargetInvocationException { InnerException: not null } tie ? tie.InnerException! : e).ToString());
				throw;
			}
		}
	}
}
