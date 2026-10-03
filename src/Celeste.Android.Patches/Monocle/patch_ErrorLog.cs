using MonoMod;

namespace Monocle
{
	[MonoModPatch("Monocle.ErrorLog")]
	public static class patch_ErrorLog
	{
		/// <summary>
		/// O original abre o errorLog.txt com o programa padrão do sistema (Process.Start),
		/// o que não existe no Android. O log continua sendo gravado por ErrorLog.Write.
		/// </summary>
		[MonoModReplace]
		public static void Open()
		{
		}
	}
}
