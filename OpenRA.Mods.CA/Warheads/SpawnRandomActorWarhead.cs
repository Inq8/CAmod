#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	[Desc("Spawn actors upon explosion. Don't use this with buildings.")]
	public class SpawnRandomActorWarhead : WarheadAS, IRulesetLoaded<WeaponInfo>
	{
		[Desc("The cell range to try placing the actors within.")]
		public readonly int Range = 10;

		[Desc("List of Random Actors to spawn.")]
		public readonly string[] RandomActors = { };

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

		[SequenceReference("Image")]
		[Desc("Defines the sequence of an optional animation played at the spawning location.")]
		public readonly string Sequence = "idle";

		[PaletteReference]
		[Desc("Defines the palette of an optional animation played at the spawning location.")]
		public readonly string Palette = "effect";

		[Desc("List of sounds that can be played at the spawning location.")]
		public readonly string[] Sounds = new string[0];

		public readonly bool UsePlayerPalette = false;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			foreach (var a in RandomActors)
			{
				var actorInfo = rules.Actors[a.ToLowerInvariant()];
				var buildingInfo = actorInfo.TraitInfoOrDefault<BuildingInfo>();

				if (buildingInfo != null)
					throw new YamlException("SpawnActorWarhead cannot be used to spawn building actor '{0}'!".F(a));
			}
		}

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			if (!target.IsValidFor(firedBy))
				return;

			var map = firedBy.World.Map;
			var targetCell = map.CellContaining(target.CenterPosition);

			if (!IsValidImpact(target.CenterPosition, firedBy))
				return;

			var targetCells = map.FindTilesInCircle(targetCell, Range);
			var cell = targetCells.GetEnumerator();

			var placed = false;
			var world = firedBy.World;
			var td = new TypeDictionary();
			var ai = map.Rules.Actors;
			var actor = RandomActors.Random(world.SharedRandom);

			if (OwnerType == ASOwnerType.Attacker)
				td.Add(new OwnerInit(firedBy.Owner));
			else
				td.Add(new OwnerInit(firedBy.World.Players.First(p => p.InternalName == InternalOwner)));

			// Lambdas can't use 'in' variables, so capture a copy for later
			var delayedTarget = target;

			firedBy.World.AddFrameEndTask(w =>
				{
					var unit = firedBy.World.CreateActor(false, actor.ToLowerInvariant(), td);
					var positionable = unit.TraitOrDefault<IPositionable>();
					cell = targetCells.GetEnumerator();

					while (cell.MoveNext() && !placed)
					{
						var subCell = positionable.GetAvailableSubCell(cell.Current);

						if (subCell != SubCell.Invalid)
						{
							positionable.SetPosition(unit, cell.Current, subCell);

							var pos = unit.CenterPosition;
							if (!ForceGround)
								pos += new WVec(WDist.Zero, WDist.Zero, firedBy.World.Map.DistanceAboveTerrain(delayedTarget.CenterPosition));

							positionable.SetVisualPosition(unit, pos);
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
			});
		}
	}
}
