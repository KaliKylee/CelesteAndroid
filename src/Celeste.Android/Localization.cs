using System;
using System.Collections.Generic;
using Android.Content;

namespace CelesteAndroid
{
	public enum Lang { Pt, En, Es }

	/// <summary>
	/// Textos da tela inicial em Português, Inglês e Espanhol.
	/// O idioma padrão segue o do sistema; a escolha manual fica salva nas preferências.
	/// </summary>
	public static class L
	{
		private const string PrefLang = "lang";

		public static Lang Current { get; private set; } = Lang.En;

		public static readonly Lang[] All = { Lang.Pt, Lang.En, Lang.Es };

		public static string Flag(Lang l) => l switch { Lang.Pt => "🇧🇷", Lang.Es => "🇪🇸", _ => "🇺🇸" };

		public static string Name(Lang l) => l switch { Lang.Pt => "Português", Lang.Es => "Español", _ => "English" };

		/// <summary>Carrega o idioma salvo; sem escolha manual, usa o do sistema (padrão: inglês).</summary>
		public static void Init(ISharedPreferences prefs)
		{
			string? saved = prefs.GetString(PrefLang, null);
			if (saved != null && Enum.TryParse(saved, out Lang parsed))
			{
				Current = parsed;
				return;
			}
			Current = FromSystem();
		}

		public static void Set(ISharedPreferences prefs, Lang lang)
		{
			Current = lang;
			prefs.Edit()!.PutString(PrefLang, lang.ToString())!.Apply();
		}

		private static Lang FromSystem()
		{
			string code = Java.Util.Locale.Default?.Language ?? "en";
			return code switch
			{
				"pt" => Lang.Pt,
				"es" => Lang.Es,
				_ => Lang.En,
			};
		}

		private static string T(string pt, string en, string es) => Current switch
		{
			Lang.Pt => pt,
			Lang.Es => es,
			_ => en,
		};

		// ---- Tela inicial ----
		public static string Subtitle => T("Port nativo para Android", "Native port for Android", "Port nativo para Android");
		public static string Play => T("JOGAR", "PLAY", "JUGAR");
		public static string SelectFiles => T("Selecionar arquivos do jogo", "Select game files", "Seleccionar archivos del juego");
		public static string ChangeFiles => T("Mudar arquivos do jogo", "Change game files", "Cambiar archivos del juego");
		public static string ImportZip => T("Importar .zip", "Import .zip", "Importar .zip");
		public static string ImportSaves => T("Importar saves", "Import saves", "Importar partidas");
		public static string Graphics => T("Gráficos", "Graphics", "Gráficos");
		public static string GraphicsFallback => T(
			"O jogo travou ao iniciar com Vulkan. Troquei para OpenGL ES; você pode mudar em Gráficos.",
			"The game crashed on start with Vulkan. Switched to OpenGL ES; you can change it under Graphics.",
			"El juego falló al iniciar con Vulkan. Cambié a OpenGL ES; puedes cambiarlo en Gráficos.");
		public static string Options => T("Opções", "Options", "Opciones");
		public static string EditControls => T("Editar controles", "Edit controls", "Editar controles");
		public static string ShowFps => T("Mostrar FPS", "Show FPS", "Mostrar FPS");
		public static string HideTouchButtons => T("Ocultar botões de toque", "Hide touch buttons", "Ocultar botones táctiles");
		public static string ReadyToPlay => T("✓  Pronto pra jogar", "✓  Ready to play", "✓  Listo para jugar");
		public static string PickPrompt => T(
			"Selecione a pasta da sua cópia do Celeste para PC (FNA, opengl Build ou do itch.io .zip).",
			"Select the folder of your PC copy of Celeste (FNA, opengl Build or the itch.io .zip).",
			"Selecciona la carpeta de tu copia de Celeste para PC (FNA, opengl Build o el .zip de itch.io).");
		public static string Imported => T("✓  Jogo importado! Pronto pra jogar", "✓  Game imported! Ready to play", "✓  ¡Juego importado! Listo para jugar");
		public static string SavesImported(int n) => T($"✓  {n} arquivo(s) de save importado(s)", $"✓  {n} save file(s) imported", $"✓  {n} archivo(s) de partida importado(s)");
		public static string SomethingWrong(string msg) => T("Algo deu errado: ", "Something went wrong: ", "Algo salió mal: ") + msg;

