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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public class SelectionTooltipLogic : ChromeLogic
	{
		readonly World world;
		int selectionHash;
		readonly Widget widget;
		int originalDescLabelHeight;
		int iconMargin;

		[ObjectCreator.UseCtor]
		public SelectionTooltipLogic(Widget widget, World world)
		{
			this.world = world;
			this.widget = widget;
			widget.IsVisible = () => Game.Settings.Game.SelectionTooltip && !Game.Settings.Debug.PerfText;
			originalDescLabelHeight = -1;
			iconMargin = -1;
		}

		public override void Tick()
		{
			if (selectionHash == world.Selection.Hash)
				return;

			UpdateTooltip();
			selectionHash = world.Selection.Hash;
		}

		void HideTooltip()
		{
			widget.Bounds.X = Game.Renderer.Resolution.Width;
			widget.Bounds.Y = Game.Renderer.Resolution.Height;
		}

		void UpdateTooltip()
		{
			var selectedActors = world.Selection.Actors;
			var numSelectedActors = selectedActors.Count();
			var uniqueActors = new HashSet<string>();

			foreach (var a in selectedActors)
			{
				uniqueActors.Add(a.Info.Name);
			}

			if (uniqueActors.Count != 1)
			{
				HideTooltip();
				return;
			}

			var actor = selectedActors.First();
			if (actor == null || actor.Info == null || actor.IsDead || !actor.IsInWorld || actor.Disposed)
			{
				HideTooltip();
				return;
			}

			var mapRules = world.Map.Rules;
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var armorTypeLabel = widget.Get<LabelWidget>("ARMORTYPE");
			var armorTypeIcon = widget.Get<ImageWidget>("ARMORTYPE_ICON");
			var costLabel = widget.Get<LabelWidget>("COST");
			var costIcon = widget.Get<ImageWidget>("COST_ICON");
			var descLabel = widget.Get<LabelWidget>("DESC");
			var strengthsLabel = widget.Get<LabelWidget>("STRENGTHS");
			var weaknessesLabel = widget.Get<LabelWidget>("WEAKNESSES");
			var attributesLabel = widget.Get<LabelWidget>("ATTRIBUTES");
			var font = Game.Renderer.Fonts[nameLabel.Font];
			var descFont = Game.Renderer.Fonts[descLabel.Font];
			var formatBuildTime = new CachedTransform<int, string>(time => WidgetUtils.FormatTime(time, world.Timestep));

			if (originalDescLabelHeight == -1)
				originalDescLabelHeight = descLabel.Bounds.Height;

			if (iconMargin == -1)
				iconMargin = armorTypeIcon.Bounds.X;

			descLabel.Bounds.Height = originalDescLabelHeight;

			var descLabelPadding = descLabel.Bounds.Height;

			var actorInfo = actor.Info;
			var tooltip = actor.TraitsImplementing<Tooltip>().FirstOrDefault(t => !t.IsTraitDisabled);
			var tooltipInfo = tooltip != null ? tooltip.Info : null;
			var tooltipExtras = actor.TraitsImplementing<TooltipExtras>().FirstOrDefault(t => !t.IsTraitDisabled);
			var tooltipExtrasInfo = tooltipExtras != null ? tooltipExtras.Info : null;

			if (tooltipExtrasInfo != null && tooltipExtrasInfo.FakeActor != null)
			{
				var o = actor.Owner;
				var stance = o == null || world.RenderPlayer == null ? PlayerRelationship.None : o.RelationshipWith(world.RenderPlayer);
				if (stance == PlayerRelationship.Enemy)
				{
					actorInfo = mapRules.Actors[tooltipExtras.Info.FakeActor];
					tooltipExtrasInfo = actorInfo.TraitInfoOrDefault<TooltipExtrasInfo>();
					tooltipInfo = actorInfo.TraitInfoOrDefault<TooltipInfo>();
				}
			}

			// Name
			var name = tooltipInfo != null ? tooltipInfo.Name : char.ToUpper(actorInfo.Name[0]) + actorInfo.Name[1..];

			if (numSelectedActors > 1)
				name = numSelectedActors.ToString() + "x " + name;

			nameLabel.Text = name;
			var nameSize = font.Measure(name);

			// Armor type
			armorTypeLabel = GetArmorTypeLabel(armorTypeLabel, actorInfo);
			var armorTypeSize = armorTypeLabel.Text != "" ? font.Measure(armorTypeLabel.Text) : new int2(0, 0);
			armorTypeIcon.Visible = armorTypeSize.Y > 0;
			armorTypeLabel.Bounds.Y = armorTypeIcon.Bounds.Y;

			// Cost
			var cost = 0;
			var valued = actorInfo.TraitInfoOrDefault<ValuedInfo>();
			if (valued != null)
				cost = valued.Cost;

			costLabel.Visible = costIcon.Visible = cost > 0;
			costLabel.Text = cost.ToString();
			costLabel.GetColor = () => Color.White;
			var costSize = font.Measure(costLabel.Text);

			// Strengths, weaknesses & attributes
			if (tooltipExtrasInfo != null)
			{
				strengthsLabel.Text = tooltipExtrasInfo.Strengths.Replace("\\n", "\n");
				weaknessesLabel.Text = tooltipExtrasInfo.Weaknesses.Replace("\\n", "\n");
				attributesLabel.Text = tooltipExtrasInfo.Attributes.Replace("\\n", "\n");
				descLabel.Text = tooltipExtrasInfo.Description.Replace("\\n", "\n");
			}
			else
			{
				strengthsLabel.Text = "";
				weaknessesLabel.Text = "";
				attributesLabel.Text = "";
				descLabel.Text = "";
			}

			var extrasSpacing = descLabel.Bounds.X / 2;

			// Description
			if (descLabel.Text == "")
			{
				var buildable = actorInfo.TraitInfoOrDefault<BuildableInfo>();

				if (buildable != null && buildable.Description != null)
				{
					descLabel.Text = buildable.Description.Replace("\\n", "\n");
				}
			}

			var descSize = descLabel.Text != "" ? descFont.Measure(descLabel.Text) : new int2(0, 0);

			descLabel.Bounds.Width = descSize.X;
			descLabel.Bounds.Height = descSize.Y;

			var strengthsSize = strengthsLabel.Text != "" ? descFont.Measure(strengthsLabel.Text) : new int2(0, 0);
			var weaknessesSize = weaknessesLabel.Text != "" ? descFont.Measure(weaknessesLabel.Text) : new int2(0, 0);
			var attributesSize = attributesLabel.Text != "" ? descFont.Measure(attributesLabel.Text) : new int2(0, 0);

			strengthsLabel.Bounds.Y = descLabel.Bounds.Bottom + extrasSpacing;
			weaknessesLabel.Bounds.Y = descLabel.Bounds.Bottom + strengthsSize.Y + extrasSpacing;
			attributesLabel.Bounds.Y = descLabel.Bounds.Bottom + strengthsSize.Y + weaknessesSize.Y + extrasSpacing;

			descLabel.Bounds.Height += strengthsSize.Y + weaknessesSize.Y + attributesSize.Y + descLabelPadding + extrasSpacing;

			var leftWidth = new[] { nameSize.X, descSize.X, strengthsSize.X, weaknessesSize.X, attributesSize.X }.Aggregate(Math.Max) + 5;
			var rightWidth = new[] { armorTypeSize.X, costSize.X }.Aggregate(Math.Max);

			armorTypeIcon.Bounds.X = costIcon.Bounds.X = leftWidth + 2 * nameLabel.Bounds.X;
			armorTypeLabel.Bounds.X = costLabel.Bounds.X = armorTypeIcon.Bounds.Right + iconMargin;
			widget.Bounds.Width = leftWidth + rightWidth + 3 * nameLabel.Bounds.X + armorTypeIcon.Bounds.Width + iconMargin;

			// Set the bottom margin to match the left margin
			var leftHeight = descLabel.Bounds.Bottom + descLabel.Bounds.X;

			// Set the bottom margin to match the top margin
			var rightHeight = armorTypeIcon.Bounds.Bottom;
			widget.Bounds.Height = Math.Max(leftHeight, rightHeight);
			widget.Bounds.X = Game.Renderer.Resolution.Width - widget.Bounds.Width - 12;
			widget.Bounds.Y = Game.Renderer.Resolution.Height - widget.Bounds.Height - 12;
		}

		LabelWidget GetArmorTypeLabel(LabelWidget armorTypeLabel, ActorInfo actor)
		{
			var armor = actor.TraitInfos<ArmorInfo>().FirstOrDefault();
			armorTypeLabel.Text = armor != null ? armor.Type : "";

			// Hard coded, specific to CA - find a better way to set user-friendly names and colors for armor types
			switch (armorTypeLabel.Text)
			{
				case "None":
					armorTypeLabel.Text = "Infantry";
					armorTypeLabel.TextColor = Color.ForestGreen;
					break;

				case "Light":
					armorTypeLabel.TextColor = Color.MediumPurple;
					break;

				case "Heavy":
					armorTypeLabel.TextColor = Color.Firebrick;
					break;

				case "Concrete":
					armorTypeLabel.Text = "Defense";
					armorTypeLabel.TextColor = Color.RoyalBlue;
					break;

				case "Wood":
					armorTypeLabel.Text = "Building";
					armorTypeLabel.TextColor = Color.Peru;
					break;

				case "Brick":
					armorTypeLabel.Text = "Wall";
					armorTypeLabel.TextColor = Color.RosyBrown;
					break;

				case "Aircraft":
					armorTypeLabel.TextColor = Color.SkyBlue;
					break;

				default:
					armorTypeLabel.Text = "";
					break;
			}

			return armorTypeLabel;
		}
	}
}
