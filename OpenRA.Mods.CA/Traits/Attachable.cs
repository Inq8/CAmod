#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Use on an actor to make it attachable to other actors with the AttachableTo trait.")]
	public class AttachableInfo : TraitInfo, Requires<IPositionableInfo>
	{
		public override object Create(ActorInitializer init) { return new Attachable(init, this); }
	}

	public class Attachable : INotifyKilled, INotifyActorDisposing
	{
		public readonly AttachableInfo Info;
		AttachableTo attachedTo;
		readonly IPositionable positionable;
		readonly Actor self;

		public Attachable(ActorInitializer init, AttachableInfo info)
		{
			self = init.Self;
			Info = info;
			positionable = self.TraitOrDefault<IPositionable>();
		}

		public bool IsValid { get { return self != null && !self.IsDead; } }

		public void AttachTo(AttachableTo attachableTo, WPos pos)
		{
			Detach();
			attachedTo = attachableTo;
			SetPosition(pos);
		}

		public void AttachedToLost()
		{
			self.Dispose();
		}

		public void SetPosition(WPos pos)
		{
			positionable.SetPosition(self, pos);
			positionable.SetVisualPosition(self, pos);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			Detach();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			Detach();
		}

		void Detach()
		{
			if (attachedTo != null)
				attachedTo.Detach(this);
		}
	}
}
