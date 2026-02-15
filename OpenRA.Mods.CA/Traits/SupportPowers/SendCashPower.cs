#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Transfers money to the owner of the targeted actor.")]
	public class SendCashPowerInfo : SupportPowerInfo
	{
		[Desc("Amount of money to send. Will send less if the player cannot afford the full amount.")]
		public readonly int Amount = 1000;

		[Desc("Percentage of amount sent to be taxed away.")]
		public readonly int TaxPercentage = 0;

		[Desc("If true, target player must have available capacity to receive the money.")]
		public readonly bool RequireTargetCapacity = false;

		[Desc("The `TargetTypes` from `Targetable` that can be targeted.")]
		public readonly BitSet<TargetableType> ValidTargets = default;

		[Desc("Target types that cannot be targeted.")]
		public readonly BitSet<TargetableType> InvalidTargets = default;

		[Desc("Player relationships which can receive money.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		[Desc("Sound to play when sending money.")]
		public readonly string OnFireSound = null;

		[Desc("If true, target player cannot have more money than the sender.")]
		public readonly bool TargetMustBePoorer = false;

		[NotificationReference("Speech")]
		[Desc("Speech notification to play when the player does not have any funds.")]
		public readonly string InsufficientFundsNotification = null;

		[FluentReference(optional: true)]
		[Desc("Text notification to display when the player does not have any funds.")]
		public readonly string InsufficientFundsTextNotification = null;

		public override object Create(ActorInitializer init) { return new SendCashPower(init.Self, this); }
	}

	public class SendCashPower : SupportPower
	{
		readonly SendCashPowerInfo info;

		public SendCashPower(Actor self, SendCashPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.OrderGenerator = new SelectSendCashTarget(self.World, order, manager, this);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			var target = GetTargetActor(self.World.Map.CellContaining(order.Target.CenterPosition));
			if (target == null)
				return;

			var playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();

			// first constrain the amount to how much the player can afford to send
			var amountToSend = Math.Min(info.Amount, playerResources.GetCashAndResources());

			if (amountToSend > 0)
			{
				var targetResources = target.Owner.PlayerActor.Trait<PlayerResources>();
				var localPlayer = self.World.LocalPlayer;

				// if the target must be poorer, further constrain the amount to send based on the difference between sender and target funds
				if (info.TargetMustBePoorer)
				{
					amountToSend = Math.Max(0, Math.Min(amountToSend, playerResources.GetCashAndResources() - targetResources.GetCashAndResources()));

					if (amountToSend <= 0)
					{
						var message = $"Cannot send to players with more resources.";
						if (localPlayer != null && localPlayer == self.Owner)
							TextNotificationsManager.AddChatLine(self.Owner.ClientIndex, "[Team] " + self.Owner.ResolvedPlayerName, message, self.Owner.Color);
						return;
					}
				}

				if (info.RequireTargetCapacity)
				{
					var capacityAvailable = targetResources.ResourceCapacity - targetResources.Resources;

					if (capacityAvailable <= 0)
					{
						var message = $"{target.Owner.ResolvedPlayerName} has no storage capacity remaining.";
						if (localPlayer != null && localPlayer == self.Owner)
							TextNotificationsManager.AddChatLine(self.Owner.ClientIndex, "[Team] " + self.Owner.ResolvedPlayerName, message, self.Owner.Color);
						return;
					}

					amountToSend = Math.Min(amountToSend, capacityAvailable);
				}

				PlayLaunchSounds();
				Game.Sound.Play(SoundType.World, info.OnFireSound, order.Target.CenterPosition);

				playerResources.TakeCash(amountToSend, true);
				var taxAmount = 0;

				if (info.TaxPercentage > 0)
				{
					taxAmount = amountToSend * info.TaxPercentage / 100;
					amountToSend -= taxAmount;
					amountToSend = Math.Max(1, amountToSend);
				}

				if (info.RequireTargetCapacity)
					targetResources.GiveResources(amountToSend);
				else
					targetResources.GiveCash(amountToSend);

				self.World.AddFrameEndTask(w =>
				{
					w.Add(new FloatingText(target.CenterPosition, target.OwnerColor(), FloatingText.FormatCashTick(amountToSend), 30));
					w.Add(new FlashTarget(target, Color.Yellow));
				});

				if (localPlayer != null && localPlayer.IsAlliedWith(self.Owner))
				{
					var message = $"Sent ${amountToSend} to {target.Owner.ResolvedPlayerName}";

					if (info.TaxPercentage > 0)
						message += $" (${amountToSend + taxAmount} - ${taxAmount} tax)";

					TextNotificationsManager.AddChatLine(self.Owner.ClientIndex, "[Team] " + self.Owner.ResolvedPlayerName, message, self.Owner.Color);
				}

			} else {
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.InsufficientFundsNotification, self.Owner.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(self.Owner, info.InsufficientFundsTextNotification);
			}
		}

		public Actor GetTargetActor(CPos xy)
		{
			var actorsAtCell = Self.World.ActorMap.GetActorsAt(xy);
			return actorsAtCell.FirstOrDefault(a => IsValidTarget(a));
		}

		bool IsValidTarget(Actor a)
		{
			if (a.IsDead || !a.IsInWorld)
				return false;

			if (a.Owner == Self.Owner)
				return false;

			if (!info.ValidRelationships.HasRelationship(Self.Owner.RelationshipWith(a.Owner)))
				return false;

			var enabledTargetTypes = a.GetEnabledTargetTypes();

			if (!info.ValidTargets.IsEmpty && !info.ValidTargets.Overlaps(enabledTargetTypes))
				return false;

			if (!info.InvalidTargets.IsEmpty && info.InvalidTargets.Overlaps(enabledTargetTypes))
				return false;

			return true;
		}

		class SelectSendCashTarget : OrderGenerator
		{
			readonly SendCashPower power;
			readonly SupportPowerManager manager;
			readonly string order;

			public SelectSendCashTarget(World world, string order, SupportPowerManager manager, SendCashPower power)
			{
				// Clear selection if using Left-Click Orders
				if (Game.Settings.Game.UseClassicMouseStyle)
					world.Selection.Clear();

				this.manager = manager;
				this.order = order;
				this.power = power;
			}

			protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
				{
					world.CancelInputMode();
					yield break;
				}

				var target = power.GetTargetActor(cell);
				if (mi.Button == MouseButton.Left && target != null)
				{
					yield return new Order(order, manager.Self, Target.FromCell(world, cell), false) { SuppressVisualFeedback = true };
				}
			}

			protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

			protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var target = power.GetTargetActor(xy);

				if (target != null)
				{
					var decorations = target.TraitsImplementing<ISelectionDecorations>().FirstEnabledTraitOrDefault();
					if (decorations != null)
					{
						foreach (var d in decorations.RenderSelectionAnnotations(target, wr, Color.Yellow))
							yield return d;
					}
				}
			}

			protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }

			protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				return power.GetTargetActor(cell) != null ? power.info.Cursor : power.info.BlockedCursor;
			}
		}
	}
}
