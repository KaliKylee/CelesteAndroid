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
using Android.Util;
using Android.Views;
using Android.Widget;
using Color = Android.Graphics.Color;

namespace CelesteAndroid
{
	public class ButtonStyle
	{
		public string Key = "-";      // nome do enum Keys do XNA/FNA, "-" = nenhuma
		public int Rgb = -1;          // -1 = cor padrão
		public int Opacity = -1;      // -1 = opacidade geral
	}

	public static class ButtonStyles
	{
		public static readonly string[] Ids = { "jump", "dash", "grab", "pause", "tab" };
		public static readonly string[] Names = { "Jump", "Dash", "Grab", "Pause", "Tab" };

		public static ButtonStyle[] Load(string path)
		{
			var styles = new ButtonStyle[Ids.Length];
			for (int i = 0; i < styles.Length; i++)
				styles[i] = new ButtonStyle();
			try
			{
				if (!File.Exists(path))
					return styles;
				foreach (string line in File.ReadAllLines(path))
				{
					string[] kv = line.Split('=');
					if (kv.Length != 2)
						continue;
					int idx = Array.IndexOf(Ids, kv[0].Trim());
					string[] v = kv[1].Split(',');
					if (idx < 0 || v.Length != 3)
						continue;
					styles[idx].Key = v[0].Trim().Length > 0 ? v[0].Trim() : "-";
					styles[idx].Rgb = v[1].Trim().Length == 6 && int.TryParse(v[1].Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb) ? rgb : -1;
					styles[idx].Opacity = int.TryParse(v[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int op) ? op : -1;
				}
			}
			catch (Exception)
			{
			}
			return styles;
		}

		public static void Save(string path, ButtonStyle[] styles)
		{
			var sb = new StringBuilder();
			for (int i = 0; i < Ids.Length; i++)
			{
				ButtonStyle s = styles[i];
				sb.Append(Ids[i]).Append('=').Append(s.Key).Append(',')
					.Append(s.Rgb >= 0 ? s.Rgb.ToString("X6", CultureInfo.InvariantCulture) : "-").Append(',')
					.Append(s.Opacity.ToString(CultureInfo.InvariantCulture)).Append('\n');
			}
			File.WriteAllText(path, sb.ToString());
		}

		// Converte um Keycode do Android no nome do enum Microsoft.Xna.Framework.Input.Keys.
		public static string? ToXnaKey(Keycode code)
		{
			if (code >= Keycode.A && code <= Keycode.Z)
				return ((char)('A' + (code - Keycode.A))).ToString();
			if (code >= Keycode.Num0 && code <= Keycode.Num9)
				return "D" + (code - Keycode.Num0);
			if (code >= Keycode.Numpad0 && code <= Keycode.Numpad9)
				return "NumPad" + (code - Keycode.Numpad0);
			if (code >= Keycode.F1 && code <= Keycode.F12)
				return "F" + (1 + (code - Keycode.F1));
			return code switch
			{
				Keycode.Space => "Space",
				Keycode.Enter or Keycode.NumpadEnter => "Enter",
				Keycode.Tab => "Tab",
				Keycode.Escape => "Escape",
				Keycode.Del => "Back",
				Keycode.ForwardDel => "Delete",
				Keycode.DpadUp => "Up",
				Keycode.DpadDown => "Down",
				Keycode.DpadLeft => "Left",
				Keycode.DpadRight => "Right",
				Keycode.ShiftLeft => "LeftShift",
				Keycode.ShiftRight => "RightShift",
				Keycode.CtrlLeft => "LeftControl",
				Keycode.CtrlRight => "RightControl",
				Keycode.AltLeft => "LeftAlt",
				Keycode.AltRight => "RightAlt",
				Keycode.Comma => "OemComma",
				Keycode.Period => "OemPeriod",
				Keycode.Slash => "OemQuestion",
				Keycode.Semicolon => "OemSemicolon",
				Keycode.Apostrophe => "OemQuotes",
				Keycode.LeftBracket => "OemOpenBrackets",
				Keycode.RightBracket => "OemCloseBrackets",
				Keycode.Backslash => "OemPipe",
				Keycode.Minus => "OemMinus",
				(Keycode)70 => "OemPlus",
				Keycode.Grave => "OemTilde",
				Keycode.PageUp => "PageUp",
				Keycode.PageDown => "PageDown",
				Keycode.MoveHome => "Home",
				Keycode.MoveEnd => "End",
				Keycode.Insert => "Insert",
				_ => null,
			};
		}

		public static string Display(string key) => key switch
		{
			"-" => L.NoKey,
			_ when key.Length == 2 && key[0] == 'D' && char.IsDigit(key[1]) => key.Substring(1),
			_ => key,
		};
	}

