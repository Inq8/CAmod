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
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	public enum CargoEffect { None, Kill, Block }

	[Desc("Changes targets to neutral.")]
	public class ChangeOwnerToNeutralWarhead : Warhead
	{
		[Desc("Faction to change to.")]
		public readonly string Owner = "Neutral";

		[Desc("Whether cargo is killed, blocks neutralization, or has no effect.")]
		public readonly CargoEffect CargoEffect = CargoEffect.Kill;

		public readonly WDist Range = WDist.FromCells(1);

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			var actors = target.Type == TargetType.Actor ? new[] { target.Actor } :
				firedBy.World.FindActorsInCircle(target.CenterPosition, Range);

			foreach (var a in actors)
			{
				// Don't do anything on friendly fire
				if (a.Owner == firedBy.Owner)
					continue;

				if (!target.IsValidFor(firedBy))
					continue;

				if (!IsValidAgainst(a, firedBy))
					continue;

				if (CargoEffect != CargoEffect.None)
				{
					var cargo = a.TraitOrDefault<Cargo>();
					if (cargo != null)
					{
						if (CargoEffect == CargoEffect.Block && cargo.PassengerCount > 0)
							continue;

						if (CargoEffect == CargoEffect.Kill)
						{
							while (!cargo.IsEmpty())
							{
								var p = cargo.Unload(a);
								p.Kill(firedBy);
							}
						}
					}
				}

				a.ChangeOwner(a.World.Players.First(p => p.InternalName == Owner)); // Permanent

				// Stop shooting, you have new enemies
				a.CancelActivity();
			}
		}
	}
}
