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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Renders selection boxes on Prism Towers within range.")]
	sealed class WithPrismLinkVisualizationInfo : ConditionalTraitInfo, IPlaceBuildingDecorationInfo
	{
		[Desc("Color of the circle")]
		public readonly Color Color = Color.FromArgb(128, Color.Cyan);

		[Desc("Range of the circle")]
		public readonly WDist Range = WDist.Zero;

		public IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			if (EnabledByDefault)
			{
				var actorsInRange = w.FindActorsInCircle(centerPosition, Range).Where(a => a.Info.HasTraitInfo<WithPrismLinkVisualizationInfo>());;
				foreach (var a in actorsInRange)
					foreach (var r in a.Trait<WithPrismLinkVisualization>().RenderPrismLinkageVisualization(a, wr, centerPosition))
						yield return r;
			}
		}

		public override object Create(ActorInitializer init) { return new WithPrismLinkVisualization(init.Self, this); }
	}

	sealed class WithPrismLinkVisualization : ConditionalTrait<WithPrismLinkVisualizationInfo>
	{
		readonly Actor self;
		Selectable selectable;

		public WithPrismLinkVisualization(Actor self, WithPrismLinkVisualizationInfo info)
			: base(info)
		{
			this.self = self;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			selectable = self.TraitOrDefault<Selectable>();
		}

		bool Visible
		{
			get
			{
				if (IsTraitDisabled)
					return false;

				var p = self.World.RenderPlayer;
				return p == null || self.Owner == p;
			}
		}

		public IEnumerable<IRenderable> RenderPrismLinkageVisualization(Actor self, WorldRenderer wr, WPos source)
		{
			if (Visible)
			{
				var bounds = selectable.DecorationBounds(self, wr);
				var boxBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
				yield return new SelectionBoxAnnotationRenderable(self, boxBounds, Info.Color);
				yield return new LineAnnotationRenderable(self.CenterPosition, source, 1, Info.Color);
			}
		}
	}
}
