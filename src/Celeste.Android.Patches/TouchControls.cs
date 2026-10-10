using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoMod;

namespace CelesteAndroid
{
	public static class TouchControls
	{
		public static float Opacity = 0.45f;
		public static float ButtonOpacity => Opacity <= 0.45f ? Opacity / 0.45f * 0.9f : Math.Min(1f, 0.9f + (Opacity - 0.45f) / 0.55f * 0.1f);
		public static float ButtonSize = 0.17f;
		public static float StickRadius = 0.13f;
		public static float StickDeadZone = 0.18f;
		public static float StickGrabRadius = 2.6f;
		public static float StickX = 0.30f;
		public static float StickBottom = 0.28f;
		public static bool Enabled = true;

		private static bool showFps;

		private static int fpsCorner;
		private static float fpsMx = -1f, fpsMy = -1f, fpsScale = 1f;

		static TouchControls()
		{
			Opacity = (HostConfig.TouchOpacityPercent ?? 45) / 100f;
			Enabled = !HostConfig.HideTouch;
			showFps = HostConfig.ShowFps;
		}

		private enum Btn { Jump, Dash, Grab, Pause, Tab }

		private struct Finger
		{
			public long Key;
			public Vector2 Pos;
		}

		private static readonly List<Finger> fingers = new();
		private static readonly bool[] pressed = new bool[5];
		private static readonly Stopwatch clock = Stopwatch.StartNew();
		private static long lastPollMs = -100;

		private static bool dpadMode; // true = setas direcionais em vez do analógico (layout: dpad=1)
		private static bool stickActive;
		private static long stickKey;
		private static Vector2 stickValue;
		private static Vector2 stickKnob;

		private static int screenW = 1920, screenH = 1080;
		private static bool realPadConnected;

