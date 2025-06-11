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

		[Desc("If true, grants a kill on creation (temporary until counts rework).")]
		public readonly bool OnCreation = false;

		public override object Create(ActorInitializer init) { return new UpdatesKillCount(this, init.Self); }
	}

	public class UpdatesKillCount : INotifyKilled, INotifyOwnerChanged, INotifyCreated
	{
		readonly string actorName;
		readonly UpdatesKillCountInfo info;

		public UpdatesKillCount(UpdatesKillCountInfo info, Actor self)
		{
			actorName = info.Type ?? self.Info.Name;
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			if (info.OnCreation)
				AddToCount(self, self.Owner);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (info.OnCreation)
				return;

			AddToCount(self, e.Attacker.Owner);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (info.OnCreation)
				return;

			AddToCount(self, newOwner);
		}

		void AddToCount(Actor self, Player attackingPlayer)
		{
			if (self.Owner.WinState != WinState.Undefined)
				return;

			if (attackingPlayer.RelationshipWith(self.Owner) != PlayerRelationship.Enemy && !info.OnCreation)
				return;

			var attackerCounters = attackingPlayer.PlayerActor.TraitsImplementing<ProvidesPrerequisiteOnKillCount>();

			foreach (var counter in attackerCounters)
				counter.Increment(actorName);
		}
	}
}