	[Activity(
		Name = "org.celesteandroid.celeste.ButtonStyleActivity",
		Label = "Celeste",
		Exported = false,
		Theme = "@style/Theme.Celeste",
		ScreenOrientation = ScreenOrientation.SensorLandscape,
		ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
			| ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation)]
	public class ButtonStyleActivity : Activity
	{
		private static readonly Color Night = Color.ParseColor("#120C22");
		private static readonly Color Accent = Color.ParseColor("#F2B8D8");

		private static readonly string[] Palette =
		{
			"FFFFFF", "FF4D4D", "FF9A3C", "FFD93D", "6BE675", "3CD6C8", "4DA3FF", "8A6BFF", "E070FF", "FF7EB6", "9AA0A6", "222222",
		};

		private ButtonStyle[] styles = null!;
		private string stylePath = null!;

		public override ScreenOrientation RequestedOrientation
		{
			get => base.RequestedOrientation;
			set => base.RequestedOrientation = LandscapeLock.Coerce(value);
		}

		protected override void OnCreate(Bundle? savedInstanceState)
		{
			RequestedOrientation = LandscapeLock.Orientation;
			base.OnCreate(savedInstanceState);
			L.Init(GameOptions.Prefs(this));
			stylePath = GameInstaller.ButtonStyleFile(this);
			styles = ButtonStyles.Load(stylePath);
			SetContentView(BuildLayout());
			HideSystemBars();
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
			root.AddView(bg, new FrameLayout.LayoutParams(-1, -1));
			root.AddView(new View(this) { Background = new ColorDrawable(Color.Argb(190, 18, 12, 34)) }, new FrameLayout.LayoutParams(-1, -1));

			var outer = new LinearLayout(this) { Orientation = Orientation.Vertical };
			outer.SetPadding(Dp(24), Dp(14), Dp(24), Dp(14));
			root.AddView(outer, new FrameLayout.LayoutParams(-1, -1));

			var title = Text("🎮  " + L.IndividualButtons, 18, Color.White, true);
			outer.AddView(title, new LinearLayout.LayoutParams(-1, -2) { BottomMargin = Dp(8) });

			var scroll = new ScrollView(this);
			var list = new LinearLayout(this) { Orientation = Orientation.Vertical };
			scroll.AddView(list, new ViewGroup.LayoutParams(-1, -2));
			outer.AddView(scroll, new LinearLayout.LayoutParams(-1, 0, 1f));

			for (int i = 0; i < ButtonStyles.Ids.Length; i++)
				list.AddView(BuildCard(i), new LinearLayout.LayoutParams(-1, -2) { BottomMargin = Dp(10) });

			var footer = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			footer.SetGravity(GravityFlags.End);
			var reset = MakeButton(L.Reset, false);
			reset.Click += (_, _) =>
			{
				for (int i = 0; i < styles.Length; i++)
					styles[i] = new ButtonStyle();
				SetContentView(BuildLayout());
				HideSystemBars();
			};
			var cancel = MakeButton(L.Cancel, false);
			cancel.Click += (_, _) => Finish();
			var save = MakeButton(L.Save, true);
			save.Click += (_, _) =>
			{
				try { ButtonStyles.Save(stylePath, styles); } catch (Exception) { }
				Toast.MakeText(this, L.ControlsSaved, ToastLength.Short)?.Show();
				Finish();
			};
			footer.AddView(reset, new LinearLayout.LayoutParams(Dp(90), Dp(38)));
			footer.AddView(cancel, new LinearLayout.LayoutParams(Dp(90), Dp(38)) { LeftMargin = Dp(8) });
			footer.AddView(save, new LinearLayout.LayoutParams(Dp(90), Dp(38)) { LeftMargin = Dp(8) });
			outer.AddView(footer, new LinearLayout.LayoutParams(-1, -2) { TopMargin = Dp(8) });
			return root;
		}

