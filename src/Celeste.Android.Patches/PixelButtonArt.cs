using System;
using System.Collections.Generic;

namespace CelesteAndroid
{
	public static class PixelButtonArt
	{
		public const int Size = 28;

		public sealed class Sprite
		{
			public readonly string[] Rows;
			public readonly int[] Rgb;
			public Sprite(string[] rows, int[] rgb) { Rows = rows; Rgb = rgb; }

			public int ColorOf(char ch)
			{
				int i = "ODBLSWK".IndexOf(ch);
				return i < 0 ? -1 : Rgb[i];
			}

			public int GlowRgb => Rgb[2];
		}


		public static readonly Sprite Jump = new(
			new[]
			{
				"...........OOOOOO...........",
				"........OOOOLLLLOOOO........",
				"......OOOLLLLLLLLLLOOO......",
				".....OOLLLBBBBBBBBBBLOO.....",
				"....OOLLBBBBBBBBBBBBBBOO....",
				"...OOLLBSSSBBBBBBBBBBBDOO...",
				"..OOLLBSSBBBBBBBBBBBBBBDOO..",
				"..OLLBSSBBBWWWWWWBBBBBBBDO..",
				".OOLBBSBBBBWWWWWWKBBBBBBDOO.",
				".OLLBBBBBWWBKKKKKWWBBBBBDDO.",
				".OLBBBBBBWWKBBBBBWWKBBBBDDO.",
				"OOLBBBBBBWWKBBBBBWWKBBBBBDOO",
				"OLLBBBBBBWWKBBBBBWWKBBBBBDDO",
				"OLLBBBBBBWWWWWWWWWWKBBBBBDDO",
				"OLLBBBBBBWWWWWWWWWWKBBBBBDDO",
				"OLLBBBBBBWWKKKKKKWWKBBBBDDDO",
				"OOLBBBBBBWWKBBBBBWWKBBBBDDOO",
				".OLBBBBBBWWKBBBBBWWKBBBBDDO.",
				".OLBBBBBBWWKBBBBBWWKBBBDDDO.",
				".OOBBBBBBWWKBBBBBWWKBBBDDOO.",
				"..OLBBBBBWWKBBBBBWWKBBDDDO..",
				"..OOBBBBBBKKBBBBBBKKBDDDOO..",
				"...OODBBBBBBBBBBBBBBDDDOO...",
				"....OODBBBBBBBBBBBDDDDOO....",
				".....OODDDDBBBBDDDDDDOO.....",
				"......OOODDDDDDDDDDOOO......",
				"........OOOODDDDOOOO........",
				"...........OOOOOO...........",
			},
			new[] { 0x032A17, 0x428A5C, 0x58A971, 0x76D08C, 0xA8ECB8, 0xF6F6F6, 0x1E5C3A });

		public static readonly Sprite Dash = new(
			new[]
			{
				"...........OOOOOO...........",
				"........OOOOLLLLOOOO........",
				"......OOOLLLLLLLLLLOOO......",
				".....OOLLLBBBBBBBBBBLOO.....",
				"....OOLLBBBBBBBBBBBBBBOO....",
				"...OOLLBSSSBBBBBBBBBBBDOO...",
				"..OOLLBSSBBBBBBBBBBBBBBDOO..",
				"..OLLBSSBWWBBBBBBWWBBBBBDO..",
				".OOLBBSBBWWKBBBBBWWKBBBBDOO.",
				".OLLBBBBBWWKBBBBBWWKBBBBDDO.",
				".OLBBBBBBWWKBBBBBWWKBBBBDDO.",
				"OOLBBBBBBBKWWBBWWBKKBBBBBDOO",
				"OLLBBBBBBBBWWKBWWKBBBBBBBDDO",
				"OLLBBBBBBBBBKWWBKKBBBBBBBDDO",
				"OLLBBBBBBBBBBWWKBBBBBBBBBDDO",
				"OLLBBBBBBBBWWBKWWBBBBBBBDDDO",
				"OOLBBBBBBBBWWKBWWKBBBBBBDDOO",
				".OLBBBBBBWWBKKBBKWWBBBBBDDO.",
				".OLBBBBBBWWKBBBBBWWKBBBDDDO.",
				".OOBBBBBBWWKBBBBBWWKBBBDDOO.",
				"..OLBBBBBWWKBBBBBWWKBBDDDO..",
				"..OOBBBBBBKKBBBBBBKKBDDDOO..",
				"...OODBBBBBBBBBBBBBBDDDOO...",
				"....OODBBBBBBBBBBBDDDDOO....",
				".....OODDDDBBBBDDDDDDOO.....",
				"......OOODDDDDDDDDDOOO......",
				"........OOOODDDDOOOO........",
				"...........OOOOOO...........",
			},
			new[] { 0x3E0321, 0xA5486C, 0xC86385, 0xE08CA8, 0xF8C0D2, 0xF6F6F6, 0x782448 });

