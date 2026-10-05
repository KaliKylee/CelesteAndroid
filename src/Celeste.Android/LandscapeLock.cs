using Android.Content.PM;

namespace CelesteAndroid
{
	public static class LandscapeLock
	{
		public const ScreenOrientation Orientation = ScreenOrientation.SensorLandscape;

		public static ScreenOrientation Coerce(ScreenOrientation requested) => requested switch
		{
			ScreenOrientation.Landscape
				or ScreenOrientation.ReverseLandscape
				or ScreenOrientation.SensorLandscape
				or ScreenOrientation.UserLandscape => requested,
			_ => Orientation,
		};
	}
}
