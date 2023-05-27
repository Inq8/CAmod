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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Spawn another actor upon being captured.")]
	public class SpawnActorOnCaptureInfo : ConditionalTraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actor to spawn on death.")]
		public readonly string Actor = null;

		[Desc("Probability the actor spawns.")]
		public readonly int Probability = 100;

		[Desc("Map player to use when 'InternalName' is defined on 'OwnerType'.")]
		public readonly string InternalOwner = "Neutral";

		[Desc("Skips the spawned actor's make animations if true.")]
		public readonly bool SkipMakeAnimations = true;

		[Desc("Offset of the spawned actor relative to the dying actor's position.",
			"Warning: Spawning an actor outside the parent actor's footprint/influence might",
			"lead to unexpected behaviour.")]
		public readonly CVec Offset = CVec.Zero;

		[Desc("Should an actor spawn after the player has been defeated (e.g. after surrendering)?")]
		public readonly bool SpawnAfterDefeat = true;

		[Desc("Delay in ticks before actor is spawned.")]
		public readonly int Delay = 0;

		public override object Create(ActorInitializer init) { return new SpawnActorOnCapture(init, this); }
	}

	public class SpawnActorOnCapture : ConditionalTrait<SpawnActorOnCaptureInfo>, INotifyCapture, ITick
	{
		readonly string faction;
		int delayTicks;
		bool spawnPending;
		WPos spawnPosition;
		CPos spawnCell;

		public SpawnActorOnCapture(ActorInitializer init, SpawnActorOnCaptureInfo info)
			: base(info)
		{
			faction = init.GetValue<FactionInit, string>(init.Self.Owner.Faction.InternalName);
			delayTicks = Info.Delay;
			spawnPending = true;
		}

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner, BitSet<CaptureType> captureTypes)
		{
			if (IsTraitDisabled || !self.IsInWorld)
				return;

			spawnPending = true;
			spawnPosition = self.CenterPosition;
			spawnCell = self.Location + Info.Offset;
			delayTicks = Info.Delay;
		}

		void ITick.Tick(OpenRA.Actor self)
		{
			if (!spawnPending)
				return;

			if (--delayTicks <= 0)
				SpawnActor(self);
		}

		void SpawnActor(Actor self)
		{
			if (self.World.SharedRandom.Next(100) > Info.Probability)
				return;

			var defeated = self.Owner.WinState == WinState.Lost;
			if (defeated && !Info.SpawnAfterDefeat)
				return;

			var td = new TypeDictionary
			{
				new ParentActorInit(self),
				new LocationInit(spawnCell),
				new CenterPositionInit(spawnPosition),
				new FactionInit(faction)
			};

			// Fall back to InternalOwner if the Victim was defeated,
			// but only if InternalOwner is defined
			if (!defeated || string.IsNullOrEmpty(Info.InternalOwner))
				td.Add(new OwnerInit(self.Owner));
			else
				td.Add(new OwnerInit(self.World.Players.First(p => p.InternalName == Info.InternalOwner)));

			if (Info.SkipMakeAnimations)
				td.Add(new SkipMakeAnimsInit());

			self.World.AddFrameEndTask(w => w.CreateActor(Info.Actor, td));
			spawnPending = false;
		}
	}
}
