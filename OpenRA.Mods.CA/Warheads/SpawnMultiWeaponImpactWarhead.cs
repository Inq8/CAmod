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
using OpenRA.Mods.CA.Effects;
using OpenRA.Mods.CA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	public class SpawnMultiWeaponImpactWarhead : WarheadAS, IRulesetLoaded<WeaponInfo>, IMultiWeaponImpactInfo
	{
		[Desc("A pair of values (x,y) for each impact offset.")]
		public readonly CVec[] ImpactOffsets = { CVec.Zero };

		[Desc("Adds a random offset to the impact position up to this value.")]
		public readonly WDist RandomOffset = WDist.Zero;

		[FieldLoader.Require]
		[WeaponReference]
		[Desc("Has to be defined in weapons.yaml, if defined, as well.")]
		public readonly string Weapon = null;

		[Desc("Whether the sequence of the impacts should be randomized.")]
		public readonly bool RandomImpactSequence = false;

		[Desc("Defines particle ownership (invoker if unset).")]
		public readonly bool Neutral = false;

		[Desc("Interval between each impact. Use two values for a random range. If not set the weapon's ReloadDelay is used.")]
		public readonly int[] Interval = null;

		WeaponInfo weapon;

		WeaponInfo IMultiWeaponImpactInfo.Weapon
		{
			get { return weapon; }
		}

		CVec[] IMultiWeaponImpactInfo.ImpactOffsets
		{
			get { return ImpactOffsets; }
		}

		WDist IMultiWeaponImpactInfo.RandomOffset
		{
			get { return RandomOffset; }
		}

		bool IMultiWeaponImpactInfo.RandomImpactSequence
		{
			get { return RandomImpactSequence; }
		}

		int[] IMultiWeaponImpactInfo.Interval
		{
			get { return Interval; }
		}

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (string.IsNullOrEmpty(Weapon))
				return;

			if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out weapon))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{Weapon.ToLowerInvariant()}'");
		}

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			if (!target.IsValidFor(firedBy))
				return;

			if (!IsValidImpact(target.CenterPosition, firedBy))
				return;

			// Lambdas can't use 'in' variables, so capture a copy for later
			var delayedTarget = target;

			firedBy.World.AddFrameEndTask(w => w.Add(new MultiWeaponImpactEffect(Neutral || firedBy.IsDead ? firedBy.World.WorldActor : firedBy, this, delayedTarget.CenterPosition)));
		}
	}
}
