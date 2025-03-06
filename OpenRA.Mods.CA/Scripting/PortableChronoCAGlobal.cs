#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.CA.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Scripting
{
	[ScriptPropertyGroup("PortableChronoCA")]
	public class PortableChronoCAProperties : ScriptActorProperties, Requires<PortableChronoCAInfo>
	{
		readonly PortableChronoCA PortableChrono;

		public PortableChronoCAProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			PortableChrono = self.Trait<PortableChronoCA>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Queue portable chrono teleport.")]
		public void PortableChronoTeleport(CPos targetCell, bool queued = false)
		{
			var target = Target.FromCell(Self.World, targetCell);
			PortableChrono.ResolveOrder(Self, new Order("PortableChronoTeleport", Self, target, queued));
		}
	}
}
