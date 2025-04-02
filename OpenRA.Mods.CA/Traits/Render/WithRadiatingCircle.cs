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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	public enum RadiatingCircleVisibility { Always, WhenSelected }

	[Desc("Radiating circle overlay with an optional outer circle.")]
	class WithRadiatingCircleInfo : ConditionalTraitInfo
	{
		[Desc("Range circle line width.")]
		public readonly int Width = 1;

		public readonly int Interval = 0;

		[Desc("Start color of the radiating circle.")]
		public readonly Color Color = Color.FromArgb(64, Color.Red);

		[Desc("Range of the circle")]
		public readonly WDist StartRadius = WDist.Zero;

		[Desc("Range of the circle")]
		public readonly WDist EndRadius = WDist.Zero;

		public readonly int Duration = 0;

		public readonly bool AlwaysShowMaxRange = false;

		public readonly Color MaxRadiusColor = Color.FromArgb(128, Color.Red);

		public readonly Color MaxRadiusFlashColor = Color.FromArgb(128, Color.Red);

		[Desc("Player relationships which will be able to see the circle.",
			"Valid values are combinations of `None`, `Ally`, `Enemy` and `Neutral`.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		[Desc("When to show the range circle. Valid values are `Always`, and `WhenSelected`")]
		public readonly RadiatingCircleVisibility Visible = RadiatingCircleVisibility.WhenSelected;

		public override object Create(ActorInitializer init) { return new WithRadiatingCircle(init.Self, this); }
	}

	class WithRadiatingCircle : ConditionalTrait<WithRadiatingCircleInfo>, ITick, IRenderAnnotationsWhenSelected, IRenderAnnotations
	{
		public new readonly WithRadiatingCircleInfo Info;
		readonly Actor self;
		WDist currentRadius;
		WDist radiusChangePerTick;
		int ticks;
		bool interval;
		bool flash;

		public WithRadiatingCircle(Actor self, WithRadiatingCircleInfo info)
			: base(info)
		{
			Info = info;
			this.self = self;

			if (info.Duration > 0)
				radiusChangePerTick = (info.EndRadius - info.StartRadius) / info.Duration;
			else
				radiusChangePerTick = WDist.Zero;

			ticks = 0;
			interval = false;
			flash = false;
			currentRadius = Info.StartRadius;
		}

		bool Visible
		{
			get
			{
				if (IsTraitDisabled)
					return false;

				if (Info.Duration == 0)
					return false;

				var p = self.World.RenderPlayer;
				return p == null || Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(p)) || (p.Spectating && !p.NonCombatant);
			}
		}

		public IEnumerable<IRenderable> RenderRadiatingCircle(Actor self, RadiatingCircleVisibility visibility)
		{
			if (Info.Visible == visibility && Visible)
			{
				if (!interval)
				{
					yield return new CircleAnnotationRenderable(
						self.CenterPosition,
						currentRadius,
						Info.Width,
						Info.Color,
						false);
				}

				if (Info.AlwaysShowMaxRange)
				{
					yield return new CircleAnnotationRenderable(
						self.CenterPosition,
						Info.EndRadius > Info.StartRadius ? Info.EndRadius : Info.StartRadius,
						Info.Width,
						flash ? Info.MaxRadiusFlashColor : Info.MaxRadiusColor,
						false);
				}
			}
		}

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			return RenderRadiatingCircle(self, RadiatingCircleVisibility.WhenSelected);
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable => false;

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			return RenderRadiatingCircle(self, RadiatingCircleVisibility.Always);
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;

		void ITick.Tick(Actor self)
		{
			if (!Visible)
			{
				ticks = 0;
				interval = false;
				flash = false;
				currentRadius = Info.StartRadius;
				return;
			}

			ticks++;

			if (interval)
			{
				if (ticks > Info.Interval)
				{
					ticks = 0;
					interval = false;
				}

				flash = false;
				return;
			}
			else if (!interval && ticks > Info.Duration)
			{
				ticks = 0;
				interval = true;
				flash = true;
				currentRadius = Info.StartRadius;
				return;
			}

			flash = false;
			currentRadius += radiusChangePerTick;
		}
	}
}
