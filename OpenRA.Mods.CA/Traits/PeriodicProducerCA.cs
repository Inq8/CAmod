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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Produces an actor without using the standard production queue.")]
	public class PeriodicProducerCAInfo : PausableConditionalTraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actors to produce.")]
		public readonly string[] Actors = null;

		[FieldLoader.Require]
		[Desc("Production queue type to use")]
		public readonly string Type = null;

		[Desc("Notification played when production is activated.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string ReadyAudio = null;

		[Desc("Notification played when the exit is jammed.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string BlockedAudio = null;

		[Desc("Duration between productions.")]
		public readonly int ChargeDuration = 1000;

		[Desc("Immediately produces before initial charge.")]
		public readonly bool Immediate = false;

		public readonly bool ResetTraitOnEnable = false;
		public readonly bool ResetTraitOnOwnerChange = false;

		public readonly bool ShowSelectionBar = false;
		public readonly Color ChargeColor = Color.DarkOrange;

		[Desc("Defines to which players the bar is to be shown.")]
		public readonly PlayerRelationship SelectionBarDisplayRelationships = PlayerRelationship.Ally;

		public override object Create(ActorInitializer init) { return new PeriodicProducerCA(init, this); }
	}

	public class PeriodicProducerCA : PausableConditionalTrait<PeriodicProducerCAInfo>, ISelectionBar, ITick, ISync, INotifyOwnerChanged
	{
		readonly PeriodicProducerCAInfo info;
		Actor self;

		[Sync]
		int ticks;

		public PeriodicProducerCA(ActorInitializer init, PeriodicProducerCAInfo info)
			: base(info)
		{
			this.info = info;
			self = init.Self;
			ticks = info.Immediate ? 0 : info.ChargeDuration;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (--ticks < 0)
			{
				var sp = self.TraitsImplementing<Production>()
				.FirstOrDefault(p => !p.IsTraitDisabled && !p.IsTraitPaused && p.Info.Produces.Contains(info.Type));

				var activated = false;

				if (sp != null)
				{
					foreach (var name in info.Actors)
					{
						var inits = new TypeDictionary
						{
							new OwnerInit(self.Owner),
							new FactionInit(sp.Faction)
						};

						activated |= sp.Produce(self, self.World.Map.Rules.Actors[name.ToLowerInvariant()], info.Type, inits, 0);
					}
				}

				if (activated)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio, self.Owner.Faction.InternalName);
				else
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.BlockedAudio, self.Owner.Faction.InternalName);

				ticks = info.ChargeDuration;
			}
		}

		protected override void TraitEnabled(Actor self)
		{
			if (info.ResetTraitOnEnable)
				ticks = info.ChargeDuration;
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			ticks = info.Immediate ? 0 : info.ChargeDuration;
		}

		float ISelectionBar.GetValue()
		{
			if (!info.ShowSelectionBar || IsTraitDisabled)
				return 0f;

			var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
			if (viewer != null && !Info.SelectionBarDisplayRelationships.HasStance(self.Owner.RelationshipWith(viewer)))
				return 0f;

			return (float)(info.ChargeDuration - ticks) / info.ChargeDuration;
		}

		Color ISelectionBar.GetColor()
		{
			return info.ChargeColor;
		}

		bool ISelectionBar.DisplayWhenEmpty
		{
			get
			{
				if (!info.ShowSelectionBar || IsTraitDisabled)
					return false;

				var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
				if (viewer != null && !Info.SelectionBarDisplayRelationships.HasStance(self.Owner.RelationshipWith(viewer)))
					return false;

				return true;
			}
		}
	}
}
