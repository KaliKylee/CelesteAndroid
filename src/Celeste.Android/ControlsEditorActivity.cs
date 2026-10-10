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
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Color = Android.Graphics.Color;
using Path = System.IO.Path;

namespace CelesteAndroid
{
	[Activity(
		Name = "org.celesteandroid.celeste.ControlsEditorActivity",
		Label = "Celeste",
		Exported = false,
		Theme = "@style/Theme.Celeste",
		ScreenOrientation = ScreenOrientation.SensorLandscape,
		ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
			| ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation)]
	public class ControlsEditorActivity : Activity
	{
		private static readonly Color Night = Color.ParseColor("#120C22");
		private static readonly Color Accent = Color.ParseColor("#F2B8D8");

		private ControlsCanvas canvas = null!;
		private Button dirButton = null!;
		private TextView title = null!;
		private readonly List<Button> tabButtons = new();
		private readonly List<View> sections = new();
		private TextView widthLabel = null!, heightLabel = null!;
		private SeekBar widthSeek = null!, heightSeek = null!;
		private LinearLayout heightRow = null!;
		private readonly Button[] shapeButtons = new Button[3];
		private LinearLayout shapeBox = null!, iconBox = null!;
		private TextView shapeNote = null!, iconNote = null!;
		private Button defaultIconButton = null!;
		private int pickTarget = -1;

		public override ScreenOrientation RequestedOrientation
		{
			get => base.RequestedOrientation;
			set => base.RequestedOrientation = LandscapeLock.Coerce(value);
		}
		private TextView opacityLabel = null!;
		private SeekBar opacitySeek = null!;

		private const string PrefGrid = "editor_grid";
		private int gridMode;
		private Button gridButton = null!;
		private LinearLayout fpsRow = null!;
		private Button fpsCornerButton = null!;
		private TextView fpsXLabel = null!, fpsYLabel = null!;
		private SeekBar fpsXSeek = null!, fpsYSeek = null!;

		protected override void OnCreate(Bundle? savedInstanceState)
		{
			CultureFix.Apply();
			RequestedOrientation = LandscapeLock.Orientation;
			base.OnCreate(savedInstanceState);
			L.Init(GetSharedPreferences("launcher", FileCreationMode.Private)!);
			SetContentView(BuildLayout());
			HideSystemBars();
			UpdateBar();
		}

		protected override void OnResume()
		{
			base.OnResume();
			HideSystemBars();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (IsFinishing)
				TouchIcons.Cleanup(GameInstaller.TouchLayoutFile(this));
		}

		private View BuildLayout()
		{
			var root = new FrameLayout(this);
			root.SetBackgroundColor(Night);

			var bg = new ImageView(this);
			bg.SetScaleType(ImageView.ScaleType.CenterCrop);
			bg.SetImageResource(Resource.Drawable.launcher_bg);
			root.AddView(bg, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

			var shade = new View(this) { Background = new ColorDrawable(Color.Argb(150, 18, 12, 34)) };
			root.AddView(shade, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

			canvas = new ControlsCanvas(this, GameInstaller.TouchLayoutFile(this));
			canvas.OpacityPercent = GameOptions.Opacity(GameOptions.Prefs(this));
			canvas.FpsEnabled = GameOptions.ShowFps(GameOptions.Prefs(this));
			gridMode = Math.Clamp(GameOptions.Prefs(this).GetInt(PrefGrid, 0), 0, 3);
			canvas.GridMode = gridMode;
			canvas.SelectionChanged = UpdateBar;
			canvas.LayoutLoaded = () => { if (dirButton != null) dirButton.Text = canvas.DpadMode ? L.DirDpad : L.DirAnalog; };
			canvas.FpsChanged = UpdateFpsRow;
			root.AddView(canvas, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

			var bar = new LinearLayout(this) { Orientation = Orientation.Vertical };
			bar.SetPadding(Dp(12), Dp(8), Dp(12), Dp(10));
			var barShape = new GradientDrawable();
			barShape.SetColor(Color.Argb(215, 18, 12, 34));
			barShape.SetCornerRadius(Dp(16));
			bar.Background = barShape;

			var header = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			header.SetGravity(GravityFlags.CenterVertical);
			title = Text("", 14, Color.White, true);
			header.AddView(title, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f));
			var cancel = MakeButton(L.Cancel, filled: false);
			cancel.Click += (_, _) => Finish();
			var save = MakeButton(L.Save, filled: true);
			save.Click += (_, _) =>
			{
				canvas.Save();
				GameOptions.SetOpacity(GameOptions.Prefs(this), canvas.OpacityPercent);
				Toast.MakeText(this, L.ControlsSaved, ToastLength.Short)?.Show();
				Finish();
			};
			header.AddView(cancel, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, Dp(34)) { LeftMargin = Dp(6) });
			header.AddView(save, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, Dp(34)) { LeftMargin = Dp(6) });
			bar.AddView(header, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

			var tabRow = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			string[] tabNames = { L.TabSize, L.TabShape, L.TabIcon, L.TabMore };
			for (int k = 0; k < tabNames.Length; k++)
			{
				int tab = k;
				Button tb = MakeButton(tabNames[k], filled: false);
				tb.Click += (_, _) => ShowTab(tab);
				tabButtons.Add(tb);
				tabRow.AddView(tb, new LinearLayout.LayoutParams(0, Dp(34), 1f) { LeftMargin = k == 0 ? 0 : Dp(6) });
			}
			bar.AddView(tabRow, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) { TopMargin = Dp(8) });

			sections.Add(BuildSizeSection());
			sections.Add(BuildShapeSection());
			sections.Add(BuildIconSection());
			sections.Add(BuildMoreSection());
			var holder = new FrameLayout(this);
			foreach (View section in sections)
				holder.AddView(section, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
			var scroll = new MaxHeightScrollView(this)
			{
				MaxHeightPx = Math.Min(Dp(190), (int)(Resources!.DisplayMetrics!.HeightPixels * 0.42f)),
				VerticalScrollBarEnabled = false,
			};
			scroll.AddView(holder, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
			bar.AddView(scroll, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) { TopMargin = Dp(8) });

			ShowTab(0);

			int barWidth = Math.Min(Dp(560), Resources!.DisplayMetrics!.WidthPixels - Dp(190));
			root.AddView(bar, new FrameLayout.LayoutParams(barWidth, ViewGroup.LayoutParams.WrapContent, GravityFlags.Top | GravityFlags.CenterHorizontal) { TopMargin = Dp(8) });

			return root;
		}

		private void ShowTab(int tab)
		{
			for (int i = 0; i < sections.Count; i++)
				sections[i].Visibility = i == tab ? ViewStates.Visible : ViewStates.Gone;
			for (int i = 0; i < tabButtons.Count; i++)
				StyleButton(tabButtons[i], i == tab);
		}

		private SeekBar MakeSeek(int max, Action<int> onChange)
		{
			var s = new SeekBar(this) { Max = max };
			s.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Accent);
			s.ThumbTintList = Android.Content.Res.ColorStateList.ValueOf(Accent);
			s.ProgressChanged += (_, e) =>
			{
				if (e.FromUser)
					onChange(e.Progress);
			};
			return s;
		}

		private LinearLayout SeekRow(out TextView lbl, out SeekBar seekBar, int max, Action<int> onChange)
		{
			var row = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			row.SetGravity(GravityFlags.CenterVertical);
			lbl = Text("", 12, Color.White, true);
			row.AddView(lbl, new LinearLayout.LayoutParams(Dp(110), ViewGroup.LayoutParams.WrapContent));
			seekBar = MakeSeek(max, onChange);
			row.AddView(seekBar, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f));
			return row;
		}

