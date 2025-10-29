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
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	[Desc("This warhead can attach an actor to the target.")]
	public class AttachActorWarhead : WarheadAS
	{
		[ActorReference(typeof(AttachableInfo))]
		[FieldLoader.Require]
		[Desc("Actor.")]
		public readonly string Actor = null;

		[Desc("Range of targets to be attached.")]
		public readonly WDist Range = WDist.FromCells(1);

		[Desc("Maximum number of targets to attach actors to.")]
		public readonly int MaxTargets = 1;

		[Desc("List of sounds that can be played on attaching.")]
		public readonly string[] AttachSounds = new string[0];

		[Desc("List of sounds that can be played if attaching is not possible.")]
		public readonly string[] MissSounds = new string[0];

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;

			if (!target.IsValidFor(firedBy))
				return;

			var pos = target.CenterPosition;

			if (!IsValidImpact(pos, firedBy))
				return;

			var world = firedBy.World;
			var availableActors = firedBy.World.FindActorsOnCircle(pos, Range);
			var numAttached = 0;

			foreach (var actor in availableActors)
			{
				if (!IsValidAgainst(actor, firedBy))
					continue;

				if (actor.IsDead)
					continue;

				var activeShapes = actor.TraitsImplementing<HitShape>().Where(Exts.IsTraitEnabled);
				if (!activeShapes.Any())
					continue;

				var distance = activeShapes.Min(t => t.DistanceFromEdge(actor, pos));

				if (distance > Range)
					continue;

				var actorToAttach = actor.World.CreateActor(false, Actor.ToLowerInvariant(), new TypeDictionary
				{
					new OwnerInit(firedBy.Owner),
				});

				var attachableTrait = actorToAttach.TraitOrDefault<Attachable>();
				if (attachableTrait == null)
				{
					actorToAttach.Dispose();
					continue;
				}

				var attachableToTrait = actor.TraitsImplementing<AttachableTo>().FirstOrDefault(a => a.CanAttach(attachableTrait));

				if (attachableToTrait != null)
				{
					Attach(actor, attachableToTrait, actorToAttach);
					numAttached++;
				}

				if (numAttached > MaxTargets)
					break;
			}

			if (numAttached > 0)
			{
				var attachSound = AttachSounds.RandomOrDefault(world.LocalRandom);
				if (attachSound != null)
					Game.Sound.Play(SoundType.World, attachSound, pos);
			}
			else
			{
				var failSound = MissSounds.RandomOrDefault(world.LocalRandom);
				if (failSound != null)
					Game.Sound.Play(SoundType.World, failSound, pos);
			}
		}

		void Attach(Actor targetActor, AttachableTo targetTrait, Actor actorToAttach)
		{
			var world = targetActor.World;
			var map = world.Map;
			var targetCell = map.CellContaining(targetActor.CenterPosition);

			targetActor.World.AddFrameEndTask(w =>
			{
				w.Add(actorToAttach);
				var positionable = actorToAttach.TraitOrDefault<IPositionable>();
				if (positionable != null)
					positionable.SetPosition(actorToAttach, targetCell);

				var attached = targetTrait.Attach(actorToAttach, actorToAttach.Trait<Attachable>());
				if (!attached)
					actorToAttach.Dispose();
			});
		}
	}
}
