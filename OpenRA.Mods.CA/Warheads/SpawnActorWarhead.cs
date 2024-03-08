#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	public enum ASOwnerType { Attacker, InternalName }

	[Desc("Spawn actors upon explosion. Don't use this with buildings.")]
	public class SpawnActorWarhead : WarheadAS, IRulesetLoaded<WeaponInfo>
	{
		[Desc("The cell range to try placing the actors within.")]
		public readonly int Range = 10;

		[Desc("Actors to spawn.")]
		public readonly string[] Actors = { };

		[Desc("Try to parachute the actors. When unset, actors will just fall down visually using FallRate."
			+ " Requires the Parachutable trait on all actors if set.")]
		public readonly bool Paradrop = false;

		public readonly int FallRate = 130;

		[Desc("Always spawn the actors on the ground.")]
		public readonly bool ForceGround = false;

		[Desc("Owner of the spawned actor. Allowed keywords:" +
			"'Attacker' and 'InternalName'.")]
		public readonly ASOwnerType OwnerType = ASOwnerType.Attacker;

		[Desc("Map player to use when 'InternalName' is defined on 'OwnerType'.")]
		public readonly string InternalOwner = "Neutral";

		[Desc("Defines the image of an optional animation played at the spawning location.")]
		public readonly string Image = null;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Defines the sequence of an optional animation played at the spawning location.")]
		public readonly string Sequence = "idle";

		[PaletteReference]
		[Desc("Defines the palette of an optional animation played at the spawning location.")]
		public readonly string Palette = "effect";

		[Desc("List of sounds that can be played at the spawning location.")]
		public readonly string[] Sounds = new string[0];

		[Desc("For non-positionable actors only, whether to avoid spawning on top of existing actors.")]
		public readonly bool AvoidActors = false;

		[Desc("For actors with facing, match the facing of the source (if the source also has a facing) .")]
		public readonly bool MatchSourceFacing = false;

		public readonly bool UsePlayerPalette = false;

		[Desc("Will only spawn if the owner of the source actor has these prerequisites.")]
		public readonly string[] Prerequisites = Array.Empty<string>();

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			foreach (var a in Actors)
			{
				var actorInfo = rules.Actors[a.ToLowerInvariant()];
				var buildingInfo = actorInfo.TraitInfoOrDefault<BuildingInfo>();

				if (buildingInfo != null)
					throw new YamlException($"SpawnActorWarhead cannot be used to spawn building actor '{a}'!");
			}
		}

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;

			if (Prerequisites.Length > 0 && !firedBy.Owner.PlayerActor.Trait<TechTree>().HasPrerequisites(Prerequisites))
				return;

			if (!target.IsValidFor(firedBy))
				return;

			var map = firedBy.World.Map;
			var targetCell = map.CellContaining(target.CenterPosition);

			if (!IsValidImpact(target.CenterPosition, firedBy))
				return;

			var targetCells = map.FindTilesInCircle(targetCell, Range);
			var cell = targetCells.GetEnumerator();

			foreach (var a in Actors)
			{
				var placed = false;
				var ai = map.Rules.Actors[a.ToLowerInvariant()];
				var td = CreateTypeDictionary(firedBy, targetCell);

				// Lambdas can't use 'in' variables, so capture a copy for later
				var delayedTarget = target;

				firedBy.World.AddFrameEndTask(w =>
				{
					var unit = firedBy.World.CreateActor(false, a.ToLowerInvariant(), td);
					var positionable = unit.TraitOrDefault<IPositionable>();
					cell = targetCells.GetEnumerator();

					if (positionable == null)
					{
						unit.Dispose();

						if (AvoidActors)
						{
							while (cell.MoveNext() && !placed)
							{
								var actorsInCell = firedBy.World.ActorMap.GetActorsAt(cell.Current);

								if (actorsInCell.Any())
									continue;

								placed = true;
								td = CreateTypeDictionary(firedBy, cell.Current);
							}
						}
						else
							placed = true;

						if (placed)
							firedBy.World.CreateActor(a, td);
					}
					else
					{
						while (cell.MoveNext() && !placed)
						{
							var subCell = positionable.GetAvailableSubCell(cell.Current);

							if (ai.HasTraitInfo<AircraftInfo>()
								&& ai.TraitInfo<AircraftInfo>().CanEnterCell(firedBy.World, unit, cell.Current, SubCell.FullCell, null, BlockedByActor.None))
								subCell = SubCell.FullCell;

							if (subCell != SubCell.Invalid)
							{
								positionable.SetPosition(unit, cell.Current, subCell);

								var pos = unit.CenterPosition;
								if (!ForceGround)
									pos += new WVec(WDist.Zero, WDist.Zero, firedBy.World.Map.DistanceAboveTerrain(delayedTarget.CenterPosition));

								positionable.SetCenterPosition(unit, pos);
								w.Add(unit);

								if (Paradrop)
									unit.QueueActivity(new Parachute(unit));
								else
									unit.QueueActivity(new FallDown(unit, pos, FallRate));

								var palette = Palette;
								if (UsePlayerPalette)
									palette += unit.Owner.InternalName;

								if (Image != null)
									w.Add(new SpriteEffect(pos, w, Image, Sequence, palette));

								var sound = Sounds.RandomOrDefault(firedBy.World.LocalRandom);
								if (sound != null)
									Game.Sound.Play(SoundType.World, sound, pos);

								placed = true;
							}
						}

						if (!placed)
							unit.Dispose();
					}
				});
			}
		}

		TypeDictionary CreateTypeDictionary(Actor firedBy, CPos targetCell)
		{
			var td = new TypeDictionary();

			if (OwnerType == ASOwnerType.Attacker)
				td.Add(new OwnerInit(firedBy.Owner));
			else
				td.Add(new OwnerInit(firedBy.World.Players.First(p => p.InternalName == InternalOwner)));

			td.Add(new LocationInit(targetCell));

			if (MatchSourceFacing)
			{
				var facing = firedBy.TraitOrDefault<IFacing>();
				if (facing != null)
					td.Add(new FacingInit(facing.Facing));
			}

			return td;
		}
	}
}
