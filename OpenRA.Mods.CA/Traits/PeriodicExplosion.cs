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
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Explodes a weapon at the actor's position when enabled."
		+ "Reload/BurstDelays are used as explosion intervals.")]
	public class PeriodicExplosionInfo : ConditionalTraitInfo, IRulesetLoaded
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Weapon to be used for explosion.")]
		public readonly string Weapon = null;

		public readonly bool ResetReloadWhenEnabled = true;

		[Desc("Which limited ammo pool should this weapon be assigned to.")]
		public readonly string AmmoPoolName = "";

		public WeaponInfo WeaponInfo { get; private set; }

		[Desc("Explosion offset relative to actor's position.")]
		public readonly WVec LocalOffset = WVec.Zero;

		[Desc("Initial Delay")]
		public readonly int InitialDelay = 0;

		[Desc("If true, will apply firepower/reload modifiers.")]
		public readonly bool ApplyModifiers = false;

		public override object Create(ActorInitializer init) { return new PeriodicExplosion(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			WeaponInfo weaponInfo;

			var weaponToLower = Weapon.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out weaponInfo))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{weaponToLower}'");

			WeaponInfo = weaponInfo;
		}
	}

	class PeriodicExplosion : ConditionalTrait<PeriodicExplosionInfo>, ITick, INotifyCreated
	{
		readonly PeriodicExplosionInfo info;
		readonly WeaponInfo weapon;
		BodyOrientation body;

		int fireDelay;
		int burst;
		int initialDelay;
		AmmoPool ammoPool;

		List<(int Tick, Action Action)> delayedActions = new List<(int, Action)>();

		IFirepowerModifier[] firepowerModifiers;
		IReloadModifier[] reloadModifiers;

		public PeriodicExplosion(Actor self, PeriodicExplosionInfo info)
			: base(info)
		{
			this.info = info;

			weapon = info.WeaponInfo;
			burst = weapon.Burst;
			initialDelay = info.InitialDelay;
		}

		protected override void Created(Actor self)
		{
			body = self.TraitOrDefault<BodyOrientation>();
			ammoPool = self.TraitsImplementing<AmmoPool>().FirstOrDefault(la => la.Info.Name == Info.AmmoPoolName);

			if (info.ApplyModifiers)
			{
				firepowerModifiers = self.TraitsImplementing<IFirepowerModifier>().ToArray();
				reloadModifiers = self.TraitsImplementing<IReloadModifier>().ToArray();
			}
			else
			{
				firepowerModifiers = Array.Empty<IFirepowerModifier>();
				reloadModifiers = Array.Empty<IReloadModifier>();
			}

			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			for (var i = 0; i < delayedActions.Count; i++)
			{
				var x = delayedActions[i];
				if (--x.Tick <= 0)
					x.Action();
				delayedActions[i] = x;
			}

			delayedActions.RemoveAll(a => a.Item1 <= 0);

			if (IsTraitDisabled)
				return;

			if (--fireDelay + initialDelay < 0)
			{
				if (ammoPool != null && !ammoPool.TakeAmmo(self, 1))
					return;

				var localoffset = body != null
					? body.LocalToWorld(info.LocalOffset.Rotate(body.QuantizeOrientation(self.Orientation)))
					: info.LocalOffset;

				var args = new WarheadArgs
				{
					Weapon = weapon,
					Source = self.CenterPosition,
					SourceActor = self,
					WeaponTarget = Target.FromPos(self.CenterPosition + localoffset)
				};

				if (info.ApplyModifiers)
					args.DamageModifiers = firepowerModifiers.Select(a => a.GetFirepowerModifier()).ToArray();

				weapon.Impact(Target.FromPos(self.CenterPosition + localoffset), args);

				if (weapon.Report != null && weapon.Report.Length > 0)
					Game.Sound.Play(SoundType.World, weapon.Report.Random(self.World.SharedRandom), self.CenterPosition);

				if (burst == weapon.Burst && weapon.StartBurstReport != null && weapon.StartBurstReport.Length > 0)
					Game.Sound.Play(SoundType.World, weapon.StartBurstReport.Random(self.World.SharedRandom), self.CenterPosition);

				if (--burst > 0)
				{
					if (weapon.BurstDelays.Length == 1)
						fireDelay = weapon.BurstDelays[0];
					else
						fireDelay = weapon.BurstDelays[weapon.Burst - (burst + 1)];
				}
				else
				{
					if (info.ApplyModifiers)
					{
						var modifiers = reloadModifiers.Select(m => m.GetReloadModifier());
						fireDelay = Util.ApplyPercentageModifiers(weapon.ReloadDelay, modifiers);
					}
					else
						fireDelay = weapon.ReloadDelay;

					burst = weapon.Burst;

					if (weapon.AfterFireSound != null && weapon.AfterFireSound.Length > 0)
					{
						ScheduleDelayedAction(weapon.AfterFireSoundDelay, () =>
						{
							Game.Sound.Play(SoundType.World, weapon.AfterFireSound.Random(self.World.SharedRandom), self.CenterPosition);
						});
					}
				}

				initialDelay = 0;
			}
		}

		protected override void TraitEnabled(Actor self)
		{
			initialDelay = info.InitialDelay;

			if (info.ResetReloadWhenEnabled)
			{
				burst = weapon.Burst;
				fireDelay = 0;
			}
		}

		protected void ScheduleDelayedAction(int t, Action a)
		{
			if (t > 0)
				delayedActions.Add((t, a));
			else
				a();
		}
	}
}