		private View BuildCard(int idx)
		{
			ButtonStyle st = styles[idx];
			var card = new LinearLayout(this) { Orientation = Orientation.Vertical };
			card.SetPadding(Dp(14), Dp(10), Dp(14), Dp(10));
			var shape = new GradientDrawable();
			shape.SetColor(Color.Argb(90, 255, 255, 255));
			shape.SetCornerRadius(Dp(14));
			card.Background = shape;

			// Linha 1: nome + tecla
			var row1 = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			row1.SetGravity(GravityFlags.CenterVertical);
			row1.AddView(Text(ButtonStyles.Names[idx], 16, Accent, true), new LinearLayout.LayoutParams(Dp(90), -2));
			row1.AddView(Text(L.KeyboardKey + ":", 13, Color.White, false), new LinearLayout.LayoutParams(-2, -2));
			var keyBtn = MakeButton(ButtonStyles.Display(st.Key), false);
			keyBtn.Click += (_, _) => CaptureKey(idx, keyBtn);
			row1.AddView(keyBtn, new LinearLayout.LayoutParams(Dp(130), Dp(34)) { LeftMargin = Dp(8) });
			var clear = MakeButton("✕", false);
			clear.Click += (_, _) => { st.Key = "-"; keyBtn.Text = ButtonStyles.Display(st.Key); };
			row1.AddView(clear, new LinearLayout.LayoutParams(Dp(40), Dp(34)) { LeftMargin = Dp(6) });
			card.AddView(row1, new LinearLayout.LayoutParams(-1, -2));

			// Linha 2: cor
			var row2 = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			row2.SetGravity(GravityFlags.CenterVertical);
			row2.AddView(Text(L.ButtonColor + ":", 13, Color.White, false), new LinearLayout.LayoutParams(Dp(90), -2));
			var hs = new HorizontalScrollView(this) { HorizontalScrollBarEnabled = false };
			var swatches = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			var views = new List<View>();
			void Refresh()
			{
				for (int k = 0; k < views.Count; k++)
				{
					int rgb = k == 0 ? -1 : Convert.ToInt32(Palette[k - 1], 16);
					bool sel = rgb == st.Rgb;
					var d = new GradientDrawable();
					d.SetShape(ShapeType.Oval);
					d.SetColor(k == 0 ? Color.Argb(60, 255, 255, 255) : Color.ParseColor("#" + Palette[k - 1]));
					d.SetStroke(Dp(sel ? 3 : 1), sel ? Accent : Color.Argb(160, 255, 255, 255));
					views[k].Background = d;
				}
			}
			for (int k = 0; k <= Palette.Length; k++)
			{
				int kk = k;
				View sw;
				if (k == 0)
				{
					var t = Text("A", 11, Color.White, true);
					t.Gravity = GravityFlags.Center;
					sw = t;
				}
				else
					sw = new View(this);
				sw.Click += (_, _) =>
				{
					st.Rgb = kk == 0 ? -1 : Convert.ToInt32(Palette[kk - 1], 16);
					Refresh();
				};
				views.Add(sw);
				swatches.AddView(sw, new LinearLayout.LayoutParams(Dp(30), Dp(30)) { RightMargin = Dp(8) });
			}
			Refresh();
			hs.AddView(swatches);
			row2.AddView(hs, new LinearLayout.LayoutParams(0, -2, 1f));
			card.AddView(row2, new LinearLayout.LayoutParams(-1, -2) { TopMargin = Dp(6) });

			// Linha 3: opacidade
			var row3 = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			row3.SetGravity(GravityFlags.CenterVertical);
			var opLabel = Text("", 13, Color.White, false);
			row3.AddView(opLabel, new LinearLayout.LayoutParams(Dp(90), -2));
			var seek = new SeekBar(this) { Max = 100 };
			seek.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Accent);
			seek.ThumbTintList = Android.Content.Res.ColorStateList.ValueOf(Accent);
			var useGlobal = new CheckBox(this) { Text = L.UseGlobalOpacity };
			useGlobal.SetTextColor(Color.White);
			useGlobal.SetTextSize(ComplexUnitType.Sp, 12);
			useGlobal.ButtonTintList = Android.Content.Res.ColorStateList.ValueOf(Accent);
			int globalOp = GameOptions.Opacity(GameOptions.Prefs(this));
			void UpdateOp()
			{
				bool g = st.Opacity < 0;
				seek.Enabled = !g;
				seek.Alpha = g ? 0.4f : 1f;
				opLabel.Text = $"{L.Opacity}: {(g ? globalOp : st.Opacity)}%";
			}
			useGlobal.Checked = st.Opacity < 0;
			seek.Progress = st.Opacity < 0 ? globalOp : st.Opacity;
			useGlobal.CheckedChange += (_, e) =>
			{
				st.Opacity = e.IsChecked ? -1 : seek.Progress;
				UpdateOp();
			};
			seek.ProgressChanged += (_, e) =>
			{
				if (!e.FromUser)
					return;
				st.Opacity = e.Progress;
				UpdateOp();
			};
			UpdateOp();
			row3.AddView(seek, new LinearLayout.LayoutParams(0, -2, 1f));
			row3.AddView(useGlobal, new LinearLayout.LayoutParams(-2, -2) { LeftMargin = Dp(8) });
			card.AddView(row3, new LinearLayout.LayoutParams(-1, -2) { TopMargin = Dp(4) });
			return card;
		}

