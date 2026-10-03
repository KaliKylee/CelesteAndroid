using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		public static float Opacity = 0.45f;          // 0..1
		public static float ButtonSize = 0.17f;       // diâmetro dos botões
		public static float StickRadius = 0.13f;      // alcance do analógico
		public static float StickDeadZone = 0.18f;    // fração do alcance ignorada
		public static float StickGrabRadius = 2.6f;   // área que "pega" o analógico (zona externa invisível), em múltiplos do alcance
		public static float StickX = 0.30f;           // posição fixa do centro (x), em múltiplos da altura da tela
		public static float StickBottom = 0.28f;      // distância do centro até a borda de baixo, em múltiplos da altura
		public static bool Enabled = true;

		private enum Btn { Jump, Dash, Grab, Pause }

		private struct Finger
		{
			public long Key;
			public Vector2 Pos; // pixels do backbuffer
		}

		private static readonly List<Finger> fingers = new();
		private static readonly bool[] pressed = new bool[4];
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

		// ---- Layout ----
		private static float Unit => screenH * ButtonSize;

		// Analógico fixo: o centro nunca se move, só o botão interno acompanha o dedo.
		private static Vector2 StickBase => new Vector2(screenH * StickX, screenH * (1f - StickBottom));

		private static Vector2 Center(Btn b)
		{
			float u = Unit;
			return b switch
			{
				Btn.Jump => new Vector2(screenW - 1.15f * u, screenH - 1.35f * u),
				Btn.Dash => new Vector2(screenW - 2.45f * u, screenH - 0.95f * u),
				Btn.Grab => new Vector2(screenW - 2.15f * u, screenH - 2.35f * u),
				Btn.Pause => new Vector2(screenW - 0.8f * u, 0.8f * u),
				_ => Vector2.Zero,
			};
		}

		private static float Radius(Btn b) => (b == Btn.Pause ? 0.32f : 0.5f) * Unit;

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
				else if (!stickActive && Vector2.Distance(f.Pos, StickBase) <= screenH * StickRadius * StickGrabRadius)
				{
					stickActive = true;
					stickKey = f.Key;
					UpdateStick(f.Pos);
				}
			}
		}

		private static void UpdateStick(Vector2 pos)
		{
			float range = screenH * StickRadius;
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
			return new GamePadState(s, Vector2.Zero, 0f, pressed[(int)Btn.Grab] ? 1f : 0f, down.ToArray());
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
		private static Texture2D? disc, ring, pixel;

		public static void Draw(GraphicsDevice device)
		{
			PresentationParameters pp = device.PresentationParameters;
			screenW = pp.BackBufferWidth;
			screenH = pp.BackBufferHeight;
			if (!Enabled || realPadConnected)
				return;

			if (batch == null)
			{
				batch = new SpriteBatch(device);
				disc = MakeCircle(device, 128, 0f);
				ring = MakeCircle(device, 128, 0.12f);
				pixel = new Texture2D(device, 1, 1);
				pixel.SetData(new[] { Color.White });
			}

			Viewport saved = device.Viewport;
			device.Viewport = new Viewport(0, 0, screenW, screenH);
			batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null);

			// Analógico fixo no canto inferior esquerdo.
			Vector2 baseCenter = StickBase;
			float range = screenH * StickRadius;
			Vector2 knob = baseCenter + stickKnob;
			float a = stickActive ? Opacity * 1.3f : Opacity;
			DrawCircle(ring!, baseCenter, range * 1.1f, Color.White * a);
			DrawCircle(disc!, knob, range * 0.45f, Color.White * (a * 1.1f));

			DrawButton(Btn.Jump, new Color(120, 220, 140), 'A');
			DrawButton(Btn.Dash, new Color(240, 120, 150), 'X');
			DrawButton(Btn.Grab, new Color(120, 170, 240), 'G');
			DrawButton(Btn.Pause, new Color(220, 220, 220), 'P');

			batch.End();
			device.Viewport = saved;
		}

		private static void DrawButton(Btn b, Color color, char glyph)
		{
			bool down = pressed[(int)b];
			float r = Radius(b);
			float alpha = down ? Math.Min(1f, Opacity * 1.9f) : Opacity;
			Vector2 c = Center(b);
			DrawCircle(disc!, c, r * (down ? 0.92f : 1f), color * (alpha * 0.55f));
			DrawCircle(ring!, c, r, color * alpha);
			DrawGlyph(glyph, c, r * 0.9f, Color.White * Math.Min(1f, alpha * 1.8f));
		}

		private static void DrawCircle(Texture2D tex, Vector2 center, float radius, Color color)
		{
			batch!.Draw(tex, new Rectangle((int)(center.X - radius), (int)(center.Y - radius), (int)(radius * 2), (int)(radius * 2)), color);
		}

		// Letras 5x7 desenhadas com retângulos (sem depender de fonte do jogo).
		private static readonly Dictionary<char, string[]> glyphs = new()
		{
			['A'] = new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" },
			['X'] = new[] { "10001", "10001", "01010", "00100", "01010", "10001", "10001" },
			['G'] = new[] { "01111", "10000", "10000", "10111", "10001", "10001", "01111" },
			['P'] = new[] { "11011", "11011", "11011", "11011", "11011", "11011", "11011" }, // pausa: duas barras
		};

		private static void DrawGlyph(char ch, Vector2 center, float size, Color color)
		{
			if (!glyphs.TryGetValue(ch, out string[]? rows))
				return;
			float cell = size / 7f;
			float x0 = center.X - cell * 2.5f;
			float y0 = center.Y - cell * 3.5f;
			for (int y = 0; y < rows.Length; y++)
				for (int x = 0; x < rows[y].Length; x++)
					if (rows[y][x] == '1')
						batch!.Draw(pixel!, new Rectangle((int)(x0 + x * cell), (int)(y0 + y * cell), (int)Math.Ceiling(cell), (int)Math.Ceiling(cell)), color);
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
