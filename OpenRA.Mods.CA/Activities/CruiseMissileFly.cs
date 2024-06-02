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
using OpenRA.Activities;
using OpenRA.Mods.CA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	public class CruiseMissileFly : Activity
	{
		readonly CruiseMissile cm;
		readonly WPos launchPos;
		WPos initTargetPos;
		WPos targetPos;
		WPos currentPos;
		int length;
		int ticks;
		WAngle facing;
		readonly Target target;
		WDist maxAltitude;
		WDist maxTargetMovement;
		bool trackingActive;
		int launchAngleDegrees;
		double launchAngleRad;

		public CruiseMissileFly(Actor self, Target t, WPos initialTargetPos, CruiseMissile cm, WDist maxAltitude, WDist maxTargetMovement, bool trackTarget)
		{
			if (cm == null)
				this.cm = self.Trait<CruiseMissile>();
			else
				this.cm = cm;

			if (t.Type == TargetType.Invalid && t.Actor != null && (t.Actor.IsDead || !t.Actor.IsInWorld))
				target = Target.FromPos(initialTargetPos);
			else
				target = t;

			launchPos = currentPos = self.CenterPosition;
			initTargetPos = targetPos = target.CenterPosition;
			length = Math.Max((targetPos - launchPos).Length / this.cm.Info.Speed, 1);
			facing = (targetPos - launchPos).Yaw;
			cm.Facing = GetEffectiveFacing();
			trackingActive = trackTarget;
			this.maxAltitude = maxAltitude;
			this.maxTargetMovement = maxTargetMovement;
			launchAngleDegrees = (int)(cm.Info.LaunchAngle.Angle / (1024f / 360f));
			launchAngleRad = Math.PI * launchAngleDegrees / 180.0;
			cm.SetState(CruiseMissileState.Ascending);
		}

		WAngle GetEffectiveFacing()
		{
			var at = (float)ticks / (length - 1);
			var attitude = cm.Info.LaunchAngle.Tan() * (1 - 2 * at) / (4 * 1024);
			if (cm.State == CruiseMissileState.Cruising)
			{
				attitude = 0f;
			}
			else if (cm.State == CruiseMissileState.Descending)
			{
				attitude *= 1.2f;
			}

			var u = facing.Angle % 512 / 512f;
			var scale = 2048 * u * (1 - u);

			var effective = (int)(facing.Angle < 512
				? facing.Angle - scale * attitude
				: facing.Angle + scale * attitude);

			return new WAngle(effective);
		}

		void FlyToward(Actor self, CruiseMissile cm)
		{
			currentPos = GetInterpolatedPos(launchPos, targetPos, launchAngleRad, ticks, length);
			cm.SetPosition(self, currentPos);
			cm.Facing = GetEffectiveFacing();
		}

		public override bool Tick(Actor self)
		{
			if (trackingActive && maxTargetMovement > WDist.Zero && target.Type == TargetType.Actor && (initTargetPos - target.CenterPosition).Length > maxTargetMovement.Length)
				trackingActive = false;

			if (trackingActive && ((target.Type == TargetType.Actor && !target.Actor.IsDead) || (target.Type == TargetType.FrozenActor && target.FrozenActor != null)))
				targetPos = target.CenterPosition;

			var d = targetPos - self.CenterPosition;

			// The next move would overshoot, so consider it close enough
			var move = cm.FlyStep(cm.Facing);

			// Destruct so that Explodes will be called
			if (d.HorizontalLengthSquared < move.HorizontalLengthSquared || currentPos.Z <= 0)
			{
				Queue(new CallFunc(() => self.Kill(self)));
				return true;
			}

			var previousZ = currentPos.Z;
			FlyToward(self, cm);

			if (currentPos.Z < previousZ)
				cm.SetState(CruiseMissileState.Descending);

			ticks++;
			return false;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(targetPos);
		}

		private WPos GetInterpolatedPos(WPos start, WPos end, double launchAngleRad, int tick, int maxTick)
		{
			if (maxTick < 1)
				throw new ArgumentException("maxTick should be at least 1.");

			double t = (double)tick / maxTick;

			int interpolatedX = (int)(start.X + t * (end.X - start.X));
			int interpolatedY = (int)(start.Y + t * (end.Y - start.Y));

			// Bézier curve for the Z-coordinate
			double controlPointZ = start.Z + Math.Tan(launchAngleRad) * Math.Sqrt((end.X - start.X) * (end.X - start.X) + (end.Y - start.Y) * (end.Y - start.Y)) / 2.0;
			double interpolatedZ = (1 - t) * (1 - t) * start.Z + 2 * (1 - t) * t * controlPointZ + t * t * end.Z;
			var clampedZ = (int)interpolatedZ.Clamp(int.MinValue, int.MaxValue);

			var wasCruising = cm.State == CruiseMissileState.Cruising;
			if (clampedZ > maxAltitude.Length)
				cm.SetState(CruiseMissileState.Cruising);
			else if (wasCruising)
				cm.SetState(CruiseMissileState.Descending);

			var finalZ = cm.State == CruiseMissileState.Cruising ? maxAltitude.Length : clampedZ;

			return new WPos(interpolatedX, interpolatedY, finalZ);
		}
	}
}
