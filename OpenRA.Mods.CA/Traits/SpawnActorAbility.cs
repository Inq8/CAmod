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

		[CursorReference]
		[Desc("Cursor to display when targeting a teleport location with modifier key held.")]
		public readonly string TargetModifiedCursor = null;

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
		public readonly Color TargetLineColor = Color.LawnGreen;

		[Desc("Skips the spawned actor's make animations if true.")]
		public readonly bool SkipMakeAnimations = true;

		[Desc("If true allow targeting in shroud.")]
		public readonly bool CanTargetShroud = true;

		[Desc("List of valid terrain types to spawn at. Leave blank for any.")]
		public readonly HashSet<string> AllowedTerrainTypes = new();

		[Desc("Play a randomly selected sound from this list when spawning the actor.")]
		public readonly string[] SpawnSounds = Array.Empty<string>();

		[NotificationReference("Speech")]
		[Desc("Speech notification for target selection.")]
		public readonly string SelectTargetSpeechNotification = null;

		[Desc("Consume ammo from this ammo pool on use.")]
		public readonly string AmmoPool = null;

		[Desc("When selecting a group, different types will not be activated together.")]
		public readonly string Type = null;

		public override object Create(ActorInitializer init) { return new SpawnActorAbility(init, this); }
	}

	public class SpawnActorAbility : PausableConditionalTrait<SpawnActorAbilityInfo>, INotifyCreated, IIssueOrder, IResolveOrder, IOrderVoice, IIssueDeployOrder
	{
		public new readonly SpawnActorAbilityInfo Info;

		readonly IMove move;
		AmmoPool ammoPool;

		public SpawnActorAbility(ActorInitializer init, SpawnActorAbilityInfo info)
			: base(info)
		{
			Info = info;
			move = init.Self.Trait<IMove>();
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

				self.QueueActivity(new SpawnActor(self, cell, order.Target.CenterPosition, Info.Actor, Info.SkipMakeAnimations, Info.SpawnSounds, ammoPool, 3, true, Info.Range, Info.CanTargetShroud, Info.AllowedTerrainTypes));
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
		readonly IEnumerable<TraitPair<SpawnActorAbility>> selectedWithAbility;

		public SpawnActorAbilityOrderGenerator(Actor self, SpawnActorAbility ability)
		{
			this.self = self;
			this.ability = ability;
			info = ability.Info;

			if (ability.Info.SelectTargetSpeechNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
					ability.Info.SelectTargetSpeechNotification, self.Owner.Faction.InternalName);

			selectedWithAbility = self.World.Selection.Actors
				.Where(a => a.Info.HasTraitInfo<SpawnActorAbilityInfo>() && a.Owner == self.Owner && !a.IsDead)
				.Select(a => new TraitPair<SpawnActorAbility>(a, a.Trait<SpawnActorAbility>()))
				.Where(s => s.Trait.Info.Type == ability.Info.Type);
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
				var targetCell = Target.FromCell(world, cell);

				var selectedOrderedByDistance = selectedWithAbility
					.Where(a => !a.Actor.IsDead
						&& a.Actor.Owner == self.Owner
						&& a.Actor.IsInWorld
						&& a.Trait.CanSpawnActor)
					.OrderBy(a => (a.Actor.CenterPosition - targetCell.CenterPosition).Length);

				if (mi.Modifiers.HasModifier(Modifiers.Ctrl))
				{
					foreach (var other in selectedOrderedByDistance)
						yield return new Order("SpawnActorAbility", other.Actor, Target.FromCell(world, cell), mi.Modifiers.HasModifier(Modifiers.Shift));
				}
				else
				{
					var closest = selectedOrderedByDistance.First();
					yield return new Order("SpawnActorAbility", self, Target.FromCell(world, cell), mi.Modifiers.HasModifier(Modifiers.Shift));
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

			foreach (var other in selectedWithAbility)
			{
				if (other.Actor.IsInWorld && other.Trait.CanSpawnActor && self.Owner == self.World.LocalPlayer)
				{
					yield return new RangeCircleAnnotationRenderable(
						other.Actor.CenterPosition + new WVec(0, other.Actor.CenterPosition.Z, 0),
						other.Trait.Info.Range,
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
				&& ability.CanSpawnActor
				&& (info.CanTargetShroud || self.Owner.Shroud.IsExplored(cell))
				&& (info.AllowedTerrainTypes.Count == 0 || info.AllowedTerrainTypes.Contains(world.Map.GetTerrainInfo(cell).Type)))
				return info.TargetModifiedCursor != null && mi.Modifiers.HasModifier(Modifiers.Ctrl) ? info.TargetModifiedCursor : info.TargetCursor;
			else
				return info.TargetBlockedCursor;
		}
	}
}
