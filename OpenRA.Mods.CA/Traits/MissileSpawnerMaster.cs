﻿#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("This actor can spawn missile actors.")]
	public class MissileSpawnerMasterInfo : BaseSpawnerMasterInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant to self right after launching a spawned unit. (Used by V3 to make immobile.)")]
		public readonly string LaunchingCondition = null;

		[Desc("After this many ticks, we remove the condition.")]
		public readonly int LaunchingTicks = 15;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while spawned units are loaded.",
			"Condition can stack with multiple spawns.")]
		public readonly string LoadedCondition = null;

		[Desc("Conditions to grant when specified actors are contained inside the transport.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> SpawnContainConditions = new Dictionary<string, string>();

		[GrantedConditionReference]
		public IEnumerable<string> LinterSpawnContainConditions { get { return SpawnContainConditions.Values; } }

		public override object Create(ActorInitializer init) { return new MissileSpawnerMaster(init, this); }
	}

	public class MissileSpawnerMaster : BaseSpawnerMaster, ITick, INotifyAttack
	{
		readonly Dictionary<string, Stack<int>> spawnContainTokens = new Dictionary<string, Stack<int>>();
		public readonly MissileSpawnerMasterInfo MissileSpawnerMasterInfo;
		readonly Stack<int> loadedTokens = new Stack<int>();

		int respawnTicks = 0;

		int launchCondition = Actor.InvalidConditionToken;
		int launchConditionTicks;

		public MissileSpawnerMaster(ActorInitializer init, MissileSpawnerMasterInfo info)
			: base(init, info)
		{
			MissileSpawnerMasterInfo = info;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			// Spawn initial load.
			var burst = Info.InitialActorCount == -1 ? Info.Actors.Length : Info.InitialActorCount;
			for (var i = 0; i < burst; i++)
				Replenish(self, SlaveEntries);
		}

		public override void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			// Do nothing, because missiles can't be captured or mind controlled.
			return;
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		// The rate of fire of the dummy weapon determines the launch cycle as each shot
		// invokes Attacking()
		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (!Info.ArmamentNames.Contains(a.Info.Name))
				return;

			// Issue retarget order for already launched ones
			foreach (var slave in SlaveEntries)
				if (slave.IsValid)
					slave.SpawnerSlave.Attack(slave.Actor, target);

			var se = GetLaunchable();
			if (se == null)
				return;

			if (MissileSpawnerMasterInfo.LaunchingCondition != null)
			{
				if (launchCondition == Actor.InvalidConditionToken)
					launchCondition = self.GrantCondition(MissileSpawnerMasterInfo.LaunchingCondition);

				launchConditionTicks = MissileSpawnerMasterInfo.LaunchingTicks;
			}

			// Program the trajectory.
			var bm = se.Actor.Trait<BallisticMissile>();
			bm.Target = Target.FromPos(target.CenterPosition);

			SpawnIntoWorld(self, se.Actor, self.CenterPosition);

			Stack<int> spawnContainToken;
			if (spawnContainTokens.TryGetValue(a.Info.Name, out spawnContainToken) && spawnContainToken.Any())
				self.RevokeCondition(spawnContainToken.Pop());

			if (loadedTokens.Any())
				self.RevokeCondition(loadedTokens.Pop());

			// Queue attack order, too.
			self.World.AddFrameEndTask(w =>
			{
				// invalidate the slave entry so that slave will regen.
				se.Actor = null;
			});

			// Set clock so that regen happens.
			if (respawnTicks <= 0) // Don't interrupt an already running timer!
				respawnTicks = Info.RespawnTicks;
		}

		BaseSpawnerSlaveEntry GetLaunchable()
		{
			foreach (var se in SlaveEntries)
				if (se.IsValid)
					return se;

			return null;
		}

		public override void Replenish(Actor self, BaseSpawnerSlaveEntry entry)
		{
			base.Replenish(self, entry);

			string spawnContainCondition;

			if (MissileSpawnerMasterInfo.SpawnContainConditions.TryGetValue(entry.Actor.Info.Name, out spawnContainCondition))
				spawnContainTokens.GetOrAdd(entry.Actor.Info.Name).Push(self.GrantCondition(spawnContainCondition));
			if (MissileSpawnerMasterInfo.LoadedCondition != null)
				loadedTokens.Push(self.GrantCondition(MissileSpawnerMasterInfo.LoadedCondition));
		}

		void ITick.Tick(Actor self)
		{
			if (launchCondition != Actor.InvalidConditionToken && --launchConditionTicks < 0)
				launchCondition = self.RevokeCondition(launchCondition);

			if (respawnTicks > 0)
			{
				respawnTicks--;

				// Time to respawn someting.
				if (respawnTicks <= 0)
				{
					Replenish(self, SlaveEntries);

					// If there's something left to spawn, restart the timer.
					if (SelectEntryToSpawn(SlaveEntries) != null)
						respawnTicks = Util.ApplyPercentageModifiers(Info.RespawnTicks, reloadModifiers.Select(rm => rm.GetReloadModifier()));
				}
			}
		}

		public override void SpawnIntoWorld(Actor self, Actor slave, WPos centerPosition)
		{
			var exit = self.RandomExitOrDefault(self.World, null);
			SetSpawnedFacing(slave, exit);

			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead)
					return;

				var spawnOffset = exit == null ? WVec.Zero : exit.Info.SpawnOffset;
				slave.Trait<IPositionable>().SetCenterPosition(slave, centerPosition + spawnOffset);

				var location = self.World.Map.CellContaining(centerPosition + spawnOffset);

				w.Add(slave);
			});
		}
	}
}
