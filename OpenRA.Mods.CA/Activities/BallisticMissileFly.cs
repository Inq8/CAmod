#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.CA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	public class BallisticMissileFly : Activity
	{
		readonly BallisticMissile sbm;
		readonly WPos initPos;
		readonly WPos targetPos;
		int length;
		int ticks;
		WAngle facing;

		public BallisticMissileFly(Actor self, Target t, BallisticMissile sbm = null)
		{
			if (sbm == null)
				this.sbm = self.Trait<BallisticMissile>();
			else
				this.sbm = sbm;

			initPos = self.CenterPosition;
			targetPos = t.CenterPosition; // fixed position == no homing
			length = Math.Max((targetPos - initPos).Length / this.sbm.Info.Speed, 1);
			facing = (targetPos - initPos).Yaw;
		}

		WAngle GetEffectiveFacing()
		{
			var at = (float)ticks / (length - 1);
			var attitude = sbm.Info.LaunchAngle.Tan() * (1 - 2 * at) / (4 * 1024);

			// HACK HACK HACK
			// BodyOrientation does a 90° rotation on isometric worlds.
			// This calculation needs to be updated to accomodate that.
			var u = (facing.Angle % 512) / 512f;
			var scale = 2048 * u * (1 - u);

			var effective = (int)(facing.Angle < 512
				? facing.Angle - scale * attitude
				: facing.Angle + scale * attitude);

			return new WAngle(effective);
		}

		public void FlyToward(Actor self, BallisticMissile sbm)
		{
			var pos = WPos.LerpQuadratic(initPos, targetPos, sbm.Info.LaunchAngle, ticks, length);
			sbm.SetPosition(self, pos);
			sbm.Facing = GetEffectiveFacing();
		}

		public override bool Tick(Actor self)
		{
			var d = targetPos - self.CenterPosition;

			// The next move would overshoot, so consider it close enough
			var move = sbm.FlyStep(sbm.Facing);

			// Destruct so that Explodes will be called
			if (d.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				Queue(new CallFunc(() => self.Kill(self)));
				return true;
			}

			FlyToward(self, sbm);
			ticks++;
			return false;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(targetPos);
		}
	}
}