		private View BuildSizeSection()
		{
			var box = new LinearLayout(this) { Orientation = Orientation.Vertical };
			LinearLayout widthRow = SeekRow(out widthLabel, out widthSeek, 150, p =>
			{
				canvas.SetSelectedScale((canvas.SelectedMinPct + p) / 100f);
				UpdateBar();
			});
			heightRow = SeekRow(out heightLabel, out heightSeek, 150, p =>
			{
				canvas.SetSelectedScaleH((canvas.SelectedMinPct + p) / 100f);
				UpdateBar();
			});
			box.AddView(widthRow, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
			box.AddView(heightRow, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
			return box;
		}

		private View BuildShapeSection()
		{
			var box = new LinearLayout(this) { Orientation = Orientation.Vertical };
			shapeBox = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			string[] names = { L.ShapeCircle, L.ShapeSquare, L.ShapeRect };
			for (int k = 0; k < names.Length; k++)
			{
				int shape = k;
				Button b = MakeButton(names[k], filled: false);
				b.Click += (_, _) =>
				{
					canvas.SetSelectedShape(shape);
					UpdateBar();
				};
				shapeButtons[k] = b;
				shapeBox.AddView(b, new LinearLayout.LayoutParams(0, Dp(36), 1f) { LeftMargin = k == 0 ? 0 : Dp(6) });
			}
			shapeNote = Text(L.ShapeUnavailable, 12, Color.Argb(200, 255, 255, 255), false);
			box.AddView(shapeBox, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
			box.AddView(shapeNote, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
			return box;
		}

		private View BuildIconSection()
		{
			var box = new LinearLayout(this) { Orientation = Orientation.Vertical };
			iconBox = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			Button pick = MakeButton(L.PickFromGallery, filled: true);
			pick.Click += (_, _) =>
			{
				if (!canvas.ShapeSupported)
					return;
				pickTarget = canvas.Selected;
				var intent = new Intent(Intent.ActionOpenDocument);
				intent.AddCategory(Intent.CategoryOpenable);
				intent.SetType("image/*");
				StartActivityForResult(intent, RequestPickIcon);
			};
			defaultIconButton = MakeButton(L.DefaultIcon, filled: false);
			defaultIconButton.Click += (_, _) =>
			{
				canvas.ClearIcon(canvas.Selected);
				UpdateBar();
			};
			iconBox.AddView(pick, new LinearLayout.LayoutParams(0, Dp(36), 2f));
			iconBox.AddView(defaultIconButton, new LinearLayout.LayoutParams(0, Dp(36), 1f) { LeftMargin = Dp(6) });
			iconNote = Text(L.ShapeUnavailable, 12, Color.Argb(200, 255, 255, 255), false);
			box.AddView(iconBox, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
			box.AddView(iconNote, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
			return box;
		}

		private View BuildMoreSection()
		{
			var box = new LinearLayout(this) { Orientation = Orientation.Vertical };

			var opacityRow = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			opacityRow.SetGravity(GravityFlags.CenterVertical);
			opacityLabel = Text("", 12, Color.White, true);
			opacityRow.AddView(opacityLabel, new LinearLayout.LayoutParams(Dp(110), ViewGroup.LayoutParams.WrapContent));
			opacitySeek = MakeSeek(100, p =>
			{
				canvas.OpacityPercent = p;
				UpdateLabel();
			});
			opacityRow.AddView(opacitySeek, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f));
			box.AddView(opacityRow, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

			var dirRow = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			dirButton = MakeButton(canvas.DpadMode ? L.DirDpad : L.DirAnalog, filled: false);
			dirButton.Click += (_, _) =>
			{
				canvas.DpadMode = !canvas.DpadMode;
				dirButton.Text = canvas.DpadMode ? L.DirDpad : L.DirAnalog;
				canvas.Save();
			};
			dirRow.AddView(dirButton, new LinearLayout.LayoutParams(0, Dp(34), 1f));
			box.AddView(dirRow, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) { TopMargin = Dp(4) });

			var gridRow = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			gridButton = MakeButton(L.GridLabel(gridMode), filled: false);
			gridButton.Click += (_, _) =>
			{
				gridMode = (gridMode + 1) % 4;
				canvas.GridMode = gridMode;
				gridButton.Text = L.GridLabel(gridMode);
				GameOptions.Prefs(this).Edit()!.PutInt(PrefGrid, gridMode)!.Apply();
			};
			var reset = MakeButton(L.Reset, filled: false);
			reset.Click += (_, _) =>
			{
				canvas.ResetToDefaults();
				canvas.OpacityPercent = GameOptions.DefaultOpacity;
				UpdateBar();
			};
			gridRow.AddView(gridButton, new LinearLayout.LayoutParams(0, Dp(34), 1f));
			gridRow.AddView(reset, new LinearLayout.LayoutParams(0, Dp(34), 1f) { LeftMargin = Dp(6) });
			box.AddView(gridRow, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) { TopMargin = Dp(4) });

			var shareRow = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			var exportBtn = MakeButton(L.ExportControls, filled: false);
			exportBtn.Click += (_, _) => StartExport();
			var importBtn = MakeButton(L.ImportControls, filled: false);
			importBtn.Click += (_, _) => StartImport();
			shareRow.AddView(exportBtn, new LinearLayout.LayoutParams(0, Dp(34), 1f));
			shareRow.AddView(importBtn, new LinearLayout.LayoutParams(0, Dp(34), 1f) { LeftMargin = Dp(6) });
			box.AddView(shareRow, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) { TopMargin = Dp(4) });

			fpsRow = new LinearLayout(this) { Orientation = Orientation.Vertical };
			fpsCornerButton = MakeButton("", filled: false);
			fpsCornerButton.Click += (_, _) =>
			{
				canvas.SelectItem(ControlsCanvas.Fps);
				canvas.FpsCorner = (canvas.FpsCorner + 1) % 4;
			};
			fpsRow.AddView(fpsCornerButton, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, Dp(34)));
			var xRow = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			xRow.SetGravity(GravityFlags.CenterVertical);
			fpsXLabel = Text("", 12, Color.White, true);
			xRow.AddView(fpsXLabel, new LinearLayout.LayoutParams(Dp(110), ViewGroup.LayoutParams.WrapContent));
			fpsXSeek = MakeFpsSeek(x: true);
			xRow.AddView(fpsXSeek, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f));
			fpsRow.AddView(xRow, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
			var yRow = new LinearLayout(this) { Orientation = Orientation.Horizontal };
			yRow.SetGravity(GravityFlags.CenterVertical);
			fpsYLabel = Text("", 12, Color.White, true);
			yRow.AddView(fpsYLabel, new LinearLayout.LayoutParams(Dp(110), ViewGroup.LayoutParams.WrapContent));
			fpsYSeek = MakeFpsSeek(x: false);
			yRow.AddView(fpsYSeek, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f));
			fpsRow.AddView(yRow, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
			fpsRow.Visibility = canvas.FpsEnabled ? ViewStates.Visible : ViewStates.Gone;
			box.AddView(fpsRow, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) { TopMargin = Dp(4) });

			var hint = Text(L.EditorHint, 11, Color.Argb(180, 255, 255, 255), false);
			box.AddView(hint, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) { TopMargin = Dp(6) });
			return box;
		}

		#region Exportar / importar HUD

		private const int RequestExport = 11, RequestImport = 12, RequestPickIcon = 13;
		private const string HudHeader = "CELESTE_HUD_V1";

		private void StartExport()
		{
			var intent = new Intent(Intent.ActionCreateDocument);
			intent.AddCategory(Intent.CategoryOpenable);
			intent.SetType("text/plain");
			intent.PutExtra(Intent.ExtraTitle, "celeste-hud.txt");
			StartActivityForResult(intent, RequestExport);
		}

