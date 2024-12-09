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
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	[Desc("Grant an external condition to hit actors.")]
	public class GrantExternalConditionCAWarhead : Warhead
	{
		[FieldLoader.Require]
		[Desc("The condition to apply. Must be included in the target actor's ExternalConditions list.")]
		public readonly string Condition = null;

		[Desc("Duration of the condition (in ticks). Set to 0 for a permanent condition.")]
		public readonly int Duration = 0;

		public readonly WDist Range = WDist.FromCells(1);

		public readonly WDist RangeLimit = WDist.FromCells(2);

		public readonly bool HitShapeCheck = true;

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;

			if (target.Type == TargetType.Invalid)
				return;

			var rangeLimit = Range > RangeLimit ? Range : RangeLimit;
			var actors = firedBy.World.FindActorsInCircle(target.CenterPosition, rangeLimit);

			foreach (var a in actors)
			{
				if (!IsValidAgainst(a, firedBy))
					continue;

				if (HitShapeCheck)
				{
					HitShape closestActiveShape = null;
					var closestDistance = int.MaxValue;

					// PERF: Avoid using TraitsImplementing<HitShape> that needs to find the actor in the trait dictionary.
					foreach (var targetPos in a.EnabledTargetablePositions)
					{
						if (targetPos is HitShape hitshape)
						{
							var distance = hitshape.DistanceFromEdge(a, target.CenterPosition).Length;
							if (distance < closestDistance)
							{
								closestDistance = distance;
								closestActiveShape = hitshape;
							}
						}
					}

					// Cannot be damaged without an active HitShape.
					if (closestActiveShape == null)
						continue;

					// Cannot be damaged if HitShape is outside Spread.
					if (closestDistance > Range.Length)
						continue;
				}

				a.TraitsImplementing<ExternalCondition>()
					.FirstOrDefault(t => t.Info.Condition == Condition && t.CanGrantCondition(firedBy))
					?.GrantCondition(a, firedBy, Duration);
			}
		}
	}
}
