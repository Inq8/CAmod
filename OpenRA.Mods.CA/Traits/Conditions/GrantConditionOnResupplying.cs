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
	[Desc("Grants a condition when resupplying another actor.")]
	public class GrantConditionOnResupplyingInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new GrantConditionOnResupplying(init.Self, this); }
	}

	public class GrantConditionOnResupplying : PausableConditionalTrait<GrantConditionOnResupplyingInfo>, INotifyResupply
	{
		int conditionToken = Actor.InvalidConditionToken;
		bool repairing;
		bool rearming;

		public GrantConditionOnResupplying(Actor self, GrantConditionOnResupplyingInfo info)
			: base(info) { }

		void INotifyResupply.BeforeResupply(Actor self, Actor target, ResupplyType types)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			repairing = types.HasFlag(ResupplyType.Repair);
			rearming = types.HasFlag(ResupplyType.Rearm);

			if (repairing || rearming)
				GrantCondition(self);
		}

		void INotifyResupply.ResupplyTick(Actor self, Actor target, ResupplyType types)
		{
			repairing = types.HasFlag(ResupplyType.Repair);
			rearming = types.HasFlag(ResupplyType.Rearm);

			if (!repairing && !rearming)
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
