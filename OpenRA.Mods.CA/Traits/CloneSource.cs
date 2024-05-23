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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("A possible source for a CloneProducer.")]
	public class CloneSourceInfo : TraitInfo, Requires<ProductionInfo>
	{
		public override object Create(ActorInitializer init) { return new CloneSource(init.Self, this); }
	}

	public class CloneSource : INotifyProduction, INotifyOwnerChanged, INotifyKilled, INotifySold, IResolveOrder, INotifyCreated
	{
		HashSet<CloneProducer> cloneProducers = new HashSet<CloneProducer>();
		public IEnumerable<string> ProductionTypes { get; private set; }

		public CloneSource(Actor self, CloneSourceInfo info)
		{
			ProductionTypes =  self.Info.TraitInfos<ProductionInfo>().SelectMany(p => p.Produces);
		}

		public void AddCloneProducer(CloneProducer cloningVat)
		{
			cloneProducers.Add(cloningVat);
		}

		public void RemoveCloneProducer(CloneProducer cloningVat)
		{
			cloneProducers.Remove(cloningVat);
		}

		void INotifyCreated.Created(Actor self)
		{
			UpdatePrimary(self);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			SeverConnections();
		}

		void INotifySold.Selling(Actor self)
		{
			SeverConnections();
		}

		void INotifySold.Sold(Actor self) {}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			SeverConnections();
		}

		void INotifyProduction.UnitProduced(Actor self, Actor other, CPos exit)
		{
			foreach (var cloningVat in cloneProducers)
				cloningVat.UnitProduced(other);
		}

		private void SeverConnections()
		{
			foreach (var cloningVat in cloneProducers)
			{
				RemoveCloneProducer(cloningVat);
				cloningVat.SourceInvalidated(this);
			}
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PrimaryProducer")
			{
				UpdatePrimary(self);
			}
		}

		private void UpdatePrimary(Actor self)
		{
			var cloneProducers = self.World.ActorsWithTrait<CloneProducer>().Where(a => a.Actor.Owner == self.Owner
				&& ProductionTypes.Where(t => a.Trait.Info.Types.Contains(t)).Any());

			foreach (var p in cloneProducers)
				p.Trait.PrimaryUpdated();
		}
	}
}