		public static readonly Sprite Grab = new(
			new[]
			{
				"...........OOOOOO...........",
				"........OOOOLLLLOOOO........",
				"......OOOLLLLLLLLLLOOO......",
				".....OOLLLBBBBBBBBBBLOO.....",
				"....OOLLBBBBBBBBBBBBBBOO....",
				"...OOLLBSSSBBBBBBBBBBBDOO...",
				"..OOLLBSSBBBBBBBBBBBBBBDOO..",
				"..OLLBSSBBBWWWWWWWWBBBBBDO..",
				".OOLBBSBBBBWWWWWWWWKBBBBDOO.",
				".OLLBBBBBWWBKKKKKKKKBBBBDDO.",
				".OLBBBBBBWWKBBBBBBBBBBBBDDO.",
				"OOLBBBBBBWWKBBBBBBBBBBBBBDOO",
				"OLLBBBBBBWWKBBBBBBBBBBBBBDDO",
				"OLLBBBBBBWWKBWWWWWWBBBBBBDDO",
				"OLLBBBBBBWWKBWWWWWWKBBBBBDDO",
				"OLLBBBBBBWWKBBKKKWWKBBBBDDDO",
				"OOLBBBBBBWWKBBBBBWWKBBBBDDOO",
				".OLBBBBBBWWKBBBBBWWKBBBBDDO.",
				".OLBBBBBBWWKBBBBBWWKBBBDDDO.",
				".OOBBBBBBBKWWWWWWWWKBBBDDOO.",
				"..OLBBBBBBBWWWWWWWWKBBDDDO..",
				"..OOBBBBBBBBKKKKKKKKBDDDOO..",
				"...OODBBBBBBBBBBBBBBDDDOO...",
				"....OODBBBBBBBBBBBDDDDOO....",
				".....OODDDDBBBBDDDDDDOO.....",
				"......OOODDDDDDDDDDOOO......",
				"........OOOODDDDOOOO........",
				"...........OOOOOO...........",
			},
			new[] { 0x020C51, 0x304488, 0x5C85C0, 0x84A6D4, 0xB0CAE8, 0xF6F6F6, 0x24388C });

