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

namespace OpenRA.Mods.CA.Widgets
{
	public class ContainerWithTooltipWidget : Widget
	{
		public readonly string TooltipContainer;
		public readonly string TooltipTemplate = "SIMPLE_TOOLTIP_WITH_DESC";

		protected Lazy<TooltipContainerWidget> tooltipContainer;

		public string TooltipText;
		public Func<string> GetTooltipText;

		public string TooltipDesc;
		public Func<string> GetTooltipDesc;

		protected readonly Ruleset ModRules;

		[ObjectCreator.UseCtor]
		public ContainerWithTooltipWidget(ModData modData)
		{
			ModRules = modData.DefaultRules;

			GetTooltipText = () => TooltipText;
			GetTooltipDesc = () => TooltipDesc;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		protected ContainerWithTooltipWidget(ContainerWithTooltipWidget other)
			: base(other)
		{
			ModRules = other.ModRules;

			TooltipTemplate = other.TooltipTemplate;
			TooltipText = other.TooltipText;
			GetTooltipText = other.GetTooltipText;
			TooltipDesc = other.TooltipDesc;
			GetTooltipDesc = other.GetTooltipDesc;
			TooltipContainer = other.TooltipContainer;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null)
				return;

			if (GetTooltipText != null)
				tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs { { "containerWidget", this }, { "getText", GetTooltipText }, { "getDesc", GetTooltipDesc } });
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null || !tooltipContainer.IsValueCreated)
				return;

			tooltipContainer.Value.RemoveTooltip();
		}

		public override Widget Clone() { return new ContainerWithTooltipWidget(this); }
	}
}
