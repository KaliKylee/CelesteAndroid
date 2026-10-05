using System;
using System.IO;
using CelesteAndroid;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod;

namespace Monocle
{
	[MonoModPatch("Monocle.Engine")]
	public class patch_Engine : Engine
	{
		[MonoModIgnore]
		private Scene scene = null!;

		private static Texture2D? pillarboxBackground;
		private static SpriteBatch? pillarboxBatch;
		private static Texture2D? pillarboxPixel;
		private static bool pillarboxLoaded;

		[MonoModIgnore]
		public patch_Engine(int width, int height, int windowWidth, int windowHeight, string windowTitle, bool fullscreen, bool vsync)
			: base(width, height, windowWidth, windowHeight, windowTitle, fullscreen, vsync)
		{
		}

		[MonoModReplace]
		protected new virtual void RenderCore()
		{
			if (scene != null)
			{
				scene.BeforeRender();
			}
			GraphicsDevice.SetRenderTarget(null);
			DrawPillarbox();
			GraphicsDevice.Viewport = Viewport;
			if (pillarboxBackground == null)
			{
				GraphicsDevice.Clear(ClearColor);
			}
			if (scene != null)
			{
				scene.Render();
				scene.AfterRender();
			}
			TouchControls.Draw(GraphicsDevice);
		}

		private void DrawPillarbox()
		{
			if (!pillarboxLoaded)
			{
				pillarboxLoaded = true;
				string? path = HostConfig.BackgroundPath;
				if (path != null && File.Exists(path))
				{
					using FileStream stream = File.OpenRead(path);
					pillarboxBackground = Texture2D.FromStream(GraphicsDevice, stream);
					pillarboxBatch = new SpriteBatch(GraphicsDevice);
					pillarboxPixel = new Texture2D(GraphicsDevice, 1, 1);
					pillarboxPixel.SetData(new[] { Color.White });
				}
			}
			if (pillarboxBackground == null)
			{
				return;
			}

			PresentationParameters pp = GraphicsDevice.PresentationParameters;
			GraphicsDevice.Viewport = new Viewport(0, 0, pp.BackBufferWidth, pp.BackBufferHeight);
			GraphicsDevice.Clear(Color.Black);

			float scale = Math.Max(
				pp.BackBufferWidth / (float)pillarboxBackground.Width,
				pp.BackBufferHeight / (float)pillarboxBackground.Height
			);
			Vector2 size = new Vector2(pillarboxBackground.Width, pillarboxBackground.Height) * scale;
			Vector2 position = (new Vector2(pp.BackBufferWidth, pp.BackBufferHeight) - size) / 2f;

			pillarboxBatch!.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp, null, null);
			pillarboxBatch.Draw(pillarboxBackground, position, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			Viewport view = Viewport;
			pillarboxBatch.Draw(pillarboxPixel, new Rectangle(view.X, view.Y, view.Width, view.Height), ClearColor);
			pillarboxBatch.End();
		}
	}
}
