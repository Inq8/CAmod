#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("This actor can destroy weaponry.")]
	public class PointDefenseInfo : ConditionalTraitInfo, Requires<ArmamentInfo>
	{
		[FieldLoader.Require]
		[Desc("Weapon used to shoot the projectile. Caution: make sure that this is an insta-hit weapon, otherwise will look very odd!")]
		public readonly string Armament;

		[FieldLoader.Require]
		[Desc("What kind of projectiles can this actor shoot at.")]
		public readonly BitSet<string> PointDefenseTypes = default;

		[Desc("What diplomatic stances are affected.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		public override object Create(ActorInitializer init) { return new PointDefense(init.Self, this); }
	}

	public class PointDefense : ConditionalTrait<PointDefenseInfo>, IPointDefense, ITick
	{
		readonly Actor self;
		readonly PointDefenseInfo info;
		readonly Armament armament;
		INotifyPointDefenseHit[] notifyHit;
		IDamageModifier[] damageModifiers;

		bool hasFiredThisTick = false;

		public PointDefense(Actor self, PointDefenseInfo info)
			: base(info)
		{
			this.self = self;
			this.info = info;
			armament = self.TraitsImplementing<Armament>().First(a => a.Info.Name == info.Armament);
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			notifyHit = self.TraitsImplementing<INotifyPointDefenseHit>().ToArray();
			damageModifiers = self.TraitsImplementing<IDamageModifier>().ToArray();
		}

		void ITick.Tick(Actor self)
		{
			hasFiredThisTick = false;
		}

		bool IPointDefense.Destroy(WPos position, Player attacker, string type, ProjectileArgs args)
		{
			if (IsTraitDisabled || armament.IsTraitDisabled || armament.IsTraitPaused || hasFiredThisTick)
				return false;

			if (!info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(attacker)))
				return false;

			if (!info.PointDefenseTypes.Contains(type))
				return false;

			if (armament.IsReloading)
				return false;

			if (!info.PointDefenseTypes.Contains(type))
				return false;

			if ((self.CenterPosition - position).HorizontalLengthSquared > armament.MaxRange().LengthSquared)
				return false;

			hasFiredThisTick = true;
			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead)
					return;

				var armor = self.TraitsImplementing<Armor>().Where(a => !a.IsTraitDisabled && a.Info.Type != null);
				var damage = 0;
				foreach (var wh in args.Weapon.Warheads)
				{
					if (wh is DamageWarhead)
					{
						var warhead = (DamageWarhead)wh;
						var armorModifiers = armor.Where(a => warhead.Versus.ContainsKey(a.Info.Type))
							.Select(a => warhead.Versus[a.Info.Type]);

						// todo: exclude point defense shield modifier in a better way
						var otherModifiers = damageModifiers.Where(dm => !(dm is TimedDamageMultiplier))
							.Select(d => d.GetDamageModifier(args.SourceActor, new Damage(warhead.Damage, warhead.DamageTypes)))
							.Where(d => d != 100);

						var modifiers = armorModifiers.Concat(otherModifiers).Concat(args.DamageModifiers).ToArray();

						damage += Util.ApplyPercentageModifiers(warhead.Damage, modifiers);
					}
				}

				if (armament.CheckFire(self, null, Target.FromPos(position)))
					foreach (var notify in notifyHit)
						notify.Hit(damage);
			});

			return true;
		}
	}
}
