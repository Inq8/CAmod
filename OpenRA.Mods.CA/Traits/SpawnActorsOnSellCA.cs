#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Spawn new actors when sold.")]
	public class SpawnActorsOnSellCAInfo : ConditionalTraitInfo
	{
		public readonly int ValuePercent = 40;
		public readonly int MinHpPercent = 30;

		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actor types to spawn on sell, amount and type based on ValuePercent. Be sure to use lowercase.")]
		public readonly string[] ActorTypes = null;

		[ActorReference]
		[Desc("Actors to spawn on sell. Be sure to use lowercase.")]
		public readonly string[] GuaranteedActorTypes = Array.Empty<string>();

		[Desc("Spawns actors only if the selling player's faction is in this list. " +
			"Leave empty to allow all factions by default.")]
		public readonly HashSet<string> Factions = new();

		[Desc("If true, the actors defined by GuaranteedActorTypes will not spawn if there isn't enough value.")]
		public readonly bool GuaranteedActorsLimitedByValue = false;

		public override object Create(ActorInitializer init) { return new SpawnActorsOnSellCA(init.Self, this); }
	}

	public class SpawnActorsOnSellCA : ConditionalTrait<SpawnActorsOnSellCAInfo>, INotifySold
	{
		readonly bool correctFaction;

		public SpawnActorsOnSellCA(Actor self, SpawnActorsOnSellCAInfo info)
			: base(info)
		{
			var factionsList = info.Factions;
			correctFaction = factionsList.Count == 0 || factionsList.Contains(self.Owner.Faction.InternalName);
		}

		void INotifySold.Selling(Actor self) { }

		void Emit(Actor self)
		{
			if (IsTraitDisabled || !correctFaction)
				return;

			var buildingInfo = self.Info.TraitInfoOrDefault<BuildingInfo>();
			if (buildingInfo == null)
				return;

			var csv = self.Info.TraitInfoOrDefault<CustomSellValueInfo>();
			var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();
			var cost = csv?.Value ?? valued?.Cost ?? 0;

			var health = self.TraitOrDefault<IHealth>();
			var dudesValue = Info.ValuePercent * cost / 100;
			if (health != null)
			{
				// Cast to long to avoid overflow when multiplying by the health
				if (100L * health.HP >= Info.MinHpPercent * (long)health.MaxHP)
					dudesValue = (int)((long)health.HP * dudesValue / health.MaxHP);
				else
					dudesValue = 0;
			}

			var eligibleLocations = buildingInfo.Tiles(self.Location).ToList();

			if (eligibleLocations.Count == 0)
				return;

			if (Info.GuaranteedActorTypes.Length > 0)
			{
				var guaranteedActorTypes = Info.GuaranteedActorTypes.Select(a =>
				{
					var av = self.World.Map.Rules.Actors[a].TraitInfoOrDefault<ValuedInfo>();
					return new
					{
						Name = a,
						Cost = av?.Cost ?? 0
					};
				}).ToList();

				while (eligibleLocations.Count > 0 && guaranteedActorTypes.Count > 0 && (!Info.GuaranteedActorsLimitedByValue || guaranteedActorTypes.Any(a => a.Cost <= dudesValue)))
				{
					var at = guaranteedActorTypes.Where(a => !Info.GuaranteedActorsLimitedByValue || a.Cost <= dudesValue).Random(self.World.SharedRandom);
					var loc = eligibleLocations.Random(self.World.SharedRandom);

					eligibleLocations.Remove(loc);
					guaranteedActorTypes.Remove(at);
					dudesValue -= at.Cost;

					self.World.AddFrameEndTask(w => w.CreateActor(at.Name, new TypeDictionary
					{
						new LocationInit(loc),
						new OwnerInit(self.Owner),
					}));
				}

				if (eligibleLocations.Count == 0)
					return;
			}

			var actorTypes = Info.ActorTypes.Select(a =>
			{
				var av = self.World.Map.Rules.Actors[a].TraitInfoOrDefault<ValuedInfo>();
				return new
				{
					Name = a,
					Cost = av?.Cost ?? 0
				};
			}).ToList();

			while (eligibleLocations.Count > 0 && actorTypes.Any(a => a.Cost <= dudesValue))
			{
				var at = actorTypes.Where(a => a.Cost <= dudesValue).Random(self.World.SharedRandom);
				var loc = eligibleLocations.Random(self.World.SharedRandom);

				eligibleLocations.Remove(loc);
				dudesValue -= at.Cost;

				self.World.AddFrameEndTask(w => w.CreateActor(at.Name, new TypeDictionary
				{
					new LocationInit(loc),
					new OwnerInit(self.Owner),
				}));
			}
		}

		void INotifySold.Sold(Actor self) { Emit(self); }
	}
}
