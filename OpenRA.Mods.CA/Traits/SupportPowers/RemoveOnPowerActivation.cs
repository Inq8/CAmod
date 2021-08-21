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

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Removes actors when support power is activated.")]
	public class RemoveOnPowerActivationInfo : TraitInfo
	{
		[ActorReference]
		[Desc("Remove all actors of these types for the owner of the actor triggering the support power.")]
		public readonly string[] Actors = null;

		[Desc("Remove the actor triggering the support power on activation.")]
		public readonly bool RemoveSelf = false;

		public override object Create(ActorInitializer init) { return new RemoveOnPowerActivation(init, this); }
	}

	public class RemoveOnPowerActivation : INotifySupportPower
	{
		public readonly RemoveOnPowerActivationInfo Info;

		public RemoveOnPowerActivation(ActorInitializer init, RemoveOnPowerActivationInfo info)
		{
			Info = info;
		}

		void INotifySupportPower.Activated(Actor self)
		{
			var actors = self.World.Actors.Where(a => !a.IsDead && a.Owner == self.Owner &&
				Info.Actors.Contains(a.Info.Name)).ToList();

			foreach (var actor in actors)
				actor.Dispose();

			if (Info.RemoveSelf)
				self.Dispose();
		}

		void INotifySupportPower.Charged(Actor self) { }
	}
}
