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
		[Desc("The attachment type (matches that of the `" + nameof(Attachable) + "` trait).")]
		[FieldLoader.Require]
		public readonly string Type = null;

		[Desc("Limit how many specific actors can be attached.")]
		public readonly int Limit = 0;

		[Desc("Conditions to apply when reaching limits.")]
		[GrantedConditionReference]
		public readonly string LimitCondition = null;

		public override object Create(ActorInitializer init) { return new AttachableTo(init, this); }
	}

	public class AttachableTo : INotifyKilled, INotifyOwnerChanged, IResolveOrder, INotifyStanceChanged,
		INotifyExitedCargo, INotifyEnteredCargo, INotifyCreated, INotifyTransform, INotifyRemovedFromWorld, INotifyAddedToWorld,
		INotifyCenterPositionChanged
	{
		public readonly AttachableToInfo Info;
		public Carryable Carryable { get; private set; }
		readonly Actor self;
		readonly HashSet<Attachable> attached = new HashSet<Attachable>();
		readonly HashSet<Attachable> attachedToTransfer = new HashSet<Attachable>();
		int attachedCount = 0;
		int limitToken = Actor.InvalidConditionToken;
		bool reserved;

		public AttachableTo(ActorInitializer init, AttachableToInfo info)
		{
			Info = info;
			self = init.Self;
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

		void INotifyCenterPositionChanged.CenterPositionChanged(Actor self, byte oldLayer, byte newLayer)
		{
			foreach (var attachable in attached)
			{
				if (attachable.IsValid)
					attachable.HostPositionChanged();
			}
		}

		public bool CanAttach(Attachable attachable, bool ignoreReservation = false)
		{
			if (attachable.Info.Type != Info.Type)
				return false;

			if (reserved && !ignoreReservation)
				return false;

			if (!attachable.IsValid)
				return false;

			if (Info.Limit > 0 && attachedCount >= Info.Limit)
				return false;

			return true;
		}

		public bool Attach(Attachable attachable, bool ignoreReservation = false)
		{
			if (!CanAttach(attachable, ignoreReservation))
				return false;

			attached.Add(attachable);
			attachable.AttachTo(this, self.CenterPosition);
			attachedCount++;

			if (Info.LimitCondition != null && limitToken == Actor.InvalidConditionToken && Info.Limit > 0 && attachedCount >= Info.Limit)
				limitToken = self.GrantCondition(Info.LimitCondition);

			reserved = false;
			return true;
		}

		public void Detach(Attachable attachable)
		{
			attached.Remove(attachable);
			attachedCount--;

			if (limitToken != Actor.InvalidConditionToken && (Info.Limit == 0 || attachedCount < Info.Limit))
				limitToken = self.RevokeCondition(limitToken);
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
