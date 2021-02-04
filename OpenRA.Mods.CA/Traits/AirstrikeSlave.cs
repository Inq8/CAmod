#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Can be slaved to a spawner.")]
	public class AirstrikeSlaveInfo : BaseSpawnerSlaveInfo
	{
		[Desc("Move this close to the spawner, before entering it.")]
		public readonly WDist LandingDistance = new WDist(5 * 1024);

		[Desc("We consider this is close enought to the spawner and enter it, instead of trying to reach 0 distance." +
			"This allows the spawned unit to enter the spawner while the spawner is moving.")]
		public readonly WDist CloseEnoughDistance = new WDist(128);

		public override object Create(ActorInitializer init) { return new AirstrikeSlave(init, this); }
	}

	public class AirstrikeSlave : BaseSpawnerSlave, INotifyIdle
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

		public override void LinkMaster(Actor self, Actor master, BaseSpawnerMaster spawnerMaster)
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
