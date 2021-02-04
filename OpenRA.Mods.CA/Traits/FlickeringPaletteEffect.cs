#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class FlickeringPaletteEffectInfo : TraitInfo
	{
		[Desc("The palette to apply this effect to.")]
		[PaletteReference]
		[FieldLoader.Require]
		public readonly string PaletteName;

		[Desc("The basic color used as offset.")]
		[FieldLoader.Require]
		public readonly Color BaseColor;

		[Desc("Amount of maximum difference on the red channel.")]
		public readonly int AmplitudeRed = 0;
		[Desc("Amount of maximum difference on the green channel.")]
		public readonly int AmplitudeGreen = 0;
		[Desc("Amount of maximum difference on the blue channel.")]
		public readonly int AmplitudeBlue = 0;

		public readonly int QuantizationCount = 16;

		public override object Create(ActorInitializer init) { return new FlickeringPaletteEffect(this); }
	}

	public class FlickeringPaletteEffect : IPaletteModifier, ITick
	{
		readonly FlickeringPaletteEffectInfo info;
		readonly int offset;

		int t;

		public FlickeringPaletteEffect(FlickeringPaletteEffectInfo info)
		{
			this.info = info;
			offset = 1024 / info.QuantizationCount;
		}

		public void AdjustPalette(IReadOnlyDictionary<string, MutablePalette> b)
		{
			// cos value is in range of [-1024, 1024].
			var red = (info.BaseColor.R + info.AmplitudeRed * WAngle.FromDegrees(t).Cos() / 1024).Clamp(0, 255);
			var green = (info.BaseColor.G + info.AmplitudeGreen * WAngle.FromDegrees(t).Cos() / 1024).Clamp(0, 255);
			var blue = (info.BaseColor.B + info.AmplitudeBlue * WAngle.FromDegrees(t).Cos() / 1024).Clamp(0, 255);

			var p = b[info.PaletteName];

			for (int j = 1; j < Palette.Size; j++)
			{
				var color = Color.FromArgb(info.BaseColor.A, red, green, blue);
				p.SetColor(j, color);
			}
		}

		void ITick.Tick(Actor self)
		{
			t = (t + offset) % 1024;
		}
	}
}
