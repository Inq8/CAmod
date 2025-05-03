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
	[Desc("A possible source for a LinkedProducerTarget.")]
	public class LinkedProducerSourceInfo : TraitInfo, Requires<ProductionInfo>
	{
		public override object Create(ActorInitializer init) { return new LinkedProducerSource(init.Self, this); }
	}

	public class LinkedProducerSource : INotifyProduction, INotifyOwnerChanged, INotifyKilled, INotifyActorDisposing, IResolveOrder, INotifyCreated
	{
		HashSet<LinkedProducerTarget> linkedProducerTargets = new HashSet<LinkedProducerTarget>();
		public IEnumerable<string> ProductionTypes { get; private set; }
		public Actor Actor { get; private set; }

		public LinkedProducerSource(Actor self, LinkedProducerSourceInfo info)
		{
			Actor = self;
			ProductionTypes =  self.Info.TraitInfos<ProductionInfo>().SelectMany(p => p.Produces);
		}

		public void AddLinkedProducerTarget(LinkedProducerTarget cloningVat)
		{
			linkedProducerTargets.Add(cloningVat);
		}

		public void RemoveLinkedProducerTarget(LinkedProducerTarget cloningVat)
		{
			linkedProducerTargets.Remove(cloningVat);
		}

		void INotifyCreated.Created(Actor self)
		{
			UpdatePrimary(self);
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
			foreach (var cloningVat in linkedProducerTargets)
				cloningVat.UnitProduced(other);
		}

		private void SeverConnections()
		{
			foreach (var cloningVat in linkedProducerTargets.ToList())
			{
				RemoveLinkedProducerTarget(cloningVat);
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
			var linkedProducerTargets = self.World.ActorsWithTrait<LinkedProducerTarget>().Where(a => a.Actor.Owner == self.Owner
				&& ProductionTypes.Where(t => a.Trait.Types.Contains(t)).Any());

			foreach (var p in linkedProducerTargets)
				p.Trait.PrimaryUpdated();
		}
	}
}
