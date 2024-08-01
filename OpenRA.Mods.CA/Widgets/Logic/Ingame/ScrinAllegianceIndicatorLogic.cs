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
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	class ScrinAllegianceIndicatorLogic : ChromeLogic
	{
		const string DisabledImage = "disabled";

		readonly ProvidesPrerequisiteOnCount counter;
		readonly UpgradesManager upgradesManager;

		string chosenAllegiance;

		int fadeInMaxTicks = 5;
		int waitMaxTicks = 85;
		int fadeOutMaxTicks = 15;

		int fadeInTicks = 0;
		int waitTicks = 0;
		int fadeOutTicks = 0;

		[ObjectCreator.UseCtor]
		public ScrinAllegianceIndicatorLogic(Widget widget, World world)
		{
			counter = world.LocalPlayer.PlayerActor.TraitsImplementing<ProvidesPrerequisiteOnCount>()
				.FirstOrDefault(c => c.Info.RequiredCounts.ContainsKey("Refineries"));

			upgradesManager = world.LocalPlayer.PlayerActor.Trait<UpgradesManager>();

			var container = widget.Get<ContainerWidget>("SCRIN_ALLEGIANCE");
			var countImage = container.Get<ImageWidget>("SCRIN_ALLEGIANCE_LEVEL");
			var incrementImage = container.Get<ImageWithAlphaWidget>("SCRIN_ALLEGIANCE_LEVEL_UP");
			var countImageGlow = container.Get<ImageWithAlphaWidget>("SCRIN_ALLEGIANCE_LEVEL_GLOW");

			if (counter == null)
			{
				countImage.GetImageName = () => DisabledImage;
				countImage.IsVisible = () => true;

				countImageGlow.GetImageName = () => DisabledImage;
				countImageGlow.IsVisible = () => false;

				incrementImage.IsVisible = () => false;
				return;
			}

			counter.Incremented += () => {
				fadeInTicks = fadeInMaxTicks;
				waitTicks = waitMaxTicks;
				fadeOutTicks = fadeOutMaxTicks;
			};

			counter.UnlockedPermanently += (allegiance) => {
				chosenAllegiance = allegiance.Split('.')[0];
				fadeInTicks = fadeInMaxTicks;
				waitTicks = waitMaxTicks;
				fadeOutTicks = fadeOutMaxTicks;
			};

			countImage.GetImageName = () =>  GetCountImageName();
			countImage.IsVisible = () => counter.Enabled;

			countImageGlow.GetImageName = () => $"{GetCountImageName()}-glow";
			countImageGlow.IsVisible = () => IncrementImageAlpha > 0;
			countImageGlow.GetAlpha = () => IncrementImageAlpha;

			incrementImage.IsVisible = () => chosenAllegiance == null && IncrementImageAlpha > 0;
			incrementImage.GetAlpha = () => chosenAllegiance == null ? IncrementImageAlpha : 0f;
		}

		private string GetCountImageName()
		{
			if (chosenAllegiance != null)
			{
				return chosenAllegiance;
			}
			else
			{
				var count = counter.Counts.ContainsKey("Refineries") ? Math.Min(counter.Counts["Refineries"], 4) : 0;
				return $"level{count}";
			}
		}

		public float IncrementImageAlpha {
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

		public override void Tick()
		{
			if (counter == null || !counter.Enabled)
				return;

			if (fadeInTicks > 0)
				fadeInTicks--;
			else if (waitTicks > 0)
				waitTicks--;
			else if (fadeOutTicks > 0)
				fadeOutTicks--;
		}
	}
}
