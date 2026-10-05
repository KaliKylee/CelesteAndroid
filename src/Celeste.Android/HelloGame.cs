using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace CelesteAndroid
{
	/// <summary>
	/// Jogo FNA mínimo para validar a stack no aparelho (M3): gráficos, toque, controle e ciclo de vida.
	/// </summary>
	public class HelloGame : Game
	{
		private readonly GraphicsDeviceManager graphics;
		private SpriteBatch? batch;
		private Texture2D? pixel;
		private readonly Stopwatch fpsTimer = Stopwatch.StartNew();
		private int frames;
		private int touchLogs;
		private Color background = Color.CornflowerBlue;

		public HelloGame()
		{
			graphics = new GraphicsDeviceManager(this)
			{
				IsFullScreen = true,
				SynchronizeWithVerticalRetrace = true,
			};
			IsFixedTimeStep = false;
		}

		protected override void Initialize()
		{
			// Sem isso o FNA usa 800x480 e estica para a tela.
			DisplayMode mode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
			graphics.PreferredBackBufferWidth = mode.Width;
			graphics.PreferredBackBufferHeight = mode.Height;
			graphics.ApplyChanges();
			base.Initialize();
		}

		protected override void LoadContent()
		{
			batch = new SpriteBatch(GraphicsDevice);
			pixel = new Texture2D(GraphicsDevice, 1, 1);
			pixel.SetData(new[] { Color.White });

			PresentationParameters pp = GraphicsDevice.PresentationParameters;
			Console.WriteLine($"[Hello] backbuffer {pp.BackBufferWidth}x{pp.BackBufferHeight}, adapter {GraphicsAdapter.DefaultAdapter.Description}");
		}

		protected override void Update(GameTime gameTime)
		{
			GamePadState pad = GamePad.GetState(PlayerIndex.One);
			if (pad.IsConnected && pad.Buttons.A == ButtonState.Pressed)
				background = Color.DarkGreen;
			else if (pad.IsConnected && pad.Buttons.B == ButtonState.Pressed)
				background = Color.DarkRed;
			else if (TouchPanel.GetState().Count > 0)
				background = Color.MidnightBlue;

			TouchCollection touches = TouchPanel.GetState();
			if (touches.Count > 0 && touchLogs++ < 5)
				Console.WriteLine($"[Hello] touch {touches[0].Id} {touches[0].State} {touches[0].Position}");
			else
				background = Color.CornflowerBlue;

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			// Depois do ApplyChanges o viewport interno do FNA3D fica no tamanho antigo (800x480).
			PresentationParameters bb = GraphicsDevice.PresentationParameters;
			GraphicsDevice.Viewport = new Viewport(0, 0, bb.BackBufferWidth, bb.BackBufferHeight);
			GraphicsDevice.Clear(background);

			batch!.Begin();
			// Quadrado girando: prova que o loop está vivo.
			Viewport vp = GraphicsDevice.Viewport;
			float t = (float)gameTime.TotalGameTime.TotalSeconds;
			batch.Draw(pixel, new Vector2(vp.Width / 2f, vp.Height / 2f), null, Color.White, t, new Vector2(0.5f), 200f, SpriteEffects.None, 0f);
			// Um quadrado em cada dedo.
			foreach (TouchLocation touch in TouchPanel.GetState())
				batch.Draw(pixel, new Rectangle((int)touch.Position.X - 60, (int)touch.Position.Y - 60, 120, 120), Color.Yellow);
			batch.End();

			frames++;
			if (fpsTimer.ElapsedMilliseconds >= 2000)
			{
				PresentationParameters pp = GraphicsDevice.PresentationParameters;
				SDL3.SDL.SDL_free(SDL3.SDL.SDL_GetTouchDevices(out int touchDevices));
				Console.WriteLine($"[Hello] sdlTouchDevices={touchDevices}");
				Console.WriteLine($"[Hello] {frames * 1000.0 / fpsTimer.ElapsedMilliseconds:F1} fps, pad={GamePad.GetState(PlayerIndex.One).IsConnected}, "
					+ $"touches={TouchPanel.GetState().Count}, touchDisplay={TouchPanel.DisplayWidth}x{TouchPanel.DisplayHeight}, "
					+ $"vp={vp.Width}x{vp.Height}, bb={pp.BackBufferWidth}x{pp.BackBufferHeight}, window={Window.ClientBounds}");
				frames = 0;
				fpsTimer.Restart();
			}

			base.Draw(gameTime);
		}
	}
}
