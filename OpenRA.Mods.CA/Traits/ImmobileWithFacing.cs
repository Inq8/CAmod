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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class ImmobileWithFacingInfo : TraitInfo, IOccupySpaceInfo, IFacingInfo
	{
		public readonly bool OccupiesSpace = true;
		public override object Create(ActorInitializer init) { return new ImmobileWithFacing(init, this); }

		public WAngle GetInitialFacing() { return new WAngle(512); }

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any)
		{
			var occupied = OccupiesSpace ? new Dictionary<CPos, SubCell>() { { location, SubCell.FullCell } } :
				new Dictionary<CPos, SubCell>();

			return new ReadOnlyDictionary<CPos, SubCell>(occupied);
		}

		bool IOccupySpaceInfo.SharesCell { get { return false; } }
	}

	class ImmobileWithFacing : IOccupySpace, ISync, INotifyAddedToWorld, INotifyRemovedFromWorld, IFacing, IDeathActorInitModifier
	{
		[Sync]
		readonly CPos location;

		[Sync]
		readonly WPos position;

		readonly (CPos, SubCell)[] occupied;

		WRot orientation;

		[Sync]
		public WAngle Facing
		{
			get { return orientation.Yaw; }
			set { orientation = orientation.WithYaw(value); }
		}

		public WRot Orientation { get { return orientation; } }

		public WAngle TurnSpeed { get { return WAngle.Zero; } }

		public ImmobileWithFacing(ActorInitializer init, ImmobileWithFacingInfo info)
		{
			location = init.GetValue<LocationInit, CPos>();
			position = init.World.Map.CenterOfCell(location);

			if (info.OccupiesSpace)
				occupied = new[] { (TopLeft, SubCell.FullCell) };
			else
				occupied = new (CPos, SubCell)[0];

			Facing = init.GetValue<FacingInit, WAngle>(info.GetInitialFacing());
		}

		public CPos TopLeft { get { return location; } }
		public WPos CenterPosition { get { return position; } }
		public (CPos, SubCell)[] OccupiedCells() { return occupied; }

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddToMaps(self, this);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.RemoveFromMaps(self, this);
		}

		void IDeathActorInitModifier.ModifyDeathActorInit(Actor self, TypeDictionary init)
		{
			init.Add(new FacingInit(Facing));
		}
	}
}
