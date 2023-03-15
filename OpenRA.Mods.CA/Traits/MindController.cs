#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("This actor can mind control other actors.")]
	public class MindControllerInfo : PausableConditionalTraitInfo, Requires<ArmamentInfo>, Requires<HealthInfo>
	{
		[Desc("Name of the armaments that grant this condition.")]
		public readonly HashSet<string> ArmamentNames = new HashSet<string>() { "primary" };

		[Desc("Up to how many units can this unit control?",
			"Use 0 or negative numbers for infinite.")]
		public readonly int Capacity = 1;

		[Desc("If the capacity is reached, discard the oldest mind controlled unit and control the new one",
			"If false, controlling new units is forbidden after capacity is reached.")]
		public readonly bool DiscardOldest = true;

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

		[Desc("If true, slave will be released when attacking a new target.")]
		public readonly bool ReleaseOnNewTarget = false;

		[Desc("Cursor to use for targeting slaves to release.")]
		public readonly string ReleaseSlaveCursor = "pinkdeploy";

		[Desc("If true, slaves can be targeted to release them.")]
		public readonly bool ManualReleaseEnabled = false;

		public override object Create(ActorInitializer init) { return new MindController(init.Self, this); }
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

		public MindController(Actor self, MindControllerInfo info)
			: base(info)
		{
			this.info = info;
			ResetProgress(self);
			capacityModifiers = self.TraitsImplementing<MindControllerCapacityModifier>();
			UpdateCapacity(self);
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
				return;

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
				order.Target.Actor.Trait<MindControllable>().RevokeMindControl(order.Target.Actor, 0);

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
			{
				throw new InvalidOperationException(
					"`{0}` tried to mindcontrol `{1}`, but the latter does not have the necessary trait!"
					.F(self.Info.Name, currentTarget.Actor.Info.Name));
			}

			if (mindControllable.IsTraitDisabled || mindControllable.IsTraitPaused)
				return;

			if (capacity > 0 && !Info.DiscardOldest && slaves.Count() >= capacity)
				return;

			if (mindControllable.Master != null)
			{
				ResetProgress(self);
				return;
			}

			slaves.Add(currentTarget.Actor);
			StackControllingCondition(self, Info.ControllingCondition);
			mindControllable.LinkMaster(currentTarget.Actor, self);

			if (capacity > 0 && Info.DiscardOldest && slaves.Count() > capacity)
				slaves[0].Trait<MindControllable>().RevokeMindControl(slaves[0], 0);

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
				var deployTrait = self.TraitOrDefault<GrantConditionOnDeploy>();
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
			if (info.Capacity == 0)
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

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				yield return new ReleaseSlaveOrderTargeter(
					"ReleaseSlave",
					5,
					Info.ReleaseSlaveCursor,
					IsManuallyReleasableSlave);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "ReleaseSlave")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		bool IsManuallyReleasableSlave(Actor a)
		{
			if (!Info.ManualReleaseEnabled)
				return false;

			return slaves.Contains(a);
		}
	}
}
