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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Scripting;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public class GameInfoLogicCA : ChromeLogic
	{
		[FluentReference]
		const string Objectives = "menu-game-info.objectives";

		[FluentReference]
		const string Briefing = "menu-game-info.briefing";

		[FluentReference]
		const string NextMission = "menu-game-info-ca.next-mission";

		[FluentReference]
		const string Options = "menu-game-info.options";

		[FluentReference]
		const string Debug = "menu-game-info.debug";

		[FluentReference]
		const string Chat = "menu-game-info.chat";

		readonly World world;
		readonly ModData modData;
		readonly Action<bool> hideMenu;
		readonly Action closeMenu;
		readonly IObjectivesPanel iop;
		readonly bool hasError;
		readonly bool showNextMissionTab;
		IngameInfoPanel activePanel;

		[ObjectCreator.UseCtor]
		public GameInfoLogicCA(Widget widget, ModData modData, World world, IngameInfoPanel initialPanel,
			Action<bool> hideMenu, Action closeMenu, Dictionary<string, MiniYaml> logicArgs)
		{
			this.world = world;
			this.modData = modData;
			this.hideMenu = hideMenu;
			this.closeMenu = closeMenu;
			activePanel = initialPanel;

			var finalMissionName = logicArgs.TryGetValue("FinalMissionName", out var finalMissionNode)
				? finalMissionNode.Value
				: null;

			showNextMissionTab = CanShowNextMissionTab(finalMissionName);

			var panels = new Dictionary<IngameInfoPanel, (string Panel, string Label, Action<ButtonWidget, Widget> Setup)>()
			{
				{ IngameInfoPanel.Objectives, ("OBJECTIVES_PANEL", Objectives, SetupObjectivesPanel) },
				{ IngameInfoPanel.Map, ("MAP_PANEL", showNextMissionTab ? NextMission : Briefing, SetupMapPanel) },
				{ IngameInfoPanel.LobbbyOptions, ("LOBBY_OPTIONS_PANEL", Options, SetupLobbyOptionsPanel) },
				{ IngameInfoPanel.Debug, ("DEBUG_PANEL", Debug, SetupDebugPanel) },
				{ IngameInfoPanel.Chat, ("CHAT_PANEL", Chat, SetupChatPanel) }
			};

			var visiblePanels = new List<IngameInfoPanel>();

			var scriptContext = world.WorldActor.TraitOrDefault<LuaScript>();
			hasError = scriptContext != null && scriptContext.FatalErrorOccurred;
			iop = world.WorldActor.TraitsImplementing<IObjectivesPanel>().FirstOrDefault();

			if (hasError || (iop != null && iop.PanelName != null))
				visiblePanels.Add(IngameInfoPanel.Objectives);

			var missionData = world.WorldActor.Info.TraitInfoOrDefault<MissionDataInfo>();
			if (showNextMissionTab || (missionData != null && !string.IsNullOrEmpty(missionData.Briefing)))
				visiblePanels.Add(IngameInfoPanel.Map);

			visiblePanels.Add(IngameInfoPanel.LobbbyOptions);

			var def = world.Map.Rules.Actors[SystemActors.Player].TraitInfo<DeveloperModeInfo>().CheckboxEnabled;
			var developerEnabled = world.LobbyInfo.GlobalSettings.OptionOrDefault("cheats", def);
			if (world.LocalPlayer != null && developerEnabled)
				visiblePanels.Add(IngameInfoPanel.Debug);

			if (world.LobbyInfo.NonBotClients.Count() > 1)
				visiblePanels.Add(IngameInfoPanel.Chat);

			var numTabs = visiblePanels.Count;
			var tabContainer = !hasError ? widget.GetOrNull($"TAB_CONTAINER_{numTabs}") : null;
			if (tabContainer != null)
				tabContainer.IsVisible = () => true;

			var chatPanel = widget.Get(panels[IngameInfoPanel.Chat].Panel);

			for (var i = 0; i < numTabs; i++)
			{
				var type = visiblePanels[i];
				var (panel, label, setup) = panels[type];
				var tabButton = tabContainer?.Get<ButtonWidget>($"BUTTON{i + 1}");

				if (tabButton != null)
				{
					var tabButtonText = FluentProvider.GetMessage(label);
					tabButton.GetText = () => tabButtonText;
					tabButton.OnClick = () =>
					{
						if (activePanel == IngameInfoPanel.Chat)
							LeaveChatPanel(chatPanel);

						activePanel = type;
					};
					tabButton.IsHighlighted = () => activePanel == type;
				}

				var panelContainer = widget.Get<ContainerWidget>(panel);
				panelContainer.IsVisible = () => activePanel == type;
				setup(tabButton, panelContainer);

				if (activePanel == IngameInfoPanel.AutoSelect)
					activePanel = type;
			}

			var titleText = widget.Get<LabelWidget>("TITLE");

			var mapTitle = world.Map.Title;
			var firstCategory = world.Map.Categories.FirstOrDefault();
			if (firstCategory != null)
				mapTitle = firstCategory + ": " + mapTitle;

			titleText.GetText = () => mapTitle;
		}

		bool CanShowNextMissionTab(string finalMissionName)
		{
			if (world.Type != WorldType.Regular || world.LocalPlayer == null)
				return false;

			if (world.LobbyInfo.GlobalSettings.Dedicated || world.LobbyInfo.NonBotClients.Count() != 1)
				return false;

			if (!world.IsGameOver || world.LocalPlayer.WinState != WinState.Won)
				return false;

			if (!world.Map.Categories.Contains("Campaign"))
				return false;

			if (MatchesCurrentMission(finalMissionName))
				return false;

			return CampaignProgressTracker.GetNextCampaignMissionUid(modData, world.Map.Uid) != null;
		}

		bool MatchesCurrentMission(string finalMissionName)
		{
			if (string.IsNullOrWhiteSpace(finalMissionName))
				return false;

			if (string.Equals(world.Map.Title, finalMissionName, StringComparison.OrdinalIgnoreCase))
				return true;

			var normalizedTitle = CampaignProgressTracker.GetMapTitleWithoutNumber(world.Map.Title);
			if (string.Equals(normalizedTitle, finalMissionName, StringComparison.OrdinalIgnoreCase))
				return true;

			var currentMapUid = modData.MapCache.GetUpdatedMap(world.Map.Uid) ?? world.Map.Uid;
			var currentPreview = modData.MapCache.FirstOrDefault(p => p.Uid == currentMapUid);
			var currentMissionId = currentPreview != null ? Path.GetFileName(currentPreview.Package.Name) : null;
			return string.Equals(currentMissionId, finalMissionName, StringComparison.OrdinalIgnoreCase);
		}

		void SetupObjectivesPanel(ButtonWidget objectivesTabButton, Widget objectivesPanelContainer)
		{
			var panel = hasError ? "SCRIPT_ERROR_PANEL" : iop.PanelName;
			Game.LoadWidget(world, panel, objectivesPanelContainer, new WidgetArgs()
			{
				{ "hideMenu", hideMenu },
				{ "closeMenu", closeMenu },
			});
		}

		void SetupMapPanel(ButtonWidget mapTabButton, Widget mapPanelContainer)
		{
			var panel = showNextMissionTab ? "NEXT_MISSION_PANEL" : "MAP_PANEL";
			Game.LoadWidget(world, panel, mapPanelContainer, new WidgetArgs());
		}

		void SetupLobbyOptionsPanel(ButtonWidget mapTabButton, Widget optionsPanelContainer)
		{
			Game.LoadWidget(world, "LOBBY_OPTIONS_PANEL", optionsPanelContainer, new WidgetArgs()
			{
				{ "getMap", (Func<MapPreview>)(() => modData.MapCache[world.Map.Uid]) },
				{ "configurationDisabled", (Func<bool>)(() => true) }
			});
		}

		void SetupDebugPanel(ButtonWidget debugTabButton, Widget debugPanelContainer)
		{
			if (debugTabButton != null)
				debugTabButton.IsDisabled = () => world.IsGameOver;

			Game.LoadWidget(world, "DEBUG_PANEL", debugPanelContainer, new WidgetArgs());

			if (activePanel == IngameInfoPanel.AutoSelect)
				activePanel = IngameInfoPanel.Debug;
		}

		void SetupChatPanel(ButtonWidget chatTabButton, Widget chatPanelContainer)
		{
			if (chatTabButton != null)
			{
				var lastOnClick = chatTabButton.OnClick;
				chatTabButton.OnClick = () =>
				{
					lastOnClick();
					chatPanelContainer.Get<TextFieldWidget>("CHAT_TEXTFIELD").TakeKeyboardFocus();
				};
			}

			Game.LoadWidget(world, "CHAT_CONTAINER", chatPanelContainer, new WidgetArgs() { { "isMenuChat", true } });
		}

		static void LeaveChatPanel(Widget chatPanelContainer)
		{
			chatPanelContainer.Get<TextFieldWidget>("CHAT_TEXTFIELD").YieldKeyboardFocus();
		}
	}
}
