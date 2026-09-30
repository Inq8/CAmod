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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Grants a condition to the actor when a given number of enemies are within a given range.")]
	public class GrantConditionOnEnemiesNearbyInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("The range within which to count enemies.")]
		public readonly WDist Range = WDist.FromCells(5);

		[Desc("The number of enemies required within the specified range to grant the condition.")]
		public readonly int EnemyCount = 1;

		[Desc("The number of game ticks between each check for nearby enemies.")]
		public readonly int TickInterval = 1;

		[Desc("Valid target types for the nearby enemies check.")]
		public readonly BitSet<TargetableType> TargetTypes = new("Ground", "Water");

		public override object Create(ActorInitializer init) { return new GrantConditionOnEnemiesNearby(init.Self, this); }
	}

	public class GrantConditionOnEnemiesNearby : ITick
	{
		readonly GrantConditionOnEnemiesNearbyInfo info;
		int token = Actor.InvalidConditionToken;
		int ticks = 0;

		public GrantConditionOnEnemiesNearby(Actor self, GrantConditionOnEnemiesNearbyInfo info)
		{
			this.info = info;
		}

		void ITick.Tick(Actor self)
		{
			ticks++;

			if (ticks < info.TickInterval)
				return;

			var actorsInRange = self.World.FindActorsInCircle(self.CenterPosition, info.Range)
				.Where(a => a.Owner.RelationshipWith(self.Owner) == PlayerRelationship.Enemy
					&& info.TargetTypes.Overlaps(a.GetEnabledTargetTypes()));

			var numEnemiesNearby = actorsInRange.Count();

			if (numEnemiesNearby >= info.EnemyCount && token == Actor.InvalidConditionToken)
				token = self.GrantCondition(info.Condition);
			else if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);

			ticks = 0;
		}
	}
}
