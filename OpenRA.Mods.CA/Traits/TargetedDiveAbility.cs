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
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Allows unit to dive to a targeted location.")]
	public class TargetedDiveAbilityInfo : TargetedMovementAbilityInfo, Requires<AircraftInfo>
	{
		[ActorReference]
		[Desc("Actor to transform into when the dive is complete.")]
		public readonly string TransformIntoActor = null;

		public override object Create(ActorInitializer init) { return new TargetedDiveAbility(init.Self, this); }
	}

	public class TargetedDiveAbility : TargetedMovementAbility
	{
		public readonly new TargetedDiveAbilityInfo Info;
		readonly Aircraft aircraft;

		public override string DeployOrderID => "TargetedDiveOrderTargeterDeploy";
		public override string MovementOrderID => "TargetedDiveOrderDive";

		public TargetedDiveAbility(Actor self, TargetedDiveAbilityInfo info)
			: base(self, info)
		{
			Info = info;
			aircraft = self.Trait<Aircraft>();
		}

		protected override void QueueMovementActivity(Actor self, Target target)
		{
			var diveTarget = Target.FromCell(self.World, self.World.Map.CellContaining(target.CenterPosition));
			Action onDiveComplete = () =>
			{
				if (Info.TransformIntoActor != null)
				{
					var transform = new InstantTransform(self, Info.TransformIntoActor);
					self.CancelActivity();
					self.QueueActivity(transform);
				}
			};

			// Fixed-wing aircraft must be aligned and have enough run-up to descend within MaximumPitch.
			if (!aircraft.Info.CanHover)
				self.QueueActivity(new DiveApproach(diveTarget, aircraft));

			self.QueueActivity(new Dive(diveTarget, aircraft, Info.Speed, onDiveComplete));
		}
	}
}
