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
using OpenRA.GameRules;
using OpenRA.Mods.CA.Orders;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public enum SlaveDeployEffect { None, Release, Detonate, Kill }

	public enum ControlAtCapacityBehaviour { BlockNew, ReleaseOldest, DetonateOldest, KillOldest }

	[Desc("This actor can mind control other actors.")]
	public class MindControllerInfo : PausableConditionalTraitInfo, Requires<ArmamentInfo>, Requires<HealthInfo>
	{
		[Desc("Name of the armaments that grant this condition.")]
		public readonly HashSet<string> ArmamentNames = new HashSet<string>() { "primary" };

		[Desc("Up to how many units can this unit control?",
			"Use 0 or negative numbers for infinite.")]
		public readonly int Capacity = 1;

		[Desc("The behaviour when attempting to control a new target when capacity has been reached.")]
		public readonly ControlAtCapacityBehaviour ControlAtCapacityBehaviour = ControlAtCapacityBehaviour.ReleaseOldest;

		[Desc("Condition to grant to self when controlling actors. Can stack up by the number of enslaved actors. You can use this to forbid firing of the dummy MC weapon.")]
		[GrantedConditionReference]
		public readonly string ControllingCondition;

		[Desc("Condition to grant to self when mind control is in progress (revoked when complete).")]
		[GrantedConditionReference]
		public readonly string ProgressCondition;

		[Desc("Condition to grant to self when at full capacity.")]
		[GrantedConditionReference]
		public readonly string MaxControlledCondition;

		[Desc("The sound played when target is mind controlled.")]
		public readonly string[] ControlSounds = { };

		[Desc("The sound played when slave is released.")]
		public readonly string[] ReleaseSounds = { };

		[Desc("If true, mind control start sound is only played to the controlling player.")]
		public readonly bool ControlSoundControllerOnly = false;

		[Desc("If true, release sound is only played to the controlling player.")]
		public readonly bool ReleaseSoundControllerOnly = false;

		[Desc("The sound played (to the controlling player only) when beginning mind control process.")]
		public readonly string[] InitSounds = { };

		[Desc("If true, mind control start sound is only played to the controlling player.")]
		public readonly bool InitSoundControllerOnly = false;

		[Desc("Ticks attacking taken to mind control something.")]
		public readonly int TicksToControl = 0;

		[Desc("Ticks taken for mind control to wear off after controller loses control.")]
		public readonly int TicksToRevoke = 0;

		[Desc("Ticks taken for mind control to wear off after controller dies. Use -1 to use TicksToRevoke value.")]
		public readonly int TicksToRevokeOnDeath = -1;

		[Desc("If true, undeploy when control is gained of target.")]
		public readonly bool UndeployOnControl = false;

		[Desc("If true and TicksToControl > 0, undeploy if interrupted (e.g. if target dies).")]
		public readonly bool UndeployOnInterrupt = false;

		[Desc("If true, slave will be released when attacking a new target.")]
		public readonly bool ReleaseOnNewTarget = false;

		[Desc("Cursor to use for targeting slaves to deploy.")]
		public readonly string DeploySlaveCursor = "pinkdeploy";

		[Desc("What happens when a slave is deployed while the master is selected (unrelated to when the master is deployed).")]
		public readonly SlaveDeployEffect SlaveDeployEffect = SlaveDeployEffect.None;

		[Desc("Weapon to detonate if SlaveDeployEffect is Detonate.")]
		[WeaponReference]
		public readonly string SlaveDetonateWeapon = null;

		public WeaponInfo SlaveDetonateWeaponInfo { get; private set; }

		[Desc("Types of damage that this trait causes to slave if killed on deploying or when capacity exceeded. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> SlaveKillDamageTypes = default(BitSet<DamageType>);

		[Desc("Percentage of targets cost gained as XP when successfully mind controlled.")]
		public readonly int ExperienceFromControl = 0;

		public override object Create(ActorInitializer init) { return new MindController(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (SlaveDetonateWeapon != null)
			{
				WeaponInfo weaponInfo;

				var slaveDetonateWeaponToLower = (SlaveDetonateWeapon ?? string.Empty).ToLowerInvariant();
				if (!rules.Weapons.TryGetValue(slaveDetonateWeaponToLower, out weaponInfo))
					throw new YamlException($"Weapons Ruleset does not contain an entry '{slaveDetonateWeaponToLower}'");

				SlaveDetonateWeaponInfo = weaponInfo;
			}
		}
	}

	public class MindController : PausableConditionalTrait<MindControllerInfo>, INotifyAttack, INotifyKilled, INotifyActorDisposing, INotifyCreated, IIssueOrder, IResolveOrder, ITick
	{
		readonly List<Actor> slaves = new List<Actor>();
		readonly Stack<int> controllingTokens = new Stack<int>();

		public IEnumerable<Actor> Slaves { get { return slaves; } }

		MindControllerInfo info;
		int capacity;
		IEnumerable<MindControllerCapacityModifier> capacityModifiers;
		bool refreshCapacity;
		int maxControlledToken = Actor.InvalidConditionToken;

		// Only tracked when TicksToControl greater than zero
		Target lastTarget = Target.Invalid;
		Target currentTarget = Target.Invalid;
		int controlTicks;
		int progressToken = Actor.InvalidConditionToken;

		GrantConditionOnDeploy deployTrait;
		HashSet<Actor> slaveHistory;
		GainsExperience gainsExperience;

		public MindController(Actor self, MindControllerInfo info)
			: base(info)
		{
			this.info = info;
			slaveHistory = new HashSet<Actor>();
			gainsExperience = self.TraitOrDefault<GainsExperience>();
			ResetProgress(self);
			capacityModifiers = self.TraitsImplementing<MindControllerCapacityModifier>();
			UpdateCapacity(self);
		}

		protected override void Created(Actor self)
		{
			deployTrait = self.TraitOrDefault<GrantConditionOnDeploy>();
		}

		void StackControllingCondition(Actor self, string condition)
		{
			if (string.IsNullOrEmpty(condition))
				return;

			controllingTokens.Push(self.GrantCondition(condition));
		}

		void UnstackControllingCondition(Actor self, string condition)
		{
			if (string.IsNullOrEmpty(condition))
				return;

			self.RevokeCondition(controllingTokens.Pop());
		}

		public void UnlinkSlave(Actor self, Actor slave)
		{
			if (slaves.Contains(slave))
			{
				slaves.Remove(slave);
				UnstackControllingCondition(self, Info.ControllingCondition);
				MaxControlledCheck(self);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (refreshCapacity)
				UpdateCapacity(self);

			if (Info.TicksToControl == 0)
				return;

			if (currentTarget.Type != TargetType.Actor)
			{
				if (Info.UndeployOnInterrupt && deployTrait != null && deployTrait.DeployState == DeployState.Deployed)
				{
					ResetProgress(self);
					deployTrait.Undeploy();
				}

				return;
			}

			if (controlTicks < Info.TicksToControl)
				controlTicks++;

			GrantProgressCondition(self);

			if (controlTicks == 1 && Info.InitSounds.Length > 0)
			{
				if (Info.InitSoundControllerOnly)
					Game.Sound.PlayToPlayer(SoundType.World, self.Owner, Info.InitSounds.Random(self.World.SharedRandom), self.CenterPosition);
				else
					Game.Sound.Play(SoundType.World, Info.InitSounds.Random(self.World.SharedRandom), self.CenterPosition);
			}

			UpdateProgressBar(self, currentTarget);

			if (controlTicks == Info.TicksToControl)
				AddSlave(self);
		}

		public void GrantProgressCondition(Actor self)
		{
			if (string.IsNullOrEmpty(Info.ProgressCondition))
				return;

			if (progressToken == Actor.InvalidConditionToken)
				progressToken = self.GrantCondition(Info.ProgressCondition);
		}

		public void RevokeProgressCondition(Actor self)
		{
			if (string.IsNullOrEmpty(Info.ProgressCondition))
				return;

			if (progressToken != Actor.InvalidConditionToken)
				progressToken = self.RevokeCondition(progressToken);
		}

		void MaxControlledCheck(Actor self)
		{
			if (capacity == 0)
				return;

			if (slaves.Count() >= capacity)
				GrantMaxControlledCondition(self);
			else
				RevokeMaxControlledCondition(self);
		}

		public void GrantMaxControlledCondition(Actor self)
		{
			if (string.IsNullOrEmpty(Info.MaxControlledCondition))
				return;

			if (maxControlledToken == Actor.InvalidConditionToken)
				maxControlledToken = self.GrantCondition(Info.MaxControlledCondition);
		}

		public void RevokeMaxControlledCondition(Actor self)
		{
			if (string.IsNullOrEmpty(Info.MaxControlledCondition))
				return;

			if (maxControlledToken != Actor.InvalidConditionToken)
				maxControlledToken = self.RevokeCondition(maxControlledToken);
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "ReleaseSlave")
			{
				if (order.Target.Type != TargetType.Actor)
					return;

				order.Target.Actor.Trait<MindControllable>().RevokeMindControl(order.Target.Actor, 0);

				if (Info.SlaveDeployEffect == SlaveDeployEffect.Kill)
				{
					self.World.AddFrameEndTask(w => {
						if (!order.Target.Actor.IsDead)
							order.Target.Actor.Kill(order.Target.Actor, Info.SlaveKillDamageTypes);
					});
				}
				else if (Info.SlaveDeployEffect == SlaveDeployEffect.Detonate)
				{
					self.World.AddFrameEndTask(w => DetonateSlave(self, order.Target.Actor));
				}

				if (Info.ReleaseSounds.Length > 0)
				{
					if (Info.ReleaseSoundControllerOnly)
						Game.Sound.PlayToPlayer(SoundType.World, self.Owner, Info.ReleaseSounds.Random(self.World.SharedRandom), self.CenterPosition);
					else
						Game.Sound.Play(SoundType.World, Info.ReleaseSounds.Random(self.World.SharedRandom), self.CenterPosition);
				}

				return;
			}

			// For all other order, if target has changed, reset progress
			if (order.Target.Actor != currentTarget.Actor)
			{
				ResetProgress(self);
				lastTarget = currentTarget;
				currentTarget = Target.Invalid;
			}
		}

		void ResetProgress(Actor self)
		{
			if (Info.TicksToControl == 0)
				return;

			controlTicks = 0;
			RevokeProgressCondition(self);
			UpdateProgressBar(self, lastTarget);
			UpdateProgressBar(self, currentTarget);
		}

		void UpdateProgressBar(Actor self, Target target)
		{
			if (target.Type != TargetType.Actor)
				return;

			var targetWatchers = target.Actor.TraitsImplementing<IMindControlProgressWatcher>().ToArray();

			foreach (var w in targetWatchers)
				w.Update(target.Actor, self, target.Actor, controlTicks, Info.TicksToControl);
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (!Info.ArmamentNames.Contains(a.Info.Name))
				return;

			if (target.Actor == null || !target.IsValidFor(self))
				return;

			lastTarget = currentTarget;
			currentTarget = target;

			if (TargetChanged() && Info.TicksToControl > 0)
			{
				ResetProgress(self);

				if (Info.ReleaseOnNewTarget)
					ReleaseSlaves(self, Info.TicksToRevoke);

				return;
			}

			AddSlave(self);
		}

		void AddSlave(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (controlTicks < Info.TicksToControl)
				return;

			if (self.Owner.RelationshipWith(currentTarget.Actor.Owner) == PlayerRelationship.Ally)
				return;

			var mindControllable = currentTarget.Actor.TraitOrDefault<MindControllable>();

			if (mindControllable == null)
				throw new InvalidOperationException($"`{self.Info.Name}` tried to mindcontrol `{currentTarget.Actor.Info.Name}`, but the latter does not have the necessary trait!");

			if (mindControllable.IsTraitDisabled || mindControllable.IsTraitPaused)
				return;

			if (capacity > 0 && Info.ControlAtCapacityBehaviour == ControlAtCapacityBehaviour.BlockNew && slaves.Count() >= capacity)
				return;

			if (mindControllable.Master != null)
			{
				ResetProgress(self);
				return;
			}

			slaves.Add(currentTarget.Actor);
			StackControllingCondition(self, Info.ControllingCondition);
			mindControllable.LinkMaster(currentTarget.Actor, self);

			if (capacity > 0 && Info.ControlAtCapacityBehaviour != ControlAtCapacityBehaviour.BlockNew && slaves.Count() > capacity)
			{
				var oldestSlave = slaves[0];
				oldestSlave.Trait<MindControllable>().RevokeMindControl(slaves[0], 0);

				if (Info.ControlAtCapacityBehaviour == ControlAtCapacityBehaviour.KillOldest)
				{
					self.World.AddFrameEndTask(w => {
						if (!oldestSlave.IsDead)
							oldestSlave.Kill(oldestSlave, Info.SlaveKillDamageTypes);
					});
				}
				else if (Info.ControlAtCapacityBehaviour == ControlAtCapacityBehaviour.DetonateOldest)
				{
					self.World.AddFrameEndTask(w => DetonateSlave(self, oldestSlave));
				}
			}

			GiveExperience(currentTarget.Actor);
			ControlComplete(self);
			MaxControlledCheck(self);
		}

		void ControlComplete(Actor self)
		{
			if (Info.ControlSounds.Length > 0)
			{
				if (Info.ControlSoundControllerOnly)
					Game.Sound.PlayToPlayer(SoundType.World, self.Owner, Info.ControlSounds.Random(self.World.SharedRandom), self.CenterPosition);
				else
					Game.Sound.Play(SoundType.World, Info.ControlSounds.Random(self.World.SharedRandom), self.CenterPosition);
			}

			if (Info.UndeployOnControl)
			{
				if (deployTrait != null && deployTrait.DeployState == DeployState.Deployed)
					deployTrait.Undeploy();
			}

			ResetProgress(self);
			currentTarget = Target.Invalid;
		}

		public void ReleaseSlaves(Actor self, int ticks)
		{
			foreach (var s in slaves)
			{
				if (s.IsDead || s.Disposed)
					continue;

				s.Trait<MindControllable>().RevokeMindControl(s, ticks);
			}

			slaves.Clear();
			while (controllingTokens.Count > 0)
				UnstackControllingCondition(self, Info.ControllingCondition);

			RevokeMaxControlledCondition(self);
		}

		public void TransformSlave(Actor self, Actor oldSlave, Actor newSlave)
		{
			if (slaves.Contains(oldSlave))
				slaves[slaves.FindIndex(o => o == oldSlave)] = newSlave;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			ResetProgress(self);
			var ticksToRevoke = Info.TicksToRevokeOnDeath > -1 ? Info.TicksToRevokeOnDeath : Info.TicksToRevoke;
			ReleaseSlaves(self, ticksToRevoke);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			ResetProgress(self);
			var ticksToRevoke = Info.TicksToRevokeOnDeath > -1 ? Info.TicksToRevokeOnDeath : Info.TicksToRevoke;
			ReleaseSlaves(self, ticksToRevoke);
		}

		protected override void TraitDisabled(Actor self)
		{
			ReleaseSlaves(self, Info.TicksToRevoke);
		}

		bool TargetChanged()
		{
			// Invalidate reveal changing the target.
			if (lastTarget.Type == TargetType.FrozenActor && currentTarget.Type == TargetType.Actor)
				if (lastTarget.FrozenActor.Actor == currentTarget.Actor)
					return false;

			if (lastTarget.Type == TargetType.Actor && currentTarget.Type == TargetType.FrozenActor)
				if (currentTarget.FrozenActor.Actor == lastTarget.Actor)
					return false;

			if (lastTarget.Type != currentTarget.Type)
				return true;

			// Invalidate attacking different targets with shared target types.
			if (lastTarget.Type == TargetType.Actor && currentTarget.Type == TargetType.Actor)
				if (lastTarget.Actor != currentTarget.Actor)
					return true;

			if (lastTarget.Type == TargetType.FrozenActor && currentTarget.Type == TargetType.FrozenActor)
				if (lastTarget.FrozenActor != currentTarget.FrozenActor)
					return true;

			if (lastTarget.Type == TargetType.Terrain && currentTarget.Type == TargetType.Terrain)
				if (lastTarget.CenterPosition != currentTarget.CenterPosition)
					return true;

			return false;
		}

		void UpdateCapacity(Actor self)
		{
			refreshCapacity = false;
			var newCapacity = info.Capacity;

			// Modifiers have no effect if the base capacity is unlimited.
			if (info.Capacity <= 0)
				return;

			foreach (var capacityModifier in capacityModifiers)
				newCapacity += capacityModifier.Amount;

			capacity = Math.Max(newCapacity, 1);

			var currentSlaveCount = slaves.Count();
			var numSlavesToRemove = currentSlaveCount - capacity;
			for (var i = numSlavesToRemove; i > 0; i--)
				slaves[i].Trait<MindControllable>().RevokeMindControl(slaves[i], 0);

			MaxControlledCheck(self);
		}

		public void ModifierUpdated()
		{
			refreshCapacity = true;
		}

		void GiveExperience(Actor slave)
		{
			if (gainsExperience == null || info.ExperienceFromControl == 0 || slaveHistory.Contains(slave))
				return;

			var valued = slave.Info.TraitInfoOrDefault<ValuedInfo>();
			if (valued == null)
				return;

			slaveHistory.Add(slave);
			gainsExperience.GiveExperience(Util.ApplyPercentageModifiers(valued.Cost, new int[] { 10000, info.ExperienceFromControl }));
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				yield return new ReleaseSlaveOrderTargeter(
					"ReleaseSlave",
					11,
					Info.DeploySlaveCursor,
					(target, modifiers) => !modifiers.HasModifier(TargetModifiers.ForceMove) && IsDeployableSlave(target));

				yield return new ReleaseSlaveOrderTargeter(
					"ReleaseSlave",
					5,
					Info.DeploySlaveCursor,
					(target, modifiers) => modifiers.HasModifier(TargetModifiers.ForceMove) && IsDeployableSlave(target));
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "ReleaseSlave")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		bool IsDeployableSlave(Actor a)
		{
			if (Info.SlaveDeployEffect == SlaveDeployEffect.None)
				return false;

			return slaves.Contains(a);
		}

		void DetonateSlave(Actor self, Actor slave)
		{
			if (slave.IsDead && !slave.IsInWorld)
				return;

			if (Info.SlaveDetonateWeapon == null)
				return;

			var weapon = Info.SlaveDetonateWeaponInfo;
			var pos = Target.FromPos(slave.CenterPosition);

			var args = new WarheadArgs
			{
				Weapon = weapon,
				DamageModifiers = self.TraitsImplementing<IFirepowerModifier>().Select(a => a.GetFirepowerModifier()).ToArray(),
				Source = self.CenterPosition,
				SourceActor = self,
				WeaponTarget = pos
			};

			weapon.Impact(pos, args);
		}
	}
}
