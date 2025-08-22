#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Graphics
{
	/// <summary>
	/// Renders a capsule shape using the same pattern as Capsule HitShape:
	/// Two circles connected by parallel lines. Much simpler and crash-safe.
	/// </summary>
	public static class CapsuleAnnotationRenderable
	{
		public static IEnumerable<IRenderable> Create(WPos capsuleStart, WPos capsuleEnd, WDist radius, int width, Color color)
		{
			// Render two circles at the ends
			yield return new CircleAnnotationRenderable(capsuleStart, radius, width, color);
			yield return new CircleAnnotationRenderable(capsuleEnd, radius, width, color);

			if (capsuleStart != capsuleEnd)
			{
				// Calculate perpendicular offset for the connecting lines (like Capsule HitShape does)
				var capsuleVec = capsuleEnd - capsuleStart;
				var perpVec = new WVec(-capsuleVec.Y, capsuleVec.X, 0);
				if (perpVec.Length > 0)
				{
					var radiusVec = perpVec * radius.Length / perpVec.Length;

					var topStart = capsuleStart + radiusVec;
					var topEnd = capsuleEnd + radiusVec;
					var bottomStart = capsuleStart - radiusVec;
					var bottomEnd = capsuleEnd - radiusVec;

					yield return new LineAnnotationRenderable(topStart, topEnd, width, color);
					yield return new LineAnnotationRenderable(bottomStart, bottomEnd, width, color);
				}
			}
		}
	}
}
