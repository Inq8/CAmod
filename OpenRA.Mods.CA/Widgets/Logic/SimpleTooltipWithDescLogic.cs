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
	public class SimpleTooltipWithDescLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public SimpleTooltipWithDescLogic(Widget widget, ContainerWithTooltipWidget containerWidget, Func<string> getText, Func<string> getDesc)
		{
			var label = widget.Get<LabelWidget>("LABEL");
			var font = Game.Renderer.Fonts[label.Font];
			var text = containerWidget.GetTooltipText();
			var labelWidth = font.Measure(text).X;

			label.GetText = () => text;
			label.Bounds.Width = labelWidth;
			widget.Bounds.Width = 2 * label.Bounds.X + labelWidth;

			var desc = containerWidget.GetTooltipDesc();
			if (!string.IsNullOrEmpty(desc))
			{
				var descTemplate = widget.Get<LabelWidget>("DESC");
				widget.RemoveChild(descTemplate);

				var descFont = Game.Renderer.Fonts[descTemplate.Font];
				var descWidth = 0;
				var descOffset = descTemplate.Bounds.Y;
				foreach (var line in desc.Split(new[] { "\\n" }, StringSplitOptions.None))
				{
					descWidth = Math.Max(descWidth, descFont.Measure(line).X);
					var lineLabel = (LabelWidget)descTemplate.Clone();
					lineLabel.GetText = () => line;
					lineLabel.Bounds.Y = descOffset;
					widget.AddChild(lineLabel);
					descOffset += descTemplate.Bounds.Height;
				}

				widget.Bounds.Width = Math.Max(widget.Bounds.Width, descTemplate.Bounds.X * 2 + descWidth);
				widget.Bounds.Height += descOffset - descTemplate.Bounds.Y + descTemplate.Bounds.X;
			}
		}
	}
}
