#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	class ChargingSelfDestructInfo : TraitInfo, IRulesetLoaded, Requires<ExplodesInfo>, Requires<WithFacingSpriteBodyInfo>
	{
		[SequenceReference]
		public readonly string ChargingSequence = "charging";

		public readonly int ChargingInterval = 8;

		[WeaponReference]
		public readonly string ChargingDamageWeapon = null;

		[Desc("Measured in ticks.")]
		public readonly int ChargeDelay = 96;

		public readonly string ChargeSound = null;

		[Desc("Measured in ticks.")]
		public readonly int DetonationDelay = 42;

		public readonly string DetonationSound = null;

		[WeaponReference]
		public readonly string DetonationWeapon = null;

		[VoiceReference]
		public readonly string Voice = "Action";

		[GrantedConditionReference]
		[Desc("The condition to grant to self while deployed.")]
		public readonly string DeployedCondition = null;

		public WeaponInfo ThumpDamageWeaponInfo { get; private set; }

		public WeaponInfo DetonationWeaponInfo { get; private set; }

		[Desc("Types of damage that this trait causes to self while self-destructing. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		public override object Create(ActorInitializer init) { return new ChargingSelfDestruct(init.Self, this); }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var thumpDamageWeaponToLower = (ChargingDamageWeapon ?? string.Empty).ToLowerInvariant();
			var detonationWeaponToLower = (DetonationWeapon ?? string.Empty).ToLowerInvariant();

			if (!rules.Weapons.TryGetValue(thumpDamageWeaponToLower, out var thumpDamageWeapon))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(thumpDamageWeaponToLower));

			if (!rules.Weapons.TryGetValue(detonationWeaponToLower, out var detonationWeapon))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(detonationWeaponToLower));

			ThumpDamageWeaponInfo = thumpDamageWeapon;
			DetonationWeaponInfo = detonationWeapon;
		}
	}

	class ChargingSelfDestruct : IIssueOrder, IResolveOrder, IOrderVoice, IIssueDeployOrder
	{
		readonly ChargingSelfDestructInfo info;

		public ChargingSelfDestruct(Actor self, ChargingSelfDestructInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new TargetTypeOrderTargeter(new BitSet<TargetableType>("DetonateAttack"), "DetonateAttack", 5, "attack", true, false) { ForceAttack = false };
				yield return new DeployOrderTargeter("Detonate", 5);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID != "DetonateAttack" && order.OrderID != "Detonate")
				return null;

			return new Order(order.OrderID, self, target, queued);
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			return new Order("Detonate", self, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return true; }

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "DetonateAttack" && order.OrderString != "Detonate")
				return null;

			return info.Voice;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "DetonateAttack")
			{
				self.QueueActivity(order.Queued, new DetonationSequence(self, this, order.Target));
				self.ShowTargetLines();
			}
			else if (order.OrderString == "Detonate")
				self.QueueActivity(order.Queued, new DetonationSequence(self, this));
		}

		class DetonationSequence : Activity
		{
			readonly Actor self;
			readonly ChargingSelfDestruct csd;
			readonly IMove move;
			readonly WithFacingSpriteBody wfsb;
			readonly ScreenShaker screenShaker;
			readonly bool assignTargetOnFirstRun;

			int ticks;
			bool initiated;
			Target target;

			public DetonationSequence(Actor self, ChargingSelfDestruct mad)
				: this(self, mad, Target.Invalid)
			{
				assignTargetOnFirstRun = true;
			}

			public DetonationSequence(Actor self, ChargingSelfDestruct csd, in Target target)
			{
				this.self = self;
				this.csd = csd;
				this.target = target;

				move = self.Trait<IMove>();
				wfsb = self.Trait<WithFacingSpriteBody>();
				screenShaker = self.World.WorldActor.Trait<ScreenShaker>();
			}

			protected override void OnFirstRun(Actor self)
			{
				if (assignTargetOnFirstRun)
					target = Target.FromCell(self.World, self.Location);
			}

			public override bool Tick(Actor self)
			{
				if (IsCanceling)
					return true;

				if (target.Type != TargetType.Invalid && !move.CanEnterTargetNow(self, target))
				{
					QueueChild(new MoveAdjacentTo(self, target, targetLineColor: Color.Red));
					return false;
				}

				if (!initiated)
				{
					// If the target has died while we were moving, we should abort detonation.
					if (target.Type == TargetType.Invalid)
						return true;

					self.GrantCondition(csd.info.DeployedCondition);

					if (csd.info.ChargingSequence != null)
						wfsb.PlayCustomAnimationRepeating(self, csd.info.ChargingSequence);

					IsInterruptible = false;
					initiated = true;
				}

				if (++ticks % csd.info.ChargingInterval == 0)
				{
					if (csd.info.ChargingDamageWeapon != null)
					{
						// Use .FromPos since this weapon needs to affect more than just the actor
						csd.info.ThumpDamageWeaponInfo.Impact(Target.FromPos(self.CenterPosition), self);
					}
				}

				if (ticks == csd.info.ChargeDelay)
					Game.Sound.Play(SoundType.World, csd.info.ChargeSound, self.CenterPosition);

				return ticks == csd.info.ChargeDelay + csd.info.DetonationDelay;
			}

			protected override void OnLastRun(Actor self)
			{
				if (!initiated)
					return;

				Game.Sound.Play(SoundType.World, csd.info.DetonationSound, self.CenterPosition);

				self.World.AddFrameEndTask(w =>
				{
					if (csd.info.DetonationWeapon != null)
					{
						// Use .FromPos since this actor is killed. Cannot use Target.FromActor
						csd.info.DetonationWeaponInfo.Impact(Target.FromPos(self.CenterPosition), self);
					}

					self.Kill(self, csd.info.DamageTypes);
				});
			}

			public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
			{
				yield return new TargetLineNode(target, Color.Crimson);
			}
		}
	}
}
