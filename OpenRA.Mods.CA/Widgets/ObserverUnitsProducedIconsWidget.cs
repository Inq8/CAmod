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
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ObserverUnitsProducedIconsWidget : Widget
	{
		public Func<Player> GetPlayer;
		readonly World world;
		readonly WorldRenderer worldRenderer;

		public int IconWidth = 32;
		public int IconHeight = 24;
		public int IconSpacing = 1;

		readonly float2 iconSize;
		public int MinWidth = 240;

		public ArmyUnit TooltipUnit { get; private set; }
		public Func<ArmyUnit> GetTooltipUnit;
		public string TooltipDesc { get; private set; }
		public Func<string> GetTooltipDesc;

		public readonly string TooltipTemplate = "ARMY_TOOLTIP_CA";
		public readonly string TooltipContainer;

		readonly Lazy<TooltipContainerWidget> tooltipContainer;
		readonly List<ArmyIcon> armyIcons = new();

		readonly CachedTransform<Player, ProductionTracker> tracker = new(player => player.PlayerActor.TraitOrDefault<ProductionTracker>());

		IEnumerable<(ArmyUnit, ProductionTrackerUnitValueItem)> unitsProduced;
		int lastTotalValue;

		int lastIconIdx;
		int currentTooltipToken;

		[ObjectCreator.UseCtor]
		public ObserverUnitsProducedIconsWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;

			GetTooltipUnit = () => TooltipUnit;
			GetTooltipDesc = () => TooltipDesc;

			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		protected ObserverUnitsProducedIconsWidget(ObserverUnitsProducedIconsWidget other)
			: base(other)
		{
			GetPlayer = other.GetPlayer;
			world = other.world;
			worldRenderer = other.worldRenderer;

			IconWidth = other.IconWidth;
			IconHeight = other.IconHeight;
			IconSpacing = other.IconSpacing;
			iconSize = new float2(IconWidth, IconHeight);

			MinWidth = other.MinWidth;

			TooltipUnit = other.TooltipUnit;
			GetTooltipUnit = () => TooltipUnit;
			TooltipDesc = other.TooltipDesc;
			GetTooltipDesc = () => TooltipDesc;

			TooltipTemplate = other.TooltipTemplate;
			TooltipContainer = other.TooltipContainer;

			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		public override void Draw()
		{
			armyIcons.Clear();

			var player = GetPlayer();
			if (player == null)
				return;

			var productionTracker = tracker.Update(player);

			if (lastTotalValue != productionTracker.TotalValue)
			{
				unitsProduced = UpdateUnitsProduced(productionTracker, player);
			}

			Game.Renderer.EnableAntialiasingFilter();

			var queueCol = 0;

			if (unitsProduced != null)
			{
				foreach (var uv in unitsProduced)
				{
					var unit = uv.Item1;

					var icon = unit.Icon;
					var topLeftOffset = new int2(queueCol * (IconWidth + IconSpacing), 0);

					var iconTopLeft = RenderOrigin + topLeftOffset;
					var centerPosition = iconTopLeft;

					var palette = unit.IconPaletteIsPlayerPalette ? unit.IconPalette + player.InternalName : unit.IconPalette;
					WidgetUtils.DrawSpriteCentered(icon.Image, worldRenderer.Palette(palette), centerPosition + 0.5f * iconSize, 0.5f);

					armyIcons.Add(new ArmyIcon
					{
						Bounds = new Rectangle(iconTopLeft.X, iconTopLeft.Y, (int)iconSize.X, (int)iconSize.Y),
						Unit = unit,
						Value = uv.Item2.Value,
						Count = uv.Item2.Count
					});

					queueCol++;
				}
			}

			var newWidth = Math.Max(queueCol * (IconWidth + IconSpacing), MinWidth);
			if (newWidth != Bounds.Width)
			{
				var wasInBounds = EventBounds.Contains(Viewport.LastMousePos);
				Bounds.Width = newWidth;
				var isInBounds = EventBounds.Contains(Viewport.LastMousePos);

				// HACK: Ui.MouseOverWidget is normally only updated when the mouse moves
				// Call ResetTooltips to force a fake mouse movement so the checks in Tick will work properly
				if (wasInBounds != isInBounds)
					Game.RunAfterTick(Ui.ResetTooltips);
			}

			Game.Renderer.DisableAntialiasingFilter();

			var parentWidth = Bounds.X + Bounds.Width;
			Parent.Bounds.Width = parentWidth;

			var gradient = Parent.Get<GradientColorBlockWidget>("PLAYER_GRADIENT");

			var offset = gradient.Bounds.X - Bounds.X;
			var gradientWidth = Math.Max(MinWidth - offset, queueCol * (IconWidth + IconSpacing));

			gradient.Bounds.Width = gradientWidth;
			var widestChildWidth = Parent.Parent.Children.Max(x => x.Bounds.Width);

			Parent.Parent.Bounds.Width = Math.Max(25 + widestChildWidth, Bounds.Left + MinWidth);
		}

		IEnumerable<(ArmyUnit, ProductionTrackerUnitValueItem)> UpdateUnitsProduced(ProductionTracker productionTracker, Player player)
		{
			lastTotalValue = productionTracker.TotalValue;

			return productionTracker.UnitValues
				.OrderByDescending(u => u.Value.Value)
				.Take(12)
				.Select(u => (new ArmyUnit(world.Map.Rules.Actors[u.Key], player), u.Value));
		}

		public override Widget Clone()
		{
			return new ObserverUnitsProducedIconsWidget(this);
		}

		public override void Tick()
		{
			if (TooltipContainer == null)
				return;

			if (Ui.MouseOverWidget != this)
			{
				if (TooltipUnit != null)
				{
					tooltipContainer.Value.RemoveTooltip(currentTooltipToken);
					lastIconIdx = 0;
					TooltipUnit = null;
					TooltipDesc = null;
				}

				return;
			}

			if (TooltipUnit != null && lastIconIdx < armyIcons.Count)
			{
				var armyIcon = armyIcons[lastIconIdx];
				if (armyIcon.Unit.ActorInfo == TooltipUnit.ActorInfo && armyIcon.Bounds.Contains(Viewport.LastMousePos))
					return;
			}

			for (var i = 0; i < armyIcons.Count; i++)
			{
				var armyIcon = armyIcons[i];
				if (!armyIcon.Bounds.Contains(Viewport.LastMousePos))
					continue;

				lastIconIdx = i;
				TooltipUnit = armyIcon.Unit;
				TooltipDesc = $"x{armyIcon.Count} (${armyIcon.Value})";
				currentTooltipToken = tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs { { "getTooltipUnit", GetTooltipUnit }, { "getDesc", GetTooltipDesc } });

				return;
			}

			TooltipUnit = null;
			TooltipDesc = null;
		}

		sealed class UnitProduced
		{
			public ArmyUnit Unit { get; set; }
			public int Value { get; set; }
			public int Count { get; set; }
		}

		sealed class ArmyIcon
		{
			public Rectangle Bounds { get; set; }
			public ArmyUnit Unit { get; set; }
			public int Value { get; set; }
			public int Count { get; set; }
		}
	}
}
