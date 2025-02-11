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
	public class CloneProducerInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Production types (must share one or more values of the `Produces` property of the clone source's Production trait).")]
		public readonly string[] Types = Array.Empty<string>();

		[FieldLoader.Require]
		[Desc("Clone type. Matches the `Produces` property of the clone producer's Production trait.")]
		public readonly string CloneType = null;

		[Desc("Valid target types, to further limit valid clone sources.")]
		public readonly BitSet<TargetableType> TargetTypes = default;

		[Desc("List of actors that cannot be cloned.")]
		public readonly string[] InvalidActors = Array.Empty<string>();

		[Desc("Actors to use instead of specific source actors.")]
		public readonly Dictionary<string, string> CloneActors = new Dictionary<string, string>();

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

	public class CloneProducer : ConditionalTrait<ConditionalTraitInfo>, IIssueOrder, IResolveOrder, INotifyOwnerChanged, INotifyCreated, INotifyKilled, INotifySold, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		const string OrderID = "SetCloneSource";

		private readonly Actor self;
		private bool singleQueue;
		private CloneSource cloneSource;
		public CloneProducerInfo info;
		CloneSourceIndicator effect;

		public List<WPos> LinkNodes;
		bool delayUntilNext;

		public string[] Types => info.Types;

		public CloneProducer(Actor self, CloneProducerInfo info)
			: base(info)
		{
			this.info = info;
			this.self = self;
			LinkNodes = new List<WPos>();
			delayUntilNext = false;
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
			if (IsTraitDisabled || self.IsDead)
				return;

			var actorName = unit.Info.Name.ToLowerInvariant();

			if (info.InvalidActors.Contains(actorName))
				return;

			if (delayUntilNext)
			{
				delayUntilNext = false;
				return;
			}

			var cloneActor = self.World.Map.Rules.Actors[info.CloneActors.ContainsKey(actorName) ? info.CloneActors[actorName] : actorName];

			var sp = self.TraitsImplementing<Production>()
				.FirstOrDefault(p => !p.IsTraitDisabled && !p.IsTraitPaused && p.Info.Produces.Where(p => info.CloneType.Contains(p)).Any());

			if (sp != null)
			{
				var inits = new TypeDictionary
				{
					new OwnerInit(self.Owner),
					new FactionInit(sp.Faction)
				};

				sp.Produce(self, cloneActor, sp.Info.Produces.First(), inits, 0);
			}
		}

		public void PrimaryUpdated()
		{
			if (singleQueue)
				SetSourceToPreferred();
		}

		private void SetSourceToPreferred()
		{
			self.World.AddFrameEndTask(w => {
				var producer = self.World.ActorsWithTrait<CloneSource>()
					.Where(a => !a.Actor.IsDead && a.Actor.IsInWorld && a.Actor.Owner == self.Owner && a.Trait.ProductionTypes.Where(t => info.Types.Contains(t)).Any())
					.OrderByDescending(p => p.Actor.TraitOrDefault<PrimaryBuilding>()?.IsPrimary)
					.ThenByDescending(p => p.Actor.ActorID)
					.FirstOrDefault();

				LinkNodes.Clear();

				if (producer.Actor != null)
				{
					SetSource(producer.Actor, producer.Trait);
				}
			});
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get {
				if (singleQueue)
					yield break;

				yield return new CloneProducerSetSourceOrderTargeter(info.Cursor);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (singleQueue)
				return null;

			if (order.OrderID == OrderID)
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.SourceSetNotification, self.Owner.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(info.SourceSetTextNotification, self.Owner);

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
			}
			else if (order.OrderString == OrderID)
			{
				UnlinkSource();
				SetSource(order.Target.Actor, order.Target.Actor.Trait<CloneSource>());
			}
		}

		void SetSource(Actor producer, CloneSource source)
		{
			cloneSource = source;
			cloneSource.AddCloneProducer(this);
			LinkNodes.Clear();
			LinkNodes.Add(producer.CenterPosition);

			if (!singleQueue && producer.TraitsImplementing<ProductionQueue>().Any(q => q.CurrentItem() != null))
				delayUntilNext = true;
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

			cloneSource = null;
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
