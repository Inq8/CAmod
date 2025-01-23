#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Plays an audio notification and shows a radar ping when actor is damaged.")]
	public class NotificationOnDamageInfo : TraitInfo
	{
		[Desc("The type of notification (interval shared by all notifications of the same type).")]
		[FieldLoader.Require]
		public readonly string Type = null;

		[Desc("Minimum duration (in milliseconds) between notification events.")]
		public readonly int NotifyInterval = 30000;

		public readonly Color RadarPingColor = Color.Red;

		[Desc("Length of time (in ticks) to display a location ping in the minimap.")]
		public readonly int RadarPingDuration = 250;

		[Desc("Minimum amount of damage required to trigger notification.")]
		public readonly int MinimumDamage = 0;

		[NotificationReference("Speech")]
		[Desc("Speech notification type to play.")]
		public readonly string Notification = null;

		[Desc("Text notification to display.")]
		public string TextNotification = null;

		public override object Create(ActorInitializer init) { return new NotificationOnDamage(init.Self, this); }
	}

	public class NotificationOnDamage : INotifyDamage, INotifyOwnerChanged, INotifyCreated, INotifyDamageStateChanged
	{
		readonly RadarPings radarPings;
		readonly NotificationOnDamageInfo info;
		NotificationManager notificationManager;

		public NotificationOnDamage(Actor self, NotificationOnDamageInfo info)
		{
			radarPings = self.World.WorldActor.TraitOrDefault<RadarPings>();
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			notificationManager = self.Owner.PlayerActor.Trait<NotificationManager>();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			notificationManager = newOwner.PlayerActor.Trait<NotificationManager>();
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			if (e.PreviousDamageState < e.DamageState && e.DamageState != DamageState.Dead && e.PreviousDamageState != DamageState.Undamaged)
				Notify(self, e);
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage.Value < info.MinimumDamage)
				return;

			Notify(self, e);
		}

		private void Notify(Actor self, AttackInfo e)
		{
			// Don't track self-damage
			if (e.Attacker != null && e.Attacker.Owner == self.Owner)
				return;

			if (Game.RunTime > notificationManager.GetLastNotificationTime(info.Type) + info.NotifyInterval)
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.Notification, self.Owner.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(info.TextNotification, self.Owner);

				radarPings?.Add(() => self.Owner.IsAlliedWith(self.World.RenderPlayer), self.CenterPosition, info.RadarPingColor, info.RadarPingDuration);

				notificationManager.SetLastNotificationTime(info.Type, Game.RunTime);
			}
		}
	}
}
