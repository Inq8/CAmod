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
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Actor can deploy to be able to target a location and spawn an actor there.")]
	public class SpawnActorAbilityInfo : PausableConditionalTraitInfo, Requires<IMoveInfo>
	{
		[Desc("Range.")]
		public readonly WDist Range = WDist.Zero;

		[VoiceReference]
		public readonly string Voice = "Action";

		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actor to spawn.")]
		public readonly string Actor = null;

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

		[Desc("Color to use for the target line.")]
		public readonly Color TargetLineColor = Color.LawnGreen;

		[Desc("Skips the spawned actor's make animations if true.")]
		public readonly bool SkipMakeAnimations = true;

		[Desc("If true allow targeting in shroud.")]
		public readonly bool CanTargetShroud = true;

		[Desc("Play a randomly selected sound from this list when spawning the actor.")]
		public readonly string[] SpawnSounds = Array.Empty<string>();

		[NotificationReference("Speech")]
		[Desc("Speech notification for target selection.")]
		public readonly string SelectTargetSpeechNotification = null;

		[Desc("Consume ammo from this ammo pool on use.")]
		public readonly string AmmoPool = null;

		public override object Create(ActorInitializer init) { return new SpawnActorAbility(init, this); }
	}

	public class SpawnActorAbility : PausableConditionalTrait<SpawnActorAbilityInfo>, INotifyCreated, IIssueOrder, IResolveOrder, IOrderVoice, IIssueDeployOrder
	{
		public new readonly SpawnActorAbilityInfo Info;

		readonly IMove move;
		readonly string faction;
		AmmoPool ammoPool;

		public SpawnActorAbility(ActorInitializer init, SpawnActorAbilityInfo info)
			: base(info)
		{
			Info = info;
			move = init.Self.Trait<IMove>();
			faction = init.GetValue<FactionInit, string>(init.Self.Owner.Faction.InternalName);
		}

		protected override void Created(Actor self)
		{
			ammoPool = self.TraitsImplementing<AmmoPool>().SingleOrDefault(ap => ap.Info.Name == Info.AmmoPool);
			base.Created(self);
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			// HACK: Switch the global order generator instead of actually issuing an order
			if (CanSpawnActor)
				self.World.OrderGenerator = new SpawnActorAbilityOrderGenerator(self, this);

			// HACK: We need to issue a fake order to stop the game complaining about the bodge above
			return new Order("SpawnActorAbilityDeploy", self, Target.Invalid, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return !IsTraitPaused && !IsTraitDisabled; }

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

				yield return new DeployOrderTargeter("SpawnActorAbilityDeploy", 5,
					() => CanSpawnActor ? Info.DeployCursor : Info.DeployBlockedCursor);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "SpawnActorAbilityDeploy")
			{
				// HACK: Switch the global order generator instead of actually issuing an order
				if (CanSpawnActor)
					self.World.OrderGenerator = new SpawnActorAbilityOrderGenerator(self, this);

				// HACK: We need to issue a fake order to stop the game complaining about the bodge above
				return new Order(order.OrderID, self, Target.Invalid, queued);
			}

			if (order.OrderID == "SpawnActorAbility")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SpawnActorAbility" && order.Target.Type != TargetType.Invalid)
			{
				if (!order.Queued)
					self.CancelActivity();

				var cell = self.World.Map.CellContaining(order.Target.CenterPosition);
				if (Info.Range > WDist.Zero)
					self.QueueActivity(move.MoveWithinRange(order.Target, Info.Range, targetLineColor: Info.TargetLineColor));

				self.QueueActivity(new SpawnActor(self, cell, order.Target.CenterPosition, Info.Actor, Info.SkipMakeAnimations, Info.SpawnSounds, ammoPool, 3, true));
				self.ShowTargetLines();
			}
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "SpawnActorAbility" ? Info.Voice : null;
		}

		public bool CanSpawnActor
		{
			get { return ammoPool == null || ammoPool.HasAmmo; }
		}
	}

	class SpawnActorAbilityOrderGenerator : OrderGenerator
	{
		readonly Actor self;
		readonly SpawnActorAbility ability;
		readonly SpawnActorAbilityInfo info;

		public SpawnActorAbilityOrderGenerator(Actor self, SpawnActorAbility ability)
		{
			this.self = self;
			this.ability = ability;
			info = ability.Info;

			if (ability.Info.SelectTargetSpeechNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
					ability.Info.SelectTargetSpeechNotification, self.Owner.Faction.InternalName);
		}

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
			{
				world.CancelInputMode();
				yield break;
			}

			if (self.IsInWorld && self.Location != cell
				&& ability.CanSpawnActor
				&& (info.CanTargetShroud || self.Owner.Shroud.IsExplored(cell)))
			{
				world.CancelInputMode();
				yield return new Order("SpawnActorAbility", self, Target.FromCell(world, cell), mi.Modifiers.HasModifier(Modifiers.Shift));
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

			if (info.Range == WDist.Zero)
				yield break;

			yield return new RangeCircleAnnotationRenderable(
				self.CenterPosition + new WVec(0, self.CenterPosition.Z, 0),
				info.Range,
				0,
				info.CircleColor,
				info.CircleWidth,
				info.CircleBorderColor,
				info.CircleBorderWidth);
		}

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (self.IsInWorld && self.Location != cell
				&& ability.CanSpawnActor
				&& (info.CanTargetShroud || self.Owner.Shroud.IsExplored(cell)))
				return info.TargetCursor;
			else
				return info.TargetBlockedCursor;
		}
	}
}
