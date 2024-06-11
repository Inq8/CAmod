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
using OpenRA.Activities;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	class EnterAirstrikeMaster : Activity
	{
		readonly Actor master;
		readonly AirstrikeMaster spawnerMaster;

		public EnterAirstrikeMaster(Actor master, AirstrikeMaster spawnerMaster)
		{
			this.master = master;
			this.spawnerMaster = spawnerMaster;
		}

		public override bool Tick(Actor self)
		{
			if (master.IsDead)
				return true;

			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead || master.IsDead)
					return;

				if (spawnerMaster == null || !spawnerMaster.SlaveEntries.Select(se => se.Actor).Contains(self))
					return;

				spawnerMaster.PickupSlave(master, self);
				w.Remove(self);

				if (spawnerMaster.AirstrikeMasterInfo.InstantRepair)
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

			return true;
		}
	}

	class ReturnAirstrikeMaster : Activity
	{
		readonly Actor master;
		readonly AirstrikeMaster spawnerMaster;
		readonly WPos edgePos;

		public ReturnAirstrikeMaster(Actor master, AirstrikeMaster spawnerMaster, WPos edgePos)
		{
			this.master = master;
			this.spawnerMaster = spawnerMaster;
			this.edgePos = edgePos;
		}

		protected override void OnFirstRun(Actor self)
		{
			if (spawnerMaster.AirstrikeMasterInfo.SendAndForget)
			{
				QueueChild(new FlyOffMap(self));
			}
			else
			{
				QueueChild(new Fly(self, Target.FromPos(edgePos)));
				QueueChild(new EnterAirstrikeMaster(master, spawnerMaster));
			}
		}
	}
}
