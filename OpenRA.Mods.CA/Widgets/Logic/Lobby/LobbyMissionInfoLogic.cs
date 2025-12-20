#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.CA.Traits;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public class LobbyMissionInfoLogic : ChromeLogic
	{
		readonly Func<MapPreview> getMap;
		string lastMapUid;

		[ObjectCreator.UseCtor]
		internal LobbyMissionInfoLogic(Widget widget, OrderManager orderManager, Func<MapPreview> getMap)
		{
			_ = widget;
			_ = orderManager;
			this.getMap = getMap;

			Game.LobbyInfoChanged += OnLobbyInfoChanged;
		}

		void OnLobbyInfoChanged()
		{
			var map = getMap();
			if (map == null || map.WorldActorInfo == null)
				return;

			var mapUid = map.Uid;
			if (mapUid == lastMapUid)
				return;

			lastMapUid = mapUid;

			var lobbyMissionInfo = map.WorldActorInfo.TraitInfoOrDefault<LobbyMissionInfoInfo>();
			if (lobbyMissionInfo == null || string.IsNullOrEmpty(lobbyMissionInfo.Info))
				return;

			var info = lobbyMissionInfo.Info;
			var prefix = lobbyMissionInfo.Prefix;

			Game.RunAfterTick(() => TextNotificationsManager.AddChatLine(TextNotificationsManager.SystemClientId, prefix, info, Color.DeepSkyBlue, Color.DeepSkyBlue));
		}

		protected override void Dispose(bool disposing)
		{
			Game.LobbyInfoChanged -= OnLobbyInfoChanged;
			base.Dispose(disposing);
		}
	}
}
