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
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	class EnterCarrierMaster : Enter
	{
		readonly Actor master;
		readonly CarrierMaster spawnerMaster;

		public EnterCarrierMaster(Actor self, Actor master, CarrierMaster spawnerMaster)
			: base(self, Target.FromActor(master))
		{
			this.master = master;
			this.spawnerMaster = spawnerMaster;
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			if (master.IsDead)
				return;

			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead || master.IsDead)
					return;

				spawnerMaster.PickupSlave(master, self);
				w.Remove(self);

				if (spawnerMaster.CarrierMasterInfo.InstantRepair)
				{
					var health = self.Trait<Health>();
					self.InflictDamage(self, new Damage(-health.MaxHP));
				}

				// Delayed launching is handled at spawner.
				var ammoPools = self.TraitsImplementing<AmmoPool>().ToArray();
				if (ammoPools != null)
					foreach (var pool in ammoPools)
						while (pool.GiveAmmo(self, 1))
						{ }
			});
		}
	}
}
