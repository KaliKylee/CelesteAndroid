using System;
using System.Collections.Generic;
using Android.Content;

namespace CelesteAndroid
{
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

		public static string KeyboardKey => T(
			"Tecla do teclado",
			"Keyboard key",
			"Tecla del teclado",
			"キーボードのキー",
			"Touche du clavier",
			"Tasto della tastiera",
			"Tastaturtaste",
			"Клавиша клавиатуры",
			"键盘按键",
			"키보드 키");

		public static string PressAKey => T(
			"Pressione uma tecla…",
			"Press a key…",
			"Presiona una tecla…",
			"キーを押してください…",
			"Appuyez sur une touche…",
			"Premi un tasto…",
			"Taste drücken …",
			"Нажмите клавишу…",
			"请按下一个键…",
			"키를 누르세요…");

		public static string NoKey => T(
			"Nenhuma",
			"None",
			"Ninguna",
			"なし",
			"Aucune",
			"Nessuno",
			"Keine",
			"Нет",
			"无",
			"없음");

		public static string ButtonColor => T(
			"Cor",
			"Color",
			"Color",
			"色",
			"Couleur",
			"Colore",
			"Farbe",
			"Цвет",
			"颜色",
			"색상");

		public static string UseGlobalOpacity => T(
			"Usar opacidade geral",
			"Use global opacity",
			"Usar opacidad general",
			"共通の不透明度を使う",
			"Utiliser l'opacité globale",
			"Usa opacità generale",
			"Globale Deckkraft nutzen",
			"Общая прозрачность",
			"使用全局不透明度",
			"전체 투명도 사용");

		public static string DefaultColor => T(
			"Padrão",
			"Default",
			"Predeterminado",
			"標準",
			"Par défaut",
			"Predefinito",
			"Standard",
			"По умолчанию",
			"默认",
			"기본");

		public static string ExportControls => T(
			"Exportar controles",
			"Export controls",
			"Exportar controles",
			"コントロールを書き出す",
			"Exporter les commandes",
			"Esporta i controlli",
			"Steuerung exportieren",
			"Экспорт управления",
			"导出控制",
			"컨트롤 내보내기");

		public static string ImportControls => T(
			"Importar controles",
			"Import controls",
			"Importar controles",
			"コントロールを読み込む",
			"Importer les commandes",
			"Importa i controlli",
			"Steuerung importieren",
			"Импорт управления",
			"导入控制",
			"컨트롤 가져오기");

		public static string ControlsExported => T(
			"Controles exportados!",
			"Controls exported!",
			"¡Controles exportados!",
			"コントロールを書き出しました！",
			"Commandes exportées !",
			"Controlli esportati!",
			"Steuerung exportiert!",
			"Управление экспортировано!",
			"控制已导出！",
			"컨트롤을 내보냈습니다!");

		public static string ControlsImported => T(
			"Controles importados!",
			"Controls imported!",
			"¡Controles importados!",
			"コントロールを読み込みました！",
			"Commandes importées !",
			"Controlli importati!",
			"Steuerung importiert!",
			"Управление импортировано!",
			"控制已导入！",
			"컨트롤을 가져왔습니다!");

		public static string ImportInvalid => T(
			"Arquivo inválido ou erro ao ler/gravar.",
			"Invalid file or read/write error.",
			"Archivo inválido o error de lectura/escritura.",
			"無効なファイル、または読み書きエラーです。",
			"Fichier invalide ou erreur de lecture/écriture.",
			"File non valido o errore di lettura/scrittura.",
			"Ungültige Datei oder Lese-/Schreibfehler.",
			"Неверный файл или ошибка чтения/записи.",
			"文件无效或读写出错。",
			"잘못된 파일이거나 읽기/쓰기 오류입니다.");

		public static string AddButton => T(
			"Adicionar botão",
			"Add button",
			"Añadir botón",
			"ボタンを追加",
			"Ajouter un bouton",
			"Aggiungi pulsante",
			"Taste hinzufügen",
			"Добавить кнопку",
			"添加按钮",
			"버튼 추가");

		public static string RemoveButton => T(
			"Remover",
			"Remove",
			"Quitar",
			"削除",
			"Supprimer",
			"Rimuovi",
			"Entfernen",
			"Удалить",
			"删除",
			"삭제");

		public static string Size => T(
			"Tamanho",
			"Size",
			"Tamaño",
			"サイズ",
			"Taille",
			"Dimensione",
			"Größe",
			"Размер",
			"大小",
			"크기");

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

