#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	public class SpawnActor : Activity
	{
		readonly WPos targetPos;
		readonly CPos targetCell;
		readonly bool skipMakeAnims;
		readonly string type;
		readonly string[] spawnSounds;
		readonly AmmoPool ammoPool;
		readonly int range;
		readonly bool avoidActors;

		public SpawnActor(Actor self, CPos targetCell, WPos targetPos, string type, bool skipMakeAnims, string[] spawnSounds, AmmoPool ammoPool, int range, bool avoidActors)
		{
			this.targetPos = targetPos;
			this.targetCell = targetCell;
			this.skipMakeAnims = skipMakeAnims;
			this.type = type;
			this.spawnSounds = spawnSounds;
			this.ammoPool = ammoPool;
			this.range = range;
			this.avoidActors = avoidActors;
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return true;

			Spawn(self);
			return true;
		}

		void Spawn(Actor self)
		{
			var map = self.World.Map;
			var targetCells = map.FindTilesInCircle(targetCell, range);
			var cell = targetCells.GetEnumerator();
			var ai = map.Rules.Actors[type.ToLowerInvariant()];
			var td = CreateTypeDictionary(self, targetCell);
			var placed = false;

			self.World.AddFrameEndTask(w =>
			{
				var unit = self.World.CreateActor(false, type, td);
				var positionable = unit.TraitOrDefault<IPositionable>();

				cell = targetCells.GetEnumerator();

				if (positionable == null)
				{
					unit.Dispose();

					if (avoidActors)
					{
						while (cell.MoveNext() && !placed)
						{
							var actorsInCell = self.World.ActorMap.GetActorsAt(cell.Current);

							if (actorsInCell.Any())
								continue;

							placed = true;
							td = CreateTypeDictionary(self, cell.Current);
						}
					}
					else
						placed = true;

					if (placed)
						self.World.CreateActor(type, td);
				}
				else
				{
					while (cell.MoveNext() && !placed)
					{
						var subCell = positionable.GetAvailableSubCell(cell.Current);

						if (ai.HasTraitInfo<AircraftInfo>()
							&& ai.TraitInfo<AircraftInfo>().CanEnterCell(self.World, unit, cell.Current, SubCell.FullCell, null, BlockedByActor.None))
							subCell = SubCell.FullCell;

						if (subCell != SubCell.Invalid)
						{
							positionable.SetPosition(unit, cell.Current, subCell);

							var pos = unit.CenterPosition;

							positionable.SetCenterPosition(unit, pos);
							w.Add(unit);

							unit.QueueActivity(new FallDown(unit, pos, 130));

							placed = true;
						}
					}

					if (!placed)
						unit.Dispose();
				}

				if (placed)
				{
					if (ammoPool != null)
						ammoPool.TakeAmmo(self, 1);

					if (spawnSounds.Length > 0)
						Game.Sound.Play(SoundType.World, spawnSounds, self.World, unit.CenterPosition);
				}
			});
		}

		TypeDictionary CreateTypeDictionary(Actor self, CPos targetCell)
		{
			var td = new TypeDictionary
			{
				new FactionInit(self.Owner.Faction.InternalName),
				new OwnerInit(self.Owner),
				new LocationInit(targetCell)
			};

			if (skipMakeAnims)
				td.Add(new SkipMakeAnimsInit());

			return td;
		}
	}
}
