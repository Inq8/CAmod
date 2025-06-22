#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Grants player XP to capturing player.")]
	public class GivesPlayerExperienceOnCaptureInfo : ConditionalTraitInfo
	{
		[Desc("Base experience to grant to the capturing player when capturing this actor.")]
		public readonly int PlayerExperience = 0;

		[Desc("Relationships that the structure's previous owner needs to have for the capturing player to receive Experience.")]
		public readonly PlayerRelationship PlayerExperienceRelationships = PlayerRelationship.Enemy;

		[Desc("If true, an amount of XP based on the value of the actor is added to PlayerExperience.")]
		public readonly bool AddExperienceFromValue = false;

		[Desc("Types of captures that grant XP. If empty, all capture types will grant XP.")]
		public readonly int ValuePercentage = 1;

		[Desc("Capture types that grant XP.")]
		public readonly BitSet<CaptureType> CaptureTypes = default;

		[Desc("List of modifiers to apply for specific relationships.")]
		public readonly Dictionary<PlayerRelationship, int> RelationshipModifiers = new Dictionary<PlayerRelationship, int>();

		[Desc("Modifier for captures after the first capture.")]
		public readonly int SubsequentCaptureModifier = 100;

		public override object Create(ActorInitializer init) { return new GivesPlayerExperienceOnCapture(init, this); }
	}

	public class GivesPlayerExperienceOnCapture : ConditionalTrait<GivesPlayerExperienceOnCaptureInfo>, INotifyCapture
	{
		public new readonly GivesPlayerExperienceOnCaptureInfo Info;
		private int captureCount;

		public GivesPlayerExperienceOnCapture(ActorInitializer init, GivesPlayerExperienceOnCaptureInfo info)
			: base(info)
		{
			Info = info;
			captureCount = 0;
		}

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner, BitSet<CaptureType> captureTypes)
		{
			captureCount++;

			if (IsTraitDisabled)
				return;

			if (!Info.CaptureTypes.IsEmpty && !Info.CaptureTypes.Overlaps(captureTypes))
				return;

			if (!Info.PlayerExperienceRelationships.HasRelationship(newOwner.RelationshipWith(oldOwner)))
				return;

			int playerExperience = Info.PlayerExperience;
			var modifiers = new List<int>();

			if (Info.AddExperienceFromValue)
			{
				var value = self.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
				playerExperience += Util.ApplyPercentageModifiers(value, new[] { Info.ValuePercentage });
			}

			if (captureCount > 1)
				modifiers.Add(Info.SubsequentCaptureModifier);

			if (Info.RelationshipModifiers.TryGetValue(newOwner.RelationshipWith(oldOwner), out var modifier))
				modifiers.Add(modifier);

			playerExperience = Util.ApplyPercentageModifiers(playerExperience, modifiers);

			newOwner.PlayerActor.TraitOrDefault<PlayerExperience>()?.GiveExperience(playerExperience);
		}
	}
}
