#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	sealed class ChronoshiftPowerCAInfo : SupportPowerInfo
	{
		[FieldLoader.Require]
		[Desc("Range in which to apply condition.")]
		public readonly WDist Range = WDist.Zero;

		[Desc("Maximum number of targets. Zero for no limit.")]
		public readonly int MaxTargets = 0;

		[Desc("If true, keeps formation of teleported units.")]
		public readonly bool KeepFormation = false;

		[Desc("Maximum distance a selected unit can move away from their initial location.")]
		public readonly WDist LeashRange = WDist.FromCells(12);

		[Desc("Ticks until returning after teleportation.")]
		public readonly int Duration = 750;

		public readonly bool KillCargo = true;

		[CursorReference]
		[Desc("Cursor to display when selecting targets for the chronoshift.")]
		public readonly string SelectionCursor = "chrono-select";

		[CursorReference]
		[Desc("Cursor to display when targeting an area for the chronoshift.")]
		public readonly string TargetCursor = "chrono-target";

		[CursorReference]
		[Desc("Cursor to display when the targeted area is blocked.")]
		public readonly string TargetBlockedCursor = "move-blocked";

		public readonly bool ShowSelectionBoxes = false;
		public readonly Color HoverSelectionBoxColor = Color.White;
		public readonly Color SelectedSelectionBoxColor = Color.Lime;

		public readonly bool ShowTargetCircle = false;
		public readonly Color TargetCircleColor = Color.White;
		public readonly bool TargetCircleUsePlayerColor = false;
		public readonly bool ShowDestinationCircle = false;
		public readonly Color DestinationCircleColor = Color.Lime;

		public override object Create(ActorInitializer init) { return new ChronoshiftPowerCA(init.Self, this); }
	}

	sealed class ChronoshiftPowerCA : SupportPower, IResolveOrder
	{
		readonly ChronoshiftPowerCAInfo info;
		readonly IList<Actor> selectedActors;
		uint selectionIteration;

		public ChronoshiftPowerCA(Actor self, ChronoshiftPowerCAInfo info)
			: base(self, info)
		{
			this.info = info;
			selectedActors = new List<Actor>();
			selectionIteration = 0;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.OrderGenerator = new SelectChronoshiftTarget(Self.World, order, manager, this);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			PlayLaunchSounds();

			var info = (ChronoshiftPowerCAInfo)Info;
			var targetCell = self.World.Map.CellContaining(order.Target.CenterPosition);
			var targetDelta = targetCell - order.ExtraLocation;

			var actorsToTeleport = selectedActors.Where(a => IsValidTarget(a));

			foreach (var actor in actorsToTeleport)
			{
				var cs = actor.TraitsImplementing<Chronoshiftable>()
					.FirstEnabledConditionalTraitOrDefault();

				if (cs == null)
					continue;

				if (info.LeashRange > WDist.Zero)
				{
					var unitDistMoved = actor.CenterPosition - self.World.Map.CenterOfCell(order.ExtraLocation);
					if (unitDistMoved.Length > info.LeashRange.Length)
						continue;
				}

				var destinationCell = info.KeepFormation ? actor.Location + targetDelta : targetCell;

				if (self.Owner.Shroud.IsExplored(targetCell)) // && cs.CanChronoshiftTo(target, targetCell)
					cs.Teleport(actor, targetCell, info.Duration, info.KillCargo, self);
			}
		}

		public IEnumerable<Actor> GetTargets(CPos xy)
		{
			var centerPos = Self.World.Map.CenterOfCell(xy);

			var actorsInRange = Self.World.FindActorsInCircle(centerPos, info.Range)
				.Where(a => IsValidTarget(a))
				.OrderBy(a => (a.CenterPosition - centerPos).LengthSquared);

			if (info.MaxTargets > 0)
				return actorsInRange.Take(info.MaxTargets);

			return actorsInRange;
		}

		public bool IsValidTarget(Actor a)
		{
			if (!a.IsInWorld || a.IsDead)
				return false;

			if (!a.TraitsImplementing<Chronoshiftable>().Any(cs => !cs.IsTraitDisabled))
				return false;

			if (Self.World.ShroudObscures(a.Location) || Self.World.FogObscures(a.Location))
				return false;

			return true;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SelectChronoshiftTargets")
			{
				selectedActors.Clear();
				foreach (var a in order.ExtraActors)
					selectedActors.Add(a);

				selectionIteration = order.ExtraData;
			}
		}

		sealed class SelectChronoshiftTarget : OrderGenerator
		{
			readonly ChronoshiftPowerCA power;
			readonly SupportPowerManager manager;
			readonly string order;

			public SelectChronoshiftTarget(World world, string order, SupportPowerManager manager, ChronoshiftPowerCA power)
			{
				// Clear selection if using Left-Click Orders
				if (Game.Settings.Game.UseClassicMouseStyle)
					manager.Self.World.Selection.Clear();

				this.manager = manager;
				this.order = order;
				this.power = power;

				var info = (ChronoshiftPowerCAInfo)power.Info;
			}

			protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				world.CancelInputMode();
				if (mi.Button == MouseButton.Left)
				{
					var newSelectionIteration = power.selectionIteration + 1;
					yield return new Order("SelectChronoshiftTargets", power.Self, false, power.GetTargets(cell).ToArray()) {
						ExtraData = newSelectionIteration
					};
					world.OrderGenerator = new SelectDestination(world, order, manager, power, cell, newSelectionIteration);
				}

				yield break;
			}

			protected override void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (!manager.Powers.TryGetValue(order, out var p) || !p.Active || !p.Ready)
					world.CancelInputMode();
			}

			protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

			protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var targetUnits = power.GetTargets(xy);

				if (power.info.ShowSelectionBoxes)
				{
					foreach (var unit in targetUnits)
					{
						if (unit.CanBeViewedByPlayer(manager.Self.Owner))
						{
							var decorations = unit.TraitsImplementing<ISelectionDecorations>().FirstEnabledTraitOrDefault();
							if (decorations != null)
								foreach (var d in decorations.RenderSelectionAnnotations(unit, wr, power.info.HoverSelectionBoxColor))
									yield return d;
						}
					}
				}

				if (power.info.ShowTargetCircle)
				{
					yield return new RangeCircleAnnotationRenderable(
						world.Map.CenterOfCell(xy),
						power.info.Range,
						0,
						power.info.TargetCircleUsePlayerColor ? power.Self.Owner.Color : power.info.TargetCircleColor,
						1,
						Color.FromArgb(96, Color.Black),
						3);
				}
			}

			protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
			{
				yield break;
			}

			protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				return ((ChronoshiftPowerCAInfo)power.Info).SelectionCursor;
			}
		}

		sealed class SelectDestination : OrderGenerator
		{
			readonly ChronoshiftPowerCA power;
			readonly CPos sourceLocation;
			readonly SupportPowerManager manager;
			readonly string order;
			readonly uint expectedIteration;

			public SelectDestination(World world, string order, SupportPowerManager manager, ChronoshiftPowerCA power, CPos sourceLocation, uint expectedIteration)
			{
				this.manager = manager;
				this.order = order;
				this.power = power;
				this.sourceLocation = sourceLocation;
				this.expectedIteration = expectedIteration;

				var info = (ChronoshiftPowerCAInfo)power.Info;
			}

			protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
				{
					world.CancelInputMode();
					yield break;
				}

				var ret = OrderInner(cell).FirstOrDefault();
				if (ret == null)
					yield break;

				world.CancelInputMode();
				yield return ret;
			}

			IEnumerable<Order> OrderInner(CPos xy)
			{
				// Cannot chronoshift into unexplored location
				if (IsValidDestination(xy))
					yield return new Order(order, manager.Self, Target.FromCell(manager.Self.World, xy), false)
					{
						ExtraLocation = sourceLocation,
						SuppressVisualFeedback = true
					};
			}

			protected override void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (!manager.Powers.TryGetValue(order, out var p) || !p.Active || !p.Ready)
					world.CancelInputMode();
			}

			protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world)
			{
				yield break;
			}

			protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
			{
				if (expectedIteration == power.selectionIteration)
				{
					var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);

					foreach (var unit in power.selectedActors.Where(a => power.IsValidTarget(a)))
					{
						if (unit.CanBeViewedByPlayer(manager.Self.Owner))
						{
							var decorations = unit.TraitsImplementing<ISelectionDecorations>().FirstEnabledTraitOrDefault();
							if (decorations != null)
								foreach (var d in decorations.RenderSelectionAnnotations(unit, wr, power.info.SelectedSelectionBoxColor))
									yield return d;
						}
					}

					if (power.info.ShowDestinationCircle)
					{
						yield return new RangeCircleAnnotationRenderable(
							world.Map.CenterOfCell(xy),
							power.info.Range,
							0,
							power.info.TargetCircleUsePlayerColor ? power.Self.Owner.Color : power.info.DestinationCircleColor,
							1,
							Color.FromArgb(96, Color.Black),
							3);
					}
				}
			}

			protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
			{
				yield break;
			}

			bool IsValidDestination(CPos xy)
			{
				var actorsToTeleport = power.selectedActors.Where(a => power.IsValidTarget(a));

				if (!actorsToTeleport.Any())
					return false;

				var canTeleport = false;
				foreach (var unit in actorsToTeleport)
				{
					var targetCell = unit.Location + (xy - sourceLocation);
					if (manager.Self.Owner.Shroud.IsExplored(targetCell)) // && unit.Trait<Chronoshiftable>().CanChronoshiftTo(unit, targetCell)
					{
						canTeleport = true;
						break;
					}
				}

				return canTeleport;
			}

			protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				var powerInfo = (ChronoshiftPowerCAInfo)power.Info;
				return IsValidDestination(cell) ? powerInfo.TargetCursor : powerInfo.TargetBlockedCursor;
			}
		}
	}
}
