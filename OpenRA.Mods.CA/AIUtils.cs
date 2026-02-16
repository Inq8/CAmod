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
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA
{
	public enum BuildingType { Building, Defense, Refinery, Fragile }

	public enum WaterCheck { NotChecked, EnoughWater, NotEnoughWater, DontCheck }

	public static class AIUtils
	{
		public static bool IsAreaAvailable<T>(World world, Player player, Map map, int radius, HashSet<string> terrainTypes)
		{
			var cells = world.ActorsHavingTrait<T>().Where(a => a.Owner == player);

			// TODO: Properly check building foundation rather than 3x3 area.
			return cells.Select(a => map.FindTilesInCircle(a.Location, radius)
				.Count(c => map.Contains(c) && terrainTypes.Contains(map.GetTerrainInfo(c).Type) &&
					Util.AdjacentCells(world, Target.FromCell(world, c))
						.All(ac => terrainTypes.Contains(map.GetTerrainInfo(ac).Type))))
							.Any(availableCells => availableCells > 0);
		}

		public static IEnumerable<ProductionQueue> FindQueues(Player player, string category)
		{
			return player.World.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == player && a.Trait.Info.Type == category && a.Trait.Enabled)
				.Select(a => a.Trait);
		}

		public static ILookup<string, ProductionQueue> FindQueuesByCategory(Player player)
		{
			return player.World.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == player && a.Trait.Enabled)
				.Select(a => a.Trait)
				.ToLookup(pq => pq.Info.Type);
		}

		public static ILookup<string, ProductionQueue> FindQueuesByCategory(IEnumerable<Player> players)
		{
			var player = players.First();

			return player.World.ActorsWithTrait<ProductionQueue>()
				.Where(a => players.Contains(a.Actor.Owner) && a.Trait.Enabled)
				.Select(a => a.Trait)
				.ToLookup(pq => pq.Info.Type);
		}

		public static IEnumerable<Actor> GetActorsWithTrait<T>(World world)
		{
			return world.ActorsHavingTrait<T>();
		}

		public static int CountActorsWithTrait<T>(string actorName, Player owner)
		{
			return GetActorsWithTrait<T>(owner.World).Count(a => a.Owner == owner && a.Info.Name == actorName);
		}

		public static int CountActorByCommonName(HashSet<string> commonNames, Player owner)
		{
			return owner.World.Actors.Count(a => !a.IsDead && a.Owner == owner &&
				commonNames.Contains(a.Info.Name));
		}

		public static int CountActorByCommonName<TTraitInfo>(
			ActorIndex.OwnerAndNamesAndTrait<TTraitInfo> actorIndex) where TTraitInfo : ITraitInfoInterface
		{
			return actorIndex.Actors.Count(a => !a.IsDead);
		}

		public static int CountBuildingByCommonName(HashSet<string> buildings, Player owner)
		{
			return GetActorsWithTrait<Building>(owner.World)
				.Count(a => a.Owner == owner && buildings.Contains(a.Info.Name));
		}

		public static List<Actor> FindEnemiesByCommonName(HashSet<string> commonNames, Player player)
		{
			return player.World.Actors.Where(a => !a.IsDead && player.RelationshipWith(a.Owner) == PlayerRelationship.Enemy &&
				commonNames.Contains(a.Info.Name)).ToList();
		}

		public static ActorInfo GetInfoByCommonName(HashSet<string> names, Player owner)
		{
			return owner.World.Map.Rules.Actors.Where(k => names.Contains(k.Key)).Random(owner.World.LocalRandom).Value;
		}

		public static void BotDebug(string s, params object[] args)
		{
			if (Game.Settings.Debug.BotDebug)
				TextNotificationsManager.Debug(s, args);
		}

		// temporarily added here (they were removed from WorldUtils)
		public static Actor ClosestTo(this IEnumerable<Actor> actors, WPos pos)
		{
			return actors.MinByOrDefault(a => (a.CenterPosition - pos).LengthSquared);
		}

		public static WPos PositionClosestTo_Old(this IEnumerable<WPos> positions, WPos pos)
		{
			return positions.MinByOrDefault(p => (p - pos).LengthSquared);
		}

		// Finds multiple distinct routes between source and target for a given locomotor.
		public static List<List<CPos>> FindDistinctRoutes(
			World world,
			Locomotor locomotor,
			CPos source,
			CPos target,
			int maxRoutes = 3,
			BlockedByActor check = BlockedByActor.None)
		{
			var routes = new List<List<CPos>>();

			// Get the PathFinder trait from the world actor
			var pathFinder = world.WorldActor.TraitOrDefault<PathFinder>();
			if (pathFinder == null)
				return routes;

			// Get the abstract graph data from the hierarchical pathfinder
			var (abstractGraph, abstractDomains) = pathFinder.GetOverlayDataForLocomotor(locomotor, check);
			if (abstractGraph == null || abstractDomains == null)
				return routes;

			// Map source and target to their abstract nodes
			var sourceAbstract = FindAbstractNodeForCell(source, abstractGraph, abstractDomains);
			var targetAbstract = FindAbstractNodeForCell(target, abstractGraph, abstractDomains);

			if (sourceAbstract == null || targetAbstract == null)
				return routes;

			// Check if source and target are in the same domain (connected)
			if (!abstractDomains.TryGetValue(sourceAbstract.Value, out var sourceDomain) ||
				!abstractDomains.TryGetValue(targetAbstract.Value, out var targetDomain) ||
				sourceDomain != targetDomain)
				return routes;

			// Track nodes that have been used in previous routes
			var excludedNodes = new HashSet<CPos>();

			for (var i = 0; i < maxRoutes; i++)
			{
				var route = FindAbstractPath(sourceAbstract.Value, targetAbstract.Value, abstractGraph, excludedNodes);
				if (route == null || route.Count == 0)
					break; // No more valid routes available

				routes.Add(route);

				// Exclude all nodes in this route (except source and target) for future routes
				foreach (var node in route)
				{
					if (node != sourceAbstract.Value
						&& node != targetAbstract.Value
						&& node != route.ElementAtOrDefault(1)
						&& node != route.ElementAtOrDefault(2)
						&& node != route.ElementAtOrDefault(route.Count - 2))
						excludedNodes.Add(node);
				}
			}

			return routes;
		}

		// Finds the abstract node that corresponds to a given cell position.
		static CPos? FindAbstractNodeForCell(
			CPos cell,
			IReadOnlyDictionary<CPos, List<GraphConnection>> abstractGraph,
			IReadOnlyDictionary<CPos, uint> abstractDomains)
		{
			// If the cell itself is an abstract node, return it
			if (abstractDomains.ContainsKey(cell))
				return cell;

			// Otherwise, find the nearest abstract node
			CPos? nearestNode = null;
			var minDistSq = int.MaxValue;

			foreach (var abstractNode in abstractDomains.Keys)
			{
				var distSq = (abstractNode - cell).LengthSquared;
				if (distSq < minDistSq)
				{
					minDistSq = distSq;
					nearestNode = abstractNode;
				}
			}

			return nearestNode;
		}

		// Performs A* pathfinding on the abstract graph to find a route from source to target,
		// avoiding any nodes in the excluded set.
		static List<CPos> FindAbstractPath(
			CPos source,
			CPos target,
			IReadOnlyDictionary<CPos, List<GraphConnection>> abstractGraph,
			HashSet<CPos> excludedNodes)
		{
			var openSet = new Dictionary<CPos, PathNode>();
			var closedSet = new HashSet<CPos>();

			// Initialize the starting node
			var startNode = new PathNode
			{
				Position = source,
				CostFromStart = 0,
				EstimatedTotalCost = Heuristic(source, target),
				Parent = null
			};

			openSet[source] = startNode;

			while (openSet.Count > 0)
			{
				// Find the node in openSet with the lowest estimated total cost
				var current = openSet.Values.MinBy(n => n.EstimatedTotalCost);
				if (current == null)
					break;

				// Check if we've reached the target
				if (current.Position == target)
					return ReconstructPath(current);

				openSet.Remove(current.Position);
				closedSet.Add(current.Position);

				// Explore neighbors
				if (!abstractGraph.TryGetValue(current.Position, out var connections))
					continue;

				foreach (var connection in connections)
				{
					var neighbor = connection.Destination;

					// Skip if already evaluated or excluded (unless it's the target)
					if (closedSet.Contains(neighbor) || (excludedNodes.Contains(neighbor) && neighbor != target))
						continue;

					var newCost = current.CostFromStart + connection.Cost;

					// If this is a new node or we found a better path to it
					if (!openSet.TryGetValue(neighbor, out var neighborNode))
					{
						neighborNode = new PathNode
						{
							Position = neighbor,
							CostFromStart = newCost,
							EstimatedTotalCost = newCost + Heuristic(neighbor, target),
							Parent = current
						};
						openSet[neighbor] = neighborNode;
					}
					else if (newCost < neighborNode.CostFromStart)
					{
						neighborNode.CostFromStart = newCost;
						neighborNode.EstimatedTotalCost = newCost + Heuristic(neighbor, target);
						neighborNode.Parent = current;
					}
				}
			}

			// No path found
			return null;
		}

		// Reconstructs the path by following parent pointers from target back to source.
		static List<CPos> ReconstructPath(PathNode targetNode)
		{
			var path = new List<CPos>();
			var current = targetNode;

			while (current != null)
			{
				path.Add(current.Position);
				current = current.Parent;
			}

			path.Reverse();
			return path;
		}

		// Simple heuristic function for A* pathfinding (Manhattan distance).
		static int Heuristic(CPos from, CPos to)
		{
			var delta = from - to;
			return Math.Abs(delta.X) + Math.Abs(delta.Y);
		}

		// Helper class to represent a node in the A* pathfinding algorithm.
		class PathNode
		{
			public CPos Position;
			public int CostFromStart;
			public int EstimatedTotalCost;
			public PathNode Parent;
		}

		public static bool PathExistsForLocomotor(
			World world,
			Locomotor locomotor,
			CPos source,
			CPos target)
		{
			var pathFinder = world.WorldActor.TraitOrDefault<IPathFinder>();

			if (pathFinder == null)
				return false;

			return pathFinder.PathExistsForLocomotor(locomotor, source, target);
		}
	}
}
