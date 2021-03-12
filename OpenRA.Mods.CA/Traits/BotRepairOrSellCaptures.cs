#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Helper trait to set the AI to try selling and then repairing newly controlled buildings.")]
	public class BotRepairOrSellCapturesInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new BotRepairOrSellCaptures(init.Self, this); }
	}

	public class BotRepairOrSellCaptures : INotifyOwnerChanged
	{
		public readonly BotRepairOrSellCapturesInfo Info;

		public BotRepairOrSellCaptures(Actor self, BotRepairOrSellCapturesInfo info)
		{
			Info = info;
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (!newOwner.IsBot)
				return;

			var health = self.TraitOrDefault<Health>();
			if (health == null)
				return;

			var sellable = self.TraitOrDefault<Sellable>();
			if (sellable != null && !sellable.IsTraitDisabled)
			{
				self.World.IssueOrder(new Order("Sell", self, Target.FromActor(self), false));
				return;
			}

			var rb = self.TraitOrDefault<RepairableBuilding>();
			if (rb != null && health.DamageState != DamageState.Undamaged)
				self.World.IssueOrder(new Order("RepairBuilding", newOwner.PlayerActor, Target.FromActor(self), false));
		}
	}
}
