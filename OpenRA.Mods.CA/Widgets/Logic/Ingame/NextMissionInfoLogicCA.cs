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
using Newtonsoft.Json;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public class NextMissionInfoLogicCA : ChromeLogic
	{
		enum PanelType { Briefing, Options }

		readonly ModData modData;
		readonly World world;
		readonly MapPreview nextMission;
		readonly ScrollPanelWidget descriptionPanel;
		readonly LabelWidget description;
		readonly Widget optionsContainer;
		readonly Widget checkboxRowTemplate;
		readonly Widget dropdownRowTemplate;
		readonly Dictionary<string, string> missionOptions = new();
		readonly string savedOptionsFilePath;

		PanelType activePanel = PanelType.Briefing;
		bool leaving;

		[ObjectCreator.UseCtor]
		public NextMissionInfoLogicCA(Widget widget, ModData modData, World world)
		{
			this.modData = modData;
			this.world = world;
			savedOptionsFilePath = Path.Combine(Platform.SupportDir + "Logs", "ca-missionoptions.log");

			var nextMissionUid = CampaignProgressTracker.GetNextCampaignMissionUid(modData, world.Map.Uid);
			if (nextMissionUid != null)
			{
				var updatedMapUid = modData.MapCache.GetUpdatedMap(nextMissionUid) ?? nextMissionUid;
				nextMission = modData.MapCache[updatedMapUid];
			}

			var previewWidget = widget.Get<MapPreviewWidget>("MISSION_PREVIEW");
			previewWidget.Preview = () => nextMission;

			var briefingButton = widget.Get<ButtonWidget>("BRIEFING_TAB");
			briefingButton.IsHighlighted = () => activePanel == PanelType.Briefing;
			briefingButton.OnClick = () => activePanel = PanelType.Briefing;

			var optionsButton = widget.Get<ButtonWidget>("OPTIONS_TAB");
			optionsButton.IsHighlighted = () => activePanel == PanelType.Options;
			optionsButton.OnClick = () => activePanel = PanelType.Options;

			descriptionPanel = widget.Get<ScrollPanelWidget>("MISSION_DESCRIPTION_PANEL");
			descriptionPanel.IsVisible = () => activePanel == PanelType.Briefing;

			description = descriptionPanel.Get<LabelWidget>("MISSION_DESCRIPTION");
			PopulateBriefing();

			optionsContainer = widget.Get("MISSION_OPTIONS");
			optionsContainer.IsVisible = () => activePanel == PanelType.Options;
			checkboxRowTemplate = optionsContainer.Get("CHECKBOX_ROW_TEMPLATE");
			dropdownRowTemplate = optionsContainer.Get("DROPDOWN_ROW_TEMPLATE");
			RebuildOptions();

			var continueButton = widget.Get<ButtonWidget>("CONTINUE_BUTTON");
			continueButton.IsDisabled = () => leaving || nextMission == null;
			continueButton.OnClick = StartNextMission;
		}

		void PopulateBriefing()
		{
			var briefingText = nextMission?.WorldActorInfo?.TraitInfoOrDefault<MissionDataInfo>()?.Briefing?.Replace("\\n", "\n") ?? string.Empty;
			var wrapped = WidgetUtils.WrapText(briefingText, description.Bounds.Width, Game.Renderer.Fonts[description.Font]);
			description.GetText = () => wrapped;
			description.Bounds.Height = Game.Renderer.Fonts[description.Font].Measure(wrapped).Y;
			descriptionPanel.ScrollToTop();
			descriptionPanel.Layout.AdjustChildren();
		}

		void RebuildOptions()
		{
			if (nextMission == null || nextMission.WorldActorInfo == null)
				return;

			LoadSavedOptions();
			optionsContainer.RemoveChildren();

			var allOptions = nextMission.PlayerActorInfo.TraitInfos<ILobbyOptions>()
				.Concat(nextMission.WorldActorInfo.TraitInfos<ILobbyOptions>())
				.SelectMany(t => t.LobbyOptions(nextMission))
				.Where(o => o.IsVisible)
				.OrderBy(o => o.DisplayOrder)
				.ToArray();

			Widget row = null;
			var checkboxColumns = new Queue<CheckboxWidget>();
			var dropdownColumns = new Queue<DropDownButtonWidget>();

			var yOffset = 0;
			foreach (var option in allOptions.Where(o => o is LobbyBooleanOption))
			{
				if (!missionOptions.ContainsKey(option.Id) || !(new[] { "True", "False" }.Contains(missionOptions[option.Id])))
					missionOptions[option.Id] = option.DefaultValue;

				if (checkboxColumns.Count == 0)
				{
					row = checkboxRowTemplate.Clone();
					row.Bounds.Y = yOffset;
					yOffset += row.Bounds.Height;
					foreach (var child in row.Children)
						if (child is CheckboxWidget childCheckbox)
							checkboxColumns.Enqueue(childCheckbox);

					optionsContainer.AddChild(row);
				}

				var checkbox = checkboxColumns.Dequeue();
				checkbox.GetText = () => option.Name;
				if (option.Description != null)
					checkbox.GetTooltipText = () => option.Description;

				checkbox.IsVisible = () => true;
				checkbox.IsChecked = () => missionOptions[option.Id] == "True";
				checkbox.IsDisabled = () => option.IsLocked;
				checkbox.OnClick = () =>
				{
					missionOptions[option.Id] = missionOptions[option.Id] == "True" ? "False" : "True";
					SaveOptions();
				};
			}

			foreach (var option in allOptions.Where(o => o is not LobbyBooleanOption))
			{
				if (!missionOptions.ContainsKey(option.Id) || !option.Values.Select(opt => opt.Key).Contains(missionOptions[option.Id]))
					missionOptions[option.Id] = option.DefaultValue;

				if (dropdownColumns.Count == 0)
				{
					row = dropdownRowTemplate.Clone();
					row.Bounds.Y = yOffset;
					yOffset += row.Bounds.Height;
					foreach (var child in row.Children)
						if (child is DropDownButtonWidget dropDown)
							dropdownColumns.Enqueue(dropDown);

					optionsContainer.AddChild(row);
				}

				var dropdown = dropdownColumns.Dequeue();
				dropdown.GetText = () => option.Values[missionOptions[option.Id]];
				if (option.Description != null)
					dropdown.GetTooltipText = () => option.Description;
				dropdown.IsVisible = () => true;
				dropdown.IsDisabled = () => option.IsLocked;

				dropdown.OnMouseDown = _ =>
				{
					ScrollItemWidget SetupItem(KeyValuePair<string, string> choice, ScrollItemWidget template)
					{
						bool IsSelected() => missionOptions[option.Id] == choice.Key;
						void OnClick()
						{
							missionOptions[option.Id] = choice.Key;
							SaveOptions();
						}

						var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);
						item.Get<LabelWidget>("LABEL").GetText = () => choice.Value;
						return item;
					}

					dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", option.Values.Count * 30, option.Values, SetupItem);
				};

				var label = row.GetOrNull<LabelWidget>(dropdown.Id + "_DESC");
				if (label != null)
				{
					label.GetText = () => option.Name + ":";
					label.IsVisible = () => true;
				}
			}
		}

		void StartNextMission()
		{
			if (nextMission == null)
				return;

			var updatedMapUid = modData.MapCache.GetUpdatedMap(nextMission.Uid) ?? nextMission.Uid;

			var map = modData.MapCache[updatedMapUid];
			var orders = new List<Order>();

			foreach (var option in missionOptions)
				orders.Add(Order.Command($"option {option.Key} {option.Value}"));

			orders.Add(Order.Command($"state {Session.ClientState.Ready}"));

			var iop = world.WorldActor.TraitsImplementing<IObjectivesPanel>().FirstOrDefault();
			var exitDelay = iop?.ExitDelay ?? 0;
			var mpe = world.WorldActor.TraitOrDefault<MenuPostProcessEffect>();

			leaving = true;
			Ui.CloseWindow();
			Ui.ResetTooltips();

			if (mpe != null)
			{
				if (Game.IsCurrentWorld(world))
					mpe.Fade(MenuPostProcessEffect.EffectType.Black);

				exitDelay += 40 * mpe.Info.FadeLength;
			}

			Game.RunAfterDelay(exitDelay, () =>
			{
				Game.Disconnect();
				Ui.ResetAll();
				Game.CreateAndStartLocalServer(map.Uid, orders);
			});
		}

		void LoadSavedOptions()
		{
			if (!File.Exists(savedOptionsFilePath))
				return;

			try
			{
				var savedOptionsFileContents = File.ReadAllText(savedOptionsFilePath);
				var savedOptions = JsonConvert.DeserializeObject<Dictionary<string, string>>(savedOptionsFileContents);

				foreach (var option in savedOptions)
					missionOptions[option.Key] = option.Value;
			}
			catch
			{
				// do nothing
			}
		}

		void SaveOptions()
		{
			try
			{
				var json = JsonConvert.SerializeObject(missionOptions);
				File.WriteAllText(savedOptionsFilePath, json);
			}
			catch
			{
				// do nothing
			}
		}
	}
}