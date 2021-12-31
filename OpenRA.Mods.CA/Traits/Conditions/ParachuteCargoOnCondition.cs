#region Copyright & License Information
/*
 * Copyright 2015-2022 OpenRA.Mods.CA Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
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
