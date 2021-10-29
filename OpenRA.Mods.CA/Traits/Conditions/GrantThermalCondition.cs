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

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Gives a condition to the actor that heats up when enabled,",
		"cools down when paused, and is revoked at maximum temp or disabled.")]
	public class GrantThermalConditionInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition type to grant.")]
		public readonly string Condition = null;

		[Desc("Initial temperature.")]
		public readonly int InitialTemp = 0;

		[Desc("Maximum temperature.")]
		public readonly int MaxTemp = 100;

		[Desc("Maximum temperature at which reactivation can occur (will always cool down to this).")]
		public readonly int MaxReactivationTemp = 50;

		[Desc("Temperature gained per tick when trait is enabled.")]
		public readonly int HeatingRate = 1;

		[Desc("Temperature lost per tick when trait is paused.")]
		public readonly int CoolingRate = 1;

		[Desc("Delay in ticks before heating/cooling after changing state.")]
		public readonly int TransitionDelay = 0;

		public readonly bool InvertSelectionBar = false;
		public readonly bool ShowSelectionBar = true;
		public readonly bool ShowSelectionBarWhenEmpty = true;

		public readonly Color LowHeatColor = Color.GreenYellow;
		public readonly Color MediumHeatColor = Color.Yellow;
		public readonly Color HighHeatColor = Color.Orange;
		public readonly Color CriticalHeatColor = Color.Red;
		public readonly Color CoolingColour = Color.Blue;

		public override object Create(ActorInitializer init) { return new GrantThermalCondition(init, this); }
	}

	public class GrantThermalCondition : PausableConditionalTrait<GrantThermalConditionInfo>, INotifyCreated, ITick, ISelectionBar
	{
		int token = Actor.InvalidConditionToken;
		int delay;
		bool forceCooling;

		[Sync]
		int temp;

		public GrantThermalCondition(ActorInitializer init, GrantThermalConditionInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			temp = Info.InitialTemp;
			delay = Info.TransitionDelay;

			if (temp < Info.MaxTemp && temp <= Info.MaxReactivationTemp)
				GrantCondition(self);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (IsTraitPaused || (temp > Info.MaxReactivationTemp))
			{
				if (delay > 0 && --delay > 0)
					return;

				if (temp == 0)
					return;

				temp -= Info.CoolingRate;

				if (temp < 0)
					temp = 0;

				if (forceCooling && temp > Info.MaxReactivationTemp)
				{
					RevokeCondition(self);
					return;
				}

				GrantCondition(self);
			}
			else
			{
				if (temp == Info.MaxTemp)
					return;

				if (delay > 0 && --delay > 0)
					return;

				temp += Info.HeatingRate;

				if (temp > Info.MaxTemp)
					temp = Info.MaxTemp;

				if (temp >= Info.MaxTemp)
					RevokeCondition(self);
			}
		}

		void GrantCondition(Actor self)
		{
			forceCooling = false;
			if (token == Actor.InvalidConditionToken)
				token = self.GrantCondition(Info.Condition);
		}

		void RevokeCondition(Actor self)
		{
			forceCooling = true;

			if (token == Actor.InvalidConditionToken)
				return;

			token = self.RevokeCondition(token);
		}

		protected override void TraitDisabled(Actor self)
		{
			temp = 0;
			delay = Info.TransitionDelay;
			RevokeCondition(self);
		}

		protected override void TraitPaused(Actor self)
		{
			delay = Info.TransitionDelay;
			forceCooling = true;
		}

		protected override void TraitResumed(Actor self)
		{
			delay = Info.TransitionDelay;
		}

		float Temperature { get { return (float)temp / Info.MaxTemp; } }

		float ISelectionBar.GetValue()
		{
			if (!Info.ShowSelectionBar || IsTraitDisabled)
				return 0f;

			return Temperature;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return Info.ShowSelectionBarWhenEmpty; } }

		Color ISelectionBar.GetColor()
		{
			if (Temperature > 0.75)
				return Info.CriticalHeatColor;
			else if (Temperature > 0.5)
				return Info.HighHeatColor;
			else if (Temperature > 0.25)
				return Info.MediumHeatColor;

			return Info.LowHeatColor;
		}
	}
}
