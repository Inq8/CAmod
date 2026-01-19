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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public enum FactionMatchType { None, Actor, Player, PlayerOrActor }

	[Desc("Creates a proxy actor on creation for each allied player.")]
	class CreateProxyActorForAlliesInfo : ConditionalTraitInfo
	{
		[ActorReference]
		public readonly string Proxy = null;

		[Desc("If set, will prefix the current actor's name to get the name of the proxy actor.")]
		public readonly string ProxyNamePrefix = null;

		[Desc("If true, spawn at the same location.")]
		public readonly bool UseLocation = false;

		[Desc("If true, spawn at the same position.")]
		public readonly bool UseCenterPosition = false;

		[Desc("If true, the spawned actor is destroyed if the parent actor dies, is sold, or is captured.")]
		public readonly bool LinkedToParent = false;

		[Desc("If true, only create for allies with the same faction.")]
		public readonly FactionMatchType RequireSameFactionAs = FactionMatchType.None;

		[Desc("If true, only create the proxy if ally faction matches a faction listed as valid for the actor.")]
		public readonly bool RequireValidFaction = false;

		[Desc("If true, only creates proxy when owner is a playable (human) player.")]
		public readonly bool RequiresPlayableOwner = false;

		public override object Create(ActorInitializer init) { return new CreateProxyActorForAllies(init, this); }
	}

	class CreateProxyActorForAllies : ConditionalTrait<CreateProxyActorForAlliesInfo>, INotifyCreated, INotifyKilled, INotifyOwnerChanged, INotifySold, INotifyTransform
	{
		readonly Actor self;
		readonly CreateProxyActorForAlliesInfo info;
		readonly Dictionary<Player, Actor> spawnedActorsByPlayer;
		HashSet<Actor> spawnedActors;
		readonly string actorFaction;
		readonly string proxyActorName;
		readonly HashSet<string> validFactions;

		public CreateProxyActorForAllies(ActorInitializer init, CreateProxyActorForAlliesInfo info)
			: base(info)
		{
			this.info = info;
			self = init.Self;
			spawnedActors = new HashSet<Actor>();
			spawnedActorsByPlayer = new Dictionary<Player, Actor>();
			actorFaction = init.GetValue<FactionInit, string>(self.Owner.Faction.InternalName);

			if (!string.IsNullOrEmpty(info.ProxyNamePrefix))
				proxyActorName = info.ProxyNamePrefix + self.Info.Name;
			else
				proxyActorName = info.Proxy;

			if (info.RequireValidFaction)
            {
				var validFactionsInfo = self.Info.TraitInfoOrDefault<ValidFactionsInfo>();
				if (validFactionsInfo != null)
				{
					validFactions = new HashSet<string>(validFactionsInfo.Factions);
				}
            }
		}

		protected override void TraitEnabled(Actor self)
		{
			CreateProxies();
		}

		protected override void TraitDisabled(Actor self)
		{
			DestroyProxies();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			DestroyProxies();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			DestroyProxies();
			CreateProxies();
		}

		void INotifySold.Selling(Actor self) {}

		void INotifySold.Sold(Actor self)
		{
			DestroyProxies();
		}

		void INotifyTransform.AfterTransform(Actor toActor)
		{
			DestroyProxies();
		}

		void INotifyTransform.BeforeTransform(Actor fromActor) {}

		void INotifyTransform.OnTransform(Actor self) {}

		void CreateProxies()
		{
			if (IsTraitDisabled)
				return;

			if (info.RequiresPlayableOwner && !self.Owner.Playable)
				return;

			if (self.World.Map.Rules.Actors.GetValueOrDefault(proxyActorName) == null)
				return;

			var allies = self.World.Players.Where(p => p.IsAlliedWith(self.Owner)
				&& p != self.Owner
				&& p.Playable);

			switch (info.RequireSameFactionAs)
			{
				case FactionMatchType.Actor:
					allies = allies.Where(p => p.Faction.InternalName == actorFaction);
					break;
				case FactionMatchType.Player:
					allies = allies.Where(p => p.Faction.InternalName == self.Owner.Faction.InternalName);
					break;
				case FactionMatchType.PlayerOrActor:
					allies = allies.Where(p => p.Faction.InternalName == self.Owner.Faction.InternalName || p.Faction.InternalName == actorFaction);
					break;
			}

			if (info.RequireValidFaction)
			{
				allies = allies.Where(p => validFactions == null || validFactions.Contains(p.Faction.InternalName));
			}

			foreach (var ally in allies)
			{
				var td = new TypeDictionary
				{
					new FactionInit(actorFaction),
					new OwnerInit(ally)
				};

				if (info.UseCenterPosition)
				{
					td.Add(new CenterPositionInit(self.CenterPosition));

					if (info.UseLocation)
						td.Add(new LocationInit(self.World.Map.CellContaining(self.CenterPosition)));
				}
				else if (info.UseLocation)
					td.Add(new LocationInit(self.Location));

				var allyPlayer = ally;
				self.World.AddFrameEndTask(w =>
				{
					var actor = w.CreateActor(proxyActorName, td);
					spawnedActors.Add(actor);
					spawnedActorsByPlayer[allyPlayer] = actor;
				});
			}
		}

		void DestroyProxies()
		{
			spawnedActorsByPlayer.Clear();
			foreach (var a in spawnedActors)
			{
				if (!a.IsDead)
					a.Dispose();
			}
		}

		public Actor GetProxyForPlayer(Player player)
		{
			if (spawnedActorsByPlayer.TryGetValue(player, out var actor) && actor != null && !actor.IsDead && actor.IsInWorld)
				return actor;

			return null;
		}
	}
}
