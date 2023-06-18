#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class ParachuteCargoOnConditionInfo : ConditionalTraitInfo, Requires<CargoInfo>, Requires<AttackAircraftInfo>
	{
		public override object Create(ActorInitializer init) { return new ParachuteCargoOnCondition(init, this); }

		[Desc("Wait at least this many ticks between each drop.")]
		public readonly int DropInterval = 5;

		[Desc("Distance around the drop-point to unload troops.")]
		public readonly WDist DropRange = WDist.FromCells(5);

		[Desc("Return to base when drop complete?")]
		public readonly bool ReturnToBase = true;
	}

	public class ParachuteCargoOnCondition : ConditionalTrait<ParachuteCargoOnConditionInfo>
	{
		readonly ParachuteCargoOnConditionInfo info;

		public ParachuteCargoOnCondition(ActorInitializer init, ParachuteCargoOnConditionInfo info)
			: base(info)
		{
			this.info = info;
		}

		protected override void TraitEnabled(Actor self)
		{
			self.CancelActivity();
			self.QueueActivity(new Activities.ParadropCargo(self, info.DropInterval, info.DropRange, info.ReturnToBase));
		}
	}
}
