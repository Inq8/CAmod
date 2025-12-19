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
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	/// <summary>
	/// A simplified landing activity for production airdrop delivery.
	/// Instead of fully landing (0 altitude), descends to altitude 1,
	/// avoiding blocking actor checks since the aircraft never touches the ground.
	/// </summary>
	public class ProductionAirdropDeliver : Activity
	{
		readonly Aircraft aircraft;
		readonly WVec offset;
		readonly WAngle? desiredFacing;
		readonly Color? targetLineColor;

		readonly Target target;
		WPos targetPosition;
		bool approachComplete;

		/// <summary>
		/// Minimum altitude to descend to (1 unit above ground).
		/// </summary>
		static readonly WDist DeliverAltitude = new(1);

		public ProductionAirdropDeliver(Actor self, in Target target, WVec offset, WAngle? facing = null, Color? targetLineColor = null)
		{
			aircraft = self.Trait<Aircraft>();
			this.target = target;
			this.offset = offset;
			this.targetLineColor = targetLineColor;
			desiredFacing = facing;
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling || target.Type == TargetType.Invalid)
				return true;

			var pos = aircraft.GetPosition();

			// Reevaluate target position in case the target has moved.
			targetPosition = target.CenterPosition + offset;

			// Move towards target position
			if (aircraft.Info.VTOL)
			{
				if ((pos - targetPosition).HorizontalLengthSquared != 0)
				{
					QueueChild(new Fly(self, Target.FromPos(targetPosition)));
					return false;
				}

				if (desiredFacing.HasValue && desiredFacing.Value != aircraft.Facing)
				{
					QueueChild(new Turn(self, desiredFacing.Value));
					return false;
				}

				// Descend to delivery altitude (1 unit above ground)
				var deliverAltitude = self.World.Map.DistanceAboveTerrain(targetPosition) + DeliverAltitude;
				if (Fly.VerticalTakeOffOrLandTick(self, aircraft, aircraft.Facing, deliverAltitude))
					return false;

				return true;
			}

			// Non-VTOL aircraft approach
			if (!approachComplete)
			{
				// Calculate approach trajectory
				var altitude = aircraft.Info.CruiseAltitude.Length;

				// Distance required for descent.
				var landDistance = altitude * 1024 / aircraft.Info.MaximumPitch.Tan();

				// Approach from the opposite direction of the desired facing
				var rotation = WRot.None;
				if (desiredFacing.HasValue)
					rotation = WRot.FromYaw(desiredFacing.Value);

				var approachStart = targetPosition + new WVec(0, landDistance, altitude).Rotate(rotation);

				var speed = aircraft.MovementSpeed * 32 / 35;
				var turnRadius = Fly.CalculateTurnRadius(speed, aircraft.TurnSpeed);

				var angle = aircraft.Facing;
				var fwd = -new WVec(angle.Sin(), angle.Cos(), 0);

				var side = new WVec(-fwd.Y, fwd.X, fwd.Z);
				var approachDelta = self.CenterPosition - approachStart;
				var sideTowardBase = new[] { side, -side }
					.MinBy(a => WVec.Dot(a, approachDelta));

				var cp = self.CenterPosition + turnRadius * sideTowardBase / 1024;
				var posCenter = new WPos(cp.X, cp.Y, altitude);
				var approachCenter = approachStart + new WVec(0, turnRadius * System.Math.Sign(self.CenterPosition.Y - approachStart.Y), 0);
				var tangentDirection = approachCenter - posCenter;
				var tangentLength = tangentDirection.Length;
				var tangentOffset = WVec.Zero;
				if (tangentLength != 0)
					tangentOffset = new WVec(-tangentDirection.Y, tangentDirection.X, 0) * turnRadius / tangentLength;

				if (tangentOffset.X > 0)
					tangentOffset = -tangentOffset;

				var w1 = posCenter + tangentOffset;
				var w2 = approachCenter + tangentOffset;
				var w3 = approachStart;

				turnRadius = Fly.CalculateTurnRadius(aircraft.Info.Speed, aircraft.TurnSpeed);

				QueueChild(new Fly(self, Target.FromPos(w1), WDist.Zero, new WDist(turnRadius * 3)));
				QueueChild(new Fly(self, Target.FromPos(w2)));
				QueueChild(new Fly(self, Target.FromPos(w3), WDist.Zero, new WDist(turnRadius / 2)));
				approachComplete = true;
				return false;
			}

			var d = targetPosition - pos;

			// The next move would overshoot, so just set the final position
			var move = aircraft.FlyStep(aircraft.Facing);
			if (d.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				var deliverAltVec = new WVec(WDist.Zero, WDist.Zero, DeliverAltitude);
				aircraft.SetPosition(self, targetPosition + deliverAltVec);
				return true;
			}

			var deliverAlt = self.World.Map.DistanceAboveTerrain(targetPosition) + DeliverAltitude;
			Fly.FlyTick(self, aircraft, d.Yaw, deliverAlt);

			return false;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (targetLineColor != null)
				yield return new TargetLineNode(target, targetLineColor.Value);
		}
	}
}
