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
			int unloadRange = 5;

			self.CancelActivity();
			self.QueueActivity(new ParadropCargo(self, WDist.FromCells(unloadRange), true));
		}
	}
}
