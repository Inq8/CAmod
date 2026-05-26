#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	[Desc("This warhead can attach a DelayedWeapon to the target. Requires an appropriate type of DelayedWeaponAttachable trait to function properly.")]
	public class AttachDelayedWeaponWarhead : Warhead, IRulesetLoaded<WeaponInfo>
	{
		[WeaponReference]
		[FieldLoader.Require]
		public readonly string Weapon = "";

		[FieldLoader.Require]
		[Desc("Type of the DelayedWeapon.")]
		public readonly string Type = "";

		[Desc("Range of targets to be attached. If zero, only the targeted actor will be affected.")]
		public readonly WDist Range = WDist.Zero;

		[Desc("Trigger the DelayedWeapon after these amount of ticks.")]
		public readonly int TriggerTime = 30;

		[Desc("If true, trigger time is added for every 100 value of the target.")]
		public readonly bool ScaleTriggerTimeWithValue = false;

		[Desc("DeathType(s) that trigger the DelayedWeapon to activate. Leave empty to always trigger the DelayedWeapon on death.")]
		public readonly BitSet<DamageType> DeathTypes = default(BitSet<DamageType>);

		[Desc("List of sounds that can be played on attaching.")]
		public readonly string[] AttachSounds = new string[0];

		[Desc("List of sounds that can be played if attaching is not possible.")]
		public readonly string[] MissSounds = new string[0];

		[WeaponReference]
		public readonly string MissWeapon = null;

		public WeaponInfo WeaponInfo;
		public WeaponInfo MissWeaponInfo;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			var weaponToLower = Weapon.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out WeaponInfo))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{weaponToLower}'");

			var missWeaponToLower = Weapon.ToLowerInvariant();
			if (MissWeapon != null && !rules.Weapons.TryGetValue(missWeaponToLower, out MissWeaponInfo))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{missWeaponToLower}'");
		}

		public int CalculatedTriggerTime { get; private set; }

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;

			if (!target.IsValidFor(firedBy))
				return;

			var pos = target.CenterPosition;

			var world = firedBy.World;
			if (Range == WDist.Zero)
			{
				var directTarget = args.WeaponTarget.Type == TargetType.Actor ? args.WeaponTarget.Actor : target.Actor;
				if (TryAttachToActor(directTarget, pos, firedBy, args, world))
					return;
			}
			else
			{
				var availableActors = firedBy.World.FindActorsOnCircle(pos, Range);

				foreach (var actor in availableActors)
				{
					if (TryAttachToActor(actor, pos, firedBy, args, world))
						return;
				}
			}

			HandleMiss(firedBy, pos);
		}

		void HandleMiss(Actor firedBy, WPos pos)
		{
			if (MissWeapon != null)
				MissWeaponInfo.Impact(Target.FromPos(pos), firedBy);
			else
			{
				var failSound = MissSounds.RandomOrDefault(firedBy.World.LocalRandom);
				if (failSound != null)
					Game.Sound.Play(SoundType.World, failSound, pos);
			}
		}

		bool TryAttachToActor(Actor actor, WPos pos, Actor firedBy, WarheadArgs args, World world)
		{
			if (actor == null || !IsValidAgainst(actor, firedBy) || actor.IsDead)
				return false;

			var attachable = actor.TraitsImplementing<DelayedWeaponAttachable>().FirstOrDefault(a => a.CanAttach(Type));
			if (attachable == null)
				return false;

			if (Range != WDist.Zero)
			{
				var activeShapes = actor.TraitsImplementing<HitShape>().Where(Exts.IsTraitEnabled);
				if (!activeShapes.Any())
					return false;

				var distance = activeShapes.Min(t => t.DistanceFromEdge(actor, pos));
				if (distance > Range)
					return false;
			}

			CalculatedTriggerTime = TriggerTime;

			if (ScaleTriggerTimeWithValue)
			{
				var valued = actor.Info.TraitInfoOrDefault<ValuedInfo>();
				if (valued != null)
					CalculatedTriggerTime = (valued.Cost / 100) * TriggerTime;
			}

			attachable.Attach(new DelayedWeaponTrigger(this, args));

			var attachSound = AttachSounds.RandomOrDefault(world.LocalRandom);
			if (attachSound != null)
				Game.Sound.Play(SoundType.World, attachSound, pos);

			return true;
		}
	}
}
