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

			var lobbyMissionInfos = map.WorldActorInfo.TraitInfos<LobbyMissionInfoInfo>();

			if (lobbyMissionInfos.Count == 0)
				return;

			Game.RunAfterTick(() => {
				foreach (var lobbyMissionInfo in lobbyMissionInfos)
				{
					var text = lobbyMissionInfo.Text;
					var prefix = lobbyMissionInfo.Prefix;
					var prefixColor = lobbyMissionInfo.PrefixColor;
					var textColor = lobbyMissionInfo.TextColor;

					text = text.Replace("\\n", "\n");

					TextNotificationsManager.AddChatLine(TextNotificationsManager.SystemClientId, prefix, text, prefixColor, textColor);
				}
			});
		}

		protected override void Dispose(bool disposing)
		{
			Game.LobbyInfoChanged -= OnLobbyInfoChanged;
			base.Dispose(disposing);
		}
	}
}
