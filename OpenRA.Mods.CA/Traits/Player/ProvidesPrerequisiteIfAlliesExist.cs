#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Provides a prerequisite if one or more allies exist.")]
	public class ProvidesPrerequisiteIfAlliesExistInfo : TraitInfo, ITechTreePrerequisiteInfo
	{
		[FieldLoader.Require]
		[Desc("The prerequisite to provide when allies exist.")]
		public readonly string Prerequisite = null;

		[Desc("Count non-playable players as allies.")]
		public readonly bool CountNonPlayable = false;

		[Desc("Count bots as allies.")]
		public readonly bool CountBots = false;

		[Desc("Count spectators as allies.")]
		public readonly bool CountSpectators = false;

		[Desc("Minimum number of allies required to provide the prerequisite.")]
		public readonly int MinimumAllies = 1;

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info)
		{
			yield return Prerequisite;
		}

		public override object Create(ActorInitializer init) { return new ProvidesPrerequisiteIfAlliesExist(init, this); }
	}

	public class ProvidesPrerequisiteIfAlliesExist : ITechTreePrerequisite, INotifyCreated
	{
		public readonly ProvidesPrerequisiteIfAlliesExistInfo Info;
		bool prerequisiteGranted;

		public ProvidesPrerequisiteIfAlliesExist(ActorInitializer init, ProvidesPrerequisiteIfAlliesExistInfo info)
		{
			Info = info;
			prerequisiteGranted = false;
		}

		public IEnumerable<string> ProvidesPrerequisites
		{
			get
			{
				if (prerequisiteGranted)
					yield return Info.Prerequisite;
			}
		}

		void INotifyCreated.Created(Actor self)
		{
			var allyCount = self.World.Players.Count(p => p != self.Owner
				&& p.IsAlliedWith(self.Owner)
				&& (Info.CountSpectators || !p.Spectating)
				&& (Info.CountBots || !p.IsBot)
				&& (Info.CountNonPlayable || p.Playable)
			);

			prerequisiteGranted = allyCount >= Info.MinimumAllies;
		}
	}
}