		private void StartImport()
		{
			var intent = new Intent(Intent.ActionOpenDocument);
			intent.AddCategory(Intent.CategoryOpenable);
			intent.SetType("*/*");
			StartActivityForResult(intent, RequestImport);
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
		{
			base.OnActivityResult(requestCode, resultCode, data);
			Android.Net.Uri? uri = data?.Data;
			if (resultCode != Result.Ok || uri == null)
				return;
			try
			{
				if (requestCode == RequestPickIcon)
				{
					if (!ApplyIcon(uri))
						Toast.MakeText(this, L.IconError, ToastLength.Long)?.Show();
					UpdateBar();
					return;
				}
				if (requestCode == RequestExport)
				{
					ExportTo(uri);
					Toast.MakeText(this, L.ControlsExported, ToastLength.Short)?.Show();
				}
				else if (requestCode == RequestImport)
				{
					if (ImportFrom(uri))
					{
						Toast.MakeText(this, L.ControlsImported, ToastLength.Short)?.Show();
						Recreate();
					}
					else
						Toast.MakeText(this, L.ImportInvalid, ToastLength.Long)?.Show();
				}
			}
			catch (Exception)
			{
				Toast.MakeText(this, L.ImportInvalid, ToastLength.Long)?.Show();
			}
		}

		private bool ApplyIcon(Android.Net.Uri uri)
		{
			int target = pickTarget;
			if (!canvas.IsShapeable(target))
				return false;
			Bitmap? bmp = DecodeIcon(uri, 512);
			if (bmp == null)
				return false;
			try
			{
				string dir = TouchIcons.Dir(GameInstaller.TouchLayoutFile(this));
				Directory.CreateDirectory(dir);
				string name = ControlsCanvas.KeyOf(target) + "_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture) + ".png";
				using (FileStream fs = File.Create(Path.Combine(dir, name)))
					bmp.Compress(Bitmap.CompressFormat.Png!, 100, fs);
				return canvas.SetIcon(target, name);
			}
			catch (Exception)
			{
				return false;
			}
			finally
			{
				bmp.Recycle();
			}
		}

		private Bitmap? DecodeIcon(Android.Net.Uri uri, int maxSide)
		{
			var bounds = new BitmapFactory.Options { InJustDecodeBounds = true };
			using (Stream? s = ContentResolver!.OpenInputStream(uri))
			{
				if (s == null)
					return null;
				BitmapFactory.DecodeStream(s, null, bounds);
			}
			if (bounds.OutWidth <= 0 || bounds.OutHeight <= 0)
				return null;
			int sample = 1;
			while (Math.Max(bounds.OutWidth, bounds.OutHeight) / sample > maxSide * 2)
				sample *= 2;
			var opts = new BitmapFactory.Options { InSampleSize = sample };
			Bitmap? bmp;
			using (Stream? s = ContentResolver!.OpenInputStream(uri))
			{
				if (s == null)
					return null;
				bmp = BitmapFactory.DecodeStream(s, null, opts);
			}
			if (bmp == null)
				return null;

			int degrees = 0;
			try
			{
				using Stream? es = ContentResolver!.OpenInputStream(uri);
				if (es != null)
				{
					var exif = new Android.Media.ExifInterface(es);
					degrees = exif.GetAttributeInt(Android.Media.ExifInterface.TagOrientation, 1) switch { 6 => 90, 3 => 180, 8 => 270, _ => 0 };
				}
			}
			catch (Exception)
			{
			}

			float factor = Math.Min(1f, maxSide / (float)Math.Max(bmp.Width, bmp.Height));
			if (degrees != 0 || factor < 1f)
			{
				var m = new Matrix();
				if (degrees != 0)
					m.PostRotate(degrees);
				if (factor < 1f)
					m.PostScale(factor, factor);
				Bitmap? scaled = Bitmap.CreateBitmap(bmp, 0, 0, bmp.Width, bmp.Height, m, true);
				if (scaled != null && scaled != bmp)
				{
					bmp.Recycle();
					bmp = scaled;
				}
			}
			return bmp;
		}

		private static string ReadOrEmpty(string path) => File.Exists(path) ? File.ReadAllText(path) : "";

		private void ExportTo(Android.Net.Uri uri)
		{
			// Grava o estado atual da tela (posições, tamanhos, FPS) antes de exportar.
			canvas.Save();
			ISharedPreferences prefs = GameOptions.Prefs(this);
			GameOptions.SetOpacity(prefs, canvas.OpacityPercent);

			var sb = new StringBuilder();
			sb.Append(HudHeader).Append('\n');
			sb.Append("[options]\n");
			sb.Append("opacity=").Append(canvas.OpacityPercent.ToString(CultureInfo.InvariantCulture)).Append('\n');
			sb.Append("grid=").Append(gridMode.ToString(CultureInfo.InvariantCulture)).Append('\n');
			sb.Append("show_fps=").Append(GameOptions.ShowFps(prefs) ? '1' : '0').Append('\n');
			sb.Append("hide_touch=").Append(GameOptions.HideTouch(prefs) ? '1' : '0').Append('\n');
			sb.Append("[layout]\n").Append(ReadOrEmpty(GameInstaller.TouchLayoutFile(this)).Replace("\r", "").TrimEnd('\n')).Append('\n');
			sb.Append("[style]\n").Append(ReadOrEmpty(GameInstaller.ButtonStyleFile(this)).Replace("\r", "").TrimEnd('\n')).Append('\n');
			sb.Append("[custom]\n").Append(ReadOrEmpty(GameInstaller.CustomButtonsFile(this)).Replace("\r", "").TrimEnd('\n')).Append('\n');

			using Stream? output = ContentResolver!.OpenOutputStream(uri, "wt");
			if (output == null)
				throw new IOException();
			byte[] bytes = new UTF8Encoding(false).GetBytes(sb.ToString());
			output.Write(bytes, 0, bytes.Length);
		}

		private bool ImportFrom(Android.Net.Uri uri)
		{
			string text;
			using (Stream? input = ContentResolver!.OpenInputStream(uri))
			{
				if (input == null)
					return false;
				using var reader = new StreamReader(input, Encoding.UTF8);
				text = reader.ReadToEnd();
			}

			string[] lines = text.Replace("\r", "").Split('\n');
			if (lines.Length == 0 || lines[0].Trim() != HudHeader)
				return false;

			var layout = new List<string>();
			var style = new List<string>();
			var custom = new List<string>();
			var options = new Dictionary<string, int>();
			string section = "";
			for (int i = 1; i < lines.Length; i++)
			{
				string line = lines[i].Trim();
				if (line.Length == 0 || line.Length > 200)
					continue;
				if (line.StartsWith('[') && line.EndsWith(']'))
				{
					section = line;
					continue;
				}
				string[] kv = line.Split('=');
				if (kv.Length != 2)
					continue;
				switch (section)
				{
					case "[layout]": layout.Add(line); break;
					case "[style]": style.Add(line); break;
					case "[custom]": custom.Add(line); break;
					case "[options]":
						if (int.TryParse(kv[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
							options[kv[0].Trim()] = v;
						break;
				}
			}

			WriteOrDelete(GameInstaller.TouchLayoutFile(this), layout);
			WriteOrDelete(GameInstaller.ButtonStyleFile(this), style);
			WriteOrDelete(GameInstaller.CustomButtonsFile(this), custom);

			ISharedPreferences prefs = GameOptions.Prefs(this);
			if (options.TryGetValue("opacity", out int op))
				GameOptions.SetOpacity(prefs, op);
			if (options.TryGetValue("grid", out int grid))
				prefs.Edit()!.PutInt(PrefGrid, Math.Clamp(grid, 0, 3))!.Apply();
			if (options.TryGetValue("show_fps", out int fps))
				GameOptions.SetShowFps(prefs, fps == 1);
			if (options.TryGetValue("hide_touch", out int hide))
				GameOptions.SetHideTouch(prefs, hide == 1);
			return true;
		}

		private static void WriteOrDelete(string path, List<string> lines)
		{
			if (lines.Count == 0)
			{
				if (File.Exists(path))
					File.Delete(path);
				return;
			}
			File.WriteAllText(path, string.Join("\n", lines) + "\n");
		}

		#endregion

		private void UpdateBar()
		{
			int sel = canvas.Selected;
			title.Text = canvas.SelectedName;

			bool shapeOk = canvas.ShapeSupported;
			bool rect = shapeOk && canvas.SelectedShape == 2;
			int minPct = canvas.SelectedMinPct, maxPct = canvas.SelectedMaxPct;
			int wPct = (int)Math.Round(canvas.SelectedScale * 100f);
			int hPct = (int)Math.Round(canvas.SelectedScaleH * 100f);
			if (widthSeek.Max != maxPct - minPct)
				widthSeek.Max = maxPct - minPct;
			if (heightSeek.Max != maxPct - minPct)
				heightSeek.Max = maxPct - minPct;
			if (widthSeek.Progress != wPct - minPct)
				widthSeek.Progress = wPct - minPct;
			if (heightSeek.Progress != hPct - minPct)
				heightSeek.Progress = hPct - minPct;
			heightRow.Visibility = rect ? ViewStates.Visible : ViewStates.Gone;
			widthLabel.Text = $"{(rect ? L.WidthLabel : L.SizeLabel)}: {wPct}%";
			heightLabel.Text = $"{L.HeightLabel}: {hPct}%";

			shapeBox.Visibility = shapeOk ? ViewStates.Visible : ViewStates.Gone;
			shapeNote.Visibility = shapeOk ? ViewStates.Gone : ViewStates.Visible;
			iconBox.Visibility = shapeOk ? ViewStates.Visible : ViewStates.Gone;
			iconNote.Visibility = shapeOk ? ViewStates.Gone : ViewStates.Visible;
			for (int k = 0; k < shapeButtons.Length; k++)
				StyleButton(shapeButtons[k], shapeOk && canvas.SelectedShape == k);
			defaultIconButton.Enabled = canvas.SelectedHasIcon;
			defaultIconButton.Alpha = canvas.SelectedHasIcon ? 1f : 0.45f;

			opacitySeek.Progress = canvas.OpacityPercent;
			UpdateLabel();
			UpdateFpsRow();
		}

		private SeekBar MakeFpsSeek(bool x)
		{
			var s = new SeekBar(this) { Max = 500 };
			s.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Accent);
			s.ThumbTintList = Android.Content.Res.ColorStateList.ValueOf(Accent);
			s.ProgressChanged += (_, e) =>
			{
				if (!e.FromUser)
					return;
				canvas.SelectItem(ControlsCanvas.Fps);
				if (x)
					canvas.FpsMx = e.Progress / 1000f;
				else
					canvas.FpsMy = e.Progress / 1000f;
			};
			return s;
		}

		private void UpdateFpsRow()
		{
			if (fpsRow == null)
				return;
			fpsCornerButton.Text = L.FpsCorners[canvas.FpsCorner];
			int px = (int)Math.Round(canvas.FpsMx * 1000f), py = (int)Math.Round(canvas.FpsMy * 1000f);
			if (fpsXSeek.Progress != px)
				fpsXSeek.Progress = px;
			if (fpsYSeek.Progress != py)
				fpsYSeek.Progress = py;
			fpsXLabel.Text = string.Format(CultureInfo.InvariantCulture, "X {0:0.0}%", px / 10f);
			fpsYLabel.Text = string.Format(CultureInfo.InvariantCulture, "Y {0:0.0}%", py / 10f);
		}

		private void UpdateLabel()
		{
			opacityLabel.Text = $"{L.Opacity}: {canvas.OpacityPercent}%";
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
			button.SetMinimumWidth(0);
			button.SetMaxLines(1);
			button.Ellipsize = Android.Text.TextUtils.TruncateAt.End;
			StyleButton(button, filled);
			return button;
		}

		private void StyleButton(Button button, bool filled)
		{
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

	internal static class TouchIcons
	{
		public static string Dir(string layoutPath) => Path.Combine(Path.GetDirectoryName(layoutPath) ?? "", "touch_icons");

		public static bool ValidName(string n)
		{
			if (n.Length < 5 || n.Length > 48 || !n.EndsWith(".png", StringComparison.Ordinal))
				return false;
			for (int i = 0; i < n.Length - 4; i++)
				if (!char.IsLetterOrDigit(n[i]) && n[i] != '_')
					return false;
			return true;
		}

		// Apaga imagens que não estão mais no layout salvo (cancelamentos, ícones trocados ou removidos).
		public static void Cleanup(string layoutPath)
		{
			try
			{
				string dir = Dir(layoutPath);
				if (!Directory.Exists(dir))
					return;
				var keep = new HashSet<string>(StringComparer.Ordinal);
				if (File.Exists(layoutPath))
				{
					foreach (string line in File.ReadAllLines(layoutPath))
					{
						string[] kv = line.Split('=');
						if (kv.Length != 2 || !kv[0].Trim().StartsWith("shape_", StringComparison.Ordinal))
							continue;
						string[] v = kv[1].Split(',');
						if (v.Length == 3)
							keep.Add(v[2].Trim());
					}
				}
				string customFile = Path.Combine(Path.GetDirectoryName(layoutPath) ?? "", "custom_buttons.txt");
				if (File.Exists(customFile))
				{
					foreach (string line in File.ReadAllLines(customFile))
					{
						string[] kv = line.Split('=');
						if (kv.Length != 2)
							continue;
						string[] v = kv[1].Split(',');
						if (v.Length == 9)
							keep.Add(v[8].Trim());
					}
				}
				foreach (string file in Directory.GetFiles(dir))
				{
					if (!keep.Contains(Path.GetFileName(file)))
						File.Delete(file);
				}
				if (Directory.GetFiles(dir).Length == 0)
					Directory.Delete(dir);
			}
			catch (Exception)
			{
			}
		}
	}

	internal sealed class MaxHeightScrollView : ScrollView
	{
		public int MaxHeightPx { get; set; } = int.MaxValue;

		public MaxHeightScrollView(Context context) : base(context)
		{
		}

		protected MaxHeightScrollView(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
		{
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			base.OnMeasure(widthMeasureSpec, MeasureSpec.MakeMeasureSpec(MaxHeightPx, MeasureSpecMode.AtMost));
		}
	}

	internal sealed class ControlsCanvas : View
	{
		public const int Stick = 0, Jump = 1, Dash = 2, Grab = 3, Pause = 4, Tab = 5, Count = 6, Fps = 6, CustomBase = 7, MaxCustom = 12, Total = CustomBase + MaxCustom;

		public static string[] Names => L.ControlNames;
		private static readonly string[] Keys = { "stick", "jump", "dash", "grab", "pause", "tab" };
		private static readonly PixelButtonArt.Sprite?[] Sprites =
		{
			null, PixelButtonArt.Jump, PixelButtonArt.Dash, PixelButtonArt.Grab, PixelButtonArt.Pause, PixelButtonArt.Tab,
		};
		private readonly Bitmap?[] bitmaps = new Bitmap?[Count];

		private const float ButtonSize = 0.17f;
		private const float StickRadius = 0.13f;
		private const float StickX = 0.30f;
		private const float StickBottom = 0.28f;

		private readonly string path;
		private readonly float[] fx = new float[Total];
		private readonly float[] fy = new float[Total];
		private readonly float[] scale = new float[Total];
		private readonly int[] shape = new int[Total];
		private readonly float[] hscale = new float[Total];
		private readonly string?[] icon = new string?[Total];
		private readonly Bitmap?[] iconBmp = new Bitmap?[Total];
		private readonly int[][] iconPx = new int[Total][];

		public static string KeyOf(int i) => i < Count ? Keys[i] : "c" + (i - CustomBase + 1);

		// Botões criados pelo usuário (custom_buttons.txt), editáveis aqui como os demais.
		private List<CustomButton> customs = new();
		private readonly Dictionary<string, Bitmap> skinCache = new();
		private string CustomPath => Path.Combine(Path.GetDirectoryName(path) ?? "", "custom_buttons.txt");

		public bool IsCustom(int i) => i >= CustomBase && i < CustomBase + customs.Count;
		public bool IsShapeable(int i) => (i >= Jump && i <= Tab) || IsCustom(i);
		public bool ShapeSupported => IsShapeable(Selected);
		public int SelectedShape => ShapeSupported ? shape[Selected] : 0;
		public float SelectedScaleH => ShapeSupported && shape[Selected] == 2 ? hscale[Selected] : scale[Selected];
		public bool SelectedHasIcon => ShapeSupported && icon[Selected] != null;
		public int SelectedMinPct => IsCustom(Selected) ? 40 : 50;
		public int SelectedMaxPct => IsCustom(Selected) ? 250 : 200;
		public string SelectedName => Selected == Fps ? "FPS" : IsCustom(Selected) ? "#" + (Selected - CustomBase + 1) + " " + ButtonStyles.Display(customs[Selected - CustomBase].Key) : Names[Selected];
		private float MinScale(int i) => IsCustom(i) ? 0.4f : 0.5f;
		private float MaxScale(int i) => IsCustom(i) ? 2.5f : 2f;
		private readonly Paint paint = new(PaintFlags.AntiAlias);
		private bool ready;
		private bool isDefault = true;
		private bool dragging;
		private float dragDx, dragDy;

		private int fpsCorner;
		private float fpsMx, fpsMy;

		public bool FpsEnabled { get; set; }
		public Action? FpsChanged { get; set; }

		public int FpsCorner
		{
			get => fpsCorner;
			set { fpsCorner = Math.Clamp(value, 0, 3); isDefault = false; ClampFps(); Invalidate(); }
		}

		public float FpsMx
		{
			get => fpsMx;
			set { fpsMx = value; isDefault = false; ClampFps(); Invalidate(); }
		}

		public float FpsMy
		{
			get => fpsMy;
			set { fpsMy = value; isDefault = false; ClampFps(); Invalidate(); }
		}

		private int gridMode;

		public int GridMode
		{
			get => gridMode;
			set { gridMode = Math.Clamp(value, 0, 3); Invalidate(); }
		}

		private float GridStep => gridMode switch { 1 => Height / 12f, 2 => Height / 24f, 3 => Height / 48f, _ => 0f };
		private float Snap(float v) => GridStep > 0f ? MathF.Round(v / GridStep) * GridStep : v;

		public void SelectItem(int i)
		{
			if (Selected == i)
				return;
			Selected = i;
			SelectionChanged?.Invoke();
			Invalidate();
		}

		public int Selected { get; private set; } = Stick;
		public Action? SelectionChanged { get; set; }
		public float SelectedScale => scale[Selected];

		private int opacityPercent = GameOptions.DefaultOpacity;
		private bool dpadMode;
		public Action? LayoutLoaded { get; set; }

		public bool DpadMode
		{
			get => dpadMode;
			set
			{
				dpadMode = value;
				isDefault = false;
				Invalidate();
			}
		}

		public int OpacityPercent
		{
			get => opacityPercent;
			set
			{
				opacityPercent = Math.Clamp(value, 0, 100);
				Invalidate();
			}
		}

		private float PreviewAlpha => Math.Max(opacityPercent / 100f, 0.12f);

		public ControlsCanvas(Context context, string path) : base(context)
		{
			this.path = path;
			for (int i = 0; i <= Count; i++)
				scale[i] = 1f;
			for (int i = 0; i < Count; i++)
				hscale[i] = 1f;
		}

		protected ControlsCanvas(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
		{
			path = "";
		}

		private float Dp(float dp) => TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, Resources!.DisplayMetrics);

		private float Unit => Height * ButtonSize;
		private float BtnRadius(int i) => (i == Pause ? 0.32f : i == Tab ? 0.4f : 0.5f) * Unit * scale[i];
		private float StickRange => Height * StickRadius * scale[Stick];
		private float VisibleRadius(int i) => i == Stick ? StickRange * 1.1f : BtnRadius(i);
		private float BtnW(int i) => BtnRadius(i) * 2f;
		private float BtnH(int i) => (i == Pause ? 0.32f : i == Tab ? 0.4f : 0.5f) * Unit * 2f * (shape[i] == 2 ? hscale[i] : scale[i]);
		private float HalfW(int i) => i == Stick ? StickRange * 1.1f : BtnW(i) / 2f;
		private float HalfH(int i) => i == Stick ? StickRange * 1.1f : BtnH(i) / 2f;
		private bool Skinned(int i) => IsCustom(i) || (i >= Jump && i <= Tab && (shape[i] != 0 || iconBmp[i] != null));

		private float HitScore(int i, float x, float y)
		{
			float dx = x - fx[i] * Width, dy = y - fy[i] * Height;
			if (i == Stick)
				return MathF.Sqrt(dx * dx + dy * dy) / HitRadius(i);
			float nx = dx / (HalfW(i) * 1.25f), ny = dy / (HalfH(i) * 1.25f);
			return shape[i] != 0 ? Math.Max(Math.Abs(nx), Math.Abs(ny)) : MathF.Sqrt(nx * nx + ny * ny);
		}
		private float FpsCell => Math.Max(2f, Height * 0.0045f) * scale[Fps];
		private float FpsW => (6 * 6f - 1f) * FpsCell;
		private float FpsH => 7f * FpsCell;

		private RectF FpsRect()
		{
			float w = FpsW, h = FpsH;
			float x = (fpsCorner & 1) == 1 ? Width - fpsMx * Width - w : fpsMx * Width;
			float y = (fpsCorner & 2) == 2 ? Height - fpsMy * Height - h : fpsMy * Height;
			return new RectF(x, y, x + w, y + h);
		}

		private void ClampFps()
		{
			if (Width <= 0 || Height <= 0)
				return;
			fpsMx = Math.Clamp(fpsMx, 0f, Math.Max(0f, Math.Min(0.5f, (Width - FpsW) / Width)));
			fpsMy = Math.Clamp(fpsMy, 0f, Math.Max(0f, Math.Min(0.5f, (Height - FpsH) / Height)));
			FpsChanged?.Invoke();
		}

		private void MoveFps(float cx, float cy)
		{
			float w = FpsW, h = FpsH;
			fpsCorner = (cx > Width / 2f ? 1 : 0) | (cy > Height / 2f ? 2 : 0);
			float mxPx = (fpsCorner & 1) == 1 ? Width - (cx + w / 2f) : cx - w / 2f;
			float myPx = (fpsCorner & 2) == 2 ? Height - (cy + h / 2f) : cy - h / 2f;
			fpsMx = Snap(mxPx) / Width;
			fpsMy = Snap(myPx) / Height;
			isDefault = false;
			ClampFps();
		}

		private float HitRadius(int i) => i == Stick ? StickRange * 1.1f : BtnRadius(i) * 1.25f;

		protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
		{
			base.OnSizeChanged(w, h, oldw, oldh);
			if (ready || w <= 0 || h <= 0)
				return;
			ready = true;
			SetDefaults();
			LoadFile();
			LoadCustoms();
			LayoutLoaded?.Invoke();
			SelectionChanged?.Invoke();
		}

		private void SetDefaults()
		{
			float w = Width, h = Height, u = h * ButtonSize;
			Place(Stick, h * StickX, h * (1f - StickBottom));
			Place(Jump, w - 1.15f * u, h - 1.35f * u);
			Place(Dash, w - 2.45f * u, h - 0.95f * u);
			Place(Grab, w - 2.15f * u, h - 2.35f * u);
			Place(Pause, w - 0.8f * u, 0.8f * u);
			Place(Tab, w - 1.75f * u, 0.8f * u);
			for (int i = 0; i <= Count; i++)
				scale[i] = 1f;
			for (int i = 0; i < Count; i++)
			{
				shape[i] = 0;
				hscale[i] = 1f;
				DropIcon(i);
			}
			fpsCorner = 0;
			fpsMx = Math.Max(2f, h * 0.0045f) * 3f / w;
			fpsMy = Math.Max(2f, h * 0.0045f) * 3f / h;
			isDefault = true;
		}

		private void Place(int i, float px, float py)
		{
			fx[i] = px / Width;
			fy[i] = py / Height;
		}

		public void ResetToDefaults()
		{
			SetDefaults();
			ResetCustoms();
			Invalidate();
		}

		private void LoadFile()
		{
			try
			{
				if (!File.Exists(path))
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
							&& float.TryParse(fv[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float mx)
							&& float.TryParse(fv[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float my)
							&& float.TryParse(fv[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float fs))
						{
							fpsCorner = Math.Clamp(corner, 0, 3);
							fpsMx = Math.Clamp(mx, 0f, 0.5f);
							fpsMy = Math.Clamp(my, 0f, 0.5f);
							scale[Fps] = Math.Clamp(fs, 0.5f, 2f);
							isDefault = false;
						}
						continue;
					}
					string lineKey = kv[0].Trim();
					if (lineKey == "dpad")
					{
						dpadMode = kv[1].Trim() == "1";
						if (dpadMode)
							isDefault = false;
						continue;
					}
					if (lineKey.StartsWith("shape_", StringComparison.Ordinal))
					{
						LoadShapeLine(lineKey.Substring(6), kv[1]);
						continue;
					}
					int idx = Array.IndexOf(Keys, kv[0].Trim());
					string[] v = kv[1].Split(',');
					if (idx < 0 || v.Length != 3)
						continue;
					if (!float.TryParse(v[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
						|| !float.TryParse(v[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)
						|| !float.TryParse(v[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float s))
						continue;
					fx[idx] = Math.Clamp(x, 0f, 1f);
					fy[idx] = Math.Clamp(y, 0f, 1f);
					scale[idx] = Math.Clamp(s, 0.5f, 2f);
					isDefault = false;
				}
			}
			catch (Exception)
			{
			}
		}

		public void Save()
		{
			SaveCustoms();
			try
			{
				if (isDefault && !dpadMode)
				{
					if (File.Exists(path))
						File.Delete(path);
					return;
				}
				var sb = new StringBuilder();
				for (int i = 0; i < Count; i++)
					sb.Append(string.Format(CultureInfo.InvariantCulture, "{0}={1:0.0000},{2:0.0000},{3:0.00}\n", Keys[i], fx[i], fy[i], scale[i]));
				for (int i = Jump; i <= Tab; i++)
				{
					if (shape[i] == 0 && icon[i] == null)
						continue;
					sb.Append(string.Format(CultureInfo.InvariantCulture, "shape_{0}={1},{2:0.00},{3}\n", Keys[i], shape[i], shape[i] == 2 ? hscale[i] : scale[i], icon[i] ?? "-"));
				}
				sb.Append(string.Format(CultureInfo.InvariantCulture, "fps={0},{1:0.0000},{2:0.0000},{3:0.00}\n", fpsCorner, fpsMx, fpsMy, scale[Fps]));
				if (dpadMode)
					sb.Append("dpad=1\n");
				File.WriteAllText(path, sb.ToString());
			}
			catch (Exception)
			{
			}
		}

		private void LoadShapeLine(string key, string value)
		{
			int idx = Array.IndexOf(Keys, key);
			string[] v = value.Split(',');
			if (idx < Jump || idx > Tab || v.Length != 3)
				return;
			if (!int.TryParse(v[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int sh)
				|| !float.TryParse(v[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float hs))
				return;
			shape[idx] = Math.Clamp(sh, 0, 2);
			hscale[idx] = Math.Clamp(hs, 0.5f, 2f);
			string name = v[2].Trim();
			if (name != "-")
			{
				Bitmap? bmp = TryLoadIcon(name);
				if (bmp != null)
					AssignIcon(idx, name, bmp);
			}
			isDefault = false;
		}

		private Bitmap? TryLoadIcon(string name)
		{
			if (!TouchIcons.ValidName(name))
				return null;
			try
			{
				string file = Path.Combine(TouchIcons.Dir(path), name);
				return File.Exists(file) ? BitmapFactory.DecodeFile(file) : null;
			}
			catch (Exception)
			{
				return null;
			}
		}

		private void AssignIcon(int i, string name, Bitmap bmp)
		{
			iconBmp[i]?.Recycle();
			iconBmp[i] = bmp;
			icon[i] = name;
			var px = new int[bmp.Width * bmp.Height];
			bmp.GetPixels(px, 0, bmp.Width, 0, 0, bmp.Width, bmp.Height);
			iconPx[i] = px;
		}

		private void DropIcon(int i)
		{
			iconBmp[i]?.Recycle();
			iconBmp[i] = null;
			icon[i] = null;
			iconPx[i] = null;
		}

		public void SetSelectedShape(int s)
		{
			if (!ShapeSupported)
				return;
			int old = shape[Selected];
			shape[Selected] = Math.Clamp(s, 0, 2);
			if (shape[Selected] == 2 && old != 2)
			{
				float min = MinScale(Selected);
				float h = scale[Selected] * 0.65f;
				if (h < min)
				{
					h = min;
					scale[Selected] = Math.Min(MaxScale(Selected), min / 0.65f);
				}
				hscale[Selected] = h;
			}
			else if (shape[Selected] != 2)
			{
				hscale[Selected] = scale[Selected];
			}
			isDefault = false;
			ClampSelected();
			Invalidate();
		}

		public void SetSelectedScaleH(float s)
		{
			if (!ShapeSupported || shape[Selected] != 2)
				return;
			hscale[Selected] = Math.Clamp(s, MinScale(Selected), MaxScale(Selected));
			isDefault = false;
			ClampSelected();
			Invalidate();
		}

		public bool SetIcon(int i, string name)
		{
			if (!IsShapeable(i))
				return false;
			Bitmap? bmp = TryLoadIcon(name);
			if (bmp == null)
				return false;
			AssignIcon(i, name, bmp);
			isDefault = false;
			Invalidate();
			return true;
		}

		public void ClearIcon(int i)
		{
			if (!IsShapeable(i))
				return;
			DropIcon(i);
			isDefault = false;
			Invalidate();
		}

		private void LoadCustoms()
		{
			for (int i = CustomBase; i < Total; i++)
				DropIcon(i);
			customs = CustomButtons.Load(CustomPath);
			for (int k = 0; k < customs.Count; k++)
			{
				int i = CustomBase + k;
				CustomButton cb = customs[k];
				fx[i] = Math.Clamp(cb.X, 0f, 100f) / 100f;
				fy[i] = Math.Clamp(cb.Y, 0f, 100f) / 100f;
				scale[i] = Math.Clamp(cb.Size, 40, 250) / 100f;
				shape[i] = Math.Clamp(cb.Shape, 0, 2);
				hscale[i] = shape[i] == 2 ? Math.Clamp(cb.HSize > 0 ? cb.HSize : cb.Size, 40, 250) / 100f : scale[i];
				if (cb.Icon != null)
				{
					Bitmap? bmp = TryLoadIcon(cb.Icon);
					if (bmp != null)
						AssignIcon(i, cb.Icon, bmp);
				}
				ClampIndex(i);
			}
		}

		private void SaveCustoms()
		{
			try
			{
				if (customs.Count == 0)
					return;
				for (int k = 0; k < customs.Count; k++)
				{
					int i = CustomBase + k;
					CustomButton cb = customs[k];
					cb.X = (float)Math.Round(fx[i] * 100.0, 2);
					cb.Y = (float)Math.Round(fy[i] * 100.0, 2);
					cb.Size = (int)Math.Round(scale[i] * 100f);
					cb.Shape = shape[i];
					cb.HSize = (int)Math.Round((shape[i] == 2 ? hscale[i] : scale[i]) * 100f);
					cb.Icon = icon[i];
				}
				CustomButtons.Save(CustomPath, customs);
			}
			catch (Exception)
			{
			}
		}

		private void ResetCustoms()
		{
			for (int k = 0; k < customs.Count; k++)
			{
				int i = CustomBase + k;
				scale[i] = 1f;
				hscale[i] = 1f;
				shape[i] = 0;
				DropIcon(i);
				ClampIndex(i);
			}
		}

		public void SetSelectedScale(float s)
		{
			scale[Selected] = Math.Clamp(s, MinScale(Selected), MaxScale(Selected));
			if (ShapeSupported && shape[Selected] != 2)
				hscale[Selected] = scale[Selected];
			isDefault = false;
			ClampSelected();
			Invalidate();
		}

		private void ClampSelected()
		{
			if (Selected == Fps)
			{
				ClampFps();
				return;
			}
			ClampIndex(Selected);
		}

		private void ClampIndex(int i)
		{
			float mx = HalfW(i), my = HalfH(i);
			float px = Math.Clamp(fx[i] * Width, mx, Math.Max(mx, Width - mx));
			float py = Math.Clamp(fy[i] * Height, my, Math.Max(my, Height - my));
			fx[i] = px / Width;
			fy[i] = py / Height;
		}

		protected override void OnDraw(Canvas? canvas)
		{
			base.OnDraw(canvas);
			if (canvas == null || !ready)
				return;
			DrawGrid(canvas);
			if (FpsEnabled)
				DrawFpsItem(canvas);
			for (int i = 0; i < Count; i++)
				DrawItem(canvas, i);
			for (int k = 0; k < customs.Count; k++)
				DrawItem(canvas, CustomBase + k);
		}

		private void DrawGrid(Canvas canvas)
		{
			float g = GridStep;
			if (g <= 0f)
				return;
			paint.SetStyle(Paint.Style.Stroke!);
			paint.StrokeWidth = Math.Max(1f, Dp(0.75f));
			for (int k = 0; k * g <= Width; k++)
			{
				paint.Color = Color.Argb(k % 4 == 0 ? 80 : 34, 255, 255, 255);
				canvas.DrawLine(k * g, 0, k * g, Height, paint);
			}
			for (int k = 0; k * g <= Height; k++)
			{
				paint.Color = Color.Argb(k % 4 == 0 ? 80 : 34, 255, 255, 255);
				canvas.DrawLine(0, k * g, Width, k * g, paint);
			}
		}

		private static readonly Dictionary<char, string[]> FpsGlyphs = new()
		{
			['6'] = new[] { "00110", "01000", "10000", "11110", "10001", "10001", "01110" },
			['0'] = new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" },
			['F'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" },
			['P'] = new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" },
			['S'] = new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" },
		};

		private void DrawFpsGlyphs(Canvas canvas, float left, float top, float c, Color color)
		{
			paint.SetStyle(Paint.Style.Fill!);
			paint.Color = color;
			float x = left;
			foreach (char ch in "60 FPS")
			{
				if (FpsGlyphs.TryGetValue(ch, out string[]? rows))
					for (int y = 0; y < rows.Length; y++)
						for (int col = 0; col < rows[y].Length; col++)
							if (rows[y][col] == '1')
								canvas.DrawRect(x + col * c, top + y * c, x + (col + 1) * c, top + (y + 1) * c, paint);
				x += c * 6f;
			}
		}

		private void DrawFpsItem(Canvas canvas)
		{
			RectF r = FpsRect();
			float c = FpsCell;
			DrawFpsGlyphs(canvas, r.Left + c * 0.5f, r.Top + c * 0.5f, c, Color.Argb(190, 0, 0, 0));
			DrawFpsGlyphs(canvas, r.Left, r.Top, c, Color.White);

			float pad = Dp(6);
			paint.SetStyle(Paint.Style.Stroke!);
			paint.StrokeWidth = Dp(Selected == Fps ? 3 : 1.5f);
			paint.Color = Selected == Fps ? Color.ParseColor("#F2B8D8") : Color.Argb(110, 255, 255, 255);
			canvas.DrawRoundRect(new RectF(r.Left - pad, r.Top - pad, r.Right + pad, r.Bottom + pad), Dp(6), Dp(6), paint);
		}

		private float ButtonAlpha
		{
			get
			{
				float o = PreviewAlpha;
				return o <= 0.45f ? o / 0.45f * 0.9f : Math.Min(1f, 0.9f + (o - 0.45f) / 0.55f * 0.1f);
			}
		}

		private int A(float factor) => Math.Clamp((int)Math.Round(PreviewAlpha * factor * 255f), 0, 255);

		private void DrawItem(Canvas canvas, int i)
		{
			float cx = fx[i] * Width, cy = fy[i] * Height;
			if (i == Stick)
			{
				float range = StickRange;
				if (dpadMode)
				{
					float s = range * 0.71f, g = s * 1.05f;
					float[][] dirs = { new[] { 0f, -1f }, new[] { 1f, 0f }, new[] { 0f, 1f }, new[] { -1f, 0f } };
					for (int k = 0; k < 4; k++)
					{
						float px = cx + dirs[k][0] * g, py = cy + dirs[k][1] * g;
						paint.SetStyle(Paint.Style.Fill!);
						paint.Color = Color.Argb(A(0.12f), 255, 255, 255);
						canvas.DrawRoundRect(px - s / 2f, py - s / 2f, px + s / 2f, py + s / 2f, s * 0.1f, s * 0.1f, paint);
						paint.SetStyle(Paint.Style.Stroke!);
						paint.StrokeWidth = Math.Max(2f, s * 0.06f);
						paint.Color = Color.Argb(A(1f), 255, 255, 255);
						canvas.DrawRoundRect(px - s / 2f, py - s / 2f, px + s / 2f, py + s / 2f, s * 0.1f, s * 0.1f, paint);
						// triângulo apontando para a direção da seta
						float ts = s * 0.3f;
						float dx = dirs[k][0], dy = dirs[k][1];
						float tx = px + dx * ts * 0.4f, ty = py + dy * ts * 0.4f;
						var tri = new Android.Graphics.Path();
						tri.MoveTo(tx + dx * ts, ty + dy * ts);
						tri.LineTo(tx - dx * ts * 0.5f - dy * ts, ty - dy * ts * 0.5f + dx * ts);
						tri.LineTo(tx - dx * ts * 0.5f + dy * ts, ty - dy * ts * 0.5f - dx * ts);
						tri.Close();
						paint.SetStyle(Paint.Style.Fill!);
						paint.Color = Color.Argb(A(1.1f), 255, 255, 255);
						canvas.DrawPath(tri, paint);
					}
				}
				else
				{
					paint.SetStyle(Paint.Style.Stroke!);
					paint.StrokeWidth = range * 0.12f;
					paint.Color = Color.Argb(A(1f), 255, 255, 255);
					canvas.DrawCircle(cx, cy, range * 1.1f, paint);
					paint.SetStyle(Paint.Style.Fill!);
					paint.Color = Color.Argb(A(1.1f), 255, 255, 255);
					canvas.DrawCircle(cx, cy, range * 0.45f, paint);
				}
			}
			else if (Skinned(i))
				DrawSkinned(canvas, i, cx, cy);
			else
			{
				PixelButtonArt.Sprite sp = Sprites[i]!;
				int n = PixelButtonArt.Size;
				int cell = Math.Max(1, (int)Math.Round(BtnRadius(i) * 2f / n));
				float half = cell * n / 2f;
				int body = sp.GlowRgb;
				int cr = (body >> 16) & 255, cg = (body >> 8) & 255, cb = body & 255;
				float alpha = ButtonAlpha;

				int glowA = Math.Clamp((int)Math.Round(alpha * 0.55f * 255f), 0, 255);
				int glowColor = Color.Argb(glowA, cr, cg, cb).ToArgb();
				int glowClear = Color.Argb(0, cr, cg, cb).ToArgb();
				using (var shader = new RadialGradient(cx, cy, half * 1.35f, new[] { glowColor, glowColor, glowClear }, new[] { 0f, 0.7f, 1f }, Shader.TileMode.Clamp!))
				{
					paint.SetStyle(Paint.Style.Fill!);
					paint.Alpha = 255;
					paint.SetShader(shader);
					canvas.DrawCircle(cx, cy, half * 1.35f, paint);
					paint.SetShader(null);
				}

				paint.SetStyle(Paint.Style.Fill!);
				paint.FilterBitmap = false;
				paint.Alpha = Math.Clamp((int)Math.Round(alpha * 255f), 0, 255);
				canvas.DrawBitmap(GetBitmap(i), null, new RectF(cx - half, cy - half, cx + half, cy + half), paint);
				paint.Alpha = 255;
			}

			if (i == Selected)
			{
				paint.SetStyle(Paint.Style.Stroke!);
				paint.StrokeWidth = Dp(3);
				paint.Color = Color.ParseColor("#F2B8D8");
				if (i == Stick || (shape[i] == 0 && !Skinned(i)))
					canvas.DrawCircle(cx, cy, HalfW(i) + Dp(6), paint);
				else
				{
					float pad = Dp(6);
					var ring = new RectF(cx - HalfW(i) - pad, cy - HalfH(i) - pad, cx + HalfW(i) + pad, cy + HalfH(i) + pad);
					if (shape[i] == 0)
						canvas.DrawOval(ring, paint);
					else
					{
						float rr = Math.Min(HalfW(i), HalfH(i)) * 0.4f + pad;
						canvas.DrawRoundRect(ring, rr, rr, paint);
					}
				}
			}
		}

		private int[] PaletteOf(int i) => IsCustom(i) ? PixelButtonArt.PaletteFrom(customs[i - CustomBase].Rgb) : Sprites[i]!.Rgb;

		private string? LabelOfItem(int i)
		{
			if (IsCustom(i))
			{
				string l = PixelButtonArt.LabelFor(customs[i - CustomBase].Key);
				return l.Length > 0 ? l : null;
			}
			return i == Jump ? "A" : i == Dash ? "X" : i == Grab ? "G" : i == Tab ? "TAB" : null;
		}

		private float AlphaOf(int i)
		{
			if (!IsCustom(i))
				return ButtonAlpha;
			int op = customs[i - CustomBase].Opacity;
			if (op < 0)
				return ButtonAlpha;
			float o = op / 100f;
			return o <= 0.45f ? o / 0.45f * 0.9f : Math.Min(1f, 0.9f + (o - 0.45f) / 0.55f * 0.1f);
		}

		// Mesma pixel art do jogo (BuildSkin), em bitmap cacheado.
		private Bitmap SkinBitmap(int i, int cols, int rows, int cell)
		{
			int[] pal = PaletteOf(i);
			string? label = LabelOfItem(i);
			string key = i + "|" + shape[i] + "|" + cols + "x" + rows + "|" + cell + "|" + label + "|" + pal[2].ToString("X6") + "|" + icon[i];
			if (skinCache.TryGetValue(key, out Bitmap? hit))
				return hit;
			if (skinCache.Count > 40)
			{
				foreach (Bitmap old in skinCache.Values)
					old.Recycle();
				skinCache.Clear();
			}
			int[]? photo = iconPx[i];
			Bitmap? src = iconBmp[i];
			int[] argb = PixelButtonArt.BuildSkin(pal, shape[i], cols, rows, label, i == Pause, photo != null && src != null, out bool[] interior);
			int bw = cols, bh = rows;
			if (photo != null && src != null)
			{
				argb = PixelButtonArt.Compose(cols, rows, cell, argb, interior, photo, src.Width, src.Height, pal[2]);
				bw = cols * cell;
				bh = rows * cell;
			}
			Bitmap bmp = Bitmap.CreateBitmap(argb, bw, bh, Bitmap.Config.Argb8888!)!;
			skinCache[key] = bmp;
			return bmp;
		}

		// Foto original no formato do botão: sem moldura, brilho nem pixelização.
		private void DrawPhoto(Canvas canvas, int i, float cx, float cy)
		{
			Bitmap bmp = iconBmp[i]!;
			float w = BtnW(i), h = BtnH(i);
			var rect = new RectF(cx - w / 2f, cy - h / 2f, cx + w / 2f, cy + h / 2f);
			float k = Math.Max(w / bmp.Width, h / bmp.Height);
			using var matrix = new Matrix();
			matrix.SetScale(k, k);
			matrix.PostTranslate(cx - bmp.Width * k / 2f, cy - bmp.Height * k / 2f);
			using var shader = new BitmapShader(bmp, Shader.TileMode.Clamp!, Shader.TileMode.Clamp!);
			shader.SetLocalMatrix(matrix);
			paint.AntiAlias = true;
			paint.FilterBitmap = true;
			paint.SetStyle(Paint.Style.Fill!);
			paint.SetShader(shader);
			paint.Alpha = Math.Clamp((int)Math.Round(AlphaOf(i) * 255f), 0, 255);
			if (shape[i] == 0)
			{
				canvas.DrawOval(rect, paint);
			}
			else
			{
				float r = Math.Min(w, h) * 0.12f;
				canvas.DrawRoundRect(rect, r, r, paint);
			}
			paint.SetShader(null);
			paint.Alpha = 255;
		}

		private void DrawSkinned(Canvas canvas, int i, float cx, float cy)
		{
			if (iconBmp[i] != null)
			{
				DrawPhoto(canvas, i, cx, cy);
				return;
			}
			PixelButtonArt.Snap(BtnW(i), BtnH(i), out int cols, out int rows, out int cell);
			Bitmap bmp = SkinBitmap(i, cols, rows, cell);
			float w = cols * cell, h = rows * cell;
			float alpha = AlphaOf(i);
			int body = PaletteOf(i)[2];
			int cr = (body >> 16) & 255, cg = (body >> 8) & 255, cb = body & 255;

			float k = shape[i] == 0 ? 1.35f : 1.6f;
			float radius = h * k / 2f;
			int glowA = Math.Clamp((int)Math.Round(alpha * 0.55f * 255f), 0, 255);
			int glowColor = Color.Argb(glowA, cr, cg, cb).ToArgb();
			int glowClear = Color.Argb(0, cr, cg, cb).ToArgb();
			using (var shader = new RadialGradient(0f, 0f, radius, new[] { glowColor, glowColor, glowClear }, new[] { 0f, 0.7f, 1f }, Shader.TileMode.Clamp!))
			{
				canvas.Save();
				canvas.Translate(cx, cy);
				canvas.Scale(w / h, 1f);
				paint.SetStyle(Paint.Style.Fill!);
				paint.SetShader(shader);
				canvas.DrawCircle(0f, 0f, radius, paint);
				paint.SetShader(null);
				canvas.Restore();
			}

			paint.SetStyle(Paint.Style.Fill!);
			paint.FilterBitmap = false;
			paint.Alpha = Math.Clamp((int)Math.Round(alpha * 255f), 0, 255);
			canvas.DrawBitmap(bmp, null, new RectF(cx - w / 2f, cy - h / 2f, cx + w / 2f, cy + h / 2f), paint);
			paint.Alpha = 255;
		}

		private Bitmap GetBitmap(int i)
		{
			if (bitmaps[i] != null)
				return bitmaps[i]!;
			PixelButtonArt.Sprite sp = Sprites[i]!;
			int n = PixelButtonArt.Size;
			int[] px = new int[n * n];
			for (int y = 0; y < n; y++)
			{
				for (int x = 0; x < n; x++)
				{
					int rgb = sp.ColorOf(sp.Rows[y][x]);
					px[y * n + x] = rgb < 0 ? 0 : Color.Argb(255, (rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255).ToArgb();
				}
			}
			Bitmap bmp = Bitmap.CreateBitmap(px, n, n, Bitmap.Config.Argb8888!)!;
			bitmaps[i] = bmp;
			return bmp;
		}

		private int HitTest(float x, float y)
		{
			int best = -1;
			float bestScore = float.MaxValue;
			for (int i = 0; i < Total; i++)
			{
				if (i == Fps || (i > Fps && !IsCustom(i)))
					continue;
				float score = HitScore(i, x, y);
				if (score <= 1f && score < bestScore)
				{
					bestScore = score;
					best = i;
				}
			}
			if (best < 0 && FpsEnabled)
			{
				RectF r = FpsRect();
				float pad = Dp(14);
				if (x >= r.Left - pad && x <= r.Right + pad && y >= r.Top - pad && y <= r.Bottom + pad)
					return Fps;
			}
			return best;
		}

		public override bool OnTouchEvent(MotionEvent? e)
		{
			if (e == null)
				return false;
			switch (e.ActionMasked)
			{
				case MotionEventActions.Down:
				{
					int hit = HitTest(e.GetX(), e.GetY());
					if (hit < 0)
						return true;
					Selected = hit;
					dragging = true;
					if (hit == Fps)
					{
						RectF r = FpsRect();
						dragDx = (r.Left + r.Right) / 2f - e.GetX();
						dragDy = (r.Top + r.Bottom) / 2f - e.GetY();
					}
					else
					{
						dragDx = fx[hit] * Width - e.GetX();
						dragDy = fy[hit] * Height - e.GetY();
					}
					SelectionChanged?.Invoke();
					Invalidate();
					return true;
				}
				case MotionEventActions.Move:
					if (dragging)
					{
						float px = e.GetX() + dragDx, py = e.GetY() + dragDy;
						if (Selected == Fps)
							MoveFps(px, py);
						else
						{
							fx[Selected] = Snap(px) / Width;
							fy[Selected] = Snap(py) / Height;
							isDefault = false;
							ClampSelected();
						}
						Invalidate();
					}
					return true;
				case MotionEventActions.Up:
				case MotionEventActions.Cancel:
					dragging = false;
					return true;
			}
			return true;
		}
	}
}
