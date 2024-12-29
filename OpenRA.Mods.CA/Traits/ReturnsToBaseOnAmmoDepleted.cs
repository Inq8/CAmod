#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("For aircraft with a Strafe AttackType, which don't return to base due to using an AttackMove.")]
	public class ReturnsToBaseOnAmmoDepletedInfo : TraitInfo
	{
		[Desc("Name of the armaments that trigger returning.")]
		public readonly HashSet<string> ArmamentNames = new() { "primary" };

		[Desc("Name(s) of AmmoPool(s) that are checked.")]
		public readonly HashSet<string> AmmoPools = new() { "primary" };

		public override object Create(ActorInitializer init) { return new ReturnsToBaseOnAmmoDepleted(init, this); }
	}

	public class ReturnsToBaseOnAmmoDepleted : INotifyAttack, INotifyCreated
	{
		readonly ReturnsToBaseOnAmmoDepletedInfo info;
		AmmoPool[] ammoPools;

		public ReturnsToBaseOnAmmoDepleted(ActorInitializer init, ReturnsToBaseOnAmmoDepletedInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			ammoPools = self.TraitsImplementing<AmmoPool>().Where(p => info.AmmoPools.Contains(p.Info.Name)).ToArray();
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (!info.ArmamentNames.Contains(a.Info.Name))
				return;

			self.World.AddFrameEndTask(w => {
				var totalAmmo = ammoPools.Sum(ap => ap.CurrentAmmoCount);

				if (ammoPools.All(ap => !ap.HasAmmo)) {
					self.QueueActivity(new ReturnToBase(self));
				}
			});
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }
	}
}
