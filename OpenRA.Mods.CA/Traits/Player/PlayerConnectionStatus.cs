#region Copyright & License Information
/*
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
	[TraitLocation(SystemActors.Player)]
	[Desc("Tracks player connection status.")]
	public class PlayerConnectionStatusInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new PlayerConnectionStatus(init, this); }
	}

	public class PlayerConnectionStatus : INotifyPlayerDisconnected
	{
		bool isConnected;
		public bool IsConnected => isConnected;

		public PlayerConnectionStatus(ActorInitializer init, PlayerConnectionStatusInfo info)
		{
			isConnected = true;
		}

		void INotifyPlayerDisconnected.PlayerDisconnected(Actor self, Player p)
		{
			isConnected = false;
		}
	}
}
