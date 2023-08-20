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
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class GrantExternalConditionPowerCAInfo : SupportPowerInfo
	{
		[FieldLoader.Require]
		[Desc("The condition to apply. Must be included in the target actor's ExternalConditions list.")]
		public readonly string Condition = null;

		[Desc("Duration of the condition (in ticks). Set to 0 for a permanent condition.")]
		public readonly int Duration = 0;

		[FieldLoader.Require]
		[Desc("Range in which to apply condition.")]
		public readonly WDist Range = WDist.Zero;

		[Desc("Sound to instantly play at the targeted area.")]
		public readonly string OnFireSound = null;

		[Desc("Target types that condition can be applied to. Leave empty for all types.")]
		public readonly BitSet<TargetableType> ValidTargets = default(BitSet<TargetableType>);

		[Desc("Player relationships which condition can be applied to.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		[CursorReference]
		[Desc("Cursor to display when there are no units to apply the condition in range.")]
		public readonly string BlockedCursor = "move-blocked";

		[Desc("If true, targets must not be under shroud/fog.")]
		public readonly bool TargetMustBeVisible = true;

		[Desc("Maximum number of targets. Zero for no limit.")]
		public readonly int MaxTargets = 0;

		[Desc("Minimum targets for power to activate.")]
		public readonly int MinTargets = 1;

		[Desc("Font to use for target count.")]
		public readonly string TargetCountFont = "Regular";

		[WeaponReference]
		[Desc("Weapon to detonate at target location.")]
		public readonly string ExplosionWeapon = null;

		[Desc("Delay between activation and explosion")]
		public readonly int ExplosionDelay = 0;

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

		public WeaponInfo WeaponInfo { get; private set; }

		public override object Create(ActorInitializer init) { return new GrantExternalConditionPowerCA(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (ExplosionWeapon != null)
				WeaponInfo = rules.Weapons[ExplosionWeapon.ToLowerInvariant()];
		}
	}

	public class GrantExternalConditionPowerCA : SupportPower
	{
		readonly GrantExternalConditionPowerCAInfo info;

		public GrantExternalConditionPowerCA(Actor self, GrantExternalConditionPowerCAInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.OrderGenerator = new SelectConditionTarget(Self.World, order, manager, this);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			PlayLaunchSounds();

			var wsb = self.TraitOrDefault<WithSpriteBody>();
			if (wsb != null && wsb.DefaultAnimation.HasSequence(info.ActiveSequence))
				wsb.PlayCustomAnimation(self, info.ActiveSequence);

			Game.Sound.Play(SoundType.World, info.OnFireSound, order.Target.CenterPosition);

			foreach (var a in GetTargets(self.World.Map.CellContaining(order.Target.CenterPosition)))
				a.TraitsImplementing<ExternalCondition>()
					.FirstOrDefault(t => t.Info.Condition == info.Condition && t.CanGrantCondition(self))
					?.GrantCondition(a, self, info.Duration);

			if (info.ExplosionWeapon != null)
			{
				var targetPosition = order.Target.CenterPosition;

				Action detonateWeapon = () => self.World.AddFrameEndTask(w => info.WeaponInfo.Impact(Target.FromPos(targetPosition), self));

				self.World.AddFrameEndTask(w => w.Add(new DelayedAction(info.ExplosionDelay, detonateWeapon)));
			}
		}

		public IEnumerable<Actor> GetTargets(CPos xy)
		{
			var centerPos = Self.World.Map.CenterOfCell(xy);

			var actorsInRange = Self.World.FindActorsInCircle(centerPos, info.Range)
				.Where(a => a.IsInWorld
					&& !a.IsDead
					&& info.ValidRelationships.HasRelationship(Self.Owner.RelationshipWith(a.Owner))
					&& (info.ValidTargets.IsEmpty || info.ValidTargets.Overlaps(a.GetAllTargetTypes()))
					&& a.TraitsImplementing<ExternalCondition>().Any(t => t.Info.Condition == info.Condition && t.CanGrantCondition(Self))
					&& !(info.TargetMustBeVisible && (Self.World.ShroudObscures(a.Location) || Self.World.FogObscures(a.Location)))
					&& a.CanBeViewedByPlayer(Self.Owner))
				.OrderBy(a => (a.CenterPosition - centerPos).LengthSquared);

			if (info.MaxTargets > 0)
				return actorsInRange.Take(info.MaxTargets);

			return actorsInRange;
		}

		class SelectConditionTarget : OrderGenerator
		{
			readonly GrantExternalConditionPowerCA power;
			readonly SupportPowerManager manager;
			readonly string order;

			public SelectConditionTarget(World world, string order, SupportPowerManager manager, GrantExternalConditionPowerCA power)
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
							foreach (var d in decorations.RenderSelectionAnnotations(unit, wr, power.info.SelectionBoxColor))
								yield return d;
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
					var textPos = new int2(Viewport.LastMousePos.X - (size.X / 2), Viewport.LastMousePos.Y + size.Y + (size.Y / 2));
					yield return new UITextRenderable(font, WPos.Zero, textPos, 0, color, text);
				}
			}

			protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
			{
				yield break;
			}

			protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				var targets = power.GetTargets(cell);
				return targets.Count() >= power.info.MinTargets ? power.info.Cursor : power.info.BlockedCursor;
			}
		}
	}
}
