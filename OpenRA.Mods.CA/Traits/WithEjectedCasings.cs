#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Ejects casings when the actor fires weapons.",
		"Supports burst firing of casings over time using the casing weapon's Burst and BurstDelays properties,",
		"or the BurstOverride and BurstDelayOverride trait properties for custom control.")]
	public class WithEjectedCasingsInfo : PausableConditionalTraitInfo
	{
		[WeaponReference]
		[Desc("Has to be defined in weapons.yaml as well.")]
		public readonly string CasingWeapon = null;

		[Desc("Casing spawn position relative to turret or body, (forward, right, up) triples.",
			"If multiple offsets are defined, they will be matched to armament LocalOffset entries.")]
		public readonly WVec[] CasingSpawnLocalOffset = Array.Empty<WVec>();

		[Desc("Casing target position relative to turret or body, (forward, right, up) triples.",
			"If multiple offsets are defined, they will be matched to armament LocalOffset entries.")]
		public readonly WVec[] CasingTargetOffset = Array.Empty<WVec>();

		[Desc("Casing target position will be modified to ground level.")]
		public readonly bool CasingHitGroundLevel = true;

		[Desc("Only eject casings for armaments with these names. If empty, all armaments will eject casings.")]
		public readonly HashSet<string> ArmamentNames = new();

		[Desc("Override the casing weapon's burst count. If 0, uses the weapon's own burst setting.")]
		public readonly int BurstOverride = 0;

		[Desc("Override the casing weapon's burst delays. If empty, uses the weapon's own burst delays.")]
		public readonly int[] BurstDelayOverride = Array.Empty<int>();

		public WeaponInfo CasingWeaponInfo { get; private set; }

		public override object Create(ActorInitializer init) { return new WithEjectedCasings(init, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (CasingWeapon != null)
			{
				var casingWeaponToLower = CasingWeapon.ToLowerInvariant();
				if (!rules.Weapons.TryGetValue(casingWeaponToLower, out var casingWeaponInfo))
					throw new YamlException($"Weapons Ruleset does not contain an entry '{casingWeaponToLower}'");

				CasingWeaponInfo = casingWeaponInfo;
			}
		}
	}

	public class WithEjectedCasings : PausableConditionalTrait<WithEjectedCasingsInfo>, INotifyCreated, INotifyAttack, ITick
	{
		BodyOrientation coords;
		Turreted[] turrets;

		readonly Dictionary<string, Turreted> turretsByArmament = new();
		readonly List<(int Ticks, int Burst, ProjectileArgs Args)> scheduledCasings = new();

		public WithEjectedCasings(ActorInitializer init, WithEjectedCasingsInfo info)
			: base(info) { }

		void INotifyCreated.Created(Actor self)
		{
			coords = self.Trait<BodyOrientation>();
			turrets = self.TraitsImplementing<Turreted>().ToArray();

			// Map armaments to their turrets
			var armaments = self.TraitsImplementing<Armament>();
			foreach (var armament in armaments)
			{
				var turret = turrets.FirstOrDefault(t => t.Name == armament.Info.Turret);
				if (turret != null)
					turretsByArmament[armament.Info.Name] = turret;
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (Info.CasingWeaponInfo == null)
				return;

			// Check if we should eject casings for this armament
			if (Info.ArmamentNames.Count > 0 && !Info.ArmamentNames.Contains(a.Info.Name))
				return;

			// Get the barrel index to determine which casing offset to use
			var barrelIndex = Array.IndexOf(a.Barrels, barrel);
			if (barrelIndex < 0)
				barrelIndex = 0;

			var casingSpawnOffset = Info.CasingSpawnLocalOffset.Length > barrelIndex
				? Info.CasingSpawnLocalOffset[barrelIndex]
				: (Info.CasingSpawnLocalOffset.Length > 0 ? Info.CasingSpawnLocalOffset[0] : WVec.Zero);

			var casingTargetOffset = Info.CasingTargetOffset.Length > barrelIndex
				? Info.CasingTargetOffset[barrelIndex]
				: (Info.CasingTargetOffset.Length > 0 ? Info.CasingTargetOffset[0] : WVec.Zero);

			var casingSpawnPosition = self.CenterPosition + CalculateOffset(self, a, casingSpawnOffset);
			var casingHitPosition = self.CenterPosition + CalculateOffset(self, a, casingTargetOffset);

			if (Info.CasingHitGroundLevel)
				casingHitPosition -= new WVec(0, 0, self.World.Map.DistanceAboveTerrain(casingHitPosition).Length);

			var casingFacing = (casingHitPosition - casingSpawnPosition).Yaw;

			var args = new ProjectileArgs
			{
				Weapon = Info.CasingWeaponInfo,
				Facing = casingFacing,
				CurrentMuzzleFacing = () => casingFacing,

				DamageModifiers = Array.Empty<int>(),
				InaccuracyModifiers = Array.Empty<int>(),
				RangeModifiers = Array.Empty<int>(),

				Source = casingSpawnPosition,
				CurrentSource = () => casingSpawnPosition,
				SourceActor = self,
				PassiveTarget = casingHitPosition
			};

			// Handle casing burst
			var casingWeapon = Info.CasingWeaponInfo;
			var burstCount = Info.BurstOverride > 0 ? Info.BurstOverride : casingWeapon.Burst;
			var burstDelays = Info.BurstDelayOverride.Length > 0 ? Info.BurstDelayOverride : casingWeapon.BurstDelays;

			if (burstCount > 1)
			{
				// Schedule multiple casings over time
				for (var i = 0; i < burstCount; i++)
				{
					var delay = 0;
					if (i > 0)
					{
						if (burstDelays.Length == 1)
							delay = burstDelays[0] * i;
						else if (burstDelays.Length > i - 1)
							delay = burstDelays.Take(i).Sum();
					}

					scheduledCasings.Add((delay, i + 1, args));
				}
			}
			else
			{
				// Fire single casing immediately
				FireCasing(args);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			// Process scheduled casings
			for (var i = 0; i < scheduledCasings.Count; i++)
			{
				var scheduled = scheduledCasings[i];
				if (--scheduled.Ticks <= 0)
				{
					FireCasing(scheduled.Args);
					scheduledCasings.RemoveAt(i--);
				}
				else
				{
					scheduledCasings[i] = scheduled;
				}
			}
		}

		static void FireCasing(ProjectileArgs args)
		{
			if (args.Weapon.Projectile != null)
			{
				var projectile = args.Weapon.Projectile.Create(args);
				if (projectile != null)
					args.SourceActor.World.Add(projectile);
			}
		}

		WVec CalculateOffset(Actor self, Armament armament, WVec localOffset)
		{
			// Apply recoil (casings should spawn considering current weapon recoil)
			var effectOffset = localOffset + new WVec(-armament.Recoil, WDist.Zero, WDist.Zero);

			// Get the turret for this armament
			turretsByArmament.TryGetValue(armament.Info.Name, out var turret);

			// Turret coordinates to body coordinates
			var bodyOrientation = coords.QuantizeOrientation(self.Orientation);
			if (turret != null)
				effectOffset = effectOffset.Rotate(turret.WorldOrientation) + turret.Offset.Rotate(bodyOrientation);
			else
				effectOffset = effectOffset.Rotate(bodyOrientation);

			// Body coordinates to world coordinates
			return coords.LocalToWorld(effectOffset);
		}
	}
}
