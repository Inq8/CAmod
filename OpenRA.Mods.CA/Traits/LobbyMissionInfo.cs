

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
	[Desc("Displays additional info in the lobby chat after a map is selected. Requires LobbyMissionInfoLogic widget logic.")]
	[TraitLocation(SystemActors.World)]
	public class LobbyMissionInfoInfo : TraitInfo
	{
		[FieldLoader.Require]
		[Desc("Info to display in the lobby chat.")]
		public readonly string Info = "";

		[Desc("Prefix to display before the info in the lobby chat.")]
		public readonly string Prefix = "Mission Info";

		public override object Create(ActorInitializer init) { return new LobbyMissionInfo(); }
	}

	public class LobbyMissionInfo { }
}
