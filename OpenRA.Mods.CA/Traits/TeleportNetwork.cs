#region Copyright & License Information
/*
 * Copyright 2019-2020 The OpenHV Developers (see CREDITS)
 * This file is part of OpenHV, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	static class TeleportNetworkExts
	{
		public static bool IsValidTeleportNetworkUser(this Actor network, Actor user)
		{
			var trait = network.TraitOrDefault<TeleportNetwork>();
			if (trait == null)
				return false;

			if (trait.Tnm.Count < 2)
				return false;

			var exit = network.TraitOrDefault<TeleportNetworkPrimaryExit>();
			if (exit != null && exit.IsPrimary)
				return false;

			return network.Owner.RelationshipWith(user.Owner).HasFlag(trait.Info.ValidStances);
		}
	}

	[Desc("This actor can teleport actors to another actor with the same trait.")]
	public class TeleportNetworkInfo : TraitInfo, IRulesetLoaded
	{
		[FieldLoader.Require]
		[Desc("Type of TeleportNetwork that pairs up, in order for it to work.")]
		public string Type;

		[Desc("Stances requirement that targeted TeleportNetwork has to meet in order to teleport units.")]
		public PlayerRelationship ValidStances = PlayerRelationship.Ally;

		[Desc("Time in ticks to wait for the teleporter to charge up.")]
		public int Delay = 20;

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (!rules.Actors["player"].TraitInfos<TeleportNetworkManagerInfo>().Any(q => Type == q.Type))
				throw new YamlException("Can't find a TeleportNetworkManager with Type '{0}'".F(Type));
		}

		public override object Create(ActorInitializer init) { return new TeleportNetwork(this); }
	}

	// The teleport network does nothing. The actor teleports itself, upon entering.
	public class TeleportNetwork : INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyOwnerChanged
	{
		public TeleportNetworkInfo Info;
		public TeleportNetworkManager Tnm { get; private set; }

		public TeleportNetwork(TeleportNetworkInfo info)
		{
			Info = info;
		}

		void IncreaseTeleportNetworkCount(Actor self)
		{
			if (Tnm.Count == 0)
			{
				var primary = self.TraitOrDefault<TeleportNetworkPrimaryExit>();

				if (primary != null)
					primary.SetPrimary(self);
			}

			Tnm.Count++;
		}

		void DecreaseTeleportNetworkCount(Actor self)
		{
			Tnm.Count--;

			if (self.IsPrimaryTeleportNetworkExit())
			{
				var actors = self.World.ActorsWithTrait<TeleportNetworkPrimaryExit>()
				.Where(a => a.Actor.Owner == self.Owner && a.Actor != self);

				if (!actors.Any())
					Tnm.PrimaryActor = null;
				else
				{
					var primary = actors.First().Actor;
					primary.Trait<TeleportNetworkPrimaryExit>().SetPrimary(primary);
				}
			}
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			Tnm = self.Owner.PlayerActor.TraitsImplementing<TeleportNetworkManager>().First(x => x.Type == Info.Type);
			IncreaseTeleportNetworkCount(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			DecreaseTeleportNetworkCount(self);
			IncreaseTeleportNetworkCount(self);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			DecreaseTeleportNetworkCount(self);
		}
	}
}
