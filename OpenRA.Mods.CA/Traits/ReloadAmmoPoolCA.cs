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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Reloads an ammo pool.",
		"CA version adds a progress bar, allows for reload to only begin below a certain ammo threshold",
		"and allows reload to be delayed on fire/reset (as opposed to just resetting on firing).")]
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

		[Desc("If greater than zero, instead of resettng the reload, the reload timer will reverse by this amount per tick.")]
		public readonly int DrainAmountOnDisabled = 0;

		public readonly bool ShowSelectionBar = true;
		public readonly Color SelectionBarColor = Color.FromArgb(128, 200, 255);

		public override object Create(ActorInitializer init) { return new ReloadAmmoPoolCA(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (ai.TraitInfos<AmmoPoolInfo>().Count(ap => ap.Name == AmmoPool) != 1)
				throw new YamlException("ReloadAmmoPoolCA.AmmoPool requires exactly one AmmoPool with matching Name!");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class ReloadAmmoPoolCA : PausableConditionalTrait<ReloadAmmoPoolCAInfo>, ITick, INotifyAttack, ISync, ISelectionBar
	{
		AmmoPool ammoPool;
		IReloadAmmoModifier[] modifiers;
		Actor self;

		[Sync]
		int reloadTicks;
		int remainingDelay;

		int MaxTicks => Util.ApplyPercentageModifiers(Info.Delay, modifiers.Select(m => m.GetReloadAmmoModifier()));
		int cachedMaxTicks;

		public ReloadAmmoPoolCA(Actor self, ReloadAmmoPoolCAInfo info)
			: base(info)
		{
			this.self = self;
		}

		protected override void Created(Actor self)
		{
			ammoPool = self.TraitsImplementing<AmmoPool>().Single(ap => ap.Info.Name == Info.AmmoPool);
			modifiers = self.TraitsImplementing<IReloadAmmoModifier>().ToArray();
			base.Created(self);

			self.World.AddFrameEndTask(w =>
			{
				reloadTicks = 0;
				remainingDelay = Info.DelayAfterReset;
				cachedMaxTicks = MaxTicks;
			});
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (Info.ResetOnFire)
			{
				reloadTicks = 0;
				remainingDelay = Info.DelayAfterReset;
			}
			else if (Info.DelayOnFire > 0)
			{
				reloadTicks -= Info.DelayOnFire;
				if (reloadTicks < 0)
					reloadTicks = 0;

				if (reloadTicks == 0)
					remainingDelay = Info.DelayAfterReset;
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		void ITick.Tick(Actor self)
		{
			if (IsTraitPaused)
				return;

			if (IsTraitDisabled)
			{
				if (Info.DrainAmountOnDisabled > 0)
				{
					if (reloadTicks > 0)
						reloadTicks -= Info.DrainAmountOnDisabled;

					if (reloadTicks < 0)
						reloadTicks = 0;
				}

				return;
			}

			Reload(self, Info.Delay, Info.Count, Info.Sound);
		}

		protected virtual void Reload(Actor self, int reloadDelay, int reloadCount, string sound)
		{
			if (--remainingDelay > 0)
				return;

			if (Info.ReloadWhenAmmoReaches > -1 && ammoPool.CurrentAmmoCount > Info.ReloadWhenAmmoReaches)
				return;

			if ((reloadCount > 0 && ammoPool.HasFullAmmo) || (reloadCount < 0 && !ammoPool.HasAmmo))
				return;

			var cachedMaxTicks = MaxTicks;

			if (++reloadTicks >= cachedMaxTicks)
			{
				reloadTicks = 0;
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
			if (!Info.ShowSelectionBar || remainingDelay > 0 || !self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return 0;

			if (reloadTicks == 0)
				return 0;

			return (float)reloadTicks / cachedMaxTicks;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }

		protected override void TraitDisabled(Actor self)
		{
			if (Info.DrainAmountOnDisabled == 0)
				reloadTicks = 0;

			remainingDelay = Info.DelayAfterReset;
		}
	}
}
