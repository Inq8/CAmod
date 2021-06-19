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

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Sound
{
	[Desc("Play announcement when actor is created.")]
	public class AnnounceOnCreationInfo : TraitInfo
	{
		[NotificationReference("Speech")]
		public readonly string Notification = "UnitReady";

		[Desc("Delay in ticks.")]
		public readonly int Delay = 0;

		public readonly bool NotifyAll = false;

		public override object Create(ActorInitializer init) { return new AnnounceOnCreation(init.Self, this); }
	}

	public class AnnounceOnCreation : INotifyCreated, ITick
	{
		readonly AnnounceOnCreationInfo info;
		int tick;

		public AnnounceOnCreation(Actor self, AnnounceOnCreationInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			if (info.Delay == 0)
				PlaySound(self);
			else
				tick = info.Delay;
		}

		void ITick.Tick(Actor self)
		{
			if (info.Delay > 0 && --tick == 0)
				PlaySound(self);
		}

		void PlaySound(Actor self)
		{
			var player = info.NotifyAll ? self.World.LocalPlayer : self.Owner;
			Game.Sound.PlayNotification(self.World.Map.Rules, player, "Speech", info.Notification, self.Owner.Faction.InternalName);
		}
	}
}
