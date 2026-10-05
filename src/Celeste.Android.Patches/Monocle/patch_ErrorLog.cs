using MonoMod;

namespace Monocle
{
	[MonoModPatch("Monocle.ErrorLog")]
	public static class patch_ErrorLog
	{
		[MonoModReplace]
		public static void Open()
		{
		}
	}
}
