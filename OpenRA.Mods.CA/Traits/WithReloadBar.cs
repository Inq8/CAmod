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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc(".")]
	public class WithReloadBarInfo : TraitInfo
	{
		[Desc("Armament to track reload of.")]
		public readonly string Armament = "primary";

		public readonly Color Color = Color.White;
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		public override object Create(ActorInitializer init) { return new WithReloadBar(init, this); }
	}

	public class WithReloadBar : INotifyAttack, ISelectionBar, ITick
	{
		public readonly WithReloadBarInfo Info;
		readonly Actor self;
		int reloadTicks;
		int reloadTicksRemaining;

		public WithReloadBar(ActorInitializer init, WithReloadBarInfo info)
		{
			Info = info;
			self = init.Self;
			reloadTicks = reloadTicksRemaining = 0;
		}

		void ITick.Tick(Actor self)
		{
			if (reloadTicksRemaining > 0)
				reloadTicksRemaining--;
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (a.Info.Name != Info.Armament)
				return;

			self.World.AddFrameEndTask(w => reloadTicks = reloadTicksRemaining = a.FireDelay);
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) {}

		float ISelectionBar.GetValue()
		{
			if (!Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(self.World.RenderPlayer)))
				return 0;

			if (reloadTicks == 0 || reloadTicksRemaining == 0)
				return 0;

			return (float)(reloadTicks - reloadTicksRemaining) / reloadTicks;
		}

		bool ISelectionBar.DisplayWhenEmpty => false;

		Color ISelectionBar.GetColor() { return Info.Color; }
	}
}
