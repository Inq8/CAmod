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
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public enum RevealPlayerFactionType
	{
		OnGameStart,
		OnSelection,
		OnSeen
	}

	[Desc("Attached to the world actor to track which players are revealed (for displaying their real faction in scores panel).")]
	[TraitLocation(SystemActors.Player)]
	public class RevealedFactionsManagerInfo : TraitInfo
	{
		[Desc("When to reveal player factions blind pick is disabled.")]
		public RevealPlayerFactionType RevealCondition { get; set; } = RevealPlayerFactionType.OnGameStart;

		[Desc("When to reveal player factions when blind pick is enabled.")]
		public RevealPlayerFactionType BlindPickRevealCondition { get; set; } = RevealPlayerFactionType.OnSeen;

		[Desc("How often to scan for newly visible actors.")]
		public int ScanInterval { get; set; } = 25;

		public override object Create(ActorInitializer init) { return new RevealedFactionsManager(init.Self, this); }
	}

	public class RevealedFactionsManager : IWorldLoaded, INotifySelection, ITick
	{
		readonly Actor self;
		readonly World world;
		readonly RevealedFactionsManagerInfo info;
		readonly RevealPlayerFactionType revealCondition;
		public HashSet<Player> PlayersWithRandomFaction { get; }
		public HashSet<Player> RevealedPlayers { get; }
		bool allPlayersRevealed;
		bool isValidPlayer;
		int rescanInterval;

		public RevealedFactionsManager(Actor self, RevealedFactionsManagerInfo info)
		{
			this.self = self;
			world = self.World;
			RevealedPlayers = new HashSet<Player>();
			PlayersWithRandomFaction = new HashSet<Player>();
			rescanInterval = 0;
			allPlayersRevealed = false;
			isValidPlayer = false;
			this.info = info;

			var blindPickDefault = world.Map.Rules.Actors[SystemActors.World].TraitInfo<MapOptionsInfo>().BlindPickModeCheckboxEnabled;
			var blindPickEnabled = world.LobbyInfo.GlobalSettings.OptionOrDefault("blindpick", blindPickDefault);
			revealCondition = blindPickEnabled ? info.BlindPickRevealCondition : info.RevealCondition;
		}

		public bool RevealOnGameStart => revealCondition == RevealPlayerFactionType.OnGameStart;

		public void RevealPlayer(Player player)
		{
			RevealedPlayers.Add(player);
		}

		public bool IsRevealed(Player player)
		{
			return RevealedPlayers.Contains(player);
		}

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer worldRenderer)
		{
			// Disable for AI and neutral players (creeps) and for spectators
			isValidPlayer = !(self.Owner.IsBot || !self.Owner.Playable || self.Owner.PlayerReference.Spectating);

			if (!isValidPlayer)
				return;

			var randomFactions = world.WorldActor.Info.TraitInfos<FactionInfo>()
				.Where(f => f.Selectable && f.RandomFactionMembers.Count > 0)
				.Select(f => f.InternalName)
				.ToList();

			foreach (var player in world.Players.Where(p => p != self.Owner
				&& p.Playable
				&& !p.NonCombatant
				&& randomFactions.Contains(p.DisplayFaction.InternalName)
				&& self.Owner.RelationshipWith(p) != PlayerRelationship.Ally))
				PlayersWithRandomFaction.Add(player);

			if (PlayersWithRandomFaction.Count == 0)
				allPlayersRevealed = true;
		}

		void INotifySelection.SelectionChanged()
		{
			if (revealCondition != RevealPlayerFactionType.OnSelection)
				return;

			if (!isValidPlayer)
				return;

			if (self.Owner != world.LocalPlayer)
				return;

			var players = world.Selection.Actors
				.Where(a => a.IsInWorld
					&& IsRevealed(a.Owner)
					&& PlayersWithRandomFaction.Contains(a.Owner))
				.Select(a => a.Owner);

			// for each selected player
			foreach (var player in players)
			{
				// for self and allies, reveal the player
				foreach (var alliedPlayer in world.Players.Where(p => p == self.Owner || self.Owner.RelationshipWith(p) == PlayerRelationship.Ally))
					alliedPlayer.PlayerActor.TraitOrDefault<RevealedFactionsManager>()?.RevealPlayer(player);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (revealCondition != RevealPlayerFactionType.OnSeen)
				return;

			if (!isValidPlayer)
				return;

			if (allPlayersRevealed)
				return;

			rescanInterval--;

			if (rescanInterval > 0)
				return;

			foreach (var actor in self.World.ActorsWithTrait<RevealsFaction>())
			{
				if (!PlayersWithRandomFaction.Contains(actor.Actor.Owner) || IsRevealed(actor.Actor.Owner))
					continue;

				// We don't want notifications for allied actors or actors disguised as such
				if (actor.Actor.AppearsFriendlyTo(self))
					continue;

				if (actor.Actor.IsDead || !actor.Actor.IsInWorld)
					continue;

				// The actor is not currently visible
				if (!actor.Actor.CanBeViewedByPlayer(self.Owner))
					continue;

				RevealPlayer(actor.Actor.Owner);
			}

			if (PlayersWithRandomFaction.All(p => IsRevealed(p)))
				allPlayersRevealed = true;

			rescanInterval = info.ScanInterval;
		}
	}
}
