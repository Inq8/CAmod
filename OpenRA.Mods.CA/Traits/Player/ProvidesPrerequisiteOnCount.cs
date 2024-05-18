#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.Player)]
	public class ProvidesPrerequisiteOnCountInfo : TraitInfo, ITechTreePrerequisiteInfo
	{
		[Desc("The prerequisite type that this provides.")]
		[FieldLoader.Require]
		public readonly string Prerequisite = null;

		[Desc("The counter name.")]
		[FieldLoader.Require]
		public readonly string Name = null;

		[Desc("The count required to enable the prerequisite.")]
		public readonly int RequiredCount = 1;

		[Desc("List of factions that can affect this count. Leave blank for any faction.")]
		public readonly string[] Factions = { };

		[Desc("If true, the prerequisite is permanent once the count is reached.")]
		public readonly bool Permanent = true;

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info)
		{
			return new string[] { Prerequisite };
		}

		public override object Create(ActorInitializer init) { return new ProvidesPrerequisiteOnCount(init, this); }
	}

	public class ProvidesPrerequisiteOnCount : ITechTreePrerequisite, INotifyCreated
	{
		readonly ProvidesPrerequisiteOnCountInfo info;
		readonly Actor self;
		int count;
		TechTree techTree;
		bool permanentlyUnlocked;

		public ProvidesPrerequisiteOnCount(ActorInitializer init, ProvidesPrerequisiteOnCountInfo info)
		{
			this.info = info;
			self = init.Self;
			permanentlyUnlocked = false;
		}

		bool Enabled
		{
			get
			{
				return permanentlyUnlocked || count >= info.RequiredCount;
			}
		}

		public string Name => info.Name;
		public string[] Factions => info.Factions;

		public IEnumerable<string> ProvidesPrerequisites
		{
			get
			{
				if (!Enabled)
					yield break;

				yield return info.Prerequisite;
			}
		}

		void INotifyCreated.Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query other player traits from self, knowing that
			// it refers to the same actor as self.Owner.PlayerActor
			var playerActor = self.Info.Name == "player" ? self : self.Owner.PlayerActor;
			techTree = playerActor.Trait<TechTree>();
			techTree.ActorChanged(self);
		}

		public void Increment()
		{
			count++;
			techTree.ActorChanged(self);

			if (info.Permanent && count >= info.RequiredCount)
				permanentlyUnlocked = true;
		}

		public void Decrement()
		{
			count--;
			techTree.ActorChanged(self);
		}
	}
}
