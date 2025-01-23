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
	[Desc("When picked up, cancels any activities.")]
	class CancelActivityOnPickupInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new CancelActivityOnPickup(init, this); }
	}

	class CancelActivityOnPickup : ConditionalTrait<CancelActivityOnPickupInfo>, INotifyRemovedFromWorld
	{
		public CancelActivityOnPickup(ActorInitializer init, CancelActivityOnPickupInfo info)
			: base(info) { }

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			var carryable = self.TraitOrDefault<Carryable>();
			if (carryable != null && carryable.Carrier != null)
			{
				self.CancelActivity();
			}
		}
	}
}
