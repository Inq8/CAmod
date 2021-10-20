#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
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

	public class AttachableTo : INotifyKilled, INotifyOwnerChanged, INotifyActorDisposing, IResolveOrder, INotifyStanceChanged, INotifyEnteredCargo, INotifyExitedCargo, INotifyCreated
	{
		public readonly AttachableToInfo Info;
		public Carryable Carryable { get; private set; }
		readonly Actor self;
		readonly HashSet<Attachable> attached = new HashSet<Attachable>();
		Dictionary<string, int> attachedCounts = new Dictionary<string, int>();
		Dictionary<string, int> limitTokens = new Dictionary<string, int>();

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

		void INotifyActorDisposing.Disposing(Actor self)
		{
			Terminate();
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
					attachable.AttachedToLost();
			}
		}

		public bool Attach(Attachable attachable)
		{
			if (!attachable.IsValid)
				return false;

			if (
				attachedCounts.ContainsKey(attachable.Info.AttachableType)
				&& Info.LimitConditions.ContainsKey(attachable.Info.AttachableType)
				&& attachedCounts[attachable.Info.AttachableType] >= Info.Limits[attachable.Info.AttachableType])
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
				attachable.ParentEnteredCargo();
		}

		void INotifyExitedCargo.OnExitedCargo(Actor self, Actor cargo)
		{
			self.World.AddFrameEndTask(w =>
			{
				foreach (var attachable in attached)
					attachable.ParentExitedCargo();
			});
		}
	}
}
