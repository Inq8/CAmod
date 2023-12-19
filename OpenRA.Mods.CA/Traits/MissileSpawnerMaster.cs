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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("This actor can spawn missile actors.")]
	public class MissileSpawnerMasterInfo : SpawnerMasterBaseInfo
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

		[Desc("Offset at which the exiting actor is spawned relative to the center of the master actor/turret.")]
		public readonly WVec SpawnOffset = WVec.Zero;

		[Desc("Which turret (if present) should the missile be launched from.")]
		public readonly string Turret = null;

		public override object Create(ActorInitializer init) { return new MissileSpawnerMaster(init, this); }
	}

	public class MissileSpawnerMaster : SpawnerMasterBase, ITick, INotifyAttack
	{
		readonly BodyOrientation body;
		readonly Dictionary<string, Stack<int>> spawnContainTokens = new Dictionary<string, Stack<int>>();
		public readonly MissileSpawnerMasterInfo MissileSpawnerMasterInfo;
		readonly Stack<int> loadedTokens = new Stack<int>();
		Turreted turreted;
		WAngle spawnFacing;

		int respawnTicks = 0;

		int launchCondition = Actor.InvalidConditionToken;
		int launchConditionTicks;

		public MissileSpawnerMaster(ActorInitializer init, MissileSpawnerMasterInfo info)
			: base(init, info)
		{
			MissileSpawnerMasterInfo = info;
			body = init.Self.TraitOrDefault<BodyOrientation>();
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			turreted = self.TraitsImplementing<Turreted>().FirstOrDefault(t => t.Name == MissileSpawnerMasterInfo.Turret);

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
			var missileTrait = se.Actor.TraitOrDefault<MissileBase>();
			missileTrait.SetTarget(target);

			var spawnPos = self.CenterPosition;
			if (turreted != null && body != null)
				spawnPos += turreted.Position(self);

			spawnFacing = (target.CenterPosition - spawnPos).Yaw;

			SpawnIntoWorld(self, se.Actor, spawnPos);

			Stack<int> spawnContainToken;
			if (spawnContainTokens.TryGetValue(a.Info.Name, out spawnContainToken) && spawnContainToken.Count > 0)
				self.RevokeCondition(spawnContainToken.Pop());

			if (loadedTokens.Count > 0)
				self.RevokeCondition(loadedTokens.Pop());

			// Queue attack order, too.
			self.World.AddFrameEndTask(w =>
			{
				// invalidate the slave entry so that slave will regen.
				se.Actor = null;
			});

			// Set clock so that regen happens.
			if (respawnTicks <= 0) // Don't interrupt an already running timer!
				respawnTicks = Util.ApplyPercentageModifiers(Info.RespawnTicks, reloadModifiers.Select(rm => rm.GetReloadModifier()));
		}

		SpawnerSlaveBaseEntry GetLaunchable()
		{
			foreach (var se in SlaveEntries)
				if (se.IsValid)
					return se;

			return null;
		}

		public override void Replenish(Actor self, SpawnerSlaveBaseEntry entry)
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

		public override void SpawnIntoWorld(Actor self, Actor slave, WPos pos)
		{
			SetSpawnedFacing(slave, spawnFacing);

			var offset = body != null ? body.LocalToWorld(MissileSpawnerMasterInfo.SpawnOffset.Rotate(WRot.FromYaw(spawnFacing))) : WVec.Zero;

			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead)
					return;

				slave.Trait<IPositionable>().SetCenterPosition(slave, pos + offset);

				w.Add(slave);
			});
		}
	}
}
