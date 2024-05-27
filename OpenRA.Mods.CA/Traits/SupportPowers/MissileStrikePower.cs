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
	public class MissileStrikePowerInfo : SupportPowerInfo
	{
		[Desc("Missile actor. Must have a trait that inherits `" + nameof(MissileBase) + "`.")]
		[ActorReference]
		[FieldLoader.Require]
		public readonly string MissileActor = null;

		[Desc("Altitude to launch missiles from. If null, the target's altitude will be used.")]
		public readonly WDist? LaunchAltitude = null;

		[Desc("Sound to instantly play at the targeted area.")]
		public readonly string[] LaunchSounds = Array.Empty<string>();

		[Desc("If true, the power will target actors, otherwise it will target cells according to TargetOffsets.")]
		public readonly bool TargetActors = true;

		[Desc("If TargetsActors is false, defines impact offsets. Cycled through until all missiles are launched.")]
		public readonly CVec[] TargetOffsets = new CVec[] { new CVec(0, 0) };

		[Desc("Whether to shuffle the impact offsets. If false, the offsets will be used in the order they are defined.")]
		public readonly bool ShuffleOffsets = false;

		[Desc("If TargetsActors is true, defines the target types that can be targeted. Leave empty for all types.")]
		public readonly BitSet<TargetableType> ValidTargets = default;

		[Desc("If TargetsActors is true, defines the target types that cannot be targeted.")]
		public readonly BitSet<TargetableType> InvalidTargets = default;

		[Desc("If TargetsActors is true, defines the player relationships of actors that can be targeted.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Enemy | PlayerRelationship.Neutral;

		[Desc("If TargetsActors is true, maximum number of targets. Zero for no limit.")]
		public readonly int MaxTargets = 0;

		[Desc("If TargetsActors is true, minimum targets for power to activate.")]
		public readonly int MinTargets = 1;

		[Desc("Font to use for target count.")]
		public readonly string TargetCountFont = "Medium";

		[Desc("If true, targets must not be under shroud/fog.")]
		public readonly bool TargetMustBeVisible = true;

		[CursorReference]
		[Desc("Cursor to display when there are no units to apply the condition in range.")]
		public readonly string BlockedCursor = "move-blocked";

		[Desc("Ticks between launches.")]
		public readonly int LaunchInterval = 10;

		[Desc("Total missiles. Zero for equates to one per target/offset.",
			"If positive, must be equal to or greater than MaxTargets, or TargetsActors must be false.")]
		public readonly int MissileCount = 0;

		[Desc("Missiles per launch.")]
		public readonly int MissilesPerLaunch = 1;

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

		[Desc("Target selection radius.")]
		public readonly WDist Range = WDist.Zero;

		public override object Create(ActorInitializer init) { return new MissileStrikePower(init.Self, this); }
	}

	enum MapEdge
	{
		Top,
		Right,
		Bottom,
		Left
	}

	public class MissileStrikePower : SupportPower, ITick, INotifyCreated
	{
		readonly MissileStrikePowerInfo info;
		readonly int halfMapHeight;
		readonly int halfMapWidth;
		readonly Rectangle mapBounds;

		Queue<Target> targetQueue;
		CPos targetCell;
		WPos soundPos;
		int ticks;
		MapEdge startEdge;

		[Sync]
		public int Ticks { get; private set; }

		public MissileStrikePower(Actor self, MissileStrikePowerInfo info)
			: base(self, info)
		{
			this.info = info;
			targetQueue = new Queue<Target>();
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
			self.World.OrderGenerator = new SelectMissileStrikeTarget(Self.World, order, manager, this);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			PlayLaunchSounds();

			var wsb = self.TraitOrDefault<WithSpriteBody>();
			if (wsb != null && wsb.DefaultAnimation.HasSequence(info.ActiveSequence))
				wsb.PlayCustomAnimation(self, info.ActiveSequence);

			targetCell = self.World.Map.CellContaining(order.Target.CenterPosition);

			var targets = info.TargetActors ? GetActorTargets(targetCell).Select(t => Target.FromActor(t)).ToList() : GetCellTargets(targetCell).ToList();
			var numMissiles = info.MissileCount > 0 ? info.MissileCount : targets.Count;

			for (int i = 0; i < numMissiles; i++)
				targetQueue.Enqueue(targets[i % targets.Count]);

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

		private IEnumerable<Actor> GetActorTargets(CPos xy)
		{
			return GetValidTargetActorsInCircle(xy);
		}

		private IEnumerable<Target> GetCellTargets(CPos xy)
		{
			var numMissiles = info.MissileCount > 0 ? info.MissileCount : info.TargetOffsets.Length;
			var offsets = info.TargetOffsets.ToList();

			if (info.ShuffleOffsets)
				offsets = offsets.Shuffle(Self.World.SharedRandom).ToList();

			for (int i = 0; i < numMissiles; i++)
			{
				var targetCell = xy + offsets[i % offsets.Count];
				yield return Target.FromCell(Self.World, targetCell);
			}
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

		private void LaunchMissile(Actor self, Target target)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (target.Type == TargetType.Actor && (target.Actor.IsDead || !target.Actor.IsInWorld))
					return;

				if (target.Type == TargetType.FrozenActor && (target.FrozenActor.Actor.IsDead || !target.FrozenActor.Actor.IsInWorld))
					return;

				var startCell = CalculateStartCell(targetCell);
				var startPos = 	self.World.Map.CenterOfCell(startCell);

				var spawnDirection = new WVec((targetCell - startCell).X, (targetCell - startCell).Y, 0);
				var spawnFacing = spawnDirection.Yaw;
				var targetAltitude = new WDist(target.CenterPosition.Z);

				var actor = w.CreateActor(false, info.MissileActor, new TypeDictionary
				{
					new CenterPositionInit(startPos + new WVec(0, 0, info.LaunchAltitude.GetValueOrDefault(targetAltitude).Length)),
					new OwnerInit(self.Owner),
					new FacingInit(spawnFacing)
				});

				// todo: implement dynamic speed
				// var dynamicSpeedMultiplier = actor.TraitOrDefault<DynamicSpeedMultiplier>();

				var gm = actor.Trait<MissileBase>();
				gm.SetTarget(target);
				w.Add(actor);
			});
		}

		private IEnumerable<Actor> GetValidTargetActorsInCircle(CPos xy)
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

		class SelectMissileStrikeTarget : OrderGenerator
		{
			readonly MissileStrikePower power;
			readonly SupportPowerManager manager;
			readonly string order;

			public SelectMissileStrikeTarget(World world, string order, SupportPowerManager manager, MissileStrikePower power)
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
				if (mi.Button == MouseButton.Left && (!power.info.TargetActors || power.GetActorTargets(cell).Count() >= power.info.MinTargets))
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

				if (power.info.TargetActors)
				{
					var targetUnits = power.GetActorTargets(xy);

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
			}

			protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);

				if (power.info.TargetActors && power.info.TargetTintColor != null)
				{
					var targetUnits = power.GetActorTargets(xy);

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
				if (!power.info.TargetActors)
					return power.info.Cursor;

				var targets = power.GetActorTargets(cell);
				return targets.Count() >= power.info.MinTargets ? power.info.Cursor : power.info.BlockedCursor;
			}
		}
	}
}
