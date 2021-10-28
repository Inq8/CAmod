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

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Attach an actor when actor is created.")]
	public class AttachOnCreationInfo : ConditionalTraitInfo, Requires<AttachableToInfo>
	{
		[ActorReference(typeof(AttachableInfo))]
		[FieldLoader.Require]
		[Desc("Actor to attach.")]
		public readonly string Actor = null;

		public override object Create(ActorInitializer init) { return new AttachOnCreation(init, this); }
	}

	public class AttachOnCreation : ConditionalTrait<AttachOnCreationInfo>, INotifyCreated
	{
		public new readonly AttachOnCreationInfo Info;

		public AttachOnCreation(ActorInitializer init, AttachOnCreationInfo info)
			: base(info)
		{
			Info = info;
		}

		protected override void Created(Actor self)
		{
			Attach(self);
		}

		void Attach(Actor self)
		{
			var map = self.World.Map;
			var targetCell = map.CellContaining(self.CenterPosition);

			var facing = self.TraitOrDefault<IFacing>();
			var attachedFacing = WAngle.Zero;
			if (facing != null)
				attachedFacing = facing.Facing;

			self.World.AddFrameEndTask(w =>
			{
				if (IsTraitDisabled)
					return;

				var actorToAttach = self.World.CreateActor(Info.Actor.ToLowerInvariant(), new TypeDictionary
				{
					new LocationInit(targetCell),
					new OwnerInit(self.Owner),
					new FacingInit(attachedFacing)
				});

				var attachable = actorToAttach.TraitOrDefault<Attachable>();
				if (attachable == null)
					return;

				var attachableToTrait = self.Trait<AttachableTo>();
				var attached = attachableToTrait.Attach(attachable);

				if (!attached)
					actorToAttach.Dispose();
			});
		}
	}
}
