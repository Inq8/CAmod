#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Extends Aircraft. Primarily for attached actors so they don't trigger TakeOff or AssociateWithAirfield activities.")]
	public class AttachedAircraftInfo : AircraftInfo
	{
		public override object Create(ActorInitializer init) { return new AttachedAircraft(init, this); }
	}

	public class AttachedAircraft : Aircraft, ICreationActivity
	{
		readonly Actor self;

		public AttachedAircraft(ActorInitializer init, AttachedAircraftInfo info)
			: base(init, info)
		{
			self = init.Self;
		}

		protected override void Created(Actor self)
		{
			var newPosition = new WPos(self.CenterPosition.X, self.CenterPosition.X, Info.CruiseAltitude.Length);
			SetPosition(self, newPosition);
			SetCenterPosition(self, newPosition);
			base.Created(self);
		}

		Activity ICreationActivity.GetCreationActivity()
		{
			return new FlyIdle(self);
		}
	}
}
