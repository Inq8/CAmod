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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class AttackAircraftCAInfo : AttackAircraftInfo, Requires<AircraftInfo>
	{
		[Desc("Tolerance for attack angle against air targets. Range [0, 128], 128 covers 360 degrees.")]
		public readonly WAngle AirFacingTolerance = new WAngle(512);

		public override object Create(ActorInitializer init) { return new AttackAircraftCA(init.Self, this); }
	}

	public class AttackAircraftCA : AttackAircraft
	{
		public new readonly AttackAircraftCAInfo Info;
		readonly AircraftInfo aircraftInfo;

		public AttackAircraftCA(Actor self, AttackAircraftCAInfo info)
			: base(self, info)
		{
			Info = info;
			aircraftInfo = self.Info.TraitInfo<AircraftInfo>();
		}

		protected override bool CanAttack(Actor self, in Target target)
		{
			// Don't fire while landed or when outside the map.
			if (self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length < aircraftInfo.MinAirborneAltitude
				|| !self.World.Map.Contains(self.Location))
				return false;

			if (!base.CanAttack(self, target))
				return false;

			var facingTolerance = Info.FacingTolerance;

			if (target.Type == TargetType.Actor)
			{
				var targetAircraftInfo = target.Actor.Info.TraitInfoOrDefault<AircraftInfo>();
				if (targetAircraftInfo != null)
					if (self.World.Map.DistanceAboveTerrain(target.Actor.CenterPosition).Length >= targetAircraftInfo.MinAirborneAltitude)
						facingTolerance = Info.AirFacingTolerance;
			}

			return TargetInFiringArc(self, target, facingTolerance);
		}
	}
}
