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
		public override object Create(ActorInitializer init) { return new AttachableTo(init, this); }
	}

	public class AttachableTo : INotifyKilled, INotifyActorDisposing, INotifyVisualPositionChanged
	{
		readonly Actor self;
		readonly HashSet<Attachable> attached = new HashSet<Attachable>();

		public AttachableTo(ActorInitializer init, AttachableToInfo info)
		{
			self = init.Self;
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

		public void Attach(Attachable attachable)
		{
			if (attachable.IsValid)
			{
				attached.Add(attachable);
				attachable.AttachTo(this, self.CenterPosition);
			}
		}

		public void Detach(Attachable attachable)
		{
			attached.Remove(attachable);
		}
	}
}
