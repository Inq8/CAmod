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
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	// TODO: add rotation support based on initiator
	[Desc("Allows to fire a a weapon to a directly specified target position relative to the warhead explosion.")]
	public class FireFragmentWarhead : WarheadAS, IRulesetLoaded<WeaponInfo>
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Has to be defined in weapons.yaml as well.")]
		public readonly string Weapon = null;

		[Desc("Percentual chance the fragment is fired.")]
		public readonly int Chance = 100;

		[Desc("Target offset relative to warhead explosion.")]
		public readonly WVec Offset = new WVec(0, 0, 0);

		[Desc("If set, Offset's Z value will be used as absolute height instead of explosion height.")]
		public readonly bool UseZOffsetAsAbsoluteHeight = false;

		WeaponInfo weapon;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out weapon))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(Weapon.ToLowerInvariant()));
		}

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			if (!target.IsValidFor(firedBy))
				return;

			var world = firedBy.World;
			var map = world.Map;

			if (Chance < world.SharedRandom.Next(100))
				return;

			if (!IsValidImpact(target.CenterPosition, firedBy))
				return;

			var targetpos = UseZOffsetAsAbsoluteHeight
				? new WPos(target.CenterPosition.X + Offset.X, target.CenterPosition.Y + Offset.Y,
					map.CenterOfCell(map.CellContaining(target.CenterPosition)).Z + Offset.Z)
				: target.CenterPosition + Offset;

			var fragmentTarget = Target.FromPos(targetpos);

			// Lambdas can't use 'in' variables, so capture a copy for later
			var centerPosition = target.CenterPosition;

			var projectileArgs = new ProjectileArgs
			{
				Weapon = weapon,
				Facing = (fragmentTarget.CenterPosition - target.CenterPosition).Yaw,

				DamageModifiers = !firedBy.IsDead ? firedBy.TraitsImplementing<IFirepowerModifier>()
					.Select(a => a.GetFirepowerModifier()).ToArray() : new int[0],

				InaccuracyModifiers = !firedBy.IsDead ? firedBy.TraitsImplementing<IInaccuracyModifier>()
					.Select(a => a.GetInaccuracyModifier()).ToArray() : new int[0],

				RangeModifiers = !firedBy.IsDead ? firedBy.TraitsImplementing<IRangeModifier>()
					.Select(a => a.GetRangeModifier()).ToArray() : new int[0],

				Source = target.CenterPosition,
				CurrentSource = () => centerPosition,
				SourceActor = firedBy,
				GuidedTarget = fragmentTarget,
				PassiveTarget = fragmentTarget.CenterPosition
			};

			if (args.Weapon.Projectile != null)
			{
				var projectile = projectileArgs.Weapon.Projectile.Create(projectileArgs);
				if (projectile != null)
					firedBy.World.AddFrameEndTask(w => w.Add(projectile));

				if (projectileArgs.Weapon.Report != null && projectileArgs.Weapon.Report.Any())
					Game.Sound.Play(SoundType.World, projectileArgs.Weapon.Report.Random(firedBy.World.SharedRandom), target.CenterPosition);
			}
		}
	}
}
