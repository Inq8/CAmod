#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Allows the player to issue the orders the AutoDeployer traits trigger.")]
	public class AutoDeployManagerInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new AutoDeployManager(init.Self, this); }
	}

	public class AutoDeployManager : ConditionalTrait<AutoDeployManagerInfo>, IBotTick
	{
		readonly HashSet<TraitPair<AutoDeployer>> active = new HashSet<TraitPair<AutoDeployer>>();
		readonly HashSet<Order> undeployOrders = new HashSet<Order>();
		readonly World world;

		public AutoDeployManager(Actor self, AutoDeployManagerInfo info)
			: base(info)
		{
			world = self.World;
		}

		public void AddEntry(TraitPair<AutoDeployer> entry)
		{
			active.Add(entry);
		}

		public void AddUndeployOrders(Order order)
		{
			undeployOrders.Add(order);
		}

		void IBotTick.BotTick(IBot bot)
		{
			foreach (var entry in active)
			{
				if (entry.Actor.IsDead || !entry.Actor.IsInWorld)
					continue;

				if (world.LocalRandom.Next(100) > entry.Trait.Info.DeployChance)
					continue;

				var orders = entry.Trait.DeployTraits.Where(d => d.CanIssueDeployOrder(entry.Actor, false)).Select(d => d.IssueDeployOrder(entry.Actor, false));

				foreach (var order in orders)
					bot.QueueOrder(order);

				if (entry.Trait.PrimaryBuilding)
					bot.QueueOrder(new Order(AutoDeployer.PrimaryBuildingOrderID, entry.Actor, false));
			}

			active.Clear();

			foreach (var order in undeployOrders)
			{
				if (order.Subject.IsDead || !order.Subject.IsInWorld)
					continue;

				bot.QueueOrder(order);
			}

			undeployOrders.Clear();
		}
	}
}
