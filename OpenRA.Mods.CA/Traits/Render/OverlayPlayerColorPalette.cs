#region Copyright & License Information
/*
 * Copyright 2016-2018 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("More beautiful variant of the PlayerColorPalette by using the overlay blend mode.")]
	public class OverlayPlayerColorPaletteInfo : TraitInfo
	{
		[Desc("The name of the palette to base off.")]
		[PaletteReference]
		public readonly string BasePalette = null;

		[Desc("The prefix for the resulting player palettes")]
		[PaletteDefinition(true)]
		public readonly string BaseName = "player";

		[Desc("Remap these indices to player colors.")]
		public readonly int[] RemapIndex = { };

		[Desc("Allow palette modifiers to change the palette.")]
		public readonly bool AllowModifiers = true;

		[Desc("Lowers brightness range.")]
		public readonly float Ramp = 0;

		public override object Create(ActorInitializer init) { return new OverlayPlayerColorPalette(this); }
	}

	public class OverlayPlayerColorPalette : ILoadsPlayerPalettes
	{
		readonly OverlayPlayerColorPaletteInfo info;

		public OverlayPlayerColorPalette(OverlayPlayerColorPaletteInfo info)
		{
			this.info = info;
		}

		public void LoadPlayerPalettes(WorldRenderer wr, string playerName, Color c, bool replaceExisting)
		{
			var pal = new MutablePalette(wr.Palette(info.BasePalette).Palette);
			var r = info.Ramp;

			foreach (var i in info.RemapIndex)
			{
				var bw = (float)(((pal[i] & 0xff) + ((pal[i] >> 8) & 0xff) + ((pal[i] >> 16) & 0xff)) / 3) / 0xff - r;
				if (bw < 0)
				{
						bw = 0;
				}

				var dstR = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)c.R / 0xff) : 2 * bw * ((float)c.R / 0xff);
				var dstG = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)c.G / 0xff) : 2 * bw * ((float)c.G / 0xff);
				var dstB = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)c.B / 0xff) : 2 * bw * ((float)c.B / 0xff);
				pal[i] = (pal[i] & 0xff000000) | ((uint)(dstR * 0xff) << 16) | ((uint)(dstG * 0xff) << 8) | (uint)(dstB * 0xff);
			}

			wr.AddPalette(info.BaseName + playerName, new ImmutablePalette(pal), info.AllowModifiers, replaceExisting);
		}
	}
}
