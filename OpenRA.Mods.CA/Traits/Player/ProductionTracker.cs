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
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Keeps track of player's initial build order and units produced for observer stats.")]
	public class ProductionTrackerInfo : TraitInfo
	{
		[Desc("Maximum number of build order items to track.")]
		public readonly int MaxBuildOrderItems = 18;

		public override object Create(ActorInitializer init) { return new ProductionTracker(init.Self, this); }
	}

	public class ProductionTracker
	{
		readonly ProductionTrackerInfo info;
		List<ProductionTrackerBuildOrderItem> buildOrder;
		Dictionary<string, ProductionTrackerUnitValueItem> unitValues;
		int totalValue;
		public int BuildOrderCount => buildOrder.Count;
		public List<ProductionTrackerBuildOrderItem> BuildOrder => buildOrder;
		public Dictionary<string, ProductionTrackerUnitValueItem> UnitValues => unitValues;
		public int TotalValue => totalValue;
		readonly World world;

		public ProductionTracker(Actor self, ProductionTrackerInfo info)
		{
			this.info = info;
			buildOrder = new List<ProductionTrackerBuildOrderItem>();
			unitValues = new Dictionary<string, ProductionTrackerUnitValueItem>();
			totalValue = 0;
			world = self.World;
		}

		public void BuildingCreated(string type)
		{
			if (BuildOrderCount >= info.MaxBuildOrderItems)
				return;

			buildOrder.Add(new ProductionTrackerBuildOrderItem { Name = type, Tick = world.WorldTick });
		}

		public void UnitCreated(string type, int value)
		{
			totalValue += value;

			if (unitValues.ContainsKey(type))
			{
				unitValues[type].Value += value;
				unitValues[type].Count++;
			}
			else
				unitValues[type] = new ProductionTrackerUnitValueItem { Count = 1, Value = value };
		}
	}

	public class ProductionTrackerBuildOrderItem
	{
		public string Name;
		public int Tick;
	}

	public class ProductionTrackerUnitValueItem
	{
		public int Value;
		public int Count;
	}
}
