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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	class GameInfoStatsCALogic : ChromeLogic
	{
		[TranslationReference]
		static readonly string Unmute = "unmute";

		[TranslationReference]
		static readonly string Mute = "mute";

		[TranslationReference]
		static readonly string Accomplished = "accomplished";

		[TranslationReference]
		static readonly string Failed = "failed";

		[TranslationReference]
		static readonly string InProgress = "in-progress";

		[TranslationReference("team")]
		static readonly string TeamNumber = "team-number";

		[TranslationReference]
		static readonly string NoTeam = "no-team";

		[TranslationReference]
		static readonly string Spectators = "spectators";

		[TranslationReference]
		static readonly string Gone = "gone";

		[TranslationReference("player")]
		static readonly string KickTitle = "kick-title";

		[TranslationReference]
		static readonly string KickPrompt = "kick-prompt";

		[TranslationReference]
		static readonly string KickAccept = "kick-accept";

		[ObjectCreator.UseCtor]
		public GameInfoStatsCALogic(Widget widget, ModData modData, World world, OrderManager orderManager, WorldRenderer worldRenderer, Action<bool> hideMenu)
		{
			var player = world.LocalPlayer;
			var playerPanel = widget.Get<ScrollPanelWidget>("PLAYER_LIST");
			var statsHeader = widget.Get("STATS_HEADERS");

			if (player != null && !player.NonCombatant)
			{
				var checkbox = widget.Get<CheckboxWidget>("STATS_CHECKBOX");
				var statusLabel = widget.Get<LabelWidget>("STATS_STATUS");

				checkbox.IsChecked = () => player.WinState != WinState.Undefined;
				checkbox.GetCheckmark = () => player.WinState == WinState.Won ? "tick" : "cross";

				if (player.HasObjectives)
				{
					var mo = player.PlayerActor.Trait<MissionObjectives>();
					checkbox.GetText = () => mo.Objectives[0].Description;
				}

				var failed = modData.Translation.GetString(Failed);
				var inProgress = modData.Translation.GetString(InProgress);
				var accomplished = modData.Translation.GetString(Accomplished);
				statusLabel.GetText = () => player.WinState == WinState.Won ? accomplished :
					player.WinState == WinState.Lost ? failed : inProgress;
				statusLabel.GetColor = () => player.WinState == WinState.Won ? Color.LimeGreen :
					player.WinState == WinState.Lost ? Color.Red : Color.White;
			}
			else
			{
				// Expand the stats window to cover the hidden objectives
				var objectiveGroup = widget.Get("OBJECTIVE");

				objectiveGroup.Visible = false;
				statsHeader.Bounds.Y -= objectiveGroup.Bounds.Height;
				playerPanel.Bounds.Y -= objectiveGroup.Bounds.Height;
				playerPanel.Bounds.Height += objectiveGroup.Bounds.Height;
			}

			if (!orderManager.LobbyInfo.Clients.Any(c => !c.IsBot && c.Index != orderManager.LocalClient?.Index && c.State != Session.ClientState.Disconnected))
				statsHeader.Get<LabelWidget>("ACTIONS").Visible = false;

			var teamTemplate = playerPanel.Get<ScrollItemWidget>("TEAM_TEMPLATE");
			var playerTemplate = playerPanel.Get("PLAYER_TEMPLATE");
			var spectatorTemplate = playerPanel.Get("SPECTATOR_TEMPLATE");
			var unmuteTooltip = modData.Translation.GetString(Unmute);
			var muteTooltip = modData.Translation.GetString(Mute);
			playerPanel.RemoveChildren();

			var teams = world.Players.Where(p => !p.NonCombatant && p.Playable)
				.Select(p => (Player: p, PlayerStatistics: p.PlayerActor.TraitOrDefault<PlayerStatistics>()))
				.OrderByDescending(p => p.PlayerStatistics?.Experience ?? 0)
				.GroupBy(p => (world.LobbyInfo.ClientWithIndex(p.Player.ClientIndex) ?? new Session.Client()).Team)
				.OrderByDescending(g => g.Sum(gg => gg.PlayerStatistics?.Experience ?? 0));

			var revealedPlayersManager = player != null ? player.World.WorldActor.TraitOrDefault<RevealedPlayersManager>() : null;

			foreach (var t in teams)
			{
				if (teams.Count() > 1)
				{
					var teamHeader = ScrollItemWidget.Setup(teamTemplate, () => false, () => { });
					var team = t.Key > 0
						? modData.Translation.GetString(TeamNumber, Translation.Arguments("team", t.Key))
						: modData.Translation.GetString(NoTeam);
					teamHeader.Get<LabelWidget>("TEAM").GetText = () => team;
					var teamRating = teamHeader.Get<LabelWidget>("TEAM_SCORE");
					var scoreCache = new CachedTransform<int, string>(s => s.ToString());
					var teamMemberScores = t.Select(tt => tt.PlayerStatistics).Where(s => s != null).ToArray().Select(s => s.Experience);
					teamRating.GetText = () => scoreCache.Update(teamMemberScores.Sum());

					playerPanel.AddChild(teamHeader);
				}

				foreach (var p in t.ToList())
				{
					var pp = p.Player;
					var client = world.LobbyInfo.ClientWithIndex(pp.ClientIndex);
					var item = playerTemplate.Clone();
					LobbyUtils.SetupProfileWidget(item, client, orderManager, worldRenderer);

					var nameLabel = item.Get<LabelWidget>("NAME");
					WidgetUtils.BindPlayerNameAndStatus(nameLabel, pp);
					nameLabel.GetColor = () => pp.Color;

					// Begin custom CA section for revealing random factions
					var isRevealed = revealedPlayersManager != null && revealedPlayersManager.IsRevealed(pp);
					var factionAndLabel = item.Get<ContainerWithTooltipWidget>("FACTIONFLAGANDLABEL");
					var realFactionVisible = player == null || player.RelationshipWith(pp) == PlayerRelationship.Ally || player.WinState != WinState.Undefined || isRevealed;

					var flag = factionAndLabel.Get<ImageWidget>("FACTIONFLAG");
					flag.GetImageCollection = () => "flags";

					var tooltipTextSplit = SplitOnFirstToken(pp.Faction.Description);
					var factionName = pp.DisplayFaction.Name;

					if (realFactionVisible)
					{
						flag.GetImageName = () => pp.Faction.InternalName;
						factionName = pp.Faction.Name != factionName ? $"{factionName} ({pp.Faction.Name})" : pp.Faction.Name;
						factionAndLabel.GetTooltipText = () => tooltipTextSplit.First;
						factionAndLabel.GetTooltipDesc = () => tooltipTextSplit.Second;
					}
					else
					{
						flag.GetImageName = () => pp.DisplayFaction.InternalName;
						factionAndLabel.GetTooltipText = () => factionName;
						factionAndLabel.GetTooltipDesc = () => "Select a unit belonging to this player\\nto reveal their faction/sub-faction.";
					}

					var factionLabel = item.Get<LabelWidget>("FACTION");
					factionLabel.GetText = () => factionName;
					// End custom CA section for revealing random factions

					var scoreCache = new CachedTransform<int, string>(s => s.ToString());
					item.Get<LabelWidget>("SCORE").GetText = () => scoreCache.Update(p.PlayerStatistics?.Experience ?? 0);

					var muteCheckbox = item.Get<CheckboxWidget>("MUTE");
					muteCheckbox.IsChecked = () => TextNotificationsManager.MutedPlayers[pp.ClientIndex];
					muteCheckbox.OnClick = () => TextNotificationsManager.MutedPlayers[pp.ClientIndex] ^= true;
					muteCheckbox.IsVisible = () => !pp.IsBot && client.State != Session.ClientState.Disconnected && pp.ClientIndex != orderManager.LocalClient?.Index;
					muteCheckbox.GetTooltipText = () => muteCheckbox.IsChecked() ? unmuteTooltip : muteTooltip;

					playerPanel.AddChild(item);
				}
			}

			var spectators = orderManager.LobbyInfo.Clients.Where(c => c.IsObserver).ToList();
			if (spectators.Count > 0)
			{
				var spectatorHeader = ScrollItemWidget.Setup(teamTemplate, () => false, () => { });
				var spectatorTeam = modData.Translation.GetString(Spectators);
				spectatorHeader.Get<LabelWidget>("TEAM").GetText = () => spectatorTeam;

				playerPanel.AddChild(spectatorHeader);

				foreach (var client in spectators)
				{
					var item = spectatorTemplate.Clone();
					LobbyUtils.SetupProfileWidget(item, client, orderManager, worldRenderer);

					var nameLabel = item.Get<LabelWidget>("NAME");
					var nameFont = Game.Renderer.Fonts[nameLabel.Font];

					var suffixLength = new CachedTransform<string, int>(s => nameFont.Measure(s).X);
					var name = new CachedTransform<(string Name, string Suffix), string>(c =>
						WidgetUtils.TruncateText(c.Name, nameLabel.Bounds.Width - suffixLength.Update(c.Suffix), nameFont) + c.Suffix);

					nameLabel.GetText = () =>
					{
						var suffix = client.State == Session.ClientState.Disconnected ? $" ({modData.Translation.GetString(Gone)})" : "";
						return name.Update((client.Name, suffix));
					};

					var kickButton = item.Get<ButtonWidget>("KICK");
					kickButton.IsVisible = () => Game.IsHost && client.Index != orderManager.LocalClient?.Index && client.State != Session.ClientState.Disconnected;
					kickButton.OnClick = () =>
					{
						hideMenu(true);
						ConfirmationDialogs.ButtonPrompt(modData,
							title: KickTitle,
							titleArguments: Translation.Arguments("player", client.Name),
							text: KickPrompt,
							onConfirm: () =>
							{
								orderManager.IssueOrder(Order.Command($"kick {client.Index} {false}"));
								hideMenu(false);
							},
							onCancel: () => hideMenu(false),
							confirmText: KickAccept);
					};

					var muteCheckbox = item.Get<CheckboxWidget>("MUTE");
					muteCheckbox.IsChecked = () => TextNotificationsManager.MutedPlayers[client.Index];
					muteCheckbox.OnClick = () => TextNotificationsManager.MutedPlayers[client.Index] ^= true;
					muteCheckbox.IsVisible = () => !client.IsBot && client.State != Session.ClientState.Disconnected && client.Index != orderManager.LocalClient?.Index;
					muteCheckbox.GetTooltipText = () => muteCheckbox.IsChecked() ? unmuteTooltip : muteTooltip;

					playerPanel.AddChild(item);
				}
			}
		}

		/// <summary>Splits a string into two parts on the first instance of a given token.</summary>
		static (string First, string Second) SplitOnFirstToken(string input, string token = "\\n")
		{
			if (string.IsNullOrEmpty(input))
				return (null, null);

			var split = input.IndexOf(token, StringComparison.Ordinal);
			var first = split > 0 ? input.Substring(0, split) : input;
			var second = split > 0 ? input.Substring(split + token.Length) : null;
			return (first, second);
		}
	}
}
