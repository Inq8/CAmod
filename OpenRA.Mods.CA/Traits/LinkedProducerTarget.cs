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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public enum LinkedProducerMode { Clone, Exit }

	[Desc("The target of a linked producer. Any units produced at the targeted source will be either cloned or redirect at the target.")]
	public class LinkedProducerTargetInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("`Clone` means the produced unit is copied. `Exit` means produced units are just moved to the target.")]
		public readonly LinkedProducerMode Mode = default;

		[FieldLoader.Require]
		[Desc("Production types (must share one or more values of the `Produces` property of the source's Production trait).")]
		public readonly string[] Types = Array.Empty<string>();

		[Desc("Matches the `Produces` property of the Production trait (for Clone mode only).")]
		public readonly string Produces = null;

		[Desc("Maximum number of sources that can be linked. Set to 0 for unlimited sources.")]
		public readonly int MaxSources = 0;

		[Desc("List of actors to ignore.")]
		public readonly string[] InvalidActors = Array.Empty<string>();

		[Desc("Actors to use instead of specific source actors.")]
		public readonly Dictionary<string, string> CloneActors = new Dictionary<string, string>();

		[CursorReference]
		[Desc("Cursor to display when selecting a source.")]
		public readonly string Cursor = "chrono-target";

		[NotificationReference("Speech")]
		[Desc("Speech notification to play when setting a new source.")]
		public readonly string SourceSetNotification = null;

		[Desc("Text notification to display when setting a new source.")]
		public readonly string SourceSetTextNotification = null;

		public override object Create(ActorInitializer init) { return new LinkedProducerTarget(init.Self, this); }
	}

	public class LinkedProducerTarget : ConditionalTrait<ConditionalTraitInfo>, IIssueOrder, IResolveOrder, INotifyOwnerChanged, INotifyCreated,
		INotifyKilled, INotifySold, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		const string OrderID = "SetLinkedProducerSource";

		private readonly Actor self;
		private bool singleQueue;
		private readonly HashSet<LinkedProducerSource> linkedSources = new HashSet<LinkedProducerSource>();
		public LinkedProducerTargetInfo info;
		LinkedProducerIndicator effect;

		public List<WPos> LinkNodes;
		private readonly HashSet<LinkedProducerSource> delayedSources = new HashSet<LinkedProducerSource>();

		public string[] Types => info.Types;
		public IEnumerable<LinkedProducerSource> Sources => linkedSources;
		public Actor Actor => self;

		public LinkedProducerTarget(Actor self, LinkedProducerTargetInfo info)
			: base(info)
		{
			this.info = info;
			this.self = self;
			LinkNodes = new List<WPos>();
		}

		void INotifyCreated.Created(Actor self)
		{
			singleQueue = self.World.LobbyInfo.GlobalSettings.OptionOrDefault("queuetype", "") == "global.singlequeue";
			effect = new LinkedProducerIndicator(self, this);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Add(effect));
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Remove(effect));
		}

		public void UnitProduced(Actor unit, LinkedProducerSource source)
		{
			if (IsTraitDisabled || self.IsDead)
				return;

			var actorName = unit.Info.Name.ToLowerInvariant();

			if (info.InvalidActors.Contains(actorName))
				return;

			if (delayedSources.Remove(source))
				return;

			if (info.Mode == LinkedProducerMode.Clone)
			{
				ProduceClone(actorName);
			}
			else
			{
				if (!unit.IsInWorld)
					return;

				var mobile = unit.TraitOrDefault<Mobile>();
				if (mobile == null)
					return;

				unit.CancelActivity();
				unit.Trait<Mobile>().SetPosition(self, self.Location);
				unit.Generation++;

				var rp = self.TraitOrDefault<RallyPoint>();
				var move = unit.TraitOrDefault<IMove>();

				if (move != null && rp != null && rp.Path.Count > 0)
				{
					foreach (var cell in rp.Path)
						unit.QueueActivity(new AttackMoveActivity(unit, () => move.MoveTo(cell, 1, evaluateNearestMovableCell: true, targetLineColor: Color.OrangeRed)));
				}
				else
				{
					unit.QueueActivity(new Nudge(unit));
				}
			}
		}

		void ProduceClone(string actorName)
		{
			var cloneActor = self.World.Map.Rules.Actors[info.CloneActors.ContainsKey(actorName) ? info.CloneActors[actorName] : actorName];

			var sp = self.TraitsImplementing<Production>()
				.FirstOrDefault(p => !p.IsTraitDisabled && !p.IsTraitPaused && p.Info.Produces.Any(p => info.Produces.Contains(p)));

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

		public IEnumerable<IOrderTargeter> Orders
		{
			get {
				yield return new LinkedProducerTargetAddLinkOrderTargeter(info.Cursor);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == OrderID)
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.SourceSetNotification, self.Owner.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(self.Owner, info.SourceSetTextNotification);

				return new Order(order.OrderID, self, target, queued);
			}

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == OrderID)
			{
				var targetActor = order.Target.Actor;
				var targetSource = targetActor.Trait<LinkedProducerSource>();

				if (linkedSources.Contains(targetSource))
					RemoveLink(targetSource, true);
				else
					AddLink(targetActor, targetSource);
			}
		}

		public void AddLink(Actor producer, LinkedProducerSource source)
		{
			if (linkedSources.Contains(source))
				return;

			if (singleQueue)
			{
				var sourceTypes = source.ProductionTypes.Where(t => info.Types.Contains(t));

				var sourcesToLink = self.World.ActorsWithTrait<LinkedProducerSource>()
					.Where(a => !a.Actor.IsDead && a.Actor.IsInWorld && a.Actor.Owner == self.Owner
						&& a.Trait.ProductionTypes.Any(pt => sourceTypes.Contains(pt)))
					.Select(a => a.Trait);

				foreach (var sourceToLink in sourcesToLink)
				{
					if (linkedSources.Add(sourceToLink))
					{
						sourceToLink.SetTarget(this);
						LinkNodes.Add(sourceToLink.Actor.CenterPosition);
					}
				}
			}
			else
			{
				if (info.MaxSources > 0 && linkedSources.Count >= info.MaxSources)
					return;

				linkedSources.Add(source);
				source.SetTarget(this);
				LinkNodes.Add(producer.CenterPosition);
			}

			if (!singleQueue && producer.TraitsImplementing<ProductionQueue>().Any(q => q.CurrentItem() != null))
			{
				delayedSources.Add(source);
			}
		}

		public void RemoveLink(LinkedProducerSource source, bool manualRemoval = false)
		{
			if (!linkedSources.Contains(source))
				return;

			if (singleQueue && manualRemoval)
			{
				// Unlink all sources with shared types
				var sourceTypes = source.ProductionTypes.Where(t => info.Types.Contains(t));

				var sourcesToUnlink = linkedSources
					.Where(s => s.ProductionTypes.Any(pt => sourceTypes.Contains(pt)))
					.ToList();

				foreach (var sourceToUnlink in sourcesToUnlink)
				{
					sourceToUnlink.RemoveTarget(this);
					linkedSources.Remove(sourceToUnlink);
					delayedSources.Remove(sourceToUnlink);
				}
			}
			else
			{
				source.RemoveTarget(this);
				linkedSources.Remove(source);
				delayedSources.Remove(source);
			}

			LinkNodes.Clear();
			foreach (var remainingSource in linkedSources)
				LinkNodes.Add(remainingSource.Actor.CenterPosition);
		}

		private void RemoveAllLinks()
		{
			foreach (var source in linkedSources.ToList())
			{
				source.RemoveTarget(this);
				linkedSources.Remove(source);
			}

			delayedSources.Clear();
			LinkNodes.Clear();
		}

		public void SourceInvalidated(LinkedProducerSource linkedSource)
		{
			RemoveLink(linkedSource);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			RemoveAllLinks();
		}

		void INotifySold.Selling(Actor self)
		{
			RemoveAllLinks();
		}

		void INotifySold.Sold(Actor self) {}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			RemoveAllLinks();
		}

		sealed class LinkedProducerTargetAddLinkOrderTargeter : UnitOrderTargeter
		{
			public const string Id = "SetLinkedProducerSource";

			private string cursor;

			public LinkedProducerTargetAddLinkOrderTargeter(string cursor)
				: base(Id, 6, cursor, false, true)
			{
				this.cursor = cursor;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				cursor = null;

				if (!target.Info.HasTraitInfo<LinkedProducerSourceInfo>())
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
