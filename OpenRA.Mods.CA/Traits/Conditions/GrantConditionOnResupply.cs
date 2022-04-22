#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Grants a condition when being resupplied.")]
	public class GrantConditionOnResupplyInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("Order name that toggles the condition.")]
		public readonly bool GrantPermanently = false;

		public override object Create(ActorInitializer init) { return new GrantConditionOnResupply(init.Self, this); }
	}

	public class GrantConditionOnResupply : PausableConditionalTrait<GrantConditionOnResupplyInfo>, INotifyBeingResupplied
	{
		int conditionToken = Actor.InvalidConditionToken;

		public GrantConditionOnResupply(Actor self, GrantConditionOnResupplyInfo info)
			: base(info) { }

		void INotifyBeingResupplied.StartingResupply(Actor self, Actor host)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			GrantCondition(self);
		}

		void INotifyBeingResupplied.StoppingResupply(OpenRA.Actor self, OpenRA.Actor host)
		{
			if (!Info.GrantPermanently)
				RevokeCondition(self);
		}

		void GrantCondition(Actor self)
		{
			if (conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.Condition);
		}

		void RevokeCondition(Actor self)
		{
			if (conditionToken == Actor.InvalidConditionToken)
				return;

			conditionToken = self.RevokeCondition(conditionToken);
		}

		protected override void TraitDisabled(Actor self)
		{
			RevokeCondition(self);
		}

		protected override void TraitPaused(Actor self)
		{
			RevokeCondition(self);
		}
	}
}
