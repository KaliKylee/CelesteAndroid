using System;
using Android.App;
using Android.Graphics;
using Android.Views;
using Android.Widget;

namespace CelesteAndroid
{
	public static class HapticsSettings
	{
		public static void Show(Activity activity, Action dismissed)
		{
			var prefs = GameOptions.Prefs(activity);
			var saved = GameOptions.Vibration(prefs);
			var preview = new TouchVibration(activity);
			int Dp(int n) => (int)(n * activity.Resources!.DisplayMetrics!.Density + 0.5f);
			var accent = Color.ParseColor("#F2B8D8");
			var content = new LinearLayout(activity) { Orientation = Orientation.Vertical };
			content.SetPadding(Dp(24), Dp(12), Dp(24), Dp(12));
			TextView Label(string text)
			{
				var label = new TextView(activity) { Text = text };
				label.SetTextSize(Android.Util.ComplexUnitType.Sp, 14);
				label.SetTextColor(accent);
				label.SetPadding(0, Dp(8), 0, Dp(8));
				return label;
			}
			var enabled = new Switch(activity) { Text = L.VibrationEnabled, Checked = saved.Enabled };
			content.AddView(enabled);
			content.AddView(Label(L.VibrationWhen));
			var modes = new RadioGroup(activity) { Orientation = Orientation.Vertical };
			string[] labels = { L.VibrationBoth, L.VibrationPress, L.VibrationRelease };
			var choices = new RadioButton[3];
			for (int i = 0; i < choices.Length; i++)
			{
				choices[i] = new RadioButton(activity) { Id = View.GenerateViewId(), Text = labels[i] };
				modes.AddView(choices[i]);
			}
			modes.Check(choices[saved.Mode].Id);
			content.AddView(modes);
			var intensityLabel = Label("");
			content.AddView(intensityLabel);
			var intensity = new SeekBar(activity) { Max = 100, Progress = saved.Intensity,
				ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(accent),
				ThumbTintList = Android.Content.Res.ColorStateList.ValueOf(accent) };
			content.AddView(intensity, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, Dp(48)));
			var joystick = new Switch(activity) { Text = L.VibrationJoystick, Checked = saved.Joystick };
			content.AddView(joystick);
			var test = new Button(activity) { Text = L.VibrationTest, HapticFeedbackEnabled = false };
			content.AddView(test);
			content.AddView(Label(!preview.Available ? L.VibrationUnavailable
				: preview.AmplitudeControl ? L.VibrationHint : L.VibrationDurationHint));

			HapticPolicy Current() => new(enabled.Checked, Array.FindIndex(choices, b => b.Checked), intensity.Progress, joystick.Checked);
			void Update()
			{
				intensityLabel.Text = $"{L.VibrationIntensity}: {intensity.Progress}%";
				foreach (var choice in choices) choice.Enabled = enabled.Checked;
				intensity.Enabled = joystick.Enabled = enabled.Checked;
				test.Enabled = enabled.Checked && intensity.Progress > 0 && preview.Available;
				if (!enabled.Checked || intensity.Progress == 0) preview.Cancel();
			}
			enabled.CheckedChange += (_, _) => Update();
			intensity.ProgressChanged += (_, _) => Update();
			bool touchClick = false;
			bool open = true;
			test.Touch += (_, e) =>
			{
				if (e.Event?.Action == MotionEventActions.Down) { touchClick = true; preview.Play(1, Current()); }
				else if (e.Event?.Action == MotionEventActions.Up) preview.Play(2, Current());
				else if (e.Event?.Action == MotionEventActions.Cancel) { touchClick = false; preview.Cancel(); }
				// Keep normal Button touch handling and accessibility click behavior.
				e.Handled = false;
			};
			test.Click += (_, _) =>
			{
				if (touchClick) { touchClick = false; return; }
				// Accessibility activation has no MotionEvent: preview both permitted edges.
				preview.Play(1, Current());
				test.PostDelayed(() => { if (open && activity.HasWindowFocus) preview.Play(2, Current()); }, 80);
			};
			Update();
			var scroll = new ScrollView(activity);
			scroll.AddView(content);
			var dialog = new AlertDialog.Builder(activity)!.SetTitle(L.Vibration)!.SetView(scroll)!
				.SetPositiveButton(L.Save, (_, _) => GameOptions.SetVibration(prefs, Current()))!
				.SetNegativeButton(L.Cancel, (_, _) => { })!.Create()!;
			dialog.DismissEvent += (_, _) => { open = false; preview.Cancel(); dismissed(); };
			dialog.Show();
		}
	}
}
