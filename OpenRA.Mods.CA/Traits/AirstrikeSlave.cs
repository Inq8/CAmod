#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.CA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Can be slaved to a spawner.")]
	public class AirstrikeSlaveInfo : SpawnerSlaveBaseInfo
	{
		public override object Create(ActorInitializer init) { return new AirstrikeSlave(init, this); }
	}

	public class AirstrikeSlave : SpawnerSlaveBase, INotifyIdle
	{
		public readonly AirstrikeSlaveInfo Info;

		WPos finishEdge;
		WVec spawnOffset;

		AirstrikeMaster spawnerMaster;

		public AirstrikeSlave(ActorInitializer init, AirstrikeSlaveInfo info)
			: base(init, info)
		{
			Info = info;
		}

		public void SetSpawnInfo(WPos finishEdge, WVec spawnOffset)
		{
			this.finishEdge = finishEdge;
			this.spawnOffset = spawnOffset;
		}

		public void LeaveMap(Actor self)
		{
			// Hopefully, self will be disposed shortly afterwards by SpawnerSlaveDisposal policy.
			if (Master == null || Master.IsDead)
				return;

			// Proceed with enter, if already at it.
			if (self.CurrentActivity is ReturnAirstrikeMaster)
				return;

			// Cancel whatever else self was doing and return.
			self.QueueActivity(false, new ReturnAirstrikeMaster(Master, spawnerMaster, finishEdge + spawnOffset));
		}

		public override void LinkMaster(Actor self, Actor master, SpawnerMasterBase spawnerMaster)
		{
			base.LinkMaster(self, master, spawnerMaster);
			this.spawnerMaster = spawnerMaster as AirstrikeMaster;
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			LeaveMap(self);
		}
	}
}
