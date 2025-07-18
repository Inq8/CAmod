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
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public class AddFactionSuffixLogicCA : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public AddFactionSuffixLogicCA(Widget widget, World world)
		{
			if (world.LocalPlayer == null || world.LocalPlayer.Spectating)
				return;

			if (!ChromeMetrics.TryGet("FactionSuffix-" + world.LocalPlayer.Faction.InternalName, out string faction))
				faction = world.LocalPlayer.Faction.InternalName;
			var suffix = "-" + faction;

			if (widget is ButtonWidget bw)
				bw.Background += suffix;
			else if (widget is ImageWidget iw)
				iw.ImageCollection += suffix;
			else if (widget is BackgroundWidget bgw)
				bgw.Background += suffix;
			else if (widget is TextFieldWidget tfw)
				tfw.Background += suffix;
			else if (widget is ScrollPanelWidget spw)
			{
				spw.Button += suffix;
				spw.Background += suffix;
				spw.ScrollBarBackground += suffix;
				spw.Decorations += suffix;
			}
			else if (widget is ProductionTabsCAWidget ptw)
			{
				ptw.TabButton += suffix;
				ptw.LeftButton += suffix;
				ptw.RightButton += suffix;
				ptw.Background += suffix;
			}
			else
				throw new InvalidOperationException(
					"AddFactionSuffixLogic only supports ButtonWidget, ImageWidget, BackgroundWidget, TextFieldWidget, ScrollPanelWidget and ProductionTabsWidget");
		}
	}
}
