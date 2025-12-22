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

using System;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	public class DiveApproach : Activity
	{
		readonly Aircraft aircraft;
		readonly Target target;

		WPos targetPosition;
		bool initialized;

		public DiveApproach(in Target target, Aircraft aircraft)
		{
			this.target = target;
			this.aircraft = aircraft;
		}

		protected override void OnFirstRun(Actor self)
		{
			if (target.Type == TargetType.Invalid || (target.Type == TargetType.Actor && target.Actor.IsDead))
			{
				Cancel(self);
				return;
			}

			targetPosition = target.CenterPosition;
			initialized = true;
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return true;

			if (target.Type == TargetType.Invalid || (target.Type == TargetType.Actor && target.Actor.IsDead))
				return true;

			if (!initialized)
			{
				targetPosition = target.CenterPosition;
				initialized = true;
			}

			targetPosition = target.CenterPosition;

			// Ensure we are airborne before attempting fixed-wing approach.
			var dat = self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);
			var isLanded = dat <= aircraft.LandAltitude;
			if (isLanded)
			{
				QueueChild(new TakeOff(self));
				return false;
			}

			// If the aircraft is paused (e.g. EMP), we can't meaningfully maneuver.
			if (aircraft.IsTraitPaused)
				return false;

			var altitude = dat.Length;
			var pitchTan = aircraft.Info.MaximumPitch.Tan();
			var requiredDistance = pitchTan != 0 ? altitude * 1024 / pitchTan : 0;

			// Maintain some slack so minor pathing/turning doesn't oscillate the activity state.
			const int Slack = 256;
			var minRange = Math.Max(requiredDistance - Slack, 0);
			var maxRange = requiredDistance + Slack;

			var pos = self.CenterPosition;
			var delta = targetPosition - pos;
			if (delta.HorizontalLengthSquared == 0)
				return true;

			var toTargetYaw = delta.Yaw;

			// Check whether we are at a suitable distance to begin a MaximumPitch-limited descent.
			var hsq = delta.HorizontalLengthSquared;
			var minSq = (long)minRange * minRange;
			var maxSq = (long)maxRange * maxRange;
			var inRange = maxRange > 0 && hsq >= minSq && hsq <= maxSq;

			// Check whether we are facing towards the dive location.
			var diff = Math.Abs(toTargetYaw.Angle - aircraft.Facing.Angle);
			if (diff > 512)
				diff = 1024 - diff;

			var facingOk = diff <= WAngle.FromDegrees(10).Angle;
			if (inRange && facingOk)
				return true;

			// Steer continuously without stopping. Fixed-wing aircraft cannot turn in place,
			// so we use Fly.FlyTick to keep moving forward while rotating.
			var desiredFacing = toTargetYaw;

			// If we're too close, turn away and fly forward to open distance.
			if (hsq < minSq)
				desiredFacing = toTargetYaw + new WAngle(512);

			// Avoid attempting an unreachable immediate turn (same heuristic as Fly).
			if (!aircraft.Info.CanSlide)
			{
				var turnRadius = Fly.CalculateTurnRadius(aircraft.MovementSpeed, aircraft.TurnSpeed);
				if (turnRadius > 0)
				{
					var turnCenterFacing = aircraft.Facing + new WAngle(Util.GetTurnDirection(aircraft.Facing, desiredFacing) * 256);
					var turnCenterDir = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(turnCenterFacing));
					turnCenterDir *= turnRadius;
					turnCenterDir /= 1024;
					var turnCenter = aircraft.CenterPosition + turnCenterDir;
					if ((targetPosition - turnCenter).HorizontalLengthSquared < (long)turnRadius * turnRadius)
						desiredFacing = aircraft.Facing;
				}
			}

			Fly.FlyTick(self, aircraft, desiredFacing, aircraft.Info.CruiseAltitude);
			return false;
		}
	}
}