		[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr SDL_GetTouchDevices(out int count);

		[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr SDL_GetTouchFingers(ulong touchId, out int count);

		[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_free(IntPtr mem);

		private static readonly Keys?[] btnKey = new Keys?[5];
		private static readonly Color?[] btnColor = new Color?[5];
		private static readonly float[] btnOpacity = { -1f, -1f, -1f, -1f, -1f };

		// Forma/ícone dos botões padrão (0 = círculo, 1 = quadrado, 2 = retângulo)
		private static readonly int[] btnShape = new int[5];
		private static readonly float[] btnHeight = { 1f, 1f, 1f, 1f, 1f };
		private static readonly string?[] btnIcon = new string?[5];
		private static string iconDir = "";

		private sealed class SkinTex
		{
			public Texture2D Tex = null!;
			public int W, H, Cell;
			public bool Photo;
		}

		private static readonly SkinTex?[] shapeSkin = new SkinTex?[5];
		private static readonly bool[] shapeBuilt = new bool[5];
		private static SkinTex?[] customSkin = new SkinTex?[0];
		private static bool[] customBuilt = new bool[0];

		private static bool Shaped(Btn b) => btnShape[(int)b] != 0 || btnIcon[(int)b] != null;

		private static bool ValidIconName(string n)
		{
			if (n.Length < 5 || n.Length > 48 || !n.EndsWith(".png", StringComparison.Ordinal))
				return false;
			for (int i = 0; i < n.Length - 4; i++)
				if (!char.IsLetterOrDigit(n[i]) && n[i] != '_')
					return false;
			return true;
		}

		private static void ParseShape(string key, string value)
		{
			int idx = key switch { "jump" => 0, "dash" => 1, "grab" => 2, "pause" => 3, "tab" => 4, _ => -1 };
			string[] v = value.Split(',');
			if (idx < 0 || v.Length != 3)
				return;
			if (!int.TryParse(v[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int shape)
				|| !float.TryParse(v[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float hs))
				return;
			btnShape[idx] = Math.Clamp(shape, 0, 2);
			btnHeight[idx] = Math.Clamp(hs, 0.4f, 2.5f);
			string icon = v[2].Trim();
			btnIcon[idx] = icon != "-" && ValidIconName(icon) && File.Exists(Path.Combine(iconDir, icon)) ? icon : null;
		}

		private struct CustomBtn
		{
			public Keys? Key;
			public Buttons? Pad; // botão de gamepad emulado (padrão); Key = tecla do teclado
			public Color Color;
			public float Opacity; // -1 = geral
			public float X, Y, Scale;
			public string Label;
			public int Rgb;
			public int Shape;
			public float HScale;
			public string? Icon;
		}

		private static readonly List<CustomBtn> customBtns = new();
		private static bool[] customPressed = new bool[0];

		private static readonly MethodInfo? realKbGetState = typeof(Keyboard).GetMethod("GetState", Type.EmptyTypes);

		private static KeyboardState RealKeyboard() => (KeyboardState)realKbGetState!.Invoke(null, null)!;

		private static string LabelOf(Keys? key)
		{
			if (!key.HasValue)
				return "";
			string n = key.Value.ToString();
			if (n.Length == 2 && n[0] == 'D' && char.IsDigit(n[1])) return n.Substring(1);
			if (n.StartsWith("NumPad", StringComparison.Ordinal)) return "N" + n.Substring(6);
			return n switch
			{
				"Space" => "SPC", "Enter" => "ENT", "Escape" => "ESC", "Back" => "BS", "Up" => "UP", "Down" => "DN",
				"Left" => "LT", "Right" => "RT", "LeftShift" => "LSH", "RightShift" => "RSH", "LeftControl" => "LCT",
				"RightControl" => "RCT", "LeftAlt" => "LAL", "RightAlt" => "RAL", "PageUp" => "PGU", "PageDown" => "PGD",
				"Home" => "HOM", "Insert" => "INS", "Delete" => "DEL",
				_ => n.StartsWith("Oem", StringComparison.Ordinal) ? "SYM" : n.ToUpperInvariant(),
			};
		}

		private static void LoadCustomButtons(string layoutPath)
		{
			customBtns.Clear();
			iconDir = Path.Combine(Path.GetDirectoryName(layoutPath) ?? "", "touch_icons");
			try
			{
				string file = Path.Combine(Path.GetDirectoryName(layoutPath) ?? "", "custom_buttons.txt");
				if (!File.Exists(file))
					return;
				foreach (string line in File.ReadAllLines(file))
				{
					string[] kv = line.Split('=');
					if (kv.Length != 2 || !kv[0].Trim().StartsWith("c", StringComparison.Ordinal))
						continue;
					string[] v = kv[1].Split(',');
					if ((v.Length != 6 && v.Length != 9 && v.Length != 10) || customBtns.Count >= 12)
						continue;
					string action = v[0].Trim();
					Keys? key = null;
					Buttons? pad = null;
					if (action.StartsWith("pad:", StringComparison.Ordinal))
						pad = PadFromId(action.Substring(4));
					else if (action != "-" && Enum.TryParse(action, out Keys k))
						key = k;
					int rgb = 0x4DA3FF;
					if (v[1].Trim().Length == 6)
						int.TryParse(v[1].Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb);
					float F(string t, float d) => float.TryParse(t.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float r) ? r : d;
					float op = F(v[2], -1f);
					float cscale = Math.Clamp(F(v[5], 100f), 40f, 250f) / 100f;
					int cshape = 0;
					float chs = cscale;
					string? cicon = null;
					if (v.Length >= 9)
					{
						cshape = Math.Clamp((int)F(v[6], 0f), 0, 2);
						chs = Math.Clamp(F(v[7], cscale * 100f), 40f, 250f) / 100f;
						string ic = v[8].Trim();
						if (ic != "-" && ValidIconName(ic) && File.Exists(Path.Combine(iconDir, ic)))
							cicon = ic;
					}
					customBtns.Add(new CustomBtn
					{
						Key = key,
						Pad = pad,
						Color = new Color((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255),
						Opacity = op < 0f ? -1f : Math.Clamp(op, 0f, 100f) / 100f,
						X = Math.Clamp(F(v[3], 50f), 0f, 100f) / 100f,
						Y = Math.Clamp(F(v[4], 50f), 0f, 100f) / 100f,
						Scale = cscale,
						Label = (v.Length == 10 ? PixelButtonArt.CleanLabel(v[9]) : null)
							?? (pad.HasValue ? PixelButtonArt.LabelFor(action) : key.HasValue ? PixelButtonArt.LabelFor(key.Value.ToString()) : ""),
						Rgb = rgb & 0xFFFFFF,
						Shape = cshape,
						HScale = chs,
						Icon = cicon,
					});
				}
			}
			catch (Exception)
			{
			}
			customPressed = new bool[customBtns.Count];
			customSkin = new SkinTex?[customBtns.Count];
			customBuilt = new bool[customBtns.Count];
		}

		private static Buttons? PadFromId(string id) => id switch
		{
			"A" => Buttons.A, "B" => Buttons.B, "X" => Buttons.X, "Y" => Buttons.Y,
			"LB" => Buttons.LeftShoulder, "RB" => Buttons.RightShoulder,
			"LT" => Buttons.LeftTrigger, "RT" => Buttons.RightTrigger,
			"L3" => Buttons.LeftStick, "R3" => Buttons.RightStick,
			"Back" => Buttons.Back, "Start" => Buttons.Start,
			"Up" => Buttons.DPadUp, "Down" => Buttons.DPadDown, "Left" => Buttons.DPadLeft, "Right" => Buttons.DPadRight,
			_ => null,
		};

		private static float CustomW(int i) => BaseUnit * customBtns[i].Scale;

		private static float CustomH(int i) => BaseUnit * (customBtns[i].Shape == 2 ? customBtns[i].HScale : customBtns[i].Scale);

		private static bool HitsCustom(Vector2 p, int i)
		{
			Vector2 c = CustomCenter(i);
			float nx = (p.X - c.X) / (CustomW(i) * 0.5f * 1.15f);
			float ny = (p.Y - c.Y) / (CustomH(i) * 0.5f * 1.15f);
			return customBtns[i].Shape != 0 ? Math.Max(Math.Abs(nx), Math.Abs(ny)) <= 1f : nx * nx + ny * ny <= 1f;
		}

		private static Vector2 CustomCenter(int i) => new Vector2(customBtns[i].X * screenW, customBtns[i].Y * screenH);

		private static bool InsideCustom(Vector2 p, out int which)
		{
			for (int i = customBtns.Count - 1; i >= 0; i--)
			{
				if (HitsCustom(p, i))
				{
					which = i;
					return true;
				}
			}
			which = -1;
			return false;
		}

		public static KeyboardState AugmentKeyboard(KeyboardState real)
		{
			if (!Enabled || realPadConnected || customBtns.Count == 0)
				return real;
			List<Keys>? extra = null;
			for (int i = 0; i < customBtns.Count && i < customPressed.Length; i++)
			{
				if (customPressed[i] && customBtns[i].Key.HasValue)
					(extra ??= new List<Keys>()).Add(customBtns[i].Key!.Value);
			}
			if (extra == null)
				return real;
			extra.AddRange(real.GetPressedKeys());
			return new KeyboardState(extra.ToArray());
		}

		private static void LoadButtonStyle(string layoutPath)
		{
			LoadCustomButtons(layoutPath);
			try
			{
				string file = Path.Combine(Path.GetDirectoryName(layoutPath) ?? "", "button_style.txt");
				if (!File.Exists(file))
					return;
				foreach (string line in File.ReadAllLines(file))
				{
					string[] kv = line.Split('=');
					if (kv.Length != 2)
						continue;
					int idx = kv[0].Trim() switch { "jump" => 0, "dash" => 1, "grab" => 2, "pause" => 3, "tab" => 4, _ => -1 };
					string[] v = kv[1].Split(',');
					if (idx < 0 || v.Length != 3)
						continue;
					btnKey[idx] = Enum.TryParse(v[0].Trim(), out Keys k) && v[0].Trim() != "-" ? k : null;
					if (v[1].Trim().Length == 6 && int.TryParse(v[1].Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
						btnColor[idx] = new Color((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
					else
						btnColor[idx] = null;
					btnOpacity[idx] = int.TryParse(v[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int op) && op >= 0
						? Math.Clamp(op, 0, 100) / 100f : -1f;
				}
			}
			catch (Exception)
			{
			}
		}

		private static float OpacityOf(Btn b)
		{
			float o = btnOpacity[(int)b];
			if (o < 0f)
				return ButtonOpacity;
			return o <= 0.45f ? o / 0.45f * 0.9f : Math.Min(1f, 0.9f + (o - 0.45f) / 0.55f * 0.1f);
		}

		private static void ApplyKeyboard()
		{
			KeyboardState ks = RealKeyboard();
			for (int i = 0; i < btnKey.Length; i++)
			{
				Keys? k = btnKey[i];
				if (k.HasValue && ks.IsKeyDown(k.Value))
					pressed[i] = true;
			}
		}

		private static void Poll()
		{
			if (!layoutLoaded)
				LoadLayout();
			long now = clock.ElapsedMilliseconds;
			if (now - lastPollMs < 4)
				return;
			lastPollMs = now;

			fingers.Clear();
			IntPtr devices = SDL_GetTouchDevices(out int deviceCount);
			if (devices != IntPtr.Zero)
			{
				for (int d = 0; d < deviceCount; d++)
				{
					ulong touchId = (ulong)Marshal.ReadInt64(devices, d * 8);
					IntPtr list = SDL_GetTouchFingers(touchId, out int fingerCount);
					if (list == IntPtr.Zero)
						continue;
					for (int i = 0; i < fingerCount; i++)
					{
						IntPtr f = Marshal.ReadIntPtr(list, i * IntPtr.Size);
						if (f == IntPtr.Zero)
							continue;
						long id = Marshal.ReadInt64(f, 0);
						float x = BitConverter.Int32BitsToSingle(Marshal.ReadInt32(f, 8));
						float y = BitConverter.Int32BitsToSingle(Marshal.ReadInt32(f, 12));
						fingers.Add(new Finger { Key = id * 31 + d, Pos = new Vector2(x * screenW, y * screenH) });
					}
					SDL_free(list);
				}
				SDL_free(devices);
			}

			Resolve();
			ApplyKeyboard();
		}

		private const int StickIdx = 5;
		private static readonly bool[] custom = new bool[6];
		private static readonly Vector2[] customPos = new Vector2[6];
		private static readonly float[] customScale = { 1f, 1f, 1f, 1f, 1f, 1f };
		private static bool layoutLoaded;

		private static void LoadLayout()
		{
			layoutLoaded = true;
			try
			{
				string? path = HostConfig.TouchLayoutPath;
				if (path != null)
				{
					iconDir = Path.Combine(Path.GetDirectoryName(path) ?? "", "touch_icons");
					LoadButtonStyle(path);
				}
				if (path == null || !File.Exists(path))
					return;
				foreach (string line in File.ReadAllLines(path))
				{
					string[] kv = line.Split('=');
					if (kv.Length != 2)
						continue;
					if (kv[0].Trim() == "fps")
					{
						string[] fv = kv[1].Split(',');
						if (fv.Length == 4
							&& int.TryParse(fv[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int corner)
							&& float.TryParse(fv[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float fmx)
							&& float.TryParse(fv[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float fmy)
							&& float.TryParse(fv[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float fsc))
						{
							fpsCorner = Math.Clamp(corner, 0, 3);
							fpsMx = Math.Clamp(fmx, 0f, 0.5f);
							fpsMy = Math.Clamp(fmy, 0f, 0.5f);
							fpsScale = Math.Clamp(fsc, 0.5f, 2f);
						}
						continue;
					}
					if (kv[0].Trim() == "dpad")
					{
						dpadMode = kv[1].Trim() == "1";
						continue;
					}
					string layoutKey = kv[0].Trim();
					if (layoutKey.StartsWith("shape_", StringComparison.Ordinal))
					{
						ParseShape(layoutKey.Substring(6), kv[1]);
						continue;
					}
					int idx = kv[0].Trim() switch { "jump" => 0, "dash" => 1, "grab" => 2, "pause" => 3, "tab" => 4, "stick" => 5, _ => -1 };
					string[] v = kv[1].Split(',');
					if (idx < 0 || v.Length != 3)
						continue;
					if (!float.TryParse(v[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
						|| !float.TryParse(v[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)
						|| !float.TryParse(v[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float sc))
						continue;
					custom[idx] = true;
					customPos[idx] = new Vector2(Math.Clamp(x, 0f, 1f), Math.Clamp(y, 0f, 1f));
					customScale[idx] = Math.Clamp(sc, 0.4f, 2.5f);
				}
			}
			catch (Exception)
			{
			}
		}

		private static float BaseUnit => screenH * ButtonSize;
		private static float Unit(Btn b) => BaseUnit * customScale[(int)b];
		private static float StickRange => screenH * StickRadius * customScale[StickIdx];

		private static Vector2 StickBase => custom[StickIdx]
			? new Vector2(customPos[StickIdx].X * screenW, customPos[StickIdx].Y * screenH)
			: new Vector2(screenH * StickX, screenH * (1f - StickBottom));

		private static Vector2 Center(Btn b)
		{
			if (custom[(int)b])
				return new Vector2(customPos[(int)b].X * screenW, customPos[(int)b].Y * screenH);
			float u = BaseUnit;
			return b switch
			{
				Btn.Jump => new Vector2(screenW - 1.15f * u, screenH - 1.35f * u),
				Btn.Dash => new Vector2(screenW - 2.45f * u, screenH - 0.95f * u),
				Btn.Grab => new Vector2(screenW - 2.15f * u, screenH - 2.35f * u),
				Btn.Pause => new Vector2(screenW - 0.8f * u, 0.8f * u),
				Btn.Tab => new Vector2(screenW - 1.75f * u, 0.8f * u),
				_ => Vector2.Zero,
			};
		}

		private static float BaseDiameter(Btn b) => 2f * (b == Btn.Pause ? 0.32f : b == Btn.Tab ? 0.4f : 0.5f) * BaseUnit;

		private static float BtnW(Btn b) => BaseDiameter(b) * customScale[(int)b];

		private static float BtnH(Btn b) => BaseDiameter(b) * (btnShape[(int)b] == 2 ? btnHeight[(int)b] : customScale[(int)b]);

		private static bool HitsButton(Vector2 p, Btn b)
		{
			Vector2 c = Center(b);
			float nx = (p.X - c.X) / (BtnW(b) * 0.5f * 1.25f);
			float ny = (p.Y - c.Y) / (BtnH(b) * 0.5f * 1.25f);
			return btnShape[(int)b] != 0 ? Math.Max(Math.Abs(nx), Math.Abs(ny)) <= 1f : nx * nx + ny * ny <= 1f;
		}

		private static float Radius(Btn b) => (b == Btn.Pause ? 0.32f : b == Btn.Tab ? 0.4f : 0.5f) * Unit(b);

		private static bool InsideButton(Vector2 p, out Btn which)
		{
			foreach (Btn b in Enum.GetValues(typeof(Btn)))
			{
				if (HitsButton(p, b))
				{
					which = b;
					return true;
				}
			}
			which = default;
			return false;
		}

		private static void Resolve()
		{
			Array.Clear(pressed, 0, pressed.Length);
			Array.Clear(customPressed, 0, customPressed.Length);

			if (stickActive)
			{
				bool found = false;
				foreach (Finger f in fingers)
					found |= f.Key == stickKey;
				if (!found)
				{
					stickActive = false;
					stickValue = Vector2.Zero;
					stickKnob = Vector2.Zero;
				}
			}

			foreach (Finger f in fingers)
			{
				if (stickActive && f.Key == stickKey)
				{
					UpdateStick(f.Pos);
					continue;
				}
				if (InsideButton(f.Pos, out Btn b))
				{
					pressed[(int)b] = true;
				}
				else if (InsideCustom(f.Pos, out int ci))
				{
					customPressed[ci] = true;
				}
				else if (!stickActive && Vector2.Distance(f.Pos, StickBase) <= StickRange * StickGrabRadius)
				{
					stickActive = true;
					stickKey = f.Key;
					UpdateStick(f.Pos);
				}
			}
		}

		private static void UpdateStick(Vector2 pos)
		{
			if (dpadMode)
			{
				UpdateDpad(pos);
				return;
			}
			float range = StickRange;
			Vector2 delta = pos - StickBase;
			float dist = delta.Length();
			float len = Math.Min(dist, range);
			Vector2 dir = delta / Math.Max(dist, 0.0001f);
			stickKnob = dir * len;
			float amount = len / range;
			if (amount < StickDeadZone)
			{
				stickValue = Vector2.Zero;
				return;
			}
			amount = (amount - StickDeadZone) / (1f - StickDeadZone);
			stickValue = new Vector2(dir.X * amount, -dir.Y * amount);
		}

		// Setas direcionais: cada seta é digital (0 ou 1). O dedo pode deslizar entre as setas;
		// perto das diagonais duas setas ficam ativas ao mesmo tempo.
		private static void UpdateDpad(Vector2 pos)
		{
			float range = StickRange;
			Vector2 d = pos - StickBase;
			float t = range * 0.4f;
			float ax = Math.Abs(d.X), ay = Math.Abs(d.Y);
			bool h = ax > t, v = ay > t;
			if (h && v)
			{
				if (ax > ay * 2f) v = false;
				else if (ay > ax * 2f) h = false;
			}
			float x = h ? Math.Sign(d.X) : 0f;
			float y = v ? -Math.Sign(d.Y) : 0f;
			if (h && v)
			{
				x *= 0.7071f;
				y *= 0.7071f;
			}
			stickValue = new Vector2(x, y);
			stickKnob = Vector2.Zero;
		}

		private static GamePadState Synthesize()
		{
			Vector2 s = stickValue;
			List<Buttons> down = new(8);
			if (pressed[(int)Btn.Jump]) down.Add(Buttons.A);
			if (pressed[(int)Btn.Dash]) { down.Add(Buttons.X); down.Add(Buttons.B); }
			if (pressed[(int)Btn.Pause]) down.Add(Buttons.Start);
			if (s.Y > 0.5f) down.Add(Buttons.DPadUp);
			if (s.Y < -0.5f) down.Add(Buttons.DPadDown);
			if (s.X < -0.5f) down.Add(Buttons.DPadLeft);
			if (s.X > 0.5f) down.Add(Buttons.DPadRight);
			float lt = pressed[(int)Btn.Tab] ? 1f : 0f, rt = pressed[(int)Btn.Grab] ? 1f : 0f;
			for (int i = 0; i < customBtns.Count && i < customPressed.Length; i++)
			{
				if (!customPressed[i] || !customBtns[i].Pad.HasValue)
					continue;
				Buttons p = customBtns[i].Pad!.Value;
				if (p == Buttons.LeftTrigger) lt = 1f;
				else if (p == Buttons.RightTrigger) rt = 1f;
				else down.Add(p);
			}
			return new GamePadState(s, Vector2.Zero, lt, rt, down.ToArray());
		}

		public static GamePadState GetState(PlayerIndex index, Func<GamePadState> real)
		{
			GamePadState realState = real();
			realPadConnected = index == PlayerIndex.One && realState.IsConnected;
			if (index != PlayerIndex.One || realPadConnected || !Enabled)
				return realState;
			Poll();
			return Synthesize();
		}

		private static SpriteBatch? batch;
		private static Texture2D? disc, ring, glow, pixel, tri;
		private static readonly Texture2D?[] sprites = new Texture2D?[5];

		private static PixelButtonArt.Sprite SpriteOf(Btn b) => b switch
		{
			Btn.Jump => PixelButtonArt.Jump,
			Btn.Dash => PixelButtonArt.Dash,
			Btn.Grab => PixelButtonArt.Grab,
			Btn.Tab => PixelButtonArt.Tab,
			_ => PixelButtonArt.Pause,
		};

		public static void Draw(GraphicsDevice device)
		{
			PresentationParameters pp = device.PresentationParameters;
			screenW = pp.BackBufferWidth;
			screenH = pp.BackBufferHeight;
			if (!layoutLoaded)
				LoadLayout();
			bool showControls = Enabled && !realPadConnected;
			if (!showControls && !showFps)
				return;

			if (batch == null)
			{
				batch = new SpriteBatch(device);
				disc = MakeCircle(device, 128, 0f);
				ring = MakeCircle(device, 128, 0.12f);
				glow = MakeGlow(device, 128);
				pixel = new Texture2D(device, 1, 1);
				pixel.SetData(new[] { Color.White });
				tri = MakeTriangle(device, 64);
				foreach (Btn b in Enum.GetValues(typeof(Btn)))
					sprites[(int)b] = MakeSprite(device, SpriteOf(b));
			}

			Viewport saved = device.Viewport;
			device.Viewport = new Viewport(0, 0, screenW, screenH);

			if (showControls)
			{
				batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null);
				Vector2 baseCenter = StickBase;
				float range = StickRange;
				if (dpadMode)
					DrawDpad(baseCenter, range);
				else
				{
					Vector2 knob = baseCenter + stickKnob;
					float a = stickActive ? Opacity * 1.3f : Opacity;
					DrawCircle(ring!, baseCenter, range * 1.1f, Color.White * a);
					DrawCircle(disc!, knob, range * 0.45f, Color.White * (a * 1.1f));
				}
				foreach (Btn b in Enum.GetValues(typeof(Btn)))
					DrawGlow(b);
				DrawCustomGlows();
				batch.End();

				batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
				foreach (Btn b in Enum.GetValues(typeof(Btn)))
					DrawButton(b);
				DrawCustomSkins();
				batch.End();
			}

			if (showFps)
			{
				batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
				DrawFps();
				batch.End();
			}

			device.Viewport = saved;
		}

		private static readonly Stopwatch fpsClock = Stopwatch.StartNew();
		private static long fpsWindowStartMs;
		private static int fpsFrames;
		private static int fpsValue;

		private static void DrawFps()
		{
			fpsFrames++;
			long now = fpsClock.ElapsedMilliseconds;
			long elapsed = now - fpsWindowStartMs;
			if (elapsed >= 500)
			{
				fpsValue = (int)Math.Round(fpsFrames * 1000.0 / elapsed);
				fpsFrames = 0;
				fpsWindowStartMs = now;
			}

			float baseCell = Math.Max(2f, screenH * 0.0045f);
			float cell = baseCell * fpsScale;
			string text = $"{fpsValue} FPS";
			float w = (text.Length * 6f - 1f) * cell, h = 7f * cell;
			float mx = fpsMx >= 0f ? fpsMx * screenW : baseCell * 3f;
			float my = fpsMy >= 0f ? fpsMy * screenH : baseCell * 3f;
			float x = (fpsCorner & 1) == 1 ? screenW - mx - w : mx;
			float y = (fpsCorner & 2) == 2 ? screenH - my - h : my;
			DrawText(text, new Vector2(x, y), cell, Color.White);
		}

		private static readonly Dictionary<char, string[]> textGlyphs = new()
		{
			['0'] = new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" },
			['1'] = new[] { "00100", "01100", "00100", "00100", "00100", "00100", "01110" },
			['2'] = new[] { "01110", "10001", "00001", "00010", "00100", "01000", "11111" },
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
			['G'] = new[] { "01110", "10001", "10000", "10111", "10001", "10001", "01111" },
			['H'] = new[] { "10001", "10001", "10001", "11111", "10001", "10001", "10001" },
			['I'] = new[] { "01110", "00100", "00100", "00100", "00100", "00100", "01110" },
			['J'] = new[] { "00111", "00010", "00010", "00010", "00010", "10010", "01100" },
			['K'] = new[] { "10001", "10010", "10100", "11000", "10100", "10010", "10001" },
			['L'] = new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" },
			['M'] = new[] { "10001", "11011", "10101", "10101", "10001", "10001", "10001" },
			['N'] = new[] { "10001", "11001", "10101", "10011", "10001", "10001", "10001" },
			['O'] = new[] { "01110", "10001", "10001", "10001", "10001", "10001", "01110" },
			['Q'] = new[] { "01110", "10001", "10001", "10001", "10101", "10010", "01101" },
			['R'] = new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" },
			['T'] = new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" },
			['U'] = new[] { "10001", "10001", "10001", "10001", "10001", "10001", "01110" },
			['V'] = new[] { "10001", "10001", "10001", "10001", "10001", "01010", "00100" },
			['W'] = new[] { "10001", "10001", "10001", "10101", "10101", "11011", "10001" },
			['X'] = new[] { "10001", "10001", "01010", "00100", "01010", "10001", "10001" },
			['Y'] = new[] { "10001", "10001", "01010", "00100", "00100", "00100", "00100" },
			['Z'] = new[] { "11111", "00001", "00010", "00100", "01000", "10000", "11111" },
			['F'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" },
			['P'] = new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" },
			['S'] = new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" },
		};

		private static void DrawText(string text, Vector2 topLeft, float cell, Color color)
		{
			DrawTextPass(text, topLeft + new Vector2(cell * 0.5f), cell, Color.Black * 0.75f);
			DrawTextPass(text, topLeft, cell, color);
		}

		private static void DrawTextPass(string text, Vector2 topLeft, float cell, Color color)
		{
			float x = topLeft.X;
			foreach (char ch in text)
			{
				if (textGlyphs.TryGetValue(ch, out string[]? rows))
				{
					for (int y = 0; y < rows.Length; y++)
						for (int col = 0; col < rows[y].Length; col++)
							if (rows[y][col] == '1')
								batch!.Draw(pixel!, new Rectangle((int)(x + col * cell), (int)(topLeft.Y + y * cell), (int)Math.Ceiling(cell), (int)Math.Ceiling(cell)), color);
				}
				x += cell * 6f;
			}
		}

		private static Rectangle SpriteRect(Btn b)
		{
			int n = PixelButtonArt.Size;
			int cell = Math.Max(1, (int)Math.Round(Radius(b) * 2f / n));
			int size = cell * n;
			Vector2 c = Center(b);
			int y = (int)Math.Round(c.Y - size / 2f) + (pressed[(int)b] ? cell : 0);
			return new Rectangle((int)Math.Round(c.X - size / 2f), y, size, size);
		}

		private static void DrawGlow(Btn b)
		{
			if (Shaped(b))
			{
				DrawShapedGlow(b);
				return;
			}
			int rgb = SpriteOf(b).GlowRgb;
			Color tint = btnColor[(int)b] ?? new Color((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
			Rectangle r = SpriteRect(b);
			int g = (int)(r.Width * 1.35f);
			float alpha = OpacityOf(b) * (pressed[(int)b] ? 0.75f : 0.55f);
			batch!.Draw(glow!, new Rectangle(r.Center.X - g / 2, r.Center.Y - g / 2, g, g), tint * alpha);
		}

		private static void DrawButton(Btn b)
		{
			if (Shaped(b))
			{
				DrawShaped(b);
				return;
			}
			float alpha = OpacityOf(b); // pressed keeps the user's chosen opacity
			batch!.Draw(sprites[(int)b]!, SpriteRect(b), (btnColor[(int)b] ?? Color.White) * alpha);
		}

		private static string? ShapeLabel(Btn b) => b switch { Btn.Jump => "A", Btn.Dash => "X", Btn.Grab => "G", Btn.Tab => "TAB", _ => null };

		// Foto da galeria exibida como está (recortada na forma do botão), sem moldura nem efeitos.
		private static SkinTex? BuildPhotoTex(GraphicsDevice device, int shape, float w, float h, ref string? iconName)
		{
			if (iconName == null)
				return null;
			try
			{
				using FileStream fs = File.OpenRead(Path.Combine(iconDir, iconName));
				using Texture2D src = Texture2D.FromStream(device, fs);
				int pw = src.Width, ph = src.Height;
				Color[] px = new Color[pw * ph];
				src.GetData(px);
				int[] photo = new int[px.Length];
				for (int k = 0; k < px.Length; k++)
					photo[k] = (px[k].A << 24) | (px[k].R << 16) | (px[k].G << 8) | px[k].B;

				int tw = Math.Clamp((int)Math.Round(w), 8, 1024), th = Math.Clamp((int)Math.Round(h), 8, 1024);
				int[] argb = PixelButtonArt.PhotoSkin(tw, th, shape, photo, pw, ph);
				Color[] data = new Color[tw * th];
				for (int k = 0; k < data.Length; k++)
				{
					int c = argb[k];
					int a = (c >> 24) & 255;
					data[k] = a == 0 ? Color.Transparent : new Color((((c >> 16) & 255) * a) / 255, (((c >> 8) & 255) * a) / 255, ((c & 255) * a) / 255, a);
				}
				Texture2D tex = new(device, tw, th);
				tex.SetData(data);
				return new SkinTex { Tex = tex, W = tw, H = th, Cell = 1, Photo = true };
			}
			catch (Exception)
			{
				iconName = null;
				return null;
			}
		}

		// Monta a textura pixel art do botão (e, se houver, a foto da galeria dentro da moldura).
		private static SkinTex BuildSkinTex(GraphicsDevice device, int[] pal, int shape, float w, float h, string? label, bool pause, ref string? iconName)
		{
			if (iconName != null)
			{
				SkinTex? photoTex = BuildPhotoTex(device, shape, w, h, ref iconName);
				if (photoTex != null)
					return photoTex;
			}

			PixelButtonArt.Snap(w, h, out int cols, out int rows, out int cell);
			int[]? photo = null;
			int pw = 0, ph = 0;
			if (iconName != null)
			{
				try
				{
					using FileStream fs = File.OpenRead(Path.Combine(iconDir, iconName));
					using Texture2D src = Texture2D.FromStream(device, fs);
					pw = src.Width;
					ph = src.Height;
					Color[] px = new Color[pw * ph];
					src.GetData(px);
					photo = new int[px.Length];
					for (int k = 0; k < px.Length; k++)
						photo[k] = (px[k].A << 24) | (px[k].R << 16) | (px[k].G << 8) | px[k].B;
				}
				catch (Exception)
				{
					photo = null;
					iconName = null;
				}
			}

			int[] argb = PixelButtonArt.BuildSkin(pal, shape, cols, rows, label, pause, photo != null, out bool[] interior);
			int tw = cols, th = rows;
			if (photo != null)
			{
				argb = PixelButtonArt.Compose(cols, rows, cell, argb, interior, photo, pw, ph, pal[2]);
				tw = cols * cell;
				th = rows * cell;
			}

			Color[] data = new Color[tw * th];
			for (int k = 0; k < data.Length; k++)
			{
				int c = argb[k];
				data[k] = ((c >> 24) & 255) == 0 ? Color.Transparent : new Color((c >> 16) & 255, (c >> 8) & 255, c & 255, 255);
			}
			Texture2D tex = new(device, tw, th);
			tex.SetData(data);
			return new SkinTex { Tex = tex, W = cols * cell, H = rows * cell, Cell = cell };
		}

		private static SkinTex? ShapeSkin(Btn b)
		{
			int i = (int)b;
			if (!shapeBuilt[i])
			{
				shapeBuilt[i] = true;
				try
				{
					PixelButtonArt.Sprite sp = SpriteOf(b);
					Color? tint = btnColor[i];
					int[] pal = tint.HasValue ? PixelButtonArt.PaletteFrom((tint.Value.R << 16) | (tint.Value.G << 8) | tint.Value.B) : sp.Rgb;
					shapeSkin[i] = BuildSkinTex(batch!.GraphicsDevice, pal, btnShape[i], BtnW(b), BtnH(b), ShapeLabel(b), b == Btn.Pause, ref btnIcon[i]);
				}
				catch (Exception)
				{
					shapeSkin[i] = null;
				}
			}
			return shapeSkin[i];
		}

		private static void GlowRect(Vector2 c, SkinTex s, int shape, Color tint, float alpha)
		{
			float k = shape == 0 ? 1.35f : 1.6f;
			int gw = (int)(s.W * k), gh = (int)(s.H * k);
			batch!.Draw(glow!, new Rectangle((int)Math.Round(c.X - gw / 2f), (int)Math.Round(c.Y - gh / 2f), gw, gh), tint * alpha);
		}

		private static void DrawShapedGlow(Btn b)
		{
			SkinTex? s = ShapeSkin(b);
			if (s == null || s.Photo)
				return;
			int i = (int)b;
			int rgb = SpriteOf(b).GlowRgb;
			Color tint = btnColor[i] ?? new Color((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
			GlowRect(Center(b), s, btnShape[i], tint, OpacityOf(b) * (pressed[i] ? 0.75f : 0.55f));
		}

		private static void DrawShaped(Btn b)
		{
			SkinTex? s = ShapeSkin(b);
			if (s == null)
				return;
			int i = (int)b;
			bool down = pressed[i];
			Vector2 c = Center(b);
			Color tint = s.Photo ? (down ? new Color(200, 200, 200, 255) * OpacityOf(b) : Color.White * OpacityOf(b)) : Color.White * OpacityOf(b);
			batch!.Draw(s.Tex, new Rectangle((int)Math.Round(c.X - s.W / 2f), (int)Math.Round(c.Y - s.H / 2f) + (down && !s.Photo ? s.Cell : 0), s.W, s.H), tint);
		}

		private static SkinTex? CustomSkinOf(int i)
		{
			if (i >= customBuilt.Length)
				return null;
			if (!customBuilt[i])
			{
				customBuilt[i] = true;
				try
				{
					CustomBtn c = customBtns[i];
					string? ic = c.Icon;
					customSkin[i] = BuildSkinTex(batch!.GraphicsDevice, PixelButtonArt.PaletteFrom(c.Rgb), c.Shape, CustomW(i), CustomH(i), c.Label.Length > 0 ? c.Label : null, false, ref ic);
				}
				catch (Exception)
				{
					customSkin[i] = null;
				}
			}
			return customSkin[i];
		}

		private static float CustomAlpha(CustomBtn c) => c.Opacity < 0f ? ButtonOpacity : (c.Opacity <= 0.45f ? c.Opacity / 0.45f * 0.9f : Math.Min(1f, 0.9f + (c.Opacity - 0.45f) / 0.55f * 0.1f));

		private static void DrawCustomGlows()
		{
			for (int i = 0; i < customBtns.Count; i++)
			{
				SkinTex? s = CustomSkinOf(i);
				if (s == null || s.Photo)
					continue;
				CustomBtn c = customBtns[i];
				bool down = i < customPressed.Length && customPressed[i];
				GlowRect(CustomCenter(i), s, c.Shape, new Color((c.Rgb >> 16) & 255, (c.Rgb >> 8) & 255, c.Rgb & 255), CustomAlpha(c) * (down ? 0.75f : 0.55f));
			}
		}

		private static void DrawCustomSkins()
		{
			for (int i = 0; i < customBtns.Count; i++)
			{
				SkinTex? s = CustomSkinOf(i);
				if (s == null)
					continue;
				bool down = i < customPressed.Length && customPressed[i];
				Vector2 c = CustomCenter(i);
				Color tint = s.Photo ? (down ? new Color(200, 200, 200, 255) * CustomAlpha(customBtns[i]) : Color.White * CustomAlpha(customBtns[i])) : Color.White * CustomAlpha(customBtns[i]);
				batch!.Draw(s.Tex, new Rectangle((int)Math.Round(c.X - s.W / 2f), (int)Math.Round(c.Y - s.H / 2f) + (down && !s.Photo ? s.Cell : 0), s.W, s.H), tint);
			}
		}

		private static void DrawCircle(Texture2D tex, Vector2 center, float radius, Color color)
		{
			batch!.Draw(tex, new Rectangle((int)(center.X - radius), (int)(center.Y - radius), (int)(radius * 2), (int)(radius * 2)), color);
		}

		private static Texture2D MakeSprite(GraphicsDevice device, PixelButtonArt.Sprite s)
		{
			int n = PixelButtonArt.Size;
			Color[] data = new Color[n * n];
			for (int y = 0; y < n; y++)
			{
				for (int x = 0; x < n; x++)
				{
					int rgb = s.ColorOf(s.Rows[y][x]);
					data[y * n + x] = rgb < 0 ? Color.Transparent : new Color((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255, 255);
				}
			}
			Texture2D tex = new(device, n, n);
			tex.SetData(data);
			return tex;
		}

		private static Texture2D MakeGlow(GraphicsDevice device, int size)
		{
			Color[] data = new Color[size * size];
			float r = size / 2f;
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
					float t = Math.Clamp((1f - d) / 0.30f, 0f, 1f);
					byte v = (byte)(t * t * (3f - 2f * t) * 255f);
					data[y * size + x] = new Color(v, v, v, v);
				}
			}
			Texture2D tex = new(device, size, size);
			tex.SetData(data);
			return tex;
		}

		private static void DrawDpad(Vector2 c, float range)
		{
			float s = range * 0.71f;
			float g = s * 1.05f;
			float a = Opacity;
			Vector2 sv = stickValue;
			// 0 = cima, 1 = direita, 2 = baixo, 3 = esquerda
			bool[] on = { sv.Y > 0.5f, sv.X > 0.5f, sv.Y < -0.5f, sv.X < -0.5f };
			Vector2[] dir = { new(0, -1), new(1, 0), new(0, 1), new(-1, 0) };
			int th = Math.Max(2, (int)Math.Round(s * 0.06f));
			for (int i = 0; i < 4; i++)
			{
				Vector2 p = c + dir[i] * g + (on[i] ? new Vector2(0, s * 0.06f) : Vector2.Zero);
				int x = (int)Math.Round(p.X - s / 2f), y = (int)Math.Round(p.Y - s / 2f), w = (int)Math.Round(s);
				batch!.Draw(pixel!, new Rectangle(x, y, w, w), Color.White * (a * (on[i] ? 0.5f : 0.12f)));
				Color edge = Color.White * a;
				batch.Draw(pixel!, new Rectangle(x, y, w, th), edge);
				batch.Draw(pixel!, new Rectangle(x, y + w - th, w, th), edge);
				batch.Draw(pixel!, new Rectangle(x, y, th, w), edge);
				batch.Draw(pixel!, new Rectangle(x + w - th, y, th, w), edge);
				float ts = s * 0.6f;
				batch.Draw(tri!, new Rectangle((int)Math.Round(p.X), (int)Math.Round(p.Y), (int)Math.Round(ts), (int)Math.Round(ts)), null,
					Color.White * Math.Min(1f, a * 1.1f), i * MathHelper.PiOver2, new Vector2(32f, 32f), SpriteEffects.None, 0f);
			}
		}

		private static Texture2D MakeTriangle(GraphicsDevice device, int size)
		{
			Color[] data = new Color[size * size];
			float top = size * 0.12f, bottom = size * 0.82f, half = size * 0.38f;
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float py = y + 0.5f, px = x + 0.5f;
					float alpha = 0f;
					if (py >= top && py <= bottom)
					{
						float w = (py - top) / (bottom - top) * half;
						alpha = Math.Clamp(w - Math.Abs(px - size / 2f) + 0.5f, 0f, 1f);
					}
					byte v = (byte)(alpha * 255f);
					data[y * size + x] = new Color(v, v, v, v);
				}
			}
			Texture2D tex = new(device, size, size);
			tex.SetData(data);
			return tex;
		}

		private static Texture2D MakeCircle(GraphicsDevice device, int size, float ringWidth)
		{
			Color[] data = new Color[size * size];
			float r = size / 2f;
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
					float alpha = Math.Clamp((1f - d) * r, 0f, 1f);
					if (ringWidth > 0f)
						alpha *= Math.Clamp((d - (1f - ringWidth)) * r, 0f, 1f);
					byte v = (byte)(alpha * 255f);
					data[y * size + x] = new Color(v, v, v, v);
				}
			}
			Texture2D tex = new(device, size, size);
			tex.SetData(data);
			return tex;
		}
	}

	public static class GamePadShim
	{
		private static readonly MethodInfo? realGetState =
			typeof(GamePad).GetMethod("GetState", new[] { typeof(PlayerIndex) });

		private static readonly MethodInfo? realGetStateDeadZone =
			typeof(GamePad).GetMethod("GetState", new[] { typeof(PlayerIndex), typeof(GamePadDeadZone) });

		[MonoModLinkFrom("Microsoft.Xna.Framework.Input.GamePadState Microsoft.Xna.Framework.Input.GamePad::GetState(Microsoft.Xna.Framework.PlayerIndex)")]
		public static GamePadState GetState(PlayerIndex index)
			=> TouchControls.GetState(index, () => (GamePadState)realGetState!.Invoke(null, new object[] { index })!);

		[MonoModLinkFrom("Microsoft.Xna.Framework.Input.GamePadState Microsoft.Xna.Framework.Input.GamePad::GetState(Microsoft.Xna.Framework.PlayerIndex,Microsoft.Xna.Framework.Input.GamePadDeadZone)")]
		public static GamePadState GetState(PlayerIndex index, GamePadDeadZone deadZone)
			=> TouchControls.GetState(index, () => (GamePadState)realGetStateDeadZone!.Invoke(null, new object[] { index, deadZone })!);
	}

	public static class KeyboardShim
	{
		private static readonly MethodInfo? realGetState = typeof(Keyboard).GetMethod("GetState", Type.EmptyTypes);

		[MonoModLinkFrom("Microsoft.Xna.Framework.Input.KeyboardState Microsoft.Xna.Framework.Input.Keyboard::GetState()")]
		public static KeyboardState GetState()
			=> TouchControls.AugmentKeyboard((KeyboardState)realGetState!.Invoke(null, null)!);
	}
}
