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
	[ScriptPropertyGroup("MadTankCA")]
	public class MadTankCAProperties : ScriptActorProperties, Requires<MadTankCAInfo>
	{
		readonly MadTankCA madTank;

		public MadTankCAProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			madTank = self.Trait<MadTankCA>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Queue MAD Tank detonation.")]
		public void MadTankDetonate()
		{
			Self.QueueActivity(true, new MadTankCA.DetonationSequence(Self, madTank));
		}
	}
}
