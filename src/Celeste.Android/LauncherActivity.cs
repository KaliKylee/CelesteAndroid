using System;
using System.IO;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Color = Android.Graphics.Color;
using Uri = Android.Net.Uri;

namespace CelesteAndroid
{
	/// <summary>
	/// Tela inicial: key art do jogo, importação dos arquivos do jogo e botão de jogar.
	/// </summary>
	[Activity(
		Name = "org.celesteandroid.celeste.LauncherActivity",
		Label = "Celeste",
		MainLauncher = true,
		Exported = true,
		Theme = "@style/Theme.Celeste",
		ScreenOrientation = ScreenOrientation.SensorLandscape,
		LaunchMode = LaunchMode.SingleTop,
		ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
			| ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation)]
	public class LauncherActivity : Activity
	{
		private const int RequestFolder = 1;
		private const int RequestZip = 2;
		private const int RequestSaves = 3;
		private const string PrefDriver = "driver";

		private static readonly Color Night = Color.ParseColor("#120C22");
		private static readonly Color Accent = Color.ParseColor("#F2B8D8");

		private ImageView art = null!;
		private LinearLayout panel = null!;
		private TextView status = null!;
		private Button play = null!;
		private Button openFolder = null!;
		private TextView openZip = null!;
		private TextView importSaves = null!;
		private TextView driverToggle = null!;
		private LinearLayout links = null!;
		private LinearLayout progressBox = null!;
		private ProgressBar progressBar = null!;
		private TextView progressText = null!;
		private bool busy;

		private ISharedPreferences Prefs => GetSharedPreferences("launcher", FileCreationMode.Private)!;

		protected override void OnCreate(Bundle? savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			SetContentView(BuildLayout());
			HideSystemBars();
			LoadArt();

			// "Splash": a arte aparece sozinha e o painel entra logo depois.
			panel.Alpha = 0f;
			panel.TranslationY = Dp(24);
			panel.Animate()!.Alpha(1f).TranslationY(0f).SetStartDelay(650).SetDuration(550)
				.SetInterpolator(new DecelerateInterpolator(2f))!.Start();

			var zoom = new ScaleAnimation(1f, 1.08f, 1f, 1.08f, Dimension.RelativeToSelf, 0.5f, Dimension.RelativeToSelf, 0.4f)
			{
				Duration = 24000,
				RepeatCount = Animation.Infinite,
				RepeatMode = RepeatMode.Reverse,
				Interpolator = new AccelerateDecelerateInterpolator(),
			};
			art.StartAnimation(zoom);
		}

		protected override void OnResume()
		{
			base.OnResume();
			HideSystemBars();
			RefreshState();

			// Desenvolvimento: arquivos já copiados por scripts/deploy-android.ps1; só patch + fundo.
			// adb shell am start -n org.celesteandroid.celeste/.LauncherActivity --ez repatch true
			if (!busy && Intent!.GetBooleanExtra("repatch", false))
			{
				Intent.RemoveExtra("repatch");
				RunInstall(_ => { });
			}
			// APK pessoal: o jogo já vem dentro do APK; instala sozinho na primeira abertura.
			else if (!busy && !GameInstaller.IsInstalled(this) && GameInstaller.HasEmbeddedGame(this))
			{
				RunInstall(installer => installer.ImportEmbedded());
			}
		}

		#region Layout

