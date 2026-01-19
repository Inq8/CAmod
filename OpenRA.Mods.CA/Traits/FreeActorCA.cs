#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Player receives a unit for free once the building is placed. This also works for structures.",
		"If you want more than one unit to appear copy this section and assign IDs like FreeActor@2, ...")]
	public class FreeActorCAInfo : ConditionalTraitInfo, IEditorActorOptions
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Name of the actor.")]
		public readonly string Actor = null;

		[Desc("Offset relative to the top-left cell of the building.")]
		public readonly CVec SpawnOffset = CVec.Zero;

		[Desc("Which direction the unit should face.")]
		public readonly WAngle Facing = WAngle.Zero;

		[Desc("Whether another actor should spawn upon re-enabling the trait.")]
		public readonly bool AllowRespawn = false;

		[Desc("Display order for the free actor checkbox in the map editor")]
		public readonly int EditorFreeActorDisplayOrder = 4;

		IEnumerable<EditorActorOption> IEditorActorOptions.ActorOptions(ActorInfo ai, World world)
		{
			yield return new EditorActorCheckbox("Spawn Child Actor", EditorFreeActorDisplayOrder,
				actor =>
				{
					var init = actor.GetInitOrDefault<FreeActorInit>(this);
					if (init != null)
						return init.Value;

					return true;
				},
				(actor, value) => actor.ReplaceInit(new FreeActorInit(this, value), this));
		}

		public override object Create(ActorInitializer init) { return new FreeActorCA(init, this); }
	}

	public class FreeActorCA : ConditionalTrait<FreeActorCAInfo>
	{
		protected bool allowSpawn;

		public FreeActorCA(ActorInitializer init, FreeActorCAInfo info)
			: base(info)
		{
			allowSpawn = init.GetValue<FreeActorInit, bool>(info, true);
		}

		protected override void TraitEnabled(Actor self)
		{
			if (!allowSpawn)
				return;

			allowSpawn = Info.AllowRespawn;

			self.World.AddFrameEndTask(w =>
			{
				var td = new TypeDictionary
				{
					new ParentActorInit(self),
					new OwnerInit(self.Owner),
				};

				var ai = w.Map.Rules.Actors[Info.Actor.ToLowerInvariant()];

				if (self.Info.HasTraitInfo<IOccupySpaceInfo>() && ai.HasTraitInfo<IOccupySpaceInfo>())
					td.Add(new LocationInit(self.Location + Info.SpawnOffset));

				if (self.Info.HasTraitInfo<IFacingInfo>() && ai.HasTraitInfo<IFacingInfo>())
					td.Add(new FacingInit(Info.Facing));

				w.CreateActor(Info.Actor, td);
			});
		}
	}
}
