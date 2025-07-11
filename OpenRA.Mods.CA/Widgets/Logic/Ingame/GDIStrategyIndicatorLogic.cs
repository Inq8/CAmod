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
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	class GDIStrategyIndicatorLogic : ChromeLogic
	{
		const string CountType = "StrategyLevel";
		const string DisabledImage = "disabled";
		readonly CountManager countManager;
		private string levelImageName;

		[ObjectCreator.UseCtor]
		public GDIStrategyIndicatorLogic(Widget widget, World world)
		{
			var container = widget.Get<ContainerWidget>("GDI_STRATEGY");
			var levelImage = container.Get<ImageWidget>("GDI_STRATEGY_LEVEL");

			if (world.LocalPlayer.Faction.Side != "GDI")
			{
				levelImage.GetImageName = () => DisabledImage;
				levelImage.IsVisible = () => false;
				return;
			}

			countManager = world.LocalPlayer.PlayerActor.Trait<CountManager>();
			UpdateLevelImageName(0);

			countManager.Incremented += HandleIncremented;
			countManager.Decremented += HandleDecremented;

			levelImage.GetImageName = () => levelImageName;
			levelImage.IsVisible = () => true;
		}

		private void HandleIncremented(string type, int newCount)
		{
			if (type != CountType)
				return;

			UpdateLevelImageName(newCount);
		}

		private void HandleDecremented(string type, int newCount)
		{
			if (type != CountType)
				return;

			UpdateLevelImageName(newCount);
		}

		private void UpdateLevelImageName(int newCount)
		{
			var count = Math.Min(newCount, 3);
			levelImageName = $"level{count}";
		}
	}
}
