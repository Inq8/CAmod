﻿#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Can be slaved to a SpawnerMaster.")]
	public abstract class SpawnerSlaveBaseInfo : TraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant to slaves when the master actor is killed.")]
		public readonly string MasterDeadCondition = null;

		[Desc("Can these actors be mind controlled or captured?")]
		public readonly bool AllowOwnerChange = false;

		[Desc("Types of damage this actor explodes with due to an unallowed slave action. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		[GrantedConditionReference]
		[Desc("The condition to grant when the master trait is disabled.")]
		public readonly string GrantConditionWhenMasterIsDisabled = null;

		[GrantedConditionReference]
		[Desc("The condition to grant when the master trait is paused.")]
		public readonly string GrantConditionWhenMasterIsPaused = null;

		public abstract override object Create(ActorInitializer init);
	}

	public abstract class SpawnerSlaveBase : INotifyCreated, INotifyKilled, INotifyOwnerChanged
	{
		protected AttackBase[] attackBases;

		readonly SpawnerSlaveBaseInfo info;

		public bool HasFreeWill = false;

		SpawnerMasterBase spawnerMaster = null;

		public Actor Master { get; private set; }

		// Make this actor attack a target.
		Target lastTarget;

		int masterTraitDisabledConditionToken = Actor.InvalidConditionToken;
		int masterTraitPausedConditionToken = Actor.InvalidConditionToken;

		public SpawnerSlaveBase(ActorInitializer init, SpawnerSlaveBaseInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			Created(self);
		}

		protected virtual void Created(Actor self)
		{
			attackBases = self.TraitsImplementing<AttackBase>().ToArray();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (Master == null || Master.IsDead)
				return;

			spawnerMaster.OnSlaveKilled(Master, self);
		}

		public virtual void LinkMaster(Actor self, Actor master, SpawnerMasterBase spawnerMaster)
		{
			Master = master;
			this.spawnerMaster = spawnerMaster;
		}

		bool TargetSwitched(Target lastTarget, Target newTarget)
		{
			if (newTarget.Type != lastTarget.Type)
				return true;

			if (newTarget.Type == TargetType.Terrain)
				return newTarget.CenterPosition != lastTarget.CenterPosition;

			if (newTarget.Type == TargetType.Actor)
				return lastTarget.Actor != newTarget.Actor;

			return false;
		}

		// Stop what self was doing.
		public virtual void Stop(Actor self)
		{
			// Drop the target so that Attack() feels the need to assign target for this slave.
			lastTarget = Target.Invalid;

			self.CancelActivity();
		}

		public virtual void Attack(Actor self, Target target)
		{
			// Don't have to change target or alter current activity.
			if (!TargetSwitched(lastTarget, target))
				return;

			if (!target.IsValidFor(self))
			{
				Stop(self);
				return;
			}

			lastTarget = target;

			foreach (var ab in attackBases)
			{
				if (ab.IsTraitDisabled)
					continue;

				ab.AttackTarget(target, AttackSource.Default, false, true, true);
			}
		}

		public virtual void OnMasterKilled(Actor self, Actor attacker, SpawnerSlaveDisposal disposal)
		{
			if (self.IsDead || self.WillDispose)
				return;

			// Grant MasterDead condition.
			self.GrantCondition(info.MasterDeadCondition);

			switch (disposal)
			{
				case SpawnerSlaveDisposal.KillSlaves:
					self.Kill(attacker, info.DamageTypes);
					break;
				case SpawnerSlaveDisposal.GiveSlavesToAttacker:
					self.CancelActivity();
					self.ChangeOwner(attacker.Owner);
					break;
				case SpawnerSlaveDisposal.DoNothing:
				// fall through
				default:
					break;
			}
		}

		// What if the master gets mind controlled?
		public virtual void OnMasterOwnerChanged(Actor self, Player oldOwner, Player newOwner, SpawnerSlaveDisposal disposal)
		{
			switch (disposal)
			{
				case SpawnerSlaveDisposal.KillSlaves:
					self.Kill(self, info.DamageTypes);
					break;
				case SpawnerSlaveDisposal.GiveSlavesToAttacker:
					self.CancelActivity();
					self.ChangeOwner(newOwner);
					break;
				case SpawnerSlaveDisposal.DoNothing:
				// fall through
				default:
					break;
			}
		}

		// What if the slave gets mind controlled?
		// Slaves aren't good without master so, kill it.
		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			// In this case, the slave will be disposed, one way or other.
			if (Master == null || !Master.IsDead)
				return;

			// This function got triggered because the master got mind controlled and
			// thus triggered slave.ChangeOwner().
			// In this case, do nothing.
			if (Master.Owner == newOwner)
				return;

			// These are independent, so why not let it be controlled?
			if (info.AllowOwnerChange)
				return;

			self.Kill(self, info.DamageTypes);
		}

		public void GrantMasterPausedCondition(Actor self)
		{
			if (masterTraitPausedConditionToken == Actor.InvalidConditionToken)
				masterTraitPausedConditionToken = self.GrantCondition(info.GrantConditionWhenMasterIsPaused);
		}

		public void RevokeMasterPausedCondition(Actor self)
		{
			if (masterTraitPausedConditionToken != Actor.InvalidConditionToken)
				masterTraitPausedConditionToken = self.RevokeCondition(masterTraitPausedConditionToken);
		}

		public void GrantMasterDisabledCondition(Actor self)
		{
			if (masterTraitDisabledConditionToken == Actor.InvalidConditionToken)
				masterTraitDisabledConditionToken = self.GrantCondition(info.GrantConditionWhenMasterIsDisabled);
		}

		public void RevokeMasterDisabledCondition(Actor self)
		{
			if (masterTraitDisabledConditionToken != Actor.InvalidConditionToken)
				masterTraitDisabledConditionToken = self.RevokeCondition(masterTraitDisabledConditionToken);
		}
	}
}
