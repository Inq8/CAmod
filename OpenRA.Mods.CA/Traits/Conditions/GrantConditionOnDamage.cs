#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Grants a timed condition to the actor on taking damage.")]
	public class GrantConditionOnDamageInfo : ConditionalTraitInfo, Requires<HealthInfo>
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		[Desc("Damage types that apply the condition.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		[Desc("Number of ticks the condition lasts.")]
		public readonly int Duration = 25;

		[Desc("Valid relationships of the attacker for triggering the condition.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		public override object Create(ActorInitializer init) { return new GrantConditionOnDamage(init.Self, this); }
	}

	public class GrantConditionOnDamage : ConditionalTrait<GrantConditionOnDamageInfo>, ITick, INotifyDamage
	{
		public readonly new GrantConditionOnDamageInfo Info;
		int token = Actor.InvalidConditionToken;
		int ticksRemaining;

		public GrantConditionOnDamage(Actor self, GrantConditionOnDamageInfo info)
			: base(info)
		{
			Info = info;
			ticksRemaining = 0;
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo ai)
		{
			if (IsTraitDisabled)
				return;

			if (!Info.ValidRelationships.HasRelationship(ai.Attacker.Owner.RelationshipWith(self.Owner)))
				return;

			if (ai.Damage.Value <= 0 || (!Info.DamageTypes.IsEmpty && !ai.Damage.DamageTypes.Overlaps(Info.DamageTypes)))
				return;

			ticksRemaining = Info.Duration;
			GrantCondition(self);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (ticksRemaining > 0 && --ticksRemaining == 0)
				RevokeCondition(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			RevokeCondition(self);
		}

		void GrantCondition(Actor self)
		{
			if (token == Actor.InvalidConditionToken)
				token = self.GrantCondition(Info.Condition);
		}

		void RevokeCondition(Actor self)
		{
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}
	}
}
