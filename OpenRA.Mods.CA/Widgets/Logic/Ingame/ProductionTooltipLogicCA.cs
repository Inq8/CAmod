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
using System.Globalization;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public class ProductionTooltipLogicCA : ChromeLogic
	{
		[FluentReference("prequisites")]
		const string Requires = "label-requires";

		[ObjectCreator.UseCtor]
		public ProductionTooltipLogicCA(Widget widget, TooltipContainerWidget tooltipContainer, Player player, Func<ProductionIcon> getTooltipIcon)
		{
			var world = player.World;
			var mapRules = world.Map.Rules;
			var pm = player.PlayerActor.TraitOrDefault<PowerManager>();
			var pr = player.PlayerActor.Trait<PlayerResources>();

			widget.IsVisible = () => getTooltipIcon() != null && getTooltipIcon().Actor != null;
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var hotkeyLabel = widget.Get<LabelWidget>("HOTKEY");
			var requiresLabel = widget.Get<LabelWidget>("REQUIRES");
			var powerLabel = widget.Get<LabelWidget>("POWER");
			var powerIcon = widget.Get<ImageWidget>("POWER_ICON");
			var armorTypeLabel = widget.Get<LabelWidget>("ARMORTYPE");
			var armorTypeIcon = widget.Get<ImageWidget>("ARMORTYPE_ICON");
			var timeLabel = widget.Get<LabelWidget>("TIME");
			var timeIcon = widget.Get<ImageWidget>("TIME_ICON");
			var costLabel = widget.Get<LabelWidget>("COST");
			var costIcon = widget.Get<ImageWidget>("COST_ICON");
			var descLabel = widget.Get<LabelWidget>("DESC");
			var strengthsLabel = widget.Get<LabelWidget>("STRENGTHS");
			var weaknessesLabel = widget.Get<LabelWidget>("WEAKNESSES");
			var attributesLabel = widget.Get<LabelWidget>("ATTRIBUTES");

			var iconMargin = timeIcon.Bounds.X;

			var font = Game.Renderer.Fonts[nameLabel.Font];
			var descFont = Game.Renderer.Fonts[descLabel.Font];
			var requiresFont = Game.Renderer.Fonts[requiresLabel.Font];
			var formatBuildTime = new CachedTransform<int, string>(time => WidgetUtils.FormatTime(time, world.Timestep));
			var requiresFormat = requiresLabel.Text;

			ActorInfo lastActor = null;
			Hotkey lastHotkey = Hotkey.Invalid;
			var lastPowerState = pm == null ? PowerState.Normal : pm.PowerState;
			var descLabelY = descLabel.Bounds.Y;
			var descLabelPadding = descLabel.Bounds.Height;

			tooltipContainer.BeforeRender = () =>
			{
				var tooltipIcon = getTooltipIcon();
				if (tooltipIcon == null)
					return;

				var actor = tooltipIcon.Actor;
				if (actor == null)
					return;

				var hotkey = tooltipIcon.Hotkey != null ? tooltipIcon.Hotkey.GetValue() : Hotkey.Invalid;
				if (actor == lastActor && hotkey == lastHotkey && (pm == null || pm.PowerState == lastPowerState))
					return;

				var tooltip = actor.TraitInfos<TooltipInfo>().FirstOrDefault(info => info.EnabledByDefault);
				var name = tooltip != null ? FluentProvider.GetMessage(tooltip.Name) : actor.Name;
				var buildable = actor.TraitInfo<BuildableInfo>();

				var cost = 0;
				if (tooltipIcon.ProductionQueue != null)
					cost = tooltipIcon.ProductionQueue.GetProductionCost(actor);
				else
				{
					var valued = actor.TraitInfoOrDefault<ValuedInfo>();
					if (valued != null)
						cost = valued.Cost;
				}

				var maxLeftWidth = 300;

				nameLabel.GetText = () => name;

				var nameSize = font.Measure(name);
				var hotkeyWidth = 0;
				hotkeyLabel.Visible = hotkey.IsValid();

				if (hotkeyLabel.Visible)
				{
					var hotkeyText = $"({hotkey.DisplayString()})";

					hotkeyWidth = font.Measure(hotkeyText).X + 2 * nameLabel.Bounds.X;
					hotkeyLabel.GetText = () => hotkeyText;
					hotkeyLabel.Bounds.X = nameSize.X + 2 * nameLabel.Bounds.X;
				}

				var prereqs = buildable.Prerequisites
					.Select(a => ActorName(mapRules, a))
					.Where(s => !s.StartsWith('~') && !s.StartsWith('!'))
					.ToList();

				var requiresSize = int2.Zero;
				if (prereqs.Count > 0)
				{
					var requiresText = FluentProvider.GetMessage(Requires, "prerequisites", prereqs.JoinWith(", "));
					requiresLabel.GetText = () => requiresText;
					requiresSize = requiresFont.Measure(requiresText);
					requiresLabel.Visible = true;
					descLabel.Bounds.Y = descLabelY + requiresLabel.Bounds.Height + (descLabel.Bounds.X / 2);
				}
				else
				{
					requiresLabel.Visible = false;
					descLabel.Bounds.Y = descLabelY;
				}

				var buildTime = tooltipIcon.ProductionQueue == null ? 0 : tooltipIcon.ProductionQueue.GetBuildTime(actor, buildable);
				var timeModifier = pm != null && pm.PowerState != PowerState.Normal ? tooltipIcon.ProductionQueue.Info.LowPowerModifier : 100;

				var timeText = formatBuildTime.Update(buildTime * timeModifier / 100);
				timeLabel.GetText = () => timeText;
				timeLabel.TextColor =
					(pm != null && pm.PowerState != PowerState.Normal && tooltipIcon.ProductionQueue.Info.LowPowerModifier > 100)
						? Color.Red
						: Color.White;
				var timeSize = font.Measure(timeText);

				var costText = cost.ToString(NumberFormatInfo.CurrentInfo);
				costLabel.GetText = () => costText;
				costLabel.GetColor = () => pr.GetCashAndResources() >= cost ? Color.White : Color.Red;
				var costSize = font.Measure(costText);

				var powerSize = new int2(0, 0);
				var power = 0;

				if (pm != null)
				{
					power = actor.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(i => i.Amount);
					var powerText = power.ToString(NumberFormatInfo.CurrentInfo);
					powerLabel.GetText = () => powerText;
					powerLabel.GetColor = () => (pm.PowerProvided - pm.PowerDrained >= -power || power > 0)
						? Color.White : Color.Red;
					powerLabel.Visible = power != 0;
					powerIcon.Visible = power != 0;
					powerSize = font.Measure(powerText);
				}

				armorTypeLabel = SelectionTooltipLogic.GetArmorTypeLabel(armorTypeLabel, actor);
				var armorTypeSize = armorTypeLabel.GetText() != "" ? font.Measure(armorTypeLabel.GetText()) : new int2(0, 0);
				armorTypeIcon.Visible = armorTypeSize.Y > 0;

				if (armorTypeLabel.Text != "" && power != 0)
					armorTypeIcon.Bounds.Y = armorTypeLabel.Bounds.Y = powerLabel.Bounds.Bottom;
				else
					armorTypeIcon.Bounds.Y = armorTypeLabel.Bounds.Y = timeLabel.Bounds.Bottom;

				var extrasSpacing = descLabel.Bounds.X / 2;

				var desc = string.IsNullOrEmpty(buildable.Description) ? "" : FluentProvider.GetMessage(buildable.Description);
				desc = WidgetUtilsCA.WrapTextWithIndent(desc.Replace("\\n", "\n"), maxLeftWidth, descFont);
				descLabel.GetText = () => desc;

				var descSize = descFont.Measure(desc);
				descLabel.Bounds.Width = descSize.X;
				descLabel.Bounds.Height = descSize.Y;

				var tooltipExtras = actor.TraitInfos<TooltipExtrasInfo>().FirstOrDefault(info => info.IsStandard);

				var strengthsLabelText = "";
				var weaknessesLabelText = "";
				var attributesLabelText = "";

				if (tooltipExtras != null)
				{
					strengthsLabelText = WidgetUtilsCA.WrapTextWithIndent(tooltipExtras.Strengths.Replace("\\n", "\n"), maxLeftWidth, descFont, 6);
					weaknessesLabelText = WidgetUtilsCA.WrapTextWithIndent(tooltipExtras.Weaknesses.Replace("\\n", "\n"), maxLeftWidth, descFont, 6);
					attributesLabelText = WidgetUtilsCA.WrapTextWithIndent(tooltipExtras.Attributes.Replace("\\n", "\n"), maxLeftWidth, descFont, 6);
				}

				strengthsLabel.GetText = () => strengthsLabelText;
				weaknessesLabel.GetText = () => weaknessesLabelText;
				attributesLabel.GetText = () => attributesLabelText;
				var strengthsSize = strengthsLabelText != "" ? descFont.Measure(strengthsLabelText) : new int2(0, 0);
				var weaknessesSize = weaknessesLabelText != "" ? descFont.Measure(weaknessesLabelText) : new int2(0, 0);
				var attributesSize = attributesLabelText != "" ? descFont.Measure(attributesLabelText) : new int2(0, 0);

				strengthsLabel.Bounds.Y = descLabel.Bounds.Bottom + extrasSpacing;
				weaknessesLabel.Bounds.Y = descLabel.Bounds.Bottom + strengthsSize.Y + extrasSpacing;
				attributesLabel.Bounds.Y = descLabel.Bounds.Bottom + strengthsSize.Y + weaknessesSize.Y + extrasSpacing;

				descLabel.Bounds.Height += strengthsSize.Y + weaknessesSize.Y + attributesSize.Y + descLabelPadding + extrasSpacing;

				var leftWidth = new[] { nameSize.X + hotkeyWidth, requiresSize.X, descSize.X, strengthsSize.X, weaknessesSize.X, attributesSize.X }.Aggregate(Math.Max);
				var rightWidth = new[] { powerSize.X, timeSize.X, costSize.X, armorTypeSize.X }.Aggregate(Math.Max);

				timeIcon.Bounds.X = powerIcon.Bounds.X = costIcon.Bounds.X = armorTypeIcon.Bounds.X = leftWidth + 2 * nameLabel.Bounds.X;
				timeLabel.Bounds.X = powerLabel.Bounds.X = costLabel.Bounds.X = armorTypeLabel.Bounds.X = timeIcon.Bounds.Right + iconMargin;
				widget.Bounds.Width = leftWidth + rightWidth + 3 * nameLabel.Bounds.X + timeIcon.Bounds.Width + iconMargin;

				// Set the bottom margin to match the left margin
				var leftHeight = descLabel.Bounds.Bottom + descLabel.Bounds.X;

				// Set the bottom margin to match the top margin
				var rightHeight = armorTypeIcon.Bounds.Bottom + costIcon.Bounds.Top;

				widget.Bounds.Height = Math.Max(leftHeight, rightHeight);

				lastActor = actor;
				lastHotkey = hotkey;
				if (pm != null)
					lastPowerState = pm.PowerState;
			};
		}

		static string ActorName(Ruleset rules, string a)
		{
			if (rules.Actors.TryGetValue(a.ToLowerInvariant(), out var ai))
			{
				var actorTooltip = ai.TraitInfos<TooltipInfo>().FirstOrDefault(info => info.EnabledByDefault);
				if (actorTooltip != null)
					return FluentProvider.GetMessage(actorTooltip.Name);
			}

			return a;
		}
	}
}
