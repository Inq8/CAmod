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
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Controls AI unit production.")]
	public class UnitBuilderBotModuleCAInfo : ConditionalTraitInfo
	{
		// TODO: Investigate whether this might the (or at least one) reason why bots occasionally get into a state of doing nothing.
		// Reason: If this is less than SquadSize, the bot might get stuck between not producing more units due to this,
		// but also not creating squads since there aren't enough idle units.
		[Desc("Only produce units as long as there are less than this amount of units idling inside the base.")]
		public readonly int IdleBaseUnitsMaximum = 12;

		[Desc("Production queues AI uses for producing units.")]
		public readonly string[] UnitQueues = { "VehicleSQ", "InfantrySQ", "AircraftSQ", "ShipSQ", "VehicleMQ", "InfantryMQ", "AircraftMQ", "ShipMQ" };

		[Desc("Fallback unit shares used when CompositionsBotModule is missing or empty.")]
		public readonly Dictionary<string, int> UnitsToBuild = null;

		[Desc("What units should the AI have a maximum limit to train.")]
		public readonly Dictionary<string, int> UnitLimits = null;

		[Desc("When should the AI start train specific units.")]
		public readonly Dictionary<string, int> UnitDelays = null;

		[Desc("Minimum duration between building a specific unit.")]
		public readonly Dictionary<string, int> UnitIntervals = null;

		[Desc("How often should the unit builder check to build more units")]
		public readonly int UnitBuilderInterval = 0;

		[Desc("Only queue construction of a new unit when above this requirement.")]
		public readonly int ProductionMinCashRequirement = 2000;

		[Desc("Only queue construction of a new unit when above this requirement.")]
		public readonly int MaximiseProductionCashRequirement = 10000;

		[Desc("Maximum number of aircraft AI can build.",
			"If MaintainAirSuperiority is true this only applies to units not listed in AirToAirUnits.")]
		public readonly int MaxAircraft = 4;

		[Desc("If true, will always attempt to match the number of enemy air threats.")]
		public readonly bool MaintainAirSuperiority = false;

		[Desc("If MaintainAirSuperiority is true and this is non-zero,",
			"sets an upper limit for the number of air superiority aircraft.")]
		public readonly int MaxAirSuperiority = 0;

		[Desc("List of actor types to be used for air superiority.")]
		public readonly HashSet<string> AirToAirUnits = new HashSet<string>();

		[Desc("List of actor types to measure against for air superiority.")]
		public readonly HashSet<string> AirThreatUnits = new HashSet<string>();

		[Desc("If true, the bot will use compositions defined in the UnitCompositionsBotModule to determine what units to build.",
			"If false, the bot will ignore compositions and just use UnitsToBuild.")]
		public readonly bool UseCompositions = true;

		[Desc("Minimum ticks before selecting a new non-baseline composition.")]
		public readonly int MinCompositionSelectInterval = 750;

		[Desc("Maximum ticks before selecting a new non-baseline composition.")]
		public readonly int MaxCompositionSelectInterval = 7500;

		public override object Create(ActorInitializer init) { return new UnitBuilderBotModuleCA(init.Self, this); }
	}

	public class UnitBuilderBotModuleCA : ConditionalTrait<UnitBuilderBotModuleCAInfo>, IBotTick, IBotNotifyIdleBaseUnits,
		IBotRequestUnitProduction, IGameSaveTraitData, IBotAircraftBuilder, INotifyActorDisposing
	{
		public const int FeedbackTime = 30; // ticks; = a bit over 1s. must be >= netlag.

		readonly World world;
		readonly Player player;
		UnitComposition baselineComposition = null;
		UnitComposition activeComposition = null;
		int activeCompositionProducedValue = 0;
		int activeCompositionSelectedTick;
		int nextCompositionSelectTick;
		readonly Dictionary<string, int> compositionLastUsedTickById = new();

		readonly List<string> queuedBuildRequests = new();
		readonly Dictionary<string, int> activeUnitIntervals = new();
		ActorIndex.OwnerAndNames unitsToBuild;

		UnitCompositionsBotModule compositionsModule;
		List<UnitComposition> possibleActiveCompositions = null;
		TechTree techTree;

		IBotRequestPauseUnitProduction[] requestPause;
		int idleUnitCount;
		int currentQueueIndex = 0;
		PlayerResources playerResources;

		int ticks;

		public UnitBuilderBotModuleCA(Actor self, UnitBuilderBotModuleCAInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		protected override void Created(Actor self)
		{
			requestPause = self.Owner.PlayerActor.TraitsImplementing<IBotRequestPauseUnitProduction>().ToArray();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			techTree = self.Owner.PlayerActor.TraitOrDefault<TechTree>();
			compositionsModule = Info.UseCompositions ? self.World.WorldActor.TraitOrDefault<UnitCompositionsBotModule>() : null;

			var referencedUnitTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (Info.UnitsToBuild != null)
				referencedUnitTypes.UnionWith(Info.UnitsToBuild.Keys);

			if (compositionsModule != null)
			{
				baselineComposition = compositionsModule != null ? compositionsModule.UnitCompositions.FirstOrDefault(c => c != null && c.IsBaseline) : null;

				foreach (var c in compositionsModule.UnitCompositions)
					if (c?.UnitsToBuild != null)
						referencedUnitTypes.UnionWith(c.UnitsToBuild.Keys);

				possibleActiveCompositions = compositionsModule.UnitCompositions.Where(c => c != null
					&& !c.IsBaseline
					&& (c.EnabledChance == 100 || self.World.LocalRandom.Next(100) < c.EnabledChance)).ToList();

				nextCompositionSelectTick = GetNextCompositionSelectTick();
			}

			unitsToBuild = new ActorIndex.OwnerAndNames(world, referencedUnitTypes, player);
		}

		void IBotNotifyIdleBaseUnits.UpdatedIdleBaseUnits(List<Actor> idleUnits)
		{
			idleUnitCount = idleUnits.Count;
		}

		void IBotTick.BotTick(IBot bot)
		{
			// Decrement any active unit intervals, removing any that reach zero
			foreach (var i in activeUnitIntervals.ToList())
			{
				activeUnitIntervals[i.Key]--;
				if (activeUnitIntervals[i.Key] <= 0)
					activeUnitIntervals.Remove(i.Key);
			}

			// PERF: We shouldn't be queueing new units when we're low on cash
			if (playerResources.GetCashAndResources() < Info.ProductionMinCashRequirement || requestPause.Any(rp => rp.PauseUnitProduction))
				return;

			ticks++;

			if (ticks % (FeedbackTime + Info.UnitBuilderInterval) == 0)
			{
				UpdateComposition();

				ILookup<string, ProductionQueue> queuesByCategory = null;

				var buildRequest = queuedBuildRequests.FirstOrDefault();
				if (buildRequest != null)
				{
					queuesByCategory ??= AIUtils.FindQueuesByCategory(player);
					BuildUnit(bot, buildRequest, queuesByCategory);
					queuedBuildRequests.Remove(buildRequest);
				}

				if (idleUnitCount < Info.IdleBaseUnitsMaximum)
				{
					queuesByCategory ??= AIUtils.FindQueuesByCategory(player);
					for (var i = 0; i < Info.UnitQueues.Length; i++)
					{
						if (++currentQueueIndex >= Info.UnitQueues.Length)
							currentQueueIndex = 0;

						var category = Info.UnitQueues[currentQueueIndex];
						var queues = queuesByCategory[category].ToArray();
						if (queues.Length != 0)
						{
							// PERF: We tick only one type of valid queue at a time
							// if AI gets enough cash, it can fill all of its queues with enough ticks
							BuildRandomUnit(bot, queues);

							if (playerResources.GetCashAndResources() < Info.MaximiseProductionCashRequirement)
								break;
						}
					}
				}
			}
		}

		void IBotRequestUnitProduction.RequestUnitProduction(IBot bot, string requestedActor)
		{
			queuedBuildRequests.Add(requestedActor);
		}

		int IBotRequestUnitProduction.RequestedProductionCount(IBot bot, string requestedActor)
		{
			return queuedBuildRequests.Count(r => r == requestedActor);
		}

		void BuildRandomUnit(IBot bot, ProductionQueue[] queues)
		{
			if (!HasAnyUnitCompositionOrFallback())
				return;

			// Pick a free queue
			var queue = queues.FirstOrDefault(q => !q.AllQueued().Any());
			if (queue == null)
				return;

			var unit = ChooseRandomUnitToBuild(queue, false);
			if (unit == null)
			{
				RevertToBaselineComposition();
				return;
			}

			AddToActiveCompositionProducedValue(unit);

			SetUnitInterval(unit.Name);
			bot.QueueOrder(Order.StartProduction(queue.Actor, unit.Name, 1));
		}

		// In cases where we want to build a specific unit but don't know the queue name (because there's more than one possibility)
		void BuildUnit(IBot bot, string name, ILookup<string, ProductionQueue> queuesByCategory)
		{
			var actorInfo = world.Map.Rules.Actors[name];
			if (actorInfo == null)
				return;

			var buildableInfo = actorInfo.TraitInfoOrDefault<BuildableInfo>();
			if (buildableInfo == null)
				return;

			if (!ShouldBuild(name, true))
				return;

			ProductionQueue queue = null;
			foreach (var pq in buildableInfo.Queue)
			{
				queue = queuesByCategory[pq].FirstOrDefault(q => !q.AllQueued().Any());
				if (queue != null)
					break;
			}

			if (queue != null && queue.BuildableItems().Any(b => b.Name == name))
			{
				SetUnitInterval(name);
				bot.QueueOrder(Order.StartProduction(queue.Actor, name, 1));
				AIUtils.BotDebug("AI: {0} decided to build {1} (external request)", queue.Actor.Owner, name);
			}
		}

		void SetUnitInterval(string name)
		{
			if (Info.UnitIntervals == null || !Info.UnitIntervals.ContainsKey(name))
				return;

			activeUnitIntervals[name] = Info.UnitIntervals[name];
		}

		bool ShouldBuild(string name, bool ignoreUnitsToBuild)
		{
			if (!ignoreUnitsToBuild && Info.UnitsToBuild != null && !Info.UnitsToBuild.ContainsKey(name))
				return false;

			if (Info.UnitDelays != null &&
				Info.UnitDelays.ContainsKey(name) &&
				Info.UnitDelays[name] > world.WorldTick)
				return false;

			if (Info.UnitIntervals != null &&
				Info.UnitIntervals.ContainsKey(name) &&
				activeUnitIntervals.ContainsKey(name))
				return false;

			if (Info.UnitLimits != null &&
				Info.UnitLimits.ContainsKey(name) &&
				world.Actors.Count(a => !a.IsDead && a.Owner == player && a.Info.Name == name) >= Info.UnitLimits[name])
				return false;

			return true;
		}

		ActorInfo ChooseRandomUnitToBuild(ProductionQueue queue, bool excludeLimited)
		{
			var unitsToBuildShares = GetUnitsToBuildForCategory(queue.Info.Type);
			if (unitsToBuildShares == null || unitsToBuildShares.Count == 0)
				return null;

			var buildableThings = queue.BuildableItems().Shuffle(world.LocalRandom).ToArray();
			if (buildableThings.Length == 0)
				return null;

			var allUnits = unitsToBuild != null ? unitsToBuild.Actors.Where(a => !a.IsDead).ToArray() : world.Actors.Where(a => !a.IsDead && a.Owner == player).ToArray();

			ActorInfo desiredUnit = null;
			var desiredError = int.MaxValue;
			foreach (var unit in buildableThings)
			{
				if (!unitsToBuildShares.TryGetValue(unit.Name, out var share) ||
					(Info.UnitDelays != null && Info.UnitDelays.TryGetValue(unit.Name, out var delay) && delay > world.WorldTick))
					continue;

				if (Info.UnitIntervals != null &&
					Info.UnitIntervals.ContainsKey(unit.Name) &&
					activeUnitIntervals.ContainsKey(unit.Name))
					continue;

				var unitCount = allUnits.Count(a => a.Info.Name == unit.Name);
				if (!excludeLimited && Info.UnitLimits != null && Info.UnitLimits.TryGetValue(unit.Name, out var count) && unitCount >= count)
					continue;

				var error = allUnits.Length > 0 ? unitCount * 100 / allUnits.Length - share : -1;
				if (error < 0)
					return CanBuildMoreOfAircraft(unit) ? unit : null;

				if (error < desiredError)
				{
					desiredError = error;
					desiredUnit = unit;
				}
			}

			return desiredUnit != null ? (CanBuildMoreOfAircraft(desiredUnit) ? desiredUnit : null) : null;
		}

		bool HasAnyUnitCompositionOrFallback()
		{
			var hasCompositions = compositionsModule != null && compositionsModule.UnitCompositions.Count != 0;
			var hasFallback = Info.UnitsToBuild != null && Info.UnitsToBuild.Count != 0;
			return hasCompositions || hasFallback;
		}

		Dictionary<string, int> GetUnitsToBuildForCategory(string queueCategory)
		{
			if (compositionsModule == null || compositionsModule.UnitCompositions.Count == 0)
				return Info.UnitsToBuild;

			if (activeComposition != null && CompositionAppliesToCategory(activeComposition, queueCategory))
				return activeComposition.UnitsToBuild;

			if (baselineComposition != null && CompositionAppliesToCategory(baselineComposition, queueCategory))
				return baselineComposition.UnitsToBuild;

			return Info.UnitsToBuild;
		}

		void UpdateComposition()
		{
			if (compositionsModule == null || compositionsModule.UnitCompositions.Count == 0)
				return;

			// If a non-baseline composition is active, then keep it until it expires.
			if (activeComposition != null)
			{
				var exceededDuration = activeComposition.MaxDuration > 0 && world.WorldTick - activeCompositionSelectedTick >= activeComposition.MaxDuration;
				var exceededValue = activeComposition.MaxProducedValue > 0 && activeCompositionProducedValue >= activeComposition.MaxProducedValue;

				if (exceededDuration || exceededValue)
					RevertToBaselineComposition();
			}
			else if (world.WorldTick >= nextCompositionSelectTick)
			{
				var newActiveComposition = ChooseActiveComposition();
				if (newActiveComposition != null)
				{
					activeComposition = newActiveComposition;
					activeCompositionProducedValue = 0;
					activeCompositionSelectedTick = world.WorldTick;
					if (!string.IsNullOrEmpty(activeComposition.Id))
						compositionLastUsedTickById[activeComposition.Id] = world.WorldTick;
				}
			}
		}

		void RevertToBaselineComposition()
		{
			activeComposition = null;
			activeCompositionProducedValue = 0;
			nextCompositionSelectTick = GetNextCompositionSelectTick();
		}

		UnitComposition ChooseActiveComposition()
		{
			if (compositionsModule == null || possibleActiveCompositions.Count == 0)
				return null;

			nextCompositionSelectTick = GetNextCompositionSelectTick();

			var playerQueues = AIUtils.FindQueuesByCategory(player);

			var candidates = possibleActiveCompositions
				.Where(c => c != null && !c.IsBaseline
					&& IsCompositionTimeValid(c)
					&& IsCompositionIntervalValid(c)
					&& AreCompositionPrerequisitesMet(c)
					&& CanProduceAnyUnitInCompositionForEachQueueCategory(c, playerQueues))
				.ToArray();

			return candidates.Length != 0 ? candidates.Random(world.LocalRandom) : null;
		}

		bool IsCompositionIntervalValid(UnitComposition composition)
		{
			if (composition == null)
				return false;

			if (composition.MinInterval <= 0)
				return true;

			if (string.IsNullOrEmpty(composition.Id))
				return true;

			if (!compositionLastUsedTickById.TryGetValue(composition.Id, out var lastTick))
				return true;

			return world.WorldTick - lastTick >= composition.MinInterval;
		}

		bool IsCompositionTimeValid(UnitComposition composition)
		{
			if (composition == null)
				return false;

			var tick = world.WorldTick;
			if (composition.MinTime > 0 && tick < composition.MinTime)
				return false;
			if (composition.MaxTime > 0 && tick > composition.MaxTime)
				return false;

			return true;
		}

		bool CanProduceAnyUnitInCompositionForQueueCategory(UnitComposition composition, string queueCategory)
		{
			if (composition == null || string.IsNullOrEmpty(queueCategory))
				return false;

			if (techTree == null)
				return true;

			var byQueue = composition.UnitPrerequisitesByQueue;
			if (byQueue == null || !byQueue.TryGetValue(queueCategory, out var unitPrereqs) || unitPrereqs == null || unitPrereqs.Count == 0)
				return false;

			foreach (var prereqs in unitPrereqs.Values)
			{
				if (prereqs == null || prereqs.Length == 0 || techTree.HasPrerequisites(prereqs))
					return true;
			}

			return false;
		}

		bool CanProduceAnyUnitInCompositionForEachQueueCategory(UnitComposition composition, ILookup<string, ProductionQueue> playerQueues)
		{
			if (composition == null)
				return false;

			var byQueue = composition.UnitPrerequisitesByQueue;
			if (byQueue == null || byQueue.Count == 0)
				return false;

			foreach (var queueCategory in byQueue.Keys)
			{
				if (!playerQueues.Contains(queueCategory))
					continue;

				if (!CanProduceAnyUnitInCompositionForQueueCategory(composition, queueCategory))
					return false;
			}

			return true;
		}

		bool CompositionAppliesToCategory(UnitComposition composition, string queueCategory)
		{
			if (composition.UnitQueues == null || composition.UnitQueues.Length == 0)
				return true;
			return composition.UnitQueues.Any(q => q.Equals(queueCategory, StringComparison.OrdinalIgnoreCase));
		}

		bool AreCompositionPrerequisitesMet(UnitComposition composition)
		{
			if (composition.Prerequisites == null || composition.Prerequisites.Length == 0)
				return true;
			return techTree == null || techTree.HasPrerequisites(composition.Prerequisites);
		}

		int GetNextCompositionSelectTick()
		{
			var min = Math.Max(0, Info.MinCompositionSelectInterval);
			var max = Math.Max(0, Info.MaxCompositionSelectInterval);

			if (min == 0 && max == 0)
				return int.MaxValue / 4;

			if (max < min)
				max = min;

			var interval = min == max ? min : world.LocalRandom.Next(min, max + 1);
			return world.WorldTick + interval;
		}

		void AddToActiveCompositionProducedValue(ActorInfo builtUnit)
		{
			compositionsModule.UnitCosts.TryGetValue(builtUnit.Name, out var unitCost);
			if (unitCost <= 0)
				return;

			activeCompositionProducedValue += unitCost;
		}

		bool IBotAircraftBuilder.CanBuildMoreOfAircraft(ActorInfo actorInfo)
		{
			return CanBuildMoreOfAircraft(actorInfo);
		}

		bool CanBuildMoreOfAircraft(ActorInfo actorInfo)
		{
			var attackAircraftInfo = actorInfo.TraitInfoOrDefault<AircraftInfo>();
			if (attackAircraftInfo == null)
				return true;

			int currentCount;
			var limit = Info.MaxAircraft;
			var isAirToAir = Info.AirToAirUnits.Contains(actorInfo.Name);

			if (Info.MaintainAirSuperiority && isAirToAir)
			{
				// Get all production queues to count queued aircraft (for allies as well)
				var queues = AIUtils.FindQueuesByCategory(player.World.Players.Where(p => p.IsAlliedWith(player)));

				// Count queued air-to-air units across all queues
				var queuedAirToAirCount = queues.SelectMany(g => g).SelectMany(q => q.AllQueued())
					.Count(item => Info.AirToAirUnits.Contains(item.Item));

				var friendlyAirToAirCount = player.World.Actors.Count(a => a.Owner.RelationshipWith(player) == PlayerRelationship.Ally
					&& Info.AirToAirUnits.Contains(a.Info.Name));

				var enemyAirThreatCount = player.World.Actors.Count(a => a.Owner.RelationshipWith(player) == PlayerRelationship.Enemy
					&& Info.AirThreatUnits.Contains(a.Info.Name));

				currentCount = friendlyAirToAirCount + queuedAirToAirCount;
				limit = Math.Max(enemyAirThreatCount + 1, limit);

				if (Info.MaxAirSuperiority > 0)
					limit = Math.Min(Info.MaxAirSuperiority, limit);
			}
			else
			{
				// Get all production queues to count queued aircraft
				var queues = AIUtils.FindQueuesByCategory(player);

				// Non air-to-air aircraft uses the standard aircraft limit
				if (Info.MaintainAirSuperiority)
				{
					var existingNonAirToAirCount = player.World.Actors.Count(a =>
						a.Owner == player &&
						a.Info.HasTraitInfo<AircraftInfo>() &&
						a.Info.HasTraitInfo<BuildableInfo>() &&
						!Info.AirToAirUnits.Contains(a.Info.Name));

					var queuedNonAirToAirCount = queues.SelectMany(g => g).SelectMany(q => q.AllQueued())
						.Count(item => !Info.AirToAirUnits.Contains(item.Item) &&
							world.Map.Rules.Actors[item.Item].HasTraitInfo<AircraftInfo>());

					currentCount = existingNonAirToAirCount + queuedNonAirToAirCount;
				}
				else
				{
					var existingAircraftCount = player.World.Actors.Count(a =>
						a.Owner == player &&
						a.Info.HasTraitInfo<AircraftInfo>() &&
						a.Info.HasTraitInfo<BuildableInfo>());

					// Count all queued aircraft
					var queuedAircraft = queues.SelectMany(g => g).SelectMany(q => q.AllQueued())
						.Count(item => world.Map.Rules.Actors[item.Item].HasTraitInfo<AircraftInfo>());

					currentCount = existingAircraftCount + queuedAircraft;
				}
			}

			return currentCount < limit;
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			return new List<MiniYamlNode>()
			{
				new MiniYamlNode("QueuedBuildRequests", FieldSaver.FormatValue(queuedBuildRequests.ToArray())),
				new MiniYamlNode("IdleUnitCount", FieldSaver.FormatValue(idleUnitCount)),
				new("CompositionLastUsed", "", compositionLastUsedTickById
					.Select(kvp => new MiniYamlNode(kvp.Key, FieldSaver.FormatValue(kvp.Value)))
					.ToList())
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			if (self.World.IsReplay)
				return;

			var queuedBuildRequestsNode = data.NodeWithKeyOrDefault("QueuedBuildRequests");
			if (queuedBuildRequestsNode != null)
			{
				queuedBuildRequests.Clear();
				queuedBuildRequests.AddRange(FieldLoader.GetValue<string[]>("QueuedBuildRequests", queuedBuildRequestsNode.Value.Value));
			}

			var idleUnitCountNode = data.NodeWithKeyOrDefault("IdleUnitCount");
			if (idleUnitCountNode != null)
				idleUnitCount = FieldLoader.GetValue<int>("IdleUnitCount", idleUnitCountNode.Value.Value);

			var compositionLastUsedNode = data.NodeWithKeyOrDefault("CompositionLastUsed");
			if (compositionLastUsedNode != null)
			{
				compositionLastUsedTickById.Clear();
				foreach (var n in compositionLastUsedNode.Value.Nodes)
					compositionLastUsedTickById[n.Key] = FieldLoader.GetValue<int>("CompositionLastUsed", n.Value.Value);
			}
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			unitsToBuild?.Dispose();
		}
	}
}
