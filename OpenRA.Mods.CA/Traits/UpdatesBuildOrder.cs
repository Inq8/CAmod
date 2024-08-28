#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Added to build order when the actor is created.")]
	public class UpdatesBuildOrderInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new UpdatesBuildOrder(init, this); }
	}

	public class UpdatesBuildOrder : INotifyCreated
	{
		public readonly UpdatesBuildOrderInfo Info;
		readonly BuildOrderTracker buildOrderTracker;

		public UpdatesBuildOrder(ActorInitializer init, UpdatesBuildOrderInfo info)
		{
			Info = info;
			buildOrderTracker = init.Self.Owner.PlayerActor.Trait<BuildOrderTracker>();
		}

		void INotifyCreated.Created(Actor self)
		{
			buildOrderTracker.BuildingCreated(self.Info.Name);
		}
	}
}
