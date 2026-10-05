using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using MonoMod;

namespace CelesteAndroid.Patcher
{
	/// <summary>
	/// Gera um Celeste.dll que roda no .NET moderno a partir do Celeste.exe original do usuário.
	/// </summary>
	public static class CelestePatcher
	{
		/// <param name="celesteExe">Celeste.exe original (build FNA).</param>
		/// <param name="modAssembly">Celeste.Android.mm.dll.</param>
		/// <param name="outputDll">Destino do Celeste.dll patcheado.</param>
		/// <param name="dependencyDirs">Onde achar FNA.dll, Steamworks.NET.dll etc. (os nossos, não os do jogo).</param>
		public static void Patch(string celesteExe, string modAssembly, string outputDll, IEnumerable<string> dependencyDirs, Action<string>? log = null)
		{
			log ??= Console.WriteLine;
			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputDll))!);

			using LoggingModder modder = new(log)
			{
				InputPath = celesteExe,
				OutputPath = outputDll,
				// Deferred: o Cecil só resolve o que usa. No Android não há mscorlib/System.Runtime como
				// arquivos, e o modo Immediate tenta resolver os tipos de todos os atributos.
				ReadingMode = ReadingMode.Deferred,
				MissingDependencyThrow = false,
			};
			modder.DependencyDirs.AddRange(dependencyDirs);

			modder.Read();
			modder.ReadMod(modAssembly);
			modder.MapDependencies();
			modder.AutoPatch();

			// O Celeste.exe vem marcado como x86 (32BITREQUIRED); em ARM64/x64 isso impede o carregamento.
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
