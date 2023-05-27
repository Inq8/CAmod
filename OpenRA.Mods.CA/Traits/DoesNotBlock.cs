#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Actor with this trait can be passed through by actors with the specified target types.")]
	public class DoesNotBlockInfo : TraitInfo
	{
		[Desc("Target types to allow to pass through.")]
		public readonly BitSet<TargetableType> TargetTypes = default(BitSet<TargetableType>);

		public override object Create(ActorInitializer init) { return new DoesNotBlock(init, this); }
	}

	public class DoesNotBlock : ITemporaryBlocker
	{
		public DoesNotBlockInfo Info { get; set; }

		public DoesNotBlock(ActorInitializer init, DoesNotBlockInfo info)
		{
			Info = info;
		}

		bool ITemporaryBlocker.CanRemoveBlockage(Actor self, Actor blocking)
		{
			return Info.TargetTypes.IsEmpty || Info.TargetTypes.Overlaps(blocking.GetEnabledTargetTypes());
		}

		bool ITemporaryBlocker.IsBlocking(Actor self, CPos cell)
		{
			return true;
		}
	}
}
