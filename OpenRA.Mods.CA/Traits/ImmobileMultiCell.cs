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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	sealed class ImmobileMultiCellInfo : TraitInfo, IOccupySpaceInfo
	{
		public readonly bool OccupiesSpace = true;

		[Desc("Dimensions of the actor (in cells).")]
		public readonly CVec Dimensions = new(1, 1);

		[Desc("Shift center of the actor by this offset.")]
		public readonly WVec LocalCenterOffset = WVec.Zero;

		public override object Create(ActorInitializer init) { return new ImmobileMultiCell(init, this); }

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any)
		{
			return OccupiesSpace ? new Dictionary<CPos, SubCell>() { { location, SubCell.FullCell } } :
				new Dictionary<CPos, SubCell>();
		}

		bool IOccupySpaceInfo.SharesCell => false;

		public WVec CenterOffset(World w)
		{
			var off = (w.Map.CenterOfCell(new CPos(Dimensions.X, Dimensions.Y)) - w.Map.CenterOfCell(new CPos(1, 1))) / 2;
			return off - new WVec(0, 0, off.Z) + LocalCenterOffset;
		}
	}

	sealed class ImmobileMultiCell : IOccupySpace, ISync, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly (CPos, SubCell)[] occupied;

		public ImmobileMultiCell(ActorInitializer init, ImmobileMultiCellInfo info)
		{
			TopLeft = init.GetValue<LocationInit, CPos>();
			CenterPosition = init.World.Map.CenterOfCell(TopLeft) + info.CenterOffset(init.World);

			if (info.OccupiesSpace)
				occupied = new[] { (TopLeft, SubCell.FullCell) };
			else
				occupied = Array.Empty<(CPos, SubCell)>();
		}

		[Sync]
		public CPos TopLeft { get; }
		[Sync]
		public WPos CenterPosition { get; }
		public (CPos, SubCell)[] OccupiedCells() { return occupied; }

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddToMaps(self, this);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.RemoveFromMaps(self, this);
		}
	}
}
