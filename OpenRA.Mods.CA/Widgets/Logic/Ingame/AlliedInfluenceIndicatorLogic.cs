#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

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
		int currentTicks = 0;

		private readonly UpgradesManager upgradesManager;
		private readonly ContainerWidget influenceMeter;
		private readonly CroppableImageWidget influenceMeterFull;
		private readonly ImageWidget influenceLevel;

		[ObjectCreator.UseCtor]
		public AlliedInfluenceIndicatorLogic(Widget widget, World world)
		{
			timeline = world.LocalPlayer.PlayerActor.TraitsImplementing<ProvidesPrerequisitesOnTimeline>()
				.FirstOrDefault(c => c.Info.Type == "AlliedInfluence");

			var container = widget.Get<ContainerWithTooltipWidget>("ALLIED_INFLUENCE");
			var coalitionImage = container.Get<ImageWidget>("ALLIED_COALITION_IMAGE");
			var noCoalitionImage = container.Get<ImageWidget>("ALLIED_NO_COALITION_IMAGE");

			influenceMeter = container.Get<ContainerWidget>("ALLIED_INFLUENCE_METER");
			influenceMeterFull = influenceMeter.Get<CroppableImageWidget>("ALLIED_INFLUENCE_METER_FULL");
			influenceLevel = container.Get<ImageWidget>("ALLIED_INFLUENCE_LEVEL");

			noCoalitionImage.IsVisible = () => false;

			// influence meter is only shown if player is an allied faction
			if (world.LocalPlayer.Faction.Side != "Allies")
			{
				container.IsVisible = () => false;
				return;
			}

			upgradesManager = world.LocalPlayer.PlayerActor.Trait<UpgradesManager>();
			upgradesManager.UpgradeCompleted += HandleUpgradeCompleted;

			if (timeline != null)
			{
				influenceMeterFull.Direction = CroppableImageWidget.CropDirection.BottomUp;
				influenceMeterFull.GetCropPercentage = () =>
				{
					var thresholds = timeline.Thresholds;
					if (thresholds.Length == 0)
						return 0f;

					var currentThreshold = 0;
					var nextThreshold = thresholds.FirstOrDefault(t => t > currentTicks);

					for (var i = thresholds.Length - 1; i >= 0; i--)
					{
						if (thresholds[i] <= currentTicks)
						{
							currentThreshold = thresholds[i];
							break;
						}
					}

					if (nextThreshold == 0)
						return 1f;

					var progressInThreshold = currentTicks - currentThreshold;
					var thresholdSize = nextThreshold - currentThreshold;
					return thresholdSize > 0 ? (float)progressInThreshold / thresholdSize : 0f;
				};

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

				timeline.TicksChanged += HandleTicksChanged;

				influenceLevel.GetImageName = () =>
				{
					if (chosenCoalition != null)
						return "level0";

					return timeline.ThresholdsPassed switch
					{
						0 => "level0",
						1 => "level1",
						2 => "level2",
						3 => "level3",
						_ => "level0",
					};
				};

				coalitionImage.GetImageName = () =>
				{
					if (timeline.TicksElapsed >= timeline.MaxTicks)
						return chosenCoalition ?? NoneImage;

					return DisabledImage;
				};

				container.GetTooltipText = () =>
				{
					var timeCoalitionPolicy = $"{(timeline.TicksUntilNextThreshold / 25).ToString()}-{chosenCoalition}-{chosenPolicy}";
					return influenceMeterTooltipTextCached.Update(timeCoalitionPolicy);
				};
			}
			else
			{
				coalitionImage.GetImageName = () => NoneImage;
				influenceMeter.IsVisible = () => false;
				influenceLevel.IsVisible = () => false;
				coalitionImage.IsVisible = () => false;
				noCoalitionImage.IsVisible = () => true;
				container.GetTooltipText = () => FluentProvider.GetMessage(PlayerInfluenceLevel, "level", "N/A");;
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
			currentTicks = ticks;

			if (ticks >= timeline.MaxTicks)
			{
				timeline.TicksChanged -= HandleTicksChanged;
				influenceMeter.IsVisible = () => false;
			}
		}
	}
}
