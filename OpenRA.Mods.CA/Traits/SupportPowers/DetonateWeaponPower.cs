#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Support power for detonating a weapon at the target position.")]
	public class DetonateWeaponPowerInfo : SupportPowerInfo, IRulesetLoaded
	{
		[WeaponReference]
		public readonly string Weapon = "";

		[Desc("Delay between activation and explosion")]
		public readonly int ActivationDelay = 10;

		[Desc("Amount of time before detonation to remove the beacon")]
		public readonly int BeaconRemoveAdvance = 5;

		[ActorReference]
		[Desc("Actor to spawn before detonation")]
		public readonly string CameraActor = null;

		[Desc("Amount of time before detonation to spawn the camera")]
		public readonly int CameraSpawnAdvance = 5;

		[Desc("Amount of time after detonation to remove the camera")]
		public readonly int CameraRemoveDelay = 5;

		[Desc("A condition to apply while active.")]
		public readonly string ActiveCondition = null;

		[Desc("Palette effect on the world actor.")]
		public readonly string PaletteEffectType = null;

		[SequenceReference]
		[Desc("Sequence to play for granting actor when activated.",
	"This requires the actor to have the WithSpriteBody trait or one of its derivatives.")]
		public readonly string Sequence = "active";

		[Desc("Duration of the condition (in ticks). Set to 0 for a permanent condition.")]
		public readonly int Duration = 0;

		[Desc("Altitude above terrain below which to explode. Zero effectively deactivates airburst.")]
		public readonly WDist AirburstAltitude = WDist.Zero;

		public readonly WDist TargetCircleRange = WDist.Zero;
		public readonly Color TargetCircleColor = Color.White;
		public readonly bool TargetCircleUsePlayerColor = false;

		[Desc("Amount of time to keep the actor alive in ticks. Value < 0 means this actor will not remove itself.")]
		public readonly int LifeTime = 250;

		public WeaponInfo WeaponInfo { get; private set; }

		public override object Create(ActorInitializer init) { return new DetonateWeaponPower(init.Self, this); }
		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			WeaponInfo = rules.Weapons[Weapon.ToLowerInvariant()];
		}
	}

	public class DetonateWeaponPower : SupportPower, ITick
	{
		public new readonly DetonateWeaponPowerInfo Info;

		[Sync]
		int ticks;

		int activeToken = Actor.InvalidConditionToken;

		public DetonateWeaponPower(Actor self, DetonateWeaponPowerInfo info)
			: base(self, info)
		{
			Info = info;
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			PlayLaunchSounds();

			if (!string.IsNullOrEmpty(Info.ActiveCondition) && activeToken == Actor.InvalidConditionToken)
				activeToken = self.GrantCondition(Info.ActiveCondition);

			var wsb = self.TraitOrDefault<WithSpriteBody>();
			if (wsb != null && wsb.DefaultAnimation.HasSequence(Info.Sequence))
				wsb.PlayCustomAnimation(self, Info.Sequence);

			foreach (var launchpad in self.TraitsImplementing<INotifySupportPower>())
				launchpad.Activated(self);

			ticks = Info.Duration;

			var targetPosition = order.Target.CenterPosition + new WVec(WDist.Zero, WDist.Zero, Info.AirburstAltitude);

			Action detonateWeapon = () => self.World.AddFrameEndTask(w => Info.WeaponInfo.Impact(Target.FromPos(targetPosition), self));

			self.World.AddFrameEndTask(w => w.Add(new DelayedAction(Info.ActivationDelay, detonateWeapon)));

			self.World.AddFrameEndTask(w =>
			{
				if (!string.IsNullOrEmpty(Info.PaletteEffectType))
				{
					var paletteEffects = w.WorldActor.TraitsImplementing<WeatherPaletteEffect>().Where(p => p.Info.Type == Info.PaletteEffectType);
					foreach (var paletteEffect in paletteEffects)
						paletteEffect.Enable(-1);
				}

				var actor = w.CreateActor(Info.CameraActor, new TypeDictionary
					{
						new LocationInit(self.World.Map.CellContaining(order.Target.CenterPosition)),
						new OwnerInit(self.Owner),
					});

				if (Info.CameraRemoveDelay > -1)
				{
					actor.QueueActivity(new Wait(Info.CameraRemoveDelay));
					actor.QueueActivity(new RemoveSelf());
				}
			});
			if (Info.DisplayBeacon)
			{
				var beacon = new Beacon(
					order.Player,
					order.Target.CenterPosition,
					Info.BeaconPaletteIsPlayerPalette,
					Info.BeaconPalette,
					Info.BeaconImage,
					Info.BeaconPoster,
					Info.BeaconPosterPalette,
					Info.BeaconSequence,
					Info.ArrowSequence,
					Info.CircleSequence,
					Info.ClockSequence,
					() => FractionComplete);

				Action removeBeacon = () => self.World.AddFrameEndTask(w =>
				{
					w.Remove(beacon);
					beacon = null;
				});

				self.World.AddFrameEndTask(w =>
				{
					w.Add(beacon);
					w.Add(new DelayedAction(Info.ActivationDelay - Info.BeaconRemoveAdvance, removeBeacon));
				});
			}
		}

		void ITick.Tick(Actor self)
		{
			if (--ticks < 0)
			{
				if (activeToken != Actor.InvalidConditionToken)
					activeToken = self.RevokeCondition(activeToken);
			}
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			Game.Sound.PlayToPlayer(SoundType.UI, manager.Self.Owner, Info.SelectTargetSound);
			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
				Info.SelectTargetSpeechNotification, self.Owner.Faction.InternalName);
			self.World.OrderGenerator = new SelectDetonateWeaponPowerTarget(order, manager, this);
		}

		float FractionComplete { get { return ticks * 1f / Info.ActivationDelay; } }
	}

	public class SelectDetonateWeaponPowerTarget : OrderGenerator
	{
		readonly SupportPowerManager manager;
		readonly string order;
		readonly DetonateWeaponPower power;

		public SelectDetonateWeaponPowerTarget(string order, SupportPowerManager manager, DetonateWeaponPower power)
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
			if (mi.Button == MouseButton.Left && world.Map.Contains(cell))
				yield return new Order(order, manager.Self, Target.FromCell(world, cell), false) { SuppressVisualFeedback = true };
		}

		protected override void Tick(World world)
		{
			// Cancel the OG if we can't use the power
			if (!manager.Powers.ContainsKey(order))
				world.CancelInputMode();
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }

		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
		{
			var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);

			if (power.Info.TargetCircleRange == WDist.Zero)
			{
				yield break;
			}
			else
			{
				yield return new RangeCircleAnnotationRenderable(
					world.Map.CenterOfCell(xy),
					power.Info.TargetCircleRange,
					0,
					power.Info.TargetCircleUsePlayerColor ? power.Self.Owner.Color : power.Info.TargetCircleColor, 1,
					Color.FromArgb(96, Color.Black), 3);
			}
		}

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return world.Map.Contains(cell) ? power.Info.Cursor : "generic-blocked";
		}
	}
}
