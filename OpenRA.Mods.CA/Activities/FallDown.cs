#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	public class FallDown : Activity
	{
		readonly IPositionable pos;
		readonly IFacing facing;
		readonly WVec fallVector;

		WPos dropPosition;
		WPos currentPosition;

		public FallDown(Actor self, WPos dropPosition, int fallRate, int speed = 0)
		{
			IsInterruptible = false;
			pos = self.TraitOrDefault<IPositionable>();
			facing = self.TraitOrDefault<IFacing>();
			this.dropPosition = dropPosition;
			fallVector = CalculateFallVector(fallRate, speed, facing != null ? facing.Facing : WAngle.Zero);
		}

		public override bool Tick(Actor self)
		{
			currentPosition += fallVector;
			pos.SetCenterPosition(self, currentPosition);

			// If the unit has landed, this will be the last tick
			if (self.World.Map.DistanceAboveTerrain(currentPosition).Length <= 0)
			{
				var dat = self.World.Map.DistanceAboveTerrain(currentPosition);
				pos.SetPosition(self, currentPosition - new WVec(WDist.Zero, WDist.Zero, dat));

				foreach (var nfd in self.TraitsImplementing<INotifyFallDown>())
					nfd.OnLanded(self);

				return true;
			}

			return false;
		}

		protected override void OnFirstRun(Actor self)
		{
			// Place the actor and retrieve its visual position (CenterPosition)
			pos.SetPosition(self, dropPosition);
			currentPosition = self.CenterPosition;
		}

		WVec CalculateFallVector(int fallRate, int speed, WAngle facing)
		{
			var dir = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(facing));
			var vec = speed * dir / 1024;
			return new WVec(vec.X, vec.Y, -fallRate);
		}
	}
}
