#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Attach to support unit with a dummy weapon so that it cancels attacks when in range (causing it to fall back to using its support weapon, e.g. heal/repair).")]
	class SupportUnitWithDummyWeaponInfo : ConditionalTraitInfo
	{
		[Desc("Attacks against actors with these relationships will be cancelled immediately when in range.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		public override object Create(ActorInitializer init) { return new SupportUnitWithDummyWeapon(init, this); }
	}

	class SupportUnitWithDummyWeapon : ConditionalTrait<SupportUnitWithDummyWeaponInfo>, INotifyAttack
	{
		public SupportUnitWithDummyWeapon(ActorInitializer init, SupportUnitWithDummyWeaponInfo info)
			: base(info) { }

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			CancelAttack(self, target);
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel)
		{
			CancelAttack(self, target);
		}

		void CancelAttack(Actor self, in Target target)
		{
			if (IsTraitDisabled)
				return;

			if (target.Type != TargetType.Actor)
				return;

			if (!Info.ValidRelationships.HasStance(self.Owner.RelationshipWith(target.Actor.Owner)))
				return;

			self.CancelActivity();
		}
	}
}
