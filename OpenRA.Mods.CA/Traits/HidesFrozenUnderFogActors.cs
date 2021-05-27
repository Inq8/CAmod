#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class HidesFrozenUnderFogActorsInfo : ConditionalTraitInfo
	{
		[Desc("Range to hide frozen actors within.")]
		public readonly WDist Range = WDist.FromCells(5);

		[Desc("Number of ticks to wait between updating frozen actors.")]
		public readonly int MovingUpdateInterval = 20;

		[Desc("Number of ticks to wait between updating frozen actors.")]
		public readonly int StationaryUpdateInterval = 100;

		public override object Create(ActorInitializer init) { return new HidesFrozenUnderFogActors(init, this); }
	}

	public class HidesFrozenUnderFogActors : ConditionalTrait<HidesFrozenUnderFogActorsInfo>, INotifyMoving, INotifyOwnerChanged, ITick
	{
		bool isMoving;
		int movingUpdateTicks;
		int stationaryUpdateTicks;
		Player player;
		Player[] enemyPlayers;
		MovementType lastMovementType;

		public HidesFrozenUnderFogActors(ActorInitializer init, HidesFrozenUnderFogActorsInfo info)
			: base(info)
		{
			movingUpdateTicks = info.MovingUpdateInterval;
			stationaryUpdateTicks = info.StationaryUpdateInterval;
			lastMovementType = MovementType.None;
			isMoving = false;
		}

		protected override void Created(Actor self)
		{
			SetPlayers(self);
			Update(self);
		}

		void INotifyMoving.MovementTypeChanged(Actor self, MovementType movementType)
		{
			if (movementType == MovementType.None)
			{
				isMoving = false;
				stationaryUpdateTicks = Info.StationaryUpdateInterval;
			}
			else
			{
				isMoving = true;
				if (lastMovementType == MovementType.None)
					movingUpdateTicks = Info.MovingUpdateInterval;

			}

			lastMovementType = movementType;
		}

		void ITick.Tick(Actor self)
		{
			if (isMoving && --movingUpdateTicks > 0)
				return;

			if (!isMoving && --stationaryUpdateTicks > 0)
				return;

			Update(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			SetPlayers(self);
		}

		protected void Update(Actor self)
		{
			foreach (var enemyPlayer in enemyPlayers)
			{
				var frozenActorLayer = enemyPlayer.FrozenActorLayer;
				var frozenActors = frozenActorLayer.FrozenActorsInCircle(self.World, self.CenterPosition, Info.Range, true).ToList();

				foreach (var frozenActor in frozenActors)
				{
					frozenActorLayer.Remove(frozenActor);
				}
			}

			movingUpdateTicks = Info.MovingUpdateInterval;
			stationaryUpdateTicks = Info.StationaryUpdateInterval;
		}

		protected void SetPlayers(Actor self)
		{
			player = self.Owner;
			enemyPlayers = self.World.Players.Where(p => !p.NonCombatant && player.RelationshipWith(p) == PlayerRelationship.Enemy).ToArray();
		}
	}
}
