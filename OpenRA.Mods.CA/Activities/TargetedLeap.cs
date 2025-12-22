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
using OpenRA.Activities;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	public class TargetedLeap : Activity
	{
		readonly TargetedLeapAbility ability;
		readonly Mobile mobile;
		readonly int speed;
		readonly CPos targetCell; // original target

		WPos originPos;
		CPos destinationCell; // actual target
		WPos destinationPos;
		SubCell destinationSubCell = SubCell.Any;

		int length;
		bool jumpComplete = false;
		int ticks = 0;
		IFacing facing;
		WAngle angle;
		int delayTicks;
		string[] takeOffSounds;
		string[] landingSounds;
		string condition;
		bool jumpStarted = false;
		bool canceling = false;
		int token = Actor.InvalidConditionToken;

		public TargetedLeap(Actor self, CPos targetCell, TargetedLeapAbility ability, Mobile mobile, IFacing facing, WAngle angle)
		{
			this.ability = ability;
			this.mobile = mobile;
			speed = ability.Info.Speed;
			this.targetCell = destinationCell = targetCell;
			this.facing = facing;
			this.angle = angle;
			takeOffSounds = ability.Info.TakeOffSounds;
			landingSounds = ability.Info.LandingSounds;
			condition = ability.Info.LeapCondition;
			delayTicks = self.World.SharedRandom.Next(0, 15);
		}

		protected override void OnFirstRun(Actor self)
		{
			if (!ability.CanPerformMovement)
			{
				canceling = true;
				return;
			}

			originPos = self.CenterPosition;

			var cell = ChooseBestDestinationCell(self, targetCell);
			if (cell.HasValue)
			{
				destinationCell = cell.Value;
			}
			else
			{
				canceling = true;
				return;
			}

			var subCell = mobile.GetAvailableSubCell(destinationCell);
			if (subCell != SubCell.Invalid)
			{
				destinationSubCell = subCell;
			}
			else
			{
				canceling = true;
				return;
			}

			destinationPos = self.World.Map.CenterOfSubCell(destinationCell, destinationSubCell);
			length = Math.Max((originPos - destinationPos).Length / speed, 1);

			if (facing != null)
				facing.Facing = (destinationPos - originPos).Yaw;

			mobile.SetLocation(destinationCell, destinationSubCell, destinationCell, destinationSubCell);

			IsInterruptible = false;
		}

		public override bool Tick(Actor self)
		{
			// Correct the visual position after we jumped
			if (jumpComplete || canceling)
			{
				if (token != Actor.InvalidConditionToken)
					token = self.RevokeCondition(token);

				return true;
			}

			if (--delayTicks > 0)
				return false;

			if (!jumpStarted)
			{
				if (takeOffSounds != null && takeOffSounds.Length > 0)
					Game.Sound.Play(SoundType.World, takeOffSounds, self.World, self.CenterPosition);

				if (token == Actor.InvalidConditionToken && condition != null)
					token = self.GrantCondition(condition);

				jumpStarted = true;
				ability.ConsumeCharge();
			}

			var position = length > 1 ? WPos.LerpQuadratic(originPos, destinationPos, angle, ticks, length - 1) : destinationPos;
			mobile.SetCenterPosition(self, position);

			// We are at the destination
			if (++ticks >= length)
			{
				// Update movement which results in movementType set to MovementType.None.
				// This is needed to prevent the move animation from playing.
				mobile.UpdateMovement();

				if (landingSounds != null && landingSounds.Length > 0)
					Game.Sound.Play(SoundType.World, landingSounds, self.World, self.CenterPosition);

				jumpComplete = true;
				QueueChild(mobile.LocalMove(self, position, self.World.Map.CenterOfSubCell(destinationCell, destinationSubCell)));
			}

			return false;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(ticks < length / 2 ? originPos : destinationPos);
		}

		CPos? ChooseBestDestinationCell(Actor self, CPos destination)
		{
			var maxDistance = ability.Info.MaxDistance;
			var restrictTo = self.World.Map.FindTilesInCircle(self.Location, maxDistance).ToList();
			var pos = self.Trait<IPositionable>();

			// Check if the original destination is within MaxDistance and is valid
			if ((destination - self.Location).LengthSquared <= maxDistance * maxDistance &&
				restrictTo.Contains(destination) &&
				pos.CanEnterCell(destination) &&
				self.Owner.Shroud.IsExplored(destination))
			{
				return destination;
			}

			// Find the closest valid cell within MaxDistance
			var closestValidCell = restrictTo
				.Where(tile => pos.CanEnterCell(tile) && self.Owner.Shroud.IsExplored(tile))
				.OrderBy(tile => (tile - destination).LengthSquared)
				.FirstOrDefault();

			if (closestValidCell != default)
				return closestValidCell;

			return null;
		}
	}
}
