#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("CA version makes the spawned proxy actor inherit the faction of the infiltrated actor.")]
	class InfiltrateForSupportPowerCAInfo : TraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		public readonly string Proxy = null;

		[Desc("The `TargetTypes` from `Targetable` that are allowed to enter.")]
		public readonly BitSet<TargetableType> Types = default(BitSet<TargetableType>);

		[NotificationReference("Speech")]
		[Desc("Sound the victim will hear when technology gets stolen.")]
		public readonly string InfiltratedNotification = null;

		[NotificationReference("Speech")]
		[Desc("Sound the perpetrator will hear after successful infiltration.")]
		public readonly string InfiltrationNotification = null;

		public override object Create(ActorInitializer init) { return new InfiltrateForSupportPowerCA(this); }
	}

	class InfiltrateForSupportPowerCA : INotifyInfiltrated
	{
		readonly InfiltrateForSupportPowerCAInfo info;

		public InfiltrateForSupportPowerCA(InfiltrateForSupportPowerCAInfo info)
		{
			this.info = info;
		}

		void INotifyInfiltrated.Infiltrated(Actor self, Actor infiltrator, BitSet<TargetableType> types)
		{
			if (!info.Types.Overlaps(types))
				return;

			if (info.InfiltratedNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.InfiltratedNotification, self.Owner.Faction.InternalName);

			if (info.InfiltrationNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, infiltrator.Owner, "Speech", info.InfiltrationNotification, infiltrator.Owner.Faction.InternalName);

			infiltrator.World.AddFrameEndTask(w => w.CreateActor(info.Proxy, new TypeDictionary
			{
				new OwnerInit(infiltrator.Owner),
				new FactionInit(self.Owner.Faction.InternalName)
			}));
		}
	}
}
