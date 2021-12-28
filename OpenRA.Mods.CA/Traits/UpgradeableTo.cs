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
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Lists actors this actor may be upgraded to (to replace in production queue).")]
	public class UpgradeableToInfo : TraitInfo
	{
		[FieldLoader.Require]
		[Desc("Actors.")]
		public readonly HashSet<string> Actors = new HashSet<string>();

		public override object Create(ActorInitializer init) { return new UpgradeableTo(init, this); }
	}

	public class UpgradeableTo
	{
		public readonly UpgradeableToInfo Info;

		public UpgradeableTo(ActorInitializer init, UpgradeableToInfo info)
		{
			Info = info;
		}
	}
}
