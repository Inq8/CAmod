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
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("A possible source for a LinkedProducerTarget.")]
	public class LinkedProducerSourceInfo : TraitInfo, Requires<ProductionInfo>
	{
		[CursorReference]
		[Desc("Cursor to display when selecting a target.")]
		public readonly string Cursor = "chrono-target";

		[NotificationReference("Speech")]
		[Desc("Speech notification to play when setting a new target.")]
		public readonly string TargetSetNotification = null;

		[Desc("Text notification to display when setting a new target.")]
		public readonly string TargetSetTextNotification = null;

		public override object Create(ActorInitializer init) { return new LinkedProducerSource(init.Self, this); }
	}

	public class LinkedProducerSource : INotifyProduction, INotifyOwnerChanged, INotifyKilled, INotifyActorDisposing, INotifyCreated, IIssueOrder, IResolveOrder, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		const string OrderID = "SetLinkedProducerTarget";

		LinkedProducerTarget linkedTarget = null;
		public IEnumerable<string> ProductionTypes { get; }
		public Actor Actor { get; }
		public bool HasTarget => linkedTarget != null;
		public LinkedProducerTarget Target => linkedTarget;
		readonly LinkedProducerSourceInfo info;
		LinkedProducerIndicator effect;

		public LinkedProducerSource(Actor self, LinkedProducerSourceInfo info)
		{
			Actor = self;
			this.info = info;
			ProductionTypes =  self.Info.TraitInfos<ProductionInfo>().SelectMany(p => p.Produces);
		}

		public void SetTarget(LinkedProducerTarget target)
		{
			if (linkedTarget != null)
			{
				linkedTarget.RemoveLink(this);
			}

			linkedTarget = target;
		}

		public void RemoveTarget(LinkedProducerTarget target)
		{
			if (linkedTarget == target)
				linkedTarget = null;
		}

		void INotifyCreated.Created(Actor self)
		{
			effect = new LinkedProducerIndicator(self, this);

			var singleQueue = self.World.LobbyInfo.GlobalSettings.OptionOrDefault("queuetype", "") == "global.singlequeue";
			if (singleQueue)
			{
				self.World.AddFrameEndTask(w =>
				{
					SingleQueueAutoLink(self);
				});
			}
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Add(effect));
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Remove(effect));
		}

		private void SingleQueueAutoLink(Actor self)
		{
			var existingSources = self.World.ActorsWithTrait<LinkedProducerSource>()
				.Where(a => !a.Actor.IsDead && a.Actor.IsInWorld && a.Actor.Owner == self.Owner
					&& a.Actor != self && a.Trait.HasTarget
					&& a.Trait.ProductionTypes.Any(pt => ProductionTypes.Contains(pt)));

			var existingSource = existingSources.FirstOrDefault();
			if (existingSource.Actor != null)
			{
				var target = existingSource.Trait.Target;
				if (target != null)
				{
					target.AddLink(self, this);
				}
			}
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			SeverConnections();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (!self.IsDead)
				SeverConnections();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			SeverConnections();
		}

		void INotifyProduction.UnitProduced(Actor self, Actor other, CPos exit)
		{
			if (linkedTarget != null)
				linkedTarget.UnitProduced(other, this);
		}

		private void SeverConnections()
		{
			if (linkedTarget != null)
			{
				var target = linkedTarget;
				linkedTarget = null;
				target.SourceInvalidated(this);
			}
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get {
				yield return new LinkedProducerSourceAddLinkOrderTargeter(info.Cursor);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == OrderID)
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.TargetSetNotification, self.Owner.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(self.Owner, info.TargetSetTextNotification);

				return new Order(order.OrderID, self, target, queued);
			}

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == OrderID)
			{
				var targetActor = order.Target.Actor;
				var targetTrait = targetActor.Trait<LinkedProducerTarget>();

				if (linkedTarget == targetTrait)
					targetTrait.RemoveLink(this, true);
				else
					targetTrait.AddLink(self, this);
			}
		}

		sealed class LinkedProducerSourceAddLinkOrderTargeter : UnitOrderTargeter
		{
			public const string Id = "SetLinkedProducerTarget";

			private string cursor;

			public LinkedProducerSourceAddLinkOrderTargeter(string cursor)
				: base(Id, 6, cursor, false, true)
			{
				this.cursor = cursor;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				cursor = null;

				if (!target.Info.HasTraitInfo<LinkedProducerTargetInfo>())
					return false;

				if (self.Owner != target.Owner)
					return false;

				var sourceTypes = self.Trait<LinkedProducerSource>().ProductionTypes;
				var targetTypes = target.Trait<LinkedProducerTarget>().Types;

				if (!sourceTypes.Any(st => targetTypes.Contains(st)))
					return false;

				cursor = this.cursor;
				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				return false;
			}
		}
	}
}
