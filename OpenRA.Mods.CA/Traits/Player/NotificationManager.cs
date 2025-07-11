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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Tracks last notification times.")]
	public class NotificationManagerInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new NotificationManager(); }
	}

	public class NotificationManager
	{
		readonly Dictionary<string, long> lastNotificationTimes;

		public NotificationManager()
		{
			lastNotificationTimes = new Dictionary<string, long>();
		}

		public void SetLastNotificationTime(string notificationType, long time)
		{
			lastNotificationTimes[notificationType] = time;
		}

		public long GetLastNotificationTime(string notificationType)
		{
			return lastNotificationTimes.ContainsKey(notificationType) ? lastNotificationTimes[notificationType] : 0;
		}
	}
}
