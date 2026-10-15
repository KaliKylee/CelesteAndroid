using Android.Content;
using Android.Media;
using Android.OS;
using Android.Provider;

namespace CelesteAndroid
{
	// Called on the UI thread, both from the game and from the settings preview.
	public sealed class TouchVibration
	{
		private readonly Context context;
		private readonly Vibrator? vibrator;
		private readonly AudioAttributes attributes;

		public TouchVibration(Context context)
		{
			this.context = context;
			vibrator = Build.VERSION.SdkInt >= BuildVersionCodes.S
				? (context.GetSystemService(Context.VibratorManagerService) as VibratorManager)?.DefaultVibrator
				: context.GetSystemService(Context.VibratorService) as Vibrator;
			attributes = new AudioAttributes.Builder()!
				.SetUsage(AudioUsageKind.AssistanceSonification)!
				.SetContentType(AudioContentType.Sonification)!.Build()!;
		}

		public bool Available => vibrator?.HasVibrator == true;
		public bool AmplitudeControl => vibrator?.HasAmplitudeControl == true;

		public void Play(int kind, HapticPolicy policy)
		{
			if (!policy.ShouldPlay(kind) || !Available) return;
			// Direct amplitude control still respects the user's system touch-feedback switch.
			if (Settings.System.GetInt(context.ContentResolver, "haptic_feedback_enabled", 1) == 0) return;
			using var effect = VibrationEffect.CreateOneShot(policy.DurationMs(kind, AmplitudeControl),
				AmplitudeControl ? policy.Amplitude : VibrationEffect.DefaultAmplitude);
			vibrator!.Vibrate(effect, attributes);
		}

		public void Cancel() => vibrator?.Cancel();
	}
}
