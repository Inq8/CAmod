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

using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Grants a shield with its own health pool. Main health pool is unaffected by damage until the shield is broken.")]
	public class ShieldedInfo : ConditionalTraitInfo
	{
		[Desc("The strength of the shield (amount of damage it will absorb).")]
		public readonly int MaxStrength = 1000;

		[Desc("Delay in ticks after absorbing damage before the shield will regenerate.")]
		public readonly int RegenDelay = 0;

		[Desc("Amount to recharge at each interval.")]
		public readonly int RegenAmount = 0;

		[Desc("Number of ticks between recharging.")]
		public readonly int RegenInterval = 25;

		[Desc("Condition to grant when shields are active.")]
		public readonly string ShieldsUpCondition = null;

		[Desc("Hides selection bar when shield is at max strength.")]
		public readonly bool HideBarWhenFull = false;

		public readonly bool ShowSelectionBar = true;
		public readonly Color SelectionBarColor = Color.FromArgb(128, 200, 255);

		public override object Create(ActorInitializer init) { return new Shielded(init, this); }
	}

	public class Shielded : ConditionalTrait<ShieldedInfo>, ITick, ISync, ISelectionBar, IDamageModifier, INotifyDamage
	{
		int conditionToken = Actor.InvalidConditionToken;
		Actor self;

		[Sync]
		int strength;
		int intervalTicks;
		int delayTicks;

		public Shielded(ActorInitializer init, ShieldedInfo info)
			: base(info)
		{
			self = init.Self;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			strength = Info.MaxStrength;
			ResetRegen();
		}

		void ITick.Tick(Actor self)
		{
			Regenerate(self);
		}

		protected void Regenerate(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (strength == Info.MaxStrength)
				return;

			if (--delayTicks > 0)
				return;

			if (--intervalTicks > 0)
				return;

			strength += Info.RegenAmount;

			if (strength > Info.MaxStrength)
				strength = Info.MaxStrength;

			if (strength > 0 && conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.ShieldsUpCondition);

			intervalTicks = Info.RegenInterval;
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled)
				return;

			if (e.Damage.Value < 0)
				return;

			ResetRegen();

			if (strength == 0 || e.Damage.Value == 0 || e.Attacker == self)
				return;

			var damageAmt = Convert.ToInt32(e.Damage.Value / 0.01);
			var damageTypes = e.Damage.DamageTypes;
			var excessDamage = damageAmt - strength;
			strength = Math.Max(strength - damageAmt, 0);

			var health = self.TraitOrDefault<IHealth>();

			if (health != null)
			{
				var absorbedDamage = new Damage(-e.Damage.Value, damageTypes);
				health.InflictDamage(self, self, absorbedDamage, true);
			}

			if (strength == 0 && conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);

			if (excessDamage > 0)
			{
				var hullDamage = new Damage(excessDamage, damageTypes);

				if (health != null)
					health.InflictDamage(self, e.Attacker, hullDamage, true);
			}
		}

		void ResetRegen()
		{
			intervalTicks = Info.RegenInterval;
			delayTicks = Info.RegenDelay;
		}

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled || !Info.ShowSelectionBar || strength == 0 || (strength == Info.MaxStrength && Info.HideBarWhenFull))
				return 0;

			var selected = self.World.Selection.Contains(self);
			var rollover = self.World.Selection.RolloverContains(self);
			var regularWorld = self.World.Type == WorldType.Regular;
			var statusBars = Game.Settings.Game.StatusBars;

			var displayHealth = selected || rollover || (regularWorld && statusBars == StatusBarsType.AlwaysShow)
				|| (regularWorld && statusBars == StatusBarsType.DamageShow && strength < Info.MaxStrength);

			if (!displayHealth)
				return 0;

			return (float)strength / Info.MaxStrength;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }

		int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage)
		{
			return IsTraitDisabled || strength == 0 ? 100 : 1;
		}

		protected override void TraitEnabled(Actor self)
		{
			ResetRegen();

			if (conditionToken == Actor.InvalidConditionToken && strength > 0)
				conditionToken = self.GrantCondition(Info.ShieldsUpCondition);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (conditionToken == Actor.InvalidConditionToken)
				return;

			conditionToken = self.RevokeCondition(conditionToken);
		}
	}
}
