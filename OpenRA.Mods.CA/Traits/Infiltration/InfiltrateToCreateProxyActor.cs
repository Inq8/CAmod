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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Replaces InfiltrateForSupportPower. Allows the spawned proxy actor to inherit the faction of the infiltrated actor,",
		"or to be owned by the target.")]
	class InfiltrateToCreateProxyActorInfo : TraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		public readonly string Proxy = null;

		[Desc("The `TargetTypes` from `Targetable` that are allowed to enter.")]
		public readonly BitSet<TargetableType> Types = default;

		[Desc("Experience to grant to the infiltrating player.")]
		public readonly int PlayerExperience = 0;

		[NotificationReference("Speech")]
		[Desc("Sound the victim will hear when technology gets stolen.")]
		public readonly string InfiltratedNotification = null;

		[NotificationReference("Speech")]
		[Desc("Sound the perpetrator will hear after successful infiltration.")]
		public readonly string InfiltrationNotification = null;

		[Desc("If true, the spawned actor will use the target's faction.")]
		public readonly bool UseTargetFaction = false;

		[Desc("If true, the spawned actor will be owned by the target.")]
		public readonly bool UseTargetOwner = false;

		[Desc("If true, spawn at the location of the infiltrated actor.")]
		public readonly bool UseLocation = false;

		[Desc("If true, spawn at the location of the infiltrated actor.")]
		public readonly bool UseCenterPosition = false;

		[Desc("If true, the spawned actor is destroyed if the parent actor dies, is sold, or is captured.")]
		public readonly bool LinkedToParent = false;

		public override object Create(ActorInitializer init) { return new InfiltrateToCreateProxyActor(this); }
	}

	class InfiltrateToCreateProxyActor : INotifyInfiltrated, INotifyRemovedFromWorld
	{
		readonly InfiltrateToCreateProxyActorInfo info;
		List<Actor> spawnedActors;

		public InfiltrateToCreateProxyActor(InfiltrateToCreateProxyActorInfo info)
		{
			this.info = info;
			spawnedActors = new List<Actor>();
		}

		void INotifyInfiltrated.Infiltrated(Actor self, Actor infiltrator, BitSet<TargetableType> types)
		{
			if (!info.Types.Overlaps(types))
				return;

			if (info.InfiltratedNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.InfiltratedNotification, self.Owner.Faction.InternalName);

			if (info.InfiltrationNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, infiltrator.Owner, "Speech", info.InfiltrationNotification, infiltrator.Owner.Faction.InternalName);

			var td = new TypeDictionary();

			if (info.UseTargetFaction)
				td.Add(new FactionInit(self.Owner.Faction.InternalName));

			if (info.UseTargetOwner)
				td.Add(new OwnerInit(self.Owner));
			else
				td.Add(new OwnerInit(infiltrator.Owner));

			if (info.UseCenterPosition)
			{
				td.Add(new CenterPositionInit(self.CenterPosition));

				if (info.UseLocation)
					td.Add(new LocationInit(self.World.Map.CellContaining(self.CenterPosition)));
			}
			else if (info.UseLocation)
				td.Add(new LocationInit(self.Location));

			infiltrator.Owner.PlayerActor.TraitOrDefault<PlayerExperience>()?.GiveExperience(info.PlayerExperience);

			infiltrator.World.AddFrameEndTask(w => spawnedActors.Add(w.CreateActor(info.Proxy, td)));
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (!info.LinkedToParent)
				return;

			foreach (var a in spawnedActors)
			{
				if (!a.IsDead)
					a.Dispose();
			}
		}
	}
}
