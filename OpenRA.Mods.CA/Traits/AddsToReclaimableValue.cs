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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("When killed, this actor adds value to the owner's reclaimable value pool.")]
	public class AddsToReclaimableValueInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("The type of reclaimable value pool to add to.")]
		public readonly string Type = null;

		[Desc("Percentage of the killed actor's Cost or CustomSellValue to be added to the pool.")]
		public readonly int ValuePercentage = 100;

		[Desc("DeathTypes for which value should be added.",
			"Use an empty list (the default) to allow all DeathTypes.")]
		public readonly BitSet<DamageType> DeathTypes = default;

		public override object Create(ActorInitializer init) { return new AddsToReclaimableValue(this); }
	}

	public class AddsToReclaimableValue : ConditionalTrait<AddsToReclaimableValueInfo>, INotifyKilled, INotifyCreated, INotifyOwnerChanged
	{
		ReclaimableValueProducer producerTrait;

		public AddsToReclaimableValue(AddsToReclaimableValueInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			base.Created(self);
			producerTrait = self.Owner.PlayerActor.TraitsImplementing<ReclaimableValueProducer>().FirstOrDefault(t => t.Info.Type == Info.Type);
		}

		int GetReclaimableValue(Actor self)
		{
			return self.GetSellValue() * Info.ValuePercentage / 100;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled)
				return;

			if (!Info.DeathTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(Info.DeathTypes))
				return;

			if (producerTrait == null)
				return;

			var value = GetReclaimableValue(self);
			if (value <= 0)
				return;

			producerTrait.AddValue(Info.Type, value);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			producerTrait = newOwner.PlayerActor.TraitsImplementing<ReclaimableValueProducer>().FirstOrDefault(t => t.Info.Type == Info.Type);
		}
	}
}