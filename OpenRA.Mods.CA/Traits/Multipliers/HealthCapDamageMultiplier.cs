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
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Modifies the damage taken by the actor to emulate specified maximum health.")]
	public class HealthCapDamageMultiplierInfo : ConditionalTraitInfo
	{
		[Desc("Maximum health to emulate.")]
		public readonly int MaxHealthEquivalent = 40000;

		public override object Create(ActorInitializer init) { return new HealthCapDamageMultiplier(init.Self, this); }
	}

	public class HealthCapDamageMultiplier : ConditionalTrait<HealthCapDamageMultiplierInfo>, IDamageModifier
	{
		readonly int modifier;

		public HealthCapDamageMultiplier(Actor self, HealthCapDamageMultiplierInfo info)
			: base(info)
		{
			modifier = 100;
			var healthInfo = self.Info.TraitInfoOrDefault<HealthInfo>();

			if (healthInfo == null)
				return;

			if (healthInfo.HP > info.MaxHealthEquivalent)
				modifier = (int)(((float)healthInfo.HP / (float)info.MaxHealthEquivalent) * 100);
		}

		int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage)
		{
			return IsTraitDisabled ? 100 : modifier;
		}
	}
}
