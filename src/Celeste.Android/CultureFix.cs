using System;
using System.Globalization;

namespace CelesteAndroid
{
	// Usa a cultura neutra do .NET para números, datas e comparação de texto. Isso evita falhas em idiomas
	// como o árabe (separador decimal "٫", calendário Hijri etc.). Não muda o idioma do app, do jogo
	// nem o do sistema: o idioma da interface continua vindo do Locale do Android.
	public static class CultureFix
	{
		public static void Apply()
		{
			try
			{
				CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
				CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
			}
			catch (Exception)
			{
			}
		}
	}
}
