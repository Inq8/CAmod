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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class UnitComposition
	{
		[Desc("Production queues the composition is applicable to. Omit to apply to all queues")]
		public readonly string[] UnitQueues = Array.Empty<string>();

		[Desc("What units to the AI should build.", "What relative share of the total army must be this type of unit.")]
		public readonly Dictionary<string, int> UnitsToBuild = new();

		[Desc("If true, the AI can use this as a baseline/fallback composition (MaxDuration and MaxProducedValue will be ignored).")]
		public readonly bool IsBaseline = false;

		[Desc("If non-zero, the maximum number of ticks the composition should remain active before reverting to baseline.")]
		public readonly int MaxDuration = 0;

		[Desc("If non-zero, the maximum value to produce before reverting to baseline.")]
		public readonly int MaxProducedValue = 0;

		[Desc("Minimum ticks into the game before compositon can be selected.")]
		public readonly int MinTime = 0;

		[Desc("Maximum ticks into the game the compositon can be selected.")]
		public readonly int MaxTime = 0;

		[Desc("Maximum ticks into the game the compositon can be selected.")]
		public readonly int MinInterval = 7500;

		[Desc("List of prerequisites that must be met for the composition to be chosen.")]
		public readonly string[] Prerequisites = Array.Empty<string>();

		// Populated at runtime for quick lookup of unit prerequisites by queue when evaluating compositions
		public Dictionary<string, Dictionary<string, string[]>> UnitPrerequisitesByQueue { get; set; }
		public string Id { get; set; }

		/// <summary>
		/// This constructor is used solely for documentation generation.
		/// </summary>
		public UnitComposition() { }

		public UnitComposition(MiniYaml content)
		{
			FieldLoader.Load(this, content);
		}
	}

	[TraitLocation(SystemActors.World)]
	public class UnitCompositionsBotModuleInfo : ConditionalTraitInfo
	{
		[FieldLoader.LoadUsing(nameof(LoadUnitCompositions))]
		public readonly List<UnitComposition> UnitCompositions = new();

		public override object Create(ActorInitializer init) { return new UnitCompositionsBotModule(init.Self, this); }

		static object LoadUnitCompositions(MiniYaml yaml)
		{
			var retList = new List<UnitComposition>();
			foreach (var node in yaml.Nodes.Where(n => n.Key.StartsWith("Composition", StringComparison.Ordinal)))
			{
				var unitComposition = new UnitComposition(node.Value);
				// Use the YAML key as a stable id so runtime state (e.g. MinInterval tracking) can persist.
				unitComposition.Id = node.Key;
				retList.Add(unitComposition);
			}

			return retList;
		}
	}

	public class UnitCompositionsBotModule : ConditionalTrait<UnitCompositionsBotModuleInfo>
	{
		public List<UnitComposition> UnitCompositions { get; }
		public Dictionary<string, string[]> UnitPrerequisites { get; } = new();
		public Dictionary<string, string[]> UnitQueues { get; } = new();
		public Dictionary<string, int> UnitCosts { get; } = new();

		public UnitCompositionsBotModule(Actor self, UnitCompositionsBotModuleInfo info)
			: base(info)
		{
			UnitCompositions = info.UnitCompositions;

			foreach (var unit in UnitCompositions.SelectMany(c => c.UnitsToBuild.Keys).Distinct())
			{
				var unitInfo = self.World.Map.Rules.Actors[unit];
				if (unitInfo == null)
					throw new Exception($"Unit {unit} in UnitCompositionsBotModule does not exist.");

				var buildable = unitInfo.TraitInfoOrDefault<BuildableInfo>();
				if (buildable == null)
					throw new Exception($"Unit {unit} in UnitCompositionsBotModule does not have Buildable trait, and thus cannot be built by the bot.");

				UnitQueues[unit] = buildable.Queue.ToArray();
				UnitPrerequisites[unit] = buildable.Prerequisites;
				var valued = unitInfo.TraitInfoOrDefault<ValuedInfo>();
				UnitCosts[unit] = valued?.Cost ?? 0;
			}

			// Populate the UnitPrerequisitesByQueue for each composition, so the bot doesn't have to look them up repeatedly
			foreach (var composition in UnitCompositions)
			{
				// Fallback for compositions that were not assigned an id during loading.
				if (string.IsNullOrEmpty(composition.Id))
					composition.Id = Guid.NewGuid().ToString();

				composition.UnitPrerequisitesByQueue = new Dictionary<string, Dictionary<string, string[]>>();

				foreach (var unit in composition.UnitsToBuild.Keys)
				{
					if (!UnitQueues.TryGetValue(unit, out var queues))
						continue;

					foreach (var queue in queues)
					{
						if (!composition.UnitPrerequisitesByQueue.TryGetValue(queue, out var unitPrerequisites))
						{
							unitPrerequisites = new Dictionary<string, string[]>();
							composition.UnitPrerequisitesByQueue[queue] = unitPrerequisites;
						}

						unitPrerequisites[unit] = UnitPrerequisites[unit];
					}
				}
			}
		}
	}
}
