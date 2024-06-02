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
	public class GuidedMissileFly : Activity
	{
		readonly GuidedMissile gm;
		readonly WPos launchPos;
		WPos initTargetPos;
		WPos targetPos;
		readonly Target target;
		WDist maxTargetMovement;
		bool trackingActive;

		public GuidedMissileFly(Actor self, Target t, WPos initialTargetPos, GuidedMissile gm, WDist maxTargetMovement)
		{
			if (gm == null)
				this.gm = self.Trait<GuidedMissile>();
			else
				this.gm = gm;

			if (t.Type == TargetType.Invalid && t.Actor != null && (t.Actor.IsDead || !t.Actor.IsInWorld))
				target = Target.FromPos(initialTargetPos);
			else
				target = t;

			trackingActive =  true;
			launchPos = self.CenterPosition;
			initTargetPos = targetPos = target.CenterPosition;
			gm.Facing = (targetPos - launchPos).Yaw;
			this.maxTargetMovement = maxTargetMovement;
		}

		public override bool Tick(Actor self)
		{
			if (trackingActive && maxTargetMovement > WDist.Zero && target.Type == TargetType.Actor && (initTargetPos - target.CenterPosition).Length > maxTargetMovement.Length)
				trackingActive = false;

			if (trackingActive && ((target.Type == TargetType.Actor && !target.Actor.IsDead) || (target.Type == TargetType.FrozenActor && target.FrozenActor != null)))
				targetPos = target.CenterPosition;

			var d = targetPos - self.CenterPosition;
			var move = gm.FlyStep(gm.Facing);
			var overshoot = d.HorizontalLengthSquared < move.HorizontalLengthSquared;

			// Destruct so that Explodes will be called
			if (overshoot || self.CenterPosition.Z <= 0)
			{
				// Snap to target
				if (overshoot)
					gm.SetPosition(self, targetPos);

				Queue(new CallFunc(() => self.Kill(self)));
				return true;
			}

			gm.Facing = d.Yaw;

			var newPosition = self.CenterPosition + move;
			newPosition += new WVec(0, 0, targetPos.Z - newPosition.Z);

			if (newPosition.Z < gm.Info.MinAltitude.Length)
				newPosition = new WPos(newPosition.X, newPosition.Y, gm.Info.MinAltitude.Length);

			gm.SetPosition(self, newPosition);

			return false;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(targetPos);
		}
	}
}
