#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	class AlliedInfluenceIndicatorLogic : ChromeLogic
	{
		[FluentReference("level")]
		const string PlayerInfluenceLevel = "label-player-influence-level";

		[FluentReference("time")]
		const string PlayerInfluenceLevelTime = "label-player-influence-level-time";

		[FluentReference("coalition")]
		const string ChosenCoalition = "label-player-influence-coalition";

		[FluentReference("policy")]
		const string ChosenPolicy = "label-player-influence-policy";

		const string NoneImage = "none";
		const string DisabledImage = "disabled";

		ProvidesPrerequisitesOnTimeline timeline;

		string chosenCoalition;
		string chosenPolicy;

		private readonly UpgradesManager upgradesManager;
		private readonly AlliedInfluenceMeterWidget influenceMeter;

		[ObjectCreator.UseCtor]
		public AlliedInfluenceIndicatorLogic(Widget widget, World world)
		{
			timeline = world.LocalPlayer.PlayerActor.TraitsImplementing<ProvidesPrerequisitesOnTimeline>()
				.FirstOrDefault(c => c.Info.Type == "AlliedInfluence");

			var container = widget.Get<ContainerWidget>("ALLIED_INFLUENCE");
			var coalitionImage = container.Get<ImageWidget>("ALLIED_COALITION_IMAGE");
			var noCoalitionImage = container.Get<ImageWidget>("ALLIED_NO_COALITION_IMAGE");
			influenceMeter = container.Get<AlliedInfluenceMeterWidget>("ALLIED_INFLUENCE_METER");
			noCoalitionImage.IsVisible = () => false;

			// influence meter is only shown if player is an allied faction
			if (world.LocalPlayer.Faction.Side != "Allies")
			{
				coalitionImage.GetImageName = () => DisabledImage;
				coalitionImage.IsVisible = () => false;
				influenceMeter.IsVisible = () => false;
				return;
			}

			upgradesManager = world.LocalPlayer.PlayerActor.Trait<UpgradesManager>();
			upgradesManager.UpgradeCompleted += HandleUpgradeCompleted;

			if (timeline != null)
			{
				influenceMeter.Thresholds = timeline.Thresholds;
				influenceMeter.MaxTicks = timeline.MaxTicks;

				var influenceMeterTooltipTextCached = new CachedTransform<string, string>((timeCoalitionPolicy) =>
				{
					var thresholdsPassed = timeline.ThresholdsPassed;

					var tooltip = FluentProvider.GetMessage(PlayerInfluenceLevel, "level", thresholdsPassed);

					if (timeline.TicksUntilNextThreshold > 0)
						tooltip += "\n" + FluentProvider.GetMessage(PlayerInfluenceLevelTime, "time", WidgetUtils.FormatTime(timeline.TicksUntilNextThreshold, world.Timestep));

					if (chosenCoalition != null)
						tooltip += "\n" + FluentProvider.GetMessage(ChosenCoalition, "coalition", char.ToUpper(chosenCoalition[0]) + chosenCoalition[1..]);

					if (chosenPolicy != null)
						tooltip += "\n" + FluentProvider.GetMessage(ChosenPolicy,"policy", char.ToUpper(chosenPolicy[0]) + chosenPolicy[1..]);

					return tooltip;
				});

				influenceMeter.GetTooltipText = () =>
				{
					var timeCoalitionPolicy = $"{(timeline.TicksUntilNextThreshold / 25).ToString()}-{chosenCoalition}-{chosenPolicy}";
					return influenceMeterTooltipTextCached.Update(timeCoalitionPolicy);
				};

				timeline.TicksChanged += HandleTicksChanged;

				coalitionImage.GetImageName = () =>
				{
					if (timeline.TicksElapsed >= timeline.MaxTicks)
						return chosenCoalition ?? NoneImage;

					return DisabledImage;
				};

				coalitionImage.GetTooltipText = () =>
				{
					var timeCoalitionPolicy = $"{(timeline.TicksUntilNextThreshold / 25).ToString()}-{chosenCoalition}-{chosenPolicy}";
					return influenceMeterTooltipTextCached.Update(timeCoalitionPolicy);
				};
			}
			else
			{
				coalitionImage.GetImageName = () => NoneImage;
				influenceMeter.IsVisible = () => false;
				coalitionImage.IsVisible = () => false;
				noCoalitionImage.IsVisible = () => true;
			}
		}

		private void HandleUpgradeCompleted(string upgradeName)
		{
			if (upgradeName.EndsWith(".coalition"))
				chosenCoalition = upgradeName.Split('.')[0];
			else if (upgradeName.EndsWith(".policy"))
				chosenPolicy = upgradeName.Split('.')[0];

			if (chosenCoalition != null && chosenPolicy != null)
				upgradesManager.UpgradeCompleted -= HandleUpgradeCompleted;
		}

		private void HandleTicksChanged(int ticks)
		{
			influenceMeter.CurrentTicks = ticks;

			if (ticks >= timeline.MaxTicks)
			{
				timeline.TicksChanged -= HandleTicksChanged;
				influenceMeter.IsVisible = () => false;
			}
		}
	}
}
