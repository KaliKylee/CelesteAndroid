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

		// Forma/ícone personalizados (0 = círculo, 1 = quadrado, 2 = retângulo)
		private static readonly int[] btnShape = new int[5];
		private static readonly float[] btnHeight = { 1f, 1f, 1f, 1f, 1f };
		private static readonly string?[] btnIcon = new string?[5];
		private static string iconDir = "";
		private static readonly Texture2D?[] shapeTex = new Texture2D?[5];
		private static readonly bool[] shapeBuilt = new bool[5];

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
			public Color Color;
			public float Opacity; // -1 = geral
			public float X, Y, Scale;
			public string Label;
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
			if (n.StartsWith("NumPad")) return "N" + n.Substring(6);
			return n switch
			{
				"Space" => "SPC", "Enter" => "ENT", "Escape" => "ESC", "Back" => "BS", "Up" => "UP", "Down" => "DN",
				"Left" => "LT", "Right" => "RT", "LeftShift" => "LSH", "RightShift" => "RSH", "LeftControl" => "LCT",
				"RightControl" => "RCT", "LeftAlt" => "LAL", "RightAlt" => "RAL", "PageUp" => "PGU", "PageDown" => "PGD",
				"Home" => "HOM", "Insert" => "INS", "Delete" => "DEL",
				_ => n.StartsWith("Oem") ? "SYM" : n.ToUpperInvariant(),
			};
		}

		private static void LoadCustomButtons(string layoutPath)
		{
			customBtns.Clear();
			try
			{
				string file = Path.Combine(Path.GetDirectoryName(layoutPath) ?? "", "custom_buttons.txt");
				if (!File.Exists(file))
					return;
				foreach (string line in File.ReadAllLines(file))
				{
					string[] kv = line.Split('=');
					if (kv.Length != 2 || !kv[0].Trim().StartsWith("c"))
						continue;
					string[] v = kv[1].Split(',');
					if (v.Length != 6 || customBtns.Count >= 12)
						continue;
					Keys? key = v[0].Trim() != "-" && Enum.TryParse(v[0].Trim(), out Keys k) ? k : null;
					int rgb = 0x4DA3FF;
					if (v[1].Trim().Length == 6)
						int.TryParse(v[1].Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb);
					float F(string t, float d) => float.TryParse(t.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float r) ? r : d;
					float op = F(v[2], -1f);
					customBtns.Add(new CustomBtn
					{
						Key = key,
						Color = new Color((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255),
						Opacity = op < 0f ? -1f : Math.Clamp(op, 0f, 100f) / 100f,
						X = Math.Clamp(F(v[3], 50f), 0f, 100f) / 100f,
						Y = Math.Clamp(F(v[4], 50f), 0f, 100f) / 100f,
						Scale = Math.Clamp(F(v[5], 100f), 40f, 250f) / 100f,
						Label = LabelOf(key),
					});
				}
			}
			catch (Exception)
			{
			}
			customPressed = new bool[customBtns.Count];
		}

		private static float CustomRadius(int i) => 0.5f * BaseUnit * customBtns[i].Scale;

		private static Vector2 CustomCenter(int i) => new Vector2(customBtns[i].X * screenW, customBtns[i].Y * screenH);

		private static bool InsideCustom(Vector2 p, out int which)
		{
			for (int i = customBtns.Count - 1; i >= 0; i--)
			{
				if (Vector2.Distance(p, CustomCenter(i)) <= CustomRadius(i) * 1.15f)
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
			return new GamePadState(s, Vector2.Zero, pressed[(int)Btn.Tab] ? 1f : 0f, pressed[(int)Btn.Grab] ? 1f : 0f, down.ToArray());
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
		private static Texture2D? disc, ring, glow, pixel;
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
				Vector2 knob = baseCenter + stickKnob;
				float a = stickActive ? Opacity * 1.3f : Opacity;
				DrawCircle(ring!, baseCenter, range * 1.1f, Color.White * a);
				DrawCircle(disc!, knob, range * 0.45f, Color.White * (a * 1.1f));
				foreach (Btn b in Enum.GetValues(typeof(Btn)))
					DrawGlow(b);
				DrawCustomButtons();
				batch.End();

				batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
				foreach (Btn b in Enum.GetValues(typeof(Btn)))
					DrawButton(b);
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
				return;
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
			float alpha = pressed[(int)b] ? 1f : OpacityOf(b);
			batch!.Draw(sprites[(int)b]!, SpriteRect(b), (btnColor[(int)b] ?? Color.White) * alpha);
		}

		private static void DrawCustomButtons()
		{
			for (int i = 0; i < customBtns.Count; i++)
			{
				CustomBtn c = customBtns[i];
				bool down = i < customPressed.Length && customPressed[i];
				float baseA = c.Opacity < 0f ? ButtonOpacity : (c.Opacity <= 0.45f ? c.Opacity / 0.45f * 0.9f : Math.Min(1f, 0.9f + (c.Opacity - 0.45f) / 0.55f * 0.1f));
				float a = down ? 1f : baseA;
				Vector2 center = CustomCenter(i);
				float r = CustomRadius(i) * (down ? 0.94f : 1f);
				DrawCircle(disc!, center, r, c.Color * a);
				DrawCircle(ring!, center, r, Color.White * (a * 0.85f));
				if (c.Label.Length == 0)
					continue;
				float cell = Math.Max(1f, Math.Min(r * 2f * 0.62f / (c.Label.Length * 6f - 1f), r * 2f * 0.1f));
				float w = (c.Label.Length * 6f - 1f) * cell, h = 7f * cell;
				float lum = (0.299f * c.Color.R + 0.587f * c.Color.G + 0.114f * c.Color.B) / 255f;
				DrawText(c.Label, new Vector2(center.X - w / 2f, center.Y - h / 2f), cell, (lum > 0.6f ? Color.Black : Color.White) * Math.Max(a, 0.35f));
			}
		}

		private static string? ShapeLabel(Btn b) => b switch { Btn.Jump => "A", Btn.Dash => "X", Btn.Grab => "G", Btn.Tab => "TAB", _ => null };

		private static void DrawShaped(Btn b)
		{
			Texture2D? tex = ShapeTexture(b);
			if (tex == null)
				return;
			int i = (int)b;
			bool down = pressed[i];
			float alpha = down ? 1f : OpacityOf(b);
			Vector2 c = Center(b);
			float k = down ? 0.94f : 1f;
			float w = BtnW(b) * k, h = BtnH(b) * k;
			batch!.Draw(tex, new Rectangle((int)Math.Round(c.X - w / 2f), (int)Math.Round(c.Y - h / 2f), (int)Math.Round(w), (int)Math.Round(h)), Color.White * alpha);
			if (btnIcon[i] != null)
				return;

			int rgb = SpriteOf(b).GlowRgb;
			Color fill = btnColor[i] ?? new Color((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
			float lum = (0.299f * fill.R + 0.587f * fill.G + 0.114f * fill.B) / 255f;
			Color ink = (lum > 0.6f ? Color.Black : Color.White) * Math.Max(alpha, 0.35f);
			float m = Math.Min(w, h);
			if (b == Btn.Pause)
			{
				float bw = Math.Max(2f, m * 0.12f), bh = m * 0.42f, gap = m * 0.14f;
				batch.Draw(pixel!, new Rectangle((int)(c.X - gap / 2f - bw), (int)(c.Y - bh / 2f), (int)bw, (int)bh), ink);
				batch.Draw(pixel!, new Rectangle((int)(c.X + gap / 2f), (int)(c.Y - bh / 2f), (int)bw, (int)bh), ink);
				return;
			}
			string label = ShapeLabel(b)!;
			float cell = Math.Max(1f, Math.Min(m * 0.5f / 7f, w * 0.7f / (label.Length * 6f - 1f)));
			float lw = (label.Length * 6f - 1f) * cell, lh = 7f * cell;
			DrawText(label, new Vector2(c.X - lw / 2f, c.Y - lh / 2f), cell, ink);
		}

		private static Texture2D? ShapeTexture(Btn b)
		{
			int i = (int)b;
			if (!shapeBuilt[i])
			{
				shapeBuilt[i] = true;
				try
				{
					shapeTex[i] = BuildShapeTexture(batch!.GraphicsDevice, b);
				}
				catch (Exception)
				{
					shapeTex[i] = null;
				}
			}
			return shapeTex[i];
		}

		private static Vector4 Sample(Color[] s, int w, int h, float fx, float fy)
		{
			int x0 = (int)MathF.Floor(fx), y0 = (int)MathF.Floor(fy);
			float tx = fx - x0, ty = fy - y0;
			int x1 = Math.Clamp(x0 + 1, 0, w - 1), y1 = Math.Clamp(y0 + 1, 0, h - 1);
			x0 = Math.Clamp(x0, 0, w - 1);
			y0 = Math.Clamp(y0, 0, h - 1);
			Vector4 top = Vector4.Lerp(s[y0 * w + x0].ToVector4(), s[y0 * w + x1].ToVector4(), tx);
			Vector4 bottom = Vector4.Lerp(s[y1 * w + x0].ToVector4(), s[y1 * w + x1].ToVector4(), tx);
			return Vector4.Lerp(top, bottom, ty);
		}

		private static Texture2D BuildShapeTexture(GraphicsDevice device, Btn b)
		{
			int i = (int)b;
			int tw = Math.Clamp((int)Math.Round(BtnW(b)), 8, 1024);
			int th = Math.Clamp((int)Math.Round(BtnH(b)), 8, 1024);
			Color[]? src = null;
			int sw = 0, sh = 0;
			if (btnIcon[i] != null)
			{
				try
				{
					using FileStream fs = File.OpenRead(Path.Combine(iconDir, btnIcon[i]!));
					using Texture2D t = Texture2D.FromStream(device, fs);
					sw = t.Width;
					sh = t.Height;
					src = new Color[sw * sh];
					t.GetData(src);
				}
				catch (Exception)
				{
					src = null;
					btnIcon[i] = null;
				}
			}

			int rgb = SpriteOf(b).GlowRgb;
			Color fillColor = btnColor[i] ?? new Color((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
			Vector4 fill = fillColor.ToVector4();
			Vector4 border = src != null ? Vector4.One : Vector4.Lerp(fill, Vector4.One, 0.65f);

			Color[] data = new Color[tw * th];
			float hw = tw / 2f, hh = th / 2f;
			float minHalf = Math.Min(hw, hh);
			float bw = Math.Max(2f, Math.Min(tw, th) * 0.045f);
			float rad = Math.Min(tw, th) * 0.2f;
			int shape = btnShape[i];
			float cu0 = 0f, cv0 = 0f, cuS = 1f, cvS = 1f;
			if (src != null)
			{
				float sa = sw / (float)sh, ta = tw / (float)th;
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
			}

			for (int y = 0; y < th; y++)
			{
				for (int x = 0; x < tw; x++)
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
					float edge = Math.Clamp(d, 0f, 1f);
					if (edge <= 0f)
						continue;
					Vector4 col = fill;
					if (src != null)
						col = Sample(src, sw, sh, (cu0 + (x + 0.5f) / tw * cuS) * sw - 0.5f, (cv0 + (y + 0.5f) / th * cvS) * sh - 0.5f);
					col = Vector4.Lerp(col, border, Math.Clamp(bw - d + 0.5f, 0f, 1f));
					float a = col.W * edge;
					data[y * tw + x] = new Color(col.X * a, col.Y * a, col.Z * a, a);
				}
			}

			Texture2D tex = new(device, tw, th);
			tex.SetData(data);
			return tex;
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
