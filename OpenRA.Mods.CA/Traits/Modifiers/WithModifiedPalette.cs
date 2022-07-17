#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Display a colored overlay when a timed condition is active.")]
	public class WithModifiedPaletteInfo : ConditionalTraitInfo
	{
		[PaletteReference]
		[Desc("Palette to use when rendering the overlay")]
		public readonly string Palette = "invuln";

		public override object Create(ActorInitializer init) { return new WithModifiedPalette(this); }
	}

	public class WithModifiedPalette : ConditionalTrait<WithModifiedPaletteInfo>, IRenderModifier
	{
		public WithModifiedPalette(WithModifiedPaletteInfo info)
			: base(info) { }

		IEnumerable<IRenderable> IRenderModifier.ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (IsTraitDisabled)
				return r;

			return ModifiedRender(self, wr, r);
		}

		IEnumerable<IRenderable> ModifiedRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (IsTraitDisabled)
				yield break;

			var palette = string.IsNullOrEmpty(Info.Palette) ? null : wr.Palette(Info.Palette);

			foreach (var a in r)
			{
				yield return a;

				if (palette != null && !a.IsDecoration && a is IPalettedRenderable)
					yield return ((IPalettedRenderable)a).WithPalette(palette)
						.WithZOffset(a.ZOffset + 1)
						.AsDecoration();
			}
		}

		IEnumerable<Rectangle> IRenderModifier.ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> bounds)
		{
			return bounds;
		}
	}
}
