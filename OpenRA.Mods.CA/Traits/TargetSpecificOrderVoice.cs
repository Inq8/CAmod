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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Lists valid factions for ProvidesPrerequisiteValidatedFaction.")]
	public class TargetSpecificOrderVoiceInfo : TraitInfo
	{
		[Desc("Order strings which trigger the custom voice line.")]
		public readonly HashSet<string> Orders = new();

		[Desc("The `TargetTypes` from `Targetable` with their corresponding voice lines.")]
		public readonly Dictionary<string, string> TargetTypeVoices = default;

		[VoiceReference]
		[Desc("Voice line to use if no target type is matched.")]
		public readonly string DefaultVoice = null;

		public override object Create(ActorInitializer init) { return new TargetSpecificOrderVoice(init, this); }
	}

	public class TargetSpecificOrderVoice : IOrderVoice
	{
		public readonly TargetSpecificOrderVoiceInfo Info;

		public TargetSpecificOrderVoice(ActorInitializer init, TargetSpecificOrderVoiceInfo info)
		{
			Info = info;
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString == null || !Info.Orders.Contains(order.OrderString))
				return null;

			if (order.Target.Type == TargetType.Invalid || order.Target.Type == TargetType.Terrain)
				return Info.DefaultVoice;

			var enabledTargetTypes = order.Target.Actor.GetEnabledTargetTypes();
			var matchingTargetType = enabledTargetTypes.FirstOrDefault(t => Info.TargetTypeVoices.ContainsKey(t));

			if (matchingTargetType != null)
				return Info.TargetTypeVoices[matchingTargetType];

			return Info.DefaultVoice;
		}
	}
}
