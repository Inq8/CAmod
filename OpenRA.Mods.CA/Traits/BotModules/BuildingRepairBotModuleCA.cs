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

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Manages AI repairing base buildings.")]
	public class BuildingRepairBotModuleCAInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new BuildingRepairBotModuleCA(init.Self, this); }
	}

	public class BuildingRepairBotModuleCA : ConditionalTrait<BuildingRepairBotModuleCAInfo>, IBotRespondToAttack
	{
		public BuildingRepairBotModuleCA(Actor self, BuildingRepairBotModuleCAInfo info)
			: base(info) { }

		void IBotRespondToAttack.RespondToAttack(IBot bot, Actor self, AttackInfo e)
		{
			// HACK: We don't want D2k bots to repair all their buildings on placement
			// where half their HP is removed via neutral terrain damage.
			// TODO: Implement concrete placement for D2k bots and remove this hack.
			if (self.Owner.RelationshipWith(e.Attacker.Owner) == PlayerRelationship.Neutral)
				return;

			var rb = self.TraitOrDefault<RepairableBuilding>();
			if (rb != null)
			{
				if (e.DamageState > DamageState.Undamaged && e.PreviousDamageState < e.DamageState && !rb.RepairActive)
				{
					AIUtils.BotDebug("{0} noticed damage {1} {2}->{3}, repairing.",
						self.Owner, self, e.PreviousDamageState, e.DamageState);
					bot.QueueOrder(new Order("RepairBuilding", self.Owner.PlayerActor, Target.FromActor(self), false));
				}
			}
		}
	}
}
