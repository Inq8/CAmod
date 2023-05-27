#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Plays an animation when trait is enabled or re-enabled, replacing the default body animation (plays once, unlike WithEnabledAnimation).")]
	public class WithEnabledAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>
	{
		[SequenceReference]
		[Desc("Sequence names to use.")]
		public readonly string Sequence = "make";

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		public override object Create(ActorInitializer init) { return new WithEnabledAnimation(init.Self, this); }
	}

	public class WithEnabledAnimation : ConditionalTrait<WithEnabledAnimationInfo>
	{
		readonly WithSpriteBody wsb;

		public WithEnabledAnimation(Actor self, WithEnabledAnimationInfo info)
			: base(info)
		{
			wsb = self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == Info.Body);
		}

		protected override void TraitEnabled(Actor self)
		{
			wsb.PlayCustomAnimation(self, Info.Sequence);
		}

		protected override void TraitDisabled(Actor self)
		{
			wsb.CancelCustomAnimation(self);
		}
	}
}
