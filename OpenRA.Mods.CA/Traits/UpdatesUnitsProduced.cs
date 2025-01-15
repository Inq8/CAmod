#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Attach to producer actors. Updates units produced.")]
	public class UpdatesUnitsProducedInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new UpdatesUnitsProduced(init, this); }
	}

	public class UpdatesUnitsProduced : INotifyCreated, INotifyOwnerChanged, INotifyProduction
	{
		public readonly UpdatesUnitsProducedInfo Info;
		ProductionTracker productionTracker;

		public UpdatesUnitsProduced(ActorInitializer init, UpdatesUnitsProducedInfo info)
		{
			Info = info;
			productionTracker = init.Self.Owner.PlayerActor.Trait<ProductionTracker>();
		}

		void INotifyCreated.Created(Actor self)
		{
			productionTracker = self.Owner.PlayerActor.Trait<ProductionTracker>();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			productionTracker = newOwner.PlayerActor.Trait<ProductionTracker>();
		}

		void INotifyProduction.UnitProduced(Actor self, Actor other, CPos exit)
		{
			var valued = other.Info.TraitInfoOrDefault<ValuedInfo>();
			var name = other.Info.Name.EndsWith(".ai") ? other.Info.Name[..^3] : other.Info.Name;

			if (valued != null && valued.Cost > 0)
				productionTracker.UnitCreated(name, valued.Cost);
		}
	}
}
