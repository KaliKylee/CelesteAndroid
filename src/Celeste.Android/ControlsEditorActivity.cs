using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Color = Android.Graphics.Color;

namespace CelesteAndroid
{
	/// <summary>
	/// Editor dos controles de toque: arraste cada botão (ou o analógico) para mudar de lugar e use a barra
	/// para mudar o tamanho. O resultado vai para touch_layout.txt, que o jogo (TouchControls) lê ao iniciar.
	/// Posições são guardadas como fração da tela, então valem em qualquer resolução.
	/// </summary>
	[Activity(
		Name = "org.celesteandroid.celeste.ControlsEditorActivity",
		Label = "Celeste",
		Exported = false,
		Theme = "@style/Theme.Celeste",
		ScreenOrientation = ScreenOrientation.SensorLandscape,
		ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
			| ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation)]
	public class ControlsEditorActivity : Activity
	{
		private static readonly Color Night = Color.ParseColor("#120C22");
		private static readonly Color Accent = Color.ParseColor("#F2B8D8");

		private ControlsCanvas canvas = null!;
		private TextView label = null!;
		private SeekBar seek = null!;

		// Só paisagem: qualquer pedido de outra orientação vira paisagem.
		public override ScreenOrientation RequestedOrientation
		{
			get => base.RequestedOrientation;
			set => base.RequestedOrientation = LandscapeLock.Coerce(value);
		}
		private TextView opacityLabel = null!;
		private SeekBar opacitySeek = null!;

		// Grade de alinhamento (só no editor, vale também para o contador de FPS) e posição do FPS.
		private const string PrefGrid = "editor_grid";
		private int gridMode;
		private Button gridButton = null!;
		private LinearLayout fpsRow = null!;
		private Button fpsCornerButton = null!;
		private TextView fpsXLabel = null!, fpsYLabel = null!;
		private SeekBar fpsXSeek = null!, fpsYSeek = null!;

		protected override void OnCreate(Bundle? savedInstanceState)
		{
			RequestedOrientation = LandscapeLock.Orientation;
			base.OnCreate(savedInstanceState);
			L.Init(GetSharedPreferences("launcher", FileCreationMode.Private)!);
			SetContentView(BuildLayout());
			HideSystemBars();
			UpdateBar();
		}

		protected override void OnResume()
		{
			base.OnResume();
			HideSystemBars();
		}

