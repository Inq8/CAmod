#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.CA.Orders;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets
{
	public class UpgradeOrderButtonLogic : ChromeLogic
	{
		readonly World world;

		int selectionHash;
		int upgradesHash;

		TraitPair<Upgradeable>[] upgradeables = Array.Empty<TraitPair<Upgradeable>>();
		TraitPair<Upgradeable>[] cancellables = Array.Empty<TraitPair<Upgradeable>>();
		UpgradesManager upgradesManager;

		[ObjectCreator.UseCtor]
		public UpgradeOrderButtonLogic(Widget widget, World world)
		{
			this.world = world;

			upgradesManager = world.LocalPlayer.PlayerActor.Trait<UpgradesManager>();

			if (widget is ButtonWidget upgrade)
				BindUpgradeButton(upgrade);
		}

		void BindUpgradeButton(ButtonWidget button)
		{
			button.Get<ImageWidget>("ICON").GetImageName = () => GetButtonIcon();

			button.IsVisible = () => true;

			button.IsDisabled = () => { UpdateStateIfSelectionChanged(); return false; };

			button.IsHighlighted = () => world.OrderGenerator is UpgradeOrderGenerator;

			button.OnMouseUp = me => DoUpgrade(me.Modifiers.HasModifier(Modifiers.Shift));

			button.OnKeyPress = e => DoUpgrade(e.Modifiers.HasModifier(Modifiers.Shift));
		}

		string GetButtonIcon()
		{
			return cancellables.Length > 0 ? "cancel" : (world.OrderGenerator is UpgradeOrderGenerator ? "upgrade-active" : "upgrade");
		}

		void UpdateStateIfSelectionChanged()
		{
			if (selectionHash == world.Selection.Hash && upgradesHash == upgradesManager.Hash)
				return;

			var upgradeablesAndCancellables = world.Selection.Actors
				.Where(a => a.Owner == world.LocalPlayer && a.IsInWorld)
				.SelectMany(a => a.TraitsImplementing<Upgradeable>()
					.Where(t => t.CanUpgrade || t.CanCancelUpgrade)
					.Select(at => new TraitPair<Upgradeable>(a, at)));

			/* Limit to one upgradeable per actor. */
			upgradeables = upgradeablesAndCancellables.Where(t => t.Trait.CanUpgrade).GroupBy(t => t.Actor).Select(t => t.First()).ToArray();
			cancellables = upgradeablesAndCancellables.Where(t => t.Trait.CanCancelUpgrade).GroupBy(t => t.Actor).Select(t => t.First()).ToArray();
			selectionHash = world.Selection.Hash;
			upgradesHash = upgradesManager.Hash;
		}

		void DoUpgrade(bool queued)
		{
			/* If one or more selected units are upgrading, cancel them. */
			if (cancellables.Length > 0)
			{
				foreach (var u in upgradeables)
					world.IssueOrder(new Order("CancelUpgrade", u.Actor, queued) { TargetString = u.Trait.Info.Type });

				return;
			}

			/* If one or more selected units can be upgraded, issue order for them to upgrade. */
			if (upgradeables.Length > 0)
			{
				foreach (var u in upgradeables)
					world.IssueOrder(new Order("Upgrade", u.Actor, Target.FromActor(u.Actor), queued) { TargetString = u.Trait.Info.Type });
				return;
			}

			/* If nothing is selected, or none of the selected units can be upgraded, toggle upgrade mode. */
			world.ToggleInputMode<UpgradeOrderGenerator>();
		}
	}
}
