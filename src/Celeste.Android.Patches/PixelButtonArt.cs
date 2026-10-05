using System;

namespace CelesteAndroid
{
	/// <summary>
	/// Arte pixel art dos botões de toque (círculo de 28x28 "pixels" com glifo embutido). Sem dependências do FNA nem do
	/// Android: o jogo (TouchControls) e o editor de layout (ControlsEditorActivity) montam a textura a partir daqui.
	/// Legenda: . transparente | O contorno | D borda escura | B corpo | L brilho | S reflexo | W glifo | K sombra do glifo.
	/// </summary>
	public static class PixelButtonArt
	{
		public const int Size = 28;

		public sealed class Sprite
		{
			public readonly string[] Rows;
			public readonly int[] Rgb; // 0xRRGGBB, na ordem: O D B L S W K
			public Sprite(string[] rows, int[] rgb) { Rows = rows; Rgb = rgb; }

			/// <summary>Cor 0xRRGGBB do caractere, ou -1 se for transparente.</summary>
			public int ColorOf(char ch)
			{
				int i = "ODBLSWK".IndexOf(ch);
				return i < 0 ? -1 : Rgb[i];
			}

			/// <summary>Cor do brilho/halo ao redor do botão (a cor do corpo).</summary>
			public int GlowRgb => Rgb[2];
		}


		public static readonly Sprite Jump = new(
			new[]
			{
				"...........OOOOOO...........",
				"........OOOOLLLLOOOO........",
				"......OOOLLLLLLLLLLOOO......",
				".....OOLLLBBBBBBBBBBLOO.....",
				"....OOLLBBBBBBBBBBBBBBOO....",
				"...OOLLBSSSBBBBBBBBBBBDOO...",
				"..OOLLBSSBBBBBBBBBBBBBBDOO..",
				"..OLLBSSBBBWWWWWWBBBBBBBDO..",
				".OOLBBSBBBBWWWWWWKBBBBBBDOO.",
				".OLLBBBBBWWBKKKKKWWBBBBBDDO.",
				".OLBBBBBBWWKBBBBBWWKBBBBDDO.",
				"OOLBBBBBBWWKBBBBBWWKBBBBBDOO",
				"OLLBBBBBBWWKBBBBBWWKBBBBBDDO",
				"OLLBBBBBBWWWWWWWWWWKBBBBBDDO",
				"OLLBBBBBBWWWWWWWWWWKBBBBBDDO",
				"OLLBBBBBBWWKKKKKKWWKBBBBDDDO",
				"OOLBBBBBBWWKBBBBBWWKBBBBDDOO",
				".OLBBBBBBWWKBBBBBWWKBBBBDDO.",
				".OLBBBBBBWWKBBBBBWWKBBBDDDO.",
				".OOBBBBBBWWKBBBBBWWKBBBDDOO.",
				"..OLBBBBBWWKBBBBBWWKBBDDDO..",
				"..OOBBBBBBKKBBBBBBKKBDDDOO..",
				"...OODBBBBBBBBBBBBBBDDDOO...",
				"....OODBBBBBBBBBBBDDDDOO....",
				".....OODDDDBBBBDDDDDDOO.....",
				"......OOODDDDDDDDDDOOO......",
				"........OOOODDDDOOOO........",
				"...........OOOOOO...........",
			},
			new[] { 0x032A17, 0x428A5C, 0x58A971, 0x76D08C, 0xA8ECB8, 0xF6F6F6, 0x1E5C3A });

