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
	[Desc("Saves to a list of captured factions to make captured production as accurate as possible.")]
	public class TracksCapturedFactionInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new TracksCapturedFaction(this, init.Self); }
	}

	public class TracksCapturedFaction : INotifyOwnerChanged
	{
		public TracksCapturedFaction(TracksCapturedFactionInfo info, Actor self) { }

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			var manager = newOwner.PlayerActor.TraitOrDefault<CapturedFactionsManager>();

			if (manager == null)
				return;

			manager.AddFaction(oldOwner.Faction.InternalName);
		}
	}
}
