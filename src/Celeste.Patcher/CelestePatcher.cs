using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using MonoMod;

namespace CelesteAndroid.Patcher
{
	public static class CelestePatcher
	{
		public static void Patch(string celesteExe, string modAssembly, string outputDll, IEnumerable<string> dependencyDirs, Action<string>? log = null)
		{
			log ??= Console.WriteLine;
			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputDll))!);

			using LoggingModder modder = new(log)
			{
				InputPath = celesteExe,
				OutputPath = outputDll,
				ReadingMode = ReadingMode.Deferred,
				MissingDependencyThrow = false,
			};
			modder.DependencyDirs.AddRange(dependencyDirs);

			modder.Read();
			modder.ReadMod(modAssembly);
			modder.MapDependencies();
			modder.AutoPatch();

			ModuleDefinition module = modder.Module;
			module.Attributes &= ~(ModuleAttributes.Required32Bit | ModuleAttributes.Preferred32Bit);
			module.Attributes |= ModuleAttributes.ILOnly;
			module.Architecture = TargetArchitecture.I386;

			modder.Write();
			log($"Celeste patcheado: {outputDll}");
		}

		private sealed class LoggingModder(Action<string> log) : MonoModder
		{
			public override void Log(string text) => log("[MonoMod] " + text);
		}
	}
}
