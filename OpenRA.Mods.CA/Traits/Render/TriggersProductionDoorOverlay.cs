#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Play production door animation on allied building when unit is produced.")]
	public class TriggersProductionDoorOverlayInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new TriggersProductionDoorOverlay(init.Self, this); }
	}

	public class TriggersProductionDoorOverlay : INotifyProduction, INotifyCreated
	{
		WithProductionDoorOverlayCA producerDoorTrait;

		public TriggersProductionDoorOverlay(Actor self, TriggersProductionDoorOverlayInfo info) {}

		void INotifyCreated.Created(Actor self)
		{
			var producer = self.World.ActorMap.GetActorsAt(self.Location)
				.FirstOrDefault(a =>
					a != self
					&& a.Owner.IsAlliedWith(self.Owner)
					&& a.Info.HasTraitInfo<WithProductionDoorOverlayCAInfo>());

			if (producer != null)
				producerDoorTrait = producer.Trait<WithProductionDoorOverlayCA>();
		}

		void INotifyProduction.UnitProduced(Actor self, Actor other, CPos exit)
		{
			producerDoorTrait?.OpenDoor(other, exit);
		}
	}
}