		public static string DirAnalog => T(
			"Direcional: Analógico", "D-pad: Analog stick", "Direccional: Analógico", "方向: アナログ", "Direction : Joystick", "Direzionale: Analogico", "Steuerung: Analogstick", "Управление: Стик", "方向：摇杆", "방향: 아날로그");

		public static string DirDpad => T(
			"Direcional: Setas", "D-pad: Arrows", "Direccional: Flechas", "方向: 十字キー", "Direction : Flèches", "Direzionale: Frecce", "Steuerung: Pfeile", "Управление: Стрелки", "方向：方向键", "방향: 방향키");

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

		public static string TabSize => T(
			"Tamanho", "Size", "Tamaño", "サイズ", "Taille", "Dimensione", "Größe", "Размер", "大小", "크기");

		public static string TabShape => T(
			"Forma", "Shape", "Forma", "形", "Forme", "Forma", "Form", "Форма", "形状", "모양");

		public static string TabIcon => T(
			"Ícone", "Icon", "Icono", "アイコン", "Icône", "Icona", "Symbol", "Значок", "图标", "아이콘");

		public static string TabButton => T(
			"Botão", "Button", "Botón", "ボタン", "Bouton", "Pulsante", "Taste", "Кнопка", "按钮", "버튼");

		public static string ButtonAction => T(
			"Ação", "Action", "Acción", "動作", "Action", "Azione", "Aktion", "Действие", "操作", "동작");

		public static string GamepadButton => T(
			"Botão do gamepad", "Gamepad button", "Botón del mando", "ゲームパッドのボタン", "Bouton de manette", "Pulsante del gamepad", "Gamepad-Taste", "Кнопка геймпада", "手柄按键", "게임패드 버튼");

		public static string ButtonText => T(
			"Texto", "Text", "Texto", "テキスト", "Texte", "Testo", "Text", "Текст", "文字", "텍스트");

		public static string TextAuto => T(
			"Auto", "Auto", "Auto", "自動", "Auto", "Auto", "Auto", "Авто", "自动", "자동");

		public static string SelectCustomNote => T(
			"Toque em Adicionar, ou selecione um botão na tela, para editar ação, texto, cor e opacidade.",
			"Tap Add, or select a button on screen, to edit its action, text, color and opacity.",
			"Pulsa Añadir, o selecciona un botón en pantalla, para editar acción, texto, color y opacidad.",
			"「追加」をタップするか、画面上のボタンを選ぶと、動作・テキスト・色・不透明度を編集できます。",
			"Touchez Ajouter, ou sélectionnez un bouton à l'écran, pour modifier action, texte, couleur et opacité.",
			"Tocca Aggiungi, o seleziona un pulsante sullo schermo, per modificare azione, testo, colore e opacità.",
			"Tippe auf Hinzufügen oder wähle eine Taste auf dem Bildschirm, um Aktion, Text, Farbe und Deckkraft zu ändern.",
			"Нажмите «Добавить» или выберите кнопку на экране, чтобы изменить действие, текст, цвет и непрозрачность.",
			"点按“添加按钮”，或选择屏幕上的按钮，即可编辑操作、文字、颜色和不透明度。",
			"추가를 누르거나 화면의 버튼을 선택해 동작, 텍스트, 색상, 불투명도를 편집하세요.");

		public static string TabMore => T(
			"Mais", "More", "Más", "その他", "Plus", "Altro", "Mehr", "Ещё", "更多", "더보기");

		public static string ShapeCircle => T(
			"Círculo", "Circle", "Círculo", "円", "Cercle", "Cerchio", "Kreis", "Круг", "圆形", "원");

		public static string ShapeSquare => T(
			"Quadrado", "Square", "Cuadrado", "正方形", "Carré", "Quadrato", "Quadrat", "Квадрат", "正方形", "정사각형");

		public static string ShapeRect => T(
			"Retângulo", "Rectangle", "Rectángulo", "長方形", "Rectangle", "Rettangolo", "Rechteck", "Прямоугольник", "矩形", "직사각형");

		public static string SizeLabel => T(
			"Tamanho", "Size", "Tamaño", "サイズ", "Taille", "Dimensione", "Größe", "Размер", "大小", "크기");

		public static string WidthLabel => T(
			"Largura", "Width", "Ancho", "幅", "Largeur", "Larghezza", "Breite", "Ширина", "宽度", "너비");

		public static string HeightLabel => T(
			"Altura", "Height", "Alto", "高さ", "Hauteur", "Altezza", "Höhe", "Высота", "高度", "높이");

