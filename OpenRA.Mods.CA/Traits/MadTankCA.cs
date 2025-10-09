#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	class MadTankCAInfo : PausableConditionalTraitInfo, IRulesetLoaded, Requires<FireWarheadsOnDeathInfo>, Requires<WithFacingSpriteBodyInfo>
	{
		[SequenceReference]
		public readonly string ThumpSequence = null;

		public readonly int ThumpInterval = 100;

		[WeaponReference]
		public readonly string ThumpDamageWeapon = null;

		[Desc("Measured in ticks.")]
		public readonly int ChargeDelay = 25;

		public readonly string ChargeSound = "madchrg2.aud";

		[ActorReference]
		public readonly string DriverActor = null;

		[VoiceReference]
		public readonly string Voice = "Action";

		[GrantedConditionReference]
		[Desc("The condition to grant to self while deployed.")]
		public readonly string DeployedCondition = null;

		public WeaponInfo ThumpDamageWeaponInfo { get; private set; }

		[CursorReference]
		[Desc("Cursor to display when able to set up the detonation sequence.")]
		public readonly string DeployCursor = "deploy";

		public override object Create(ActorInitializer init) { return new MadTankCA(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (ThumpDamageWeapon != null)
			{
				var thumpDamageWeaponToLower = ThumpDamageWeapon.ToLowerInvariant();
				if (!rules.Weapons.TryGetValue(thumpDamageWeaponToLower, out var thumpDamageWeapon))
					throw new YamlException($"Weapons Ruleset does not contain an entry '{thumpDamageWeaponToLower}'");

				ThumpDamageWeaponInfo = thumpDamageWeapon;
			}
		}
	}

	class MadTankCA : PausableConditionalTrait<MadTankCAInfo>, IIssueOrder, IResolveOrder, IOrderVoice, IIssueDeployOrder
	{
		readonly MadTankCAInfo info;
		public bool FirstDetonationComplete { get; set; }
		bool initiated;

		IReloadModifier[] reloadModifiers;

		public MadTankCA(Actor self, MadTankCAInfo info)
			: base(info)
		{
			this.info = info;
			FirstDetonationComplete = false;
		}

		protected override void Created(Actor self)
		{
			reloadModifiers = self.TraitsImplementing<IReloadModifier>().ToArray();
			base.Created(self);
		}

		int GetModifiedThumpInterval()
		{
			return Util.ApplyPercentageModifiers(info.ThumpInterval, reloadModifiers.Select(m => m.GetReloadModifier()));
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return
					new TargetTypeOrderTargeter(new BitSet<TargetableType>("DetonateAttack"), "DetonateAttack", 5, "attack", true, false)
					{ ForceAttack = false };

				if (!initiated)
					yield return new DeployOrderTargeter("Detonate", 5, () => Info.DeployCursor);
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

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return !(self.CurrentActivity is DetonationSequence); }

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

		public class DetonationSequence : Activity
		{
			readonly Actor self;
			readonly MadTankCA mad;
			readonly IMove move;
			readonly WithFacingSpriteBody wfsb;
			readonly bool assignTargetOnFirstRun;

			int ticks;
			Target target;
			int thumpInterval;

			public DetonationSequence(Actor self, MadTankCA mad)
				: this(self, mad, Target.Invalid)
			{
				assignTargetOnFirstRun = true;
			}

			public DetonationSequence(Actor self, MadTankCA mad, in Target target)
			{
				this.self = self;
				this.mad = mad;
				this.target = target;

				move = self.Trait<IMove>();
				wfsb = self.Trait<WithFacingSpriteBody>();

				// Cache interval with modifiers applied
				thumpInterval = mad.GetModifiedThumpInterval();
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

				if (mad.IsTraitPaused)
					return false;

				if (target.Type != TargetType.Invalid && !move.CanEnterTargetNow(self, target))
				{
					QueueChild(new MoveAdjacentTo(self, target, targetLineColor: Color.Red));
					return false;
				}

				if (!mad.initiated)
				{
					// If the target has died while we were moving, we should abort detonation.
					if (target.Type == TargetType.Invalid)
						return true;

					self.GrantCondition(mad.info.DeployedCondition);
					self.World.AddFrameEndTask(w => EjectDriver());
					IsInterruptible = false;
					mad.initiated = true;
				}

				if (ticks == 1 && mad.info.ChargeSound != null)
					Game.Sound.Play(SoundType.World, mad.info.ChargeSound, self.CenterPosition);

				if (++ticks == mad.info.ChargeDelay)
				{
					if (mad.info.ThumpDamageWeapon != null)
					{
						var args = new WarheadArgs
						{
							Weapon = mad.info.ThumpDamageWeaponInfo,
							SourceActor = self,
							WeaponTarget = target,
							DamageModifiers = self.TraitsImplementing<IFirepowerModifier>()
								.Select(a => a.GetFirepowerModifier()).ToArray()
						};

						// Use .FromPos since this weapon needs to affect more than just the MadTank actor
						mad.info.ThumpDamageWeaponInfo.Impact(Target.FromPos(self.CenterPosition), args);
					}

					if (mad.info.ThumpSequence != null)
						wfsb.PlayCustomAnimation(self, mad.info.ThumpSequence);
				}

				if (ticks == thumpInterval + mad.info.ChargeDelay)
				{
					thumpInterval = mad.GetModifiedThumpInterval();
					ticks = 0;
				}

				return false;
			}

			public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
			{
				yield return new TargetLineNode(target, Color.Crimson);
			}

			void EjectDriver()
			{
				if (mad.info.DriverActor == null)
					return;

				var driver = self.World.CreateActor(mad.info.DriverActor.ToLowerInvariant(), new TypeDictionary
				{
					new LocationInit(self.Location),
					new OwnerInit(self.Owner)
				});
				driver.QueueActivity(false, new Nudge(driver));
			}
		}
	}
}
