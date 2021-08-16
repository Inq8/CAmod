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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	class InfiltrateToAttachInfo : TraitInfo, Requires<AttachableToInfo>
	{
		[ActorReference(typeof(AttachableInfo))]
		[Desc("Actor to attach.")]
		public readonly string Actor = null;

		[Desc("The `TargetTypes` from `Infiltrates.Types` that are allowed to enter.")]
		public readonly BitSet<TargetableType> Types = default(BitSet<TargetableType>);

		[Desc("List of sounds that can be played on successful infiltration.")]
		public readonly string InfiltratedSound = null;

		[NotificationReference("Speech")]
		[Desc("Sound the victim will hear after successful infiltration.")]
		public readonly string InfiltratedNotification = null;

		[NotificationReference("Speech")]
		[Desc("Sound the perpetrator will hear after successful infiltration.")]
		public readonly string InfiltrationNotification = null;

		public override object Create(ActorInitializer init) { return new InfiltrateToAttach(this); }
	}

	class InfiltrateToAttach : INotifyInfiltrated
	{
		public readonly InfiltrateToAttachInfo Info;

		public InfiltrateToAttach(InfiltrateToAttachInfo info)
		{
			Info = info;
		}

		void INotifyInfiltrated.Infiltrated(Actor self, Actor infiltrator, BitSet<TargetableType> types)
		{
			if (!Info.Types.Overlaps(types))
				return;

			var attachableToTrait = self.TraitsImplementing<AttachableTo>().FirstOrDefault();

			if (attachableToTrait == null)
				return;

			Attach(self, infiltrator, attachableToTrait);

			if (Info.InfiltratedSound != null)
				Game.Sound.Play(SoundType.World, Info.InfiltratedSound, self.CenterPosition);

			if (Info.InfiltratedNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.InfiltratedNotification, self.Owner.Faction.InternalName);

			if (Info.InfiltrationNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, infiltrator.Owner, "Speech", Info.InfiltrationNotification, infiltrator.Owner.Faction.InternalName);
		}

		void Attach(Actor self, Actor infiltrator, AttachableTo attachableToTrait)
		{
			var map = self.World.Map;
			var targetCell = map.CellContaining(self.CenterPosition);

			self.World.AddFrameEndTask(w =>
			{
				var actorToAttach = infiltrator;

				if (Info.Actor != null)
				{
					var initialFacing = WAngle.Zero;
					var mobile = actorToAttach.TraitOrDefault<Mobile>();
					if (mobile != null)
						initialFacing = mobile.Facing;

					actorToAttach = self.World.CreateActor(Info.Actor.ToLowerInvariant(), new TypeDictionary
					{
						new LocationInit(targetCell),
						new OwnerInit(infiltrator.Owner),
						new FacingInit(initialFacing)
					});
				}

				var attachable = actorToAttach.TraitOrDefault<Attachable>();
				if (attachable == null)
					return;

				var attached = attachableToTrait.Attach(attachable);

				if (!attached && actorToAttach != infiltrator)
					actorToAttach.Dispose();
			});
		}
	}
}
