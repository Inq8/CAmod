#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	// Identical to ArmyTooltipLogic, but replaces \n with newlines (only needed where not using fluent)
	public class ArmyTooltipLogicCA : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public ArmyTooltipLogicCA(Widget widget, TooltipContainerWidget tooltipContainer, Func<ArmyUnit> getTooltipUnit)
		{
			widget.IsVisible = () => getTooltipUnit() != null;
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var descLabel = widget.Get<LabelWidget>("DESC");

			var font = Game.Renderer.Fonts[nameLabel.Font];
			var descFont = Game.Renderer.Fonts[descLabel.Font];

			ArmyUnit lastArmyUnit = null;
			var descLabelPadding = descLabel.Bounds.Height;

			tooltipContainer.BeforeRender = () =>
			{
				var armyUnit = getTooltipUnit();

				if (armyUnit == null || armyUnit == lastArmyUnit)
					return;

				var tooltip = armyUnit.TooltipInfo;
				var name = tooltip != null ? FluentProvider.GetMessage(tooltip.Name) : armyUnit.ActorInfo.Name;
				var buildable = armyUnit.BuildableInfo;

				nameLabel.GetText = () => name;
				var nameSize = font.Measure(name);

				var desc = string.IsNullOrEmpty(buildable.Description) ? "" : FluentProvider.GetMessage(buildable.Description).Replace("\\n", "\n");
				descLabel.GetText = () => desc;
				var descSize = descFont.Measure(desc);
				descLabel.Bounds.Width = descSize.X;
				descLabel.Bounds.Height = descSize.Y + descLabelPadding;

				var leftWidth = Math.Max(nameSize.X, descSize.X);

				widget.Bounds.Width = leftWidth + 2 * nameLabel.Bounds.X;

				// Set the bottom margin to match the left margin
				var leftHeight = descLabel.Bounds.Bottom + descLabel.Bounds.X;

				widget.Bounds.Height = leftHeight;

				lastArmyUnit = armyUnit;
			};
		}
	}
}
