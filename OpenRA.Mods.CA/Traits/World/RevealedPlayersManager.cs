#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Attached to the world actor to track which players are revealed (for displaying their real faction in scores panel).")]
	[TraitLocation(SystemActors.World)]
	public class RevealedPlayersManagerInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new RevealedPlayersManager(init.World, this); }
	}

	public class RevealedPlayersManager : INotifySelection
	{
		readonly World world;
		public HashSet<Player> Players { get; private set; }

		public RevealedPlayersManager(World world, RevealedPlayersManagerInfo info)
		{
			Players = new HashSet<Player>();
			this.world = world;
		}

		public void RevealPlayer(Player player)
		{
			Players.Add(player);
		}

		public bool IsRevealed(Player player)
		{
			return Players.Contains(player);
		}

		void INotifySelection.SelectionChanged()
		{
			// Disable for spectators
			if (world.LocalPlayer == null || world.LocalPlayer.Spectating)
				return;

			var players = world.Selection.Actors
				.Where(a => a.IsInWorld)
				.Select(a => a.Owner);

			foreach (var player in players)
				RevealPlayer(player);
		}
	}
}
