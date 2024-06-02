#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("When aircraft attacks, dive into the target.")]
	public class DiveOnAttackInfo : AttackAircraftInfo, Requires<AircraftInfo>
	{
		[Desc("Dive speed (in WDist units/tick).")]
		public readonly WDist Speed = new(426);

		[Desc("Condition to grant on diving.")]
		[GrantedConditionReference]
		public readonly string DiveCondition = null;

		[NotificationReference("Speech")]
		[Desc("Speech notification to play if shot down by enemy while diving.")]
		public readonly string Notification = "UnitLost";

		public override object Create(ActorInitializer init) { return new DiveOnAttack(init.Self, this); }
	}

	public class DiveOnAttack : INotifyAttack, INotifyKilled
	{
		readonly DiveOnAttackInfo Info;
		readonly Aircraft aircraft;
		bool isDiving;

		public DiveOnAttack(Actor self, DiveOnAttackInfo info)
		{
			Info = info;
			aircraft = self.Trait<Aircraft>();
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			self.QueueActivity(false, new Dive(target, aircraft, Info.Speed.Length));

			if (Info.DiveCondition != null)
				self.GrantCondition(Info.DiveCondition);

			isDiving = true;
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) {}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (isDiving && Info.Notification != null && e.Attacker.Owner != self.Owner)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.Notification, self.Owner.Faction.InternalName);
		}
	}
}
