#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	public class CloakPaletteEffectCAInfo : TraitInfo
	{
		[Desc("Palette to apply cloak effect to.")]
		[PaletteReference]
		public readonly string Palette = "cloak";

		public override object Create(ActorInitializer init) { return new CloakPaletteEffectCA(init, this); }
	}

	public class CloakPaletteEffectCA : IPaletteModifier, ITick
	{
		float t = 0;
		readonly CloakPaletteEffectCAInfo info;

		readonly Color[] colors =
		{
			Color.FromLinear(55, 205 / 255f, 205 / 255f, 205 / 255f),
			Color.FromLinear(120, 205 / 255f, 205 / 255f, 230 / 255f),
			Color.FromLinear(192, 180 / 255f, 180 / 255f, 255 / 255f),
			Color.FromLinear(178, 205 / 255f, 250 / 255f, 220 / 255f),
			//Color.FromLinear(70, 188 / 255f, 188 / 255f, 188 / 255f),
			//Color.FromLinear(60, 164 / 255f, 164 / 255f, 164 / 255f),
		};

		public CloakPaletteEffectCA(ActorInitializer init, CloakPaletteEffectCAInfo info)
		{
			this.info = info;
		}

		void IPaletteModifier.AdjustPalette(IReadOnlyDictionary<string, MutablePalette> b)
		{
			var i = (int)t;
			var p = b[info.Palette];

			for (var j = 0; j < colors.Length; j++)
			{
				var k = (i + j) % 16 + 0xb0;
				p.SetColor(k, colors[j]);
			}
		}

		void ITick.Tick(Actor self)
		{
			t += 0.25f;
			if (t >= 256) t = 0;
		}
	}
}
