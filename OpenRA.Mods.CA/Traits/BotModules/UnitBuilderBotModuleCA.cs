#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
		public readonly HashSet<string> UnitQueues = new HashSet<string> { "Vehicle", "Infantry", "Plane", "Ship", "Aircraft" };

		[Desc("What units to the AI should build.", "What relative share of the total army must be this type of unit.")]
		public readonly Dictionary<string, int> UnitsToBuild = null;

		[Desc("What units should the AI have a maximum limit to train.")]
		public readonly Dictionary<string, int> UnitLimits = null;

		[Desc("When should the AI start train specific units.")]
		public readonly Dictionary<string, int> UnitDelays = null;

		[Desc("Minimum duration between building a specific unit.")]
		public readonly Dictionary<string, int> UnitIntervals = null;

		[Desc("How often should the unit builder check to build more units")]
		public readonly int UnitBuilderInterval = 0;

		[Desc("Mininum amount of credits in reserve for the Unit Builder to be active.")]
		public readonly int UnitBuilderMinCredits = 2000;

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

		public override object Create(ActorInitializer init) { return new UnitBuilderBotModuleCA(init.Self, this); }
	}

	public class UnitBuilderBotModuleCA : ConditionalTrait<UnitBuilderBotModuleCAInfo>, IBotTick, IBotNotifyIdleBaseUnits, IBotRequestUnitProduction, IGameSaveTraitData, IBotAircraftBuilder
	{
		public const int FeedbackTime = 30; // ticks; = a bit over 1s. must be >= netlag.

		readonly World world;
		readonly Player player;

		readonly List<string> queuedBuildRequests = new List<string>();
		readonly Dictionary<string, int> activeUnitIntervals = new Dictionary<string, int>();

		IBotRequestPauseUnitProduction[] requestPause;
		int idleUnitCount;

		int ticks;

		public UnitBuilderBotModuleCA(Actor self, UnitBuilderBotModuleCAInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		protected override void Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query player traits from self, which refers
			// for bot modules always to the Player actor.
			requestPause = self.TraitsImplementing<IBotRequestPauseUnitProduction>().ToArray();
		}

		void IBotNotifyIdleBaseUnits.UpdatedIdleBaseUnits(List<Actor> idleUnits)
		{
			idleUnitCount = idleUnits.Count;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (requestPause.Any(rp => rp.PauseUnitProduction))
				return;

			foreach (KeyValuePair<string, int> i in activeUnitIntervals.ToList())
			{
				activeUnitIntervals[i.Key]--;
				if (activeUnitIntervals[i.Key] <= 0)
					activeUnitIntervals.Remove(i.Key);
			}

			ticks++;

			if (ticks % (FeedbackTime + Info.UnitBuilderInterval) == 0)
			{
				var buildRequest = queuedBuildRequests.FirstOrDefault();
				if (buildRequest != null)
				{
					BuildUnit(bot, buildRequest);
					queuedBuildRequests.Remove(buildRequest);
				}

				if (player.PlayerActor.Trait<PlayerResources>().Cash + player.PlayerActor.Trait<PlayerResources>().Resources >= Info.UnitBuilderMinCredits)
					foreach (var q in Info.UnitQueues)
						BuildUnit(bot, q, idleUnitCount < Info.IdleBaseUnitsMaximum);
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

		void BuildUnit(IBot bot, string category, bool buildRandom)
		{
			// Pick a free queue
			var queue = AIUtils.FindQueues(player, category).FirstOrDefault(q => !q.AllQueued().Any());
			if (queue == null)
				return;

			var unit = buildRandom ?
				ChooseRandomUnitToBuild(queue) :
				ChooseUnitToBuild(queue);

			if (unit == null)
				return;

			var name = unit.Name;

			if (!ShouldBuild(name, false))
				return;

			SetUnitInterval(name);
			bot.QueueOrder(Order.StartProduction(queue.Actor, name, 1));
		}

		// In cases where we want to build a specific unit but don't know the queue name (because there's more than one possibility)
		void BuildUnit(IBot bot, string name)
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
				queue = AIUtils.FindQueues(player, pq).FirstOrDefault(q => !q.AllQueued().Any());
				if (queue != null)
					break;
			}

			if (queue != null)
			{
				SetUnitInterval(name);
				bot.QueueOrder(Order.StartProduction(queue.Actor, name, 1));
				AIUtils.BotDebug("AI: {0} decided to build {1} (external request)", queue.Actor.Owner, name);
			}
		}

		void SetUnitInterval(string name)
		{
			if (!Info.UnitIntervals.ContainsKey(name))
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

		ActorInfo ChooseRandomUnitToBuild(ProductionQueue queue)
		{
			var buildableThings = queue.BuildableItems();
			if (!buildableThings.Any())
				return null;

			var unit = buildableThings.Random(world.LocalRandom);
			return CanBuildMoreOfAircraft(unit) ? unit : null;
		}

		ActorInfo ChooseUnitToBuild(ProductionQueue queue)
		{
			var buildableThings = queue.BuildableItems();
			if (!buildableThings.Any())
				return null;

			var myUnits = player.World
				.ActorsHavingTrait<IPositionable>()
				.Where(a => a.Owner == player)
				.Select(a => a.Info.Name).ToList();

			foreach (var unit in Info.UnitsToBuild.Shuffle(world.LocalRandom))
				if (buildableThings.Any(b => b.Name == unit.Key))
					if (myUnits.Count(a => a == unit.Key) * 100 < unit.Value * myUnits.Count)
						if (CanBuildMoreOfAircraft(world.Map.Rules.Actors[unit.Key]))
							return world.Map.Rules.Actors[unit.Key];

			return null;
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

			var limit = Info.MaxAircraft;
			var currentCount = 0;

			if (Info.MaintainAirSuperiority)
			{
				var numAirToAirUnits = AIUtils.GetActorsWithTrait<Aircraft>(player.World).Count(a => a.Owner == player && Info.AirToAirUnits.Contains(a.Info.Name));

				if (Info.AirToAirUnits.Contains(actorInfo.Name))
				{
					currentCount = numAirToAirUnits;
					var numFriendlyAirToAirUnits = player.World.Actors.Count(a => a.Owner.RelationshipWith(player) == PlayerRelationship.Ally && Info.AirToAirUnits.Contains(a.Info.Name));
					var numEnemyAirThreatUnits = player.World.Actors.Count(a => a.Owner.RelationshipWith(player) == PlayerRelationship.Enemy && Info.AirThreatUnits.Contains(a.Info.Name));
					limit = Math.Max(numEnemyAirThreatUnits - numFriendlyAirToAirUnits + 1, limit);

					if (Info.MaxAirSuperiority > 0)
						limit = Math.Min(Info.MaxAirSuperiority, limit);
				}
				else
					currentCount = AIUtils.GetActorsWithTrait<Aircraft>(player.World).Count(a => a.Owner == player && a.Info.HasTraitInfo<BuildableInfo>()) - numAirToAirUnits;
			}
			else
				currentCount = AIUtils.GetActorsWithTrait<Aircraft>(player.World).Count(a => a.Owner == player && a.Info.HasTraitInfo<BuildableInfo>());

			return currentCount < limit;
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			return new List<MiniYamlNode>()
			{
				new MiniYamlNode("QueuedBuildRequests", FieldSaver.FormatValue(queuedBuildRequests.ToArray())),
				new MiniYamlNode("IdleUnitCount", FieldSaver.FormatValue(idleUnitCount))
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, List<MiniYamlNode> data)
		{
			if (self.World.IsReplay)
				return;

			var queuedBuildRequestsNode = data.FirstOrDefault(n => n.Key == "QueuedBuildRequests");
			if (queuedBuildRequestsNode != null)
			{
				queuedBuildRequests.Clear();
				queuedBuildRequests.AddRange(FieldLoader.GetValue<string[]>("QueuedBuildRequests", queuedBuildRequestsNode.Value.Value));
			}

			var idleUnitCountNode = data.FirstOrDefault(n => n.Key == "IdleUnitCount");
			if (idleUnitCountNode != null)
				idleUnitCount = FieldLoader.GetValue<int>("IdleUnitCount", idleUnitCountNode.Value.Value);
		}
	}
}
