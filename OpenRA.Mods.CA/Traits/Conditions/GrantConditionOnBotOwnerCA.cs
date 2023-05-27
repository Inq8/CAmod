#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Grants a condition to this actor when it is owned by an AI bot. CA version means not specifying bots will apply to all.")]
	public class GrantConditionOnBotOwnerCAInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[Desc("Bot types that trigger the condition.")]
		public readonly string[] Bots = Array.Empty<string>();

		public override object Create(ActorInitializer init) { return new GrantConditionOnBotOwnerCA(this); }
	}

	public class GrantConditionOnBotOwnerCA : INotifyCreated, INotifyOwnerChanged
	{
		readonly GrantConditionOnBotOwnerCAInfo info;

		int conditionToken = Actor.InvalidConditionToken;

		public GrantConditionOnBotOwnerCA(GrantConditionOnBotOwnerCAInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			if (self.Owner.IsBot && (info.Bots.Length == 0 || info.Bots.Contains(self.Owner.BotType)))
				conditionToken = self.GrantCondition(info.Condition);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);

			if (self.Owner.IsBot && (info.Bots.Length == 0 || info.Bots.Contains(self.Owner.BotType)))
				conditionToken = self.GrantCondition(info.Condition);
		}
	}
}
