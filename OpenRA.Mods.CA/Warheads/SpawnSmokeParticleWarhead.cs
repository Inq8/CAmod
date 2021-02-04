#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Mods.CA.Effects;
using OpenRA.Mods.CA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	public class SpawnSmokeParticleWarhead : WarheadAS, IRulesetLoaded<WeaponInfo>, ISmokeParticleInfo
	{
		[Desc("Amount of particles spawned. Two values mean actual amount will vary between them.")]
		public readonly int[] Count = { 1 };

		[FieldLoader.Require]
		[Desc("The duration of an individual particle. Two values mean actual lifetime will vary between them.")]
		public readonly int[] Duration;

		[Desc("Randomize particle forward movement.")]
		public readonly WDist[] Speed = { WDist.Zero };

		[Desc("Randomize particle gravity.")]
		public readonly WDist[] Gravity = { WDist.Zero };

		[Desc("Randomize particle turnrate.")]
		public readonly int TurnRate = 0;

		[Desc("Rate to reset particle movement properties.")]
		public readonly int RandomRate = 4;

		[Desc("Which image to use.")]
		public readonly string Image = "particles";

		[FieldLoader.Require]
		[SequenceReference(nameof(Image))]
		[Desc("Which sequence to use.")]
		public readonly string[] Sequences = null;

		[PaletteReference]
		[Desc("Which palette to use.")]
		public readonly string Palette = null;

		[Desc("Defines particle ownership (invoker if unset).")]
		public readonly bool Neutral = false;

		[WeaponReference]
		[Desc("Has to be defined in weapons.yaml, if defined, as well.")]
		public readonly string Weapon = null;

		WeaponInfo weapon;

		string ISmokeParticleInfo.Image
		{
			get { return Image; }
		}

		string[] ISmokeParticleInfo.Sequences
		{
			get { return Sequences; }
		}

		string ISmokeParticleInfo.Palette
		{
			get { return Palette; }
		}

		WDist[] ISmokeParticleInfo.Speed
		{
			get { return Speed; }
		}

		WDist[] ISmokeParticleInfo.Gravity
		{
			get { return Gravity; }
		}

		int[] ISmokeParticleInfo.Duration
		{
			get { return Duration; }
		}

		WeaponInfo ISmokeParticleInfo.Weapon
		{
			get { return weapon; }
		}

		int ISmokeParticleInfo.TurnRate
		{
			get { return TurnRate; }
		}

		int ISmokeParticleInfo.RandomRate
		{
			get { return RandomRate; }
		}

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (string.IsNullOrEmpty(Weapon))
				return;

			if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out weapon))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(Weapon.ToLowerInvariant()));
		}

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			if (!target.IsValidFor(firedBy))
				return;

			if (!IsValidImpact(target.CenterPosition, firedBy))
				return;

			var count = Count.Length == 2
				? firedBy.World.SharedRandom.Next(Count[0], Count[1])
				: Count[0];

			// Lambdas can't use 'in' variables, so capture a copy for later
			var delayedTarget = target;

			for (var i = 0; i < count; i++)
				firedBy.World.AddFrameEndTask(w => w.Add(new SmokeParticle(Neutral || firedBy.IsDead ? firedBy.World.WorldActor : firedBy, this, delayedTarget.CenterPosition)));
		}
	}
}