		private View BuildLayout()
		{
			var root = new FrameLayout(this);
			root.SetBackgroundColor(Night);

			var bg = new ImageView(this);
			bg.SetScaleType(ImageView.ScaleType.CenterCrop);
			bg.SetImageResource(Resource.Drawable.launcher_bg);
			root.AddView(bg, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

			var shade = new View(this) { Background = new ColorDrawable(Color.Argb(150, 18, 12, 34)) };
			root.AddView(shade, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

			canvas = new ControlsCanvas(this, GameInstaller.TouchLayoutFile(this));
			canvas.OpacityPercent = GameOptions.Opacity(GameOptions.Prefs(this));
			canvas.FpsEnabled = GameOptions.ShowFps(GameOptions.Prefs(this)); // sem "Mostrar FPS" ligado não há contador para posicionar
			gridMode = Math.Clamp(GameOptions.Prefs(this).GetInt(PrefGrid, 0), 0, 3);
			canvas.GridMode = gridMode;
			canvas.SelectionChanged = UpdateBar;
			canvas.FpsChanged = UpdateFpsRow;
			root.AddView(canvas, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

			// Barra no topo, no meio: os controles ficam nos cantos e na parte de baixo.
			var bar = new LinearLayout(this) { Orientation = Orientation.Vertical };
			bar.SetPadding(Dp(14), Dp(8), Dp(14), Dp(10));
			var barShape = new GradientDrawable();
			barShape.SetColor(Color.Argb(215, 18, 12, 34));
			barShape.SetCornerRadius(Dp(16));
			bar.Background = barShape;

			var hint = Text(L.EditorHint, 12, Color.Argb(200, 255, 255, 255), false);
			hint.Gravity = GravityFlags.Center;
			bar.AddView(hint, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

			var row = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			row.SetGravity(GravityFlags.CenterVertical);

			label = Text("", 13, Color.White, true);
			row.AddView(label, new LinearLayout.LayoutParams(Dp(112), ViewGroup.LayoutParams.WrapContent));

			seek = new SeekBar(this) { Max = 150 }; // 50% .. 200%
			seek.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Accent);
			seek.ThumbTintList = Android.Content.Res.ColorStateList.ValueOf(Accent);
			seek.ProgressChanged += (_, e) =>
			{
				if (!e.FromUser)
					return;
				canvas.SetSelectedScale(0.5f + e.Progress / 100f);
				UpdateLabel();
			};
			row.AddView(seek, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f));

			var reset = MakeButton(L.Reset, filled: false);
			reset.Click += (_, _) =>
			{
				canvas.ResetToDefaults();
				canvas.OpacityPercent = GameOptions.DefaultOpacity;
				UpdateBar();
			};
			var cancel = MakeButton(L.Cancel, filled: false);
			cancel.Click += (_, _) => Finish();
			var save = MakeButton(L.Save, filled: true);
			save.Click += (_, _) =>
			{
				canvas.Save();
				GameOptions.SetOpacity(GameOptions.Prefs(this), canvas.OpacityPercent);
				Toast.MakeText(this, L.ControlsSaved, ToastLength.Short)?.Show();
				Finish();
			};
			row.AddView(reset, new LinearLayout.LayoutParams(Dp(74), Dp(36)) { LeftMargin = Dp(8) });
			row.AddView(cancel, new LinearLayout.LayoutParams(Dp(78), Dp(36)) { LeftMargin = Dp(6) });
			row.AddView(save, new LinearLayout.LayoutParams(Dp(70), Dp(36)) { LeftMargin = Dp(6) });
			bar.AddView(row, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) { TopMargin = Dp(4) });

			// Opacidade dos botões: 0% (invisíveis) .. 100% (sólidos), vale para todos os controles.
			var opacityRow = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			opacityRow.SetGravity(GravityFlags.CenterVertical);
			opacityLabel = Text("", 13, Color.White, true);
			opacityRow.AddView(opacityLabel, new LinearLayout.LayoutParams(Dp(112), ViewGroup.LayoutParams.WrapContent));
			opacitySeek = new SeekBar(this) { Max = 100 };
			opacitySeek.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Accent);
			opacitySeek.ThumbTintList = Android.Content.Res.ColorStateList.ValueOf(Accent);
			opacitySeek.ProgressChanged += (_, e) =>
			{
				if (!e.FromUser)
					return;
				canvas.OpacityPercent = e.Progress;
				UpdateLabel();
			};
			opacityRow.AddView(opacitySeek, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f));
			// Mesmo espaço dos botões da linha de cima (8 + 74 + 6 + 78 + 6 + 70), para as duas barras ficarem alinhadas:
			// ali fica o botão da grade, que alterna desligada / grande / média / fina.
			gridButton = MakeButton(L.GridLabel(gridMode), filled: false);
			gridButton.Click += (_, _) =>
			{
				gridMode = (gridMode + 1) % 4;
				canvas.GridMode = gridMode;
				gridButton.Text = L.GridLabel(gridMode);
				GameOptions.Prefs(this).Edit()!.PutInt(PrefGrid, gridMode)!.Apply();
			};
			opacityRow.AddView(gridButton, new LinearLayout.LayoutParams(Dp(74 + 6 + 78 + 6 + 70), Dp(36)) { LeftMargin = Dp(8) });
			bar.AddView(opacityRow, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

			// Posição do contador de FPS: canto + margem X e Y (distância até a borda). Também dá para arrastá-lo na tela.
			fpsRow = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			fpsRow.SetGravity(GravityFlags.CenterVertical);
			fpsCornerButton = MakeButton("", filled: false);
			fpsCornerButton.Click += (_, _) =>
			{
				canvas.SelectItem(ControlsCanvas.Fps);
				canvas.FpsCorner = (canvas.FpsCorner + 1) % 4;
			};
			fpsRow.AddView(fpsCornerButton, new LinearLayout.LayoutParams(Dp(132), Dp(34)));
			fpsXLabel = Text("", 12, Color.White, true);
			fpsXLabel.Gravity = GravityFlags.Right | GravityFlags.CenterVertical;
			fpsRow.AddView(fpsXLabel, new LinearLayout.LayoutParams(Dp(58), ViewGroup.LayoutParams.WrapContent) { LeftMargin = Dp(6) });
			fpsXSeek = MakeFpsSeek(x: true);
			fpsRow.AddView(fpsXSeek, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f));
			fpsYLabel = Text("", 12, Color.White, true);
			fpsYLabel.Gravity = GravityFlags.Right | GravityFlags.CenterVertical;
			fpsRow.AddView(fpsYLabel, new LinearLayout.LayoutParams(Dp(58), ViewGroup.LayoutParams.WrapContent));
			fpsYSeek = MakeFpsSeek(x: false);
			fpsRow.AddView(fpsYSeek, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f));
			fpsRow.Visibility = canvas.FpsEnabled ? ViewStates.Visible : ViewStates.Gone;
			bar.AddView(fpsRow, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) { TopMargin = Dp(2) });

			int barWidth = Math.Min(Dp(620), Resources!.DisplayMetrics!.WidthPixels - Dp(190));
			root.AddView(bar, new FrameLayout.LayoutParams(barWidth, ViewGroup.LayoutParams.WrapContent, GravityFlags.Top | GravityFlags.CenterHorizontal) { TopMargin = Dp(8) });

			return root;
		}

		private void UpdateBar()
		{
			seek.Progress = (int)Math.Round((canvas.SelectedScale - 0.5f) * 100f);
			opacitySeek.Progress = canvas.OpacityPercent;
			UpdateLabel();
			UpdateFpsRow();
		}

		/// <summary>Margem do FPS: 0..50% da tela em passos de 0,1%.</summary>
		private SeekBar MakeFpsSeek(bool x)
		{
			var s = new SeekBar(this) { Max = 500 };
			s.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Accent);
			s.ThumbTintList = Android.Content.Res.ColorStateList.ValueOf(Accent);
			s.ProgressChanged += (_, e) =>
			{
				if (!e.FromUser)
					return;
				canvas.SelectItem(ControlsCanvas.Fps);
				if (x)
					canvas.FpsMx = e.Progress / 1000f;
				else
					canvas.FpsMy = e.Progress / 1000f;
			};
			return s;
		}

		/// <summary>Sincroniza canto e margens do FPS com o canvas (também chamado quando o FPS é arrastado).</summary>
		private void UpdateFpsRow()
		{
			if (fpsRow == null)
				return;
			fpsCornerButton.Text = L.FpsCorners[canvas.FpsCorner];
			int px = (int)Math.Round(canvas.FpsMx * 1000f), py = (int)Math.Round(canvas.FpsMy * 1000f);
			if (fpsXSeek.Progress != px)
				fpsXSeek.Progress = px;
			if (fpsYSeek.Progress != py)
				fpsYSeek.Progress = py;
			fpsXLabel.Text = string.Format(CultureInfo.InvariantCulture, "X {0:0.0}%", px / 10f);
			fpsYLabel.Text = string.Format(CultureInfo.InvariantCulture, "Y {0:0.0}%", py / 10f);
		}

		private void UpdateLabel()
		{
			string name = canvas.Selected == ControlsCanvas.Fps ? "FPS" : ControlsCanvas.Names[canvas.Selected];
			label.Text = $"{name}: {(int)Math.Round(canvas.SelectedScale * 100f)}%";
			opacityLabel.Text = $"{L.Opacity}: {canvas.OpacityPercent}%";
		}

		private TextView Text(string text, float sp, Color color, bool bold)
		{
			var view = new TextView(this) { Text = text };
			view.SetTextSize(ComplexUnitType.Sp, sp);
			view.SetTextColor(color);
			view.SetTypeface(Typeface.Create("sans-serif", bold ? TypefaceStyle.Bold : TypefaceStyle.Normal), bold ? TypefaceStyle.Bold : TypefaceStyle.Normal);
			return view;
		}

		private Button MakeButton(string text, bool filled)
		{
			var button = new Button(this) { Text = text, StateListAnimator = null };
			button.SetAllCaps(false);
			button.SetTextSize(ComplexUnitType.Sp, 12);
			button.SetPadding(Dp(8), 0, Dp(8), 0);
			button.SetMinHeight(0);
			button.SetMinimumHeight(0);
			button.SetMinWidth(0);
			button.SetMinimumWidth(0);
			var shape = new GradientDrawable();
			shape.SetCornerRadius(Dp(18));
			if (filled)
			{
				shape.SetColor(Color.White);
				button.SetTextColor(Night);
			}
			else
			{
				shape.SetColor(Color.Argb(40, 255, 255, 255));
				shape.SetStroke(Dp(1), Color.Argb(200, 255, 255, 255));
				button.SetTextColor(Color.White);
			}
			button.Background = new RippleDrawable(Android.Content.Res.ColorStateList.ValueOf(Color.Argb(60, 242, 184, 216)), shape, null);
			return button;
		}

		private int Dp(float dp) => (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, Resources!.DisplayMetrics);

		private void HideSystemBars()
		{
			if (!OperatingSystem.IsAndroidVersionAtLeast(30))
				return;
			// A partir do Android 15 o app já é edge-to-edge por padrão.
			if (!OperatingSystem.IsAndroidVersionAtLeast(35))
				Window!.SetDecorFitsSystemWindows(false);
			IWindowInsetsController? insets = Window!.InsetsController;
			if (insets != null)
			{
				insets.Hide(WindowInsets.Type.SystemBars());
				insets.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
			}
		}
	}

	/// <summary>Desenha os controles na mesma geometria do TouchControls e deixa arrastá-los.</summary>
	internal sealed class ControlsCanvas : View
	{
		// Count = botões/analógico com posição própria; Fps (= Count) é o contador de FPS, que tem canto + margens em vez de x/y.
		public const int Stick = 0, Jump = 1, Dash = 2, Grab = 3, Pause = 4, Tab = 5, Count = 6, Fps = 6;

		public static string[] Names => L.ControlNames;
		private static readonly string[] Keys = { "stick", "jump", "dash", "grab", "pause", "tab" };
		// Mesma arte pixel art do jogo (PixelButtonArt, compartilhado com o TouchControls).
		private static readonly PixelButtonArt.Sprite?[] Sprites =
		{
			null, PixelButtonArt.Jump, PixelButtonArt.Dash, PixelButtonArt.Grab, PixelButtonArt.Pause, PixelButtonArt.Tab,
		};
		private readonly Bitmap?[] bitmaps = new Bitmap?[Count];

		// Mesmos valores do TouchControls.
		private const float ButtonSize = 0.17f;
		private const float StickRadius = 0.13f;
		private const float StickX = 0.30f;
		private const float StickBottom = 0.28f;

		private readonly string path;
		private readonly float[] fx = new float[Count];
		private readonly float[] fy = new float[Count];
		private readonly float[] scale = new float[Count + 1]; // o último é a escala do texto do FPS
		private readonly Paint paint = new(PaintFlags.AntiAlias);
		private bool ready;
		private bool isDefault = true;
		private bool dragging;
		private float dragDx, dragDy;

		// ---- Contador de FPS: canto (bit 0 = direita, bit 1 = baixo) e margens em fração da largura/altura ----
		private int fpsCorner;
		private float fpsMx, fpsMy;

		/// <summary>Só mostra/permite arrastar o FPS se a opção "Mostrar FPS" estiver ligada.</summary>
		public bool FpsEnabled { get; set; }
		public Action? FpsChanged { get; set; }

		public int FpsCorner
		{
			get => fpsCorner;
			set { fpsCorner = Math.Clamp(value, 0, 3); isDefault = false; ClampFps(); Invalidate(); }
		}

		public float FpsMx
		{
			get => fpsMx;
			set { fpsMx = value; isDefault = false; ClampFps(); Invalidate(); }
		}

		public float FpsMy
		{
			get => fpsMy;
			set { fpsMy = value; isDefault = false; ClampFps(); Invalidate(); }
		}

		private int gridMode;

		/// <summary>Grade de alinhamento: 0 = desligada; 1, 2, 3 = 12, 24, 48 células na altura. Os itens "grudam" nela ao serem arrastados.</summary>
		public int GridMode
		{
			get => gridMode;
			set { gridMode = Math.Clamp(value, 0, 3); Invalidate(); }
		}

		private float GridStep => gridMode switch { 1 => Height / 12f, 2 => Height / 24f, 3 => Height / 48f, _ => 0f };
		private float Snap(float v) => GridStep > 0f ? MathF.Round(v / GridStep) * GridStep : v;

		public void SelectItem(int i)
		{
			if (Selected == i)
				return;
			Selected = i;
			SelectionChanged?.Invoke();
			Invalidate();
		}

		public int Selected { get; private set; } = Stick;
		public Action? SelectionChanged { get; set; }
		public float SelectedScale => scale[Selected];

		private int opacityPercent = GameOptions.DefaultOpacity;

		/// <summary>Opacidade dos controles (0..100); a prévia usa a mesma fórmula do TouchControls.</summary>
		public int OpacityPercent
		{
			get => opacityPercent;
			set
			{
				opacityPercent = Math.Clamp(value, 0, 100);
				Invalidate();
			}
		}

		// Com 0% a prévia sumiria e não haveria o que arrastar: mantém um mínimo só no editor
		// (o item selecionado também tem um aro de destaque).
		private float PreviewAlpha => Math.Max(opacityPercent / 100f, 0.12f);

		public ControlsCanvas(Context context, string path) : base(context)
		{
			this.path = path;
			for (int i = 0; i <= Count; i++)
				scale[i] = 1f;
		}

		protected ControlsCanvas(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
		{
			path = "";
		}

		private float Dp(float dp) => TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, Resources!.DisplayMetrics);

		// ---- Geometria ----
		private float Unit => Height * ButtonSize;
		private float BtnRadius(int i) => (i == Pause ? 0.32f : i == Tab ? 0.4f : 0.5f) * Unit * scale[i];
		private float StickRange => Height * StickRadius * scale[Stick];
		private float VisibleRadius(int i) => i == Stick ? StickRange * 1.1f : BtnRadius(i);
		// Mesma geometria do TouchControls.DrawFps: célula = 0,45% da altura * escala; "60 FPS" = 6 caracteres de 6 células (sem o espaço final) x 7 de altura.
		private float FpsCell => Math.Max(2f, Height * 0.0045f) * scale[Fps];
		private float FpsW => (6 * 6f - 1f) * FpsCell;
		private float FpsH => 7f * FpsCell;

		private RectF FpsRect()
		{
			float w = FpsW, h = FpsH;
			float x = (fpsCorner & 1) == 1 ? Width - fpsMx * Width - w : fpsMx * Width;
			float y = (fpsCorner & 2) == 2 ? Height - fpsMy * Height - h : fpsMy * Height;
			return new RectF(x, y, x + w, y + h);
		}

		/// <summary>Mantém o texto do FPS inteiro dentro da tela.</summary>
		private void ClampFps()
		{
			if (Width <= 0 || Height <= 0)
				return;
			fpsMx = Math.Clamp(fpsMx, 0f, Math.Max(0f, Math.Min(0.5f, (Width - FpsW) / Width)));
			fpsMy = Math.Clamp(fpsMy, 0f, Math.Max(0f, Math.Min(0.5f, (Height - FpsH) / Height)));
			FpsChanged?.Invoke();
		}

		/// <summary>Arrastando: o canto é o mais próximo do centro do texto; as margens são a distância até as bordas desse canto.</summary>
		private void MoveFps(float cx, float cy)
		{
			float w = FpsW, h = FpsH;
			fpsCorner = (cx > Width / 2f ? 1 : 0) | (cy > Height / 2f ? 2 : 0);
			float mxPx = (fpsCorner & 1) == 1 ? Width - (cx + w / 2f) : cx - w / 2f;
			float myPx = (fpsCorner & 2) == 2 ? Height - (cy + h / 2f) : cy - h / 2f;
			fpsMx = Snap(mxPx) / Width;
			fpsMy = Snap(myPx) / Height;
			isDefault = false;
			ClampFps();
		}

		private float HitRadius(int i) => i == Stick ? StickRange * 1.1f : BtnRadius(i) * 1.25f;

		protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
		{
			base.OnSizeChanged(w, h, oldw, oldh);
			if (ready || w <= 0 || h <= 0)
				return;
			ready = true;
			SetDefaults();
			LoadFile();
			SelectionChanged?.Invoke(); // atualiza a barra com o tamanho salvo
		}

		/// <summary>Layout padrão. Manter em sincronia com TouchControls.Center/StickBase.</summary>
		private void SetDefaults()
		{
			float w = Width, h = Height, u = h * ButtonSize;
			Place(Stick, h * StickX, h * (1f - StickBottom));
			Place(Jump, w - 1.15f * u, h - 1.35f * u);
			Place(Dash, w - 2.45f * u, h - 0.95f * u);
			Place(Grab, w - 2.15f * u, h - 2.35f * u);
			Place(Pause, w - 0.8f * u, 0.8f * u);
			Place(Tab, w - 1.75f * u, 0.8f * u);
			for (int i = 0; i <= Count; i++)
				scale[i] = 1f;
			// FPS: canto superior esquerdo, margem de 3 células (igual ao padrão do TouchControls).
			fpsCorner = 0;
			fpsMx = Math.Max(2f, h * 0.0045f) * 3f / w;
			fpsMy = Math.Max(2f, h * 0.0045f) * 3f / h;
			isDefault = true;
		}

		private void Place(int i, float px, float py)
		{
			fx[i] = px / Width;
			fy[i] = py / Height;
		}

		public void ResetToDefaults()
		{
			SetDefaults();
			Invalidate();
		}

		private void LoadFile()
		{
			try
			{
				if (!File.Exists(path))
					return;
				foreach (string line in File.ReadAllLines(path))
				{
					string[] kv = line.Split('=');
					if (kv.Length != 2)
						continue;
					if (kv[0].Trim() == "fps")
					{
						// fps=canto,mx,my,escala
						string[] fv = kv[1].Split(',');
						if (fv.Length == 4
							&& int.TryParse(fv[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int corner)
							&& float.TryParse(fv[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float mx)
							&& float.TryParse(fv[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float my)
							&& float.TryParse(fv[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float fs))
						{
							fpsCorner = Math.Clamp(corner, 0, 3);
							fpsMx = Math.Clamp(mx, 0f, 0.5f);
							fpsMy = Math.Clamp(my, 0f, 0.5f);
							scale[Fps] = Math.Clamp(fs, 0.5f, 2f);
							isDefault = false;
						}
						continue;
					}
					int idx = Array.IndexOf(Keys, kv[0].Trim());
					string[] v = kv[1].Split(',');
					if (idx < 0 || v.Length != 3)
						continue;
					if (!float.TryParse(v[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
						|| !float.TryParse(v[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)
						|| !float.TryParse(v[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float s))
						continue;
					fx[idx] = Math.Clamp(x, 0f, 1f);
					fy[idx] = Math.Clamp(y, 0f, 1f);
					scale[idx] = Math.Clamp(s, 0.5f, 2f);
					isDefault = false;
				}
			}
			catch (Exception)
			{
				// Arquivo ilegível: fica o layout padrão.
			}
		}

		/// <summary>Salva o layout; se estiver no padrão, apaga o arquivo (o jogo volta ao padrão dele).</summary>
		public void Save()
		{
			try
			{
				if (isDefault)
				{
					if (File.Exists(path))
						File.Delete(path);
					return;
				}
				var sb = new StringBuilder();
				for (int i = 0; i < Count; i++)
					sb.Append(string.Format(CultureInfo.InvariantCulture, "{0}={1:0.0000},{2:0.0000},{3:0.00}\n", Keys[i], fx[i], fy[i], scale[i]));
				sb.Append(string.Format(CultureInfo.InvariantCulture, "fps={0},{1:0.0000},{2:0.0000},{3:0.00}\n", fpsCorner, fpsMx, fpsMy, scale[Fps]));
				File.WriteAllText(path, sb.ToString());
			}
			catch (Exception)
			{
			}
		}

		public void SetSelectedScale(float s)
		{
			scale[Selected] = Math.Clamp(s, 0.5f, 2f);
			isDefault = false;
			ClampSelected();
			Invalidate();
		}

		private void ClampSelected()
		{
			if (Selected == Fps)
			{
				ClampFps();
				return;
			}
			float m = VisibleRadius(Selected);
			float px = Math.Clamp(fx[Selected] * Width, m, Math.Max(m, Width - m));
			float py = Math.Clamp(fy[Selected] * Height, m, Math.Max(m, Height - m));
			fx[Selected] = px / Width;
			fy[Selected] = py / Height;
		}

		// ---- Desenho ----
		protected override void OnDraw(Canvas? canvas)
		{
			base.OnDraw(canvas);
			if (canvas == null || !ready)
				return;
			DrawGrid(canvas);
			if (FpsEnabled)
				DrawFpsItem(canvas);
			for (int i = 0; i < Count; i++)
				DrawItem(canvas, i);
		}

		private void DrawGrid(Canvas canvas)
		{
			float g = GridStep;
			if (g <= 0f)
				return;
			paint.SetStyle(Paint.Style.Stroke!);
			paint.StrokeWidth = Math.Max(1f, Dp(0.75f));
			// Uma linha mais forte a cada 4 células, para contar com o olho.
			for (int k = 0; k * g <= Width; k++)
			{
				paint.Color = Color.Argb(k % 4 == 0 ? 80 : 34, 255, 255, 255);
				canvas.DrawLine(k * g, 0, k * g, Height, paint);
			}
			for (int k = 0; k * g <= Height; k++)
			{
				paint.Color = Color.Argb(k % 4 == 0 ? 80 : 34, 255, 255, 255);
				canvas.DrawLine(0, k * g, Width, k * g, paint);
			}
		}

		// Fonte 5x7 igual à do TouchControls (só os caracteres de "60 FPS").
		private static readonly Dictionary<char, string[]> FpsGlyphs = new()
		{
			['6'] = new[] { "00110", "01000", "10000", "11110", "10001", "10001", "01110" },
			['0'] = new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" },
			['F'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" },
			['P'] = new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" },
			['S'] = new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" },
		};

		private void DrawFpsGlyphs(Canvas canvas, float left, float top, float c, Color color)
		{
			paint.SetStyle(Paint.Style.Fill!);
			paint.Color = color;
			float x = left;
			foreach (char ch in "60 FPS")
			{
				if (FpsGlyphs.TryGetValue(ch, out string[]? rows))
					for (int y = 0; y < rows.Length; y++)
						for (int col = 0; col < rows[y].Length; col++)
							if (rows[y][col] == '1')
								canvas.DrawRect(x + col * c, top + y * c, x + (col + 1) * c, top + (y + 1) * c, paint);
				x += c * 6f;
			}
		}

		private void DrawFpsItem(Canvas canvas)
		{
			RectF r = FpsRect();
			float c = FpsCell;
			DrawFpsGlyphs(canvas, r.Left + c * 0.5f, r.Top + c * 0.5f, c, Color.Argb(190, 0, 0, 0)); // sombra, como no jogo
			DrawFpsGlyphs(canvas, r.Left, r.Top, c, Color.White);

			// Moldura: forte quando selecionado, discreta senão (mostra onde tocar para arrastar).
			float pad = Dp(6);
			paint.SetStyle(Paint.Style.Stroke!);
			paint.StrokeWidth = Dp(Selected == Fps ? 3 : 1.5f);
			paint.Color = Selected == Fps ? Color.ParseColor("#F2B8D8") : Color.Argb(110, 255, 255, 255);
			canvas.DrawRoundRect(new RectF(r.Left - pad, r.Top - pad, r.Right + pad, r.Bottom + pad), Dp(6), Dp(6), paint);
		}

		/// <summary>Opacidade dos botões pixel art: mesma curva do TouchControls.ButtonOpacity (45% vira 90%, 100% fica sólido).</summary>
		private float ButtonAlpha
		{
			get
			{
				float o = PreviewAlpha;
				return o <= 0.45f ? o / 0.45f * 0.9f : Math.Min(1f, 0.9f + (o - 0.45f) / 0.55f * 0.1f);
			}
		}

		/// <summary>Alfa 0..255 = opacidade * fator (mesmos fatores do TouchControls.Draw).</summary>
		private int A(float factor) => Math.Clamp((int)Math.Round(PreviewAlpha * factor * 255f), 0, 255);

		private void DrawItem(Canvas canvas, int i)
		{
			float cx = fx[i] * Width, cy = fy[i] * Height;
			if (i == Stick)
			{
				float range = StickRange;
				paint.SetStyle(Paint.Style.Stroke!);
				paint.StrokeWidth = range * 0.12f;
				paint.Color = Color.Argb(A(1f), 255, 255, 255);
				canvas.DrawCircle(cx, cy, range * 1.1f, paint);
				paint.SetStyle(Paint.Style.Fill!);
				paint.Color = Color.Argb(A(1.1f), 255, 255, 255);
				canvas.DrawCircle(cx, cy, range * 0.45f, paint);
			}
			else
			{
				PixelButtonArt.Sprite sp = Sprites[i]!;
				int n = PixelButtonArt.Size;
				// Tamanho múltiplo de 28: cada "pixel" da arte ocupa um número inteiro de pixels da tela.
				int cell = Math.Max(1, (int)Math.Round(BtnRadius(i) * 2f / n));
				float half = cell * n / 2f;
				int body = sp.GlowRgb;
				int cr = (body >> 16) & 255, cg = (body >> 8) & 255, cb = body & 255;
				float alpha = ButtonAlpha; // segue o controle de opacidade do editor

				// Brilho suave ao redor, como no jogo.
				int glowA = Math.Clamp((int)Math.Round(alpha * 0.55f * 255f), 0, 255);
				int glowColor = Color.Argb(glowA, cr, cg, cb).ToArgb();
				int glowClear = Color.Argb(0, cr, cg, cb).ToArgb();
				using (var shader = new RadialGradient(cx, cy, half * 1.35f, new[] { glowColor, glowColor, glowClear }, new[] { 0f, 0.7f, 1f }, Shader.TileMode.Clamp!))
				{
					paint.SetStyle(Paint.Style.Fill!);
					paint.Alpha = 255;
					paint.SetShader(shader);
					canvas.DrawCircle(cx, cy, half * 1.35f, paint);
					paint.SetShader(null);
				}

				// Sprite sem suavização, para os pixels ficarem nítidos.
				paint.SetStyle(Paint.Style.Fill!);
				paint.FilterBitmap = false;
				paint.Alpha = Math.Clamp((int)Math.Round(alpha * 255f), 0, 255);
				canvas.DrawBitmap(GetBitmap(i), null, new RectF(cx - half, cy - half, cx + half, cy + half), paint);
				paint.Alpha = 255;
			}

			if (i == Selected)
			{
				paint.SetStyle(Paint.Style.Stroke!);
				paint.StrokeWidth = Dp(3);
				paint.Color = Color.ParseColor("#F2B8D8");
				canvas.DrawCircle(cx, cy, VisibleRadius(i) + Dp(6), paint);
			}
		}

		private Bitmap GetBitmap(int i)
		{
			if (bitmaps[i] != null)
				return bitmaps[i]!;
			PixelButtonArt.Sprite sp = Sprites[i]!;
			int n = PixelButtonArt.Size;
			int[] px = new int[n * n];
			for (int y = 0; y < n; y++)
			{
				for (int x = 0; x < n; x++)
				{
					int rgb = sp.ColorOf(sp.Rows[y][x]);
					px[y * n + x] = rgb < 0 ? 0 : Color.Argb(255, (rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255).ToArgb();
				}
			}
			Bitmap bmp = Bitmap.CreateBitmap(px, n, n, Bitmap.Config.Argb8888!)!;
			bitmaps[i] = bmp;
			return bmp;
		}

		// ---- Toque ----
		private int HitTest(float x, float y)
		{
			int best = -1;
			float bestScore = float.MaxValue;
			for (int i = 0; i < Count; i++)
			{
				float dx = x - fx[i] * Width, dy = y - fy[i] * Height;
				float reach = HitRadius(i);
				float score = MathF.Sqrt(dx * dx + dy * dy) / reach;
				if (score <= 1f && score < bestScore)
				{
					bestScore = score;
					best = i;
				}
			}
			// O FPS fica por último: onde ele encosta num botão, o botão tem prioridade.
			if (best < 0 && FpsEnabled)
			{
				RectF r = FpsRect();
				float pad = Dp(14);
				if (x >= r.Left - pad && x <= r.Right + pad && y >= r.Top - pad && y <= r.Bottom + pad)
					return Fps;
			}
			return best;
		}

		public override bool OnTouchEvent(MotionEvent? e)
		{
			if (e == null)
				return false;
			switch (e.ActionMasked)
			{
				case MotionEventActions.Down:
				{
					int hit = HitTest(e.GetX(), e.GetY());
					if (hit < 0)
						return true;
					Selected = hit;
					dragging = true;
					if (hit == Fps)
					{
						RectF r = FpsRect();
						dragDx = (r.Left + r.Right) / 2f - e.GetX();
						dragDy = (r.Top + r.Bottom) / 2f - e.GetY();
					}
					else
					{
						dragDx = fx[hit] * Width - e.GetX();
						dragDy = fy[hit] * Height - e.GetY();
					}
					SelectionChanged?.Invoke();
					Invalidate();
					return true;
				}
				case MotionEventActions.Move:
					if (dragging)
					{
						float px = e.GetX() + dragDx, py = e.GetY() + dragDy;
						if (Selected == Fps)
							MoveFps(px, py);
						else
						{
							// Com a grade ligada o centro do botão "gruda" no cruzamento mais próximo.
							fx[Selected] = Snap(px) / Width;
							fy[Selected] = Snap(py) / Height;
							isDefault = false;
							ClampSelected();
						}
						Invalidate();
					}
					return true;
				case MotionEventActions.Up:
				case MotionEventActions.Cancel:
					dragging = false;
					return true;
			}
			return true;
		}
	}
}
