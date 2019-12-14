#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	public class FallDown : Activity
	{
		readonly IPositionable pos;
		readonly WVec fallVector;

		WPos dropPosition;
		WPos currentPosition;

		public FallDown(Actor self, WPos dropPosition, int fallRate, Actor ignoreActor = null)
		{
			pos = self.TraitOrDefault<IPositionable>();
			IsInterruptible = false;
			fallVector = new WVec(0, 0, fallRate);
			this.dropPosition = dropPosition;
		}

		public override bool Tick(Actor self)
		{
			currentPosition -= fallVector;
			pos.SetVisualPosition(self, currentPosition);

			// If the unit has landed, this will be the last tick
			if (self.World.Map.DistanceAboveTerrain(currentPosition).Length <= 0)
			{
				var dat = self.World.Map.DistanceAboveTerrain(currentPosition);
				pos.SetPosition(self, currentPosition - new WVec(WDist.Zero, WDist.Zero, dat));

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
	}
}
