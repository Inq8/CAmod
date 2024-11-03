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
	public enum MindControlledOwnerType { Victim, Master, InternalName }

	[Desc("Spawn another actor immediately upon being mind controlled.")]
	public class SpawnActorOnMindControlledInfo : ConditionalTraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actor to spawn on being mind controlled.")]
		public readonly string Actor = null;

		[Desc("Probability the actor spawns.")]
		public readonly int Probability = 100;

		[Desc("Owner of the spawned actor. Allowed keywords:" +
			"'Victim', 'Killer' and 'InternalName'. " +
			"Falls back to 'InternalName' if 'Victim' is used " +
			"and the victim is defeated (see 'SpawnAfterDefeat').")]
		public readonly MindControlledOwnerType OwnerType = MindControlledOwnerType.Victim;

		[Desc("Map player to use when 'InternalName' is defined on 'OwnerType'.")]
		public readonly string InternalOwner = "Neutral";

		[Desc("Changes the effective (displayed) owner of the spawned actor to the old owner (victim).")]
		public readonly bool EffectiveOwnerFromOwner = false;

		[Desc("Skips the spawned actor's make animations if true.")]
		public readonly bool SkipMakeAnimations = true;

		[Desc("Should an actor only be spawned when the 'Creeps' setting is true?")]
		public readonly bool RequiresLobbyCreeps = false;

		[Desc("Offset of the spawned actor relative to the dying actor's position.",
			"Warning: Spawning an actor outside the parent actor's footprint/influence might",
			"lead to unexpected behaviour.")]
		public readonly CVec Offset = CVec.Zero;

		[Desc("Should an actor spawn after the player has been defeated (e.g. after surrendering)?")]
		public readonly bool SpawnAfterDefeat = true;

		public override object Create(ActorInitializer init) { return new SpawnActorOnMindControlled(init, this); }
	}

	public class SpawnActorOnMindControlled : ConditionalTrait<SpawnActorOnMindControlledInfo>, INotifyMindControlled
	{
		readonly string faction;
		readonly bool enabled;

		Player attackingPlayer;

		public SpawnActorOnMindControlled(ActorInitializer init, SpawnActorOnMindControlledInfo info)
			: base(info)
		{
			enabled = !info.RequiresLobbyCreeps || init.Self.World.WorldActor.Trait<MapCreeps>().Enabled;
			faction = init.GetValue<FactionInit, string>(init.Self.Owner.Faction.InternalName);
		}

		void INotifyMindControlled.MindControlled(Actor self, Actor master)
		{
			if (!enabled || IsTraitDisabled || !self.IsInWorld)
				return;

			if (self.World.SharedRandom.Next(100) > Info.Probability)
				return;

			attackingPlayer = master.Owner;

			var defeated = self.Owner.WinState == WinState.Lost;
			if (defeated && !Info.SpawnAfterDefeat)
				return;

			var td = new TypeDictionary
			{
				new ParentActorInit(self),
				new LocationInit(self.Location + Info.Offset),
				new CenterPositionInit(self.CenterPosition),
				new FactionInit(faction)
			};

			if (self.EffectiveOwner != null && self.EffectiveOwner.Disguised)
				td.Add(new EffectiveOwnerInit(self.EffectiveOwner.Owner));
			else if (Info.EffectiveOwnerFromOwner)
				td.Add(new EffectiveOwnerInit(self.Owner));

			if (Info.OwnerType == MindControlledOwnerType.Victim)
			{
				// Fall back to InternalOwner if the Victim was defeated,
				// but only if InternalOwner is defined
				if (!defeated || string.IsNullOrEmpty(Info.InternalOwner))
					td.Add(new OwnerInit(self.Owner));
				else
				{
					td.Add(new OwnerInit(self.World.Players.First(p => p.InternalName == Info.InternalOwner)));
					if (!td.Contains<EffectiveOwnerInit>())
						td.Add(new EffectiveOwnerInit(self.Owner));
				}
			}
			else if (Info.OwnerType == MindControlledOwnerType.Master)
				td.Add(new OwnerInit(attackingPlayer));
			else
				td.Add(new OwnerInit(self.World.Players.First(p => p.InternalName == Info.InternalOwner)));

			if (Info.SkipMakeAnimations)
				td.Add(new SkipMakeAnimsInit());

			foreach (var modifier in self.TraitsImplementing<IDeathActorInitModifier>())
				modifier.ModifyDeathActorInit(self, td);

			var huskActor = self.TraitsImplementing<IHuskModifier>()
				.Select(ihm => ihm.HuskActor(self))
				.FirstOrDefault(a => a != null);

			self.World.AddFrameEndTask(w => w.CreateActor(huskActor ?? Info.Actor, td));
		}

		void INotifyMindControlled.Released(Actor self, Actor master) {}
	}
}
