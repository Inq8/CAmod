#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Widgets;

namespace OpenRA.Mods.CA.Widgets
{
	public class ImageCAWidget : ImageWidget
	{
		public readonly int MinXResolution = 0;
		public readonly int MaxXResolution = 0;

		public override void Draw()
		{
			var resolution = Game.Renderer.Resolution;
			var resolutionWidth = resolution.Width;

			if ((MinXResolution > 0 && resolutionWidth < MinXResolution) || (MaxXResolution > 0 && resolutionWidth > MaxXResolution))
				return;

			base.Draw();
		}
	}
}
