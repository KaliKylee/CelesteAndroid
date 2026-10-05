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
	/// <summary>
	/// Controles na tela. O FNA só consulta o primeiro dos dispositivos de toque do SDL, então o
	/// TouchPanel não serve: aqui os dedos são lidos direto do SDL3 (todos os dispositivos), viram um
	/// GamePadState sintético (devolvido no lugar de GamePad.GetState) e o overlay é desenhado por cima
	/// do jogo no final do RenderCore. Se um controle físico estiver conectado, ele tem prioridade e
	/// o overlay some.
	/// </summary>
	public static class TouchControls
	{
		// ---- Ajustes (valores relativos à altura da tela, para valer em qualquer aparelho) ----
		public static float Opacity = 0.45f;          // 0..1 (analógico)
		/// <summary>Opacidade dos botões pixel art, derivada de Opacity (a opção da tela inicial): o padrão de 45% vira 90%, 0% some e 100% fica sólido.</summary>
		public static float ButtonOpacity => Opacity <= 0.45f ? Opacity / 0.45f * 0.9f : Math.Min(1f, 0.9f + (Opacity - 0.45f) / 0.55f * 0.1f);
		public static float ButtonSize = 0.17f;       // diâmetro dos botões
		public static float StickRadius = 0.13f;      // alcance do analógico
		public static float StickDeadZone = 0.18f;    // fração do alcance ignorada
		public static float StickGrabRadius = 2.6f;   // área que "pega" o analógico (zona externa invisível), em múltiplos do alcance
		public static float StickX = 0.30f;           // posição fixa do centro (x), em múltiplos da altura da tela
		public static float StickBottom = 0.28f;      // distância do centro até a borda de baixo, em múltiplos da altura
		public static bool Enabled = true;

		// Opções vindas da tela inicial (menu "Opções" e editor de controles), via HostConfig.
		private static bool showFps;

		// Posição do contador de FPS (salva pelo editor em touch_layout.txt, linha "fps=canto,mx,my,escala").
		// Canto: 0 = sup. esquerdo, 1 = sup. direito, 2 = inf. esquerdo, 3 = inf. direito (bit 0 = direita, bit 1 = baixo).
		// mx/my = distância até a borda, em fração da largura/altura da tela; negativo = margem padrão.
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
			public Vector2 Pos; // pixels do backbuffer
		}

		private static readonly List<Finger> fingers = new();
		private static readonly bool[] pressed = new bool[5];
		private static readonly Stopwatch clock = Stopwatch.StartNew();
		private static long lastPollMs = -100;

		private static bool stickActive;
		private static long stickKey;
		private static Vector2 stickValue; // -1..1, Y para cima positivo (convenção do gamepad)
		private static Vector2 stickKnob;  // deslocamento do botão interno em pixels (tela), limitado ao alcance; só visual

		private static int screenW = 1920, screenH = 1080;
		private static bool realPadConnected;

		// ---- SDL3 ----
		[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr SDL_GetTouchDevices(out int count);

		[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr SDL_GetTouchFingers(ulong touchId, out int count);

		[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_free(IntPtr mem);

		/// <summary>Chamado a cada GetState; refaz a leitura no máximo 1x por ~4 ms.</summary>
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
						// SDL_Finger { SDL_FingerID id (u64); float x, y, pressure; } com x/y em 0..1.
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
		}

		// ---- Layout: padrão + personalização salva pelo editor (ControlsEditorActivity) ----
		// Índices 0..4 = Btn (Jump, Dash, Grab, Pause, Tab); 5 = analógico.
		private const int StickIdx = 5;
		private static readonly bool[] custom = new bool[6];
		private static readonly Vector2[] customPos = new Vector2[6]; // centro, em frações da largura/altura da tela
		private static readonly float[] customScale = { 1f, 1f, 1f, 1f, 1f, 1f };
		private static bool layoutLoaded;

		/// <summary>Lê touch_layout.txt (linhas "nome=x,y,escala"); sem arquivo, vale o layout padrão.</summary>
		private static void LoadLayout()
		{
			layoutLoaded = true;
			try
			{
				string? path = HostConfig.TouchLayoutPath;
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
				// Arquivo ilegível: segue com o layout padrão.
			}
		}

		private static float BaseUnit => screenH * ButtonSize;
		private static float Unit(Btn b) => BaseUnit * customScale[(int)b];
		private static float StickRange => screenH * StickRadius * customScale[StickIdx];

		// Analógico fixo: o centro nunca se move durante o jogo, só o botão interno acompanha o dedo.
		private static Vector2 StickBase => custom[StickIdx]
			? new Vector2(customPos[StickIdx].X * screenW, customPos[StickIdx].Y * screenH)
			: new Vector2(screenH * StickX, screenH * (1f - StickBottom));

		// Padrão: manter em sincronia com ControlsCanvas.SetDefaults (Celeste.Android).
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

		private static float Radius(Btn b) => (b == Btn.Pause ? 0.32f : b == Btn.Tab ? 0.4f : 0.5f) * Unit(b);

		private static bool InsideButton(Vector2 p, out Btn which)
		{
			foreach (Btn b in Enum.GetValues(typeof(Btn)))
			{
				// 25% de folga: o dedo nunca acerta exatamente no meio.
				if (Vector2.Distance(p, Center(b)) <= Radius(b) * 1.25f)
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

			// Solta o analógico se o dedo que o controlava sumiu.
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
			float len = Math.Min(dist, range); // passou do alcance: o valor trava no máximo, a base não sai do lugar
			Vector2 dir = delta / Math.Max(dist, 0.0001f);
			// Visual: o botão interno segue o dedo de verdade (sem a zona morta), preso dentro do aro.
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

		// ---- Estado de gamepad sintético ----
		private static GamePadState Synthesize()
		{
			Vector2 s = stickValue;
			List<Buttons> down = new(8);
			if (pressed[(int)Btn.Jump]) down.Add(Buttons.A);
			// Dash manda X e B: o Celeste usa B para dash, para falar com NPCs e para voltar nos menus.
			if (pressed[(int)Btn.Dash]) { down.Add(Buttons.X); down.Add(Buttons.B); }
			if (pressed[(int)Btn.Pause]) down.Add(Buttons.Start);
			// Direcional digital a partir do analógico: os menus do Celeste navegam pelo D-pad/analógico.
			if (s.Y > 0.5f) down.Add(Buttons.DPadUp);
			if (s.Y < -0.5f) down.Add(Buttons.DPadDown);
			if (s.X < -0.5f) down.Add(Buttons.DPadLeft);
			if (s.X > 0.5f) down.Add(Buttons.DPadRight);
			// Tab (diário): no Celeste o Journal fica no gatilho esquerdo do controle (e na tecla Tab no teclado).
			return new GamePadState(s, Vector2.Zero, pressed[(int)Btn.Tab] ? 1f : 0f, pressed[(int)Btn.Grab] ? 1f : 0f, down.ToArray());
		}

		// ---- Chamado pelos shims ----
		public static GamePadState GetState(PlayerIndex index, Func<GamePadState> real)
		{
			GamePadState realState = real();
			realPadConnected = index == PlayerIndex.One && realState.IsConnected;
			if (index != PlayerIndex.One || realPadConnected || !Enabled)
				return realState;
			Poll();
			return Synthesize();
		}

		// ---- Overlay ----
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
				// 1) Analógico fixo no canto inferior esquerdo + brilho suave atrás dos botões.
				batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null);
				Vector2 baseCenter = StickBase;
				float range = StickRange;
				Vector2 knob = baseCenter + stickKnob;
				float a = stickActive ? Opacity * 1.3f : Opacity;
				DrawCircle(ring!, baseCenter, range * 1.1f, Color.White * a);
				DrawCircle(disc!, knob, range * 0.45f, Color.White * (a * 1.1f));
				foreach (Btn b in Enum.GetValues(typeof(Btn)))
					DrawGlow(b);
				batch.End();

				// 2) Botões pixel art: amostragem por ponto, para os pixels ficarem nítidos.
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

		// ---- Contador de FPS ----
		private static readonly Stopwatch fpsClock = Stopwatch.StartNew();
		private static long fpsWindowStartMs;
		private static int fpsFrames;
		private static int fpsValue;

		private static void DrawFps()
		{
			// Quadros desenhados por segundo, atualizado a cada 500 ms para o número não tremer.
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
			float w = (text.Length * 6f - 1f) * cell, h = 7f * cell; // mesma conta do ControlsCanvas.FpsRect (editor)
			// Sem posição salva: margem padrão de 3 células. Nos cantos da direita/de baixo o texto cresce para dentro da tela.
			float mx = fpsMx >= 0f ? fpsMx * screenW : baseCell * 3f;
			float my = fpsMy >= 0f ? fpsMy * screenH : baseCell * 3f;
			float x = (fpsCorner & 1) == 1 ? screenW - mx - w : mx;
			float y = (fpsCorner & 2) == 2 ? screenH - my - h : my;
			DrawText(text, new Vector2(x, y), cell, Color.White);
		}

		// Fonte 5x7 só para o contador (o glifo 'P' do botão de pausa são duas barras, por isso é separada).
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
			['F'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" },
			['P'] = new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" },
			['S'] = new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" },
		};

		/// <summary>Texto com sombra (legível sobre qualquer cenário); topLeft e cell em pixels.</summary>
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
				x += cell * 6f; // 5 colunas + 1 de espaço (o espaço em branco cai aqui também)
			}
		}

		/// <summary>Retângulo do sprite: o tamanho é sempre múltiplo de 28 px, para cada "pixel" da arte cair num número inteiro de pixels da tela.</summary>
		private static Rectangle SpriteRect(Btn b)
		{
			int n = PixelButtonArt.Size;
			int cell = Math.Max(1, (int)Math.Round(Radius(b) * 2f / n));
			int size = cell * n;
			Vector2 c = Center(b);
			// Apertado: o botão "afunda" um pixel da arte.
			int y = (int)Math.Round(c.Y - size / 2f) + (pressed[(int)b] ? cell : 0);
			return new Rectangle((int)Math.Round(c.X - size / 2f), y, size, size);
		}

		private static void DrawGlow(Btn b)
		{
			int rgb = SpriteOf(b).GlowRgb;
			Color tint = new Color((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
			Rectangle r = SpriteRect(b);
			int g = (int)(r.Width * 1.35f);
			float alpha = ButtonOpacity * (pressed[(int)b] ? 0.75f : 0.55f);
			batch!.Draw(glow!, new Rectangle(r.Center.X - g / 2, r.Center.Y - g / 2, g, g), tint * alpha);
		}

		private static void DrawButton(Btn b)
		{
			float alpha = pressed[(int)b] ? 1f : ButtonOpacity;
			batch!.Draw(sprites[(int)b]!, SpriteRect(b), Color.White * alpha);
		}

		private static void DrawCircle(Texture2D tex, Vector2 center, float radius, Color color)
		{
			batch!.Draw(tex, new Rectangle((int)(center.X - radius), (int)(center.Y - radius), (int)(radius * 2), (int)(radius * 2)), color);
		}

		/// <summary>Monta a textura 28x28 a partir do mapa de caracteres de PixelButtonArt (alfa já pré-multiplicado: tudo é opaco ou transparente).</summary>
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

		/// <summary>Disco branco com borda bem suave (pré-multiplicado), usado como brilho colorido atrás dos botões.</summary>
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

		/// <summary>Círculo com alfa pré-multiplicado (o SpriteBatch padrão usa AlphaBlend pré-multiplicado). ringWidth 0 = disco cheio.</summary>
		private static Texture2D MakeCircle(GraphicsDevice device, int size, float ringWidth)
		{
			Color[] data = new Color[size * size];
			float r = size / 2f;
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
					float alpha = Math.Clamp((1f - d) * r, 0f, 1f); // borda suave de ~1px
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

	/// <summary>
	/// Religa as chamadas do Celeste a GamePad.GetState para o estado com toque.
	/// A leitura real do FNA é feita por reflexão (uma chamada direta seria religada para cá: recursão infinita).
	/// </summary>
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
}
