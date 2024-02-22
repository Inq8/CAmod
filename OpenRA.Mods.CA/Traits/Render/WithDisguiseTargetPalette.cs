#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc(".")]
	public class WithDisguiseTargetPaletteInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new WithDisguiseTargetPalette(init.Self, this); }
	}

	public class WithDisguiseTargetPalette : ConditionalTrait<WithDisguiseTargetPaletteInfo>, IRenderModifier
	{
		readonly Disguise disguise;

		public WithDisguiseTargetPalette(Actor self, WithDisguiseTargetPaletteInfo info)
			: base(info)
		{
			disguise = self.Trait<Disguise>();
		}

		IEnumerable<IRenderable> IRenderModifier.ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (IsTraitDisabled || !disguise.IsTraitEnabled() || !disguise.Disguised)
				return r;

			var disguisedAs = disguise.AsActor;
			var renderSprites = disguisedAs.TraitInfoOrDefault<RenderSpritesInfo>();
			var palette = wr.Palette(renderSprites.Palette ?? renderSprites.PlayerPalette + disguise.Owner.InternalName);

			if (palette == null)
				return r;
			else
				return r.Select(a => !a.IsDecoration && a is IPalettedRenderable pr ? pr.WithPalette(palette) : a);
		}

		IEnumerable<Rectangle> IRenderModifier.ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> bounds)
		{
			return bounds;
		}
	}
}
