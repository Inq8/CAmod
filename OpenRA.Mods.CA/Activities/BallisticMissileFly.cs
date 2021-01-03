#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
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
		readonly BallisticMissile bm;
		readonly WPos initPos;
		readonly WPos targetPos;
		readonly int length;
		readonly WAngle pitchStep;
		int ticks;

		public BallisticMissileFly(Actor self, Target t, BallisticMissile bm)
		{
			this.bm = bm;
			initPos = self.CenterPosition;
			targetPos = t.CenterPosition;
			length = Math.Max((targetPos - initPos).Length / this.bm.Info.Speed, 1);
			bm.Facing = (targetPos - initPos).Yaw;
			pitchStep = new WAngle(2 * bm.Info.LaunchAngle.Angle / length);
		}

		public override bool Tick(Actor self)
		{
			var d = targetPos - self.CenterPosition;
			var move = bm.FlyStep(bm.Facing);

			// HACK: my math is failing to derivate this properly, this shouldn't be a linear interpolation
			bm.Pitch -= pitchStep;

			if (d.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				// Snap to the target position to prevent overshooting.
				bm.SetPosition(self, targetPos);
				Queue(new CallFunc(() => self.Kill(self, bm.Info.DamageTypes)));
				return true;
			}

			var pos = WPos.LerpQuadratic(initPos, targetPos, bm.Info.LaunchAngle, ticks, length);
			bm.SetPosition(self, pos);

			ticks++;
			return false;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(targetPos);
		}
	}
}
