#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Grants a condition if the owner is the Neutral player.")]
	public class GrantConditionIfOwnerIsNeutralInfo : ITraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		public object Create(ActorInitializer init) { return new GrantConditionIfOwnerIsNeutral(this); }
	}

	public class GrantConditionIfOwnerIsNeutral : INotifyCreated, INotifyOwnerChanged
	{
		readonly GrantConditionIfOwnerIsNeutralInfo info;
		ConditionManager manager;

		int token = ConditionManager.InvalidConditionToken;

		public GrantConditionIfOwnerIsNeutral(GrantConditionIfOwnerIsNeutralInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			manager = self.Trait<ConditionManager>();

			if (self.Owner.PlayerName == "Neutral")
				GrantCondition(self, info.Condition);
		}

		void GrantCondition(Actor self, string condition)
		{
			if (manager == null)
				return;

			if (string.IsNullOrEmpty(condition))
				return;

			token = manager.GrantCondition(self, condition);
		}

		void RevokeCondition(Actor self)
		{
			if (manager == null)
				return;

			token = manager.RevokeCondition(self, token);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (newOwner.PlayerName == "Neutral" && token == ConditionManager.InvalidConditionToken)
				GrantCondition(self, info.Condition);
			else if (newOwner.PlayerName != "Neutral" && token != ConditionManager.InvalidConditionToken)
				RevokeCondition(self);
		}
	}
}
