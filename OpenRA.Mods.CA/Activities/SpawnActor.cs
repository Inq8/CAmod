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
using System.Linq;
using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	public class SpawnActor : Activity
	{
		readonly CPos targetCell;
		readonly WAngle? initFacing;
		readonly bool skipMakeAnims;
		readonly string[]types;
		readonly string[] spawnSounds;
		readonly AmmoPool ammoPool;
		readonly int range;
		readonly bool avoidActors;
		readonly WDist maxRange;
		readonly bool spawnInShroud;
		readonly HashSet<string> allowedTerrainTypes;
		readonly Action<Actor, Actor> onActorSpawned;

		public SpawnActor(string[] types, CPos targetCell, WAngle? initFacing, bool skipMakeAnims, string[] spawnSounds,
			AmmoPool ammoPool, int range, bool avoidActors, WDist maxRange, bool spawnInShroud, HashSet<string> allowedTerrainTypes,
			Action<Actor, Actor> onActorSpawned = null)
		{
			this.targetCell = targetCell;
			this.initFacing = initFacing;
			this.skipMakeAnims = skipMakeAnims;
			this.types = types;
			this.spawnSounds = spawnSounds;
			this.ammoPool = ammoPool;
			this.range = range;
			this.avoidActors = avoidActors;
			this.maxRange = maxRange;
			this.spawnInShroud = spawnInShroud;
			this.allowedTerrainTypes = allowedTerrainTypes;
			this.onActorSpawned = onActorSpawned;

			IsInterruptible = false;
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
			var soundPlayed = false;

			foreach (var type in types)
			{
				var actorType = type.ToLowerInvariant();
				var ai = map.Rules.Actors[actorType];
				var td = CreateTypeDictionary(self, targetCell);
				var placed = false;

				self.World.AddFrameEndTask(w =>
				{
					Actor unit = null;
					cell = targetCells.GetEnumerator();

					if (!ai.HasTraitInfo<IPositionableInfo>())
					{
						if (avoidActors)
						{
							while (cell.MoveNext() && !placed)
							{
								if (!IsValidTargetCell(cell.Current, self))
									continue;

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
							unit = self.World.CreateActor(type, td);
					}
					else
					{
						unit = self.World.CreateActor(false, actorType, td);
						var positionable = unit.TraitOrDefault<IPositionable>();

						while (cell.MoveNext() && !placed)
						{
							var subCell = positionable.GetAvailableSubCell(cell.Current);

							if (ai.HasTraitInfo<AircraftInfo>()
								&& ai.TraitInfo<AircraftInfo>().CanEnterCell(self.World, unit, cell.Current, SubCell.FullCell, null, BlockedByActor.None))
								subCell = SubCell.FullCell;

							if (!IsValidTargetCell(cell.Current, self))
								continue;

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

					if (placed && unit != null)
					{
						if (ammoPool != null)
							ammoPool.TakeAmmo(self, 1);

						if (!soundPlayed && spawnSounds.Length > 0)
						{
							Game.Sound.Play(SoundType.World, spawnSounds, self.World, unit.CenterPosition);
							soundPlayed = true;
						}

						onActorSpawned?.Invoke(self, unit);
					}
				});
			}
		}

		bool IsValidTargetCell(CPos cell, Actor self)
		{
			var targetPos = self.World.Map.CenterOfCell(cell);
			var sourcePos = self.CenterPosition;

			return ((maxRange == WDist.Zero || (targetPos - sourcePos).Length <= maxRange.Length)
				&& (spawnInShroud || self.Owner.Shroud.IsExplored(cell))
				&& (allowedTerrainTypes.Count == 0 || allowedTerrainTypes.Contains(self.World.Map.GetTerrainInfo(cell).Type)));
		}

		TypeDictionary CreateTypeDictionary(Actor self, CPos targetCell)
		{
			var td = new TypeDictionary
			{
				new FactionInit(self.Owner.Faction.InternalName),
				new OwnerInit(self.Owner),
				new LocationInit(targetCell)
			};

			if (initFacing.HasValue)
				td.Add(new FacingInit(initFacing.Value));

			if (skipMakeAnims)
				td.Add(new SkipMakeAnimsInit());

			return td;
		}
	}
}