		private View BuildLayout()
		{
			var root = new FrameLayout(this);
			root.SetBackgroundColor(Night);

			art = new ImageView(this);
			art.SetScaleType(ImageView.ScaleType.CenterCrop);
			root.AddView(art, Match());

			// Escurece a arte por igual, para o texto e os botões (no centro) ficarem legíveis.
			var shade = new View(this) { Background = new ColorDrawable(Color.Argb(130, 18, 12, 34)) };
			root.AddView(shade, Match());

			panel = new LinearLayout(this) { Orientation = Orientation.Vertical };
			panel.SetGravity(GravityFlags.Center);
			panel.SetPadding(Dp(24), Dp(12), Dp(24), Dp(12));
			root.AddView(panel, new FrameLayout.LayoutParams(Dp(460), ViewGroup.LayoutParams.MatchParent, GravityFlags.Center));

			// Logo: Resources/drawable-nodpi/launcher_logo.png
			var logo = new ImageView(this);
			logo.SetImageResource(Resource.Drawable.launcher_logo);
			logo.SetScaleType(ImageView.ScaleType.FitCenter);
			panel.AddView(logo, new LinearLayout.LayoutParams(Dp(150), Dp(121)));

			var subtitle = Text("Port nativo para Android", 15, Accent, TypefaceStyle.Normal);
			subtitle.LetterSpacing = 0.06f;
			subtitle.Gravity = GravityFlags.Center;
			panel.AddView(subtitle, Margins(top: -4));

			status = Text("", 14, Color.Argb(220, 255, 255, 255), TypefaceStyle.Normal);
			status.Gravity = GravityFlags.Center;
			panel.AddView(status, Margins(top: 10));

			play = PillButton("JOGAR", filled: true);
			play.Click += (_, _) => Play();
			panel.AddView(play, new LinearLayout.LayoutParams(Dp(260), Dp(50)) { TopMargin = Dp(14) });

			openFolder = PillButton("Selecionar arquivos do jogo", filled: false);
			openFolder.Click += (_, _) => PickFolder();
			panel.AddView(openFolder, new LinearLayout.LayoutParams(Dp(260), Dp(44)) { TopMargin = Dp(10) });

			links = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			openZip = LinkText("Importar .zip");
			openZip.Click += (_, _) => PickZip();
			importSaves = LinkText("Importar saves");
			importSaves.Click += (_, _) => StartActivityForResult(new Intent(Intent.ActionOpenDocumentTree), RequestSaves);
			driverToggle = LinkText("");
			driverToggle.Click += (_, _) => ToggleDriver();
			links.AddView(openZip);
			links.AddView(Text("·", 14, Color.Argb(120, 255, 255, 255), TypefaceStyle.Normal), Margins(left: 10, right: 10));
			links.AddView(importSaves);
			links.AddView(Text("·", 14, Color.Argb(120, 255, 255, 255), TypefaceStyle.Normal), Margins(left: 10, right: 10));
			links.AddView(driverToggle);
			panel.AddView(links, Margins(top: 6));

			progressBox = new LinearLayout(this) { Orientation = Orientation.Vertical, Visibility = ViewStates.Gone };
			progressBox.SetGravity(GravityFlags.CenterHorizontal);
			progressBar = new ProgressBar(this, null, Android.Resource.Attribute.ProgressBarStyleHorizontal) { Max = 1000 };
			progressBar.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Accent);
			progressBar.IndeterminateTintList = Android.Content.Res.ColorStateList.ValueOf(Accent);
			progressText = Text("", 13, Color.Argb(210, 255, 255, 255), TypefaceStyle.Normal);
			progressText.Gravity = GravityFlags.Center;
			progressBox.AddView(progressBar, new LinearLayout.LayoutParams(Dp(260), Dp(8)));
			progressBox.AddView(progressText, Margins(top: 6));
			panel.AddView(progressBox, Margins(top: 12));

			// Créditos no canto inferior direito: autor do port + link do Discord.
			var credits = new LinearLayout(this) { Orientation = Orientation.Vertical };
			credits.SetGravity(GravityFlags.End);
			var byline = Text("Port By Kali Kyle", 13, Color.Argb(220, 255, 255, 255), TypefaceStyle.Bold);
			byline.SetShadowLayer(Dp(4), 0, Dp(1), Color.Argb(180, 0, 0, 0));
			byline.Gravity = GravityFlags.End;
			var discord = LinkText("Discord: discord.gg/BMv35jwYn5");
			discord.SetTextSize(ComplexUnitType.Sp, 13);
			discord.SetShadowLayer(Dp(4), 0, Dp(1), Color.Argb(180, 0, 0, 0));
			discord.Gravity = GravityFlags.End;
			discord.Click += (_, _) => StartActivity(new Intent(Intent.ActionView, Uri.Parse("https://discord.gg/BMv35jwYn5")));
			credits.AddView(byline);
			credits.AddView(discord);
			var creditsParams = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent,
				GravityFlags.Bottom | GravityFlags.End) { RightMargin = Dp(28), BottomMargin = Dp(14) };
			root.AddView(credits, creditsParams);