		public static readonly Sprite Tab = new(
			new[]
			{
				"...........OOOOOO...........",
				"........OOOOLLLLOOOO........",
				"......OOOLLLLLLLLLLOOO......",
				".....OOLLLBBBBBBBBBBLOO.....",
				"....OOLLBBBBBBBBBBBBBBOO....",
				"...OOLLBSSSBBBBBBBBBBBDOO...",
				"..OOLLBSSBBBBBBBBBBBBBBDOO..",
				"..OLLBSSBBBBBBBBBBBBBBBBDO..",
				".OOLBBSBBBBBBBBBBBBBBBBBDOO.",
				".OLLWWWWWWBBBWWBBBWWWWBBDDO.",
				".OLBWWWWWWKBBWWKBBWWWWKBDDO.",
				"OOLBBKWWKKKWWBKWWBWWKKWWBDOO",
				"OLLBBBWWKBBWWKBWWKWWKBWWKDDO",
				"OLLBBBWWKBBWWWWWWKWWWWBKKDDO",
				"OLLBBBWWKBBWWWWWWKWWWWKBBDDO",
				"OLLBBBWWKBBWWKKWWKWWKKWWDDDO",
				"OOLBBBWWKBBWWKBWWKWWKBWWKDOO",
				".OLBBBWWKBBWWKBWWKWWWWBKKDO.",
				".OLBBBWWKBBWWKBWWKWWWWKDDDO.",
				".OOBBBBKKBBBKKBBKKBKKKKDDOO.",
				"..OLBBBBBBBBBBBBBBBBBBDDDO..",
				"..OOBBBBBBBBBBBBBBBBBDDDOO..",
				"...OODBBBBBBBBBBBBBBDDDOO...",
				"....OODBBBBBBBBBBBDDDDOO....",
				".....OODDDDBBBBDDDDDDOO.....",
				"......OOODDDDDDDDDDOOO......",
				"........OOOODDDDOOOO........",
				"...........OOOOOO...........",
			},
			new[] { 0x4A2600, 0xB07628, 0xE0AC48, 0xF2CC74, 0xFFE8A8, 0xFAF6F0, 0x965C18 });

		public static readonly Sprite Pause = new(
			new[]
			{
				"...........OOOOOO...........",
				"........OOOOLLLLOOOO........",
				"......OOOLLLLLLLLLLOOO......",
				".....OOLLLBBBBBBBBBBLOO.....",
				"....OOLLBBBBBBBBBBBBBBOO....",
				"...OOLLBSSSBBBBBBBBBBBDOO...",
				"..OOLLBSSBBBBBBBBBBBBBBDOO..",
				"..OLLBSSBBBBBBBBBBBBBBBBDO..",
				".OOLBBSBBBBBBBBBBBBBBBBBDOO.",
				".OLLBBBBBWWWBBBBBWWWBBBBDDO.",
				".OLBBBBBBWWWKBBBBWWWKBBBDDO.",
				"OOLBBBBBBWWWKBBBBWWWKBBBBDOO",
				"OLLBBBBBBWWWKBBBBWWWKBBBBDDO",
				"OLLBBBBBBWWWKBBBBWWWKBBBBDDO",
				"OLLBBBBBBWWWKBBBBWWWKBBBBDDO",
				"OLLBBBBBBWWWKBBBBWWWKBBBDDDO",
				"OOLBBBBBBWWWKBBBBWWWKBBBDDOO",
				".OLBBBBBBWWWKBBBBWWWKBBBDDO.",
				".OLBBBBBBWWWKBBBBWWWKBBDDDO.",
				".OOBBBBBBBKKKBBBBBKKKBBDDOO.",
				"..OLBBBBBBBBBBBBBBBBBBDDDO..",
				"..OOBBBBBBBBBBBBBBBBBDDDOO..",
				"...OODBBBBBBBBBBBBBBDDDOO...",
				"....OODBBBBBBBBBBBDDDDOO....",
				".....OODDDDBBBBDDDDDDOO.....",
				"......OOODDDDDDDDDDOOO......",
				"........OOOODDDDOOOO........",
				"...........OOOOOO...........",
			},
			new[] { 0x1C1C2C, 0x78788A, 0xB0B0C0, 0xD2D2E0, 0xF0F0F8, 0xFAFAFA, 0x606074 });

		// ---------- Botões com forma livre (círculo / quadrado / retângulo) no mesmo estilo pixel art ----------

