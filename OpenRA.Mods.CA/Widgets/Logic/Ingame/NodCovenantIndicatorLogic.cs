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
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	class NodCovenantIndicatorLogic : ChromeLogic
	{
		[FluentReference("level")]
		const string CovenantLevel = "label-covenant-level";
		const string CovenantDescription = "label-covenant-description";

		const string CountType = "NodCovenant";
		const string DisabledImage = "disabled";

		readonly ProvidesPrerequisitesOnCount counter;

		private string levelImageName;

		[ObjectCreator.UseCtor]
		public NodCovenantIndicatorLogic(Widget widget, World world)
		{
			var container = widget.Get<ContainerWidget>("NOD_COVENANT");
			var levelImage = container.Get<ImageWidget>("NOD_COVENANT_LEVEL");

			if (world.LocalPlayer.Faction.Side != "Nod")
			{
				levelImage.GetImageName = () => DisabledImage;
				levelImage.IsVisible = () => false;
				return;
			}

			counter = world.LocalPlayer.PlayerActor.TraitsImplementing<ProvidesPrerequisitesOnCount>()
				.FirstOrDefault(c => c.Info.Type == CountType);

			if (counter == null)
			{
				levelImage.GetImageName = () => DisabledImage;
				levelImage.IsVisible = () => true;
				return;
			}

			UpdateLevelImageName();

			counter.Incremented += HandleIncremented;

			levelImage.GetImageName = () => levelImageName;
			levelImage.IsVisible = () => true;

			var tooltipTextCached = new CachedTransform<int, string>((CurrentCount) =>
			{
				var tooltip = FluentProvider.GetMessage(CovenantLevel, "level", CurrentCount);
				tooltip += "\n\n";
				tooltip += FluentProvider.GetMessage(CovenantDescription);
				return tooltip;
			});

			levelImage.GetTooltipText = () => tooltipTextCached.Update(Math.Min(counter.CurrentCount, 3));
		}

		private void HandleIncremented()
		{
			UpdateLevelImageName();
		}

		private void UpdateLevelImageName()
		{
			var count = Math.Min(counter.CurrentCount, 3);
			levelImageName = $"level{count}";
		}
	}
}
