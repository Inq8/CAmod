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
using OpenRA.Mods.Common.Effects;
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

		[Desc("Maximum number of enemy targets. Zero for no limit (MaxTargets still applies).")]
		public readonly int MaxEnemyTargets = 0;

		[Desc("If true, keeps formation of teleported units.")]
		public readonly bool KeepFormation = false;

		[Desc("Maximum distance a selected unit can move away from their initial location.")]
		public readonly WDist LeashRange = WDist.FromCells(12);

		[Desc("Ticks until returning after teleportation.")]
		public readonly int Duration = 750;

		[Desc("Duration for enemy units. Set to zero to use same as main duration.")]
		public readonly int EnemyDuration = 375;

		public readonly bool KillCargo = true;

		[Desc("Target types that cannot be targeted for chronoshifting.")]
		public readonly BitSet<TargetableType> InvalidTargetTypes = default(BitSet<TargetableType>);

		[CursorReference]
		[Desc("Cursor to display when selecting targets for the chronoshift.")]
		public readonly string SelectionCursor = "chrono-select";

		[CursorReference]
		[Desc("Cursor to display when targeting an area for the chronoshift.")]
		public readonly string TargetCursor = "chrono-target";

		[CursorReference]
		[Desc("Cursor to display when the targeted area is blocked.")]
		public readonly string TargetBlockedCursor = "move-blocked";

		[Desc("Font to use for target count.")]
		public readonly string TargetCountFont = "Medium";

		public readonly bool ShowSelectionBoxes = false;
		public readonly Color HoverSelectionBoxColor = Color.White;
		public readonly Color SelectedSelectionBoxColor = Color.Lime;

		public readonly bool ShowTargetCircle = false;
		public readonly Color TargetCircleColor = Color.White;
		public readonly bool TargetCircleUsePlayerColor = false;
		public readonly bool ShowDestinationCircle = false;
		public readonly Color DestinationCircleColor = Color.Lime;

		[Desc("Warp from sequence sprite image.")]
		public readonly string WarpFromImage = null;

		[Desc("Warp from sequence.")]
		[SequenceReference(nameof(WarpFromImage))]
		public readonly string WarpFromSequence = null;

		[Desc("Warp to sequence sprite image.")]
		public readonly string WarpToImage = null;

		[Desc("Warp to sequence.")]
		[SequenceReference(nameof(WarpToImage))]
		public readonly string WarpToSequence = null;

		[PaletteReference]
		public readonly string WarpEffectPalette = "effect";

		[Desc("Target tint colour.")]
		public readonly Color? TargetTintColor = null;

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
			var sourceCell = order.ExtraLocation;
			var sourcePos = self.World.Map.CenterOfCell(sourceCell);
			var targetDelta = targetCell - sourceCell;

			var actorsToTeleport = selectedActors.Where(a => IsValidTarget(a));

			foreach (var actor in actorsToTeleport)
			{
				var cs = actor.TraitsImplementing<ChronoshiftableCA>()
					.FirstEnabledConditionalTraitOrDefault();

				if (cs == null)
					continue;

				if (info.LeashRange > WDist.Zero)
				{
					var unitDistMoved = actor.CenterPosition - sourcePos;
					if (unitDistMoved.Length > info.LeashRange.Length)
						continue;
				}

				var destinationCell = info.KeepFormation ? actor.Location + targetDelta : targetCell;
				var duration = info.Duration;

				if (info.EnemyDuration != info.Duration && !actor.Owner.IsAlliedWith(self.Owner))
					duration = info.EnemyDuration;

				if (self.Owner.Shroud.IsExplored(targetCell)) // && cs.CanChronoshiftTo(target, targetCell)
					cs.Teleport(actor, targetCell, duration, info.KillCargo, self);
			}

			if (info.WarpFromImage != null && info.WarpFromSequence != null)
				self.World.Add(new SpriteEffect(sourcePos, self.World, info.WarpFromImage, info.WarpFromSequence, info.WarpEffectPalette));

			if (info.WarpToImage != null && info.WarpToSequence != null)
				self.World.Add(new SpriteEffect(order.Target.CenterPosition, self.World, info.WarpToImage, info.WarpToSequence, info.WarpEffectPalette));
		}

		public IEnumerable<Actor> GetTargets(CPos xy)
		{
			var centerPos = Self.World.Map.CenterOfCell(xy);

			var actorsInRange = Self.World.FindActorsInCircle(centerPos, info.Range)
				.Where(a => IsValidTarget(a))
				.OrderBy(a => (a.CenterPosition - centerPos).LengthSquared);

			// If we have a target limit
			if (info.MaxTargets > 0)
			{
				// If no enemy target limit, or the overall target limit is lower
				if (info.MaxEnemyTargets == 0 || info.MaxTargets < info.MaxEnemyTargets)
					return actorsInRange.Take(info.MaxTargets);
				else
				{
					var targets = new List<Actor>();
					var enemyTargets = 0;

					foreach (var a in actorsInRange)
					{
						if (info.MaxTargets > 0 && targets.Count() >= info.MaxTargets)
							break;

						if (info.MaxEnemyTargets > 0)
						{
							var isEnemy = !a.Owner.IsAlliedWith(Self.Owner);

							if (isEnemy && enemyTargets >= info.MaxEnemyTargets)
								continue;

							if (isEnemy)
								enemyTargets++;
						}

						targets.Add(a);
					}

					return targets;
				}
			}
			else
				return actorsInRange;
		}

		public bool IsValidTarget(Actor a)
		{
			if (a == null || !a.IsInWorld || a.IsDead)
				return false;

			if (!a.TraitsImplementing<ChronoshiftableCA>().Any(cs => !cs.IsTraitDisabled))
				return false;

			var targetTypes = a.GetEnabledTargetTypes();
			if (targetTypes.Overlaps(info.InvalidTargetTypes))
				return false;

			if (!Self.Owner.Shroud.IsVisible(a.Location))
				return false;

			if (!a.CanBeViewedByPlayer(Self.Owner))
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
						var decorations = unit.TraitsImplementing<ISelectionDecorations>().FirstEnabledTraitOrDefault();
						if (decorations != null)
							foreach (var d in decorations.RenderSelectionAnnotations(unit, wr, power.info.HoverSelectionBoxColor))
								yield return d;
					}
				}

				if (power.info.ShowTargetCircle)
				{
					yield return new RangeCircleAnnotationRenderable(
						world.Map.CenterOfCell(xy),
						power.info.Range,
						0,
						power.info.TargetCircleUsePlayerColor ? power.Self.OwnerColor() : power.info.TargetCircleColor,
						1,
						Color.FromArgb(96, Color.Black),
						3);
				}

				if (power.info.MaxTargets > 0)
				{
					var font = Game.Renderer.Fonts[power.info.TargetCountFont];
					var color = power.info.TargetCircleColor;
					var text = targetUnits.Count() + " / " + power.info.MaxTargets;
					var size = font.Measure(text);
					var textPos = new int2(Viewport.LastMousePos.X - (size.X / 2), Viewport.LastMousePos.Y - (size.Y * 2) - (size.Y / 3));
					yield return new UITextRenderable(font, WPos.Zero, textPos, 0, color, text);
				}
			}

			protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);

				if (power.info.TargetTintColor != null)
				{
					var targetUnits = power.GetTargets(xy);

					foreach (var unit in targetUnits)
					{
						var renderables = unit.Render(wr)
							.Where(r => !r.IsDecoration && r is IModifyableRenderable)
							.Select(r =>
							{
								var mr = (IModifyableRenderable)r;
								var tint = new float3(power.info.TargetTintColor.Value.R, power.info.TargetTintColor.Value.G, power.info.TargetTintColor.Value.B) / 255f;
								mr = mr.WithTint(tint, mr.TintModifiers | TintModifiers.ReplaceColor).WithAlpha(power.info.TargetTintColor.Value.A / 255f);
								return mr;
							});

						foreach (var r in renderables)
						{
							yield return r;
						}
					}
				}
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
						if (!unit.CanBeViewedByPlayer(manager.Self.Owner))
							continue;

						if (power.info.LeashRange > WDist.Zero)
						{
							var unitDistMoved = unit.CenterPosition - world.Map.CenterOfCell(sourceLocation);
							if (unitDistMoved.Length > power.info.LeashRange.Length)
								continue;
						}

						var decorations = unit.TraitsImplementing<ISelectionDecorations>().FirstEnabledTraitOrDefault();
						if (decorations == null)
							continue;

						foreach (var d in decorations.RenderSelectionAnnotations(unit, wr, power.info.SelectedSelectionBoxColor))
							yield return d;
					}

					if (power.info.ShowDestinationCircle)
					{
						yield return new RangeCircleAnnotationRenderable(
							world.Map.CenterOfCell(xy),
							power.info.Range,
							0,
							power.info.TargetCircleUsePlayerColor ? power.Self.OwnerColor() : power.info.DestinationCircleColor,
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

				if (!manager.Self.Owner.Shroud.IsExplored(xy))
					return false;

				return true;
			}

			protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				var powerInfo = (ChronoshiftPowerCAInfo)power.Info;
				return IsValidDestination(cell) ? powerInfo.TargetCursor : powerInfo.TargetBlockedCursor;
			}
		}
	}
}
