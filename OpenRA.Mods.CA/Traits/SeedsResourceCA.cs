#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Lets the actor spread resources around it in a circle.")]
	sealed class SeedsResourceCAInfo : ConditionalTraitInfo
	{
		public readonly int Interval = 75;
		public readonly string ResourceType = "Ore";
		public readonly int MaxRange = 100;

		public override object Create(ActorInitializer init) { return new SeedsResourceCA(init.Self, this); }
	}

	sealed class SeedsResourceCA : ConditionalTrait<SeedsResourceCAInfo>, ITick, ISeedableResource
	{
		readonly SeedsResourceCAInfo info;
		readonly IResourceLayer resourceLayer;
		ISeedsResourceIntervalModifier[] seedsResourceModifiers;

		public SeedsResourceCA(Actor self, SeedsResourceCAInfo info)
			: base(info)
		{
			this.info = info;
			resourceLayer = self.World.WorldActor.Trait<IResourceLayer>();
		}

		int ticks;

		protected override void Created(Actor self)
		{
			base.Created(self);
			seedsResourceModifiers = self.TraitsImplementing<ISeedsResourceIntervalModifier>().ToArray();
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (--ticks <= 0)
			{
				Seed(self);
				var seedsResourceModifier = seedsResourceModifiers.Select(x => x.GetModifier());
				ticks = Util.ApplyPercentageModifiers(info.Interval, seedsResourceModifier);
			}
		}

		public void Seed(Actor self)
		{
			var cell = Util.RandomWalk(self.Location, self.World.SharedRandom)
				.Take(info.MaxRange)
				.SkipWhile(p => resourceLayer.GetResource(p).Type == info.ResourceType && !resourceLayer.CanAddResource(info.ResourceType, p))
				.Cast<CPos?>().FirstOrDefault();

			if (cell != null && resourceLayer.CanAddResource(info.ResourceType, cell.Value))
				resourceLayer.AddResource(info.ResourceType, cell.Value);
		}
	}
}
