#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// What to do when master is killed or mind controlled
	public enum SpawnerSlaveDisposal
	{
		DoNothing,
		KillSlaves,
		GiveSlavesToAttacker
	}

	public class SpawnerSlaveBaseEntry
	{
		public string ActorName = null;
		public Actor Actor = null;
		public SpawnerSlaveBase SpawnerSlave = null;
		public bool IsLaunched;

		public bool IsValid { get { return Actor != null && !Actor.IsDead; } }
	}

	[Desc("This actor can spawn actors.")]
	public abstract class SpawnerMasterBaseInfo : PausableConditionalTraitInfo
	{
		[Desc("Spawn these units. Define this like paradrop support power.")]
		public readonly string[] Actors;

		[Desc("Slave actors to contain upon creation. Set to -1 to start with full slaves.")]
		public readonly int InitialActorCount = -1;

		[Desc("Name of the armaments that grant this condition.")]
		public readonly HashSet<string> ArmamentNames = new HashSet<string>() { "primary" };

		[Desc("What happens to the slaves when the master is killed?")]
		public readonly SpawnerSlaveDisposal SlaveDisposalOnKill = SpawnerSlaveDisposal.KillSlaves;

		[Desc("What happens to the slaves when the master is mind controlled?")]
		public readonly SpawnerSlaveDisposal SlaveDisposalOnOwnerChange = SpawnerSlaveDisposal.GiveSlavesToAttacker;

		[Desc("Only spawn initial load of slaves?")]
		public readonly bool NoRegeneration = false;

		[Desc("Spawn all slaves at once when regenerating slaves, instead of one by one?")]
		public readonly bool SpawnAllAtOnce = false;

		[Desc("Spawn regen delay, in ticks")]
		public readonly int RespawnTicks = 150;

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (Actors == null || Actors.Length == 0)
				throw new YamlException($"Actors is null or empty for a spawner trait in actor type {ai.Name}!");

			if (InitialActorCount > Actors.Length)
				throw new YamlException($"InitialActorCount can't be larger than the actors defined! (Actor type = {ai.Name})");

			if (InitialActorCount < -1)
				throw new YamlException($"InitialActorCount must be -1 or non-negative. Actor type = {ai.Name}");
		}

		public abstract override object Create(ActorInitializer init);
	}

	public abstract class SpawnerMasterBase : PausableConditionalTrait<SpawnerMasterBaseInfo>, INotifyKilled, INotifyOwnerChanged, INotifyActorDisposing
	{
		readonly Actor self;

		protected IFacing facing;

		protected IReloadModifier[] reloadModifiers;

		public readonly SpawnerSlaveBaseEntry[] SlaveEntries;

		int nextExitIndex = 0;

		public SpawnerMasterBase(ActorInitializer init, SpawnerMasterBaseInfo info)
			: base(info)
		{
			self = init.Self;

			// Initialize slave entries (doesn't instantiate the slaves yet)
			SlaveEntries = CreateSlaveEntries(info);

			for (var i = 0; i < info.Actors.Length; i++)
			{
				var entry = SlaveEntries[i];
				entry.ActorName = info.Actors[i].ToLowerInvariant();
			}
		}

		public virtual SpawnerSlaveBaseEntry[] CreateSlaveEntries(SpawnerMasterBaseInfo info)
		{
			var slaveEntries = new SpawnerSlaveBaseEntry[info.Actors.Length];

			for (var i = 0; i < slaveEntries.Length; i++)
				slaveEntries[i] = new SpawnerSlaveBaseEntry();

			return slaveEntries;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			facing = self.TraitOrDefault<IFacing>();

			reloadModifiers = self.TraitsImplementing<IReloadModifier>().ToArray();
		}

		/// <summary>
		/// Replenish destoyed slaves or create new ones from nothing.
		/// Follows policy defined by Info.OneShotSpawn.
		/// </summary>
		public void Replenish(Actor self, SpawnerSlaveBaseEntry[] slaveEntries)
		{
			if (Info.SpawnAllAtOnce)
			{
				foreach (var se in slaveEntries)
				{
					if (!se.IsValid)
						Replenish(self, se);
				}
			}
			else
			{
				var entry = SelectEntryToSpawn(slaveEntries);

				// All are alive and well.
				if (entry == null)
					return;

				Replenish(self, entry);
			}
		}

		/// <summary>
		/// Replenish one slave entry.
		/// </summary>
		public virtual void Replenish(Actor self, SpawnerSlaveBaseEntry entry)
		{
			if (entry.IsValid)
				throw new InvalidOperationException("Replenish must not be run on a valid entry!");

			// Some members are missing. Create a new one.
			var slave = self.World.CreateActor(false, entry.ActorName,
				new TypeDictionary { new OwnerInit(self.Owner) });

			// Initialize slave entry
			InitializeSlaveEntry(slave, entry);
			entry.SpawnerSlave.LinkMaster(entry.Actor, self, this);
		}

		/// <summary>
		/// Slave entry initializer function.
		/// Override this function from derived classes to initialize their own specific stuff.
		/// </summary>
		public virtual void InitializeSlaveEntry(Actor slave, SpawnerSlaveBaseEntry entry)
		{
			entry.Actor = slave;
			entry.SpawnerSlave = slave.Trait<SpawnerSlaveBase>();

			if (IsTraitDisabled)
				entry.SpawnerSlave.GrantMasterDisabledCondition(entry.Actor);

			if (IsTraitPaused)
				entry.SpawnerSlave.GrantMasterPausedCondition(entry.Actor);
		}

		protected SpawnerSlaveBaseEntry SelectEntryToSpawn(SpawnerSlaveBaseEntry[] slaveEntries)
		{
			// If any thing is marked dead or null, that's a candidate.
			var candidates = slaveEntries.Where(m => !m.IsValid);
			if (!candidates.Any())
				return null;

			return candidates.Random(self.World.SharedRandom);
		}

		public virtual void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			self.World.AddFrameEndTask(w =>
			{
				foreach (var slaveEntry in SlaveEntries)
				{
					if (slaveEntry.IsValid)
						slaveEntry.SpawnerSlave.OnMasterOwnerChanged(slaveEntry.Actor, oldOwner, newOwner, Info.SlaveDisposalOnOwnerChange);
				}
			});
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			foreach (var slaveEntry in SlaveEntries)
			{
				if (slaveEntry.IsValid)
				{
					var killer = self.Owner.PlayerActor.IsDead ? slaveEntry.Actor.Owner.PlayerActor : self.Owner.PlayerActor;
					slaveEntry.SpawnerSlave.OnMasterKilled(slaveEntry.Actor, killer, Info.SlaveDisposalOnKill);
				}
			}
		}

		public virtual void SpawnIntoWorld(Actor self, Actor slave, WPos centerPosition)
		{
			var exits = self.Exits().ToList();
			Exit exit;

			if (exits.Count == 0)
			{
				exit = null;
			}
			else
			{
				exit = exits[nextExitIndex % exits.Count];
				nextExitIndex = (nextExitIndex + 1) % exits.Count;
			}

			SetSpawnedFacing(slave, exit);

			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead)
					return;

				var spawnOffset = exit == null ? WVec.Zero : exit.Info.SpawnOffset;
				slave.Trait<IPositionable>().SetCenterPosition(slave, centerPosition + spawnOffset);

				var location = self.World.Map.CellContaining(centerPosition + spawnOffset);

				var mv = slave.Trait<IMove>();
				slave.QueueActivity(mv.ReturnToCell(slave));

				slave.QueueActivity(mv.MoveTo(location, 2));

				w.Add(slave);
			});
		}

		protected void SetSpawnedFacing(Actor spawned, Exit exit)
		{
			var exitFacing = exit != null && exit.Info.Facing != null ? exit.Info.Facing : WAngle.Zero;

			SetSpawnedFacing(spawned, exitFacing.Value);
		}

		protected void SetSpawnedFacing(Actor spawned, WAngle launchFacing)
		{
			WAngle spawnerFacing = facing == null ? WAngle.Zero : facing.Facing;

			var spawnFacing = spawned.TraitOrDefault<IFacing>();
			if (spawnFacing != null)
				spawnFacing.Facing = launchFacing + spawnerFacing;
		}

		public void StopSlaves()
		{
			foreach (var slaveEntry in SlaveEntries)
			{
				if (!slaveEntry.IsValid)
					continue;

				slaveEntry.SpawnerSlave.Stop(slaveEntry.Actor);
			}
		}

		public virtual void OnSlaveKilled(Actor self, Actor slave) { }

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			Killed(self, e);
		}

		protected virtual void Killed(Actor self, AttackInfo e)
		{
			// Notify slaves.
			foreach (var slaveEntry in SlaveEntries)
			{
				if (slaveEntry.IsValid)
					slaveEntry.SpawnerSlave.OnMasterKilled(slaveEntry.Actor, e.Attacker, Info.SlaveDisposalOnKill);
			}
		}

		protected override void TraitEnabled(Actor self)
		{
			foreach (var slaveEntry in SlaveEntries)
			{
				if (slaveEntry.IsValid)
					slaveEntry.SpawnerSlave.RevokeMasterDisabledCondition(slaveEntry.Actor);
			}
		}

		protected override void TraitDisabled(Actor self)
		{
			foreach (var slaveEntry in SlaveEntries)
			{
				if (slaveEntry.IsValid)
					slaveEntry.SpawnerSlave.GrantMasterDisabledCondition(slaveEntry.Actor);
			}
		}

		protected override void TraitResumed(Actor self)
		{
			foreach (var slaveEntry in SlaveEntries)
			{
				if (slaveEntry.IsValid)
					slaveEntry.SpawnerSlave.RevokeMasterPausedCondition(slaveEntry.Actor);
			}
		}

		protected override void TraitPaused(Actor self)
		{
			foreach (var slaveEntry in SlaveEntries)
			{
				if (slaveEntry.IsValid)
					slaveEntry.SpawnerSlave.GrantMasterPausedCondition(slaveEntry.Actor);
			}
		}
	}
}
