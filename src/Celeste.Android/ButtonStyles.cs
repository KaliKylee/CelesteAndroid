using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Android.Views;

namespace CelesteAndroid
{
	public class ButtonStyle
	{
		public string Key = "-";
		public int Rgb = -1;
		public int Opacity = -1;
		public string? Text;
	}

	public class CustomButton
	{
		public string Key = "pad:A";
		public string? Text;
		public int Rgb = 0x4DA3FF;
		public int Opacity = -1;
		public float X = 50f, Y = 50f;
		public int Size = 100;
		public int Shape = 0;
		public int HSize = -1;
		public string? Icon;
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
					if ((v.Length != 6 && v.Length != 9 && v.Length != 10) || list.Count >= Max)
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
						Shape = v.Length >= 9 ? Math.Clamp(I(v[6], 0), 0, 2) : 0,
						HSize = v.Length >= 9 ? Math.Clamp(I(v[7], -1), 40, 250) : -1,
						Icon = v.Length >= 9 && v[8].Trim().Length > 0 && v[8].Trim() != "-" ? v[8].Trim() : null,
						Text = v.Length == 10 ? PixelButtonArt.CleanLabel(v[9]) : null,
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
				string? text = PixelButtonArt.CleanLabel(b.Text);
				if (b.Shape != 0 || b.Icon != null || text != null)
					sb.Append(',').Append(b.Shape.ToString(CultureInfo.InvariantCulture)).Append(',').Append((b.HSize > 0 ? b.HSize : b.Size).ToString(CultureInfo.InvariantCulture)).Append(',').Append(b.Icon ?? "-");
				if (text != null)
					sb.Append(',').Append(text);
				sb.Append('\n');
			}
			File.WriteAllText(path, sb.ToString());
		}
	}

	public static class ButtonStyles
	{
		public static readonly string[] Palette =
		{
			"FFFFFF", "FF4D4D", "FF9A3C", "FFD93D", "6BE675", "3CD6C8", "4DA3FF", "8A6BFF", "E070FF", "FF7EB6", "9AA0A6", "222222",
		};

		public static readonly string[] KeyList = BuildKeyList();

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
					if (idx < 0 || (v.Length != 3 && v.Length != 4))
						continue;
					styles[idx].Text = v.Length == 4 ? PixelButtonArt.CleanLabel(v[3]) : null;
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
			bool untouched = true;
			for (int i = 0; i < styles.Length; i++)
			{
				ButtonStyle s = styles[i];
				if (s.Key != "-" || s.Rgb >= 0 || s.Opacity >= 0 || PixelButtonArt.CleanLabel(s.Text) != null)
					untouched = false;
			}
			if (untouched)
			{
				if (File.Exists(path))
					File.Delete(path);
				return;
			}
			var sb = new StringBuilder();
			for (int i = 0; i < Ids.Length; i++)
			{
				ButtonStyle s = styles[i];
				sb.Append(Ids[i]).Append('=').Append(s.Key).Append(',')
					.Append(s.Rgb >= 0 ? s.Rgb.ToString("X6", CultureInfo.InvariantCulture) : "-").Append(',')
					.Append(s.Opacity.ToString(CultureInfo.InvariantCulture));
				string? text = PixelButtonArt.CleanLabel(s.Text);
				if (text != null)
					sb.Append(',').Append(text);
				sb.Append('\n');
			}
			File.WriteAllText(path, sb.ToString());
		}


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
			_ when key.StartsWith("pad:", StringComparison.Ordinal) => "🎮 " + PixelButtonArt.PadName(key.Substring(4)),
			_ when key.Length == 2 && key[0] == 'D' && char.IsDigit(key[1]) => key.Substring(1),
			_ => key,
		};
	}
}
