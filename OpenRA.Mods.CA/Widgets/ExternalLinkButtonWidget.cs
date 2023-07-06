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

using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Widgets
{
	public class ExternalLinkButtonWidget : ButtonWidget
	{
		bool hovering = false;
		public Color? TextHoverColor;
		public string Url;

		[ObjectCreator.UseCtor]
		public ExternalLinkButtonWidget(ModData modData)
			: base(modData)
		{
			GetColor = () => (hovering && TextHoverColor.HasValue ? TextHoverColor.Value : TextColor);
		}

		protected ExternalLinkButtonWidget(ExternalLinkButtonWidget other)
			: base(other) { }

		public override void MouseEntered()
		{
			base.MouseEntered();
			hovering = true;
		}

		public override void MouseExited()
		{
			base.MouseExited();
			hovering = false;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Up)
				OpenUrl(Url);

			return base.HandleMouseInput(mi);
		}

		private void OpenUrl(string url)
		{
			try
			{
				Process.Start(url);
			}
			catch
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					url = url.Replace("&", "^&");
					Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				{
					Process.Start("xdg-open", url);
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				{
					Process.Start("open", url);
				}
				else
				{
					// do nothing
				}
			}
		}
	}
}
