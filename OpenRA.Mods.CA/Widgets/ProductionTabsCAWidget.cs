#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ProductionTabCA
	{
		public string Name;
		public ProductionQueue Queue;
		public Actor Actor;
	}

	public class ProductionTabGroupCA
	{
		public List<ProductionTabCA> Tabs = new List<ProductionTabCA>();
		public string Group;
		public int NextQueueName = 1;
		public bool Alert { get { return Tabs.Any(t => t.Queue.AllQueued().Any(i => i.Done)); } }

		public void Update(IEnumerable<ProductionQueue> allQueues)
		{
			var queues = allQueues.Where(q => q.Info.Group == Group).ToList();
			var tabs = new List<ProductionTabCA>();
			var largestUsedName = 0;

			// Remove stale queues
			foreach (var t in Tabs)
			{
				if (!queues.Contains(t.Queue))
					continue;

				tabs.Add(t);
				queues.Remove(t.Queue);
				largestUsedName = Math.Max(int.Parse(t.Name), largestUsedName);
			}

			NextQueueName = largestUsedName + 1;

			// Add new queues
			foreach (var queue in queues)
				tabs.Add(new ProductionTabCA()
				{
					Name = (NextQueueName++).ToString(),
					Queue = queue,
					Actor = queue.GetType() == typeof(ProductionQueue) ? queue.Actor : null
				});
			Tabs = tabs;
		}
	}

	public class ProductionTabsCAWidget : Widget
	{
		readonly World world;
		readonly WorldRenderer worldRenderer;

		public readonly string PaletteWidget = null;
		public readonly string TypesContainer = null;
		public readonly string BackgroundContainer = null;

		public readonly int TabWidth = 30;
		public readonly int TabSpacing = 0;
		public readonly int ArrowWidth = 20;
		public readonly int MaxTabsVisible = 5;

		public readonly string ClickSound = ChromeMetrics.Get<string>("ClickSound");
		public readonly string ClickDisabledSound = ChromeMetrics.Get<string>("ClickDisabledSound");

		public readonly HotkeyReference PreviousProductionTabKey = new HotkeyReference();
		public readonly HotkeyReference NextProductionTabKey = new HotkeyReference();

		public readonly Dictionary<string, ProductionTabGroupCA> Groups;

		public string LeftButton = null;
		public string RightButton = null;
		public string TabButton = "button";
		public string Background = null;

		int contentWidth = 0;
		bool leftPressed = false;
		bool rightPressed = false;
		SpriteFont font;
		Rectangle leftButtonRect;
		Rectangle rightButtonRect;
		readonly Lazy<ProductionPaletteWidget> paletteWidget;
		string queueGroup;

		int startTabIndex;

		[ObjectCreator.UseCtor]
		public ProductionTabsCAWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;

			Groups = world.Map.Rules.Actors.Values.SelectMany(a => a.TraitInfos<ProductionQueueInfo>())
				.Select(q => q.Group).Distinct().ToDictionary(g => g, g => new ProductionTabGroupCA() { Group = g });

			// Only visible if the production palette has icons to display
			IsVisible = () => queueGroup != null && Groups[queueGroup].Tabs.Count > 0;

			paletteWidget = Exts.Lazy(() => Ui.Root.Get<ProductionPaletteWidget>(PaletteWidget));
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			var rb = RenderBounds;
			leftButtonRect = new Rectangle(rb.X, rb.Y, ArrowWidth, rb.Height);
			rightButtonRect = new Rectangle(rb.Right - ArrowWidth, rb.Y, ArrowWidth, rb.Height);
			font = Game.Renderer.Fonts["TinyBold"];
		}

		public bool SelectNextTab(bool reverse)
		{
			if (queueGroup == null)
				return true;

			// Prioritize alerted queues
			var queues = Groups[queueGroup].Tabs.Select(t => t.Queue)
					.OrderByDescending(q => q.AllQueued().Any(i => i.Done) ? 1 : 0)
					.ToList();

			if (reverse) queues.Reverse();

			CurrentQueue = queues.SkipWhile(q => q != CurrentQueue)
				.Skip(1).FirstOrDefault() ?? queues.FirstOrDefault();

			return true;
		}

		public void PickUpCompletedBuilding()
		{
			// This is called from ProductionTabsLogic
			paletteWidget.Value.PickUpCompletedBuilding();
		}

		// Production Type
		public string QueueGroup
		{
			get => queueGroup;

			set
			{
				queueGroup = value;
				SelectNextTab(false);
				paletteWidget.Value.ScrollToTop();
			}
		}

		// Tab
		public ProductionQueue CurrentQueue
		{
			get => paletteWidget.Value.CurrentQueue;

			set
			{
				paletteWidget.Value.CurrentQueue = value;
				queueGroup = value != null ? value.Info.Group : null;
				ForceSelectedTabVisible();
				paletteWidget.Value.ScrollToTop();
			}
		}

		public IEnumerable<ProductionTabCA> GetTabs()
		{
			return Groups[queueGroup].Tabs.Where(t => t.Queue.BuildableItems().Any());
		}

		public override void Draw()
		{
			var tabs = GetTabs();
			var numTabs = tabs.Count();

			if (numTabs == 0)
				return;

			var rb = RenderBounds;

			var leftDisabled = startTabIndex == 0;
			var leftHover = Ui.MouseOverWidget == this && leftButtonRect.Contains(Viewport.LastMousePos);
			var rightDisabled = startTabIndex >= numTabs - MaxTabsVisible;
			var rightHover = Ui.MouseOverWidget == this && rightButtonRect.Contains(Viewport.LastMousePos);

			ButtonWidget.DrawBackground(LeftButton, leftButtonRect, leftDisabled, leftPressed, leftHover, false);
			ButtonWidget.DrawBackground(RightButton, rightButtonRect, rightDisabled, rightPressed, rightHover, false);

			// Draw tab buttons
			Game.Renderer.EnableScissor(new Rectangle(leftButtonRect.Right, rb.Y, rightButtonRect.Left - leftButtonRect.Right, rb.Height));
			var origin = new int2(leftButtonRect.Right, leftButtonRect.Y);
			contentWidth = 0;

			var tabIndex = -1;
			var tabsShown = 0;

			foreach (var tab in tabs)
			{
				tabIndex++;

				if (tabsShown >= MaxTabsVisible)
					break;

				if (tabIndex < startTabIndex)
					continue;

				var rect = new Rectangle(origin.X + contentWidth, origin.Y, TabWidth, rb.Height);
				var hover = !leftHover && !rightHover && Ui.MouseOverWidget == this && rect.Contains(Viewport.LastMousePos);
				var highlighted = tab.Queue == CurrentQueue;
				ButtonWidget.DrawBackground(TabButton, rect, false, false, hover, highlighted);

				contentWidth += TabWidth + TabSpacing;

				// Draw number label
				var textSize = font.Measure(tab.Name);
				var position = new int2(rect.X + (rect.Width - textSize.X) / 2, (rect.Y + (rect.Height - textSize.Y) / 2) - 1);
				font.DrawText(tab.Name, position, tab.Queue.AllQueued().Any(i => i.Done) ? Color.Gold : Color.White);

				tabsShown++;
			}

			Game.Renderer.DisableScissor();
		}

		void ForceSelectedTabVisible()
		{
			var tabs = GetTabs();
			var tabIndex = -1;
			var selectedTabIndex = 0;

			foreach (var tab in tabs)
			{
				tabIndex++;

				if (tab.Queue == CurrentQueue)
					selectedTabIndex = tabIndex;
			}

			if (selectedTabIndex < startTabIndex)
				startTabIndex = selectedTabIndex;
			else if (selectedTabIndex > startTabIndex + MaxTabsVisible - 1)
				startTabIndex = selectedTabIndex + 1 - MaxTabsVisible;
		}

		void Scroll(int amount)
		{
			startTabIndex += amount;
		}

		// Is added to world.ActorAdded by the SidebarLogic handler
		public void ActorChanged(Actor a)
		{
			if (a.Info.HasTraitInfo<ProductionQueueInfo>())
			{
				UpdateQueues(a);

				if (queueGroup == null)
					return;

				// Queue destroyed, was last of type: switch to a new group
				if (Groups[queueGroup].Tabs.Count == 0)
					QueueGroup = Groups.Where(g => g.Value.Tabs.Count > 0)
						.Select(g => g.Key).FirstOrDefault();

				// Queue destroyed, others of same type: switch to another tab
				else if (!Groups[queueGroup].Tabs.Select(t => t.Queue).Contains(CurrentQueue))
					SelectNextTab(false);
			}
			else if (a.Info.HasTraitInfo<ProvidesPrerequisiteInfo>())
				UpdateQueues(a);
		}

		void UpdateQueues(Actor a)
		{
			var allQueues = a.World.ActorsWithTrait<ProductionQueue>()
				.Where(p => p.Actor.Owner == p.Actor.World.LocalPlayer && p.Actor.IsInWorld && p.Trait.Enabled)
				.Select(p => p.Trait).ToList();

			foreach (var g in Groups.Values)
				g.Update(allQueues);

			if (allQueues.Count > 0 && CurrentQueue == null)
				CurrentQueue = allQueues.First();
		}

		public override bool YieldMouseFocus(MouseInput mi)
		{
			leftPressed = rightPressed = false;
			return base.YieldMouseFocus(mi);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			var leftDisabled = startTabIndex == 0;
			var rightDisabled = startTabIndex >= GetTabs().Count() - MaxTabsVisible;

			if (mi.Event == MouseInputEvent.Scroll)
			{
				if (mi.Delta.Y > 0 && !rightDisabled)
					Scroll(1);
				else if (mi.Delta.Y < 0 && !leftDisabled)
					Scroll(-1);

				return true;
			}

			if (mi.Button != MouseButton.Left)
				return true;

			if (mi.Event == MouseInputEvent.Down && !TakeMouseFocus(mi))
				return true;

			if (!HasMouseFocus)
				return true;

			if (HasMouseFocus && mi.Event == MouseInputEvent.Up)
				return YieldMouseFocus(mi);

			leftPressed = leftButtonRect.Contains(mi.Location);
			rightPressed = rightButtonRect.Contains(mi.Location);

			if (leftPressed || rightPressed)
			{
				if ((leftPressed && !leftDisabled) || (rightPressed && !rightDisabled))
				{
					if (rightPressed)
						Scroll(1);
					else
						Scroll(-1);

					Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", ClickSound, null);
				}
				else
					Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", ClickDisabledSound, null);
			}

			// Switch to production tab clicked on by getting location of click
			var offsetloc = mi.Location - new int2(leftButtonRect.Right, leftButtonRect.Y);
			if (offsetloc.X > 0 && offsetloc.X < contentWidth)
			{
				var tabIndex = (offsetloc.X / (TabWidth + TabSpacing)) + startTabIndex;

				if (Groups[queueGroup].Tabs.Count >= tabIndex)
				{
					var tab = Groups[queueGroup].Tabs[tabIndex];
					CurrentQueue = tab.Queue;
					Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", ClickSound, null);

					if (mi.MultiTapCount > 1 && tab.Actor != null && tab.Actor.IsInWorld)
					{
						var viewport = worldRenderer.Viewport;
						viewport.Center(tab.Actor.CenterPosition);
					}
				}
			}

			return true;
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event != KeyInputEvent.Down)
				return false;

			if (PreviousProductionTabKey.IsActivatedBy(e))
			{
				Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", ClickSound, null);
				return SelectNextTab(true);
			}

			if (NextProductionTabKey.IsActivatedBy(e))
			{
				Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", ClickSound, null);
				return SelectNextTab(false);
			}

			return false;
		}
	}
}
