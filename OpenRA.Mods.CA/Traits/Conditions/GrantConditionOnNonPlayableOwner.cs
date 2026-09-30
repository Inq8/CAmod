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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants a condition to this actor when it is owned by a non-playable player.")]
	public class GrantConditionOnNonPlayableOwnerInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[Desc("Only grant condition if the owner has no playable allies.")]
		public readonly bool RequireNoPlayableAllies = false;

		public override object Create(ActorInitializer init) { return new GrantConditionOnNonPlayableOwner(this); }
	}

	public class GrantConditionOnNonPlayableOwner : INotifyCreated, INotifyOwnerChanged
	{
		readonly GrantConditionOnNonPlayableOwnerInfo info;

		int conditionToken = Actor.InvalidConditionToken;

		public GrantConditionOnNonPlayableOwner(GrantConditionOnNonPlayableOwnerInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			GrantConditionIfApplicable(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);

			GrantConditionIfApplicable(self);
		}

		void GrantConditionIfApplicable(Actor self)
		{
			if (self.Owner.Playable)
				return;

			if (info.RequireNoPlayableAllies)
			{
				var players = self.World.Players;
				if (players.Any(p => p != self.Owner && p.Playable && p.IsAlliedWith(self.Owner)))
					return;
			}

			conditionToken = self.GrantCondition(info.Condition);
		}
	}
}
