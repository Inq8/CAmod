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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Overlays a copy of each renderable of the actor using the specified palette.",
		"Will obscure the underlying renderables if the chosen palette has no transparency.")]
	public class WithPalettedOverlayInfo : ConditionalTraitInfo
	{
		[PaletteReference]
		[Desc("Palette to use when rendering the overlay")]
		public readonly string Palette = "invuln";

		public override object Create(ActorInitializer init) { return new WithPalettedOverlay(this); }
	}

	public class WithPalettedOverlay : ConditionalTrait<WithPalettedOverlayInfo>, IRenderModifier
	{
		public WithPalettedOverlay(WithPalettedOverlayInfo info)
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