		public static readonly Sprite Dash = new(
			new[]
			{
				"...........OOOOOO...........",
				"........OOOOLLLLOOOO........",
				"......OOOLLLLLLLLLLOOO......",
				".....OOLLLBBBBBBBBBBLOO.....",
				"....OOLLBBBBBBBBBBBBBBOO....",
				"...OOLLBSSSBBBBBBBBBBBDOO...",
				"..OOLLBSSBBBBBBBBBBBBBBDOO..",
				"..OLLBSSBWWBBBBBBWWBBBBBDO..",
				".OOLBBSBBWWKBBBBBWWKBBBBDOO.",
				".OLLBBBBBWWKBBBBBWWKBBBBDDO.",
				".OLBBBBBBWWKBBBBBWWKBBBBDDO.",
				"OOLBBBBBBBKWWBBWWBKKBBBBBDOO",
				"OLLBBBBBBBBWWKBWWKBBBBBBBDDO",
				"OLLBBBBBBBBBKWWBKKBBBBBBBDDO",
				"OLLBBBBBBBBBBWWKBBBBBBBBBDDO",
				"OLLBBBBBBBBWWBKWWBBBBBBBDDDO",
				"OOLBBBBBBBBWWKBWWKBBBBBBDDOO",
				".OLBBBBBBWWBKKBBKWWBBBBBDDO.",
				".OLBBBBBBWWKBBBBBWWKBBBDDDO.",
				".OOBBBBBBWWKBBBBBWWKBBBDDOO.",
				"..OLBBBBBWWKBBBBBWWKBBDDDO..",
				"..OOBBBBBBKKBBBBBBKKBDDDOO..",
				"...OODBBBBBBBBBBBBBBDDDOO...",
				"....OODBBBBBBBBBBBDDDDOO....",
				".....OODDDDBBBBDDDDDDOO.....",
				"......OOODDDDDDDDDDOOO......",
				"........OOOODDDDOOOO........",
				"...........OOOOOO...........",
			},
			new[] { 0x3E0321, 0xA5486C, 0xC86385, 0xE08CA8, 0xF8C0D2, 0xF6F6F6, 0x782448 });

		public static readonly Sprite Grab = new(
			new[]
			{
				"...........OOOOOO...........",
				"........OOOOLLLLOOOO........",
				"......OOOLLLLLLLLLLOOO......",
				".....OOLLLBBBBBBBBBBLOO.....",
				"....OOLLBBBBBBBBBBBBBBOO....",
				"...OOLLBSSSBBBBBBBBBBBDOO...",
				"..OOLLBSSBBBBBBBBBBBBBBDOO..",
				"..OLLBSSBBBWWWWWWWWBBBBBDO..",
				".OOLBBSBBBBWWWWWWWWKBBBBDOO.",
				".OLLBBBBBWWBKKKKKKKKBBBBDDO.",
				".OLBBBBBBWWKBBBBBBBBBBBBDDO.",
				"OOLBBBBBBWWKBBBBBBBBBBBBBDOO",
				"OLLBBBBBBWWKBBBBBBBBBBBBBDDO",
				"OLLBBBBBBWWKBWWWWWWBBBBBBDDO",
				"OLLBBBBBBWWKBWWWWWWKBBBBBDDO",
				"OLLBBBBBBWWKBBKKKWWKBBBBDDDO",
				"OOLBBBBBBWWKBBBBBWWKBBBBDDOO",
				".OLBBBBBBWWKBBBBBWWKBBBBDDO.",
				".OLBBBBBBWWKBBBBBWWKBBBDDDO.",
				".OOBBBBBBBKWWWWWWWWKBBBDDOO.",
				"..OLBBBBBBBWWWWWWWWKBBDDDO..",
				"..OOBBBBBBBBKKKKKKKKBDDDOO..",
				"...OODBBBBBBBBBBBBBBDDDOO...",
				"....OODBBBBBBBBBBBDDDDOO....",
				".....OODDDDBBBBDDDDDDOO.....",
				"......OOODDDDDDDDDDOOO......",
				"........OOOODDDDOOOO........",
				"...........OOOOOO...........",
			},
			new[] { 0x020C51, 0x304488, 0x5C85C0, 0x84A6D4, 0xB0CAE8, 0xF6F6F6, 0x24388C });

