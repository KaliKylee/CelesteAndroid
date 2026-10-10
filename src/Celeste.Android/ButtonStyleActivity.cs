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

	public class CustomButton
	{
		public string Key = "-";
		public int Rgb = 0x4DA3FF;
		public int Opacity = -1;   // -1 = opacidade geral
		public float X = 50f, Y = 50f; // % da tela
		public int Size = 100;     // % do tamanho padrão (largura)
		public int Shape = 0;      // 0 = círculo, 1 = quadrado, 2 = retângulo
		public int HSize = -1;     // % da altura no retângulo (-1 = igual à largura)
		public string? Icon;       // foto escolhida na galeria
	}

	public static class CustomButtons
	{
		public const int Max = 12;

		public static List<CustomButton> Load(string path)
		{
			var list = new List<CustomButton>();
			try
			{
				if (!File.Exists(path))
					return list;
				foreach (string line in File.ReadAllLines(path))
				{
					string[] kv = line.Split('=');
					if (kv.Length != 2 || !kv[0].Trim().StartsWith("c", StringComparison.Ordinal))
						continue;
					string[] v = kv[1].Split(',');
					if ((v.Length != 6 && v.Length != 9) || list.Count >= Max)
						continue;
					int I(string t, int d) => int.TryParse(t.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int r) ? r : d;
					float Fl(string t, float d) => float.TryParse(t.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float r) ? r : d;
					int rgb = v[1].Trim().Length == 6 && int.TryParse(v[1].Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int c) ? c : 0x4DA3FF;
					list.Add(new CustomButton
					{
						Key = v[0].Trim().Length > 0 ? v[0].Trim() : "-",
						Rgb = rgb,
						Opacity = I(v[2], -1),
						X = Math.Clamp(Fl(v[3], 50f), 0f, 100f),
						Y = Math.Clamp(Fl(v[4], 50f), 0f, 100f),
						Size = Math.Clamp(I(v[5], 100), 40, 250),
						Shape = v.Length == 9 ? Math.Clamp(I(v[6], 0), 0, 2) : 0,
						HSize = v.Length == 9 ? Math.Clamp(I(v[7], -1), 40, 250) : -1,
						Icon = v.Length == 9 && v[8].Trim().Length > 0 && v[8].Trim() != "-" ? v[8].Trim() : null,
					});
				}
			}
			catch (Exception)
			{
			}
			return list;
		}

		public static void Save(string path, List<CustomButton> list)
		{
			if (list.Count == 0)
			{
				if (File.Exists(path))
					File.Delete(path);
				return;
			}
			var sb = new StringBuilder();
			for (int i = 0; i < list.Count; i++)
			{
				CustomButton b = list[i];
				sb.Append('c').Append(i + 1).Append('=').Append(b.Key).Append(',').Append(b.Rgb.ToString("X6", CultureInfo.InvariantCulture)).Append(',')
					.Append(b.Opacity.ToString(CultureInfo.InvariantCulture)).Append(',').Append(b.X.ToString("0.##", CultureInfo.InvariantCulture)).Append(',')
					.Append(b.Y.ToString("0.##", CultureInfo.InvariantCulture)).Append(',').Append(b.Size.ToString(CultureInfo.InvariantCulture));
				if (b.Shape != 0 || b.Icon != null)
					sb.Append(',').Append(b.Shape.ToString(CultureInfo.InvariantCulture)).Append(',').Append((b.HSize > 0 ? b.HSize : b.Size).ToString(CultureInfo.InvariantCulture)).Append(',').Append(b.Icon ?? "-");
				sb.Append('\n');
			}
			File.WriteAllText(path, sb.ToString());
		}
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
		private List<CustomButton> customs = null!;
		private string customPath = null!;
		private string stylePath = null!;

		public override ScreenOrientation RequestedOrientation
		{
			get => base.RequestedOrientation;
			set => base.RequestedOrientation = LandscapeLock.Coerce(value);
		}

		protected override void OnCreate(Bundle? savedInstanceState)
		{
			CultureFix.Apply();
			RequestedOrientation = LandscapeLock.Orientation;
			base.OnCreate(savedInstanceState);
			L.Init(GameOptions.Prefs(this));
			stylePath = GameInstaller.ButtonStyleFile(this);
			styles = ButtonStyles.Load(stylePath);
			customPath = GameInstaller.CustomButtonsFile(this);
			customs = CustomButtons.Load(customPath);
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

			list.AddView(Text(L.CustomButtons, 15, Accent, true), new LinearLayout.LayoutParams(-1, -2) { BottomMargin = Dp(6) });
			for (int i = 0; i < customs.Count; i++)
				list.AddView(BuildCustomCard(i), new LinearLayout.LayoutParams(-1, -2) { BottomMargin = Dp(10) });
			var add = MakeButton("＋  " + L.AddButton, true);
			add.Enabled = customs.Count < CustomButtons.Max;
			add.Click += (_, _) =>
			{
				customs.Add(new CustomButton { X = 36 + (customs.Count % 7) * 5, Y = 40 + (customs.Count / 7) * 14 });
				SetContentView(BuildLayout());
				HideSystemBars();
			};
			list.AddView(add, new LinearLayout.LayoutParams(Dp(220), Dp(40)) { BottomMargin = Dp(16) });

			list.AddView(Text(L.DefaultButtons, 15, Accent, true), new LinearLayout.LayoutParams(-1, -2) { BottomMargin = Dp(6) });
			for (int i = 0; i < ButtonStyles.Ids.Length; i++)
				list.AddView(BuildCard(i), new LinearLayout.LayoutParams(-1, -2) { BottomMargin = Dp(10) });

			var footer = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			footer.SetGravity(GravityFlags.End);
			var reset = MakeButton(L.Reset, false);
			reset.Click += (_, _) =>
			{
				for (int i = 0; i < styles.Length; i++)
					styles[i] = new ButtonStyle();
				customs.Clear();
				SetContentView(BuildLayout());
				HideSystemBars();
			};
			var cancel = MakeButton(L.Cancel, false);
			cancel.Click += (_, _) => Finish();
			var save = MakeButton(L.Save, true);
			save.Click += (_, _) =>
			{
				try { ButtonStyles.Save(stylePath, styles); CustomButtons.Save(customPath, customs); } catch (Exception) { }
				Toast.MakeText(this, L.ControlsSaved, ToastLength.Short)?.Show();
				Finish();
			};
			footer.AddView(reset, new LinearLayout.LayoutParams(Dp(90), Dp(38)));
			footer.AddView(cancel, new LinearLayout.LayoutParams(Dp(90), Dp(38)) { LeftMargin = Dp(8) });
			footer.AddView(save, new LinearLayout.LayoutParams(Dp(90), Dp(38)) { LeftMargin = Dp(8) });
			outer.AddView(footer, new LinearLayout.LayoutParams(-1, -2) { TopMargin = Dp(8) });
			return root;
		}

		private View BuildCustomCard(int idx)
		{
			CustomButton cb = customs[idx];
			var card = new LinearLayout(this) { Orientation = Orientation.Vertical };
			card.SetPadding(Dp(14), Dp(10), Dp(14), Dp(10));
			var shape = new GradientDrawable();
			shape.SetColor(Color.Argb(90, 255, 255, 255));
			shape.SetCornerRadius(Dp(14));
			card.Background = shape;

			var row1 = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			row1.SetGravity(GravityFlags.CenterVertical);
			row1.AddView(Text("#" + (idx + 1), 16, Accent, true), new LinearLayout.LayoutParams(Dp(90), -2));
			row1.AddView(Text(L.KeyboardKey + ":", 13, Color.White, false), new LinearLayout.LayoutParams(-2, -2));
			var keyBtn = MakeButton(ButtonStyles.Display(cb.Key), false);
			keyBtn.Click += (_, _) => PickKey(cb.Key, key => { cb.Key = key; keyBtn.Text = ButtonStyles.Display(key); });
			row1.AddView(keyBtn, new LinearLayout.LayoutParams(Dp(130), Dp(34)) { LeftMargin = Dp(8) });
			var spacer = new View(this);
			row1.AddView(spacer, new LinearLayout.LayoutParams(0, 1, 1f));
			var del = MakeButton("🗑  " + L.RemoveButton, false);
			del.Click += (_, _) =>
			{
				customs.RemoveAt(idx);
				SetContentView(BuildLayout());
				HideSystemBars();
			};
			row1.AddView(del, new LinearLayout.LayoutParams(-2, Dp(34)));
			card.AddView(row1, new LinearLayout.LayoutParams(-1, -2));

			AddColorRow(card, () => cb.Rgb, v => cb.Rgb = v, allowDefault: false);

			int globalOp = GameOptions.Opacity(GameOptions.Prefs(this));
			var opRow = SliderRow(L.Opacity, 0, 100, cb.Opacity < 0 ? globalOp : cb.Opacity, v => cb.Opacity = v, "%");
			var useGlobal = new CheckBox(this) { Text = L.UseGlobalOpacity, Checked = cb.Opacity < 0 };
			useGlobal.SetTextColor(Color.White);
			useGlobal.SetTextSize(ComplexUnitType.Sp, 12);
			useGlobal.ButtonTintList = Android.Content.Res.ColorStateList.ValueOf(Accent);
			useGlobal.CheckedChange += (_, e) =>
			{
				if (e.IsChecked)
					cb.Opacity = -1;
				else
					cb.Opacity = ((SeekBar)opRow.GetChildAt(1)!).Progress;
				((SeekBar)opRow.GetChildAt(1)!).Enabled = !e.IsChecked;
			};
			((SeekBar)opRow.GetChildAt(1)!).Enabled = cb.Opacity >= 0;
			opRow.AddView(useGlobal, new LinearLayout.LayoutParams(-2, -2) { LeftMargin = Dp(8) });
			card.AddView(opRow, new LinearLayout.LayoutParams(-1, -2) { TopMargin = Dp(4) });

			card.AddView(SliderRow("X", 0, 100, (int)Math.Round(cb.X), v => cb.X = v, "%"), new LinearLayout.LayoutParams(-1, -2));
			card.AddView(SliderRow("Y", 0, 100, (int)Math.Round(cb.Y), v => cb.Y = v, "%"), new LinearLayout.LayoutParams(-1, -2));
			card.AddView(SliderRow(L.Size, 40, 250, cb.Size, v => cb.Size = v, "%"), new LinearLayout.LayoutParams(-1, -2));
			return card;
		}

		private LinearLayout SliderRow(string name, int min, int max, int value, Action<int> set, string unit)
		{
			var row = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			row.SetGravity(GravityFlags.CenterVertical);
			var label = Text("", 13, Color.White, false);
			var seek = new SeekBar(this) { Max = max - min };
			seek.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Accent);
			seek.ThumbTintList = Android.Content.Res.ColorStateList.ValueOf(Accent);
			seek.Progress = Math.Clamp(value, min, max) - min;
			label.Text = $"{name}: {Math.Clamp(value, min, max)}{unit}";
			seek.ProgressChanged += (_, e) =>
			{
				if (!e.FromUser)
					return;
				set(e.Progress + min);
				label.Text = $"{name}: {e.Progress + min}{unit}";
			};
			row.AddView(label, new LinearLayout.LayoutParams(Dp(110), -2));
			row.AddView(seek, new LinearLayout.LayoutParams(0, -2, 1f));
			return row;
		}

		private void AddColorRow(LinearLayout card, Func<int> get, Action<int> set, bool allowDefault)
		{
			var row = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			row.SetGravity(GravityFlags.CenterVertical);
			row.AddView(Text(L.ButtonColor + ":", 13, Color.White, false), new LinearLayout.LayoutParams(Dp(110), -2));
			var hs = new HorizontalScrollView(this) { HorizontalScrollBarEnabled = false };
			var swatches = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			var views = new List<View>();
			int offset = allowDefault ? 1 : 0;
			int RgbAt(int k) => allowDefault && k == 0 ? -1 : Convert.ToInt32(Palette[k - offset], 16);
			void Refresh()
			{
				for (int k = 0; k < views.Count; k++)
				{
					bool sel = RgbAt(k) == get();
					var d = new GradientDrawable();
					d.SetShape(ShapeType.Oval);
					d.SetColor(allowDefault && k == 0 ? Color.Argb(60, 255, 255, 255) : Color.ParseColor("#" + Palette[k - offset]));
					d.SetStroke(Dp(sel ? 3 : 1), sel ? Accent : Color.Argb(160, 255, 255, 255));
					views[k].Background = d;
				}
			}
			for (int k = 0; k < Palette.Length + offset; k++)
			{
				int kk = k;
				View sw;
				if (allowDefault && k == 0)
				{
					var t = Text("A", 11, Color.White, true);
					t.Gravity = GravityFlags.Center;
					sw = t;
				}
				else
					sw = new View(this);
				sw.Click += (_, _) => { set(RgbAt(kk)); Refresh(); };
				views.Add(sw);
				swatches.AddView(sw, new LinearLayout.LayoutParams(Dp(30), Dp(30)) { RightMargin = Dp(8) });
			}
			Refresh();
			hs.AddView(swatches);
			row.AddView(hs, new LinearLayout.LayoutParams(0, -2, 1f));
			card.AddView(row, new LinearLayout.LayoutParams(-1, -2) { TopMargin = Dp(6) });
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
			=> PickKey(styles[idx].Key, key => { styles[idx].Key = key; target.Text = ButtonStyles.Display(key); });

		private void PickKey(string currentKey, Action<string> onPicked)
		{
			string[] labels = new string[KeyList.Length];
			for (int i = 0; i < labels.Length; i++)
				labels[i] = ButtonStyles.Display(KeyList[i]);
			int current = Math.Max(0, Array.IndexOf(KeyList, currentKey));
			var dialog = new AlertDialog.Builder(this)!
				.SetTitle(L.KeyboardKey)!
				.SetSingleChoiceItems(labels, current, (IDialogInterfaceOnClickListener?)null)!
				.SetNegativeButton(L.Cancel, (IDialogInterfaceOnClickListener?)null)!
				.Create()!;
			dialog.DismissEvent += (_, _) => HideSystemBars();
			dialog.Show();
			dialog.ListView!.ItemClick += (_, e) =>
			{
				onPicked(KeyList[e.Position]);
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
