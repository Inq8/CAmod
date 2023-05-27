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
	[Desc("If actor spawns an actor on death, transfer auto target stance to it.")]
	public class TransfersStanceToDeathActorInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new TransfersStanceToDeathActor(this, init.Self); }
	}

	public class TransfersStanceToDeathActor : IDeathActorInitModifier
	{
		TransfersStanceToDeathActorInfo info;

		public TransfersStanceToDeathActor(TransfersStanceToDeathActorInfo info, Actor self)
		{
			this.info = info;
		}

		void IDeathActorInitModifier.ModifyDeathActorInit(Actor self, TypeDictionary init)
		{
			var autoTarget = self.TraitOrDefault<AutoTarget>();
			if (autoTarget != null)
			{
				var stance = autoTarget.Stance;
				init.Add(new StanceInit(info, stance));
			}
		}
	}
}
