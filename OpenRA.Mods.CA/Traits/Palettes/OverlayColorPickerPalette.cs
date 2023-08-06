#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Create a color picker palette from another palette, using the overlay blend mode which increases contrast.")]
	class OverlayColorPickerPaletteInfo : TraitInfo
	{
		[PaletteDefinition]
		[FieldLoader.Require]
		[Desc("Internal palette name.")]
		public readonly string Name = null;

		[PaletteReference]
		[FieldLoader.Require]
		[Desc("The name of the palette to base off.")]
		public readonly string BasePalette = null;

		[FieldLoader.Require]
		[Desc("Remap these indices to player colors.")]
		public readonly int[] RemapIndex = Array.Empty<int>();

		[Desc("Allow palette modifiers to change the palette.")]
		public readonly bool AllowModifiers = true;

		[Desc("Lowers brightness range.")]
		public readonly float Ramp = 0.125f;

		public override object Create(ActorInitializer init) { return new OverlayColorPickerPalette(this); }
	}

	class OverlayColorPickerPalette : ILoadsPalettes, IProvidesAssetBrowserColorPickerPalettes, ITickRender
	{
		readonly OverlayColorPickerPaletteInfo info;
		Color color;
		Color preferredColor;

		public OverlayColorPickerPalette(OverlayColorPickerPaletteInfo info)
		{
			this.info = info;

			// All users need to use the same TraitInfo instance, chosen as the default mod rules
			var colorManager = Game.ModData.DefaultRules.Actors[SystemActors.World].TraitInfo<IColorPickerManagerInfo>();
			colorManager.OnColorPickerColorUpdate += c => preferredColor = c;
			preferredColor = Game.Settings.Player.Color;
		}

		void ILoadsPalettes.LoadPalettes(WorldRenderer wr)
		{
			color = preferredColor;
			var pal = new MutablePalette(wr.Palette(info.BasePalette).Palette);
			var r = info.Ramp;

			foreach (var i in info.RemapIndex)
			{
				var bw = (float)(((pal[i] & 0xff) + ((pal[i] >> 8) & 0xff) + ((pal[i] >> 16) & 0xff)) / 3) / 0xff - r;
				if (bw < 0)
				{
					bw = 0;
				}

				var dstR = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)color.R / 0xff) : 2 * bw * ((float)color.R / 0xff);
				var dstG = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)color.G / 0xff) : 2 * bw * ((float)color.G / 0xff);
				var dstB = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)color.B / 0xff) : 2 * bw * ((float)color.B / 0xff);
				pal[i] = (pal[i] & 0xff000000) | ((uint)(dstR * 0xff) << 16) | ((uint)(dstG * 0xff) << 8) | (uint)(dstB * 0xff);
			}

			wr.AddPalette(info.Name, new ImmutablePalette(pal));
		}

		IEnumerable<string> IProvidesAssetBrowserColorPickerPalettes.ColorPickerPaletteNames { get { yield return info.Name; } }

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			if (color == preferredColor)
				return;

			color = preferredColor;
			var pal = new MutablePalette(wr.Palette(info.BasePalette).Palette);
			var r = info.Ramp;

			foreach (var i in info.RemapIndex)
			{
				var bw = (float)(((pal[i] & 0xff) + ((pal[i] >> 8) & 0xff) + ((pal[i] >> 16) & 0xff)) / 3) / 0xff - r;
				if (bw < 0)
				{
					bw = 0;
				}

				var dstR = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)color.R / 0xff) : 2 * bw * ((float)color.R / 0xff);
				var dstG = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)color.G / 0xff) : 2 * bw * ((float)color.G / 0xff);
				var dstB = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)color.B / 0xff) : 2 * bw * ((float)color.B / 0xff);
				pal[i] = (pal[i] & 0xff000000) | ((uint)(dstR * 0xff) << 16) | ((uint)(dstG * 0xff) << 8) | (uint)(dstB * 0xff);
			}

			wr.ReplacePalette(info.Name, new ImmutablePalette(pal));
		}
	}
}
