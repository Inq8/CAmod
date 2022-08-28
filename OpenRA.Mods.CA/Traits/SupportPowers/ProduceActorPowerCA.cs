#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	static class PrimaryExts
	{
		public static bool IsPrimaryBuilding(this Actor a)
		{
			var pb = a.TraitOrDefault<PrimaryBuilding>();
			return pb != null && pb.IsPrimary;
		}
	}

	[Desc("Produces an actor without using the standard production queue.",
		"CA version allows actors to be produced immediately when charged.",
		"Also removes sorting of the producing actor as this can cause a crash when multiple exist.")]
	public class ProduceActorPowerCAInfo : SupportPowerInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actors to produce.")]
		public readonly string[] Actors = null;

		[FieldLoader.Require]
		[Desc("Production queue type to use")]
		public readonly string Type = null;

		[NotificationReference("Speech")]
		[Desc("Notification played when production is activated.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string ReadyAudio = null;

		[NotificationReference("Speech")]
		[Desc("Notification played when the exit is jammed.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string BlockedAudio = null;

		[Desc("Allows the actors to be produced immediately when charged.")]
		public readonly bool AutoFire = false;

		[Desc("Cursor to display when unable to Cash Hack.")]
		public readonly string BlockedCursor = "move-blocked";

		public override object Create(ActorInitializer init) { return new ProduceActorPowerCA(init, this); }
	}

	public class ProduceActorPowerCA : SupportPower
	{
		readonly ProduceActorPowerCAInfo info;
		readonly string faction;

		public ProduceActorPowerCA(ActorInitializer init, ProduceActorPowerCAInfo info)
			: base(init.Self, info)
		{
			this.info = info;
			faction = init.GetValue<FactionInit, string>(init.Self.Owner.Faction.InternalName);
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			if (info.AutoFire)
				self.World.IssueOrder(new Order(order, manager.Self, false));
			else
				self.World.OrderGenerator = new SelectProductionTarget(Self.World, order, manager, this);
		}

		public override void Charged(Actor self, string key)
		{
			base.Charged(self, key);

			self.World.AddFrameEndTask(w =>
			{
				var info = Info as ProduceActorPowerCAInfo;
				if (info.AutoFire)
					self.Owner.PlayerActor.Trait<SupportPowerManager>().Powers[key].Activate(new Order());
			});
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			PlayLaunchSounds();

			IOrderedEnumerable<TraitPair<Production>> producers;

			if (info.AutoFire)
			{
				producers = self.World.ActorsWithTrait<Production>()
					.Where(x => x.Actor.Owner == self.Owner
						&& !x.Trait.IsTraitDisabled
						&& x.Trait.Info.Produces.Contains(info.Type))
					.OrderByDescending(x => x.Actor.IsPrimaryBuilding())
					.ThenByDescending(x => x.Actor.ActorID);
			}
			else
			{
				producers = UnitsInRange(self.World.Map.CellContaining(order.Target.CenterPosition))
					.Select(a => new TraitPair<Production>(a, a.TraitsImplementing<Production>()
						.First(p => !p.IsTraitDisabled
							&& p.Info.Produces.Contains(info.Type))))
					.OrderByDescending(x => x.Actor.ActorID);
			}

			// TODO: The power should not reset if the production fails.
			// Fixing this will require a larger rework of the support power code
			var activated = false;

			foreach (var p in producers)
			{
				foreach (var name in info.Actors)
				{
					var ai = self.World.Map.Rules.Actors[name];
					var inits = new TypeDictionary
					{
						new OwnerInit(self.Owner),
						new FactionInit(BuildableInfo.GetInitialFaction(ai, faction))
					};

					activated |= p.Trait.Produce(p.Actor, ai, info.Type, inits, 0);
				}

				if (activated)
					break;
			}

			if (activated)
				Game.Sound.PlayNotification(self.World.Map.Rules, manager.Self.Owner, "Speech", info.ReadyAudio, self.Owner.Faction.InternalName);
			else
				Game.Sound.PlayNotification(self.World.Map.Rules, manager.Self.Owner, "Speech", info.BlockedAudio, self.Owner.Faction.InternalName);
		}

		public IEnumerable<Actor> UnitsInRange(CPos xy)
		{
			var range = 0;
			var tiles = Self.World.Map.FindTilesInCircle(xy, range);
			var units = new List<Actor>();
			foreach (var t in tiles)
				units.AddRange(Self.World.ActorMap.GetActorsAt(t));

			return units.Distinct().Where(a =>
			{
				if (a.Owner != Self.Owner)
					return false;

				var production = a.TraitsImplementing<Production>()
					.Where(p => !p.IsTraitDisabled
						&& p.Info.Produces.Contains(info.Type));

				if (!production.Any())
					return false;

				return true;
			});
		}

		class SelectProductionTarget : OrderGenerator
		{
			readonly ProduceActorPowerCA power;
			readonly SupportPowerManager manager;
			readonly string order;

			public SelectProductionTarget(World world, string order, SupportPowerManager manager, ProduceActorPowerCA power)
			{
				// Clear selection if using Left-Click Orders
				if (Game.Settings.Game.UseClassicMouseStyle)
					manager.Self.World.Selection.Clear();

				this.manager = manager;
				this.order = order;
				this.power = power;
			}

			protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				world.CancelInputMode();
				if (mi.Button == MouseButton.Left && power.UnitsInRange(cell).Any())
					yield return new Order(order, manager.Self, Target.FromCell(world, cell), false) { SuppressVisualFeedback = true };
			}

			protected override void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (!manager.Powers.ContainsKey(order))
					world.CancelInputMode();
			}

			protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

			protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				foreach (var unit in power.UnitsInRange(xy))
				{
					var decorations = unit.TraitsImplementing<ISelectionDecorations>().FirstEnabledTraitOrDefault();
					foreach (var d in decorations.RenderSelectionAnnotations(unit, wr, Color.Lime))
						yield return d;
				}
			}

			protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
			protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				return power.UnitsInRange(cell).Any() ? power.info.Cursor : power.info.BlockedCursor;
			}
		}
	}
}
