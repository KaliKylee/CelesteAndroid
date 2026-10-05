using System;
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
			canvas.SelectionChanged = UpdateBar;
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
			// Mesmo espaço dos botões da linha de cima, para as duas barras ficarem alinhadas.
			opacityRow.AddView(new View(this), new LinearLayout.LayoutParams(Dp(8 + 74 + 6 + 78 + 6 + 70), 1));
			bar.AddView(opacityRow, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

			int barWidth = Math.Min(Dp(620), Resources!.DisplayMetrics!.WidthPixels - Dp(190));
			root.AddView(bar, new FrameLayout.LayoutParams(barWidth, ViewGroup.LayoutParams.WrapContent, GravityFlags.Top | GravityFlags.CenterHorizontal) { TopMargin = Dp(8) });

			return root;
		}

		private void UpdateBar()
		{
			seek.Progress = (int)Math.Round((canvas.SelectedScale - 0.5f) * 100f);
			opacitySeek.Progress = canvas.OpacityPercent;
			UpdateLabel();
		}

		private void UpdateLabel()
		{
			label.Text = $"{ControlsCanvas.Names[canvas.Selected]}: {(int)Math.Round(canvas.SelectedScale * 100f)}%";
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
		public const int Stick = 0, Jump = 1, Dash = 2, Grab = 3, Pause = 4, Count = 5;

		public static string[] Names => L.ControlNames;
		private static readonly string[] Keys = { "stick", "jump", "dash", "grab", "pause" };
		private static readonly int[][] Rgb =
		{
			new[] { 255, 255, 255 },
			new[] { 120, 220, 140 },
			new[] { 240, 120, 150 },
			new[] { 120, 170, 240 },
			new[] { 220, 220, 220 },
		};
		private static readonly string[] Glyphs = { "", "A", "X", "G", "" };

		// Mesmos valores do TouchControls.
		private const float ButtonSize = 0.17f;
		private const float StickRadius = 0.13f;
		private const float StickX = 0.30f;
		private const float StickBottom = 0.28f;

		private readonly string path;
		private readonly float[] fx = new float[Count];
		private readonly float[] fy = new float[Count];
		private readonly float[] scale = new float[Count];
		private readonly Paint paint = new(PaintFlags.AntiAlias);
		private bool ready;
		private bool isDefault = true;
		private bool dragging;
		private float dragDx, dragDy;

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
			for (int i = 0; i < Count; i++)
				scale[i] = 1f;
		}

		protected ControlsCanvas(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
		{
			path = "";
		}

		private float Dp(float dp) => TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, Resources!.DisplayMetrics);

		// ---- Geometria ----
		private float Unit => Height * ButtonSize;
		private float BtnRadius(int i) => (i == Pause ? 0.32f : 0.5f) * Unit * scale[i];
		private float StickRange => Height * StickRadius * scale[Stick];
		private float VisibleRadius(int i) => i == Stick ? StickRange * 1.1f : BtnRadius(i);
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
			for (int i = 0; i < Count; i++)
				scale[i] = 1f;
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
			for (int i = 0; i < Count; i++)
				DrawItem(canvas, i);
		}

		/// <summary>Alfa 0..255 = opacidade * fator (mesmos fatores do TouchControls.Draw).</summary>
		private int A(float factor) => Math.Clamp((int)Math.Round(PreviewAlpha * factor * 255f), 0, 255);

		private void DrawItem(Canvas canvas, int i)
		{
			float cx = fx[i] * Width, cy = fy[i] * Height;
			int[] c = Rgb[i];
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
				float r = BtnRadius(i);
				paint.SetStyle(Paint.Style.Fill!);
				paint.Color = Color.Argb(A(0.55f), c[0], c[1], c[2]);
				canvas.DrawCircle(cx, cy, r, paint);
				paint.SetStyle(Paint.Style.Stroke!);
				paint.StrokeWidth = r * 0.12f;
				paint.Color = Color.Argb(A(1f), c[0], c[1], c[2]);
				canvas.DrawCircle(cx, cy, r, paint);

				paint.SetStyle(Paint.Style.Fill!);
				paint.Color = Color.Argb(A(1.8f), 255, 255, 255);
				if (i == Pause)
				{
					float bw = r * 0.2f, bh = r * 0.8f, gap = r * 0.18f;
					canvas.DrawRect(cx - gap - bw, cy - bh / 2f, cx - gap, cy + bh / 2f, paint);
					canvas.DrawRect(cx + gap, cy - bh / 2f, cx + gap + bw, cy + bh / 2f, paint);
				}
				else
				{
					paint.TextSize = r * 0.95f;
					paint.TextAlign = Paint.Align.Center!;
					paint.SetTypeface(Typeface.DefaultBold);
					canvas.DrawText(Glyphs[i], cx, cy + paint.TextSize * 0.35f, paint);
				}
			}

			if (i == Selected)
			{
				paint.SetStyle(Paint.Style.Stroke!);
				paint.StrokeWidth = Dp(3);
				paint.Color = Color.ParseColor("#F2B8D8");
				canvas.DrawCircle(cx, cy, VisibleRadius(i) + Dp(6), paint);
			}
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
					dragDx = fx[hit] * Width - e.GetX();
					dragDy = fy[hit] * Height - e.GetY();
					SelectionChanged?.Invoke();
					Invalidate();
					return true;
				}
				case MotionEventActions.Move:
					if (dragging)
					{
						fx[Selected] = (e.GetX() + dragDx) / Width;
						fy[Selected] = (e.GetY() + dragDy) / Height;
						isDefault = false;
						ClampSelected();
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
