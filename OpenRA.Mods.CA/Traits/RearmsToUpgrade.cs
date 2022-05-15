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
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Grants a condition when being resupplied.")]
	public class RearmsToUpgradeInfo : PausableConditionalTraitInfo
	{
		[Desc("Ammo pool to use to trigger upgrade.")]
		public readonly string AmmoPool = "primary";

		[ActorReference]
		[Desc("Ammo pool to use to trigger upgrade.")]
		public readonly string Actor = null;

		[Desc("Ticks taken to upgrade.")]
		public readonly int Delay = 50;

		[Desc("Voice to use on upgrade completion.")]
		public readonly string UpgradeAudio = null;

		public readonly bool MustBeUndamaged = true;

		public readonly bool SkipMakeAnims = true;

		public override object Create(ActorInitializer init) { return new RearmsToUpgrade(init.Self, this); }
	}

	public class RearmsToUpgrade : PausableConditionalTrait<RearmsToUpgradeInfo>, INotifyBeingResupplied, ITick
	{
		public new RearmsToUpgradeInfo Info;
		AmmoPool ammoPool;
		int ticksUntilUpgrade;
		bool resupplying;

		public RearmsToUpgrade(Actor self, RearmsToUpgradeInfo info)
			: base(info)
		{
			Info = info;
		}

		protected override void Created(Actor self)
		{
			ResetDelay();
			ammoPool = self.TraitsImplementing<AmmoPool>().Single(ap => ap.Info.Name == Info.AmmoPool);
		}

		void INotifyBeingResupplied.StartingResupply(Actor self, Actor host)
		{
			resupplying = true;
		}

		void INotifyBeingResupplied.StoppingResupply(OpenRA.Actor self, OpenRA.Actor host)
		{
			ResetDelay();
			resupplying = false;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused || !resupplying || (Info.MustBeUndamaged && self.GetDamageState() != DamageState.Undamaged))
				return;

			if (--ticksUntilUpgrade == 0)
				Transform(self);
		}

		void Transform(Actor self)
		{
			var faction = self.Owner.Faction.InternalName;
			var facing = self.TraitOrDefault<IFacing>();
			var transform = new InstantTransform(self, Info.Actor) { ForceHealthPercentage = 0, Faction = faction };
			if (facing != null) transform.Facing = facing.Facing;
			transform.SkipMakeAnims = Info.SkipMakeAnims;
			transform.Altitude = self.CenterPosition;
			self.CurrentActivity.QueueChild(transform);

			if (Info.UpgradeAudio != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.UpgradeAudio, faction);

			FillAmmo(self);
		}

		void ResetDelay()
		{
			ticksUntilUpgrade = Info.Delay;
		}

		protected override void TraitEnabled(Actor self)
		{
			EmptyAmmo(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			FillAmmo(self);
			ResetDelay();
		}

		void FillAmmo(Actor self)
		{
			while (ammoPool.CurrentAmmoCount < ammoPool.Info.Ammo)
				ammoPool.GiveAmmo(self, 1);
		}

		void EmptyAmmo(Actor self)
		{
			while (ammoPool.CurrentAmmoCount > 0)
				ammoPool.TakeAmmo(self, 1);
		}
	}
}
