#region Copyright & License Information
/**
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
using OpenRA.Mods.Common.Widgets;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public class LobbyOptionsLogicCA : ChromeLogic
	{
		[TranslationReference]
		const string NotAvailable = "label-not-available";

		readonly ScrollPanelWidget panel;
		readonly Widget optionsContainer;
		readonly Widget checkboxRowTemplate;
		readonly Widget dropdownRowTemplate;
		readonly int yMargin;

		readonly Func<MapPreview> getMap;
		readonly OrderManager orderManager;
		readonly Func<bool> configurationDisabled;
		MapPreview mapPreview;

		readonly string savedOptionsFilePath;

		[ObjectCreator.UseCtor]
		internal LobbyOptionsLogicCA(Widget widget, OrderManager orderManager, Func<MapPreview> getMap, Func<bool> configurationDisabled)
		{
			this.getMap = getMap;
			this.orderManager = orderManager;
			this.configurationDisabled = configurationDisabled;

			panel = (ScrollPanelWidget)widget;
			optionsContainer = widget.Get("LOBBY_OPTIONS");
			yMargin = optionsContainer.Bounds.Y;
			checkboxRowTemplate = optionsContainer.Get("CHECKBOX_ROW_TEMPLATE");
			dropdownRowTemplate = optionsContainer.Get("DROPDOWN_ROW_TEMPLATE");

			var logDir = Platform.SupportDir + "Logs";
			savedOptionsFilePath = Path.Combine(logDir, "ca-lobbyoptions.log");
			var loadOptions = widget.Parent.Get<ButtonWidget>("LOAD_OPTIONS");
			var saveOptions = widget.Parent.Get<ButtonWidget>("SAVE_OPTIONS");
			loadOptions.OnClick = () => LoadOptions();
			loadOptions.IsDisabled = () => configurationDisabled();
			saveOptions.OnClick = () => SaveOptions();
			saveOptions.IsDisabled = () => configurationDisabled();

			mapPreview = getMap();
			RebuildOptions();
		}

		public override void Tick()
		{
			var newMapPreview = getMap();
			if (newMapPreview == mapPreview)
				return;

			// We are currently enumerating the widget tree and so can't modify any layout
			// Defer it to the end of tick instead
			Game.RunAfterTick(() =>
			{
				mapPreview = newMapPreview;
				RebuildOptions();
			});
		}

		void RebuildOptions()
		{
			if (mapPreview == null || mapPreview.WorldActorInfo == null)
				return;

			optionsContainer.RemoveChildren();
			optionsContainer.Bounds.Height = 0;
			var allOptions = GetOptions();

			Widget row = null;
			var checkboxColumns = new Queue<CheckboxWidget>();
			var dropdownColumns = new Queue<DropDownButtonWidget>();

			foreach (var option in allOptions.Where(o => o is LobbyBooleanOption))
			{
				if (checkboxColumns.Count == 0)
				{
					row = checkboxRowTemplate.Clone();
					row.Bounds.Y = optionsContainer.Bounds.Height;
					optionsContainer.Bounds.Height += row.Bounds.Height;
					foreach (var child in row.Children)
						if (child is CheckboxWidget childCheckbox)
							checkboxColumns.Enqueue(childCheckbox);

					optionsContainer.AddChild(row);
				}

				var checkbox = checkboxColumns.Dequeue();
				var optionEnabled = new PredictedCachedTransform<Session.Global, bool>(
					gs => gs.LobbyOptions[option.Id].IsEnabled);

				var optionLocked = new CachedTransform<Session.Global, bool>(
					gs => gs.LobbyOptions[option.Id].IsLocked);

				checkbox.GetText = () => option.Name;
				checkbox.GetTooltipText = () => option.Name;
				if (option.Description != null)
					checkbox.GetTooltipDesc = () => option.Description;

				checkbox.IsVisible = () => true;
				checkbox.IsChecked = () => optionEnabled.Update(orderManager.LobbyInfo.GlobalSettings);
				checkbox.IsDisabled = () => configurationDisabled() || optionLocked.Update(orderManager.LobbyInfo.GlobalSettings);
				checkbox.OnClick = () =>
				{
					var state = !optionEnabled.Update(orderManager.LobbyInfo.GlobalSettings);
					orderManager.IssueOrder(Order.Command($"option {option.Id} {state}"));
					optionEnabled.Predict(state);
				};
			}

			foreach (var option in allOptions.Where(o => o is not LobbyBooleanOption))
			{
				if (dropdownColumns.Count == 0)
				{
					row = dropdownRowTemplate.Clone();
					row.Bounds.Y = optionsContainer.Bounds.Height;
					optionsContainer.Bounds.Height += row.Bounds.Height;
					foreach (var child in row.Children)
						if (child is DropDownButtonWidget dropDown)
							dropdownColumns.Enqueue(dropDown);

					optionsContainer.AddChild(row);
				}

				var dropdown = dropdownColumns.Dequeue();
				var optionValue = new CachedTransform<Session.Global, Session.LobbyOptionState>(
					gs => gs.LobbyOptions[option.Id]);

				var getOptionLabel = new CachedTransform<string, string>(id =>
				{
					if (id == null || !option.Values.TryGetValue(id, out var value))
						return TranslationProvider.GetString(NotAvailable);

					return value;
				});

				dropdown.GetText = () => getOptionLabel.Update(optionValue.Update(orderManager.LobbyInfo.GlobalSettings).Value);
				dropdown.GetTooltipText = () => option.Name;
				if (option.Description != null)
					dropdown.GetTooltipDesc = () => option.Description;
				dropdown.IsVisible = () => true;
				dropdown.IsDisabled = () => configurationDisabled() ||
					optionValue.Update(orderManager.LobbyInfo.GlobalSettings).IsLocked;

				dropdown.OnMouseDown = _ =>
				{
					ScrollItemWidget SetupItem(KeyValuePair<string, string> c, ScrollItemWidget template)
					{
						bool IsSelected() => optionValue.Update(orderManager.LobbyInfo.GlobalSettings).Value == c.Key;
						void OnClick() => orderManager.IssueOrder(Order.Command($"option {option.Id} {c.Key}"));

						var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);
						item.Get<LabelWidget>("LABEL").GetText = () => c.Value;
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

			panel.ContentHeight = yMargin + optionsContainer.Bounds.Height;
			optionsContainer.Bounds.Y = yMargin;

			panel.ScrollToTop();
		}

		IEnumerable<LobbyOption> GetOptions()
		{
			return mapPreview.PlayerActorInfo.TraitInfos<ILobbyOptions>()
				.Concat(mapPreview.WorldActorInfo.TraitInfos<ILobbyOptions>())
				.SelectMany(t => t.LobbyOptions(mapPreview))
				.Where(o => o.IsVisible)
				.OrderBy(o => o.DisplayOrder)
				.ToArray();
		}

		void LoadOptions()
		{
			var savedOptions = new Dictionary<string, string>();

			if (!File.Exists(savedOptionsFilePath))
				return;

			try {
				var savedOptionsFileContents = File.ReadAllText(savedOptionsFilePath);
				savedOptions = JsonConvert.DeserializeObject<Dictionary<string, string>>(savedOptionsFileContents);

				foreach (var option in savedOptions)
					orderManager.IssueOrder(Order.Command($"option {option.Key} {option.Value}"));

				TextNotificationsManager.AddSystemLine("Lobby options loaded.");
			}
			catch
			{
				TextNotificationsManager.AddSystemLine("Could not load lobby options.");
			}
		}

		void SaveOptions()
		{
			var allOptions = GetOptions();
			var options = new Dictionary<string, string>();

			foreach (var option in allOptions.Where(o => o is LobbyBooleanOption))
			{
				var optionEnabled = orderManager.LobbyInfo.GlobalSettings.OptionOrDefault(option.Id, false);
				options.Add(option.Id, optionEnabled ? "True" : "False");
			}

			foreach (var option in allOptions.Where(o => o is not LobbyBooleanOption))
			{
				var optionValue = orderManager.LobbyInfo.GlobalSettings.OptionOrDefault(option.Id, null);
				if (optionValue != null)
					options.Add(option.Id, optionValue);
			}

			try {
				var json = JsonConvert.SerializeObject(options);
				File.WriteAllText(savedOptionsFilePath, json);
				TextNotificationsManager.AddSystemLine("Lobby options saved.");
			}
			catch
			{
				TextNotificationsManager.AddSystemLine("Could not save lobby options.");
			}
		}
	}
}
