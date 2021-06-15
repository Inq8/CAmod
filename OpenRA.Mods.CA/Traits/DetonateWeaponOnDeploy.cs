#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Detonate weapon on deploy command.")]
	public class DetonateWeaponOnDeployInfo : PausableConditionalTraitInfo
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Weapon to use for explosion.")]
		public readonly string Weapon = null;

		[Desc("Detonate the weapon on the ground.")]
		public readonly bool ForceGround = false;

		[Desc("Cooldown in ticks until the unit can deploy.")]
		public readonly int CooldownTicks;

		[Desc("The deployed state's length in ticks.")]
		public readonly int DeployedTicks;

		[Desc("Cursor to display when able to (un)deploy the actor.")]
		public readonly string DeployCursor = "deploy";

		[Desc("Cursor to display when unable to (un)deploy the actor.")]
		public readonly string DeployBlockedCursor = "deploy-blocked";

		[SequenceReference]
		[Desc("Sequence name to use")]
		public readonly string OverlaySequence = null;

		[Desc("Position relative to body")]
		public readonly WVec OverlayOffset = WVec.Zero;

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name")]
		public readonly string OverlayPalette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		[Desc("Sound to play when deploying.")]
		public readonly string DeploySound = null;

		public readonly bool StartsFullyCharged = false;

		[VoiceReference]
		public readonly string Voice = "Action";

		public readonly bool ShowSelectionBar = true;
		public readonly Color ChargingColor = Color.DarkRed;
		public readonly Color DischargingColor = Color.DarkMagenta;

		public WeaponInfo WeaponInfo { get; private set; }

		public override object Create(ActorInitializer init) { return new DetonateWeaponOnDeploy(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (!string.IsNullOrEmpty(Weapon))
			{
				var weaponToLower = Weapon.ToLowerInvariant();
				if (!rules.Weapons.TryGetValue(weaponToLower, out var weapon))
					throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));
				WeaponInfo = weapon;
			}
		}
	}

	public enum DetonateWeaponOnDeployState { Charging, Ready, Active, Deploying, Undeploying }

	public class DetonateWeaponOnDeploy : PausableConditionalTrait<DetonateWeaponOnDeployInfo>,
		IResolveOrder, IIssueOrder, ISelectionBar, IOrderVoice, ISync, ITick, IIssueDeployOrder
	{
		readonly Actor self;

		[Sync]
		int ticks;

		DetonateWeaponOnDeployState deployState;

		public DetonateWeaponOnDeploy(Actor self, DetonateWeaponOnDeployInfo info)
			: base(info)
		{
			this.self = self;
		}

		protected override void Created(Actor self)
		{
			if (Info.StartsFullyCharged)
			{
				ticks = Info.DeployedTicks;
				deployState = DetonateWeaponOnDeployState.Ready;
			}
			else
			{
				ticks = Info.CooldownTicks;
				deployState = DetonateWeaponOnDeployState.Charging;
			}

			base.Created(self);
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			return new Order("DetonateWeaponOnDeploy", self, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return !IsTraitPaused && !IsTraitDisabled; }

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (!IsTraitDisabled)
					yield return new DeployOrderTargeter("DetonateWeaponOnDeploy", 5,
						() => IsCursorBlocked() ? Info.DeployBlockedCursor : Info.DeployCursor);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "DetonateWeaponOnDeploy")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "DetonateWeaponOnDeploy" || deployState != DetonateWeaponOnDeployState.Ready)
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.QueueActivity(new CallFunc(Deploy));
		}

		bool IsCursorBlocked()
		{
			return deployState != DetonateWeaponOnDeployState.Ready && !IsTraitPaused;
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "DetonateWeaponOnDeploy" && deployState == DetonateWeaponOnDeployState.Ready ? Info.Voice : null;
		}

		void Deploy()
		{
			// Something went wrong, most likely due to deploy order spam and the fact that this is a delayed action.
			if (deployState != DetonateWeaponOnDeployState.Ready)
				return;

			deployState = DetonateWeaponOnDeployState.Deploying;

			if (!string.IsNullOrEmpty(Info.DeploySound))
				Game.Sound.Play(SoundType.World, Info.DeploySound, self.CenterPosition);

			OnDeployCompleted();
		}

		void OnDeployCompleted()
		{
			var target = Target.FromPos(self.CenterPosition);

			if (Info.ForceGround)
				target = Target.FromCell(self.World, self.Location);

			var weapon = Info.WeaponInfo;
			weapon.Impact(target, self);

			deployState = DetonateWeaponOnDeployState.Active;

			PlayOverlayAnimation();
		}

		void RevokeDeploy()
		{
			deployState = DetonateWeaponOnDeployState.Undeploying;
			OnUndeployCompleted();
		}

		void OnUndeployCompleted()
		{
			deployState = DetonateWeaponOnDeployState.Charging;
			ticks = Info.CooldownTicks;
		}

		void PlayOverlayAnimation()
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			var image = rs.GetImage(self);
			var overlay = new Animation(self.World, image, () => IsTraitPaused);
			overlay.IsDecoration = false;

			var anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(Info.OverlayOffset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				() => IsTraitDisabled,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1));

			overlay.PlayThen(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), Info.OverlaySequence),
				() => self.World.AddFrameEndTask(w => rs.Remove(anim)));

			rs.Add(anim, Info.OverlayPalette, Info.IsPlayerPalette);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitPaused || IsTraitDisabled)
				return;

			if (deployState == DetonateWeaponOnDeployState.Ready || deployState == DetonateWeaponOnDeployState.Deploying || deployState == DetonateWeaponOnDeployState.Undeploying)
				return;

			if (--ticks < 0)
			{
				if (deployState == DetonateWeaponOnDeployState.Charging)
				{
					ticks = Info.DeployedTicks;
					deployState = DetonateWeaponOnDeployState.Ready;
				}
				else
					RevokeDeploy();
			}
		}

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled || !Info.ShowSelectionBar || deployState == DetonateWeaponOnDeployState.Undeploying)
				return 0f;

			if (deployState == DetonateWeaponOnDeployState.Deploying || deployState == DetonateWeaponOnDeployState.Ready)
				return 1f;

			return deployState == DetonateWeaponOnDeployState.Charging
				? (float)(Info.CooldownTicks - ticks) / Info.CooldownTicks
				: (float)ticks / Info.DeployedTicks;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return !IsTraitDisabled && Info.ShowSelectionBar; } }

		Color ISelectionBar.GetColor() { return deployState == DetonateWeaponOnDeployState.Charging ? Info.ChargingColor : Info.DischargingColor; }
	}
}
