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

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	class InfiltrateToAttachActorInfo : TraitInfo
	{
		[ActorReference(typeof(AttachableInfo))]
		[FieldLoader.Require]
		[Desc("Actor to attach.")]
		public readonly string Actor = null;

		[Desc("The `TargetTypes` from `Targetable` that are allowed to enter.")]
		public readonly BitSet<TargetableType> Types = default(BitSet<TargetableType>);

		[Desc("List of sounds that can be played on successful infiltration.")]
		public readonly string InfiltratedSound = null;

		[NotificationReference("Speech")]
		[Desc("Sound the victim will hear after successful infiltration.")]
		public readonly string InfiltratedNotification = null;

		[NotificationReference("Speech")]
		[Desc("Sound the perpetrator will hear after successful infiltration.")]
		public readonly string InfiltrationNotification = null;

		public override object Create(ActorInitializer init) { return new InfiltrateToAttachActor(this); }
	}

	class InfiltrateToAttachActor : INotifyInfiltrated
	{
		readonly InfiltrateToAttachActorInfo info;

		public InfiltrateToAttachActor(InfiltrateToAttachActorInfo info)
		{
			this.info = info;
		}

		void INotifyInfiltrated.Infiltrated(Actor self, Actor infiltrator, BitSet<TargetableType> types)
		{
			if (!info.Types.Overlaps(types))
				return;

			var targetTrait = self.TraitsImplementing<AttachableTo>().FirstOrDefault();

			if (targetTrait == null)
				return;

			Attach(self, infiltrator, targetTrait);

			if (info.InfiltratedSound != null)
				Game.Sound.Play(SoundType.World, info.InfiltratedSound, self.CenterPosition);

			if (info.InfiltratedNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.InfiltratedNotification, self.Owner.Faction.InternalName);

			if (info.InfiltrationNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, infiltrator.Owner, "Speech", info.InfiltrationNotification, infiltrator.Owner.Faction.InternalName);
		}

		void Attach(Actor self, Actor infiltrator, AttachableTo targetTrait)
		{
			var map = self.World.Map;
			var targetCell = map.CellContaining(self.CenterPosition);

			self.World.AddFrameEndTask(w =>
			{
				var attachedActor = self.World.CreateActor(info.Actor.ToLowerInvariant(), new TypeDictionary
				{
					new LocationInit(targetCell),
					new OwnerInit(infiltrator.Owner),
				});

				targetTrait.Attach(attachedActor);
			});
		}
	}
}
