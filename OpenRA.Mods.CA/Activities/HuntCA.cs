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
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	public class HuntCA : Activity
	{
		readonly IEnumerable<Actor> targets;
		readonly IMove move;
		int ticks;
		int scanInterval;

		public HuntCA(Actor self)
		{
			ticks = 0;
			scanInterval = self.World.SharedRandom.Next(20, 40);
			move = self.Trait<IMove>();
			var attack = self.Trait<AttackBase>();
			targets = self.World.ActorsHavingTrait<Huntable>().Where(
				a => self != a && !a.IsDead && a.IsInWorld && a.AppearsHostileTo(self)
				&& a.IsTargetableBy(self) && attack.HasAnyValidWeapons(Target.FromActor(a)));
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return true;

			ticks++;

			var targetActor = ticks % scanInterval == 0 ? targets.ClosestToWithPathFrom(self) : null;

			if (targetActor != null)
			{
				scanInterval = self.World.SharedRandom.Next(20, 40);

				// We want to keep 2 cells of distance from the target to prevent the pathfinder from thinking the target position is blocked.
				QueueChild(new AttackMoveActivity(self, () => move.MoveWithinRange(Target.FromCell(self.World, targetActor.Location), WDist.FromCells(2))));
				QueueChild(new Wait(25));
			}
			else if (ticks > 375)
				scanInterval = self.World.SharedRandom.Next(250, 500);

			return false;
		}
	}
}
