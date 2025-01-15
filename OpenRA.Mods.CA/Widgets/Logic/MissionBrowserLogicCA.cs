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
using System.Threading;
using Newtonsoft.Json;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	// Temporary until added to engine
	public class MissionBrowserLogicCA : ChromeLogic
	{
		enum PlayingVideo { None, Info, Briefing, GameStart }
		enum PanelType { MissionInfo, Options }

		[TranslationReference]
		const string NoVideoTitle = "dialog-no-video.title";

		[TranslationReference]
		const string NoVideoPrompt = "dialog-no-video.prompt";

		[TranslationReference]
		const string NoVideoCancel = "dialog-no-video.cancel";

		[TranslationReference]
		const string CantPlayTitle = "dialog-cant-play-video.title";

		[TranslationReference]
		const string CantPlayPrompt = "dialog-cant-play-video.prompt";

		[TranslationReference]
		const string CantPlayCancel = "dialog-cant-play-video.cancel";

		// Added to prevent unused language string warnings
		[TranslationReference]
		const string DifficultyLabel = "dropdown-difficulty.label";
		[TranslationReference]
		const string DifficultyDescription = "dropdown-difficulty.description";
		[TranslationReference]
		const string DifficultyEasy = "options-difficulty.easy";
		[TranslationReference]
		const string DifficultyNormal = "options-difficulty.normal";
		[TranslationReference]
		const string DifficultyHard = "options-difficulty.hard";

		readonly ModData modData;
		readonly Action onStart;
		readonly Widget missionDetail;
		readonly Widget optionsContainer;
		readonly Widget checkboxRowTemplate;
		readonly Widget dropdownRowTemplate;
		readonly ScrollPanelWidget descriptionPanel;
		readonly LabelWidget description;
		readonly SpriteFont descriptionFont;
		readonly ButtonWidget startBriefingVideoButton;
		readonly ButtonWidget stopBriefingVideoButton;
		readonly ButtonWidget startInfoVideoButton;
		readonly ButtonWidget stopInfoVideoButton;
		readonly VideoPlayerWidget videoPlayer;
		readonly BackgroundWidget fullscreenVideoPlayer;

		readonly ScrollPanelWidget missionList;
		readonly ScrollItemWidget headerTemplate;
		readonly ScrollItemWidget template;

		MapPreview selectedMap;
		PlayingVideo playingVideo;
		readonly Dictionary<string, string> missionOptions = new();
		PanelType panel = PanelType.MissionInfo;
		readonly string savedOptionsFilePath;
		Dictionary<string, MissionVictoryResult> campaignProgress = new();

		[ObjectCreator.UseCtor]
		public MissionBrowserLogicCA(Widget widget, ModData modData, World world, Action onStart, Action onExit, string initialMap)
		{
			this.modData = modData;
			this.onStart = onStart;
			Game.BeforeGameStart += OnGameStart;

			var logDir = Platform.SupportDir + "Logs";
			savedOptionsFilePath = Path.Combine(logDir, "ca-missionoptions.log");
			campaignProgress = CampaignProgressTracker.GetCampaignProgress();

			missionList = widget.Get<ScrollPanelWidget>("MISSION_LIST");

			headerTemplate = widget.Get<ScrollItemWidget>("HEADER");
			template = widget.Get<ScrollItemWidget>("TEMPLATE");

			var title = widget.GetOrNull<LabelWidget>("MISSIONBROWSER_TITLE");
			if (title != null)
				title.GetText = () => playingVideo != PlayingVideo.None ? selectedMap.Title : title.Text;

			widget.Get("MISSION_INFO").IsVisible = () => selectedMap != null;

			var previewWidget = widget.Get<MapPreviewWidget>("MISSION_PREVIEW");
			previewWidget.Preview = () => selectedMap;
			previewWidget.IsVisible = () => playingVideo == PlayingVideo.None;

			videoPlayer = widget.Get<VideoPlayerWidget>("MISSION_VIDEO");
			widget.Get("MISSION_BIN").IsVisible = () => playingVideo != PlayingVideo.None;
			fullscreenVideoPlayer = Ui.LoadWidget<BackgroundWidget>("FULLSCREEN_PLAYER", Ui.Root, new WidgetArgs { { "world", world } });

			missionDetail = widget.Get("MISSION_DETAIL");

			descriptionPanel = missionDetail.Get<ScrollPanelWidget>("MISSION_DESCRIPTION_PANEL");
			descriptionPanel.IsVisible = () => panel == PanelType.MissionInfo;

			description = descriptionPanel.Get<LabelWidget>("MISSION_DESCRIPTION");
			descriptionFont = Game.Renderer.Fonts[description.Font];

			optionsContainer = missionDetail.Get("MISSION_OPTIONS");
			optionsContainer.IsVisible = () => panel == PanelType.Options;
			checkboxRowTemplate = optionsContainer.Get("CHECKBOX_ROW_TEMPLATE");
			dropdownRowTemplate = optionsContainer.Get("DROPDOWN_ROW_TEMPLATE");

			startBriefingVideoButton = widget.Get<ButtonWidget>("START_BRIEFING_VIDEO_BUTTON");
			stopBriefingVideoButton = widget.Get<ButtonWidget>("STOP_BRIEFING_VIDEO_BUTTON");
			stopBriefingVideoButton.IsVisible = () => playingVideo == PlayingVideo.Briefing;
			stopBriefingVideoButton.OnClick = () => StopVideo(videoPlayer);

			startInfoVideoButton = widget.Get<ButtonWidget>("START_INFO_VIDEO_BUTTON");
			stopInfoVideoButton = widget.Get<ButtonWidget>("STOP_INFO_VIDEO_BUTTON");
			stopInfoVideoButton.IsVisible = () => playingVideo == PlayingVideo.Info;
			stopInfoVideoButton.OnClick = () => StopVideo(videoPlayer);

			var allPreviews = new List<MapPreview>();
			missionList.RemoveChildren();

			// Add a group for each campaign
			if (modData.Manifest.Missions.Length > 0)
			{
				var yaml = MiniYaml.Merge(modData.Manifest.Missions.Select(
					m => MiniYaml.FromStream(modData.DefaultFileSystem.Open(m), m)));

				foreach (var kv in yaml)
				{
					var missionMapPaths = kv.Value.Nodes.Select(n => n.Key).ToList();

					var previews = modData.MapCache
						.Where(p => p.Class == MapClassification.System && p.Status == MapStatus.Available)
						.Select(p => new
						{
							Preview = p,
							Index = missionMapPaths.IndexOf(Path.GetFileName(p.Package.Name))
						})
						.Where(x => x.Index != -1)
						.OrderBy(x => x.Index)
						.Select(x => x.Preview);

					if (previews.Any())
					{
						CreateMissionGroup(kv.Key, previews, onExit);
						allPreviews.AddRange(previews);
					}
				}
			}

			// Add an additional group for loose missions
			var loosePreviews = modData.MapCache
				.Where(p => p.Status == MapStatus.Available && p.Visibility.HasFlag(MapVisibility.MissionSelector) && !allPreviews.Any(a => a.Uid == p.Uid));

			if (loosePreviews.Any())
			{
				CreateMissionGroup("Missions", loosePreviews, onExit);
				allPreviews.AddRange(loosePreviews);
			}

			if (allPreviews.Count > 0)
			{
				var uid = modData.MapCache.GetUpdatedMap(initialMap);
				var map = uid == null ? null : modData.MapCache[uid];
				if (map != null && map.Visibility.HasFlag(MapVisibility.MissionSelector))
				{
					SelectMap(map);
					missionList.ScrollToSelectedItem();
				}
				else
					SelectMap(allPreviews.First());
			}

			// Preload map preview to reduce jank
			new Thread(() =>
			{
				foreach (var p in allPreviews)
					p.GetMinimap();
			}).Start();

			var startButton = widget.Get<ButtonWidget>("STARTGAME_BUTTON");
			startButton.OnClick = () => StartMissionClicked(onExit);
			startButton.IsDisabled = () => selectedMap == null;

			widget.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				StopVideo(videoPlayer);
				Game.Disconnect();
				Ui.CloseWindow();
				onExit();
			};

			var tabContainer = widget.Get("MISSION_TABS");
			tabContainer.IsVisible = () => true;

			var optionsTab = tabContainer.Get<ButtonWidget>("OPTIONS_TAB");
			optionsTab.IsHighlighted = () => panel == PanelType.Options;
			optionsTab.IsDisabled = () => false;
			optionsTab.OnClick = () => panel = PanelType.Options;

			var missionTab = tabContainer.Get<ButtonWidget>("MISSIONINFO_TAB");
			missionTab.IsHighlighted = () => panel == PanelType.MissionInfo;
			missionTab.IsDisabled = () => false;
			missionTab.OnClick = () => panel = PanelType.MissionInfo;
		}

		void OnGameStart()
		{
			Ui.CloseWindow();

			DiscordService.UpdateStatus(DiscordState.PlayingCampaign);

			onStart();
		}

		bool disposed;
		protected override void Dispose(bool disposing)
		{
			if (disposing && !disposed)
			{
				disposed = true;
				Game.BeforeGameStart -= OnGameStart;
			}

			base.Dispose(disposing);
		}

		void CreateMissionGroup(string title, IEnumerable<MapPreview> previews, Action onExit)
		{
			var header = ScrollItemWidget.Setup(headerTemplate, () => false, () => { });
			header.Get<LabelWidget>("LABEL").GetText = () => title;
			missionList.AddChild(header);

			foreach (var preview in previews)
			{
				var item = ScrollItemWidget.Setup(template,
					() => selectedMap != null && selectedMap.Uid == preview.Uid,
					() => SelectMap(preview),
					() => StartMissionClicked(onExit));

				var label = item.Get<LabelWithTooltipWidget>("TITLE");
				var missionTitle = CampaignProgressTracker.GetMapTileWithoutNumber(preview.Title);

				if (campaignProgress.ContainsKey(missionTitle))
				{
					var difficulty = campaignProgress[missionTitle].Difficulty;
					if (difficulty != null)
					{
						var stars = item.Get<ImageWidget>("COMPLETION_DIFFICULTY");
						stars.IsVisible = () => true;
						stars.GetImageName = () => difficulty;
					}

					var tick = item.Get<ImageWidget>("COMPLETION_ICON");
					tick.GetImageName = () => "complete";
				}

				WidgetUtils.TruncateLabelToTooltip(label, preview.Title);

				missionList.AddChild(item);
			}
		}

		void SelectMap(MapPreview preview)
		{
			selectedMap = preview;

			var briefingVideo = "";
			var briefingVideoVisible = false;

			var infoVideo = "";
			var infoVideoVisible = false;

			panel = PanelType.MissionInfo;

			new Thread(() =>
			{
				var missionData = preview.WorldActorInfo.TraitInfoOrDefault<MissionDataInfo>();
				if (missionData != null)
				{
					briefingVideo = missionData.BriefingVideo;
					briefingVideoVisible = briefingVideo != null;

					infoVideo = missionData.BackgroundVideo;
					infoVideoVisible = infoVideo != null;

					var briefingText = missionData.Briefing?.Replace("\\n", "\n");
					var missionTitle = CampaignProgressTracker.GetMapTileWithoutNumber(preview.Title);

					if (campaignProgress.ContainsKey(missionTitle))
					{
						var missionProgress = campaignProgress[missionTitle];
						var difficultyCompleted = missionProgress.Difficulty != null ? char.ToUpper(missionProgress.Difficulty[0]) + missionProgress.Difficulty.Substring(1) : null;
						var dateCompleted = missionProgress.DateCompleted.ToString();
						var completionTime = missionProgress.Time;
						briefingText = $"[{dateCompleted} - Completed in {completionTime}{(difficultyCompleted != null ? $" on {difficultyCompleted}" : "")}]\n\n---------\n\n" + briefingText;
					}

					var briefing = WidgetUtils.WrapText(briefingText, description.Bounds.Width, descriptionFont);
					var height = descriptionFont.Measure(briefing).Y;
					Game.RunAfterTick(() =>
					{
						if (preview == selectedMap)
						{
							description.Text = briefing;
							description.Bounds.Height = height;
							descriptionPanel.Layout.AdjustChildren();
						}
					});
				}
			}).Start();

			startBriefingVideoButton.IsVisible = () => briefingVideoVisible && playingVideo != PlayingVideo.Briefing;
			startBriefingVideoButton.OnClick = () => PlayVideo(videoPlayer, briefingVideo, PlayingVideo.Briefing);

			startInfoVideoButton.IsVisible = () => infoVideoVisible && playingVideo != PlayingVideo.Info;
			startInfoVideoButton.OnClick = () => PlayVideo(videoPlayer, infoVideo, PlayingVideo.Info);

			descriptionPanel.ScrollToTop();

			RebuildOptions();
		}

		void RebuildOptions()
		{
			if (selectedMap == null || selectedMap.WorldActorInfo == null)
				return;

			LoadSavedOptions();

			// missionOptions.Clear();
			optionsContainer.RemoveChildren();

			var allOptions = selectedMap.PlayerActorInfo.TraitInfos<ILobbyOptions>()
					.Concat(selectedMap.WorldActorInfo.TraitInfos<ILobbyOptions>())
					.SelectMany(t => t.LobbyOptions(selectedMap))
					.Where(o => o.IsVisible)
					.OrderBy(o => o.DisplayOrder).ToArray();

			Widget row = null;
			var checkboxColumns = new Queue<CheckboxWidget>();
			var dropdownColumns = new Queue<DropDownButtonWidget>();

			var yOffset = 0;
			foreach (var option in allOptions.Where(o => o is LobbyBooleanOption))
			{
				if (!missionOptions.ContainsKey(option.Id) || !( new[] { "True", "False" }.Contains(missionOptions[option.Id])))
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
					if (missionOptions[option.Id] == "True")
						missionOptions[option.Id] = "False";
					else
						missionOptions[option.Id] = "True";

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
					ScrollItemWidget SetupItem(KeyValuePair<string, string> c, ScrollItemWidget template)
					{
						bool IsSelected() => missionOptions[option.Id] == c.Key;
						void OnClick()
						{
							missionOptions[option.Id] = c.Key;
							SaveOptions();
						}

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
		}

		float cachedSoundVolume;
		float cachedMusicVolume;
		void MuteSounds()
		{
			cachedSoundVolume = Game.Sound.SoundVolume;
			cachedMusicVolume = Game.Sound.MusicVolume;
			Game.Sound.SoundVolume = Game.Sound.MusicVolume = 0;
		}

		void UnMuteSounds()
		{
			if (cachedSoundVolume > 0)
				Game.Sound.SoundVolume = cachedSoundVolume;

			if (cachedMusicVolume > 0)
				Game.Sound.MusicVolume = cachedMusicVolume;
		}

		void PlayVideo(VideoPlayerWidget player, string video, PlayingVideo pv, Action onComplete = null)
		{
			if (!modData.DefaultFileSystem.Exists(video))
			{
				ConfirmationDialogs.ButtonPrompt(modData,
					title: NoVideoTitle,
					text: NoVideoPrompt,
					cancelText: NoVideoCancel,
					onCancel: () => { });
			}
			else
			{
				StopVideo(player);

				playingVideo = pv;
				player.LoadAndPlay(video);

				if (player.Video == null)
				{
					StopVideo(player);

					ConfirmationDialogs.ButtonPrompt(modData,
						title: CantPlayTitle,
						text: CantPlayPrompt,
						cancelText: CantPlayCancel,
						onCancel: () => { });
				}
				else
				{
					// video playback runs asynchronously
					player.PlayThen(() =>
					{
						StopVideo(player);
						onComplete?.Invoke();
					});

					// Mute other distracting sounds
					MuteSounds();
				}
			}
		}

		void StopVideo(VideoPlayerWidget player)
		{
			if (playingVideo == PlayingVideo.None)
				return;

			UnMuteSounds();
			player.Stop();
			playingVideo = PlayingVideo.None;
		}

		void StartMissionClicked(Action onExit)
		{
			StopVideo(videoPlayer);

			// If selected mission becomes unavailable, exit MissionBrowser to refresh
			var map = modData.MapCache.GetUpdatedMap(selectedMap.Uid);
			if (map == null)
			{
				Game.Disconnect();
				Ui.CloseWindow();
				onExit();
				return;
			}

			selectedMap = modData.MapCache[map];
			var orders = new List<Order>();

			foreach (var option in missionOptions)
				orders.Add(Order.Command($"option {option.Key} {option.Value}"));

			orders.Add(Order.Command($"state {Session.ClientState.Ready}"));

			var missionData = selectedMap.WorldActorInfo.TraitInfoOrDefault<MissionDataInfo>();
			if (missionData != null && missionData.StartVideo != null && modData.DefaultFileSystem.Exists(missionData.StartVideo))
			{
				var fsPlayer = fullscreenVideoPlayer.Get<VideoPlayerWidget>("PLAYER");
				fullscreenVideoPlayer.Visible = true;
				PlayVideo(fsPlayer, missionData.StartVideo, PlayingVideo.GameStart,
					() => Game.CreateAndStartLocalServer(selectedMap.Uid, orders));
			}
			else
				Game.CreateAndStartLocalServer(selectedMap.Uid, orders);
		}

		void LoadSavedOptions()
		{
			if (!File.Exists(savedOptionsFilePath))
				return;

			try
			{
				var savedOptionsFileContents = File.ReadAllText(savedOptionsFilePath);
				var savedOptions = JsonConvert.DeserializeObject<Dictionary<string, string>>(savedOptionsFileContents);

				foreach(KeyValuePair<string, string> option in savedOptions)
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
