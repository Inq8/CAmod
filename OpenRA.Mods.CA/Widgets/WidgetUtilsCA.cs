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
using OpenRA.Graphics;

namespace OpenRA.Mods.CA.Widgets
{
	public static class WidgetUtilsCA
	{
		public static string WrapTextWithIndent(string text, int width, SpriteFont font, int indent = 4)
		{
			var textSize = font.Measure(text);
			var indentString = indent > 0 ? new string(' ', indent) : "";
			var effectiveWidth = indent > 0 ? width - font.Measure(indentString).X : width;

			if (textSize.X > width)
			{
				var lines = text.Split('\n').ToList();
				var isOriginalLine = new bool[lines.Count];

				// Mark all initial lines as original
				for (var i = 0; i < lines.Count; i++)
				{
					isOriginalLine[i] = true;
				}

				for (var i = 0; i < lines.Count; i++)
				{
					var line = lines[i];
					var currentWidth = isOriginalLine[i] ? width : effectiveWidth;

					if (font.Measure(line).X <= currentWidth)
						continue;

					// Scan forwards until we find the last word that fits
					// This guarantees a small bound on the amount of string we need to search before a linebreak
					var start = 0;
					while (true)
					{
						var spaceIndex = line.IndexOf(' ', start);
						if (spaceIndex == -1)
							break;

						var fragmentWidth = font.Measure(line[..spaceIndex]).X;
						if (fragmentWidth > currentWidth)
							break;

						start = spaceIndex + 1;
					}

					if (start > 0)
					{
						lines[i] = line[..(start - 1)];
						lines.Insert(i + 1, line[start..]);

						// Expand the isOriginalLine array and mark the new line as wrapped
						var newIsOriginalLine = new bool[lines.Count];
						for (var j = 0; j <= i; j++)
						{
							newIsOriginalLine[j] = isOriginalLine[j];
						}

						newIsOriginalLine[i + 1] = false; // This is a wrapped line
						for (var j = i + 2; j < lines.Count; j++)
						{
							newIsOriginalLine[j] = isOriginalLine[j - 1];
						}

						isOriginalLine = newIsOriginalLine;
					}
				}

				// Apply indentation only to wrapped lines (not original lines)
				if (indent > 0)
				{
					for (var i = 0; i < lines.Count; i++)
					{
						if (!isOriginalLine[i])
						{
							lines[i] = indentString + lines[i];
						}
					}
				}

				return string.Join("\n", lines);
			}

			return text;
		}
	}
}
