#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Turns actor randomly when idle.",
		"CA version applies to aircraft and allows turning when mobile trait is paused.")]
	class TurnOnIdleCAInfo : ConditionalTraitInfo, Requires<AircraftInfo>
	{
		[Desc("Minimum amount of ticks the actor will wait before the turn.")]
		public readonly int MinDelay = 400;

		[Desc("Maximum amount of ticks the actor will wait before the turn.")]
		public readonly int MaxDelay = 800;

		[Desc("Continue turning while aircraft trait is paused.")]
		public readonly bool TurnWhileAircraftPaused = false;

		public override object Create(ActorInitializer init) { return new TurnOnIdleCA(init, this); }
	}

	class TurnOnIdleCA : ConditionalTrait<TurnOnIdleCAInfo>, ITick
	{
		int currentDelay;
		WAngle targetFacing;
		readonly Aircraft aircraft;

		public TurnOnIdleCA(ActorInitializer init, TurnOnIdleCAInfo info)
			: base(info)
		{
			currentDelay = init.World.SharedRandom.Next(Info.MinDelay, Info.MaxDelay);
			aircraft = init.Self.Trait<Aircraft>();
			targetFacing = aircraft.Facing;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (aircraft.IsTraitDisabled || (aircraft.IsTraitPaused && !Info.TurnWhileAircraftPaused))
				return;

			if (!(self.CurrentActivity is FlyIdle))
				return;

			if (--currentDelay > 0)
				return;

			if (targetFacing == aircraft.Facing)
			{
				targetFacing = new WAngle(self.World.SharedRandom.Next(1024));
				currentDelay = self.World.SharedRandom.Next(Info.MinDelay, Info.MaxDelay);
			}

			aircraft.Facing = Util.TickFacing(aircraft.Facing, targetFacing, aircraft.TurnSpeed);
		}
	}
}
