#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Attach an actor when actor is transformed.")]
	public class AttachOnTransformInfo : ConditionalTraitInfo, Requires<AttachableToInfo>
	{
		[ActorReference(typeof(AttachableInfo))]
		[FieldLoader.Require]
		[Desc("Actor to attach.")]
		public readonly string Actor = null;

		public override object Create(ActorInitializer init) { return new AttachOnTransform(init, this); }
	}

	public class AttachOnTransform : ConditionalTrait<AttachOnTransformInfo>, INotifyTransform
	{
		public new readonly AttachOnTransformInfo Info;

		public AttachOnTransform(ActorInitializer init, AttachOnTransformInfo info)
			: base(info)
		{
			Info = info;
		}

		void INotifyTransform.OnTransform(Actor self) { }
		void INotifyTransform.BeforeTransform(Actor self) { }
		void INotifyTransform.AfterTransform(Actor toActor)
		{
			Attach(toActor);
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
