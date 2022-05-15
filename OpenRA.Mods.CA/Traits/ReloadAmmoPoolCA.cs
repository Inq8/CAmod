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

using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Reloads an ammo pool.")]
	public class ReloadAmmoPoolCAInfo : PausableConditionalTraitInfo
	{
		[Desc("Reload ammo pool with this name.")]
		public readonly string AmmoPool = "primary";

		[Desc("Reload time in ticks per Count.")]
		public readonly int Delay = 50;

		[Desc("How much ammo is reloaded after Delay.")]
		public readonly int Count = 1;

		[Desc("Whether or not reload timer should be reset when ammo has been fired.")]
		public readonly bool ResetOnFire = false;

		[Desc("Number of ticks reload timer should be delayed when ammo has been fired.")]
		public readonly int DelayOnFire = 0;

		[Desc("Delay before beginning to reload after being reset.")]
		public readonly int DelayAfterReset = 0;

		[Desc("Play this sound each time ammo is reloaded.")]
		public readonly string Sound = null;

		[Desc("Only begin reloading when ammo is equal to or less than this number. -1 means reload whenever below full ammo.")]
		public readonly int ReloadWhenAmmoReaches = -1;

		public readonly bool ShowSelectionBar = true;
		public readonly Color SelectionBarColor = Color.FromArgb(128, 200, 255);

		public override object Create(ActorInitializer init) { return new ReloadAmmoPoolCA(this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (ai.TraitInfos<AmmoPoolInfo>().Count(ap => ap.Name == AmmoPool) != 1)
				throw new YamlException("ReloadsAmmoPool.AmmoPool requires exactly one AmmoPool with matching Name!");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class ReloadAmmoPoolCA : PausableConditionalTrait<ReloadAmmoPoolCAInfo>, ITick, INotifyAttack, ISync, ISelectionBar
	{
		AmmoPool ammoPool;
		IReloadAmmoModifier[] modifiers;

		[Sync]
		int remainingTicks;
		int remainingDelay;

		public ReloadAmmoPoolCA(ReloadAmmoPoolCAInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			ammoPool = self.TraitsImplementing<AmmoPool>().Single(ap => ap.Info.Name == Info.AmmoPool);
			modifiers = self.TraitsImplementing<IReloadAmmoModifier>().ToArray();
			base.Created(self);

			self.World.AddFrameEndTask(w =>
			{
				remainingTicks = Util.ApplyPercentageModifiers(Info.Delay, modifiers.Select(m => m.GetReloadAmmoModifier()));
				remainingDelay = Info.DelayAfterReset;
			});
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			var maxTicks = Util.ApplyPercentageModifiers(Info.Delay, modifiers.Select(m => m.GetReloadAmmoModifier()));
			if (Info.ResetOnFire)
			{
				remainingTicks = maxTicks;
				remainingDelay = Info.DelayAfterReset;
			}
			else if (Info.DelayOnFire > 0)
			{
				remainingTicks += Info.DelayOnFire;

				if (remainingTicks > maxTicks)
					remainingTicks = maxTicks;

				if (remainingTicks == maxTicks)
					remainingDelay = Info.DelayAfterReset;
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		void ITick.Tick(Actor self)
		{
			if (IsTraitPaused || IsTraitDisabled)
				return;

			Reload(self, Info.Delay, Info.Count, Info.Sound);
		}

		protected virtual void Reload(Actor self, int reloadDelay, int reloadCount, string sound)
		{
			if (--remainingDelay > 0)
				return;

			if (Info.ReloadWhenAmmoReaches > -1 && ammoPool.CurrentAmmoCount > Info.ReloadWhenAmmoReaches)
				return;

			if (((reloadCount > 0 && !ammoPool.HasFullAmmo) || (reloadCount < 0 && ammoPool.HasAmmo)) && --remainingTicks == 0)
			{
				remainingTicks = Util.ApplyPercentageModifiers(reloadDelay, modifiers.Select(m => m.GetReloadAmmoModifier()));
				if (!string.IsNullOrEmpty(sound))
					Game.Sound.PlayToPlayer(SoundType.World, self.Owner, sound, self.CenterPosition);

				if (reloadCount < 0)
					ammoPool.TakeAmmo(self, -reloadCount);
				else
					ammoPool.GiveAmmo(self, reloadCount);
			}
		}

		float ISelectionBar.GetValue()
		{
			if (!Info.ShowSelectionBar || remainingDelay > 0)
				return 0;
			var maxTicks = Util.ApplyPercentageModifiers(Info.Delay, modifiers.Select(m => m.GetReloadAmmoModifier()));
			if (remainingTicks == maxTicks)
				return 0;

			return (float)(maxTicks - remainingTicks) / maxTicks;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }

		protected override void TraitDisabled(Actor self)
		{
			remainingTicks = Util.ApplyPercentageModifiers(Info.Delay, modifiers.Select(m => m.GetReloadAmmoModifier()));
			remainingDelay = Info.DelayAfterReset;
		}
	}
}
