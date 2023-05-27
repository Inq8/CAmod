#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public enum RequiredHealingType
	{
		Absolute,
		Percentage
	}

	[Desc("Gives a condition to the actor after being healed a given amount or number of times in a given timeframe.")]
	public class GrantConditionOnHealingReceivedInfo : ConditionalTraitInfo, Requires<HealthInfo>
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("Damage types that count for healing purposes.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		[Desc("Number of stacks required for condition to apply.")]
		public readonly int RequiredStacks = 1;

		[Desc("If a positive number, ignore RequiredStacks and require an amount of healing.")]
		public readonly int RequiredHealing = 0;

		[Desc("If using RequiredHealing, whether to use an absolute amount or a percentage of maximum HP.")]
		public readonly RequiredHealingType RequiredHealingType = RequiredHealingType.Absolute;

		[Desc("Minimum amount of healing that applies a stack.")]
		public readonly int MinimumHealing = 1000;

		[Desc("Number of ticks one stack lasts.")]
		public readonly int StackDuration = 250;

		public readonly bool ShowSelectionBar = true;
		public readonly Color SelectionBarColor = Color.White;

		public override object Create(ActorInitializer init) { return new GrantConditionOnHealingReceived(init.Self, this); }
	}

	public class GrantConditionOnHealingReceived : ConditionalTrait<GrantConditionOnHealingReceivedInfo>, ITick, INotifyDamage, ISelectionBar
	{
		public readonly new GrantConditionOnHealingReceivedInfo Info;
		int token = Actor.InvalidConditionToken;
		int initialDuration;
		List<HealingStack> stacks;
		bool worthShowingBar;

		readonly int requiredStacks;
		readonly int requiredHealing;

		int AmountHealed { get { return stacks.Sum(s => s.AmountHealed); } }
		bool ThresholdMet { get { return stacks.Count > 0 && ((Info.RequiredHealing <= 0 && stacks.Count >= requiredStacks) || (Info.RequiredHealing > 0 && AmountHealed >= requiredHealing)); } }

		int ThresholdStackDuration
		{
			get
			{
				if (Info.RequiredHealing <= 0)
					return stacks[stacks.Count - requiredStacks].RemainingDuration;

				var thresholdStackNumber = 0;
				var amountHealed = 0;

				for (int i = stacks.Count - 1; i >= 0; i--)
				{
					amountHealed += stacks[i].AmountHealed;
					if (amountHealed >= requiredHealing)
					{
						thresholdStackNumber = i;
					}
				}

				return stacks[thresholdStackNumber].RemainingDuration;
			}
		}

		public GrantConditionOnHealingReceived(Actor self, GrantConditionOnHealingReceivedInfo info)
			: base(info)
		{
			Info = info;
			stacks = new List<HealingStack>();
			initialDuration = Info.StackDuration;

			if (Info.RequiredHealing > 0)
			{
				if (Info.RequiredHealingType == RequiredHealingType.Percentage)
				{
					var healthInfo = self.Info.TraitInfo<HealthInfo>();
					requiredHealing = healthInfo.HP * (Info.RequiredHealing / 100);
				}
				else
					requiredHealing = Info.RequiredHealing;
			}
			else
				requiredStacks = Info.RequiredStacks;
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo ai)
		{
			if (IsTraitDisabled)
				return;

			if (ai.Damage.Value >= 0 || (!Info.DamageTypes.IsEmpty && !ai.Damage.DamageTypes.Overlaps(Info.DamageTypes)))
				return;

			if (ai.Damage.Value * -1 >= Info.MinimumHealing)
				AddStack(ai.Damage.Value * -1);

			if (ThresholdMet)
			{
				// Set to the remaining duration of the highest stack required for the condition to be active
				initialDuration = ThresholdStackDuration;
				worthShowingBar = initialDuration > 30;
				GrantCondition(self);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (stacks.Count == 0)
				return;

			for (int i = 0; i < stacks.Count; i++)
				stacks[i].RemainingDuration--;

			stacks.RemoveAll(s => s.RemainingDuration <= 0);

			if (!ThresholdMet)
				RevokeCondition(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			stacks = new List<HealingStack>();
			RevokeCondition(self);
		}

		void AddStack(int amountHealed)
		{
			stacks.Add(new HealingStack
			{
				RemainingDuration = Info.StackDuration,
				AmountHealed = amountHealed
			});
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

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled || !Info.ShowSelectionBar || token == Actor.InvalidConditionToken || !worthShowingBar)
				return 0f;

			return (float)ThresholdStackDuration / initialDuration;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }
	}

	class HealingStack
	{
		public int RemainingDuration { get; set; }
		public int AmountHealed { get; set; }
	}
}
