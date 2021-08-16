#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Allows actor to have actors with Attachable trait attached to it.")]
	public class AttachableToInfo : TraitInfo
	{
		[Desc("Limit how many specific actors can be attached.")]
		public readonly Dictionary<string, int> Limits = new Dictionary<string, int>();

		[Desc("Limit how many specific actors can be attached.")]
		public readonly Dictionary<string, string> LimitConditions = new Dictionary<string, string>();

		public override object Create(ActorInitializer init) { return new AttachableTo(init, this); }
	}

	public class AttachableTo : INotifyKilled, INotifyActorDisposing, INotifyVisualPositionChanged
	{
		public readonly AttachableToInfo Info;
		readonly Actor self;
		readonly HashSet<Attachable> attached = new HashSet<Attachable>();
		Dictionary<string, int> attachedCounts = new Dictionary<string, int>();
		Dictionary<string, int> limitTokens = new Dictionary<string, int>();

		public AttachableTo(ActorInitializer init, AttachableToInfo info)
		{
			Info = info;
			self = init.Self;

			foreach (var type in Info.Limits)
			{
				attachedCounts[type.Key] = 0;
				limitTokens[type.Key] = Actor.InvalidConditionToken;
			}
		}

		void INotifyVisualPositionChanged.VisualPositionChanged(Actor self, byte oldLayer, byte newLayer)
		{
			if (!self.IsInWorld)
				return;

			var pos = self.CenterPosition;

			foreach (var attachable in attached)
			{
				if (attachable.IsValid)
					attachable.SetPosition(pos);
			}
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			Terminate();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			Terminate();
		}

		void Terminate()
		{
			foreach (var attachable in attached)
			{
				if (attachable.IsValid)
					attachable.AttachedToLost();
			}
		}

		public bool Attach(Attachable attachable)
		{
			if (!attachable.IsValid)
				return false;

			if (
				attachedCounts.ContainsKey(attachable.Info.AttachableType)
				&& Info.LimitConditions.ContainsKey(attachable.Info.AttachableType)
				&& attachedCounts[attachable.Info.AttachableType] >= Info.Limits[attachable.Info.AttachableType]
			)
				return false;

			attached.Add(attachable);
			attachable.AttachTo(this, self.CenterPosition);

			if (attachedCounts.ContainsKey(attachable.Info.AttachableType))
			{
				attachedCounts[attachable.Info.AttachableType]++;

				if (
					Info.LimitConditions.ContainsKey(attachable.Info.AttachableType)
					&& attachedCounts[attachable.Info.AttachableType] >= Info.Limits[attachable.Info.AttachableType]
					&& limitTokens[attachable.Info.AttachableType] == Actor.InvalidConditionToken
				)
					limitTokens[attachable.Info.AttachableType] = self.GrantCondition(Info.LimitConditions[attachable.Info.AttachableType]);
			}

			return true;
		}

		public void Detach(Attachable attachable)
		{
			attached.Remove(attachable);

			if (attachedCounts.ContainsKey(attachable.Info.AttachableType))
				attachedCounts[attachable.Info.AttachableType]--;

				if (
					Info.LimitConditions.ContainsKey(attachable.Info.AttachableType)
					&& attachedCounts[attachable.Info.AttachableType] < Info.Limits[attachable.Info.AttachableType]
					&& limitTokens[attachable.Info.AttachableType] != Actor.InvalidConditionToken
				)
					limitTokens[attachable.Info.AttachableType] = self.RevokeCondition(limitTokens[attachable.Info.AttachableType]);
		}
	}
}
