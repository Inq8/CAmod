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
using OpenRA.Mods.CA.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Falls to the ground then transforms into a different actor.")]
	public class FallsDownAndTransformsInfo : TraitInfo, Requires<IPositionableInfo>
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Velocity (per tick) the actor falls.")]
		public readonly string Actor = null;

		[Desc("Secondary actor to spawn on landing.")]
		public readonly string SpawnActor = null;

		[Desc("Velocity (per tick) the actor falls.")]
		public readonly WDist FallVelocity = new WDist(43);

		[Desc("Velocity (per tick) the actor moves forwards.")]
		public readonly WDist ForwardVelocity = new WDist(43);

		public override object Create(ActorInitializer init) { return new FallsDownAndTransforms(init, this); }
	}

	public class FallsDownAndTransforms : INotifyCreated, INotifyFallDown
	{
		readonly FallsDownAndTransformsInfo info;
		readonly IPositionable positionable;
		IFacing facing;

		public FallsDownAndTransforms(ActorInitializer init, FallsDownAndTransformsInfo info)
		{
			this.info = info;
			positionable = init.Self.Trait<IPositionable>();
			facing = init.Self.TraitOrDefault<IFacing>();
		}

		void INotifyCreated.Created(Actor self)
		{
			facing = self.TraitOrDefault<IFacing>();
			self.QueueActivity(false, new FallDown(self, positionable.CenterPosition, info.FallVelocity.Length, info.ForwardVelocity.Length));
		}

		void INotifyFallDown.OnLanded(Actor self)
		{
			Transform(self);

			if (info.SpawnActor != null)
			{
				var td = new TypeDictionary
				{
					new ParentActorInit(self),
					new LocationInit(self.Location),
					new CenterPositionInit(self.CenterPosition),
					new OwnerInit(self.Owner)
				};

				if (facing != null)
					td.Add(new FacingInit(facing.Facing));

				self.World.AddFrameEndTask(w => w.CreateActor(info.SpawnActor, td));
			}
		}

		void Transform(Actor self)
		{
			var transform = new InstantTransform(self, info.Actor);
			transform.SkipMakeAnims = true;
			self.CurrentActivity.QueueChild(transform);
		}
	}
}
