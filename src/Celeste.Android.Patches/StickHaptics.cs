using System;

namespace CelesteAndroid
{
	// Independent of Android/FNA so the angular edge cases can be tested directly.
	public sealed class StickHaptics
	{
		private const double Step = Math.PI / 4;
		private const double Enter = 5 * Math.PI / 180;
		private const double Rearm = 9 * Math.PI / 180;
		private double? previous;
		private int lastAxis = -1;

		private static double Wrap(double angle) => Math.Atan2(Math.Sin(angle), Math.Cos(angle));
		private static int Axis(int index) => (index % 8 + 8) % 8;

		public bool Update(float x, float y)
		{
			if (x * x + y * y < 0.000001f)
			{
				previous = null;
				lastAxis = -1;
				return false;
			}
			double angle = Math.Atan2(y, x);
			if (lastAxis >= 0 && Math.Abs(Wrap(angle - lastAxis * Step)) > Rearm)
				lastAxis = -1;
			int nearest = (int)Math.Round(angle / Step);
			int target = Math.Abs(Wrap(angle - nearest * Step)) <= Enter ? Axis(nearest) : -1;
			if (target < 0 && previous.HasValue)
			{
				// Detect a crossed axis even if a fast swipe skips its 10-degree window.
				double start = previous.Value;
				double delta = Wrap(angle - start);
				double end = start + delta;
				if (delta > 0 && Math.Floor(end / Step) > Math.Floor(start / Step))
					target = Axis((int)Math.Floor(end / Step));
				else if (delta < 0 && Math.Ceiling(end / Step) < Math.Ceiling(start / Step))
					target = Axis((int)Math.Ceiling(end / Step));
			}
			previous = angle;
			if (target < 0 || target == lastAxis)
				return false;
			lastAxis = target;
			return true;
		}
	}
}
