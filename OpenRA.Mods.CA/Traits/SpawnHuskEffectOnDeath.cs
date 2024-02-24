#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Spawn projectile as husk upon death.")]
	public class SpawnHuskEffectOnDeathInfo : ConditionalTraitInfo, IRulesetLoaded
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Weapon to spawn on death as husk.")]
		public readonly string Weapon = null;

		[Desc("DeathType(s) that trigger the effect. Leave empty to always trigger an effect.")]
		public readonly BitSet<DamageType> DeathTypes = default;

		[Desc("Offset relative to actor's position to fire husk weapon from on death.")]
		public readonly WVec LocalOffset = WVec.Zero;

		[Desc("Give random facing instead of actor facing to husk weapon.")]
		public readonly bool RandomFacing = false;

		[Desc("Target offset relative to actor's position to fire husk weapon to on death.")]
		public readonly WVec TargetOffset = new(200, 0, 0);

		[Desc("Always target ground level when fire at TargetOffset.")]
		public readonly bool ForceToGround = true;

		[Desc("Pass current actor speed as RangeModifier to husk weapon.",
			"Only supports aircraft for now.")]
		public readonly bool UnitSpeedAsRangeModifier = true;

		public WeaponInfo WeaponInfo { get; private set; }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (string.IsNullOrEmpty(Weapon))
				return;

			var weaponToLower = Weapon.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out var weapon))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{weaponToLower}'");

			WeaponInfo = weapon;

			base.RulesetLoaded(rules, ai);
		}

		public override object Create(ActorInitializer init) { return new SpawnHuskEffectOnDeath(this); }
	}

	public class SpawnHuskEffectOnDeath : ConditionalTrait<SpawnHuskEffectOnDeathInfo>, INotifyKilled
	{
		public SpawnHuskEffectOnDeath(SpawnHuskEffectOnDeathInfo info)
			: base(info) { }

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled || (!Info.DeathTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(Info.DeathTypes)))
				return;

			var weapon = Info.WeaponInfo;
			var body = self.TraitOrDefault<BodyOrientation>();
			var facing = Info.RandomFacing ? new WAngle(self.World.SharedRandom.Next(1024)) : self.TraitOrDefault<IFacing>()?.Facing;
			if (!facing.HasValue) facing = WAngle.Zero;

			var epicenter = self.CenterPosition + (body != null
					? body.LocalToWorld(Info.LocalOffset.Rotate(body.QuantizeOrientation(self.Orientation)))
					: Info.LocalOffset);
			var world = self.World;

			var map = world.Map;
			var targetpos = epicenter + body.LocalToWorld(new WVec(Info.TargetOffset.Length, 0, 0).Rotate(body.QuantizeOrientation(self.Orientation)));
			var target = Target.FromPos(new WPos(targetpos.X, targetpos.Y, Info.ForceToGround ? map.CenterOfCell(map.CellContaining(targetpos)).Z : targetpos.Z));

			var rangeModifiers = Array.Empty<int>();
			if (Info.UnitSpeedAsRangeModifier)
			{
				var aircraft = self.TraitOrDefault<Aircraft>();
				if (aircraft != null && !self.IsIdle)
				{
					if (self.CurrentActivity is FlyIdle)
						rangeModifiers = new int[1] { aircraft.Info.CanHover ? 0 : aircraft.IdleMovementSpeed };
					else if (self.CurrentActivity.ActivitiesImplementing<Fly>().Any())
						rangeModifiers = new int[1] { aircraft.MovementSpeed };
				}
				else
					rangeModifiers = new int[1] { 0 };
			}

			var projectileArgs = new ProjectileArgs
			{
				Weapon = weapon,
				Facing = facing.Value,
				CurrentMuzzleFacing = () => facing.Value,

				DamageModifiers = Array.Empty<int>(),

				InaccuracyModifiers = Array.Empty<int>(),

				RangeModifiers = rangeModifiers,
				Source = epicenter,
				CurrentSource = () => epicenter,
				SourceActor = self,
				GuidedTarget = target,
				PassiveTarget = target.CenterPosition
			};

			if (projectileArgs.Weapon.Projectile != null)
			{
				var projectile = projectileArgs.Weapon.Projectile.Create(projectileArgs);
				if (projectile != null)
					world.AddFrameEndTask(w => w.Add(projectile));
			}

			if (weapon.Report != null && weapon.Report.Length > 0)
			{
				if (!self.World.ShroudObscures(epicenter) && !self.World.FogObscures(epicenter))
					Game.Sound.Play(SoundType.World, weapon.Report, world, epicenter);
			}
		}
	}
}
