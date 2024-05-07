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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class GuidedMissilePowerInfo : SupportPowerInfo
	{
		[Desc("Missile actor. Must have the `" + nameof(GuidedMissile) + "` trait.")]
		[ActorReference]
		[FieldLoader.Require]
		public readonly string MissileActor = null;

		[Desc("Sound to instantly play at the targeted area.")]
		public readonly string[] LaunchSounds = Array.Empty<string>();

		[Desc("Target types that condition can be applied to. Leave empty for all types.")]
		public readonly BitSet<TargetableType> ValidTargets = default;

		[Desc("Target types that condition can be applied to. Leave empty for all types.")]
		public readonly BitSet<TargetableType> InvalidTargets = default;

		[Desc("Player relationships which can be targeted.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Enemy | PlayerRelationship.Neutral;

		[CursorReference]
		[Desc("Cursor to display when there are no units to apply the condition in range.")]
		public readonly string BlockedCursor = "move-blocked";

		[Desc("If true, targets must not be under shroud/fog.")]
		public readonly bool TargetMustBeVisible = true;

		[Desc("Ticks between launches.")]
		public readonly int LaunchInterval = 10;

		[Desc("Total missiles. Zero for equates to one per target. If positive, must be equal to or greater than MaxTargets.")]
		public readonly int MissileCount = 0;

		[Desc("Missiles per launch.")]
		public readonly int MissilesPerLaunch = 1;

		[Desc("Maximum number of targets. Zero for no limit.")]
		public readonly int MaxTargets = 0;

		[Desc("Minimum targets for power to activate.")]
		public readonly int MinTargets = 1;

		[Desc("Font to use for target count.")]
		public readonly string TargetCountFont = "Medium";

		[SequenceReference]
		[Desc("Sequence to play for granting actor when activated.",
			"This requires the actor to have the WithSpriteBody trait or one of its derivatives.")]
		public readonly string ActiveSequence = "active";

		[GrantedConditionReference]
		[Desc("A condition to apply while active.")]
		public readonly string ActiveCondition = null;

		[Desc("Duration of the Active condition (in ticks). Set to 0 for a permanent condition.")]
		public readonly int ActiveDuration = 50;

		public readonly bool ShowSelectionBoxes = false;
		public readonly Color SelectionBoxColor = Color.Red;

		public readonly bool ShowTargetCircle = false;
		public readonly Color TargetCircleColor = Color.Red;
		public readonly bool TargetCircleUsePlayerColor = false;

		[Desc("Target tint colour.")]
		public readonly Color? TargetTintColor = null;

		public WeaponInfo WeaponInfo { get; private set; }

		[Desc("Range in which to apply condition.")]
		public readonly WDist Range = WDist.Zero;

		public override object Create(ActorInitializer init) { return new GuidedMissilePower(init.Self, this); }
	}

	enum MapEdge
	{
		Top,
		Right,
		Bottom,
		Left
	}

	public class GuidedMissilePower : SupportPower, ITick, INotifyCreated
	{
		readonly GuidedMissilePowerInfo info;
		readonly int halfMapHeight;
		readonly int halfMapWidth;
		readonly Rectangle mapBounds;

		Queue<Actor> targetQueue;
		CPos targetCell;
		WPos soundPos;
		int ticks;
		MapEdge startEdge;

		[Sync]
		public int Ticks { get; private set; }

		public GuidedMissilePower(Actor self, GuidedMissilePowerInfo info)
			: base(self, info)
		{
			this.info = info;
			targetQueue = new Queue<Actor>();
			mapBounds = self.World.Map.Bounds;
			halfMapHeight = mapBounds.Height / 2;
			halfMapWidth = mapBounds.Width / 2;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.OrderGenerator = new SelectGuidedMissileTarget(Self.World, order, manager, this);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			PlayLaunchSounds();

			var wsb = self.TraitOrDefault<WithSpriteBody>();
			if (wsb != null && wsb.DefaultAnimation.HasSequence(info.ActiveSequence))
				wsb.PlayCustomAnimation(self, info.ActiveSequence);

			targetCell = self.World.Map.CellContaining(order.Target.CenterPosition);
			var targets = GetTargets(targetCell).ToList();
			var numMissiles = info.MissileCount > 0 ? info.MissileCount : targets.Count;
			var targetIdx = 0;

			while (numMissiles-- > 0)
			{
				targetQueue.Enqueue(targets[targetIdx]);
				if (++targetIdx >= targets.Count)
					targetIdx = 0;
			}

			startEdge = CalculateStartEdge(targetCell);

			switch (startEdge)
			{
				case MapEdge.Top:
					soundPos = self.World.Map.CenterOfCell(new CPos(targetCell.X, 0));
					break;

				case MapEdge.Right:
					soundPos = self.World.Map.CenterOfCell(new CPos(mapBounds.Width, targetCell.Y));
					break;

				case MapEdge.Bottom:
					soundPos = self.World.Map.CenterOfCell(new CPos(targetCell.X, mapBounds.Height));
					break;

				case MapEdge.Left:
					soundPos = self.World.Map.CenterOfCell(new CPos(0, targetCell.Y));
					break;
			}
		}

		void ITick.Tick(Actor self)
		{
			if (targetQueue.Count == 0)
				return;

			if (++ticks % info.LaunchInterval != 0)
				return;

			var launchSound = info.LaunchSounds.RandomOrDefault(self.World.LocalRandom);
			if (launchSound != null)
				Game.Sound.Play(SoundType.World, launchSound, soundPos);

			var launches = 0;
			while (++launches <= info.MissilesPerLaunch && targetQueue.Count > 0)
				LaunchMissile(self, targetQueue.Dequeue());

			if (targetQueue.Count == 0)
				ticks = 0;
		}

		private IEnumerable<Actor> GetTargets(CPos xy)
		{
			return GetTargetsInCircle(xy);
		}

		private MapEdge CalculateStartEdge(CPos targetCell)
		{
			var distFromTopEdge = targetCell.Y;
			var distFromLeftEdge = targetCell.X;
			var distFromBottomEdge = mapBounds.Height - targetCell.Y;
			var distFromRightEdge = mapBounds.Width - targetCell.X;

			if (distFromTopEdge <= distFromLeftEdge && distFromTopEdge <= distFromBottomEdge && distFromTopEdge <= distFromRightEdge)
			{
				return MapEdge.Top;
			}
			else if (distFromRightEdge <= distFromBottomEdge && distFromRightEdge <= distFromLeftEdge)
			{
				return MapEdge.Right;
			}
			else if (distFromBottomEdge <= distFromLeftEdge)
			{
				return MapEdge.Bottom;
			}
			else
			{
				return MapEdge.Left;
			}
		}

		private CPos CalculateStartCell(CPos targetCell)
		{
			var scatter = Self.World.SharedRandom.Next(-25, 25);
			switch (startEdge)
			{
				case MapEdge.Top:
					return new CPos(targetCell.X, targetCell.Y - halfMapHeight) + new CVec(scatter, 0);

				case MapEdge.Right:
					return new CPos(targetCell.X + halfMapWidth, targetCell.Y) + new CVec(0, scatter);

				case MapEdge.Bottom:
					return new CPos(targetCell.X, targetCell.Y + halfMapHeight) + new CVec(scatter, 0);

				case MapEdge.Left:
				default:
					return new CPos(targetCell.X - halfMapWidth, targetCell.Y) + new CVec(0, scatter);
			}
		}

		private void LaunchMissile(Actor self, Actor targetActor)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (targetActor.IsDead || !targetActor.IsInWorld)
					return;

				var startCell = CalculateStartCell(targetCell);
				var startPos = 	self.World.Map.CenterOfCell(startCell);

				var spawnDirection = new WVec((targetCell - startCell).X, (targetCell - startCell).Y, 0);
				var spawnFacing = spawnDirection.Yaw;

				var actor = w.CreateActor(false, info.MissileActor, new TypeDictionary
				{
					new CenterPositionInit(startPos + new WVec(0, 0, targetActor.CenterPosition.Z)),
					new OwnerInit(self.Owner),
					new FacingInit(spawnFacing)
				});

				// todo: implement dynamic speed
				// var dynamicSpeedMultiplier = actor.TraitOrDefault<DynamicSpeedMultiplier>();

				var gm = actor.Trait<GuidedMissile>();
				var target = Target.FromActor(targetActor);
				gm.SetTarget(target);
				w.Add(actor);
			});
		}

		private IEnumerable<Actor> GetTargetsInCircle(CPos xy)
		{
			var centerPos = Self.World.Map.CenterOfCell(xy);

			var actorsInRange = Self.World.FindActorsInCircle(centerPos, info.Range)
				.Where(a => a.IsInWorld
					&& !a.IsDead
					&& info.ValidRelationships.HasRelationship(Self.Owner.RelationshipWith(a.Owner))
					&& (info.ValidTargets.IsEmpty || info.ValidTargets.Overlaps(a.GetEnabledTargetTypes()))
					&& (info.InvalidTargets.IsEmpty || !info.InvalidTargets.Overlaps(a.GetEnabledTargetTypes()))
					&& (!info.TargetMustBeVisible || Self.Owner.Shroud.IsVisible(a.Location))
					&& a.CanBeViewedByPlayer(Self.Owner))
				.OrderBy(a => (a.CenterPosition - centerPos).LengthSquared);

			if (info.MaxTargets > 0)
				return actorsInRange.Take(info.MaxTargets);

			return actorsInRange;
		}

		class SelectGuidedMissileTarget : OrderGenerator
		{
			readonly GuidedMissilePower power;
			readonly SupportPowerManager manager;
			readonly string order;

			public SelectGuidedMissileTarget(World world, string order, SupportPowerManager manager, GuidedMissilePower power)
			{
				// Clear selection if using Left-Click Orders
				if (Game.Settings.Game.UseClassicMouseStyle)
					manager.Self.World.Selection.Clear();

				this.manager = manager;
				this.order = order;
				this.power = power;
			}

			protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				world.CancelInputMode();
				var targets = power.GetTargets(cell);
				if (mi.Button == MouseButton.Left && targets.Count() >= power.info.MinTargets)
					yield return new Order(order, manager.Self, Target.FromCell(world, cell), false) { SuppressVisualFeedback = true };
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
						{
							foreach (var d in decorations.RenderSelectionAnnotations(unit, wr, power.info.SelectionBoxColor))
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

				if (power.info.MaxTargets > 0)
				{
					var font = Game.Renderer.Fonts[power.info.TargetCountFont];
					var color = power.info.TargetCircleColor;
					var text = targetUnits.Count() + " / " + power.info.MaxTargets;
					var size = font.Measure(text);
					var textPos = new int2(Viewport.LastMousePos.X - (size.X / 2), Viewport.LastMousePos.Y + size.Y + (size.Y / 3));
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
				var targets = power.GetTargets(cell);
				return targets.Count() >= power.info.MinTargets ? power.info.Cursor : power.info.BlockedCursor;
			}
		}
	}
}
