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
	public class CloakPaletteEffectCAInfo : TraitInfo
	{
		[Desc("Palette to apply cloak effect to.")]
		[PaletteReference]
		public readonly string Palette = "cloak";

		public readonly int[] SkipIndexes = new int[] { 0, 4 };

		public readonly int[] EraseIndexes = new int[] { };

		public override object Create(ActorInitializer init) { return new CloakPaletteEffectCA(init, this); }
	}

	public class CloakPaletteEffectCA : IPaletteModifier, ITick
	{
		float t = 0;
		readonly CloakPaletteEffectCAInfo info;

		readonly Color[] colors =
		{
			Color.FromLinear(10, 0 / 255f, 0 / 255f, 0 / 255f),
			Color.FromLinear(20, 0 / 255f, 0 / 255f, 0 / 255f),
			Color.FromLinear(30, 0 / 255f, 0 / 255f, 0 / 255f),
			Color.FromLinear(40, 0 / 255f, 0 / 255f, 0 / 255f)
		};

		public CloakPaletteEffectCA(ActorInitializer init, CloakPaletteEffectCAInfo info)
		{
			this.info = info;
		}

		void IPaletteModifier.AdjustPalette(IReadOnlyDictionary<string, MutablePalette> b)
		{
			var i = (int)t;
			var p = b[info.Palette];

			for (var idx = 0; idx < 255; idx += 16)
			{
				for (var j = 0; j < colors.Length; j++)
				{
					var k = (i + j) % 16 + idx;

					if (info.SkipIndexes.Contains(k))
						continue;

					p.SetColor(k, colors[j]);
				}
			}

			foreach (var idx in info.EraseIndexes)
				p.SetColor(idx, Color.FromArgb(0, 0, 0, 0));
		}

		void ITick.Tick(Actor self)
		{
			t += 0.5f;
			if (t >= 256) t = 0;
		}
	}
}
