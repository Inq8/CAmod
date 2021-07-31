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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Spawns an actor that stays for a limited amount of time.")]
	public class SpawnActorPowerCAInfo : SpawnActorPowerInfo
	{
		public readonly WDist TargetCircleRange = WDist.Zero;
		public readonly Color TargetCircleColor = Color.White;
		public readonly bool TargetCircleUsePlayerColor = false;

		public override object Create(ActorInitializer init) { return new SpawnActorPowerCA(init.Self, this); }
	}

	public class SpawnActorPowerCA : SpawnActorPower
	{
		public new readonly SpawnActorPowerCAInfo Info;

		public SpawnActorPowerCA(Actor self, SpawnActorPowerCAInfo info)
			: base(self, info)
		{
			Info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.OrderGenerator = new SelectSpawnActorPowerCATarget(order, manager, this);
		}
	}

	public class SelectSpawnActorPowerCATarget : OrderGenerator
	{
		readonly SupportPowerManager manager;
		readonly string order;
		readonly SpawnActorPowerCA power;

		public SelectSpawnActorPowerCATarget(string order, SupportPowerManager manager, SpawnActorPowerCA power)
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
			if (mi.Button == MouseButton.Left && world.Map.Contains(cell))
				yield return new Order(order, manager.Self, Target.FromCell(world, cell), false) { SuppressVisualFeedback = true };
		}

		protected override void Tick(World world)
		{
			// Cancel the OG if we can't use the power
			if (!manager.Powers.ContainsKey(order))
				world.CancelInputMode();
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }

		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
		{
			var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);

			if (power.Info.TargetCircleRange == WDist.Zero)
			{
				yield break;
			}
			else
			{
				yield return new RangeCircleAnnotationRenderable(
					world.Map.CenterOfCell(xy),
					power.Info.TargetCircleRange,
					0,
					power.Info.TargetCircleUsePlayerColor ? power.Self.Owner.Color : power.Info.TargetCircleColor,
					1,
					Color.FromArgb(96, Color.Black),
					3);
			}
		}

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return world.Map.Contains(cell) ? power.Info.Cursor : "generic-blocked";
		}
	}
}
