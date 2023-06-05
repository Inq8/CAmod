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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Actor can deploy to be able to target a location and fire a weapon at that target.",
		"Relies on the armament being paused if the ActiveCondition is not applied.")]
	public class TargetedAttackAbilityInfo : PausableConditionalTraitInfo, Requires<AttackBaseInfo>
	{
		[VoiceReference]
		public readonly string Voice = "Action";

		[CursorReference]
		[Desc("Cursor to display when able to deploy the actor.")]
		public readonly string DeployCursor = "deploy";

		[CursorReference]
		[Desc("Cursor to display when unable to deploy the actor.")]
		public readonly string DeployBlockedCursor = "deploy-blocked";

		[CursorReference]
		[Desc("Cursor to display when targeting a teleport location.")]
		public readonly string TargetCursor = "ability";

		[CursorReference]
		[Desc("Cursor to display when the targeted location is blocked.")]
		public readonly string TargetBlockedCursor = "move-blocked";

		[Desc("Range circle color.")]
		public readonly Color CircleColor = Color.FromArgb(128, Color.LawnGreen);

		[Desc("Range circle line width.")]
		public readonly float CircleWidth = 1;

		[Desc("Range circle border color.")]
		public readonly Color CircleBorderColor = Color.FromArgb(96, Color.Black);

		[Desc("Range circle border width.")]
		public readonly float CircleBorderWidth = 3;

		public readonly WDist TargetCircleRange = WDist.Zero;
		public readonly Color TargetCircleColor = Color.White;

		[Desc("Color to use for the target line.")]
		public readonly Color TargetLineColor = Color.Magenta;

		[Desc("If true allow targeting in shroud.")]
		public readonly bool CanTargetShroud = true;

		[GrantedConditionReference]
		[Desc("Condition to apply while the targeted ability attack is being carried out.")]
		public readonly string ActiveCondition = null;

		[Desc("Name of the armament used to attack with.")]
		public readonly string ArmamentName = "primary";

		[Desc("Ability type. When selecting a group, different types will not be activated together.")]
		public readonly string Type = null;

		public override object Create(ActorInitializer init) { return new TargetedAttackAbility(init, this); }
	}

	public class TargetedAttackAbility : PausableConditionalTrait<TargetedAttackAbilityInfo>, INotifyCreated, IIssueOrder, IResolveOrder,
		IOrderVoice, IIssueDeployOrder, INotifyBurstComplete
	{
		public new readonly TargetedAttackAbilityInfo Info;
		public readonly Armament Armament;
		readonly AttackBase attack;
		int conditionToken = Actor.InvalidConditionToken;

		public TargetedAttackAbility(ActorInitializer init, TargetedAttackAbilityInfo info)
			: base(info)
		{
			Info = info;
			Armament = init.Self.TraitsImplementing<Armament>()
				.Single(a => a.Info.Name == Info.ArmamentName);
			attack = init.Self.Trait<AttackBase>();
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			// HACK: Switch the global order generator instead of actually issuing an order
			if (IsAvailable)
				self.World.OrderGenerator = new TargetedAttackAbilityOrderGenerator(self, this);

			// HACK: We need to issue a fake order to stop the game complaining about the bodge above
			return new Order("TargetedAttackAbilityDeploy", self, Target.Invalid, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return !IsTraitPaused && !IsTraitDisabled; }

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

				yield return new DeployOrderTargeter("TargetedAttackAbilityDeploy", 5,
					() => IsAvailable ? Info.DeployCursor : Info.DeployBlockedCursor);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "TargetedAttackAbilityDeploy")
			{
				// HACK: Switch the global order generator instead of actually issuing an order
				if (IsAvailable)
					self.World.OrderGenerator = new TargetedAttackAbilityOrderGenerator(self, this);

				// HACK: We need to issue a fake order to stop the game complaining about the bodge above
				return new Order(order.OrderID, self, Target.Invalid, queued);
			}

			if (order.OrderID == "TargetedAttackAbilityAttack")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return;

			if (order.OrderString == "TargetedAttackAbilityAttack" && order.Target.Type != TargetType.Invalid)
			{
				Enable(self);
				attack.AttackTarget(order.Target, AttackSource.Default, false, true, true, Info.TargetLineColor);
			}
			else
			{
				Disable(self);
			}
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "TargetedAttackAbilityAttack" ? Info.Voice : null;
		}

		public bool IsAvailable
		{
			get { return !IsTraitDisabled && !IsTraitPaused && !Armament.IsTraitDisabled && !Armament.IsReloading; }
		}

		void Enable(Actor self)
		{
			if (conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.ActiveCondition);
		}

		void Disable(Actor self)
		{
			if (conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);
		}

		void INotifyBurstComplete.FiredBurst(Actor self, in Target target, Armament a)
		{
			if (Info.ArmamentName != a.Info.Name)
				return;

			if (target.Type == TargetType.Terrain || target.Type == TargetType.Invalid)
				self.CancelActivity();

			Disable(self);
		}
	}

	class TargetedAttackAbilityOrderGenerator : OrderGenerator
	{
		readonly Actor self;
		readonly TargetedAttackAbility ability;
		readonly TargetedAttackAbilityInfo info;
		readonly IEnumerable<TraitPair<TargetedAttackAbility>> selectedWithAbility;

		public TargetedAttackAbilityOrderGenerator(Actor self, TargetedAttackAbility ability)
		{
			this.self = self;
			this.ability = ability;
			info = ability.Info;

			selectedWithAbility = self.World.Selection.Actors
				.Where(a => a.Info.HasTraitInfo<TargetedAttackAbilityInfo>() && a != self && a.Owner == self.Owner)
				.Select(a => new TraitPair<TargetedAttackAbility>(a, a.Trait<TargetedAttackAbility>()))
				.Where(s => s.Trait.Info.Type == ability.Info.Type);
		}

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
			{
				world.CancelInputMode();
				yield break;
			}

			if (self.IsInWorld && ability.IsAvailable
				&& (info.CanTargetShroud || self.Owner.Shroud.IsExplored(cell)))
			{
				world.CancelInputMode();

				var underCursor = world.ScreenMap.ActorsAtMouse(mi)
					.Select(a => a.Actor)
					.FirstOrDefault(a => !world.FogObscures(a));

				var target = underCursor != null ? Target.FromActor(underCursor) : Target.FromCell(world, cell);

				yield return new Order("TargetedAttackAbilityAttack", self, target, mi.Modifiers.HasModifier(Modifiers.Shift));

				foreach (var other in selectedWithAbility)
				{
					if (other.Actor.IsInWorld && other.Trait.IsAvailable && other.Actor.Owner == self.Owner)
					{
						yield return new Order("TargetedAttackAbilityAttack", other.Actor, target, mi.Modifiers.HasModifier(Modifiers.Shift));
					}
				}
			}
		}

		protected override void SelectionChanged(World world, IEnumerable<Actor> selected)
		{
			if (!selected.Contains(self))
				world.CancelInputMode();
		}

		protected override void Tick(World world)
		{
			if (ability.IsTraitDisabled || ability.IsTraitPaused)
			{
				world.CancelInputMode();
				return;
			}
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }

		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
		{
			if (!self.IsInWorld || self.Owner != self.World.LocalPlayer)
				yield break;

			if (ability.Armament.MaxRange() == WDist.Zero)
				yield break;

			yield return new RangeCircleAnnotationRenderable(
				self.CenterPosition + new WVec(0, self.CenterPosition.Z, 0),
				ability.Armament.MaxRange(),
				0,
				info.CircleColor,
				info.CircleWidth,
				info.CircleBorderColor,
				info.CircleBorderWidth);

			foreach (var other in selectedWithAbility)
			{
				if (other.Actor.IsInWorld && other.Trait.IsAvailable && self.Owner == self.World.LocalPlayer)
				{
					yield return new RangeCircleAnnotationRenderable(
						other.Actor.CenterPosition + new WVec(0, other.Actor.CenterPosition.Z, 0),
						other.Trait.Armament.MaxRange(),
						0,
						other.Trait.Info.CircleColor,
						other.Trait.Info.CircleWidth,
						other.Trait.Info.CircleBorderColor,
						other.Trait.Info.CircleBorderWidth);
				}
			}

			if (ability.Info.TargetCircleRange > WDist.Zero)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);

				yield return new RangeCircleAnnotationRenderable(
					world.Map.CenterOfCell(xy),
					ability.Info.TargetCircleRange,
					0,
					ability.Info.TargetCircleColor,
					1,
					Color.FromArgb(96, Color.Black),
					3);
			}
		}

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (self.IsInWorld && self.Location != cell
				&& ability.IsAvailable
				&& (info.CanTargetShroud || self.Owner.Shroud.IsExplored(cell)))
				return info.TargetCursor;
			else
				return info.TargetBlockedCursor;
		}
	}
}
