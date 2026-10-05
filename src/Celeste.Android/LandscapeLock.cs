using Android.Content.PM;

namespace CelesteAndroid
{
	/// <summary>
	/// O app só existe na horizontal. Qualquer pedido de outra orientação (por exemplo, o SDL ao criar a
	/// janela, antes do hint SDL_ORIENTATIONS valer) é trocado por paisagem nos dois sentidos.
	/// </summary>
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