		public static readonly Sprite Tab = new(
			new[]
			{
				"...........OOOOOO...........",
				"........OOOOLLLLOOOO........",
				"......OOOLLLLLLLLLLOOO......",
				".....OOLLLBBBBBBBBBBLOO.....",
				"....OOLLBBBBBBBBBBBBBBOO....",
				"...OOLLBSSSBBBBBBBBBBBDOO...",
				"..OOLLBSSBBBBBBBBBBBBBBDOO..",
				"..OLLBSSBBBBBBBBBBBBBBBBDO..",
				".OOLBBSBBBBBBBBBBBBBBBBBDOO.",
				".OLLWWWWWWBBBWWBBBWWWWBBDDO.",
				".OLBWWWWWWKBBWWKBBWWWWKBDDO.",
				"OOLBBKWWKKKWWBKWWBWWKKWWBDOO",
				"OLLBBBWWKBBWWKBWWKWWKBWWKDDO",
				"OLLBBBWWKBBWWWWWWKWWWWBKKDDO",
				"OLLBBBWWKBBWWWWWWKWWWWKBBDDO",
				"OLLBBBWWKBBWWKKWWKWWKKWWDDDO",
				"OOLBBBWWKBBWWKBWWKWWKBWWKDOO",
				".OLBBBWWKBBWWKBWWKWWWWBKKDO.",
				".OLBBBWWKBBWWKBWWKWWWWKDDDO.",
				".OOBBBBKKBBBKKBBKKBKKKKDDOO.",
				"..OLBBBBBBBBBBBBBBBBBBDDDO..",
				"..OOBBBBBBBBBBBBBBBBBDDDOO..",
				"...OODBBBBBBBBBBBBBBDDDOO...",
				"....OODBBBBBBBBBBBDDDDOO....",
				".....OODDDDBBBBDDDDDDOO.....",
				"......OOODDDDDDDDDDOOO......",
				"........OOOODDDDOOOO........",
				"...........OOOOOO...........",
			},
			new[] { 0x4A2600, 0xB07628, 0xE0AC48, 0xF2CC74, 0xFFE8A8, 0xFAF6F0, 0x965C18 });

		public static readonly Sprite Pause = new(
			new[]
			{
				"...........OOOOOO...........",
				"........OOOOLLLLOOOO........",
				"......OOOLLLLLLLLLLOOO......",
				".....OOLLLBBBBBBBBBBLOO.....",
				"....OOLLBBBBBBBBBBBBBBOO....",
				"...OOLLBSSSBBBBBBBBBBBDOO...",
				"..OOLLBSSBBBBBBBBBBBBBBDOO..",
				"..OLLBSSBBBBBBBBBBBBBBBBDO..",
				".OOLBBSBBBBBBBBBBBBBBBBBDOO.",
				".OLLBBBBBWWWBBBBBWWWBBBBDDO.",
				".OLBBBBBBWWWKBBBBWWWKBBBDDO.",
				"OOLBBBBBBWWWKBBBBWWWKBBBBDOO",
				"OLLBBBBBBWWWKBBBBWWWKBBBBDDO",
				"OLLBBBBBBWWWKBBBBWWWKBBBBDDO",
				"OLLBBBBBBWWWKBBBBWWWKBBBBDDO",
				"OLLBBBBBBWWWKBBBBWWWKBBBDDDO",
				"OOLBBBBBBWWWKBBBBWWWKBBBDDOO",
				".OLBBBBBBWWWKBBBBWWWKBBBDDO.",
				".OLBBBBBBWWWKBBBBWWWKBBDDDO.",
				".OOBBBBBBBKKKBBBBBKKKBBDDOO.",
				"..OLBBBBBBBBBBBBBBBBBBDDDO..",
				"..OOBBBBBBBBBBBBBBBBBDDDOO..",
				"...OODBBBBBBBBBBBBBBDDDOO...",
				"....OODBBBBBBBBBBBDDDDOO....",
				".....OODDDDBBBBDDDDDDOO.....",
				"......OOODDDDDDDDDDOOO......",
				"........OOOODDDDOOOO........",
				"...........OOOOOO...........",
			},
			new[] { 0x1C1C2C, 0x78788A, 0xB0B0C0, 0xD2D2E0, 0xF0F0F8, 0xFAFAFA, 0x606074 });
	}
}