		private static readonly string[] KeyList = BuildKeyList();

		private static string[] BuildKeyList()
		{
			var keys = new List<string> { "-" };
			for (char c = 'A'; c <= 'Z'; c++)
				keys.Add(c.ToString());
			for (int i = 0; i <= 9; i++)
				keys.Add("D" + i);
			keys.AddRange(new[] { "Space", "Enter", "Tab", "Escape", "Back", "Up", "Down", "Left", "Right",
				"LeftShift", "RightShift", "LeftControl", "RightControl", "LeftAlt", "RightAlt" });
			for (int i = 1; i <= 12; i++)
				keys.Add("F" + i);
			for (int i = 0; i <= 9; i++)
				keys.Add("NumPad" + i);
			keys.AddRange(new[] { "OemComma", "OemPeriod", "OemQuestion", "OemSemicolon", "OemQuotes", "OemOpenBrackets",
				"OemCloseBrackets", "OemPipe", "OemMinus", "OemPlus", "OemTilde", "PageUp", "PageDown", "Home", "End", "Insert", "Delete" });
			return keys.ToArray();
		}

		private void CaptureKey(int idx, Button target)
		{
			string[] labels = new string[KeyList.Length];
			for (int i = 0; i < labels.Length; i++)
				labels[i] = ButtonStyles.Display(KeyList[i]);
			int current = Math.Max(0, Array.IndexOf(KeyList, styles[idx].Key));
			var dialog = new AlertDialog.Builder(this)!
				.SetTitle(L.KeyboardKey)!
				.SetSingleChoiceItems(labels, current, (IDialogInterfaceOnClickListener?)null)!
				.SetNegativeButton(L.Cancel, (IDialogInterfaceOnClickListener?)null)!
				.Create()!;
			dialog.DismissEvent += (_, _) => HideSystemBars();
			dialog.Show();
			dialog.ListView!.ItemClick += (_, e) =>
			{
				styles[idx].Key = KeyList[e.Position];
				target.Text = ButtonStyles.Display(styles[idx].Key);
				dialog.Dismiss();
			};
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
}
