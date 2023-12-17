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
		int length;
		int ticks;
		WAngle facing;
		readonly Target target;
		WDist maxAltitude;
		WDist maxTargetMovement;
		bool trackingLost;
		bool isCruising;
		bool isDeclining;
		int launchAngleDegrees;

		public CruiseMissileFly(Actor self, Target t, CruiseMissile cm, WDist maxAltitude, WDist maxTargetMovement)
		{
			this.cm = cm;
			launchPos = self.CenterPosition;
			initTargetPos = targetPos = t.CenterPosition;
			target = t;
			length = Math.Max((targetPos - launchPos).Length / this.cm.Info.Speed, 1);
			facing = (targetPos - launchPos).Yaw;
			cm.Facing = GetEffectiveFacing();
			isCruising = false;
			isDeclining = false;
			trackingLost = false;
			this.maxAltitude = maxAltitude;
			this.maxTargetMovement = maxTargetMovement;
			launchAngleDegrees = (int)(cm.Info.LaunchAngle.Angle / (1024f / 360f));
		}

		WAngle GetEffectiveFacing()
		{
			var at = (float)ticks / (length - 1);
			var attitude = cm.Info.LaunchAngle.Tan() * (1 - 2 * at) / (4 * 1024);
			if (isCruising)
			{
				attitude = 0f;
			}
			else if (isDeclining)
			{
				attitude *= 1.2f;
			}

			var u = (facing.Angle % 512) / 512f;
			var scale = 2048 * u * (1 - u);

			var effective = (int)(facing.Angle < 512
				? facing.Angle - scale * attitude
				: facing.Angle + scale * attitude);

			return new WAngle(effective);
		}

		public void FlyToward(Actor self, CruiseMissile cm)
		{
			var pos = GetInterpolatedPos(launchPos, targetPos, launchAngleDegrees, ticks, length);
			cm.SetPosition(self, pos);
			cm.Facing = GetEffectiveFacing();
		}

		public override bool Tick(Actor self)
		{
			if (!trackingLost && maxTargetMovement > WDist.Zero && target.Type == TargetType.Actor && (initTargetPos - target.CenterPosition).Length > maxTargetMovement.Length)
				trackingLost = true;

			if (!trackingLost && ((target.Type == TargetType.Actor && !target.Actor.IsDead) || (target.Type == TargetType.FrozenActor && target.FrozenActor != null)))
				targetPos = target.CenterPosition;

			var d = targetPos - self.CenterPosition;

			// The next move would overshoot, so consider it close enough
			var move = cm.FlyStep(cm.Facing);

			// Destruct so that Explodes will be called
			if (d.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				Queue(new CallFunc(() => self.Kill(self)));
				return true;
			}

			FlyToward(self, cm);
			ticks++;
			return false;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(targetPos);
		}

		/*
		private WPos LerpQuadratic(in WPos a, in WPos b, WAngle pitch, int mul, int div)
		{
			// Start with a linear lerp between the points
			var ret = WPos.Lerp(a, b, mul, div);

			if (pitch.Angle == 0)
				return ret;

			// Add an additional quadratic variation to height
			// Uses decimal to avoid integer overflow
			var steepnessFactor = 1.0m; // isDeclining ? 4.0m : 4.0m; // Adjust this factor to control the steepness
			var offset = (decimal)(b - a).Length * pitch.Tan() * mul * (div - mul) / (1024 * div * div) * steepnessFactor;
			var clampedOffset = (int)(offset + ret.Z).Clamp(int.MinValue, int.MaxValue);
			var easedOffset = clampedOffset; // EaseInOut(ret.Z, clampedOffset, 0.35f, 0.35f);

			var wasCruising = isCruising;
			isCruising = easedOffset > maxAltitude.Length;

			if (!isCruising && wasCruising)
				isDeclining = true;

			var finalOffset = isCruising ? maxAltitude.Length : easedOffset;

			return new WPos(ret.X, ret.Y, finalOffset);
		}

		private static int EaseInOut(int start, int end, float easeInFactor, float easeOutFactor)
		{
			var t = 0.5f - 0.5f * (float)Math.Cos(Math.PI * 2.0 * 0.5f);
			var factor = (1.0f - t) * easeInFactor + t * easeOutFactor;
			return (int)(start + (end - start) * factor);
		}
		*/

		private WPos GetInterpolatedPos(WPos start, WPos end, int launchAngle, int currentPoint, int numberOfPoints)
		{
			if (numberOfPoints < 2)
			{
				throw new ArgumentException("Number of points should be at least 2.");
			}

			// Convert launch angle to radians
			double launchAngleRad = Math.PI * launchAngle / 180.0;

			double t = (double)currentPoint / (numberOfPoints - 1);

			int interpolatedX = (int)(start.X + t * (end.X - start.X));
			int interpolatedY = (int)(start.Y + t * (end.Y - start.Y));

			// Bézier curve for the Z-coordinate
			double controlPointZ = start.Z + Math.Tan(launchAngleRad) * Math.Sqrt((end.X - start.X) * (end.X - start.X) + (end.Y - start.Y) * (end.Y - start.Y)) / 2.0;
			double interpolatedZ = (1 - t) * (1 - t) * start.Z + 2 * (1 - t) * t * controlPointZ + t * t * end.Z;
			var clampedZ = (int)interpolatedZ.Clamp(int.MinValue, int.MaxValue);

			var wasCruising = isCruising;
			isCruising = clampedZ > maxAltitude.Length;

			if (!isCruising && wasCruising)
				isDeclining = true;

        	var finalZ = isCruising ? maxAltitude.Length : clampedZ;

			return new WPos(interpolatedX, interpolatedY, finalZ);
		}
	}
}
