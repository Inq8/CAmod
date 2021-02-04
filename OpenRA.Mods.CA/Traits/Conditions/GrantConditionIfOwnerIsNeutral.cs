#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Grants a condition if the owner is the Neutral player.")]
	public class GrantConditionIfOwnerIsNeutralInfo : TraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new GrantConditionIfOwnerIsNeutral(this); }
	}

	public class GrantConditionIfOwnerIsNeutral : INotifyCreated, INotifyOwnerChanged
	{
		readonly GrantConditionIfOwnerIsNeutralInfo info;

		int token = Actor.InvalidConditionToken;

		public GrantConditionIfOwnerIsNeutral(GrantConditionIfOwnerIsNeutralInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			if (self.Owner.PlayerName == "Neutral")
				GrantCondition(self, info.Condition);
		}

		void GrantCondition(Actor self, string condition)
		{
			token = self.GrantCondition(condition);
		}

		void RevokeCondition(Actor self)
		{
			token = self.RevokeCondition(token);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (newOwner.PlayerName == "Neutral" && token == Actor.InvalidConditionToken)
				GrantCondition(self, info.Condition);
			else if (newOwner.PlayerName != "Neutral" && token != Actor.InvalidConditionToken)
				RevokeCondition(self);
		}
	}
}