			// Editor dos controles de toque (mover e redimensionar). Fica no canto, fora do painel central.
			var customize = PillButton("Personalizar controles", filled: false);
			customize.SetTextSize(ComplexUnitType.Sp, 13);
			customize.Click += (_, _) => StartActivity(new Intent(this, typeof(ControlsEditorActivity)));
			root.AddView(customize, new FrameLayout.LayoutParams(Dp(196), Dp(38), GravityFlags.Top | GravityFlags.End) { TopMargin = Dp(14), RightMargin = Dp(20) });

			return root;
		}

		private void LoadArt()
		{
			// Fundo da tela inicial: Resources/drawable-nodpi/launcher_bg.jpg
			art.SetImageResource(Resource.Drawable.launcher_bg);
		}

		private Button PillButton(string label, bool filled)
		{
			var button = new Button(this) { Text = label, StateListAnimator = null };
			button.SetAllCaps(false);
			button.SetPadding(Dp(10), 0, Dp(10), 0);
			button.SetTextSize(ComplexUnitType.Sp, filled ? 18 : 15);
			button.SetTypeface(Typeface.Create("sans-serif-medium", filled ? TypefaceStyle.Bold : TypefaceStyle.Normal), filled ? TypefaceStyle.Bold : TypefaceStyle.Normal);
			button.LetterSpacing = filled ? 0.12f : 0.02f;
			var shape = new GradientDrawable();
			shape.SetCornerRadius(Dp(25));
			if (filled)
			{
				shape.SetColor(Color.White);
				button.SetTextColor(Night);
			}
			else
			{
				shape.SetColor(Color.Argb(40, 255, 255, 255));
				shape.SetStroke(Dp(1.5f), Color.Argb(200, 255, 255, 255));
				button.SetTextColor(Color.White);
			}
			button.Background = new RippleDrawable(Android.Content.Res.ColorStateList.ValueOf(Color.Argb(60, 242, 184, 216)), shape, null);
			return button;
		}

		private TextView Text(string text, float sp, Color color, TypefaceStyle style)
		{
			var view = new TextView(this) { Text = text };
			view.SetTextSize(ComplexUnitType.Sp, sp);
			view.SetTextColor(color);
			view.SetTypeface(Typeface.Create("sans-serif", style), style);
			return view;
		}

		private TextView LinkText(string text)
		{
			var view = Text(text, 14, Accent, TypefaceStyle.Normal);
			view.SetPadding(0, Dp(6), 0, Dp(6));
			return view;
		}

		private static FrameLayout.LayoutParams Match() =>
			new(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);

		private LinearLayout.LayoutParams Margins(int top = 0, int left = 0, int right = 0) =>
			new(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
			{
				TopMargin = Dp(top), LeftMargin = Dp(left), RightMargin = Dp(right),
			};

		private int Dp(float dp) => (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, Resources!.DisplayMetrics);

		private void HideSystemBars()
		{
			if (!OperatingSystem.IsAndroidVersionAtLeast(30))
				return;
			// A partir do Android 15 o app já é edge-to-edge por padrão.
			if (!OperatingSystem.IsAndroidVersionAtLeast(35))
				Window!.SetDecorFitsSystemWindows(false);
			IWindowInsetsController? insets = Window!.InsetsController;
			if (insets != null)
			{
				insets.Hide(WindowInsets.Type.SystemBars());
				insets.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
			}
		}

		#endregion

		#region Estado e ações

		private void RefreshState()
		{
			bool installed = GameInstaller.IsInstalled(this);
			play.Enabled = installed && !busy;
			play.Alpha = play.Enabled ? 1f : 0.4f;
			openFolder.Enabled = !busy;
			openZip.Enabled = !busy;
			importSaves.Enabled = !busy;
			// A tela é baixa (~360dp): durante a instalação a barra de progresso ocupa o lugar dos links.
			links.Visibility = busy ? ViewStates.Gone : ViewStates.Visible;
			openFolder.Text = installed ? "Mudar arquivos do jogo" : "Selecionar arquivos do jogo";
			if (!busy)
			{
				status.Text = installed
					? "✓  Pronto pra jogar"
					: "Selecione a pasta da sua cópia do Celeste para PC (FNA, opengl Build ou do itch.io .zip).";
			}
			driverToggle.Text = "Gráficos: " + (Prefs.GetString(PrefDriver, "") == "OpenGL" ? "OpenGL ES" : "Vulkan");
		}

		private void Play()
		{
			var intent = new Intent(this, typeof(GameActivity));
			string? driver = Prefs.GetString(PrefDriver, "");
			if (!string.IsNullOrEmpty(driver))
				intent.PutExtra(GameActivity.ExtraDriver, driver);
			StartActivity(intent);
		}

		private void ToggleDriver()
		{
			string next = Prefs.GetString(PrefDriver, "") == "OpenGL" ? "" : "OpenGL";
			Prefs.Edit()!.PutString(PrefDriver, next)!.Apply();
			RefreshState();
		}

		private void PickFolder()
		{
			StartActivityForResult(new Intent(Intent.ActionOpenDocumentTree), RequestFolder);
		}

		private void PickZip()
		{
			var intent = new Intent(Intent.ActionOpenDocument);
			intent.AddCategory(Intent.CategoryOpenable);
			intent.SetType("*/*");
			intent.PutExtra(Intent.ExtraMimeTypes, new[] { "application/zip", "application/x-zip-compressed" });
			StartActivityForResult(intent, RequestZip);
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
		{
			base.OnActivityResult(requestCode, resultCode, data);
			Uri? uri = data?.Data;
			if (resultCode != Result.Ok || uri == null)
				return;
			if (requestCode == RequestFolder)
				RunInstall(installer => installer.ImportFolder(uri));
			else if (requestCode == RequestZip)
				RunInstall(installer => installer.ImportZip(uri));
			else if (requestCode == RequestSaves)
				RunJob(installer => $"✓  {installer.ImportSaves(uri)} arquivo(s) de save importado(s)");
		}

		/// <summary>Importação do jogo: cópia → patch → fundo.</summary>
		private void RunInstall(Action<GameInstaller> import)
		{
			RunJob(installer =>
			{
				import(installer);
				installer.Patch();
				installer.PrepareBackground();
				RunOnUiThread(LoadArt);
				return "✓  Jogo importado! Pronto pra jogar";
			});
		}

		/// <summary>Roda uma tarefa do instalador fora da thread de UI, com progresso; devolve a mensagem de sucesso.</summary>
		private void RunJob(Func<GameInstaller, string> job)
		{
			busy = true;
			progressBox.Visibility = ViewStates.Visible;
			RefreshState();

			var installer = new GameInstaller(this, (message, fraction) => RunOnUiThread(() =>
			{
				progressText.Text = message;
				progressBar.Indeterminate = fraction < 0;
				if (fraction >= 0)
					progressBar.Progress = (int)(fraction * 1000);
			}));

			new Thread(() =>
			{
				string? error = null;
				string? success = null;
				try
				{
					success = job(installer);
				}
				catch (InstallException e)
				{
					error = e.Message;
				}
				catch (Exception e)
				{
					Log.Error(GameActivity.LogTag, e.ToString());
					error = "Algo deu errado: " + e.Message;
				}

				RunOnUiThread(() =>
				{
					busy = false;
					progressBox.Visibility = ViewStates.Gone;
					RefreshState();
					status.Text = error ?? success;
				});
			}) { Name = "CelesteInstall", IsBackground = true }.Start();
		}

		#endregion
	}
}
