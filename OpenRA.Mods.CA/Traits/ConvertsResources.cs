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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Gradually converts resources within a given radius.")]
	sealed class ConvertsResourcesInfo : ConditionalTraitInfo
	{
		public readonly string[] ConvertFrom = { "Ore", "Tiberium", "Gems", "BlueTiberium" };
		public readonly int Interval = 75;
		public readonly string ConvertTo = "BlackTiberium";
		public readonly WDist Range = WDist.FromCells(5);
		public readonly int Amount = 1;

		public override object Create(ActorInitializer init) { return new ConvertsResources(init.Self, this); }
	}

	sealed class ConvertsResources : ConditionalTrait<ConvertsResourcesInfo>, ITick
	{
		readonly ConvertsResourcesInfo info;
		readonly IResourceLayer resourceLayer;
		readonly Dictionary<CPos, int> cellsToConvert;

		public ConvertsResources(Actor self, ConvertsResourcesInfo info)
			: base(info)
		{
			this.info = info;
			resourceLayer = self.World.WorldActor.Trait<IResourceLayer>();
			cellsToConvert = new Dictionary<CPos, int>();
			ticks = info.Interval;
		}

		int ticks;

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (--ticks <= 0)
			{
				Convert(self);
				ticks = info.Interval;
			}
		}

		void Convert(Actor self)
		{
			// Find cells with convertible resources within range
			var cells = self.World.Map.FindTilesInCircle(self.Location, info.Range.Length / 1024)
				.Where(c =>
				{
					var resource = resourceLayer.GetResource(c);
					return resource.Type != null &&
						   info.ConvertFrom.Contains(resource.Type) &&
						   (resource.Density > 0 || cellsToConvert.ContainsKey(c));
				});

			if (cells.Any())
				Remove(cells.Random(self.World.SharedRandom));

			// Only try to add resources to cells that are ready for conversion
			var convertedCells = cellsToConvert
				.Where(kv => resourceLayer.GetResource(kv.Key).Type == null || resourceLayer.GetResource(kv.Key).Type == info.ConvertTo)
				.Select(kv => kv.Key)
				.ToList();

			if (convertedCells.Count > 0)
				Add(convertedCells.Random(self.World.SharedRandom));
		}

		void Remove(CPos cell)
		{
			var resource = resourceLayer.GetResource(cell);
			var amountRemoved = resourceLayer.RemoveResource(resource.Type, cell, info.Amount);

			// Remove some of the original resource
			if (amountRemoved > 0)
			{
				// Track how much we've removed from this cell
				if (!cellsToConvert.ContainsKey(cell))
					cellsToConvert[cell] = 0;

				cellsToConvert[cell] += amountRemoved;
				var updatedResource = resourceLayer.GetResource(cell);

				// If none of the original resource is left, mark it as converted
				if (updatedResource.Density == 0)
					Add(cell);
			}
		}

		void Add(CPos cell)
		{
			if (!resourceLayer.CanAddResource(info.ConvertTo, cell))
				return;

			var toConvert = cellsToConvert[cell];
			var maxDensity = resourceLayer.GetMaxDensity(info.ConvertTo);
			var currentDensity = resourceLayer.GetResource(cell).Density;

			// Only try to add info.Amount, limited by remaining space
			var amountToAdd = Math.Min(info.Amount, maxDensity - currentDensity);
			var amountAdded = resourceLayer.AddResource(info.ConvertTo, cell, amountToAdd);

			if (amountAdded > 0)
			{
				var remaining = toConvert - amountAdded;
				if (remaining > 0)
					cellsToConvert[cell] = remaining;
				else
					cellsToConvert.Remove(cell);
			}
			else if (currentDensity >= maxDensity)
				cellsToConvert.Remove(cell);
		}
	}
}
