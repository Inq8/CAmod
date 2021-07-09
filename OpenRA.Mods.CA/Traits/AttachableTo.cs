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
		readonly HashSet<Actor> attachedActors = new HashSet<Actor>();

		public AttachableTo(ActorInitializer init, AttachableToInfo info)
		{
			self = init.Self;
		}

		void INotifyVisualPositionChanged.VisualPositionChanged(Actor self, byte oldLayer, byte newLayer)
		{
			if (!self.IsInWorld)
				return;

			var pos = self.Location;

			foreach (var actor in attachedActors)
			{
				var attachableTrait = actor.TraitOrDefault<Attachable>();
				if (attachableTrait != null && attachableTrait.IsValid)
					attachableTrait.OnTargetMoved(pos);
			}
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			KillAttached();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			KillAttached();
		}

		void KillAttached()
		{
			foreach (var actor in attachedActors)
			{
				if (actor == null || actor.IsDead)
					continue;

				var attachableTrait = actor.TraitOrDefault<Attachable>();
				if (attachableTrait != null && attachableTrait.IsValid)
					attachableTrait.OnTargetLost();
			}
		}

		public void Attach(Actor actor)
		{
			var attachableTrait = actor.TraitOrDefault<Attachable>();
			if (attachableTrait != null && attachableTrait.IsValid)
			{
				attachedActors.Add(actor);
				attachableTrait.SetTarget(this);
			}
		}

		public void Detach(Actor actor)
		{
			attachedActors.Remove(actor);
		}
	}
}
