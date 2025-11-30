#region Copyright & License Information
/*
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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Produces actors when enough reclaimable value is accumulated for the specified type.")]
	public class ReclaimableValueProducerInfo : TraitInfo
	{
		[FieldLoader.Require]
		[Desc("The type of reclaimable value pool to monitor.")]
		public readonly string Type = null;

		[FieldLoader.Require]
		[Desc("Actor types that can produce the unit (searched in order).")]
		public readonly string[] ProducerActors = null;

		[FieldLoader.Require]
		[ActorReference]
		[Desc("Actor to produce when enough value is accumulated.")]
		public readonly string ActorToProduce = null;

		[Desc("Required value to produce the actor. If not set, uses the Valued trait Cost of ActorToProduce.")]
		public readonly int RequiredValue = -1;

		[Desc("Production type to use")]
		public readonly HashSet<string> ProductionTypes = new HashSet<string>();

		public override object Create(ActorInitializer init) { return new ReclaimableValueProducer(init, this); }
	}

	public class ReclaimableValueProducer : INotifyCreated
	{
		public readonly ReclaimableValueProducerInfo Info;
		Actor self;
		int requiredValue;
		int currentValue;

		public ReclaimableValueProducer(ActorInitializer init, ReclaimableValueProducerInfo info)
		{
			Info = info;
			currentValue = 0;
		}

		void INotifyCreated.Created(Actor self)
		{
			this.self = self;

			if (Info.RequiredValue < 0)
			{
				var actorInfo = self.World.Map.Rules.Actors[Info.ActorToProduce];
				var valued = actorInfo.TraitInfoOrDefault<ValuedInfo>();
				if (valued == null)
					throw new InvalidOperationException($"ReclaimableValueProducer for type '{Info.Type}' requires " +
						$"ActorToProduce '{Info.ActorToProduce}' to have a Valued trait, or RequiredValue must be explicitly set.");

				requiredValue = valued.Cost;
			}
			else
				requiredValue = Info.RequiredValue;
		}

		public void AddValue(int amount)
		{
			currentValue += amount;

			if (currentValue >= requiredValue)
				ProduceActor();
		}

		TraitPair<Production>? FindAvailableProducer()
		{
			var producer = self.World.ActorsWithTrait<Production>()
				.FirstOrDefault(p => !p.Actor.IsDead
					&& p.Actor.IsInWorld
					&& p.Actor.Owner == self.Owner
					&& Info.ProducerActors.Contains(p.Actor.Info.Name)
					&& !p.Trait.IsTraitDisabled
					&& !p.Trait.IsTraitPaused
					&& Info.ProductionTypes.Any(t => p.Trait.Info.Produces.Contains(t)));

			if (producer.Trait != null)
				return producer;

			return null;
		}

		void ProduceActor()
		{
			var producer = FindAvailableProducer();
			if (producer == null)
				return;

			var actorInfo = self.World.Map.Rules.Actors[Info.ActorToProduce];
			var inits = new TypeDictionary
			{
				new OwnerInit(self.Owner),
				new FactionInit(BuildableInfo.GetInitialFaction(actorInfo, self.Owner.Faction.InternalName))
			};

			if (producer.Value.Trait.Produce(producer.Value.Actor, actorInfo, Info.ProductionTypes.First(), inits, 0))
				currentValue -= requiredValue;
		}
	}
}
