#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

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

		[Desc("The `TargetTypes` from `Targetable` that can be targeted.")]
		public readonly BitSet<TargetableType> ValidTargets = default;

		[Desc("Target types that cannot be targeted.")]
		public readonly BitSet<TargetableType> InvalidTargets = default;

		[Desc("Player relationships which can receive money.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		[Desc("Sound to play when sending money.")]
		public readonly string OnFireSound = null;

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

			PlayLaunchSounds();
			Game.Sound.Play(SoundType.World, info.OnFireSound, order.Target.CenterPosition);

			var playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			var amountToSend = System.Math.Min(info.Amount, playerResources.Cash + playerResources.Resources);

			if (amountToSend > 0)
			{
				playerResources.TakeCash(amountToSend, true);

				var targetResources = target.Owner.PlayerActor.Trait<PlayerResources>();
				targetResources.GiveCash(amountToSend);

				self.World.AddFrameEndTask(w =>
				{
					w.Add(new FloatingText(target.CenterPosition, target.OwnerColor(), FloatingText.FormatCashTick(amountToSend), 30));
					w.Add(new FlashTarget(target, Color.Yellow));
				});

				TextNotificationsManager.AddTransientLine(self.Owner, $"Sent ${amountToSend} to {target.Owner.ResolvedPlayerName}");
				TextNotificationsManager.AddTransientLine(target.Owner, $"Received ${amountToSend} from {self.Owner.ResolvedPlayerName}");
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
