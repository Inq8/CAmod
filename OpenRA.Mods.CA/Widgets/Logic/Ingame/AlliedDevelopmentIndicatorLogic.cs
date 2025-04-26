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
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	class AlliedDevelopmentIndicatorLogic : ChromeLogic
	{
		[TranslationReference("level")]
		const string PlayerDevelopmentLevel = "label-player-development-level";

		[TranslationReference("time")]
		const string PlayerDevelopmentLevelTime = "label-player-development-level-time";

		const string NoneImage = "none";
		const string DisabledImage = "disabled";

		ProvidesPrerequisitesOnTimeline timeline;

		string chosenCoalition;

		private readonly UpgradesManager upgradesManager;
		private readonly AlliedDevelopmentMeterWidget developmentMeter;

		[ObjectCreator.UseCtor]
		public AlliedDevelopmentIndicatorLogic(Widget widget, World world)
		{
			timeline = world.LocalPlayer.PlayerActor.TraitsImplementing<ProvidesPrerequisitesOnTimeline>()
				.FirstOrDefault(c => c.Info.Type == "AlliedDevelopment");

			var container = widget.Get<ContainerWidget>("ALLIED_DEVELOPMENT");
			var coalitionImage = container.Get<ImageWidget>("ALLIED_DEVELOPMENT_IMAGE");
			developmentMeter = container.Get<AlliedDevelopmentMeterWidget>("ALLIED_DEVELOPMENT_METER");
			developmentMeter.Thresholds = timeline.Thresholds;

			var tooltipTextCached = new CachedTransform<int?, string>((secs) =>
			{
				if (timeline == null)
					return "";

				var thresholdsPassed = timeline.ThresholdsPassed;

				var tooltip = TranslationProvider.GetString(PlayerDevelopmentLevel, Translation.Arguments("level", thresholdsPassed));

				if (timeline.TicksUntilNextThreshold > 0)
					tooltip += "\n" + TranslationProvider.GetString(PlayerDevelopmentLevelTime, Translation.Arguments("time", WidgetUtils.FormatTime(timeline.TicksUntilNextThreshold, world.Timestep)));

				return tooltip;
			});

			developmentMeter.GetTooltipText = () =>
			{
				return tooltipTextCached.Update(timeline.TicksUntilNextThreshold / 25);
			};

			if (world.LocalPlayer.Faction.Side != "Allies")
			{
				coalitionImage.GetImageName = () => DisabledImage;
				coalitionImage.IsVisible = () => false;
				return;
			}

			upgradesManager = world.LocalPlayer.PlayerActor.Trait<UpgradesManager>();

			if (upgradesManager == null)
			{
				coalitionImage.GetImageName = () => NoneImage;
				coalitionImage.IsVisible = () => true;
				return;
			}

			upgradesManager.UpgradeCompleted += HandleUpgradeCompleted;
			timeline.PercentageChanged += HandlePercentageChanged;

			coalitionImage.GetImageName = () =>  {
				if (timeline == null)
					return NoneImage;

				if (timeline.PercentageComplete == 100)
					return chosenCoalition ?? NoneImage;

				return DisabledImage;
			};

			coalitionImage.IsVisible = () => true;
		}

		private void HandleUpgradeCompleted(string coalitionName)
		{
			if (coalitionName.EndsWith(".coalition"))
			{
				chosenCoalition = coalitionName.Split('.')[0];
				upgradesManager.UpgradeCompleted -= HandleUpgradeCompleted;
			}
		}

		private void HandlePercentageChanged(int percentage)
		{
			developmentMeter.Percentage = percentage;

			if (percentage == 100)
			{
				timeline.PercentageChanged -= HandlePercentageChanged;
				developmentMeter.IsVisible = () => false;
			}
		}
	}
}
