#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	[Desc("Apply damage in a specified range.")]
	public class HealthPercentageSpreadDamageWarhead : SpreadDamageWarhead, IRulesetLoaded<WeaponInfo>
	{
		[Desc("If target health is higher than this, assume this amount instead.")]
		public readonly int MaxReferenceHp = 0;

		[Desc("If target health is lower than this, assume this amount instead.")]
		public readonly int MinReferenceHp = 0;

		protected override void InflictDamage(Actor victim, Actor firedBy, HitShape shape, WarheadArgs args)
		{
			var healthInfo = victim.Info.TraitInfo<HealthInfo>();
			var referenceHp = healthInfo.HP;

			if (MaxReferenceHp > 0 && referenceHp > MaxReferenceHp)
				referenceHp = MaxReferenceHp;

			if (MinReferenceHp > 0 && referenceHp < MinReferenceHp)
				referenceHp = MinReferenceHp;

			var damage = Util.ApplyPercentageModifiers(referenceHp, args.DamageModifiers.Append(Damage, DamageVersus(victim, shape, args)));
			victim.InflictDamage(firedBy, new Damage(damage, DamageTypes));
		}
	}
}
