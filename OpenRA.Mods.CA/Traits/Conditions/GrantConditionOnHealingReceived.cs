#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Gives a condition to the actor after being healed a given number of times in a given timeframe.")]
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

		[Desc("If a positive number, ignore RequiredStacks and divide HP by this number to get the required stacks.")]
		public readonly int RequiredStacksHPDivisor = 0;

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
		List<int> stacks;
		bool worthShowingBar;

		readonly int requiredStacks;

		public GrantConditionOnHealingReceived(Actor self, GrantConditionOnHealingReceivedInfo info)
			: base(info)
		{
			Info = info;
			stacks = new List<int>();
			initialDuration = Info.StackDuration;

			if (Info.RequiredStacksHPDivisor > 0)
			{
				var healthInfo = self.Info.TraitInfo<HealthInfo>();
				requiredStacks = healthInfo.HP / Info.RequiredStacksHPDivisor;
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
				AddStack();

			if (stacks.Count >= requiredStacks)
			{
				initialDuration = stacks[stacks.Count - requiredStacks];
				worthShowingBar = initialDuration > 30;
				GrantCondition(self);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (!stacks.Any())
				return;

			var stacksToRemove = 0;

			for (int i = 0; i < stacks.Count; i++)
			{
				stacks[i]--;

				if (stacks[i] <= 0)
					stacksToRemove++;
			}

			for (var i = 0; i < stacksToRemove; i++)
				stacks.RemoveAt(0);

			if (stacks.Count < requiredStacks)
				RevokeCondition(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			stacks = new List<int>();
			RevokeCondition(self);
		}

		void AddStack()
		{
			stacks.Add(Info.StackDuration);
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

			var ticksRemaining = stacks[stacks.Count - requiredStacks];
			return (float)ticksRemaining / initialDuration;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }
	}
}
