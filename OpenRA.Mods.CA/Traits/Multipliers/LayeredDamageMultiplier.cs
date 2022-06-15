#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Gives layered damage reduction to the actor. Layers are removed each time damage is taken.")]
	public class LayeredDamageMultiplierInfo : ConditionalTraitInfo
	{
		[Desc("Layers of damage reduction.")]
		public readonly int[] Layers = { 25, 50, 75 };

		[Desc("Number of ticks to restore a layer.")]
		public readonly int LayerRestoreTime = 50;

		[Desc("Minimum damage to trigger the timed damage modifier.")]
		public readonly int MinimumDamage = 100;

		[Desc("If true, restoration will be reset on taking any damage.")]
		public readonly bool ResetRegenOnDamage = false;

		[Desc("Damage type(s) that are affected by the damage multiplier and consume layers.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		public override object Create(ActorInitializer init) { return new LayeredDamageMultiplier(this); }
	}

	public class LayeredDamageMultiplier : ConditionalTrait<LayeredDamageMultiplierInfo>, ITick, IDamageModifier, INotifyDamage
	{
		public new readonly LayeredDamageMultiplierInfo Info;
		readonly int maxLayers;
		int layersRemaining;
		int ticks;

		public LayeredDamageMultiplier(LayeredDamageMultiplierInfo info)
			: base(info)
		{
			Info = info;
			maxLayers = layersRemaining = Info.Layers.Count();
		}

		int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage)
		{
			if (IsTraitDisabled || layersRemaining == 0 || damage.Value < Info.MinimumDamage)
				return 100;

			var validDamageType = Info.DamageTypes.IsEmpty || damage.DamageTypes.Overlaps(Info.DamageTypes);
			var currentLayer = maxLayers - layersRemaining;
			return validDamageType ? Info.Layers[currentLayer] : 100;
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled)
				return;

			if (Info.ResetRegenOnDamage && layersRemaining < maxLayers && e.Damage.Value > 0)
				ticks = Info.LayerRestoreTime;

			if (layersRemaining == 0 || e.Damage.Value < Info.MinimumDamage)
				return;

			if (!Info.DamageTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(Info.DamageTypes))
				return;

			layersRemaining--;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (layersRemaining == maxLayers)
				return;

			if (--ticks > 0)
				return;

			layersRemaining++;
			ticks = Info.LayerRestoreTime;
		}

		protected override void TraitEnabled(Actor self)
		{
			layersRemaining = maxLayers;
			ticks = Info.LayerRestoreTime;
		}
	}
}
