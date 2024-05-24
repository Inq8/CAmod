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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Used to waypoint units after production or repair is finished.")]
	public class CloneProducerInfo : TraitInfo
	{
		[FieldLoader.Require]
		[Desc("Production types (must share one or more values of the `Produces` property of the target's Production trait).")]
		public readonly string[] Types = Array.Empty<string>();

		[Desc("Valid target types, to further limit valid clone sources.")]
		public readonly BitSet<TargetableType> TargetTypes = default;

		[CursorReference]
		[Desc("Cursor to display when selecting a clone source.")]
		public readonly string Cursor = "chrono-target";

		[NotificationReference("Speech")]
		[Desc("Speech notification to play when setting a new clone source.")]
		public readonly string SourceSetNotification = null;

		[Desc("Text notification to display when setting a new clone source.")]
		public readonly string SourceSetTextNotification = null;

		public override object Create(ActorInitializer init) { return new CloneProducer(init.Self, this); }
	}

	public class CloneProducer : IIssueOrder, IResolveOrder, INotifyOwnerChanged, INotifyCreated, INotifyKilled, INotifySold, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		const string OrderID = "SetCloneSource";

		private readonly Actor self;
		private bool singleQueue;
		private CloneSource cloneSource;
		public CloneProducerInfo Info;
		CloneSourceIndicator effect;

		public List<WPos> LinkNodes;

		public CloneProducer(Actor self, CloneProducerInfo info)
		{
			Info = info;
			this.self = self;
			LinkNodes = new List<WPos>();
		}

		void INotifyCreated.Created(Actor self)
		{
			singleQueue = self.World.LobbyInfo.GlobalSettings.OptionOrDefault("queuetype", "") == "global.singlequeue";
			effect = new CloneSourceIndicator(self, this);
			SetSourceToPreferred();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Add(effect));
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Remove(effect));
		}

		public void UnitProduced(Actor unit)
		{
			var sp = self.TraitsImplementing<Production>()
				.FirstOrDefault(p => !p.IsTraitDisabled && !p.IsTraitPaused && p.Info.Produces.Where(p => Info.Types.Contains(p)).Any());

			if (sp != null)
			{
				var inits = new TypeDictionary
				{
					new OwnerInit(self.Owner),
					new FactionInit(sp.Faction)
				};

				sp.Produce(self, self.World.Map.Rules.Actors[unit.Info.Name.ToLowerInvariant()], sp.Info.Produces.First(), inits, 0);
			}
		}

		public void PrimaryUpdated()
		{
			if (singleQueue)
				SetSourceToPreferred();
		}

		private void SetSourceToPreferred()
		{
			var producer = self.World.ActorsWithTrait<CloneSource>()
				.Where(a => !a.Actor.IsDead && a.Actor.Owner == self.Owner && a.Trait.ProductionTypes.Where(t => Info.Types.Contains(t)).Any())
				.OrderByDescending(p => p.Actor.TraitOrDefault<PrimaryBuilding>()?.IsPrimary)
				.ThenByDescending(p => p.Actor.ActorID)
				.FirstOrDefault();

			if (producer.Actor != null)
			{
				cloneSource = producer.Trait;
				cloneSource.AddCloneProducer(this);
				LinkNodes.Clear();
				LinkNodes.Add(producer.Actor.CenterPosition);
			}
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get {
				if (singleQueue)
					yield break;

				yield return new CloneProducerSetSourceOrderTargeter(Info.Cursor);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (singleQueue)
				return null;

			if (order.OrderID == OrderID)
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.SourceSetNotification, self.Owner.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(Info.SourceSetTextNotification, self.Owner);

				return new Order(order.OrderID, self, target, queued);
			}

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (singleQueue)
				return;

			if (order.OrderString == "Stop")
			{
				UnlinkSource();
				cloneSource = null;
			}
			else if (order.OrderString == OrderID)
			{
				UnlinkSource();
				cloneSource = null;
				cloneSource = order.Target.Actor.Trait<CloneSource>();
				cloneSource.AddCloneProducer(this);
				LinkNodes.Clear();
				LinkNodes.Add(order.Target.Actor.CenterPosition);
			}
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			UnlinkSource();
		}

		void INotifySold.Selling(Actor self)
		{
			UnlinkSource();
		}

		void INotifySold.Sold(Actor self) {}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			UnlinkSource();
		}

		public void SourceInvalidated(CloneSource cloneSource)
		{
			if (this.cloneSource == cloneSource)
				UnlinkSource();

			SetSourceToPreferred();
		}

		private void UnlinkSource()
		{
			if (cloneSource != null)
				cloneSource.RemoveCloneProducer(this);
		}

		sealed class CloneProducerSetSourceOrderTargeter : UnitOrderTargeter
		{
			public const string Id = "SetCloneSource";

			private string cursor;

			public CloneProducerSetSourceOrderTargeter(string cursor)
				: base(Id, 6, cursor, false, true)
			{
				this.cursor = cursor;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				cursor = null;

				if (!target.Info.HasTraitInfo<CloneSourceInfo>())
					return false;

				if (self.Owner != target.Owner)
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
