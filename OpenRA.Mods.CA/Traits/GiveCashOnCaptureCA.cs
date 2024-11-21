#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Lets the actor grant cash when captured.")]
	public class GivesCashOnCaptureCAInfo : ConditionalTraitInfo
	{
		[Desc("Whether to show the cash tick indicators rising from the actor.")]
		public readonly bool ShowTicks = true;

		[Desc("How long to show the Amount tick indicator when enabled.")]
		public readonly int DisplayDuration = 30;

		[Desc("Amount of money awarded for capturing the actor.")]
		public readonly int Amount = 0;

		[Desc("Award cash only if the capturer's CaptureTypes overlap with these types. Leave empty to allow all types.")]
		public readonly BitSet<CaptureType> CaptureTypes = default;

		[Desc("If set, to get the value remove this suffix and use the actor with the name that remains.")]
		public readonly string SuffixToRemoveForValueActor = null;

		[Desc("Value actor percentage (if actor is used for value).")]
		public readonly int ValueActorPercentage = 100;

		public override object Create(ActorInitializer init) { return new GivesCashOnCaptureCA(init.Self, this); }
	}

	public class GivesCashOnCaptureCA : ConditionalTrait<GivesCashOnCaptureCAInfo>, INotifyCapture
	{
		readonly GivesCashOnCaptureCAInfo info;
		readonly int cashAmount;

		public GivesCashOnCaptureCA(Actor self, GivesCashOnCaptureCAInfo info)
			: base(info)
		{
			this.info = info;
			cashAmount = info.Amount;

			if (info.SuffixToRemoveForValueActor != null)
			{
				var valueActorName = self.Info.Name.Replace(info.SuffixToRemoveForValueActor.ToLowerInvariant(), "").ToLowerInvariant();
				if (self.World.Map.Rules.Actors.ContainsKey(valueActorName))
				{
					var valueActorValued = self.World.Map.Rules.Actors[valueActorName].TraitInfoOrDefault<ValuedInfo>();
					if (valueActorValued != null)
					{
						cashAmount = valueActorValued.Cost * info.ValueActorPercentage / 100;
					}
				}
			}
		}

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner, BitSet<CaptureType> captureTypes)
		{
			if (IsTraitDisabled)
				return;

			if (!info.CaptureTypes.IsEmpty && !info.CaptureTypes.Overlaps(captureTypes))
				return;

			var resources = newOwner.PlayerActor.Trait<PlayerResources>();
			var amount = resources.ChangeCash(cashAmount);
			if (!info.ShowTicks && amount != 0)
				return;

			self.World.AddFrameEndTask(w => w.Add(
				new FloatingText(self.CenterPosition, self.Owner.Color, FloatingText.FormatCashTick(amount), info.DisplayDuration)));
		}
	}
}
