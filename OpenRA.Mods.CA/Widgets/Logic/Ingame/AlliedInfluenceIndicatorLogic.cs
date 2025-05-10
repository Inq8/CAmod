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
	class AlliedInfluenceIndicatorLogic : ChromeLogic
	{
		[TranslationReference("level")]
		const string PlayerInfluenceLevel = "label-player-influence-level";

		[TranslationReference("time")]
		const string PlayerInfluenceLevelTime = "label-player-influence-level-time";

		[TranslationReference("coalition")]
		const string ChosenCoalition = "label-player-influence-coalition";

		[TranslationReference("policy")]
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
			influenceMeter = container.Get<AlliedInfluenceMeterWidget>("ALLIED_INFLUENCE_METER");

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
				influenceMeter.MaxTicks = timeline.Info.MaxTicks;

				var influenceMeterTooltipTextCached = new CachedTransform<string, string>((timeCoalitionPolicy) =>
				{
					var thresholdsPassed = timeline.ThresholdsPassed;

					var tooltip = TranslationProvider.GetString(PlayerInfluenceLevel, Translation.Arguments("level", thresholdsPassed));

					if (timeline.TicksUntilNextThreshold > 0)
						tooltip += "\n" + TranslationProvider.GetString(PlayerInfluenceLevelTime, Translation.Arguments("time", WidgetUtils.FormatTime(timeline.TicksUntilNextThreshold, world.Timestep)));

					if (chosenCoalition != null)
						tooltip += "\n" + TranslationProvider.GetString(ChosenCoalition, Translation.Arguments("coalition", char.ToUpper(chosenCoalition[0]) + chosenCoalition[1..]));

					if (chosenPolicy != null)
						tooltip += "\n" + TranslationProvider.GetString(ChosenPolicy, Translation.Arguments("policy", char.ToUpper(chosenPolicy[0]) + chosenPolicy[1..]));

					return tooltip;
				});

				influenceMeter.GetTooltipText = () =>
				{
					var timeCoalitionPolicy = $"{(timeline.TicksUntilNextThreshold / 25).ToString()}-{chosenCoalition}-{chosenPolicy}";
					return influenceMeterTooltipTextCached.Update(timeCoalitionPolicy);
				};

				timeline.PercentageChanged += HandlePercentageChanged;

				coalitionImage.GetImageName = () =>  {
					if (timeline.PercentageComplete == 100)
						return chosenCoalition ?? NoneImage;

					return DisabledImage;
				};

				coalitionImage.GetTooltipText = () =>
				{
					var timeCoalitionPolicy = $"0-{chosenCoalition}-{chosenPolicy}";
					return influenceMeterTooltipTextCached.Update(timeCoalitionPolicy);
				};
			}
			else
			{
				coalitionImage.GetImageName = () => NoneImage;
				influenceMeter.IsVisible = () => false;
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

		private void HandlePercentageChanged(int percentage)
		{
			influenceMeter.Percentage = percentage;

			if (percentage == 100)
			{
				timeline.PercentageChanged -= HandlePercentageChanged;
				influenceMeter.IsVisible = () => false;
			}
		}
	}
}
