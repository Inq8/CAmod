#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Turns actor randomly when idle. CA version just allows turning when mobile trait is paused.")]
	class TurnOnIdleCAInfo : ConditionalTraitInfo, Requires<MobileInfo>
	{
		[Desc("Minimum amount of ticks the actor will wait before the turn.")]
		public readonly int MinDelay = 400;

		[Desc("Maximum amount of ticks the actor will wait before the turn.")]
		public readonly int MaxDelay = 800;

		[Desc("Continue turning while mobile trait is paused.")]
		public readonly bool TurnWhileMobilePaused = false;

		public override object Create(ActorInitializer init) { return new TurnOnIdleCA(init, this); }
	}

	class TurnOnIdleCA : ConditionalTrait<TurnOnIdleCAInfo>, INotifyIdle
	{
		int currentDelay;
		WAngle targetFacing;
		readonly Mobile mobile;

		public TurnOnIdleCA(ActorInitializer init, TurnOnIdleCAInfo info)
			: base(info)
		{
			currentDelay = init.World.SharedRandom.Next(Info.MinDelay, Info.MaxDelay);
			mobile = init.Self.Trait<Mobile>();
			targetFacing = mobile.Facing;
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (mobile.IsTraitDisabled || (mobile.IsTraitPaused && !Info.TurnWhileMobilePaused))
				return;

			if (--currentDelay > 0)
				return;

			if (targetFacing == mobile.Facing)
			{
				targetFacing = new WAngle(self.World.SharedRandom.Next(1024));
				currentDelay = self.World.SharedRandom.Next(Info.MinDelay, Info.MaxDelay);
			}

			mobile.Facing = Util.TickFacing(mobile.Facing, targetFacing, mobile.TurnSpeed);
		}
	}
}
