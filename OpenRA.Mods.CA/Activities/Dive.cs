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
using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	public class Dive : Activity
	{
		readonly Aircraft aircraft;
		readonly int speed;
		readonly Target target;

		WPos origin, targetPosition;
		int length;
		bool canceled = false;
		bool jumpComplete = false;
		int ticks = 0;

		public Dive(in Target target, Aircraft aircraft, int speed)
		{
			this.aircraft = aircraft;
			this.target = target;
			this.speed = speed;
		}

		protected override void OnFirstRun(Actor self)
		{
			if (target.Type == TargetType.Invalid || (target.Type == TargetType.Actor && target.Actor.IsDead))
			{
				canceled = true;
				return;
			}

			origin = self.CenterPosition;
			targetPosition = target.CenterPosition;
			length = Math.Max((origin - targetPosition).Length / speed, 1);
			IsInterruptible = false;
		}

		public override bool Tick(Actor self)
		{
			// Correct the visual position after we jumped
			if (canceled || jumpComplete)
				return true;

			if (target.Type != TargetType.Invalid)
				targetPosition = target.CenterPosition;

			var position = length > 1 ? WPos.Lerp(origin, targetPosition, ticks, length - 1) : targetPosition;
			aircraft.SetCenterPosition(self, position);

			var desiredFacing = (targetPosition - position).Yaw;
			aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.TurnSpeed);

			// We are at the destination
			if (++ticks >= length)
			{
				// Revoke the run condition
				self.Kill(self);
				jumpComplete = true;
			}

			return false;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(ticks < length / 2 ? origin : targetPosition);
		}
	}
}
