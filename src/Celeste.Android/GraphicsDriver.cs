using System;
using System.Collections.Generic;
using System.IO;
using Android.Content;
using Android.OS;

namespace CelesteAndroid
{
	public static class GraphicsDriver
	{
		public const string OpenGL = "OpenGL";
		public const string Vulkan = "Vulkan";
		private const string PrefDriver = "driver";

		private static string MarkerPath(Context context) => Path.Combine(context.FilesDir!.AbsolutePath, "launch_pending.txt");

		public static bool IsMediaTek { get; } = DetectMediaTek();

		private static bool DetectMediaTek()
		{
			try
			{
				var fields = new List<string?> { Build.Hardware, Build.Board };
				if (OperatingSystem.IsAndroidVersionAtLeast(31))
				{
					fields.Add(Build.SocManufacturer);
					fields.Add(Build.SocModel);
				}
				foreach (string? field in fields)
				{
					string s = (field ?? "").ToLowerInvariant();
					if (s.Contains("mediatek") || (s.Length > 2 && s.StartsWith("mt") && char.IsDigit(s[2])))
						return true;
				}
			}
			catch (Exception)
			{
			}
			return false;
		}

		public static string Effective(ISharedPreferences prefs)
		{
			string stored = prefs.GetString(PrefDriver, "") ?? "";
			if (stored == OpenGL || stored == Vulkan)
				return stored;
			return IsMediaTek ? OpenGL : Vulkan;
		}

		public static void Set(ISharedPreferences prefs, string driver) =>
			prefs.Edit()!.PutString(PrefDriver, driver)!.Apply();

		public static void Toggle(ISharedPreferences prefs) =>
			Set(prefs, Effective(prefs) == OpenGL ? Vulkan : OpenGL);

		public static bool PrepareLaunch(Context context, ISharedPreferences prefs)
		{
			bool switched = false;
			try
			{
				string path = MarkerPath(context);
				if (File.Exists(path))
				{
					string previous = File.ReadAllText(path).Trim();
					File.Delete(path);
					if (previous == Vulkan && Effective(prefs) == Vulkan)
					{
						Set(prefs, OpenGL);
						switched = true;
					}
				}
				File.WriteAllText(path, Effective(prefs));
			}
			catch (Exception)
			{
			}
			return switched;
		}

		public static void MarkLaunchOk(Context context)
		{
			try
			{
				File.Delete(MarkerPath(context));
			}
			catch (Exception)
			{
			}
		}
	}
}
