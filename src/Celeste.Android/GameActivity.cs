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
	/// <summary>
	/// Roda o jogo. Fica num processo próprio (":game") porque o Celeste e o FNA guardam estado estático:
	/// cada partida começa num processo novo, e o processo morre quando o jogo fecha.
	/// </summary>
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

		// O Java carrega SDL3 e FMOD (o FMOD precisa estar carregado antes do FMOD.init);
		// FNA3D/FAudio são carregados pelo .NET via DllImport.
		protected override string[] GetLibraries() => new[] { "SDL3", "fmod", "fmodstudio" };

		protected override void OnCreate(Bundle? savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			// O FMOD no Android precisa do Context para o áudio e para ler arquivos.
			Org.Fmod.FMOD.Init(this);
		}

		protected override void OnDestroy()
		{
			Org.Fmod.FMOD.Close();
			base.OnDestroy();
			if (IsFinishing)
				Process.KillProcess(Process.MyPid());
		}

		// Chamado pelo SDL na thread "SDLThread" depois que a superfície existe; substitui o SDL_main nativo.
		protected override void Main()
		{
			// O FNA/FNA3D loga no stderr, que no Android não vai para o logcat.
			FNALoggerEXT.LogInfo = msg => Log.Info(LogTag, msg);
			FNALoggerEXT.LogWarn = msg => Log.Warn(LogTag, msg);
			FNALoggerEXT.LogError = msg => Log.Error(LogTag, msg);

			// Sem este hint o SDL escolhe a orientação ao criar a janela (rotação livre / retrato).
			// Só paisagem, nos dois sentidos.
			SDL3.SDL.SDL_SetHint("SDL_ORIENTATIONS", "LandscapeLeft LandscapeRight");

			// "OpenGL" força o driver GLES; o padrão é Vulkan (SDL_GPU).
			string? driver = Intent?.GetStringExtra(ExtraDriver);
			if (!string.IsNullOrEmpty(driver))
				SDL3.SDL.SDL_SetHint("FNA3D_FORCE_DRIVER", driver);

			// Opções da tela inicial; lidas pelo TouchControls via HostConfig (Celeste.Android.Patches).
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
