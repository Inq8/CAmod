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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Allows actor to have actors with Attachable trait attached to it.")]
	public class AttachableToInfo : TraitInfo
	{
		[Desc("Limit how many specific actors can be attached.")]
		public readonly Dictionary<string, int> Limits = new Dictionary<string, int>();

		[ActorReference(dictionaryReference: LintDictionaryReference.Keys)]
		[Desc("Conditions to apply when reaching limits.")]
		public readonly Dictionary<string, string> LimitConditions = new Dictionary<string, string>();

		[GrantedConditionReference]
		public IEnumerable<string> LinterLimitConditions { get { return LimitConditions.Values; } }

		public override object Create(ActorInitializer init) { return new AttachableTo(init, this); }
	}

	public class AttachableTo : INotifyKilled, INotifyOwnerChanged, IResolveOrder, INotifyStanceChanged,
		INotifyExitedCargo, INotifyEnteredCargo, INotifyCreated, INotifyTransform, INotifyRemovedFromWorld, INotifyAddedToWorld
	{
		public readonly AttachableToInfo Info;
		public Carryable Carryable { get; private set; }
		readonly Actor self;
		readonly HashSet<Attachable> attached = new HashSet<Attachable>();
		readonly HashSet<Attachable> attachedToTransfer = new HashSet<Attachable>();
		Dictionary<string, int> attachedCounts = new Dictionary<string, int>();
		Dictionary<string, int> limitTokens = new Dictionary<string, int>();
		bool reserved;

		public AttachableTo(ActorInitializer init, AttachableToInfo info)
		{
			Info = info;
			self = init.Self;

			foreach (var type in Info.Limits)
			{
				attachedCounts[type.Key] = 0;
				limitTokens[type.Key] = Actor.InvalidConditionToken;
			}
		}

		public WPos CenterPosition { get { return self.CenterPosition; } }
		public bool IsInWorld { get { return self.IsInWorld; } }

		public bool Reserve()
		{
			if (reserved)
				return false;

			reserved = true;
			return reserved;
		}

		void INotifyCreated.Created(Actor self)
		{
			Carryable = self.TraitOrDefault<Carryable>();
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (attached.Count == 0)
				return;

			var attackOrders = new HashSet<string> { "Attack", "ForceAttack" };

			if (attackOrders.Contains(order.OrderString))
			{
				foreach (var attachable in attached)
				{
					if (attachable.IsValid)
						attachable.Attack(order.Target, order.OrderString == "ForceAttack");
				}
			}

			var otherOrders = new HashSet<string> { "Stop" };

			if (!otherOrders.Contains(order.OrderString))
				return;

			foreach (var attachable in attached)
			{
				if (attachable.IsValid)
					attachable.Stop();
			}
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			Terminate();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			Terminate();
		}

		void Terminate()
		{
			foreach (var attachable in attached)
			{
				if (attachable.IsValid)
					attachable.HostLost();
			}
		}

		void INotifyTransform.BeforeTransform(Actor self) {}
		void INotifyTransform.OnTransform(Actor self)
		{
			foreach (var attachable in attached)
			{
				if (attachable.IsValid)
					attachedToTransfer.Add(attachable);
			}
		}
		void INotifyTransform.AfterTransform(Actor toActor)
		{
			foreach (var attachable in attachedToTransfer)
			{
				if (attachable.IsValid)
					attachable.HostTransformed(toActor);
			}
		}

		public bool CanAttach(Attachable attachable, bool ignoreReservation = false)
		{
			if (reserved && !ignoreReservation)
				return false;

			if (!attachable.IsValid)
				return false;

			if (
				attachedCounts.ContainsKey(attachable.Info.AttachableType)
				&& Info.LimitConditions.ContainsKey(attachable.Info.AttachableType)
				&& attachedCounts[attachable.Info.AttachableType] >= Info.Limits[attachable.Info.AttachableType])
				return false;

			return true;
		}

		public bool Attach(Attachable attachable, bool ignoreReservation = false)
		{
			if (!CanAttach(attachable, ignoreReservation))
				return false;

			attached.Add(attachable);
			attachable.AttachTo(this, self.CenterPosition);

			if (attachedCounts.ContainsKey(attachable.Info.AttachableType))
			{
				attachedCounts[attachable.Info.AttachableType]++;

				if (
					Info.LimitConditions.ContainsKey(attachable.Info.AttachableType)
					&& attachedCounts[attachable.Info.AttachableType] >= Info.Limits[attachable.Info.AttachableType]
					&& limitTokens[attachable.Info.AttachableType] == Actor.InvalidConditionToken)
					limitTokens[attachable.Info.AttachableType] = self.GrantCondition(Info.LimitConditions[attachable.Info.AttachableType]);
			}

			reserved = false;
			return true;
		}

		public void Detach(Attachable attachable)
		{
			attached.Remove(attachable);

			if (attachedCounts.ContainsKey(attachable.Info.AttachableType))
			{
				attachedCounts[attachable.Info.AttachableType]--;

				if (
					Info.LimitConditions.ContainsKey(attachable.Info.AttachableType)
					&& attachedCounts[attachable.Info.AttachableType] < Info.Limits[attachable.Info.AttachableType]
					&& limitTokens[attachable.Info.AttachableType] != Actor.InvalidConditionToken)
					limitTokens[attachable.Info.AttachableType] = self.RevokeCondition(limitTokens[attachable.Info.AttachableType]);
			}
		}

		void INotifyStanceChanged.StanceChanged(Actor self, AutoTarget autoTarget, UnitStance oldStance, UnitStance newStance)
		{
			foreach (var attachable in attached)
			{
				if (attachable.IsValid)
					attachable.SetStance(newStance);
			}
		}

		void INotifyEnteredCargo.OnEnteredCargo(Actor self, Actor cargo)
		{
			foreach (var attachable in attached)
				attachable.HostEnteredCargo();
		}

		void INotifyExitedCargo.OnExitedCargo(Actor self, Actor cargo)
		{
			foreach (var attachable in attached)
				attachable.HostExitedCargo();
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			var carryable = self.TraitOrDefault<Carryable>();
			if (carryable != null && carryable.Carrier != null)
				foreach (var attachable in attached)
					attachable.HostEnteredCargo();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			var carryable = self.TraitOrDefault<Carryable>();
			if (carryable != null && carryable.Carrier != null)
				foreach (var attachable in attached)
					attachable.HostExitedCargo();
		}
	}
}
