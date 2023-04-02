#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Orders
{
	public class UpgradeOrderGenerator : OrderGenerator
	{
		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				world.CancelInputMode();

			return OrderInner(world, mi);
		}

		static IEnumerable<Order> OrderInner(World world, MouseInput mi)
		{
			if (mi.Button != MouseButton.Left)
				yield break;

			var underCursor = world.ScreenMap.ActorsAtMouse(mi)
				.Select(a => a.Actor)
				.FirstOrDefault(a => a.AppearsFriendlyTo(world.LocalPlayer.PlayerActor) && !world.FogObscures(a));

			if (underCursor == null)
				yield break;

			var upgradeable = underCursor.TraitsImplementing<Upgradeable>().Where(u => u.CanUpgrade).FirstOrDefault();
			if (upgradeable == null || !upgradeable.GetValidHosts().Any())
				yield break;

			// Don't command allied units
			if (underCursor.Owner != world.LocalPlayer)
				yield break;

			yield return new Order("Upgrade", underCursor, Target.FromActor(underCursor), mi.Modifiers.HasModifier(Modifiers.Shift)) { TargetString = upgradeable.Info.Type };
		}

		protected override void Tick(World world)
		{
			if (world.LocalPlayer != null &&
				world.LocalPlayer.WinState != WinState.Undefined)
				world.CancelInputMode();
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }
		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world) { yield break; }

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			mi.Button = MouseButton.Left;
			return OrderInner(world, mi).Any()
				? "upgrade" : "upgrade-blocked";
		}
	}
}
