#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Variant of the PlayerColorPalette using the overlay blend mode which increases contrast.")]
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
		public readonly float Ramp = 0.125f;

		[Desc("If player name is set here, remap to these colours instead.")]
		public readonly Dictionary<string, Color> PlayerColors;

		[Desc("If faction is set here, remap to these colors instead.")]
		public readonly Dictionary<string, Color> FactionColors;

		[Desc("Players listed here will have their colors determined by their faction color if set.")]
		public readonly HashSet<string> FactionColorPlayers = new();

		public override object Create(ActorInitializer init) { return new OverlayPlayerColorPalette(init, this); }
	}

	public class OverlayPlayerColorPalette : ILoadsPlayerPalettes
	{
		readonly OverlayPlayerColorPaletteInfo info;
		readonly World world;

		public OverlayPlayerColorPalette(ActorInitializer init, OverlayPlayerColorPaletteInfo info)
		{
			this.info = info;
			world = init.Self.World;
		}

		public void LoadPlayerPalettes(WorldRenderer wr, string playerName, Color c, bool replaceExisting)
		{
			var basePalette = wr.Palette(info.BasePalette).Palette;
			var player = world.Players.FirstOrDefault(p => p.InternalName == playerName);

			if (player != null
				&& info.FactionColors != null
				&& info.FactionColors.TryGetValue(player.Faction.InternalName, out var factionColor)
				&& (!info.FactionColorPlayers.Any() || info.FactionColorPlayers.Contains(playerName)))
			{
				c = factionColor;
			}

			if (info.PlayerColors != null && info.PlayerColors.TryGetValue(playerName, out var playerColor))
				c = playerColor;

			var pal = new MutablePalette(basePalette);
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
