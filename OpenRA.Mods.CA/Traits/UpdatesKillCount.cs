#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.CA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to a unit to update kill count for that unit.")]
	public class UpdatesKillCountInfo : TraitInfo
	{
		[Desc("If not set, will use the actor's internal name.")]
		public readonly string Type = null;

		public override object Create(ActorInitializer init) { return new UpdatesKillCount(this, init.Self); }
	}

	public class UpdatesKillCount : INotifyKilled, INotifyOwnerChanged
	{
		readonly string actorName;

		public UpdatesKillCount(UpdatesKillCountInfo info, Actor self)
		{
			actorName = info.Type ?? self.Info.Name;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			AddToCount(self, e.Attacker.Owner);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			AddToCount(self, newOwner);
		}

		void AddToCount(Actor self, Player attackingPlayer)
		{
			if (self.Owner.WinState != WinState.Undefined)
				return;

			if (attackingPlayer.RelationshipWith(self.Owner) != PlayerRelationship.Enemy)
				return;

			var attackerCounters = attackingPlayer.PlayerActor.TraitsImplementing<ProvidesPrerequisiteOnKillCount>();

			foreach (var counter in attackerCounters)
				counter.Increment(actorName);
		}
	}
}
