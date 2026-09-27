#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	public class FireShrapnelWarhead : WarheadAS, IRulesetLoaded<WeaponInfo>
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Has to be defined in weapons.yaml as well.")]
		public readonly string Weapon = null;

		[Desc("Amount of shrapnels thrown.")]
		public readonly int[] Amount = { 1 };

		[Desc("The percentage of aiming this shrapnel to a suitable target actor.")]
		public readonly int AimChance = 0;

		[Desc("What diplomatic stances can be targeted by the shrapnel.")]
		public readonly PlayerRelationship AimTargetStances = PlayerRelationship.Ally | PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		[Desc("Allow this shrapnel to be thrown randomly when no targets found.")]
		public readonly bool ThrowWithoutTarget = true;

		[Desc("Should the shrapnel hit the direct target?")]
		public readonly bool AllowDirectHit = false;

		[Desc("Should the weapons be fired around the intended target or at the explosion's epicenter.")]
		public readonly bool AroundTarget = false;

		[Desc("Does this weapon aim at the target's center regardless of other targetable offsets?")]
		public readonly bool TargetActorCenter = false;

		[Desc("List of sounds that can be played on impact.")]
		public readonly string[] ImpactSounds = Array.Empty<string>();

		[Desc("Should the shrapnel target actors in order of distance?")]
		public readonly bool TargetClosest = false;

		[Desc("Maximum deviation from the impact orientation. Range [0, 512], 512 covers 360 degrees.")]
		public readonly WAngle MaxAngle = new WAngle(512);

		WeaponInfo weapon;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (MaxAngle.Angle > 512)
				throw new YamlException("Max angle must be in range of [0, 512], 512 covers 360 degrees.");

			if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out weapon))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{Weapon.ToLowerInvariant()}'");
		}

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			if (!target.IsValidFor(firedBy))
				return;

			var world = firedBy.World;
			var map = world.Map;

			if (!IsValidImpact(target.CenterPosition, firedBy))
				return;

			var epicenter = AroundTarget && args.WeaponTarget.Type != TargetType.Invalid
				? args.WeaponTarget.CenterPosition
				: target.CenterPosition;

			var directActors = world.FindActorsOnCircle(epicenter, WDist.Zero)
				.Where(a =>
				{
					var activeShapes = a.TraitsImplementing<HitShape>().Where(Exts.IsTraitEnabled);
					if (!activeShapes.Any())
						return false;

					var distance = activeShapes.Min(t => t.DistanceFromEdge(a, epicenter));

					if (distance != WDist.Zero)
						return false;

					return true;
				}).ToHashSet();

			var candidateActors = world.FindActorsOnCircle(epicenter, weapon.Range);
			var coneDirection = WVec.Zero;
			var coneCosine = 0;
			var coneCosineSquared = 0L;
			if (MaxAngle.Angle < 512)
			{
				var impactYaw = args.ImpactOrientation.Yaw;
				coneDirection = new WVec(impactYaw.Sin(), -impactYaw.Cos(), 0);
				coneCosine = MaxAngle.Cos();
				coneCosineSquared = (long)coneCosine * coneCosine;

				if (coneCosine > 512)
				{
					var searchRange = weapon.Range + world.ActorMap.LargestActorRadius;
					var coneRadius = new WDist((int)((long)searchRange.Length * 1024 / (2 * coneCosine)) + 1);
					var coneCenter = epicenter + coneDirection * coneRadius.Length / 1024;
					candidateActors = world.FindActorsInCircle(coneCenter, coneRadius);
				}
			}

			var availableTargetActors = candidateActors
				.Where(x => (AllowDirectHit || !directActors.Contains(x))
					&& weapon.IsValidAgainst(Target.FromActor(x), firedBy.World, firedBy)
					&& AimTargetStances.HasRelationship(firedBy.Owner.RelationshipWith(x.Owner)));

			if (MaxAngle.Angle < 512)
				availableTargetActors = availableTargetActors.Where(x => IsWithinCone(x.CenterPosition - epicenter, coneDirection, coneCosine, coneCosineSquared));

			availableTargetActors = availableTargetActors.Where(x =>
			{
				var activeShapes = x.TraitsImplementing<HitShape>().Where(Exts.IsTraitEnabled);
				return activeShapes.Any() && activeShapes.Min(t => t.DistanceFromEdge(x, epicenter)) < weapon.Range;
			});

			if (TargetClosest)
				availableTargetActors = availableTargetActors.OrderBy(x => (x.CenterPosition - epicenter).Length);
			else
				availableTargetActors = availableTargetActors.Shuffle(world.SharedRandom);

			var targetActor = availableTargetActors.GetEnumerator();

			var amount = Amount.Length == 2
					? world.SharedRandom.Next(Amount[0], Amount[1])
					: Amount[0] == 0 ? availableTargetActors.Count() : Amount[0];

			var targetFound = false;

			for (var i = 0; i < amount; i++)
			{
				var shrapnelTarget = Target.Invalid;

				if (world.SharedRandom.Next(100) < AimChance && targetActor.MoveNext())
					shrapnelTarget = Target.FromActor(targetActor.Current);

				if (ThrowWithoutTarget && shrapnelTarget.Type == TargetType.Invalid)
				{
					var yaw = MaxAngle.Angle == 0
						? args.ImpactOrientation.Yaw
						: new WAngle(args.ImpactOrientation.Yaw.Angle - MaxAngle.Angle + world.SharedRandom.Next(MaxAngle.Angle * 2));
					var rotation = WRot.FromYaw(yaw);
					var range = world.SharedRandom.Next(weapon.MinRange.Length, weapon.Range.Length);
					var targetpos = epicenter + new WVec(0, -range, 0).Rotate(rotation);
					var tpos = Target.FromPos(new WPos(targetpos.X, targetpos.Y, map.CenterOfCell(map.CellContaining(targetpos)).Z));
					if (weapon.IsValidAgainst(tpos, firedBy.World, firedBy))
						shrapnelTarget = tpos;
				}

				if (shrapnelTarget.Type == TargetType.Invalid)
					continue;

				targetFound = true;

				var shrapnelFacing = (shrapnelTarget.CenterPosition - epicenter).Yaw;

				// Lambdas can't use 'in' variables, so capture a copy for later
				var centerPosition = target.CenterPosition;

				var projectileArgs = new ProjectileArgs
				{
					Weapon = weapon,
					Facing = shrapnelFacing,
					CurrentMuzzleFacing = () => shrapnelFacing,

					DamageModifiers = !firedBy.IsDead ? firedBy.TraitsImplementing<IFirepowerModifier>()
						.Select(a => a.GetFirepowerModifier()).ToArray() : new int[0],

					InaccuracyModifiers = !firedBy.IsDead ? firedBy.TraitsImplementing<IInaccuracyModifier>()
						.Select(a => a.GetInaccuracyModifier()).ToArray() : new int[0],

					RangeModifiers = !firedBy.IsDead ? firedBy.TraitsImplementing<IRangeModifier>()
						.Select(a => a.GetRangeModifier()).ToArray() : new int[0],

					Source = target.CenterPosition,
					CurrentSource = () => centerPosition,
					SourceActor = firedBy,
					GuidedTarget = shrapnelTarget,
					PassiveTarget = TargetActorCenter ? shrapnelTarget.CenterPosition : shrapnelTarget.Positions.ClosestToIgnoringPath(epicenter)
				};

				if (projectileArgs.Weapon.Projectile != null)
				{
					var projectile = projectileArgs.Weapon.Projectile.Create(projectileArgs);
					if (projectile != null)
						firedBy.World.AddFrameEndTask(w => w.Add(projectile));

					if (projectileArgs.Weapon.Report != null && projectileArgs.Weapon.Report.Length > 0)
						Game.Sound.Play(SoundType.World, projectileArgs.Weapon.Report.Random(firedBy.World.SharedRandom), target.CenterPosition);
				}
			}

			if (targetFound)
			{
				var impactSound = ImpactSounds.RandomOrDefault(world.LocalRandom);
				if (impactSound != null)
					Game.Sound.Play(SoundType.World, impactSound, target.CenterPosition);
			}
		}

		static bool IsWithinCone(WVec delta, WVec coneDirection, int coneCosine, long coneCosineSquared)
		{
			var distanceSquared = delta.HorizontalLengthSquared;
			if (distanceSquared == 0)
				return true;

			var dot = (long)coneDirection.X * delta.X + (long)coneDirection.Y * delta.Y;
			var dotSquared = dot * dot;
			var threshold = distanceSquared * coneCosineSquared;

			return coneCosine >= 0
				? dot >= 0 && dotSquared >= threshold
				: dot >= 0 || dotSquared <= threshold;
		}
	}
}
