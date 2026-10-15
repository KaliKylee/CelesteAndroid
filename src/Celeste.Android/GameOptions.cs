using Android.Content;

namespace CelesteAndroid
{
	public static class GameOptions
	{
		private const string PrefShowFps = "show_fps";
		private const string PrefHideTouch = "hide_touch";
		private const string PrefOpacity = "touch_opacity";

		public static HapticPolicy Vibration(ISharedPreferences prefs) => new(
			prefs.GetBoolean("vibration_enabled", true), prefs.GetInt("vibration_mode", 0),
			prefs.GetInt("vibration_intensity", 50), prefs.GetBoolean("vibration_joystick", true));

		public static void PutVibrationExtras(Intent intent, ISharedPreferences prefs)
		{
			var value = Vibration(prefs);
			intent.PutExtra("vibration_enabled", value.Enabled);
			intent.PutExtra("vibration_mode", value.Mode);
			intent.PutExtra("vibration_intensity", value.Intensity);
			intent.PutExtra("vibration_joystick", value.Joystick);
		}

		public static HapticPolicy VibrationFromIntent(Intent? intent) => new(
			intent?.GetBooleanExtra("vibration_enabled", true) ?? true,
			intent?.GetIntExtra("vibration_mode", 0) ?? 0,
			intent?.GetIntExtra("vibration_intensity", 50) ?? 50,
			intent?.GetBooleanExtra("vibration_joystick", true) ?? true);

		public static void SetVibration(ISharedPreferences prefs, HapticPolicy value) =>
			prefs.Edit()!.PutBoolean("vibration_enabled", value.Enabled)!
				.PutInt("vibration_mode", value.Mode)!.PutInt("vibration_intensity", value.Intensity)!
				.PutBoolean("vibration_joystick", value.Joystick)!.Apply();

		public const int DefaultOpacity = 45;

		public static bool DynamicJoystick(ISharedPreferences prefs) => prefs.GetBoolean("dynamic_joystick", false);

		public static void SetDynamicJoystick(ISharedPreferences prefs, bool value) =>
			prefs.Edit()!.PutBoolean("dynamic_joystick", value)!.Apply();

		public static ISharedPreferences Prefs(Context context) =>
			context.GetSharedPreferences("launcher", FileCreationMode.Private)!;

		public static bool ShowFps(ISharedPreferences prefs) => prefs.GetBoolean(PrefShowFps, false);

		public static void SetShowFps(ISharedPreferences prefs, bool value) =>
			prefs.Edit()!.PutBoolean(PrefShowFps, value)!.Apply();

		public static bool HideTouch(ISharedPreferences prefs) => prefs.GetBoolean(PrefHideTouch, false);

		public static void SetHideTouch(ISharedPreferences prefs, bool value) =>
			prefs.Edit()!.PutBoolean(PrefHideTouch, value)!.Apply();

		public static int Opacity(ISharedPreferences prefs) =>
			System.Math.Clamp(prefs.GetInt(PrefOpacity, DefaultOpacity), 0, 100);

		public static void SetOpacity(ISharedPreferences prefs, int percent) =>
			prefs.Edit()!.PutInt(PrefOpacity, System.Math.Clamp(percent, 0, 100))!.Apply();
	}
}
