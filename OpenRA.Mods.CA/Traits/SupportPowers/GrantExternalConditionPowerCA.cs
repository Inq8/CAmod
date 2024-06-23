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

		[Desc("Sound to instantly play at the targeted area.")]
		public readonly string OnFireSound = null;

		[Desc("Target types that condition can be applied to. Leave empty for all types.")]
		public readonly BitSet<TargetableType> ValidTargets = default;

		[Desc("Target types that condition can be applied to. Leave empty for all types.")]
		public readonly BitSet<TargetableType> InvalidTargets = default;

		[Desc("Player relationships which condition can be applied to.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		[Desc("If true, targets must be owned by the player using the support power (overrides ValidRelationships).")]
		public readonly bool OwnedTargetsOnly = false;

		[CursorReference]
		[Desc("Cursor to display when there are no units to apply the condition in range.")]
		public readonly string BlockedCursor = "move-blocked";

		[Desc("If true, targets must not be under shroud/fog.")]
		public readonly bool TargetMustBeVisible = true;

		[Desc("Maximum number of targets. Zero for no limit.")]
		public readonly int MaxTargets = 0;

		[Desc("Minimum targets for power to activate.")]
		public readonly int MinTargets = 1;

		[Desc("If true, targets must not be under shroud/fog.")]
		public readonly bool ShowTargetCount = false;

		[Desc("Font to use for target count.")]
		public readonly string TargetCountFont = "Medium";

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

		[Desc("Target tint colour.")]
		public readonly Color? TargetTintColor = null;

		[Desc("Maximum altitude of targets.")]
		public readonly WDist MaxAltitude = WDist.Zero;

		public WeaponInfo WeaponInfo { get; private set; }

		// Circle mode only

		[Desc("Range in which to apply condition.")]
		public readonly WDist Range = WDist.Zero;

		// Footprint mode only

		[Desc("Size of the footprint of the affected area.")]
		public readonly CVec Dimensions = CVec.Zero;

		[Desc("Actual footprint. Cells marked as x will be affected.")]
		public readonly string Footprint = string.Empty;

		public readonly string FootprintImage = "overlay";

		[SequenceReference(nameof(FootprintImage))]
		public readonly string FootprintSequence = "target-select";

		[Desc("Prerequisites grouped together to be referenced by the Prerequisite based overrides.")]
		public readonly Dictionary<string, string[]> PrerequisiteGroupings = new();

		[Desc("Overrides Condition based on prerequsites being met. If multiple are met, the first is used.",
			"Keys can either be a single prerequisite or be a key of PrerequisiteGroupings.")]
		public readonly Dictionary<string, string> PrerequisiteConditions = new();

		public override object Create(ActorInitializer init) { return new GrantExternalConditionPowerCA(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (ExplosionWeapon != null)
				WeaponInfo = rules.Weapons[ExplosionWeapon.ToLowerInvariant()];
		}
	}

	public class GrantExternalConditionPowerCA : SupportPower, ITick, INotifyCreated
	{
		readonly GrantExternalConditionPowerCAInfo info;
		int activeToken = Actor.InvalidConditionToken;
		IConditionTimerWatcher[] watchers;
		readonly char[] footprint;
		TechTree techTree;

		[Sync]
		public int Ticks { get; private set; }

		public GrantExternalConditionPowerCA(Actor self, GrantExternalConditionPowerCAInfo info)
			: base(self, info)
		{
			this.info = info;
			footprint = info.Footprint.Where(c => !char.IsWhiteSpace(c)).ToArray();
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			watchers = self.TraitsImplementing<IConditionTimerWatcher>().Where(Notifies).ToArray();
			techTree = self.Owner.PlayerActor.Trait<TechTree>();
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.OrderGenerator = new SelectConditionTarget(Self.World, order, manager, this);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			PlayLaunchSounds();

			if (!string.IsNullOrEmpty(info.ActiveCondition) && activeToken == Actor.InvalidConditionToken)
			{
				Ticks = info.Duration;
				activeToken = self.GrantCondition(info.ActiveCondition);
			}

			var wsb = self.TraitOrDefault<WithSpriteBody>();
			if (wsb != null && wsb.DefaultAnimation.HasSequence(info.ActiveSequence))
				wsb.PlayCustomAnimation(self, info.ActiveSequence);

			Game.Sound.Play(SoundType.World, info.OnFireSound, order.Target.CenterPosition);

			foreach (var a in GetTargets(self.World.Map.CellContaining(order.Target.CenterPosition)))
				a.TraitsImplementing<ExternalCondition>()
					.FirstOrDefault(t => t.Info.Condition == Condition && t.CanGrantCondition(self))
					?.GrantCondition(a, self, info.Duration);

			if (info.ExplosionWeapon != null)
			{
				var targetPosition = order.Target.CenterPosition;

				Action detonateWeapon = () => self.World.AddFrameEndTask(w => info.WeaponInfo.Impact(Target.FromPos(targetPosition), self));

				self.World.AddFrameEndTask(w => w.Add(new DelayedAction(info.ExplosionDelay, detonateWeapon)));
			}
		}

		private IEnumerable<Actor> GetTargets(CPos xy)
		{
			if (info.Footprint.Length > 0)
				return GetTargetsInFootprint(xy);

			return GetTargetsInCircle(xy);
		}

		private IEnumerable<Actor> GetTargetsInCircle(CPos xy)
		{
			var centerPos = Self.World.Map.CenterOfCell(xy);

			var actorsInRange = Self.World.FindActorsInCircle(centerPos, info.Range)
				.Where(a => {
					return IsValidTarget(a);
				})
				.OrderBy(a => (a.CenterPosition - centerPos).LengthSquared);

			if (info.MaxTargets > 0)
				return actorsInRange.Take(info.MaxTargets);

			return actorsInRange;
		}

		private IEnumerable<Actor> GetTargetsInFootprint(CPos xy)
		{
			var tiles = CellsMatching(xy, footprint, info.Dimensions);
			var units = new List<Actor>();
			foreach (var t in tiles)
				units.AddRange(Self.World.ActorMap.GetActorsAt(t));

			return units.Distinct().Where(a =>
			{
				return IsValidTarget(a);
			});
		}

		bool IsValidTarget(Actor a)
		{
			if (a.IsDead || !a.IsInWorld)
				return false;

			if (!info.ValidRelationships.HasRelationship(Self.Owner.RelationshipWith(a.Owner)))
				return false;

			if (info.OwnedTargetsOnly && a.Owner != Self.Owner)
				return false;

			var enabledTargetTypes = a.GetEnabledTargetTypes();

			if (!info.ValidTargets.IsEmpty && !info.ValidTargets.Overlaps(enabledTargetTypes))
				return false;

			if (!info.InvalidTargets.IsEmpty && info.InvalidTargets.Overlaps(enabledTargetTypes))
				return false;

			if (!a.TraitsImplementing<ExternalCondition>().Any(t => t.Info.Condition == Condition && t.CanGrantCondition(Self)))
				return false;

			if (info.TargetMustBeVisible && !Self.Owner.Shroud.IsVisible(a.Location))
				return false;

			if (!a.CanBeViewedByPlayer(Self.Owner))
				return false;

			if (a.CenterPosition.Z > info.MaxAltitude.Length)
				return false;

			return true;
		}

		void RevokeCondition(Actor self)
		{
			if (activeToken != Actor.InvalidConditionToken)
				activeToken = self.RevokeCondition(activeToken);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled && activeToken != Actor.InvalidConditionToken)
				RevokeCondition(self);

			if (IsTraitPaused || IsTraitDisabled)
				return;

			foreach (var w in watchers)
				w.Update(info.Duration, Ticks);

			if (activeToken == Actor.InvalidConditionToken)
				return;

			if (--Ticks < 1)
				RevokeCondition(self);
		}

		bool Notifies(IConditionTimerWatcher watcher) { return watcher.Condition == info.ActiveCondition; }

		string Condition
		{
			get
			{
				if (info.PrerequisiteConditions.Any())
				{
					foreach (var item in info.PrerequisiteConditions)
					{
						if (techTree.HasPrerequisites(GetPrerequisitesList(item.Key)))
							return item.Value;
					}
				}

				return info.Condition;
			}
		}

		string[] GetPrerequisitesList(string key)
		{
			if (info.PrerequisiteGroupings.TryGetValue(key, out var prerequisites))
				return prerequisites;

			return new string[] { key };
		}

		class SelectConditionTarget : OrderGenerator
		{
			readonly GrantExternalConditionPowerCA power;
			readonly char[] footprint;
			readonly CVec dimensions;
			readonly Sprite tile;
			readonly float alpha;
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

				if (power.info.Footprint.Length > 0)
				{
					footprint = power.info.Footprint.Where(c => !char.IsWhiteSpace(c)).ToArray();
					dimensions = power.info.Dimensions;
					var sequence = world.Map.Sequences.GetSequence(power.info.FootprintImage, power.info.FootprintSequence);
					tile = sequence.GetSprite(0);
					alpha = sequence.GetAlpha(0);
				}
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

				if (power.info.ShowTargetCount)
				{
					var font = Game.Renderer.Fonts[power.info.TargetCountFont];
					var color = power.info.TargetCircleColor;
					var text = power.info.MaxTargets > 0 ? $"{targetUnits.Count()} / {power.info.MaxTargets}" : targetUnits.Count().ToString();
					var size = font.Measure(text);
					var textPos = new int2(Viewport.LastMousePos.X - (size.X / 2), Viewport.LastMousePos.Y + size.Y + (size.Y / 3));
					yield return new UITextRenderable(font, WPos.Zero, textPos, 0, color, text);
				}
			}

			protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);

				if (power.info.Footprint.Length > 0)
				{
					var pal = wr.Palette(TileSet.TerrainPaletteInternalName);

					foreach (var t in power.CellsMatching(xy, footprint, dimensions))
						yield return new SpriteRenderable(tile, wr.World.Map.CenterOfCell(t), WVec.Zero, -511, pal, 1f, alpha, float3.Ones, TintModifiers.IgnoreWorldTint, true);
				}

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
