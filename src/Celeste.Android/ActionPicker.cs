using System;
using Android.App;
using Android.Content;
using Android.Text;
using Android.Views;
using Android.Widget;

namespace CelesteAndroid
{
	// Diálogos compartilhados: escolher a ação de um botão (gamepad ou tecla) e o texto exibido nele.
	internal static class ActionPicker
	{
		public static void Show(Activity activity, string current, Action<string> onPicked, Action? onClosed = null)
		{
			string[] top = { "🎮  " + L.GamepadButton, "⌨  " + L.KeyboardKey };
			var dialog = new AlertDialog.Builder(activity)!
				.SetTitle(L.ButtonAction)!
				.SetItems(top, (_, e) =>
				{
					if (e.Which == 0)
						PickPad(activity, current, onPicked, onClosed);
					else
						PickKey(activity, current, onPicked, onClosed);
				})!
				.SetNegativeButton(L.Cancel, (IDialogInterfaceOnClickListener?)null)!
				.Create()!;
			dialog.DismissEvent += (_, _) => onClosed?.Invoke();
			dialog.Show();
		}

		public static void ShowKey(Activity activity, string current, Action<string> onPicked, Action? onClosed = null)
			=> PickKey(activity, current, onPicked, onClosed);

		private static void PickPad(Activity activity, string current, Action<string> onPicked, Action? onClosed)
		{
			string[] ids = PixelButtonArt.PadIds;
			string[] labels = new string[ids.Length];
			for (int i = 0; i < ids.Length; i++)
				labels[i] = PixelButtonArt.PadName(ids[i]);
			int cur = current.StartsWith("pad:", StringComparison.Ordinal) ? Math.Max(0, Array.IndexOf(ids, current.Substring(4))) : -1;
			var dialog = new AlertDialog.Builder(activity)!
				.SetTitle(L.GamepadButton)!
				.SetSingleChoiceItems(labels, cur, (IDialogInterfaceOnClickListener?)null)!
				.SetNegativeButton(L.Cancel, (IDialogInterfaceOnClickListener?)null)!
				.Create()!;
			dialog.DismissEvent += (_, _) => onClosed?.Invoke();
			dialog.Show();
			dialog.ListView!.ItemClick += (_, e) =>
			{
				onPicked("pad:" + ids[e.Position]);
				dialog.Dismiss();
			};
		}

		private static void PickKey(Activity activity, string current, Action<string> onPicked, Action? onClosed)
		{
			string[] keys = ButtonStyles.KeyList;
			string[] labels = new string[keys.Length];
			for (int i = 0; i < labels.Length; i++)
				labels[i] = ButtonStyles.Display(keys[i]);
			int cur = Math.Max(0, Array.IndexOf(keys, current));
			var dialog = new AlertDialog.Builder(activity)!
				.SetTitle(L.KeyboardKey)!
				.SetSingleChoiceItems(labels, cur, (IDialogInterfaceOnClickListener?)null)!
				.SetNegativeButton(L.Cancel, (IDialogInterfaceOnClickListener?)null)!
				.Create()!;
			dialog.DismissEvent += (_, _) => onClosed?.Invoke();
			dialog.Show();
			dialog.ListView!.ItemClick += (_, e) =>
			{
				onPicked(keys[e.Position]);
				dialog.Dismiss();
			};
		}

		// Texto do botão: A-Z e 0-9, até 4 caracteres (a fonte pixel do jogo). Vazio = automático.
		public static void ShowText(Activity activity, string? current, Action<string?> onPicked, Action? onClosed = null)
		{
			var edit = new EditText(activity) { Text = current ?? "" };
			edit.SetSingleLine(true);
			edit.InputType = InputTypes.ClassText | InputTypes.TextFlagCapCharacters | InputTypes.TextFlagNoSuggestions;
			edit.SetFilters(new IInputFilter[] { new InputFilterLengthFilter(4) });
			edit.SetSelection(edit.Text!.Length);
			var dialog = new AlertDialog.Builder(activity)!
				.SetTitle(L.ButtonText)!
				.SetView(edit)!
				.SetPositiveButton(L.Save, (_, _) => onPicked(PixelButtonArt.CleanLabel(edit.Text)))!
				.SetNeutralButton(L.TextAuto, (_, _) => onPicked(null))!
				.SetNegativeButton(L.Cancel, (IDialogInterfaceOnClickListener?)null)!
				.Create()!;
			dialog.DismissEvent += (_, _) => onClosed?.Invoke();
			dialog.Show();
		}
	}
}
