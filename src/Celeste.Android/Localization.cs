using System;
using System.Collections.Generic;
using Android.Content;

namespace CelesteAndroid
{
	// ATENÇÃO: não mude a ordem dos itens (o nome é salvo nas preferências); só acrescente no fim.
	public enum Lang { Pt, En, Es, Ja, Fr, It, De, Ru, Zh, Ko }

	public static class L
	{
		private const string PrefLang = "lang";

		public static Lang Current { get; private set; } = Lang.En;

		public static readonly Lang[] All =
		{
			Lang.Pt, Lang.En, Lang.Es, Lang.Fr, Lang.It, Lang.De, Lang.Ru, Lang.Ja, Lang.Zh, Lang.Ko,
		};

		public static string Flag(Lang l) => l switch
		{
			Lang.Pt => "🇧🇷",
			Lang.Es => "🇪🇸",
			Lang.Ja => "🇯🇵",
			Lang.Fr => "🇫🇷",
			Lang.It => "🇮🇹",
			Lang.De => "🇩🇪",
			Lang.Ru => "🇷🇺",
			Lang.Zh => "🇨🇳",
			Lang.Ko => "🇰🇷",
			_ => "🇺🇸",
		};

		public static string Name(Lang l) => l switch
		{
			Lang.Pt => "Português",
			Lang.Es => "Español",
			Lang.Ja => "日本語",
			Lang.Fr => "Français",
			Lang.It => "Italiano",
			Lang.De => "Deutsch",
			Lang.Ru => "Русский",
			Lang.Zh => "简体中文",
			Lang.Ko => "한국어",
			_ => "English",
		};

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
				"ja" => Lang.Ja,
				"fr" => Lang.Fr,
				"it" => Lang.It,
				"de" => Lang.De,
				"ru" => Lang.Ru,
				"zh" => Lang.Zh,
				"ko" => Lang.Ko,
				_ => Lang.En,
			};
		}

		// Ordem dos argumentos: pt, en, es, ja, fr, it, de, ru, zh, ko
		private static TValue V<TValue>(TValue pt, TValue en, TValue es, TValue ja, TValue fr, TValue it, TValue de, TValue ru, TValue zh, TValue ko) => Current switch
		{
			Lang.Pt => pt,
			Lang.Es => es,
			Lang.Ja => ja,
			Lang.Fr => fr,
			Lang.It => it,
			Lang.De => de,
			Lang.Ru => ru,
			Lang.Zh => zh,
			Lang.Ko => ko,
			_ => en,
		};

		private static string T(string pt, string en, string es, string ja, string fr, string it, string de, string ru, string zh, string ko)
			=> V(pt, en, es, ja, fr, it, de, ru, zh, ko);

		public static string Subtitle => T(
			"Port nativo para Android",
			"Native port for Android",
			"Port nativo para Android",
			"Android向けネイティブポート",
			"Port natif pour Android",
			"Port nativo per Android",
			"Native Portierung für Android",
			"Нативный порт для Android",
			"Android 原生移植版",
			"Android 네이티브 포트");

		public static string Play => T(
			"JOGAR", "PLAY", "JUGAR", "プレイ", "JOUER", "GIOCA", "SPIELEN", "ИГРАТЬ", "开始游戏", "플레이");

		public static string SelectFiles => T(
			"Selecionar arquivos do jogo",
			"Select game files",
			"Seleccionar archivos del juego",
			"ゲームファイルを選択",
			"Sélectionner les fichiers du jeu",
			"Seleziona i file del gioco",
			"Spieldateien auswählen",
			"Выбрать файлы игры",
			"选择游戏文件",
			"게임 파일 선택");

		public static string ChangeFiles => T(
			"Mudar arquivos do jogo",
			"Change game files",
			"Cambiar archivos del juego",
			"ゲームファイルを変更",
			"Changer les fichiers du jeu",
			"Cambia i file del gioco",
			"Spieldateien ändern",
			"Изменить файлы игры",
			"更换游戏文件",
			"게임 파일 변경");

		public static string ImportZip => T(
			"Importar .zip",
			"Import .zip",
			"Importar .zip",
			".zipをインポート",
			"Importer un .zip",
			"Importa .zip",
			".zip importieren",
			"Импорт .zip",
			"导入 .zip",
			".zip 가져오기");

		public static string ImportSaves => T(
			"Importar saves",
			"Import saves",
			"Importar partidas",
			"セーブデータをインポート",
			"Importer les sauvegardes",
			"Importa i salvataggi",
			"Spielstände importieren",
			"Импорт сохранений",
			"导入存档",
			"세이브 가져오기");

		public static string Graphics => T(
			"Gráficos", "Graphics", "Gráficos", "グラフィック", "Graphismes", "Grafica", "Grafik", "Графика", "图形", "그래픽");

		public static string GraphicsFallback => T(
			"O jogo travou ao iniciar com Vulkan. Troquei para OpenGL ES; você pode mudar em Gráficos.",
			"The game crashed on start with Vulkan. Switched to OpenGL ES; you can change it under Graphics.",
			"El juego falló al iniciar con Vulkan. Cambié a OpenGL ES; puedes cambiarlo en Gráficos.",
			"Vulkanでの起動時にゲームがクラッシュしました。OpenGL ESに切り替えました。グラフィック設定から変更できます。",
			"Le jeu a planté au démarrage avec Vulkan. Passage à OpenGL ES ; vous pouvez le modifier dans Graphismes.",
			"Il gioco si è bloccato all'avvio con Vulkan. Sono passato a OpenGL ES; puoi cambiarlo in Grafica.",
			"Das Spiel ist beim Start mit Vulkan abgestürzt. Ich habe auf OpenGL ES umgestellt; du kannst das unter Grafik ändern.",
			"Игра вылетела при запуске с Vulkan. Я переключился на OpenGL ES; это можно изменить в разделе «Графика».",
			"游戏使用 Vulkan 启动时崩溃了。已切换为 OpenGL ES；你可以在“图形”中更改。",
			"Vulkan으로 시작할 때 게임이 충돌했습니다. OpenGL ES로 전환했으며, 그래픽 설정에서 변경할 수 있습니다.");

		public static string Options => T(
			"Opções", "Options", "Opciones", "オプション", "Options", "Opzioni", "Optionen", "Настройки", "选项", "옵션");

		public static string EditControls => T(
			"Editar controles",
			"Edit controls",
			"Editar controles",
			"コントロールを編集",
			"Modifier les commandes",
			"Modifica i controlli",
			"Steuerung bearbeiten",
			"Изменить управление",
			"编辑控制",
			"컨트롤 편집");

		public static string ShowFps => T(
			"Mostrar FPS",
			"Show FPS",
			"Mostrar FPS",
			"FPSを表示",
			"Afficher les FPS",
			"Mostra FPS",
			"FPS anzeigen",
			"Показывать FPS",
			"显示 FPS",
			"FPS 표시");

		public static string HideTouchButtons => T(
			"Ocultar botões de toque",
			"Hide touch buttons",
			"Ocultar botones táctiles",
			"タッチボタンを非表示",
			"Masquer les boutons tactiles",
			"Nascondi i pulsanti touch",
			"Touch-Tasten ausblenden",
			"Скрыть сенсорные кнопки",
			"隐藏触控按钮",
			"터치 버튼 숨기기");

		public static string ReadyToPlay => T(
			"✓  Pronto pra jogar",
			"✓  Ready to play",
			"✓  Listo para jugar",
			"✓  プレイの準備完了",
			"✓  Prêt à jouer",
			"✓  Pronto per giocare",
			"✓  Bereit zum Spielen",
			"✓  Готово к игре",
			"✓  已准备就绪",
			"✓  플레이 준비 완료");

		public static string PickPrompt => T(
			"Selecione a pasta da sua cópia do Celeste para PC (FNA, opengl Build ou do itch.io .zip).",
			"Select the folder of your PC copy of Celeste (FNA, opengl Build or the itch.io .zip).",
			"Selecciona la carpeta de tu copia de Celeste para PC (FNA, opengl Build o el .zip de itch.io).",
			"PC版Celesteのフォルダ（FNA、opengl Build、またはitch.ioの.zip）を選択してください。",
			"Sélectionnez le dossier de votre copie PC de Celeste (FNA, build opengl ou le .zip d'itch.io).",
			"Seleziona la cartella della tua copia di Celeste per PC (FNA, build opengl o il .zip di itch.io).",
			"Wähle den Ordner deiner PC-Kopie von Celeste (FNA, opengl-Build oder die .zip von itch.io).",
			"Выберите папку с вашей копией Celeste для ПК (FNA, сборка opengl или .zip с itch.io).",
			"请选择你的 PC 版 Celeste 文件夹（FNA、opengl 版本或 itch.io 的 .zip）。",
			"PC용 Celeste 폴더(FNA, opengl 빌드 또는 itch.io의 .zip)를 선택하세요.");

		public static string Imported => T(
			"✓  Jogo importado! Pronto pra jogar",
			"✓  Game imported! Ready to play",
			"✓  ¡Juego importado! Listo para jugar",
			"✓  ゲームをインポートしました！プレイの準備完了",
			"✓  Jeu importé ! Prêt à jouer",
			"✓  Gioco importato! Pronto per giocare",
			"✓  Spiel importiert! Bereit zum Spielen",
			"✓  Игра импортирована! Готово к игре",
			"✓  游戏已导入！已准备就绪",
			"✓  게임을 가져왔습니다! 플레이 준비 완료");

		public static string SavesImported(int n) => T(
			$"✓  {n} arquivo(s) de save importado(s)",
			$"✓  {n} save file(s) imported",
			$"✓  {n} archivo(s) de partida importado(s)",
			$"✓  セーブファイルを{n}件インポートしました",
			$"✓  {n} fichier(s) de sauvegarde importé(s)",
			$"✓  {n} file di salvataggio importato/i",
			$"✓  {n} Speicherdatei(en) importiert",
			$"✓  Импортировано файлов сохранений: {n}",
			$"✓  已导入 {n} 个存档文件",
			$"✓  세이브 파일 {n}개를 가져왔습니다");

		public static string SomethingWrong(string msg) => T(
			"Algo deu errado: ",
			"Something went wrong: ",
			"Algo salió mal: ",
			"問題が発生しました: ",
			"Un problème est survenu : ",
			"Qualcosa è andato storto: ",
			"Etwas ist schiefgelaufen: ",
			"Что-то пошло не так: ",
			"出错了：",
			"문제가 발생했습니다: ") + msg;

		public static string PortBy => T(
			"Port por", "Port by", "Port por", "移植:", "Port par", "Port di", "Port von", "Порт от", "移植:", "포팅:");

		public static string EditorHint => T(
			"Arraste os controles para mudar de lugar. Use as barras para mudar o tamanho e a opacidade, e a grade para alinhar.",
			"Drag the controls to move them. Use the sliders to change size and opacity, and the grid to align.",
			"Arrastra los controles para moverlos. Usa las barras para cambiar el tamaño y la opacidad, y la cuadrícula para alinear.",
			"コントロールをドラッグして移動します。スライダーでサイズと不透明度を変更し、グリッドで位置を揃えられます。",
			"Faites glisser les commandes pour les déplacer. Utilisez les curseurs pour changer la taille et l'opacité, et la grille pour aligner.",
			"Trascina i controlli per spostarli. Usa i cursori per cambiare dimensione e opacità, e la griglia per allinearli.",
			"Ziehe die Steuerelemente, um sie zu verschieben. Mit den Reglern änderst du Größe und Deckkraft, mit dem Raster richtest du sie aus.",
			"Перетаскивайте элементы управления, чтобы переместить их. Ползунками меняйте размер и прозрачность, а сеткой выравнивайте.",
			"拖动控件即可移动。使用滑块调整大小和不透明度，使用网格进行对齐。",
			"컨트롤을 드래그하여 이동하세요. 슬라이더로 크기와 불투명도를 조절하고, 격자로 정렬할 수 있습니다.");

		public static string GridLabel(int mode) => mode switch
		{
			1 => T("Grade: grande", "Grid: large", "Cuadrícula: grande", "グリッド: 大", "Grille : grande", "Griglia: grande", "Raster: groß", "Сетка: крупная", "网格：大", "격자: 크게"),
			2 => T("Grade: média", "Grid: medium", "Cuadrícula: media", "グリッド: 中", "Grille : moyenne", "Griglia: media", "Raster: mittel", "Сетка: средняя", "网格：中", "격자: 보통"),
			3 => T("Grade: fina", "Grid: fine", "Cuadrícula: fina", "グリッド: 小", "Grille : fine", "Griglia: fine", "Raster: fein", "Сетка: мелкая", "网格：小", "격자: 작게"),
			_ => T("Grade: desligada", "Grid: off", "Cuadrícula: apagada", "グリッド: オフ", "Grille : désactivée", "Griglia: disattivata", "Raster: aus", "Сетка: выкл.", "网格：关闭", "격자: 끔"),
		};

		public static string[] FpsCorners => V(
			new[] { "FPS ↖ Sup. esq.", "FPS ↗ Sup. dir.", "FPS ↙ Inf. esq.", "FPS ↘ Inf. dir." },
			new[] { "FPS ↖ Top left", "FPS ↗ Top right", "FPS ↙ Bottom left", "FPS ↘ Bottom right" },
			new[] { "FPS ↖ Sup. izq.", "FPS ↗ Sup. der.", "FPS ↙ Inf. izq.", "FPS ↘ Inf. der." },
			new[] { "FPS ↖ 左上", "FPS ↗ 右上", "FPS ↙ 左下", "FPS ↘ 右下" },
			new[] { "FPS ↖ Haut gauche", "FPS ↗ Haut droite", "FPS ↙ Bas gauche", "FPS ↘ Bas droite" },
			new[] { "FPS ↖ Alto sx", "FPS ↗ Alto dx", "FPS ↙ Basso sx", "FPS ↘ Basso dx" },
			new[] { "FPS ↖ Oben links", "FPS ↗ Oben rechts", "FPS ↙ Unten links", "FPS ↘ Unten rechts" },
			new[] { "FPS ↖ Верх слева", "FPS ↗ Верх справа", "FPS ↙ Низ слева", "FPS ↘ Низ справа" },
			new[] { "FPS ↖ 左上", "FPS ↗ 右上", "FPS ↙ 左下", "FPS ↘ 右下" },
			new[] { "FPS ↖ 좌상단", "FPS ↗ 우상단", "FPS ↙ 좌하단", "FPS ↘ 우하단" });

		public static string Opacity => T(
			"Opacidade", "Opacity", "Opacidad", "不透明度", "Opacité", "Opacità", "Deckkraft", "Непрозрачность", "不透明度", "불투명도");

		public static string Reset => T(
			"Resetar", "Reset", "Restablecer", "リセット", "Réinitialiser", "Ripristina", "Zurücksetzen", "Сбросить", "重置", "초기화");

		public static string Cancel => T(
			"Cancelar", "Cancel", "Cancelar", "キャンセル", "Annuler", "Annulla", "Abbrechen", "Отмена", "取消", "취소");

		public static string Save => T(
			"Salvar", "Save", "Guardar", "保存", "Enregistrer", "Salva", "Speichern", "Сохранить", "保存", "저장");

		public static string ControlsSaved => T(
			"Controles salvos",
			"Controls saved",
			"Controles guardados",
			"コントロールを保存しました",
			"Commandes enregistrées",
			"Controlli salvati",
			"Steuerung gespeichert",
			"Управление сохранено",
			"控制设置已保存",
			"컨트롤이 저장되었습니다");

		public static string[] ControlNames => V(
			new[] { "Analógico", "Pular", "Dash", "Agarrar", "Pausar", "Tab" },
			new[] { "Stick", "Jump", "Dash", "Grab", "Pause", "Tab" },
			new[] { "Analógico", "Saltar", "Dash", "Agarrar", "Pausa", "Tab" },
			new[] { "スティック", "ジャンプ", "ダッシュ", "つかむ", "ポーズ", "Tab" },
			new[] { "Stick", "Saut", "Dash", "Agripper", "Pause", "Tab" },
			new[] { "Stick", "Salto", "Dash", "Presa", "Pausa", "Tab" },
			new[] { "Stick", "Springen", "Dash", "Greifen", "Pause", "Tab" },
			new[] { "Стик", "Прыжок", "Рывок", "Хват", "Пауза", "Tab" },
			new[] { "摇杆", "跳跃", "冲刺", "抓取", "暂停", "Tab" },
			new[] { "스틱", "점프", "대시", "잡기", "일시정지", "Tab" });

		public static string Searching => T(
			"Procurando por Celeste na pasta…",
			"Looking for Celeste in the folder…",
			"Buscando Celeste en la carpeta…",
			"フォルダ内でCelesteを検索中…",
			"Recherche de Celeste dans le dossier…",
			"Ricerca di Celeste nella cartella…",
			"Suche nach Celeste im Ordner…",
			"Поиск Celeste в папке…",
			"正在文件夹中查找 Celeste…",
			"폴더에서 Celeste를 찾는 중…");

		public static string NotFound => T(
			"Não encontrei o Celeste.exe e a pasta Content aí.",
			"Couldn't find Celeste.exe and the Content folder there.",
			"No encontré Celeste.exe y la carpeta Content ahí.",
			"Celeste.exeとContentフォルダが見つかりませんでした。",
			"Impossible de trouver Celeste.exe et le dossier Content ici.",
			"Non ho trovato Celeste.exe e la cartella Content qui.",
			"Celeste.exe und der Ordner Content wurden dort nicht gefunden.",
			"Не удалось найти там Celeste.exe и папку Content.",
			"在那里没有找到 Celeste.exe 和 Content 文件夹。",
			"Celeste.exe와 Content 폴더를 찾을 수 없습니다.");

		public static string ReadingZip => T(
			"Lendo o .zip…",
			"Reading the .zip…",
			"Leyendo el .zip…",
			".zipを読み込み中…",
			"Lecture du .zip…",
			"Lettura del .zip…",
			".zip wird gelesen…",
			"Чтение .zip…",
			"正在读取 .zip…",
			".zip 읽는 중…");

		public static string CantOpenFile => T(
			"Não consegui abrir o arquivo.",
			"Couldn't open the file.",
			"No pude abrir el archivo.",
			"ファイルを開けませんでした。",
			"Impossible d'ouvrir le fichier.",
			"Impossibile aprire il file.",
			"Die Datei konnte nicht geöffnet werden.",
			"Не удалось открыть файл.",
			"无法打开该文件。",
			"파일을 열 수 없습니다.");

		public static string ZipNoExe => T(
			"Este .zip não contém o Celeste.exe.",
			"This .zip doesn't contain Celeste.exe.",
			"Este .zip no contiene Celeste.exe.",
			"この.zipにはCeleste.exeが含まれていません。",
			"Ce .zip ne contient pas Celeste.exe.",
			"Questo .zip non contiene Celeste.exe.",
			"Diese .zip enthält keine Celeste.exe.",
			"В этом .zip нет Celeste.exe.",
			"此 .zip 中不包含 Celeste.exe。",
			"이 .zip에는 Celeste.exe가 없습니다.");

		public static string PreparingEmbedded => T(
			"Preparando o jogo incluído…",
			"Preparing the bundled game…",
			"Preparando el juego incluido…",
			"同梱のゲームを準備中…",
			"Préparation du jeu inclus…",
			"Preparazione del gioco incluso…",
			"Mitgeliefertes Spiel wird vorbereitet…",
			"Подготовка встроенной игры…",
			"正在准备内置游戏…",
			"포함된 게임을 준비하는 중…");

		public static string SearchingSaves => T(
			"Procurando arquivos de save…",
			"Looking for save files…",
			"Buscando archivos de partida…",
			"セーブファイルを検索中…",
			"Recherche des fichiers de sauvegarde…",
			"Ricerca dei file di salvataggio…",
			"Suche nach Speicherdateien…",
			"Поиск файлов сохранений…",
			"正在查找存档文件…",
			"세이브 파일을 찾는 중…");

		public static string NoSaves => T(
			"Nenhum arquivo de save .celeste nessa pasta.",
			"No .celeste save files in that folder.",
			"Ningún archivo de partida .celeste en esa carpeta.",
			"そのフォルダに.celesteセーブファイルはありません。",
			"Aucun fichier de sauvegarde .celeste dans ce dossier.",
			"Nessun file di salvataggio .celeste in quella cartella.",
			"Keine .celeste-Speicherdateien in diesem Ordner.",
			"В этой папке нет файлов сохранений .celeste.",
			"该文件夹中没有 .celeste 存档文件。",
			"해당 폴더에 .celeste 세이브 파일이 없습니다.");

		public static string Copying(int i, int n) => T(
			$"Copiando arquivos do jogo… {i}/{n}",
			$"Copying game files… {i}/{n}",
			$"Copiando archivos del juego… {i}/{n}",
			$"ゲームファイルをコピー中… {i}/{n}",
			$"Copie des fichiers du jeu… {i}/{n}",
			$"Copia dei file del gioco… {i}/{n}",
			$"Spieldateien werden kopiert… {i}/{n}",
			$"Копирование файлов игры… {i}/{n}",
			$"正在复制游戏文件… {i}/{n}",
			$"게임 파일 복사 중… {i}/{n}");

		public static string XnaVersion => T(
			"Esta é a versão XNA do Celeste. Na Steam, ative o beta \"opengl\" (Propriedades → Betas) e copie a pasta de novo, ou use o .zip do Linux do itch.io.",
			"This is the XNA version of Celeste. On Steam, enable the \"opengl\" beta (Properties → Betas) and copy the folder again, or use the Linux .zip from itch.io.",
			"Esta es la versión XNA de Celeste. En Steam, activa la beta \"opengl\" (Propiedades → Betas) y copia la carpeta de nuevo, o usa el .zip de Linux de itch.io.",
			"これはCelesteのXNA版です。Steamで「opengl」ベータを有効にして（プロパティ → ベータ）フォルダを再度コピーするか、itch.ioのLinux版.zipを使用してください。",
			"Ceci est la version XNA de Celeste. Sur Steam, activez la bêta « opengl » (Propriétés → Bêtas) et copiez à nouveau le dossier, ou utilisez le .zip Linux d'itch.io.",
			"Questa è la versione XNA di Celeste. Su Steam, attiva la beta «opengl» (Proprietà → Beta) e copia di nuovo la cartella, oppure usa il .zip per Linux di itch.io.",
			"Dies ist die XNA-Version von Celeste. Aktiviere auf Steam die Beta „opengl“ (Eigenschaften → Betas) und kopiere den Ordner erneut, oder verwende die Linux-.zip von itch.io.",
			"Это XNA-версия Celeste. В Steam включите бету «opengl» (Свойства → Беты) и скопируйте папку заново, либо используйте Linux-версию .zip с itch.io.",
			"这是 Celeste 的 XNA 版本。请在 Steam 中启用“opengl”测试版（属性 → 测试版）后重新复制文件夹，或使用 itch.io 上的 Linux 版 .zip。",
			"이것은 Celeste의 XNA 버전입니다. Steam에서 \"opengl\" 베타(속성 → 베타)를 활성화한 후 폴더를 다시 복사하거나, itch.io의 Linux용 .zip을 사용하세요.");

		public static string CopyIncomplete => T(
			"A cópia está incompleta.",
			"The copy is incomplete.",
			"La copia está incompleta.",
			"コピーが不完全です。",
			"La copie est incomplète.",
			"La copia è incompleta.",
			"Die Kopie ist unvollständig.",
			"Копия неполная.",
			"复制不完整。",
			"복사가 불완전합니다.");

		public static string Patching => T(
			"Adaptando o jogo para Android…",
			"Adapting the game for Android…",
			"Adaptando el juego para Android…",
			"ゲームをAndroid向けに調整中…",
			"Adaptation du jeu pour Android…",
			"Adattamento del gioco per Android…",
			"Spiel wird für Android angepasst…",
			"Адаптация игры для Android…",
			"正在为 Android 适配游戏…",
			"Android용으로 게임을 조정하는 중…");

		public static string MakingBackground => T(
			"Gerando o fundo…",
			"Generating the background…",
			"Generando el fondo…",
			"背景を生成中…",
			"Génération de l'arrière-plan…",
			"Generazione dello sfondo…",
			"Hintergrund wird erstellt…",
			"Создание фона…",
			"正在生成背景…",
			"배경을 생성하는 중…");
	}
}
