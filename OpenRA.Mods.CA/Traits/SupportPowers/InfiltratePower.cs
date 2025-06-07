#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Acts like infiltrating a targeted structure.")]
	public class InfiltratePowerInfo : SupportPowerInfo
	{
		[FieldLoader.Require]
		[Desc("The `TargetTypes` from `Targetable` that can be targeted.")]
		public readonly BitSet<TargetableType> Types = default;

		[Desc("Should visibility (Shroud, Fog, Cloak, etc) be considered when searching for targets?")]
		public readonly bool RequireVisibleTarget = true;

		public override object Create(ActorInitializer init) { return new InfiltratePower(init, this); }
	}

	public class InfiltratePower : SupportPower
	{
		readonly InfiltratePowerInfo info;

		public InfiltratePower(ActorInitializer init, InfiltratePowerInfo info)
			: base(init.Self, info)
		{
			this.info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.OrderGenerator = new SelectInfiltrateTarget(Self.World, order, manager, this);
		}

		public override void Charged(Actor self, string key)
		{
			base.Charged(self, key);

			self.World.AddFrameEndTask(w =>
			{
				var info = Info as InfiltratePowerInfo;
			});
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			PlayLaunchSounds();

			IOrderedEnumerable<Actor> targets;

			targets = UnitsInRange(self.World.Map.CellContaining(order.Target.CenterPosition), true)
				.OrderByDescending(x => x.ActorID);

			foreach (var t in targets)
			{
				var notifiers = t.TraitsImplementing<INotifyInfiltrated>().ToArray();
				foreach (var n in notifiers)
					n.Infiltrated(t, self, info.Types);
			}
		}

		public IEnumerable<Actor> UnitsInRange(CPos xy, bool skipVisibilityCheck = false)
		{
			var range = 0;
			var tiles = Self.World.Map.FindTilesInCircle(xy, range);
			var units = new List<Actor>();
			foreach (var t in tiles)
				units.AddRange(Self.World.ActorMap.GetActorsAt(t));

			return units.Distinct().Where(a =>
			{
				if (a.Owner.IsAlliedWith(Self.Owner))
					return false;

				if (!a.GetAllTargetTypes().Overlaps(info.Types))
					return false;

				if (!skipVisibilityCheck && info.RequireVisibleTarget && !a.CanBeViewedByPlayer(Self.Owner))
					return false;

				return true;
			});
		}

		class SelectInfiltrateTarget : OrderGenerator
		{
			readonly InfiltratePower power;
			readonly SupportPowerManager manager;
			readonly string order;

			public SelectInfiltrateTarget(World world, string order, SupportPowerManager manager, InfiltratePower power)
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
