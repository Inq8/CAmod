#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public enum ExplodedSlaveAction
	{
		DoNothing,
		Neutralize,
		Unlink
	}

	[Desc("Explodes a weapon at the actor's position when enabled."
		+ "Reload/BurstDelays are used as explosion intervals.")]
	public class PeriodicExplosionOnSlavesInfo : ConditionalTraitInfo, IRulesetLoaded, Requires<MindControllerInfo>
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

		[Desc("Types of slave that are killed when explosion is triggered.")]
		public readonly BitSet<TargetableType> KillSlaveTypes = default(BitSet<TargetableType>);

		[Desc("If true, slave dies when explosion is triggered.")]
		public readonly BitSet<DamageType> KillSlavesDamageTypes = default(BitSet<DamageType>);

		[Desc("What happens to surviving slaves after the explosion?")]
		public readonly ExplodedSlaveAction PostExplosionAction = ExplodedSlaveAction.DoNothing;

		public override object Create(ActorInitializer init) { return new PeriodicExplosionOnSlaves(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			WeaponInfo weaponInfo;

			var weaponToLower = Weapon.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out weaponInfo))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));

			WeaponInfo = weaponInfo;
		}
	}

	class PeriodicExplosionOnSlaves : ConditionalTrait<PeriodicExplosionOnSlavesInfo>, ITick, INotifyCreated
	{
		readonly PeriodicExplosionOnSlavesInfo info;
		readonly WeaponInfo weapon;
		readonly BodyOrientation body;

		int fireDelay;
		int burst;
		AmmoPool ammoPool;

		List<(int Tick, Action Action)> delayedActions = new List<(int, Action)>();

		public PeriodicExplosionOnSlaves(Actor self, PeriodicExplosionOnSlavesInfo info)
			: base(info)
		{
			this.info = info;

			weapon = info.WeaponInfo;
			burst = weapon.Burst;
			body = self.TraitOrDefault<BodyOrientation>();
		}

		protected override void Created(Actor self)
		{
			ammoPool = self.TraitsImplementing<AmmoPool>().FirstOrDefault(la => la.Info.Name == Info.AmmoPoolName);

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

			if (--fireDelay + Info.InitialDelay < 0)
			{
				var mc = self.Trait<MindController>();
				if (!mc.Slaves.Any())
					return;

				if (ammoPool != null && !ammoPool.TakeAmmo(self, 1))
					return;

				var slaves = mc.Slaves.ToList();

				for (int i = 0; i < slaves.Count; i++)
				{
					var slave = slaves[i];
					var initialBurst = burst;
					var initialFireDelay = fireDelay;

					var localoffset = body != null
						? body.LocalToWorld(info.LocalOffset.Rotate(body.QuantizeOrientation(self, self.Orientation)))
						: info.LocalOffset;

					var args = new WarheadArgs
					{
						Weapon = weapon,
						DamageModifiers = self.TraitsImplementing<IFirepowerModifier>().Select(a => a.GetFirepowerModifier()).ToArray(),
						Source = self.CenterPosition,
						SourceActor = self,
						WeaponTarget = Target.FromPos(slave.CenterPosition + localoffset)
					};

					weapon.Impact(Target.FromPos(slave.CenterPosition + localoffset), args);

					if (weapon.Report != null && weapon.Report.Any())
						Game.Sound.Play(SoundType.World, weapon.Report.Random(self.World.SharedRandom), slave.CenterPosition);

					if (burst == weapon.Burst && weapon.StartBurstReport != null && weapon.StartBurstReport.Any())
						Game.Sound.Play(SoundType.World, weapon.StartBurstReport.Random(self.World.SharedRandom), slave.CenterPosition);

					if (--burst > 0)
					{
						if (weapon.BurstDelays.Length == 1)
							fireDelay = weapon.BurstDelays[0];
						else
							fireDelay = weapon.BurstDelays[weapon.Burst - (burst + 1)];
					}
					else
					{
						var modifiers = self.TraitsImplementing<IReloadModifier>()
							.Select(m => m.GetReloadModifier());
						fireDelay = Util.ApplyPercentageModifiers(weapon.ReloadDelay, modifiers);
						burst = weapon.Burst;

						if (weapon.AfterFireSound != null && weapon.AfterFireSound.Any())
						{
							ScheduleDelayedAction(weapon.AfterFireSoundDelay, () =>
							{
								Game.Sound.Play(SoundType.World, weapon.AfterFireSound.Random(self.World.SharedRandom), slave.CenterPosition);
							});
						}
					}

					if (i < slaves.Count - 1)
					{
						burst = initialBurst;
						fireDelay = initialFireDelay;
					}

					var targetTypes = slaves[i].GetEnabledTargetTypes();
					if (Info.KillSlaveTypes.Overlaps(targetTypes))
						slaves[i].Kill(slaves[i], Info.KillSlavesDamageTypes);
				}

				if (Info.PostExplosionAction == ExplodedSlaveAction.Unlink || Info.PostExplosionAction == ExplodedSlaveAction.Neutralize)
					mc.ReleaseSlaves(self, 0);

				if (Info.PostExplosionAction == ExplodedSlaveAction.Neutralize)
				{
					for (int i = 0; i < slaves.Count; i++)
					{
						slaves[i].ChangeOwner(self.World.Players.First(p => p.InternalName == "Neutral"));
						slaves[i].CancelActivity();
					}
				}
			}
		}

		protected override void TraitEnabled(Actor self)
		{
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
