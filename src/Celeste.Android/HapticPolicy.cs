using System;

namespace CelesteAndroid
{
	public readonly struct HapticPolicy
	{
		public bool Enabled { get; }
		// 0 = press and release, 1 = press only, 2 = release only.
		public int Mode { get; }
		public int Intensity { get; }
		public bool Joystick { get; }

		public HapticPolicy(bool enabled, int mode, int intensity, bool joystick)
		{
			Enabled = enabled;
			Mode = mode is >= 0 and <= 2 ? mode : 0;
			Intensity = Math.Clamp(intensity, 0, 100);
			Joystick = joystick;
		}

		public bool ShouldPlay(int kind) => Enabled && Intensity > 0 && (kind switch
		{
			1 => Mode != 2,
			2 => Mode != 1,
			3 => Joystick,
			_ => false,
		});

		public int Amplitude => Math.Max(1, (int)Math.Round(Intensity * 255d / 100));

		public int DurationMs(int kind, bool amplitudeControl)
		{
			int duration = kind switch { 1 => 12, 2 => 8, _ => 6 };
			return amplitudeControl ? duration : Math.Max(1, (int)Math.Round(duration * Intensity / 100d));
		}
	}
}
