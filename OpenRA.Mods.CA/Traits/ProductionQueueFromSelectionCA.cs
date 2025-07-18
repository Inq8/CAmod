#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.CA.Widgets;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.World)]
	class ProductionQueueFromSelectionCAInfo : TraitInfo
	{
		public readonly string ProductionTabsWidget = null;
		public readonly string ProductionPaletteWidget = null;

		public override object Create(ActorInitializer init) { return new ProductionQueueFromSelection(init.World, this); }
	}

	class ProductionQueueFromSelection : INotifySelection
	{
		readonly World world;
		readonly Lazy<ProductionTabsCAWidget> tabsWidget;
		readonly Lazy<ProductionPaletteWidget> paletteWidget;

		public ProductionQueueFromSelection(World world, ProductionQueueFromSelectionCAInfo info)
		{
			this.world = world;

			tabsWidget = Exts.Lazy(() => Ui.Root.GetOrNull(info.ProductionTabsWidget) as ProductionTabsCAWidget);
			paletteWidget = Exts.Lazy(() => Ui.Root.GetOrNull(info.ProductionPaletteWidget) as ProductionPaletteWidget);
		}

		void INotifySelection.SelectionChanged()
		{
			// Disable for spectators
			if (world.LocalPlayer == null)
				return;

			// Queue-per-actor
			var queue = world.Selection.Actors
				.Where(a => a.IsInWorld && a.World.LocalPlayer == a.Owner)
				.SelectMany(a => a.TraitsImplementing<ProductionQueue>())
				.FirstOrDefault(q => q.Enabled);

			// Linked producer queue
			if (queue == null)
			{
				queue = world.Selection.Actors
					.Where(a => a.IsInWorld && a.World.LocalPlayer == a.Owner)
					.SelectMany(a => a.TraitsImplementing<LinkedProducerTarget>())
					.Where(t => t.Source?.Actor != null)
					.Select(t => t.Source.Actor)
					.SelectMany(s => s.TraitsImplementing<ProductionQueue>())
					.FirstOrDefault(q => q.Enabled);
			}

			// Queue-per-player
			if (queue == null)
			{
				var types = world.Selection.Actors.Where(a => a.IsInWorld && a.World.LocalPlayer == a.Owner)
					.SelectMany(a => a.TraitsImplementing<Production>().Where(p => !p.IsTraitDisabled))
					.SelectMany(t => t.Info.Produces)
					.Concat(world.Selection.Actors.Where(a => a.IsInWorld && a.World.LocalPlayer == a.Owner)
					.SelectMany(a => a.TraitsImplementing<LinkedProducerTarget>())
					.SelectMany(t => t.Types));

				queue = world.LocalPlayer.PlayerActor.TraitsImplementing<ProductionQueue>()
					.FirstOrDefault(q => q.Enabled && types.Contains(q.Info.Type));
			}

			if (queue == null || !queue.AnyItemsToBuild())
				return;

			if (tabsWidget.Value != null)
				tabsWidget.Value.CurrentQueue = queue;
			else if (paletteWidget.Value != null)
				paletteWidget.Value.CurrentQueue = queue;
		}
	}
}