		public static string PickFromGallery => T(
			"Escolher da galeria", "Choose from gallery", "Elegir de la galería", "ギャラリーから選ぶ", "Choisir dans la galerie", "Scegli dalla galleria", "Aus Galerie wählen", "Выбрать из галереи", "从相册选择", "갤러리에서 선택");

		public static string DefaultIcon => T(
			"Padrão", "Default", "Predeterminado", "標準", "Par défaut", "Predefinito", "Standard", "По умолчанию", "默认", "기본");

		public static string ShapeUnavailable => T(
			"Este controle é sempre redondo e não aceita ícone.", "This control is always round and can't have an icon.", "Este control siempre es redondo y no admite icono.", "このコントロールは常に円形で、アイコンを設定できません。", "Cette commande est toujours ronde et n'a pas d'icône.", "Questo controllo è sempre rotondo e non supporta icone.", "Dieses Steuerelement ist immer rund und hat kein Symbol.", "Этот элемент всегда круглый, значок для него недоступен.", "此控件始终为圆形，无法设置图标。", "이 컨트롤은 항상 원형이며 아이콘을 설정할 수 없습니다.");

		public static string IconError => T(
			"Não foi possível usar essa imagem", "Couldn't use that image", "No se pudo usar esa imagen", "この画像は使用できません", "Impossible d'utiliser cette image", "Impossibile usare questa immagine", "Dieses Bild kann nicht verwendet werden", "Не удалось использовать это изображение", "无法使用该图片", "이 이미지를 사용할 수 없습니다");

		public static string SavesMenu => T(
			"Saves",
			"Saves",
			"Partidas",
			"セーブ",
			"Sauvegardes",
			"Salvataggi",
			"Spielstände",
			"Сохранения",
			"存档",
			"저장 데이터");

		public static string SavesFromFolder => T(
			"Importar de uma pasta",
			"Import from a folder",
			"Importar desde una carpeta",
			"フォルダからインポート",
			"Importer depuis un dossier",
			"Importa da una cartella",
			"Aus einem Ordner importieren",
			"Импорт из папки",
			"从文件夹导入",
			"폴더에서 가져오기");

		public static string SavesFromZip => T(
			"Importar de um arquivo .zip",
			"Import from a .zip file",
			"Importar desde un archivo .zip",
			".zipファイルからインポート",
			"Importer depuis un fichier .zip",
			"Importa da un file .zip",
			"Aus einer .zip-Datei importieren",
			"Импорт из .zip-файла",
			"从 .zip 文件导入",
			".zip 파일에서 가져오기");

		public static string SavesExport => T(
			"Exportar saves (.zip)",
			"Export saves (.zip)",
			"Exportar partidas (.zip)",
			"セーブをエクスポート (.zip)",
			"Exporter les sauvegardes (.zip)",
			"Esporta i salvataggi (.zip)",
			"Spielstände exportieren (.zip)",
			"Экспорт сохранений (.zip)",
			"导出存档 (.zip)",
			"저장 데이터 내보내기 (.zip)");

		public static string SavesRestore => T(
			"Restaurar um backup",
			"Restore a backup",
			"Restaurar una copia de seguridad",
			"バックアップから復元",
			"Restaurer un backup",
			"Ripristina un backup",
			"Backup wiederherstellen",
			"Восстановить резервную копию",
			"恢复备份",
			"백업 복원");

		public static string SavesConfirmTitle => T(
			"Importar estes saves?",
			"Import these saves?",
			"¿Importar estas partidas?",
			"このセーブをインポートしますか？",
			"Importer ces sauvegardes ?",
			"Importare questi salvataggi?",
			"Diese Spielstände importieren?",
			"Импортировать эти сохранения?",
			"导入这些存档？",
			"이 저장 데이터를 가져올까요?");

		public static string SaveNew => T(
			"novo",
			"new",
			"nuevo",
			"新規",
			"nouveau",
			"nuovo",
			"neu",
			"новое",
			"新建",
			"새 항목");

		public static string SaveReplace => T(
			"substitui o atual",
			"replaces current",
			"reemplaza el actual",
			"現在のものを置き換え",
			"remplace l'actuel",
			"sostituisce l'attuale",
			"ersetzt den aktuellen",
			"заменит текущее",
			"替换当前存档",
			"현재 항목을 덮어씀");

