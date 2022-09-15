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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Attach this to the player actor (not a building!) to define a new shared build queue.",
		"Will only work together with the Production: trait on the actor that actually does the production.",
		"You will also want to add PrimaryBuildings: to let the user choose where new units should exit.",
		"CA version allows build speed reduction to take into account all buildings for a type if BuildAtProductionType is set.")]
	public class ClassicProductionQueueCAInfo : ClassicProductionQueueInfo
	{
		[Desc("If true, ignore BuildAtProductionType when calculating build duration, so all structures for this queue are counted.")]
		public readonly bool CombinedBuildSpeedReduction = false;

		public override object Create(ActorInitializer init) { return new ClassicProductionQueueCA(init, this); }
	}

	public class ClassicProductionQueueCA : ClassicProductionQueue
	{
		public readonly new ClassicProductionQueueCAInfo Info;
		readonly Actor self;

		public ClassicProductionQueueCA(ActorInitializer init, ClassicProductionQueueCAInfo info)
			: base(init, info)
		{
			self = init.Self;
			Info = info;
		}

		public override int GetBuildTime(ActorInfo unit, BuildableInfo bi)
		{
			if (developerMode.FastBuild)
				return 0;

			var time = GetBaseBuildTime(unit, bi);

			if (Info.SpeedUp)
			{
				var type = bi.BuildAtProductionType ?? Info.Type;

				// difference with ClassicProductionQueue, prevents BuildAtProductionType from overriding the type
				if (Info.CombinedBuildSpeedReduction)
					type = Info.Type;

				var selfsameProductionsCount = self.World.ActorsWithTrait<Production>()
					.Count(p => !p.Trait.IsTraitDisabled && !p.Trait.IsTraitPaused && p.Actor.Owner == self.Owner && p.Trait.Info.Produces.Contains(type));

				var speedModifier = selfsameProductionsCount.Clamp(1, Info.BuildTimeSpeedReduction.Length) - 1;
				time = (time * Info.BuildTimeSpeedReduction[speedModifier]) / 100;
			}

			return time;
		}

		// copied from ProductionQueue to bypass ClassicProductionQueue.GetBuildTime()
		public virtual int GetBaseBuildTime(ActorInfo unit, BuildableInfo bi)
		{
			if (developerMode.FastBuild)
				return 0;

			var time = bi.BuildDuration;
			if (time == -1)
				time = GetProductionCost(unit);

			var modifiers = unit.TraitInfos<IProductionTimeModifierInfo>()
				.Select(t => t.GetProductionTimeModifier(techTree, Info.Type))
				.Append(bi.BuildDurationModifier)
				.Append(Info.BuildDurationModifier);

			return Util.ApplyPercentageModifiers(time, modifiers);
		}
	}
}
