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
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	public enum RangeCircleVisibility { Always, WhenSelected }

	[Desc("CA version can be set to be visible either always, or only when actor is selected, and allows alpha to be defined for player color.")]
	public class RenderShroudCircleCAInfo : ConditionalTraitInfo
	{
		[Desc("Color of the circle.")]
		public readonly Color Color = Color.FromArgb(128, Color.Cyan);

		[Desc("Contrast color of the circle.")]
		public readonly Color ContrastColor = Color.FromArgb(96, Color.Black);

		[Desc("When to show the range circle. Valid values are `Always`, and `WhenSelected`")]
		public readonly RangeCircleVisibility Visible = RangeCircleVisibility.WhenSelected;

		[Desc("Range circle line width.")]
		public readonly float Width = 1;

		[Desc("Range circle border width.")]
		public readonly float ContrastColorWidth = 3;

		[Desc("Player relationships which will be able to see the circle.",
			"Valid values are combinations of `None`, `Ally`, `Enemy` and `Neutral`.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		[Desc("If set, the color of the owning player will be used instead of `Color`.")]
		public readonly bool UsePlayerColor = false;

		[Desc("The alpha value [from 0 to 255] of color used for the player color.")]
		public readonly int PlayerColorAlpha = 255;

		public override object Create(ActorInitializer init) { return new RenderShroudCircleCA(init.Self, this); }
	}

	public class RenderShroudCircleCA : ConditionalTrait<RenderShroudCircleCAInfo>, INotifyCreated, IRenderAnnotationsWhenSelected, IRenderAnnotations
	{
		readonly RenderShroudCircleCAInfo info;
		WDist range;

		public RenderShroudCircleCA(Actor self, RenderShroudCircleCAInfo info)
			: base(info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			range = self.TraitsImplementing<CreatesShroud>()
				.Select(cs => cs.Info.Range)
				.DefaultIfEmpty(WDist.Zero)
				.Max();
		}

		public IEnumerable<IRenderable> RangeCircleRenderables(Actor self, WorldRenderer wr, RangeCircleVisibility visibility)
		{
			if (IsTraitDisabled)
				yield break;

			var p = self.World.RenderPlayer;

			if (p != null && !Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(p)) && !(p.Spectating && !p.NonCombatant))
				yield break;

			if (info.Visible == visibility)
				yield return new RangeCircleAnnotationRenderable(
					self.CenterPosition,
					range,
					0,
					info.UsePlayerColor ? Color.FromArgb(info.PlayerColorAlpha, self.OwnerColor()) : info.Color,
					info.Width,
					info.ContrastColor,
					info.ContrastColorWidth);
		}

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			return RangeCircleRenderables(self, wr, RangeCircleVisibility.WhenSelected);
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable { get { return false; } }

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			return RangeCircleRenderables(self, wr, RangeCircleVisibility.Always);
		}

		bool IRenderAnnotations.SpatiallyPartitionable { get { return false; } }
	}
}
