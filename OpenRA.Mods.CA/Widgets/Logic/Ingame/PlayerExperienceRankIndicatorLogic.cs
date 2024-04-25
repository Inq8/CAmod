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
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	class PlayerExperienceRankIndicatorLogic : ChromeLogic
	{
		[TranslationReference("level")]
		const string PlayerLevel = "label-player-level";

		[ObjectCreator.UseCtor]
		public PlayerExperienceRankIndicatorLogic(Widget widget, World world)
		{
			var playerExperienceLevels = world.LocalPlayer.PlayerActor.Trait<PlayerExperienceLevels>();
			var rankImage = widget.Get<ImageWidget>("PLAYER_EXPERIENCE_RANK");
			rankImage.GetImageName = () => "rank" + playerExperienceLevels.CurrentLevel;
			rankImage.IsVisible = () => playerExperienceLevels.Enabled;

			var tooltipTextCached = new CachedTransform<string, string>((Level) =>
			{
				return TranslationProvider.GetString(
					PlayerLevel,
					Translation.Arguments("level", Level));
			});

			rankImage.GetTooltipText = () =>
			{
				return tooltipTextCached.Update(playerExperienceLevels.CurrentLevel.ToString());
			};
		}
	}
}
