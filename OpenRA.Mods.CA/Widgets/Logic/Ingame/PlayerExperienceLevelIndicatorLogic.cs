#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	class PlayerExperienceLevelIndicatorLogic : ChromeLogic
	{
		[TranslationReference("level")]
		const string PlayerLevel = "label-player-level";

		[TranslationReference("currentXp")]
		const string PlayerLevelCurrentXp = "label-player-level-current-xp";

		[TranslationReference("nextLevelXp")]
		const string PlayerLevelRequiredXp = "label-player-level-required-xp";

		[ObjectCreator.UseCtor]
		public PlayerExperienceLevelIndicatorLogic(Widget widget, World world)
		{
			var playerExperience = world.LocalPlayer.PlayerActor.Trait<PlayerExperience>();
			var playerExperienceLevels = world.LocalPlayer.PlayerActor.Trait<PlayerExperienceLevels>();
			var rankImage = widget.Get<ImageWidget>("PLAYER_EXPERIENCE_LEVEL");
			rankImage.GetImageName = () => "level" + playerExperienceLevels.CurrentLevel;
			rankImage.IsVisible = () => playerExperienceLevels.Enabled;

			var tooltipTextCached = new CachedTransform<int?, string>((CurrentXp) =>
			{
				var tooltip = TranslationProvider.GetString(
					PlayerLevel,
					Translation.Arguments("level", playerExperienceLevels.CurrentLevel));

				if (playerExperienceLevels.XpRequiredForNextLevel != null) {
					tooltip = tooltip
					+ "\n\n"
					+ TranslationProvider.GetString(
					PlayerLevelCurrentXp,
					Translation.Arguments("currentXp", CurrentXp))
					+ "\n"
					+ TranslationProvider.GetString(
					PlayerLevelRequiredXp,
					Translation.Arguments("nextLevelXp", playerExperienceLevels.XpRequiredForNextLevel));
				}

				return tooltip;
			});

			rankImage.GetTooltipText = () =>
			{
				return tooltipTextCached.Update(playerExperienceLevels.XpRequiredForNextLevel == null ? null : playerExperience.Experience);
			};
		}
	}
}
