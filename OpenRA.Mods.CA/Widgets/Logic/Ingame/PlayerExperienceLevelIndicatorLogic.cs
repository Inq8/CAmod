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

		const string DisabledImage = "disabled";

		readonly PlayerExperienceLevels playerExperienceLevels;

		int fadeInMaxTicks = 5;
		int waitMaxTicks = 85;
		int fadeOutMaxTicks = 15;

		int fadeInTicks = 0;
		int waitTicks = 0;
		int fadeOutTicks = 0;

		[ObjectCreator.UseCtor]
		public PlayerExperienceLevelIndicatorLogic(Widget widget, World world)
		{
			var playerExperience = world.LocalPlayer.PlayerActor.Trait<PlayerExperience>();
			playerExperienceLevels = world.LocalPlayer.PlayerActor.TraitOrDefault<PlayerExperienceLevels>();
			var container = widget.Get<ContainerWidget>("PLAYER_EXPERIENCE");
			var rankImage = container.Get<ImageWidget>("PLAYER_EXPERIENCE_LEVEL");
			var rankUpImage = container.Get<ImageWithAlphaWidget>("PLAYER_EXPERIENCE_LEVEL_UP");
			var rankImageGlow = container.Get<ImageWithAlphaWidget>("PLAYER_EXPERIENCE_LEVEL_GLOW");

			if (playerExperienceLevels == null)
			{
				rankImage.GetImageName = () => DisabledImage;
				rankImage.IsVisible = () => true;
				rankImage.GetTooltipText = () => TranslationProvider.GetString(PlayerLevel, Translation.Arguments("level", "N/A"));

				rankImageGlow.GetImageName = () => DisabledImage;
				rankImageGlow.IsVisible = () => false;

				rankUpImage.IsVisible = () => false;
				return;
			}

			playerExperienceLevels.LevelledUp += (level) =>
			{
				fadeInTicks = fadeInMaxTicks;
				waitTicks = waitMaxTicks;
				fadeOutTicks = fadeOutMaxTicks;
			};

			rankImage.GetImageName = () =>  $"level{playerExperienceLevels.CurrentLevel}";
			rankImage.IsVisible = () => playerExperienceLevels.Enabled;

			rankImageGlow.GetImageName = () => $"level{playerExperienceLevels.CurrentLevel}-glow";
			rankImageGlow.IsVisible = () => LevelUpImageAlpha > 0;
			rankImageGlow.GetAlpha = () => LevelUpImageAlpha;

			rankUpImage.IsVisible = () => LevelUpImageAlpha > 0;
			rankUpImage.GetAlpha = () => LevelUpImageAlpha;

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

		public override void Tick()
		{
			if (playerExperienceLevels == null || !playerExperienceLevels.Enabled)
				return;

			if (fadeInTicks > 0)
				fadeInTicks--;
			else if (waitTicks > 0)
				waitTicks--;
			else if (fadeOutTicks > 0)
				fadeOutTicks--;
		}

		public float LevelUpImageAlpha {
			get {
				if (fadeInTicks > 0)
					return 1f - (float)fadeInTicks / fadeInMaxTicks;
				else if (waitTicks > 0)
					return 1f;
				else if (fadeOutTicks > 0)
					return (float)fadeOutTicks / fadeOutMaxTicks;
				else
					return 0f;
			}
		}
	}
}
