#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class ParachuteCargoOnConditionInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new ParachuteCargoOnCondition(init, this); }

		[Desc("Wait at least this many ticks between each drop.")]
		public readonly int DropInterval = 5;

		[Desc("Radius to search for a load/unload location if the ordered cell is blocked.")]
		public readonly WDist ExitRange = WDist.FromCells(5);
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
			self.QueueActivity(new Activities.ParadropCargo(self, info.DropInterval, info.ExitRange));
		}
	}
}
