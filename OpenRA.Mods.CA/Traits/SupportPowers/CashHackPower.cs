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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	class CashHackPowerInfo : SupportPowerInfo
	{
		[Desc("Percentage of the victim's resources that will be stolen.")]
		public readonly int Percentage = 100;

		[Desc("Amount of guaranteed funds to claim when the victim does not have enough resources.")]
		public readonly int Minimum = 0;

		[Desc("Should visibility (Shroud, Fog, Cloak, etc) be considered when searching for hackable targets?")]
		public readonly bool CheckHackTargetForVisibility = true;

		[Desc("Maximum amount of funds which will be stolen.")]
		public readonly int Maximum = int.MaxValue;

		[Desc("Type of support power. Used for targerting along with 'CashHackable' trait on actors.")]
		public readonly string Type = "Cash-Hack";

		[Desc("Sound to instantly play at the targeted area.")]
		public readonly string OnFireSound = null;

		[Desc("Cursor to display when unable to Cash Hack.")]
		public readonly string BlockedCursor = "move-blocked";

		[NotificationReference("Speech")]
		[Desc("Sound the victim will hear when they get robbed.")]
		public readonly string Notification = null;

		[Desc("Whether to show the cash tick indicators rising from the actor.")]
		public readonly bool ShowTicks = true;

		public override object Create(ActorInitializer init) { return new CashHackPower(init.Self, this); }
	}

	class CashHackPower : SupportPower
	{
		readonly CashHackPowerInfo info;

		public CashHackPower(Actor self, CashHackPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.OrderGenerator = new SelectHackTarget(Self.World, order, manager, this);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			PlayLaunchSounds();

			var ownResources = self.Owner.PlayerActor.Trait<PlayerResources>();

			Game.Sound.Play(SoundType.World, info.OnFireSound, order.Target.CenterPosition);

			foreach (var a in UnitsInRange(self.World.Map.CellContaining(order.Target.CenterPosition)))
			{
				var enemyResources = a.Owner.PlayerActor.Trait<PlayerResources>();

				var toTake = Math.Min(info.Maximum, (enemyResources.Cash + enemyResources.Resources) * info.Percentage / 100);
				var toGive = Math.Max(toTake, info.Minimum);

				enemyResources.TakeCash(toTake);
				ownResources.GiveCash(toGive);

				if (info.Notification != null)
					Game.Sound.PlayNotification(a.World.Map.Rules, a.Owner, "Speech", info.Notification, a.Owner.Faction.InternalName);

				if (info.ShowTicks)
					self.World.AddFrameEndTask(w => w.Add(new FloatingText(a.CenterPosition, self.Owner.Color, FloatingText.FormatCashTick(toGive), 30)));
			}
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
				if (a.Owner.IsAlliedWith(Self.Owner) || a.Info.TraitInfoOrDefault<CashHackableInfo>() == null || (info.CheckHackTargetForVisibility == true && !(a.CanBeViewedByPlayer(Self.Owner))))
					return false;

				return a.Info.TraitInfoOrDefault<CashHackableInfo>().ValidTypes.Contains(info.Type);
			});
		}

		class SelectHackTarget : OrderGenerator
		{
			readonly CashHackPower power;
			readonly SupportPowerManager manager;
			readonly string order;

			public SelectHackTarget(World world, string order, SupportPowerManager manager, CashHackPower power)
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
					foreach (var d in decorations.RenderSelectionAnnotations(unit, wr, Color.Red))
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
