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
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("This unit, when ordered to move, will fly in ballistic path then will detonate itself upon reaching target.")]
	public abstract class MissileBaseInfo : TraitInfo, IMoveInfo, IPositionableInfo, IFacingInfo
	{
		[Desc("Projectile speed in WDist / tick, two values indicate variable velocity.")]
		public readonly int Speed = 17;

		[Desc("In angle. MissileBase is launched at this pitch and the intial tangential line of the ballistic path will be this.")]
		public readonly WAngle LaunchAngle = WAngle.Zero;

		[Desc("Minimum altitude where this missile is considered airborne")]
		public readonly int MinAirborneAltitude = 5;

		[Desc("Types of damage missile explosion is triggered with. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while airborne.")]
		public readonly string AirborneCondition = null;

		[Desc("Color to use for the target line.")]
		public readonly Color TargetLineColor = Color.Crimson;

		public abstract override object Create(ActorInitializer init);

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any) { return new Dictionary<CPos, SubCell>(); }
		bool IOccupySpaceInfo.SharesCell { get { return false; } }
		public bool CanEnterCell(World world, Actor self, CPos cell, SubCell subCell = SubCell.FullCell, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			// SBMs may not land.
			return false;
		}

		// set by spawned logic, not this.
		public WAngle GetInitialFacing() { return WAngle.Zero; }

		public Color GetTargetLineColor() { return TargetLineColor; }
	}

	public abstract class MissileBase : ISync, IFacing, IMove, IPositionable,
		INotifyCreated, INotifyAddedToWorld, INotifyRemovedFromWorld, IOccupySpace
	{
		static readonly (CPos, SubCell)[] NoCells = { };

		public readonly MissileBaseInfo Info;
		readonly Actor self;
		public Target Target;

		IEnumerable<int> speedModifiers;
		INotifyCenterPositionChanged[] notifyCenterPositionChanged;
		bool requiresVisibilityChecks = false;

		[Sync]
		public WAngle Facing
		{
			get { return Orientation.Yaw; }
			set { Orientation = Orientation.WithYaw(value); }
		}

		public WAngle Pitch
		{
			get { return Orientation.Pitch; }
			set { Orientation = Orientation.WithPitch(value); }
		}

		public WRot Orientation { get; private set; }

		[Sync]
		public WPos CenterPosition { get; private set; }

		public CPos TopLeft { get { return self.World.Map.CellContaining(CenterPosition); } }

		bool airborne;
		int airborneToken = Actor.InvalidConditionToken;

		public MissileBase(ActorInitializer init, MissileBaseInfo info)
		{
			Info = info;
			self = init.Self;

			var locationInit = init.GetOrDefault<LocationInit>(info);
			if (locationInit != null)
				SetPosition(self, locationInit.Value);

			var centerPositionInit = init.GetOrDefault<CenterPositionInit>(info);
			if (centerPositionInit != null)
				SetPosition(self, centerPositionInit.Value);

			// I need facing but initial facing doesn't matter, they are determined by the spawner's facing.
			Facing = init.GetValue<FacingInit, WAngle>(info, WAngle.Zero);
		}

		// This kind of missile will not turn anyway. Hard-coding here.
		public WAngle TurnSpeed { get { return new WAngle(40); } }

		void INotifyCreated.Created(Actor self)
		{
			speedModifiers = self.TraitsImplementing<ISpeedModifier>().ToArray().Select(sm => sm.GetSpeedModifier());
			notifyCenterPositionChanged = self.TraitsImplementing<INotifyCenterPositionChanged>().ToArray();
			requiresVisibilityChecks = self.TraitsImplementing<AffectsShroud>().Any();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddToMaps(self, this);
			self.QueueActivity(GetActivity(self, Target));

			var altitude = self.World.Map.DistanceAboveTerrain(CenterPosition);
			if (altitude.Length >= Info.MinAirborneAltitude)
				OnAirborneAltitudeReached();
		}

		public (CPos Cell, SubCell SubCell)[] OccupiedCells()
		{
			return NoCells;
		}

		public int MovementSpeed
		{
			get { return Util.ApplyPercentageModifiers(Info.Speed, speedModifiers); }
		}

		public WVec FlyStep(WAngle facing)
		{
			return FlyStep(MovementSpeed, facing);
		}

		public WVec FlyStep(int speed, WAngle facing)
		{
			var dir = new WVec(0, -1024, 0).Rotate(WRot.FromFacing(facing.Facing));
			return speed * dir / 1024;
		}

		protected abstract Activity GetActivity(Actor self, Target target);
		public abstract void SetTarget(Target target);

		#region Implement IPositionable

		public bool CanExistInCell(CPos cell) { return true; }
		public bool IsLeavingCell(CPos location, SubCell subCell = SubCell.Any) { return false; } // TODO: Handle landing
		public bool CanEnterCell(CPos location, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All) { return true; }
		public SubCell GetValidSubCell(SubCell preferred) { return SubCell.Invalid; }
		public SubCell GetAvailableSubCell(CPos location, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			// Does not use any subcell
			return SubCell.Invalid;
		}

		public void SetCenterPosition(Actor self, WPos pos) { SetPosition(self, pos); }

		// Changes position, but not altitude
		public void SetPosition(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			SetPosition(self, self.World.Map.CenterOfCell(cell) + new WVec(0, 0, CenterPosition.Z));
		}

		public void SetPosition(Actor self, WPos pos)
		{
			CenterPosition = pos;

			if (!self.IsInWorld)
				return;

			self.World.UpdateMaps(self, this);

			var altitude = self.World.Map.DistanceAboveTerrain(CenterPosition);
			var isAirborne = altitude.Length >= Info.MinAirborneAltitude;
			if (isAirborne && !airborne)
				OnAirborneAltitudeReached();
			else if (!isAirborne && airborne)
				OnAirborneAltitudeLeft();

			// NB: This can be called from the constructor before notifyCenterPositionChanged is assigned.
			if (requiresVisibilityChecks && notifyCenterPositionChanged != null)
				foreach (var n in notifyCenterPositionChanged)
					n.CenterPositionChanged(self, 0, 0);
		}

		#endregion

		#region Implement IMove

		public Activity MoveTo(CPos cell, int nearEnough = 0, Actor ignoreActor = null,
			bool evaluateNearestMovableCell = false, Color? targetLineColor = null)
		{
			return GetActivity(self, Target.FromCell(self.World, cell));
		}

		public Activity MoveWithinRange(in Target target, WDist range,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return GetActivity(self, target);
		}

		public Activity MoveWithinRange(in Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return GetActivity(self, target);
		}

		public Activity MoveFollow(Actor self, in Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return null;
		}

		public Activity ReturnToCell(Actor self)
		{
			return null;
		}

		public Activity MoveToTarget(Actor self, in Target target,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return GetActivity(self, target);
		}

		public Activity MoveIntoTarget(Actor self, in Target target)
		{
			return GetActivity(self, target);
		}

		public Activity MoveOntoTarget(Actor self, in Target target, in WVec offset,
			WAngle? facing, Color? targetLineColor = null)
		{
			return GetActivity(self, target);
		}

		public Activity LocalMove(Actor self, WPos fromPos, WPos toPos)
		{
			return null;
		}

		public int EstimatedMoveDuration(Actor self, WPos fromPos, WPos toPos)
		{
			var speed = MovementSpeed;
			return speed > 0 ? (toPos - fromPos).Length / speed : 0;
		}

		public CPos NearestMoveableCell(CPos cell) { return cell; }

		// Actors with MissileBase always move
		public MovementType CurrentMovementTypes { get { return MovementType.Horizontal | MovementType.Vertical; } set { } }

		public bool CanEnterTargetNow(Actor self, in Target target)
		{
			// you can never control ballistic missiles anyway
			return false;
		}

		#endregion

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.RemoveFromMaps(self, this);
			OnAirborneAltitudeLeft();
		}

		#region Airborne conditions

		void OnAirborneAltitudeReached()
		{
			if (airborne)
				return;

			airborne = true;
			if (airborneToken == Actor.InvalidConditionToken)
				airborneToken = self.GrantCondition(Info.AirborneCondition);
		}

		void OnAirborneAltitudeLeft()
		{
			if (!airborne)
				return;

			airborne = false;
			if (airborneToken != Actor.InvalidConditionToken)
				airborneToken = self.RevokeCondition(airborneToken);
		}

		#endregion
	}
}
