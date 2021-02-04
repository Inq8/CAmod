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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class ProvidesDelayedPrerequisiteInfo : ConditionalTraitInfo, ITechTreePrerequisiteInfo
	{
		[Desc("The prerequisite type that this provides. If left empty it defaults to the actor's name.")]
		public readonly string Prerequisite = null;

		[Desc("Number of ticks to wait before granting prerequisite.")]
		public readonly int Delay = 250;

		[Desc("Only grant this prerequisite when you have these prerequisites.")]
		public readonly string[] RequiresPrerequisites = { };

		[Desc("Only grant this prerequisite for certain factions.")]
		public readonly HashSet<string> Factions = new HashSet<string>();

		[Desc("Should it recheck everything when it is captured?")]
		public readonly bool ResetOnOwnerChange = false;

		[Desc("If true, unmet conditions will disable the prerequisite entirely, otherwise the timer will be paused.")]
		public readonly bool FullDisable = false;

		public readonly bool ShowSelectionBar = true;
		public readonly Color SelectionBarColor = Color.FromArgb(200, 200, 200);

		[NotificationReference("Speech")]
		[Desc("Sound played when prerequisite is granted.")]
		public readonly string Notification = null;

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info)
		{
			return new string[] { Prerequisite ?? info.Name };
		}

		public override object Create(ActorInitializer init) { return new ProvidesDelayedPrerequisite(init, this); }
	}

	public class ProvidesDelayedPrerequisite : ConditionalTrait<ProvidesDelayedPrerequisiteInfo>, ITick, ITechTreePrerequisite, INotifyOwnerChanged, INotifyCreated, ISelectionBar
	{
		readonly string prerequisite;

		[Sync]
		int remainingDelay;

		bool enabled;
		TechTree techTree;
		string faction;

		public ProvidesDelayedPrerequisite(ActorInitializer init, ProvidesDelayedPrerequisiteInfo info)
			: base(info)
		{
			prerequisite = info.Prerequisite;

			if (string.IsNullOrEmpty(prerequisite))
				prerequisite = init.Self.Info.Name;

			faction = init.GetValue<FactionInit, string>(init.Self.Owner.Faction.InternalName);
		}

		public IEnumerable<string> ProvidesPrerequisites
		{
			get
			{
				if (!enabled)
					yield break;

				yield return prerequisite;
			}
		}

		protected override void Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query other player traits from self, knowing that
			// it refers to the same actor as self.Owner.PlayerActor
			var playerActor = self.Info.Name == "player" ? self : self.Owner.PlayerActor;

			techTree = playerActor.Trait<TechTree>();

			base.Created(self);

			self.World.AddFrameEndTask(w =>
			{
				Reset(self);
			});
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (--remainingDelay == 0)
			{
				Update();
				if (enabled)
					techTree.ActorChanged(self);
				if (Info.Notification != null && enabled)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.Notification, self.Owner.Faction.InternalName);
			}
		}

		void Reset(Actor self)
		{
			enabled = false;
			remainingDelay = Info.Delay;
			techTree.ActorChanged(self);
		}

		void Update()
		{
			if (IsTraitDisabled && Info.FullDisable)
			{
				enabled = false;
				return;
			}

			enabled = remainingDelay <= 0;

			if (Info.Factions.Any())
				enabled = Info.Factions.Contains(faction);

			if (Info.RequiresPrerequisites.Any() && enabled)
				enabled = techTree.HasPrerequisites(Info.RequiresPrerequisites);
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			techTree = newOwner.PlayerActor.Trait<TechTree>();

			if (Info.ResetOnOwnerChange)
			{
				faction = newOwner.Faction.InternalName;
				Reset(self);
			}
		}

		protected override void TraitEnabled(Actor self)
		{
			Update();
			techTree.ActorChanged(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			Update();
			techTree.ActorChanged(self);
		}

		float ISelectionBar.GetValue()
		{
			if (!Info.ShowSelectionBar || remainingDelay <= 0)
				return 0;

			var maxTicks = Info.Delay;

			if (remainingDelay == maxTicks)
				return 0;

			return (float)(maxTicks - remainingDelay) / maxTicks;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }
	}
}
