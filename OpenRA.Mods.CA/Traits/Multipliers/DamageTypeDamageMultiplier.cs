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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Modifies the damage applied to this actor.",
		"Use 0 to make actor invulnerable.")]
	public class DamageTypeDamageMultiplierInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Percentage modifier to apply.")]
		public readonly int Modifier = 100;

		[FieldLoader.Require]
		[Desc("DamageType(s) that trigger the modifier.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		public override object Create(ActorInitializer init) { return new DamageTypeDamageMultiplier(this); }
	}

	public class DamageTypeDamageMultiplier : ConditionalTrait<DamageTypeDamageMultiplierInfo>, IDamageModifier
	{
		public DamageTypeDamageMultiplier(DamageTypeDamageMultiplierInfo info)
			: base(info) { }

		int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage)
		{
			var validDamageType = !Info.DamageTypes.IsEmpty && damage.DamageTypes.Overlaps(Info.DamageTypes);
			return IsTraitDisabled || !validDamageType ? 100 : Info.Modifier;
		}
	}
}
