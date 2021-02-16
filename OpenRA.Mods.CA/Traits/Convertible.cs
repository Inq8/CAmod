#region Copyright & License Information
/*
 * Copyright 2016-2021 The CA Developers (see AUTHORS)
 * This file is part of CA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.CA.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.UnitConverter
{
	[Desc("This Actor can be converted into another actor (or actors) through the UnitConverter trait.")]
	class ConvertibleInfo : TraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actors to produce.")]
		public readonly string[] SpawnActors = null;

		[Desc("Cursor used for order.")]
		public readonly string Cursor = "enter";

		[Desc("Voice used when ordering to enter.")]
		[VoiceReference]
		public readonly string Voice = "Action";

		public override object Create(ActorInitializer init) { return new Convertible(init, this); }
	}

	class Convertible : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public readonly ConvertibleInfo Info;

		public Convertible(ActorInitializer init, ConvertibleInfo info)
		{
			Info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new ConvertibleOrderTargeter(Info.Cursor); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			return order.OrderID == ConvertibleOrderTargeter.Id ? new Order(order.OrderID, self, target, queued) : null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != ConvertibleOrderTargeter.Id)
				return null;

			return Info.Voice;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != ConvertibleOrderTargeter.Id)
				return;

			if (order.Target.Type != TargetType.Actor || order.Target.Actor == null)
				return;

			self.CancelActivity();
			self.QueueActivity(new ConvertActor(self, order.Target, Color.Yellow));
		}
	}
}
