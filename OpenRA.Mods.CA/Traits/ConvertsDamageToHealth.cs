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
	[Desc("Health gained per damage dealt.")]
	public class ConvertsDamageToHealthInfo : ConditionalTraitInfo
	{
		[Desc("Percentage of damage dealt that is converted to health.")]
		public readonly int DamagePercentConverted = 100;

		[Desc("The `TargetTypes` from `Targetable` that won't result in damage being converted.")]
		public readonly BitSet<TargetableType> InvalidTargetTypes = default;

		public override object Create(ActorInitializer init) { return new ConvertsDamageToHealth(init, this); }
	}

	public class ConvertsDamageToHealth : ConditionalTrait<ConvertsDamageToHealthInfo>, INotifyAppliedDamage
	{
		public ConvertsDamageToHealth(ActorInitializer init, ConvertsDamageToHealthInfo info)
			: base(info) { }

		void INotifyAppliedDamage.AppliedDamage(Actor self, Actor damaged, AttackInfo e)
		{
			if (IsTraitDisabled)
				return;

			if (e.Damage.Value <= 0 || damaged == self)
				return;

			if (Info.InvalidTargetTypes.Overlaps(damaged.GetEnabledTargetTypes()))
				return;

			var health = self.TraitOrDefault<IHealth>();

			if (health == null)
				return;

			var healthAmt = (e.Damage.Value / 100) * Info.DamagePercentConverted;
			var damageTypes = e.Damage.DamageTypes;
			var damage = new Damage(-healthAmt, damageTypes);
			health.InflictDamage(self, self, damage, true);
		}
	}
}