		public static string PortBy => T("Port por", "Port by", "Port por");

		// ---- Editor de controles ----
		public static string EditorHint => T(
			"Arraste os controles para mudar de lugar. Use as barras para mudar o tamanho e a opacidade.",
			"Drag the controls to move them. Use the sliders to change size and opacity.",
			"Arrastra los controles para moverlos. Usa las barras para cambiar el tamaño y la opacidad.");
		public static string Opacity => T("Opacidade", "Opacity", "Opacidad");
		public static string Reset => T("Resetar", "Reset", "Restablecer");
		public static string Cancel => T("Cancelar", "Cancel", "Cancelar");
		public static string Save => T("Salvar", "Save", "Guardar");
		public static string ControlsSaved => T("Controles salvos", "Controls saved", "Controles guardados");
		public static string[] ControlNames => Current switch
		{
			Lang.Pt => new[] { "Analógico", "Pular", "Dash", "Agarrar", "Pausar", "Tab" },
			Lang.Es => new[] { "Analógico", "Saltar", "Dash", "Agarrar", "Pausa", "Tab" },
			_ => new[] { "Stick", "Jump", "Dash", "Grab", "Pause", "Tab" },
		};

		// ---- Instalador ----
		public static string Searching => T("Procurando por Celeste na pasta…", "Looking for Celeste in the folder…", "Buscando Celeste en la carpeta…");
		public static string NotFound => T("Não encontrei o Celeste.exe e a pasta Content aí.", "Couldn't find Celeste.exe and the Content folder there.", "No encontré Celeste.exe y la carpeta Content ahí.");
		public static string ReadingZip => T("Lendo o .zip…", "Reading the .zip…", "Leyendo el .zip…");
		public static string CantOpenFile => T("Não consegui abrir o arquivo.", "Couldn't open the file.", "No pude abrir el archivo.");
		public static string ZipNoExe => T("Este .zip não contém o Celeste.exe.", "This .zip doesn't contain Celeste.exe.", "Este .zip no contiene Celeste.exe.");
		public static string PreparingEmbedded => T("Preparando o jogo incluído…", "Preparing the bundled game…", "Preparando el juego incluido…");
		public static string SearchingSaves => T("Procurando arquivos de save…", "Looking for save files…", "Buscando archivos de partida…");
		public static string NoSaves => T("Nenhum arquivo de save .celeste nessa pasta.", "No .celeste save files in that folder.", "Ningún archivo de partida .celeste en esa carpeta.");
		public static string Copying(int i, int n) => T($"Copiando arquivos do jogo… {i}/{n}", $"Copying game files… {i}/{n}", $"Copiando archivos del juego… {i}/{n}");
		public static string XnaVersion => T(
			"Esta é a versão XNA do Celeste. Na Steam, ative o beta \"opengl\" (Propriedades → Betas) e copie a pasta de novo, ou use o .zip do Linux do itch.io.",
			"This is the XNA version of Celeste. On Steam, enable the \"opengl\" beta (Properties → Betas) and copy the folder again, or use the Linux .zip from itch.io.",
			"Esta es la versión XNA de Celeste. En Steam, activa la beta \"opengl\" (Propiedades → Betas) y copia la carpeta de nuevo, o usa el .zip de Linux de itch.io.");
		public static string CopyIncomplete => T("A cópia está incompleta.", "The copy is incomplete.", "La copia está incompleta.");
		public static string Patching => T("Adaptando o jogo para Android…", "Adapting the game for Android…", "Adaptando el juego para Android…");
		public static string MakingBackground => T("Gerando o fundo…", "Generating the background…", "Generando el fondo…");
	}
}
