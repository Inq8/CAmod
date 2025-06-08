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
		[FieldLoader.Require]
		[Desc("Named type of mind control. Determines which progress bar is shown.")]
		public readonly string ControlType = null;

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

		[Desc("Ticks attacking taken to mind control something.")]
		public readonly Dictionary<string, int> TargetTypeTicksToControl = new Dictionary<string, int>();

		[Desc("Ticks taken for mind control to wear off after controller loses control.")]
		public readonly int TicksToRevoke = 0;

		[Desc("Ticks taken for mind control to wear off after controller dies. Use -1 to use TicksToRevoke value.")]
		public readonly int TicksToRevokeOnDeath = -1;

		[Desc("If true, undeploy when control is gained of target, or if interrupted (e.g. if target dies).")]
		public readonly bool AutoUndeploy = false;

		[Desc("If true, slave will be released when attacking a new target.")]
		public readonly bool ReleaseOnNewTarget = false;

		[Desc("Cursor to use for targeting slaves to deploy.")]
		public readonly string DeploySlaveCursor = "mc-deploy";

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

		[Desc("If true, the mind controller transfer slaves to transport on entering. If the transport doesn't have a matching MindController trait, control will not be transferred.")]
		public readonly bool TransferToTransport = true;

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

	public class MindController : PausableConditionalTrait<MindControllerInfo>, INotifyAttack, INotifyKilled, INotifyActorDisposing, INotifyCreated, IIssueOrder, IResolveOrder, ITick, INotifyEnteredCargo, INotifyExitedCargo
	{
		readonly List<TraitPair<MindControllable>> slaves = new List<TraitPair<MindControllable>>();
		readonly Queue<int> controllingTokens = new Queue<int>();

		public List<TraitPair<MindControllable>> Slaves { get { return slaves; } }

		MindControllerInfo info;
		int capacity;
		IEnumerable<MindControllerCapacityModifier> capacityModifiers;
		bool refreshCapacity;
		int maxControlledToken = Actor.InvalidConditionToken;

		// Only tracked when TicksToControl greater than zero
		Target lastTarget = Target.Invalid;
		Target currentTarget = Target.Invalid;
		int lastTargetTicksToControl;
		int currentTargetTicksToControl;
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
			capacityModifiers = self.TraitsImplementing<MindControllerCapacityModifier>();
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			deployTrait = self.TraitOrDefault<GrantConditionOnDeploy>();
			ResetProgress(self);
			UpdateCapacity(self);
		}

		void AddControllingCondition(Actor self)
		{
			var condition = Info.ControllingCondition;
			if (string.IsNullOrEmpty(condition))
				return;

			controllingTokens.Enqueue(self.GrantCondition(condition));
		}

		void SubtractControllingCondition(Actor self)
		{
			var condition = Info.ControllingCondition;
			if (string.IsNullOrEmpty(condition))
				return;

			self.RevokeCondition(controllingTokens.Dequeue());
		}

		public void UnlinkSlave(Actor self, Actor slave)
		{
			if (slaves.Any(s => s.Actor == slave))
			{
				slaves.Remove(slaves.First(s => s.Actor == slave));
				SubtractControllingCondition(self);
				MaxControlledCheck(self);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (refreshCapacity)
				UpdateCapacity(self);

			if (currentTargetTicksToControl == 0)
				return;

			if (currentTarget.Type != TargetType.Actor)
			{
				if (Info.AutoUndeploy && deployTrait != null && deployTrait.DeployState == DeployState.Deployed)
					ResolveOrder(self, new Order("Stop", self, false)); // for some reason Undeploy() doesn't work properly here

				return;
			}

			if (controlTicks < currentTargetTicksToControl)
				controlTicks++;

			GrantProgressCondition(self);

			if (controlTicks == 1 && Info.InitSounds.Length > 0)
			{
				if (Info.InitSoundControllerOnly)
					Game.Sound.PlayToPlayer(SoundType.World, self.Owner, Info.InitSounds.Random(self.World.SharedRandom), self.CenterPosition);
				else
					Game.Sound.Play(SoundType.World, Info.InitSounds.Random(self.World.SharedRandom), self.CenterPosition);
			}

			UpdateProgressBar(self, currentTarget, currentTargetTicksToControl);

			if (controlTicks == currentTargetTicksToControl)
				AddSlave(self, currentTarget.Actor);
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

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "ReleaseSlave")
			{
				if (order.Target.Type != TargetType.Actor)
					return;

				var slave = slaves.SingleOrDefault(s => s.Actor == order.Target.Actor);
				slave.Trait?.RevokeMindControl(order.Target.Actor, 0);

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

			// For all other orders, if target has changed, reset progress
			if (order.Target.Actor != currentTarget.Actor && !order.Queued)
			{
				if (Info.AutoUndeploy && deployTrait != null && deployTrait.DeployState == DeployState.Deployed && currentTarget.Actor != null && order.OrderString != "GrantConditionOnDeploy")
					deployTrait.Undeploy();

				ResetProgress(self);
				ResetTarget();
			}
		}

		void ResetTarget()
		{
			lastTarget = currentTarget;
			lastTargetTicksToControl = currentTargetTicksToControl;
			currentTarget = Target.Invalid;
			currentTargetTicksToControl = 0;
		}

		void ResetProgress(Actor self)
		{
			controlTicks = 0;
			RevokeProgressCondition(self);

			if (lastTargetTicksToControl > 0)
				UpdateProgressBar(self, lastTarget, lastTargetTicksToControl);

			if (currentTargetTicksToControl > 0)
				UpdateProgressBar(self, currentTarget, currentTargetTicksToControl);
		}

		void UpdateProgressBar(Actor self, Target target, int ticksToControl)
		{
			if (target.Type != TargetType.Actor)
				return;

			var targetWatchers = target.Actor.TraitsImplementing<IMindControlProgressWatcher>().ToArray();

			foreach (var w in targetWatchers)
				w.Update(target.Actor, self, target.Actor, controlTicks, ticksToControl, Info.ControlType);
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

			if (TargetChanged(target, lastTarget) && (Info.TicksToControl > 0 || Info.TargetTypeTicksToControl.Any()))
			{
				lastTargetTicksToControl = currentTargetTicksToControl;
				currentTargetTicksToControl = Info.TicksToControl;

				if (Info.TargetTypeTicksToControl.Any())
				{
					var targetTypes = currentTarget.Actor.GetEnabledTargetTypes();
					var matchingTargetTypeTicks = Info.TargetTypeTicksToControl.Where(t => targetTypes.Contains(t.Key));

					if (matchingTargetTypeTicks.Any())
					{
						var maxTicksToControl = matchingTargetTypeTicks.Max(t => t.Value);
						currentTargetTicksToControl = Math.Max(maxTicksToControl, currentTargetTicksToControl);
					}
				}

				ResetProgress(self);

				if (Info.ReleaseOnNewTarget)
					ReleaseSlaves(self, Info.TicksToRevoke);

				if (currentTargetTicksToControl > 0)
					return;
			}

			AddSlave(self, currentTarget.Actor);
		}

		public void AddSlave(Actor self, Actor slaveToAdd, bool transfer = false)
		{
			if (!transfer && (IsTraitDisabled || IsTraitPaused))
				return;

			if (controlTicks < currentTargetTicksToControl)
				return;

			if (!transfer && self.Owner.RelationshipWith(slaveToAdd.Owner) == PlayerRelationship.Ally)
				return;

			var mindControllable = slaveToAdd.TraitsImplementing<MindControllable>().FirstOrDefault(mc => mc.Info.ControlType == info.ControlType);

			if (mindControllable == null)
				throw new InvalidOperationException($"`{self.Info.Name}` tried to mindcontrol `{slaveToAdd.Info.Name}`, but the latter does not have the necessary trait!");

			if (mindControllable.IsTraitDisabled || mindControllable.IsTraitPaused)
				return;

			if (capacity > 0 && Info.ControlAtCapacityBehaviour == ControlAtCapacityBehaviour.BlockNew && slaves.Count() >= capacity)
				return;

			if (!transfer && mindControllable.Master != null)
			{
				ResetProgress(self);
				return;
			}

			var traitPair = new TraitPair<MindControllable>(slaveToAdd, mindControllable);
			slaves.Add(traitPair);
			AddControllingCondition(self);
			mindControllable.LinkMaster(slaveToAdd, self);

			if (capacity > 0 && Info.ControlAtCapacityBehaviour != ControlAtCapacityBehaviour.BlockNew && slaves.Count() > capacity)
			{
				var oldestSlave = slaves[0];
				oldestSlave.Trait.RevokeMindControl(oldestSlave.Actor, 0);

				if (Info.ControlAtCapacityBehaviour == ControlAtCapacityBehaviour.KillOldest)
				{
					self.World.AddFrameEndTask(w => {
						if (!oldestSlave.Actor.IsDead)
							oldestSlave.Actor.Kill(oldestSlave.Actor, Info.SlaveKillDamageTypes);
					});
				}
				else if (Info.ControlAtCapacityBehaviour == ControlAtCapacityBehaviour.DetonateOldest)
				{
					self.World.AddFrameEndTask(w => DetonateSlave(self, oldestSlave.Actor));
				}
			}

			if (!transfer)
				GiveExperience(slaveToAdd);

			ControlComplete(self);
			MaxControlledCheck(self);

			foreach (var notify in self.TraitsImplementing<INotifyMindControlling>())
				notify.MindControlling(self, slaveToAdd);
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

			if (Info.AutoUndeploy && deployTrait != null && deployTrait.DeployState == DeployState.Deployed)
				deployTrait.Undeploy();

			ResetProgress(self);
			ResetTarget();
		}

		public void ReleaseSlaves(Actor self, int ticks)
		{
			foreach (var s in slaves)
			{
				if (s.Actor.IsDead || s.Actor.Disposed)
					continue;

				if (s.Trait.Master.HasValue && s.Trait.Master.Value.Actor != self)
					continue;

				s.Trait.RevokeMindControl(s.Actor, ticks);
			}

			slaves.Clear();
			while (controllingTokens.Count > 0)
				SubtractControllingCondition(self);

			RevokeMaxControlledCondition(self);
		}

		public void TransformSlave(Actor self, Actor oldSlave, Actor newSlave)
		{
			if (slaves.Any(s => s.Actor == oldSlave))
			{
				var traitPair = new TraitPair<MindControllable>(newSlave, newSlave.TraitsImplementing<MindControllable>().First(mc => mc.Info.ControlType == info.ControlType));
				slaves[slaves.FindIndex(o => o.Actor == oldSlave)] = traitPair;
			}
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

		static bool TargetChanged(in Target lastTarget, in Target target)
		{
			// Invalidate reveal changing the target.
			if (lastTarget.Type == TargetType.FrozenActor &&
				target.Type == TargetType.Actor &&
				lastTarget.FrozenActor.Actor == target.Actor)
				return false;

			if (lastTarget.Type == TargetType.Actor &&
				target.Type == TargetType.FrozenActor &&
				target.FrozenActor.Actor == lastTarget.Actor)
				return false;

			if (lastTarget.Type != target.Type)
				return true;

			// Invalidate attacking different targets with shared target types.
			if (lastTarget.Type == TargetType.Actor &&
				target.Type == TargetType.Actor &&
				lastTarget.Actor != target.Actor)
				return true;

			if (lastTarget.Type == TargetType.FrozenActor &&
				target.Type == TargetType.FrozenActor &&
				lastTarget.FrozenActor != target.FrozenActor)
				return true;

			if (lastTarget.Type == TargetType.Terrain &&
				target.Type == TargetType.Terrain &&
				lastTarget.CenterPosition != target.CenterPosition)
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
				slaves[i].Trait.RevokeMindControl(slaves[i].Actor, 0);

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
					101,
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

			return slaves.Any(s => s.Actor == a);
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

		public void TransferSlaves(Actor self, Actor newMaster)
		{
			if (self.IsDead || self.Disposed)
				return;

			foreach (var s in slaves)
			{
				if (s.Actor.IsDead || s.Actor.Disposed)
					continue;

				s.Trait.LinkMaster(s.Actor, newMaster);
			}
		}

		void INotifyEnteredCargo.OnEnteredCargo(Actor self, Actor cargo)
		{
			if (Info.TransferToTransport)
			{
				var transportMc = cargo.TraitsImplementing<MindController>().FirstOrDefault(mc => mc.Info.ControlType == info.ControlType);
				if (transportMc != null)
				{
					foreach (var s in slaves)
					{
						if (s.Actor.IsDead || s.Actor.Disposed)
							continue;

						transportMc.AddSlave(cargo, s.Actor, true);
					}
				}
			}
		}

		void INotifyExitedCargo.OnExitedCargo(Actor self, Actor cargo)
		{
			if (Info.TransferToTransport)
			{
				var transportMc = cargo.TraitsImplementing<MindController>().FirstOrDefault(mc => mc.Info.ControlType == info.ControlType);
				if (transportMc != null)
				{
					foreach (var s in transportMc.Slaves)
					{
						if (s.Actor.IsDead || s.Actor.Disposed)
							continue;

						AddSlave(self, s.Actor, true);
					}
				}
			}
		}
	}
}