		public static string SavesConfirmNote => T(
			"Os saves atuais serão guardados em um backup antes de serem substituídos.",
			"Your current saves will be backed up before being replaced.",
			"Tus partidas actuales se guardarán en una copia antes de reemplazarlas.",
			"置き換える前に、現在のセーブはバックアップされます。",
			"Vos sauvegardes actuelles seront copiées dans un backup avant d'être remplacées.",
			"I salvataggi attuali verranno salvati in un backup prima di essere sostituiti.",
			"Deine aktuellen Spielstände werden vor dem Ersetzen gesichert.",
			"Текущие сохранения будут скопированы в резервную копию перед заменой.",
			"替换前会先备份当前存档。",
			"교체하기 전에 현재 저장 데이터를 백업합니다.");

		public static string SavesImportBtn => T(
			"Importar",
			"Import",
			"Importar",
			"インポート",
			"Importer",
			"Importa",
			"Importieren",
			"Импортировать",
			"导入",
			"가져오기");

		public static string SavesExported(int n) => T(
			$"✓  {n} save(s) exportado(s)",
			$"✓  {n} save file(s) exported",
			$"✓  {n} partida(s) exportada(s)",
			$"✓  セーブを{n}件エクスポートしました",
			$"✓  {n} sauvegarde(s) exportée(s)",
			$"✓  {n} salvataggio/i esportato/i",
			$"✓  {n} Spielstand/-stände exportiert",
			$"✓  Экспортировано сохранений: {n}",
			$"✓  已导出 {n} 个存档",
			$"✓  저장 데이터 {n}개를 내보냈습니다");

		public static string NoLocalSaves => T(
			"Ainda não há saves no app para exportar",
			"There are no saves in the app to export yet",
			"Aún no hay partidas en la app para exportar",
			"エクスポートできるセーブがまだありません",
			"Il n'y a pas encore de sauvegarde à exporter",
			"Non ci sono ancora salvataggi da esportare",
			"Es gibt noch keine Spielstände zum Exportieren",
			"Пока нет сохранений для экспорта",
			"应用中还没有可导出的存档",
			"내보낼 저장 데이터가 아직 없습니다");

		public static string NoBackups => T(
			"Nenhum backup encontrado",
			"No backups found",
			"No se encontró ninguna copia de seguridad",
			"バックアップが見つかりません",
			"Aucun backup trouvé",
			"Nessun backup trovato",
			"Keine Backups gefunden",
			"Резервные копии не найдены",
			"未找到备份",
			"백업을 찾을 수 없습니다");

		public static string SavesPickBackup => T(
			"Escolha um backup",
			"Choose a backup",
			"Elige una copia de seguridad",
			"バックアップを選択",
			"Choisissez un backup",
			"Scegli un backup",
			"Backup auswählen",
			"Выберите резервную копию",
			"选择备份",
			"백업 선택");

		public static string SavesRestored(int n) => T(
			$"✓  Backup restaurado ({n} arquivo(s))",
			$"✓  Backup restored ({n} file(s))",
			$"✓  Copia restaurada ({n} archivo(s))",
			$"✓  バックアップを復元しました（{n}件）",
			$"✓  Backup restauré ({n} fichier(s))",
			$"✓  Backup ripristinato ({n} file)",
			$"✓  Backup wiederhergestellt ({n} Datei(en))",
			$"✓  Резервная копия восстановлена (файлов: {n})",
			$"✓  已恢复备份（{n} 个文件）",
			$"✓  백업을 복원했습니다 ({n}개 파일)");

		public static string SavesSkipped(string names) => T(
			$"Ignorados (inválidos): {names}",
			$"Skipped (invalid): {names}",
			$"Omitidos (no válidos): {names}",
			$"スキップ（無効）: {names}",
			$"Ignorés (invalides) : {names}",
			$"Ignorati (non validi): {names}",
			$"Übersprungen (ungültig): {names}",
			$"Пропущены (повреждены): {names}",
			$"已跳过（无效）：{names}",
			$"건너뜀(잘못된 파일): {names}");

		public static string SavesCount(int n) => T(
			$"{n} save(s)",
			$"{n} save(s)",
			$"{n} partida(s)",
			$"{n}件",
			$"{n} sauvegarde(s)",
			$"{n} salvataggio/i",
			$"{n} Spielstand/-stände",
			$"{n} сохр.",
			$"{n} 个存档",
			$"{n}개");

		public static string SavesWorking => T(
			"Processando saves…",
			"Working on saves…",
			"Procesando partidas…",
			"セーブを処理中…",
			"Traitement des sauvegardes…",
			"Elaborazione dei salvataggi…",
			"Spielstände werden verarbeitet…",
			"Обработка сохранений…",
			"正在处理存档…",
			"저장 데이터 처리 중…");

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
