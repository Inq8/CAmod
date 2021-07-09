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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Attach this to actors which should regenerate or lose health points over time.")]
	class ChangesHealthVersusInfo : ConditionalTraitInfo, Requires<IHealthInfo>
	{
		[Desc("Absolute amount of health points added in each step.",
			"Use negative values to apply damage.")]
		public readonly int Step = 5;

		[Desc("Relative percentages of health added in each step.",
			"Use negative values to apply damage.",
			"When both values are defined, their summary will be applied.")]
		public readonly int PercentageStep = 0;

		[Desc("Time in ticks to wait between each health modification.")]
		public readonly int Delay = 5;

		[Desc("Change health if current health is below this percentage of full health. Use zero to always apply.")]
		public readonly int StartIfBelow = 0;

		[Desc("Time in ticks to wait after taking damage.")]
		public readonly int DamageCooldown = 0;

		[Desc("Apply the health change when encountering these damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		[Desc("Damage percentage versus each armortype.")]
		public readonly Dictionary<string, int> Versus = new Dictionary<string, int>();

		public override object Create(ActorInitializer init) { return new ChangesHealthVersus(init.Self, this); }
	}

	class ChangesHealthVersus : ConditionalTrait<ChangesHealthVersusInfo>, ITick, INotifyDamage
	{
		readonly IHealth health;

		[Sync]
		int ticks;

		[Sync]
		int damageTicks;

		public ChangesHealthVersus(Actor self, ChangesHealthVersusInfo info)
			: base(info)
		{
			health = self.Trait<IHealth>();
		}

		void ITick.Tick(Actor self)
		{
			if (self.IsDead || IsTraitDisabled)
				return;

			// Cast to long to avoid overflow when multiplying by the health
			if (Info.StartIfBelow > 0 && health.HP >= Info.StartIfBelow * (long)health.MaxHP / 100)
				return;

			if (damageTicks > 0)
			{
				--damageTicks;
				return;
			}

			if (--ticks <= 0)
			{
				ticks = Info.Delay;
				var damagePercentages = new int[] { DamagePercentage(self) };
				var damageAmount = (int)-(Info.Step + Info.PercentageStep * (long)health.MaxHP / 100);
				damageAmount = Util.ApplyPercentageModifiers(damageAmount, damagePercentages);

				if (damageAmount == 0)
					return;

				var damage = new Damage(damageAmount, Info.DamageTypes);
				self.InflictDamage(self, damage);
			}
		}

		protected int DamagePercentage(Actor self)
		{
			// if no Versus values are defined assume 100% damage
			if (Info.Versus.Count == 0)
				return 100;

			var armor = self.TraitsImplementing<Armor>()
				.Where(a => !a.IsTraitDisabled && a.Info.Type != null && Info.Versus.ContainsKey(a.Info.Type))
				.Select(a => Info.Versus[a.Info.Type]);

			return Util.ApplyPercentageModifiers(100, armor);
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage.Value > 0)
				damageTicks = Info.DamageCooldown;
		}
	}
}
