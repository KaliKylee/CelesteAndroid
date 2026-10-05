using Android.Content;

namespace CelesteAndroid
{
	/// <summary>
	/// Opções da tela inicial (menu "Opções") e a opacidade dos controles (editor), guardadas nas
	/// preferências "launcher". O jogo roda em outro processo, então os valores seguem para ele
	/// como extras do Intent (ver GameActivity) e chegam ao TouchControls via HostConfig.
	/// </summary>
	public static class GameOptions
	{
		private const string PrefShowFps = "show_fps";
		private const string PrefHideTouch = "hide_touch";
		private const string PrefOpacity = "touch_opacity";

		/// <summary>Mesmo valor padrão de TouchControls.Opacity (0.45).</summary>
		public const int DefaultOpacity = 45;

		public static ISharedPreferences Prefs(Context context) =>
			context.GetSharedPreferences("launcher", FileCreationMode.Private)!;

		public static bool ShowFps(ISharedPreferences prefs) => prefs.GetBoolean(PrefShowFps, false);

		public static void SetShowFps(ISharedPreferences prefs, bool value) =>
			prefs.Edit()!.PutBoolean(PrefShowFps, value)!.Apply();

		public static bool HideTouch(ISharedPreferences prefs) => prefs.GetBoolean(PrefHideTouch, false);

		public static void SetHideTouch(ISharedPreferences prefs, bool value) =>
			prefs.Edit()!.PutBoolean(PrefHideTouch, value)!.Apply();

		/// <summary>Opacidade dos controles em % (0..100).</summary>
		public static int Opacity(ISharedPreferences prefs) =>
			System.Math.Clamp(prefs.GetInt(PrefOpacity, DefaultOpacity), 0, 100);

		public static void SetOpacity(ISharedPreferences prefs, int percent) =>
			prefs.Edit()!.PutInt(PrefOpacity, System.Math.Clamp(percent, 0, 100))!.Apply();
	}
}