		public static readonly Dictionary<char, string[]> Glyphs = new()
		{
			['0'] = new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" },
			['1'] = new[] { "00100", "01100", "00100", "00100", "00100", "00100", "01110" },
			['2'] = new[] { "01110", "10001", "00001", "00110", "01000", "10000", "11111" },
			['3'] = new[] { "11110", "00001", "00001", "01110", "00001", "00001", "11110" },
			['4'] = new[] { "00010", "00110", "01010", "10010", "11111", "00010", "00010" },
			['5'] = new[] { "11111", "10000", "11110", "00001", "00001", "10001", "01110" },
			['6'] = new[] { "00110", "01000", "10000", "11110", "10001", "10001", "01110" },
			['7'] = new[] { "11111", "00001", "00010", "00100", "01000", "01000", "01000" },
			['8'] = new[] { "01110", "10001", "10001", "01110", "10001", "10001", "01110" },
			['9'] = new[] { "01110", "10001", "10001", "01111", "00001", "00010", "01100" },
			['A'] = new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" },
			['B'] = new[] { "11110", "10001", "10001", "11110", "10001", "10001", "11110" },
			['C'] = new[] { "01110", "10001", "10000", "10000", "10000", "10001", "01110" },
			['D'] = new[] { "11110", "10001", "10001", "10001", "10001", "10001", "11110" },
			['E'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" },
			['F'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" },
			['G'] = new[] { "01110", "10001", "10000", "10111", "10001", "10001", "01111" },
			['H'] = new[] { "10001", "10001", "10001", "11111", "10001", "10001", "10001" },
			['I'] = new[] { "01110", "00100", "00100", "00100", "00100", "00100", "01110" },
			['J'] = new[] { "00111", "00010", "00010", "00010", "00010", "10010", "01100" },
			['K'] = new[] { "10001", "10010", "10100", "11000", "10100", "10010", "10001" },
			['L'] = new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" },
			['M'] = new[] { "10001", "11011", "10101", "10101", "10001", "10001", "10001" },
			['N'] = new[] { "10001", "11001", "10101", "10011", "10001", "10001", "10001" },
			['O'] = new[] { "01110", "10001", "10001", "10001", "10001", "10001", "01110" },
			['P'] = new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" },
			['Q'] = new[] { "01110", "10001", "10001", "10001", "10101", "10010", "01101" },
			['R'] = new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" },
			['S'] = new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" },
			['T'] = new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" },
			['U'] = new[] { "10001", "10001", "10001", "10001", "10001", "10001", "01110" },
			['V'] = new[] { "10001", "10001", "10001", "10001", "10001", "01010", "00100" },
			['W'] = new[] { "10001", "10001", "10001", "10101", "10101", "11011", "10001" },
			['X'] = new[] { "10001", "10001", "01010", "00100", "01010", "10001", "10001" },
			['Y'] = new[] { "10001", "10001", "01010", "00100", "00100", "00100", "00100" },
			['Z'] = new[] { "11111", "00001", "00010", "00100", "01000", "10000", "11111" },
		};

		// Texto curto mostrado num botão ligado a uma tecla (F11, ESC, TAB, SPC...).
		// Botões de gamepad que um botão de toque pode emular (guardados como "pad:<id>").
		public static readonly string[] PadIds = { "A", "B", "X", "Y", "LB", "RB", "LT", "RT", "L3", "R3", "Back", "Start", "Up", "Down", "Left", "Right" };

		public static string PadName(string id) => id switch
		{
			"Up" => "D-Pad Up", "Down" => "D-Pad Down", "Left" => "D-Pad Left", "Right" => "D-Pad Right", _ => id,
		};

		private static string PadLabel(string id) => id switch
		{
			"Back" => "BACK", "Start" => "STRT", "Up" => "DU", "Down" => "DD", "Left" => "DL", "Right" => "DR", _ => id,
		};

		// Texto digitado pelo usuário: só A-Z e 0-9 (o que a fonte pixel desenha), até 4 caracteres.
		public static string? CleanLabel(string? t)
		{
			if (string.IsNullOrEmpty(t))
				return null;
			var sb = new System.Text.StringBuilder();
			foreach (char c in t.ToUpperInvariant())
			{
				if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
					sb.Append(c);
				if (sb.Length >= 4)
					break;
			}
			return sb.Length == 0 ? null : sb.ToString();
		}

		public static string LabelFor(string n)
		{
			if (string.IsNullOrEmpty(n) || n == "-")
				return "";
			if (n.StartsWith("pad:", StringComparison.Ordinal))
				return PadLabel(n.Substring(4));
			if (n.Length == 2 && n[0] == 'D' && char.IsDigit(n[1]))
				return n.Substring(1);
			if (n.StartsWith("NumPad", StringComparison.Ordinal))
				return "N" + n.Substring(6);
			string s = n switch
			{
				"Space" => "SPC", "Enter" => "ENT", "Escape" => "ESC", "Back" => "BS",
				"Up" => "UP", "Down" => "DN", "Left" => "LT", "Right" => "RT",
				"LeftShift" => "LSH", "RightShift" => "RSH", "LeftControl" => "LCT", "RightControl" => "RCT",
				"LeftAlt" => "LAL", "RightAlt" => "RAL", "PageUp" => "PGU", "PageDown" => "PGD",
				"Home" => "HOM", "Insert" => "INS", "Delete" => "DEL",
				_ => n.StartsWith("Oem", StringComparison.Ordinal) ? "SYM" : n.ToUpperInvariant(),
			};
			return s.Length > 4 ? s.Substring(0, 4) : s;
		}

		private static int Mix(int rgb, int to, float t)
		{
			int r = (int)Math.Round(((rgb >> 16) & 255) * (1f - t) + ((to >> 16) & 255) * t);
			int g = (int)Math.Round(((rgb >> 8) & 255) * (1f - t) + ((to >> 8) & 255) * t);
			int b = (int)Math.Round((rgb & 255) * (1f - t) + (to & 255) * t);
			return (r << 16) | (g << 8) | b;
		}

		// Paleta (contorno, sombra, base, luz, brilho, branco, sombra do texto) a partir de uma cor.
		public static int[] PaletteFrom(int rgb)
		{
			rgb &= 0xFFFFFF;
			return new[] { Mix(rgb, 0, 0.80f), Mix(rgb, 0, 0.30f), rgb, Mix(rgb, 0xFFFFFF, 0.30f), Mix(rgb, 0xFFFFFF, 0.60f), 0xF6F6F6, Mix(rgb, 0, 0.55f) };
		}

		private static float Luma(int rgb) => (0.299f * ((rgb >> 16) & 255) + 0.587f * ((rgb >> 8) & 255) + 0.114f * (rgb & 255)) / 255f;

		private static int Opaque(int rgb) => unchecked((int)0xFF000000) | (rgb & 0xFFFFFF);

		// Tamanho da grade de pixels: a menor dimensão do botão vira ~28 "pixels", como nos sprites originais.
		public static void Snap(float w, float h, out int cols, out int rows, out int cell)
		{
			cell = Math.Max(1, (int)Math.Round(Math.Min(w, h) / 28f));
			cols = Math.Max(10, (int)Math.Round(w / cell));
			rows = Math.Max(10, (int)Math.Round(h / cell));
		}

		// Desenha o botão em pixel art (contorno, bisel de luz/sombra, brilho e texto) numa grade cols x rows.
		// shape: 0 = elipse, 1/2 = retângulo de cantos arredondados. Retorna ARGB; fora da forma o alfa é 0.
		// frameOnly: só a moldura (miolo transparente, marcado em interior) para receber uma foto.
		public static int[] BuildSkin(int[] pal, int shape, int cols, int rows, string? label, bool pause, bool frameOnly, out bool[] interior)
		{
			int n = cols * rows;
			var mask = new bool[n];
			float hw = cols / 2f, hh = rows / 2f;
			float rad = Math.Clamp(MathF.Round(Math.Min(cols, rows) * 0.2f), 2f, 7f);
			for (int y = 0; y < rows; y++)
			{
				for (int x = 0; x < cols; x++)
				{
					float px = x + 0.5f - hw, py = y + 0.5f - hh;
					bool inside;
					if (shape == 0)
					{
						float a = px / hw, b = py / hh;
						inside = a * a + b * b <= 1f;
					}
					else
					{
						float qx = MathF.Abs(px) - (hw - rad), qy = MathF.Abs(py) - (hh - rad);
						float ox = MathF.Max(qx, 0f), oy = MathF.Max(qy, 0f);
						inside = MathF.Sqrt(ox * ox + oy * oy) + MathF.Min(MathF.Max(qx, qy), 0f) - rad <= 0f;
					}
					mask[y * cols + x] = inside;
				}
			}

			const int Inf = 9999;
			var dist = new int[n];
			for (int y = 0; y < rows; y++)
			{
				for (int x = 0; x < cols; x++)
				{
					int i = y * cols + x;
					if (!mask[i]) { dist[i] = -1; continue; }
					bool edge = x == 0 || y == 0 || x == cols - 1 || y == rows - 1
						|| !mask[i - 1] || !mask[i + 1] || !mask[i - cols] || !mask[i + cols];
					dist[i] = edge ? 0 : Inf;
				}
			}
			for (int y = 0; y < rows; y++)
			{
				for (int x = 0; x < cols; x++)
				{
					int i = y * cols + x;
					if (!mask[i] || dist[i] == 0) continue;
					int d = dist[i];
					if (x > 0 && mask[i - 1]) d = Math.Min(d, dist[i - 1] + 1);
					if (y > 0 && mask[i - cols]) d = Math.Min(d, dist[i - cols] + 1);
					dist[i] = d;
				}
			}
			for (int y = rows - 1; y >= 0; y--)
			{
				for (int x = cols - 1; x >= 0; x--)
				{
					int i = y * cols + x;
					if (!mask[i] || dist[i] == 0) continue;
					int d = dist[i];
					if (x < cols - 1 && mask[i + 1]) d = Math.Min(d, dist[i + 1] + 1);
					if (y < rows - 1 && mask[i + cols]) d = Math.Min(d, dist[i + cols] + 1);
					dist[i] = d;
				}
			}

			var argb = new int[n];
			interior = new bool[n];
			for (int y = 0; y < rows; y++)
			{
				for (int x = 0; x < cols; x++)
				{
					int i = y * cols + x;
					if (!mask[i]) continue;
					int e = dist[i];
					float u = (x + 0.5f) / cols, v = (y + 0.5f) / rows;
					float diag = (u - 0.5f) + (v - 0.5f);
					int c;
					if (e == 0) c = pal[0];
					else if (e <= 2) c = diag < 0f ? pal[3] : pal[1];
					else if (e == 3 && u + v < 0.55f) c = pal[4];
					else c = pal[2];
					if (frameOnly && e >= 3)
					{
						interior[i] = true;
						continue;
					}
					argb[i] = Opaque(c);
				}
			}

			if (frameOnly)
				return argb;

			bool darkInk = Luma(pal[2]) > 0.78f;
			int ink = darkInk ? pal[0] : pal[5];
			int shadow = darkInk ? pal[4] : pal[6];
			void Block(int x0, int y0, int sz, int rgb)
			{
				for (int yy = y0; yy < y0 + sz; yy++)
					for (int xx = x0; xx < x0 + sz; xx++)
						if (xx >= 0 && yy >= 0 && xx < cols && yy < rows && mask[yy * cols + xx])
							argb[yy * cols + xx] = Opaque(rgb);
			}

			if (pause)
			{
				int bw = Math.Max(2, Math.Min(cols, rows) / 7), bh = Math.Max(5, (int)(rows * 0.45f)), gap = Math.Max(2, bw);
				int left = cols / 2 - gap / 2 - bw, right = cols / 2 + (gap + 1) / 2, top = (rows - bh) / 2;
				for (int pass = 0; pass < 2; pass++)
				{
					int off = pass == 0 ? 1 : 0, rgb = pass == 0 ? shadow : ink;
					for (int yy = 0; yy < bh; yy++)
						for (int xx = 0; xx < bw; xx++)
						{
							Block(left + xx + off, top + yy + off, 1, rgb);
							Block(right + xx + off, top + yy + off, 1, rgb);
						}
				}
			}
			else if (!string.IsNullOrEmpty(label))
			{
				int len = label.Length;
				int g = 0;
				for (int s = 3; s >= 1; s--)
				{
					if (7 * s <= rows * 0.6f && (len * 6 - 1) * s <= cols * 0.82f)
					{
						g = s;
						break;
					}
				}
				if (g == 0 && len * 6 - 1 <= cols - 4 && rows >= 11)
					g = 1;
				if (g > 0)
				{
					int gw = (len * 6 - 1) * g, gh = 7 * g;
					int x0 = (cols - gw) / 2, y0 = (rows - gh) / 2;
					for (int pass = 0; pass < 2; pass++)
					{
						int off = pass == 0 ? 1 : 0, rgb = pass == 0 ? shadow : ink;
						for (int k = 0; k < len; k++)
						{
							if (!Glyphs.TryGetValue(label[k], out string[]? rowsG))
								continue;
							for (int gy = 0; gy < 7; gy++)
								for (int gx = 0; gx < 5; gx++)
									if (rowsG[gy][gx] == '1')
										Block(x0 + (k * 6 + gx) * g + off, y0 + gy * g + off, g, rgb);
						}
					}
				}
			}
			return argb;
		}

		private static int Bilerp(int[] s, int w, int h, float fx, float fy)
		{
			int x0 = (int)MathF.Floor(fx), y0 = (int)MathF.Floor(fy);
			float tx = fx - x0, ty = fy - y0;
			int x1 = Math.Clamp(x0 + 1, 0, w - 1), y1 = Math.Clamp(y0 + 1, 0, h - 1);
			x0 = Math.Clamp(x0, 0, w - 1);
			y0 = Math.Clamp(y0, 0, h - 1);
			int c00 = s[y0 * w + x0], c10 = s[y0 * w + x1], c01 = s[y1 * w + x0], c11 = s[y1 * w + x1];
			int res = 0;
			for (int sh = 0; sh <= 24; sh += 8)
			{
				float a = ((c00 >> sh) & 255) * (1f - tx) + ((c10 >> sh) & 255) * tx;
				float b = ((c01 >> sh) & 255) * (1f - tx) + ((c11 >> sh) & 255) * tx;
				int val = (int)Math.Round(a * (1f - ty) + b * ty);
				res |= (Math.Clamp(val, 0, 255) & 255) << sh;
			}
			return res;
		}

		// Junta a moldura pixel art com uma foto (recorte "cover") dentro do miolo. Retorna (cols*cell) x (rows*cell) em ARGB.
		public static int[] Compose(int cols, int rows, int cell, int[] frame, bool[] interior, int[] photo, int pw, int ph, int baseRgb)
		{
			int W = cols * cell, H = rows * cell;
			var res = new int[W * H];
			float sa = pw / (float)ph, ta = W / (float)H;
			float cu0 = 0f, cv0 = 0f, cuS = 1f, cvS = 1f;
			if (sa > ta)
			{
				cuS = ta / sa;
				cu0 = (1f - cuS) / 2f;
			}
			else
			{
				cvS = sa / ta;
				cv0 = (1f - cvS) / 2f;
			}
			int br = (baseRgb >> 16) & 255, bg = (baseRgb >> 8) & 255, bb = baseRgb & 255;
			for (int y = 0; y < H; y++)
			{
				for (int x = 0; x < W; x++)
				{
					int ci = (y / cell) * cols + (x / cell);
					if (!interior[ci])
					{
						res[y * W + x] = frame[ci];
						continue;
					}
					int p = Bilerp(photo, pw, ph, (cu0 + (x + 0.5f) / W * cuS) * pw - 0.5f, (cv0 + (y + 0.5f) / H * cvS) * ph - 0.5f);
					float a = ((p >> 24) & 255) / 255f;
					int r = (int)Math.Round(((p >> 16) & 255) * a + br * (1f - a));
					int g = (int)Math.Round(((p >> 8) & 255) * a + bg * (1f - a));
					int b = (int)Math.Round((p & 255) * a + bb * (1f - a));
					res[y * W + x] = Opaque((r << 16) | (g << 8) | b);
				}
			}
			return res;
		}

		// Média dos pixels da foto dentro de um retângulo (reduz sem serrilhado ao diminuir a imagem).
		private static int BoxAverage(int[] s, int w, int h, float cx, float cy, float fw, float fh)
		{
			int x0 = Math.Clamp((int)MathF.Floor(cx - fw / 2f), 0, w - 1), x1 = Math.Clamp((int)MathF.Ceiling(cx + fw / 2f), x0 + 1, w);
			int y0 = Math.Clamp((int)MathF.Floor(cy - fh / 2f), 0, h - 1), y1 = Math.Clamp((int)MathF.Ceiling(cy + fh / 2f), y0 + 1, h);
			long sa = 0, sr = 0, sg = 0, sb = 0;
			int n = 0;
			for (int y = y0; y < y1; y++)
			{
				for (int x = x0; x < x1; x++)
				{
					int c = s[y * w + x];
					int a = (c >> 24) & 255;
					sa += a;
					sr += ((c >> 16) & 255) * a;
					sg += ((c >> 8) & 255) * a;
					sb += (c & 255) * a;
					n++;
				}
			}
			if (n == 0 || sa == 0)
				return 0;
			int r = (int)(sr / sa), g = (int)(sg / sa), b = (int)(sb / sa), al = (int)(sa / n);
			return (al << 24) | (r << 16) | (g << 8) | b;
		}

		// Foto original no formato do botão: sem moldura, brilho nem pixelização; só a borda é suavizada.
		// shape: 0 = elipse, 1/2 = retângulo de cantos levemente arredondados. Retorna ARGB (alfa reto).
		public static int[] PhotoSkin(int w, int h, int shape, int[] photo, int pw, int ph)
		{
			var res = new int[w * h];
			float sa = pw / (float)ph, ta = w / (float)h;
			float cu0 = 0f, cv0 = 0f, cuS = 1f, cvS = 1f;
			if (sa > ta)
			{
				cuS = ta / sa;
				cu0 = (1f - cuS) / 2f;
			}
			else
			{
				cvS = sa / ta;
				cv0 = (1f - cvS) / 2f;
			}
			float fw = cuS * pw / w, fh = cvS * ph / h;
			float hw = w / 2f, hh = h / 2f;
			float minHalf = Math.Min(hw, hh);
			float rad = Math.Min(w, h) * 0.12f;
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					float px = x + 0.5f - hw, py = y + 0.5f - hh;
					float d;
					if (shape == 0)
					{
						float r = MathF.Sqrt((px / hw) * (px / hw) + (py / hh) * (py / hh));
						d = (1f - r) * minHalf;
					}
					else
					{
						float qx = MathF.Abs(px) - (hw - rad), qy = MathF.Abs(py) - (hh - rad);
						float ox = MathF.Max(qx, 0f), oy = MathF.Max(qy, 0f);
						d = -(MathF.Sqrt(ox * ox + oy * oy) + MathF.Min(MathF.Max(qx, qy), 0f) - rad);
					}
					float cov = Math.Clamp(d + 0.5f, 0f, 1f);
					if (cov <= 0f)
						continue;
					float sx = (cu0 + (x + 0.5f) / w * cuS) * pw, sy = (cv0 + (y + 0.5f) / h * cvS) * ph;
					int c = fw > 1.5f || fh > 1.5f ? BoxAverage(photo, pw, ph, sx, sy, fw, fh) : Bilerp(photo, pw, ph, sx - 0.5f, sy - 0.5f);
					int a = (int)Math.Round(((c >> 24) & 255) * cov);
					if (a <= 0)
						continue;
					res[y * w + x] = (a << 24) | (c & 0xFFFFFF);
				}
			}
			return res;
		}
	}
}
