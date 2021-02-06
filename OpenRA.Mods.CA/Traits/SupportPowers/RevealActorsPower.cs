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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Spawns camera actors at the location of specified actor types that stay for a limited amount of time.")]
	public class RevealActorsPowerInfo : SupportPowerInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actor to spawn.")]
		public readonly string CameraActor = null;

		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actors to spawn at.")]
		public readonly string[] TargetActors;

		[Desc("Amount of time to keep the actor alive in ticks. Value < 0 means this actor will not remove itself.")]
		public readonly int LifeTime = 250;

		public readonly string DeploySound = null;

		public readonly string EffectImage = null;

		[SequenceReference(nameof(EffectImage))]
		public readonly string EffectSequence = "idle";

		[PaletteReference]
		public readonly string EffectPalette = null;

		public override object Create(ActorInitializer init) { return new RevealActorsPower(init.Self, this); }
	}

	public class RevealActorsPower : SupportPower
	{
		readonly RevealActorsPowerInfo info;

		public RevealActorsPower(Actor self, RevealActorsPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.IssueOrder(new Order(order, manager.Self, false));
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			PlayLaunchSounds();

			var info = Info as RevealActorsPowerInfo;

			if (info.CameraActor != null)
			{
				self.World.AddFrameEndTask(w =>
				{
					PlayLaunchSounds();

					foreach (var target in FindTargetActors(self.World))
					{
						if (!string.IsNullOrEmpty(info.EffectSequence) && !string.IsNullOrEmpty(info.EffectPalette))
							w.Add(new SpriteEffect(target.CenterPosition, w, info.EffectImage, info.EffectSequence, info.EffectPalette));

						var actor = w.CreateActor(info.CameraActor, new TypeDictionary
						{
							new LocationInit(self.World.Map.CellContaining(target.CenterPosition)),
							new OwnerInit(self.Owner),
						});

						if (info.LifeTime > -1)
						{
							actor.QueueActivity(new Wait(info.LifeTime));
							actor.QueueActivity(new RemoveSelf());
						}
					}
				});
			}
		}

		internal List<Actor> FindTargetActors(World world)
		{
			return world.Actors.Where(IsTargetActor).ToList();
		}

		public bool IsTargetActor(Actor a)
		{
			return a != null && !a.IsDead && info.TargetActors.Contains(a.Info.Name);
		}
	}
}
