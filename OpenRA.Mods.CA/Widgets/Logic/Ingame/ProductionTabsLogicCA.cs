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
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public class ProductionTabsLogicCA : ChromeLogic
	{
		readonly ProductionTabsCAWidget tabs;
		readonly World world;

		void SetupProductionGroupButton(ProductionTypeButtonWidget button)
		{
			if (button == null)
				return;

			Action<bool> selectTab = reverse =>
			{
				if (tabs.QueueGroup == button.ProductionGroup)
					tabs.SelectNextTab(reverse);
				else
					tabs.QueueGroup = button.ProductionGroup;

				tabs.PickUpCompletedBuilding();
			};

			// hard coded to always enable upgrades tab if structures exist to build them, even if all have been acquired already
			button.IsDisabled = () => !tabs.Groups[button.ProductionGroup].Tabs.Any(t => t.Queue.BuildableItems().Any() || (t.Queue.Info.Type == "Upgrade" && t.Queue.AllItems().Any()));
			button.OnMouseUp = mi => selectTab(mi.Modifiers.HasModifier(Modifiers.Shift));
			button.OnKeyPress = e => selectTab(e.Modifiers.HasModifier(Modifiers.Shift));
			button.IsHighlighted = () => tabs.QueueGroup == button.ProductionGroup;

			var chromeName = button.ProductionGroup.ToLowerInvariant();
			var icon = button.Get<ImageWidget>("ICON");
			icon.GetImageName = () => button.IsDisabled() ? chromeName + "-disabled" :
				tabs.Groups[button.ProductionGroup].Alert ? chromeName + "-alert" : chromeName;
		}

		[ObjectCreator.UseCtor]
		public ProductionTabsLogicCA(Widget widget, World world)
		{
			this.world = world;
			tabs = widget.Get<ProductionTabsCAWidget>("PRODUCTION_TABS");
			world.ActorAdded += tabs.ActorChanged;
			world.ActorRemoved += tabs.ActorChanged;
			Game.BeforeGameStart += UnregisterEvents;

			var typesContainer = Ui.Root.Get(tabs.TypesContainer);
			foreach (var i in typesContainer.Children)
				SetupProductionGroupButton(i as ProductionTypeButtonWidget);

			var background = Ui.Root.GetOrNull(tabs.BackgroundContainer);
			if (background != null)
			{
				var palette = tabs.Parent.Get<ProductionPaletteWidget>(tabs.PaletteWidget);
				var icontemplate = background.Get("ICON_TEMPLATE");

				Action<int, int> updateBackground = (oldCount, newCount) =>
				{
					background.RemoveChildren();

					for (var i = 0; i < newCount; i++)
					{
						var x = i % palette.Columns;
						var y = i / palette.Columns;

						var bg = icontemplate.Clone();
						bg.Bounds.X = palette.IconSize.X * x;
						bg.Bounds.Y = palette.IconSize.Y * y;
						background.AddChild(bg);
					}
				};

				palette.OnIconCountChanged += updateBackground;

				// Set the initial palette state
				updateBackground(0, 0);
			}
		}

		void UnregisterEvents()
		{
			Game.BeforeGameStart -= UnregisterEvents;
			world.ActorAdded -= tabs.ActorChanged;
			world.ActorRemoved -= tabs.ActorChanged;
		}
	}
}
