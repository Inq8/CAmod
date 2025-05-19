#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;
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
		public readonly Color Color = Color.FromArgb(80, Color.Red);

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
		Queue<RadiatingCircle> activeCircles;
		int intervalTicks;
		bool flash;
		WDist radiusChangePerTick;

		struct RadiatingCircle
		{
			public WDist CurrentRadius;
			int tick;

			public RadiatingCircle(WDist startRadius)
			{
				CurrentRadius = startRadius;
				tick = 0;
			}

			public int CurrentTick
			{
				get { return tick; }
			}

			public void Tick()
			{
				tick++;
			}
		}

		public WithRadiatingCircle(Actor self, WithRadiatingCircleInfo info)
			: base(info)
		{
			Info = info;
			this.self = self;
			activeCircles = new Queue<RadiatingCircle>();
			intervalTicks = 0;
			flash = false;
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
				foreach (var circle in activeCircles)
				{
					yield return new CircleAnnotationRenderable(
						self.CenterPosition,
						circle.CurrentRadius,
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
				return;

			intervalTicks++;

			// Create new circle every interval
			if (intervalTicks >= Info.Interval)
			{
				intervalTicks = 0;
				activeCircles.Enqueue(new RadiatingCircle(Info.StartRadius));
			}

			// Update all active circles
			var tempCircles = new Queue<RadiatingCircle>();
			var circleCompleted = false;

			while (activeCircles.Count > 0)
			{
				var circle = activeCircles.Dequeue();
				circle.Tick();

				// Only keep circles that haven't completed
				if (circle.CurrentTick < Info.Duration)
				{
					circle.CurrentRadius += radiusChangePerTick;
					tempCircles.Enqueue(circle);
				}
				else
				{
					circleCompleted = true;
				}
			}

			// Flash when a circle completes its expansion
			flash = circleCompleted;

			// Replace the old queue with updated circles
			activeCircles = tempCircles;
		}

		protected override void TraitEnabled(Actor self)
		{
			if (Info.Duration > 0)
				radiusChangePerTick = (Info.EndRadius - Info.StartRadius) / Info.Duration;
			else
				radiusChangePerTick = WDist.Zero;

			if (Info.Interval == 0 || !activeCircles.Any())
				activeCircles.Enqueue(new RadiatingCircle(Info.StartRadius));

			intervalTicks = 0;
		}

		protected override void TraitDisabled(Actor self)
		{
			activeCircles.Clear();
			intervalTicks = 0;
			flash = false;
		}
	}
}
