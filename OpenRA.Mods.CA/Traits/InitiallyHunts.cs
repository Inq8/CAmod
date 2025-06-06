﻿#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Eject a ground soldier or a paratrooper while in the air.")]
	public class InitiallyHuntsInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new InitiallyHunts(this); }
	}

	public class InitiallyHunts : ConditionalTrait<InitiallyHuntsInfo>, INotifyCreated
	{
		public InitiallyHunts(InitiallyHuntsInfo info)
			: base(info) { }

		void INotifyCreated.Created(Actor self)
		{
			self.QueueActivity(new Hunt(self));
		}
	}
}
