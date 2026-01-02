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
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets
{
  /// <summary>
  /// A label widget that supports clickable cross-reference links.
  /// Supports two syntaxes:
  /// - [[Entry Name]] - displays and links to "Entry Name"
  /// - [[Display Text|Link Target]] - displays "Display Text", links to "Link Target"
  /// Links are rendered in bold with an accent color.
  /// Ctrl+Left-Click on a link triggers the OnLinkClicked callback.
  /// </summary>
  public class LinkableLabelWidget : LabelWidget
  {
    /// <summary>Callback invoked when a link is Ctrl+Clicked. Receives the link target text.</summary>
    public Action<string> OnLinkClicked;

    /// <summary>Color for link text. If not set, inherits from TextColor.</summary>
    public Color? LinkColor = null;

    /// <summary>Color for link text when hovering. If not set, inherits from TextColor.</summary>
    public Color? LinkHoverColor = null;

    /// <summary>Font for link text. If not set, inherits from Font.</summary>
    public string LinkFont = null;

    /// <summary>Validates that a link target exists. If null, all links are considered valid.</summary>
    public Func<string, bool> IsValidLink;

    /// <summary>
    /// Resolves a link target to its display text.
    /// Used when only [[target]] syntax is provided (no explicit display text).
    /// If null or returns null, the target itself is used as display text.
    /// </summary>
    public Func<string, string> ResolveDisplayText;

    // Regex to match [[link text]] or [[display text|link target]] patterns
    // Group 1: Display text (or entry name if no pipe)
    // Group 2: Link target (optional, defaults to display text if not provided)
    static readonly Regex LinkPattern = new(@"\[\[([^\]|]+)(?:\|([^\]]+))?\]\]", RegexOptions.Compiled);

    // Cached parsed text segments
    readonly List<TextSegment> segments = new();
    readonly List<LinkRegion> linkRegions = new();
    string lastParsedText;
    int lastParsedWidth;
    string hoveredLink;

    // Represents a segment of text (either normal or link)
    struct TextSegment
    {
      public string Text; // The text to display
      public string LinkTarget; // The link target (null if not a link, may differ from Text)
      public bool IsValid; // only relevant for links
    }

    // Represents the clickable region of a link in screen coordinates
    struct LinkRegion
    {
      public Rectangle Bounds;
      public string LinkTarget;
    }

    [ObjectCreator.UseCtor]
    public LinkableLabelWidget(ModData modData)
      : base(modData)
    {
    }

    protected LinkableLabelWidget(LinkableLabelWidget other)
      : base(other)
    {
      OnLinkClicked = other.OnLinkClicked;
      LinkColor = other.LinkColor;
      LinkHoverColor = other.LinkHoverColor;
      LinkFont = other.LinkFont;
      IsValidLink = other.IsValidLink;
      ResolveDisplayText = other.ResolveDisplayText;
    }

    void ParseText(string text, int wrapWidth)
    {
      // Check if we need to reparse
      if (text == lastParsedText && wrapWidth == lastParsedWidth)
        return;

      lastParsedText = text;
      lastParsedWidth = wrapWidth;
      segments.Clear();

      if (string.IsNullOrEmpty(text))
        return;

      var matches = LinkPattern.Matches(text);
      var lastIndex = 0;

      foreach (Match match in matches)
      {
        // Add text before the link
        if (match.Index > lastIndex)
        {
          segments.Add(new TextSegment
          {
            Text = text[lastIndex..match.Index],
            LinkTarget = null
          });
        }

        // Add the link
        // Group 1 is display text (or target if no pipe), Group 2 is link target (optional)
        var hasExplicitDisplayText = match.Groups[2].Success;
        var linkTarget = hasExplicitDisplayText ? match.Groups[2].Value : match.Groups[1].Value;

        // Resolve display text: use explicit text if provided, otherwise resolve from target
        string displayText;
        if (hasExplicitDisplayText)
          displayText = match.Groups[1].Value;
        else
          displayText = ResolveDisplayText?.Invoke(linkTarget) ?? linkTarget;

        var isValid = IsValidLink?.Invoke(linkTarget) ?? true;
        segments.Add(new TextSegment
        {
          Text = displayText,
          LinkTarget = linkTarget,
          IsValid = isValid
        });

        lastIndex = match.Index + match.Length;
      }

      // Add remaining text after the last link
      if (lastIndex < text.Length)
      {
        segments.Add(new TextSegment
        {
          Text = text[lastIndex..],
          LinkTarget = null
        });
      }
    }

    public override void Draw()
    {
      if (!Game.Renderer.Fonts.TryGetValue(Font, out var normalFont))
        throw new ArgumentException($"Requested font '{Font}' was not found.");

      // Determine link font: inherit from label if not specified
      SpriteFont linkFont;
      if (string.IsNullOrEmpty(LinkFont))
        linkFont = normalFont;
      else if (!Game.Renderer.Fonts.TryGetValue(LinkFont, out linkFont))
        linkFont = normalFont;

      var text = GetText();
      if (string.IsNullOrEmpty(text))
        return;

      // Parse the text into segments
      ParseText(text, Bounds.Width);

      // If no links found, fall back to base rendering
      if (segments.Count == 0 || (segments.Count == 1 && segments[0].LinkTarget == null))
      {
        base.Draw();
        return;
      }

      // Clear link regions for recalculation
      linkRegions.Clear();

      // Calculate base position
      var position = RenderOrigin;
      var offset = normalFont.TopOffset;

      // Handle word wrapping by building the full display text first
      var displayText = BuildDisplayText();
      var wrappedText = WordWrap ? WidgetUtils.WrapText(displayText, Bounds.Width, normalFont) : displayText;
      var textSize = normalFont.Measure(wrappedText);

      if (VAlign == TextVAlign.Top)
        position += new int2(0, -offset);
      else if (VAlign == TextVAlign.Middle)
        position += new int2(0, (Bounds.Height - textSize.Y - offset) / 2);
      else if (VAlign == TextVAlign.Bottom)
        position += new int2(0, Bounds.Height - textSize.Y);

      if (Align == TextAlign.Center)
        position += new int2((Bounds.Width - textSize.X) / 2, 0);
      else if (Align == TextAlign.Right)
        position += new int2(Bounds.Width - textSize.X, 0);

      // Draw segments
      DrawSegments(position, normalFont, linkFont, wrappedText);
    }

    string BuildDisplayText()
    {
      var sb = new System.Text.StringBuilder();
      foreach (var segment in segments)
        sb.Append(segment.Text);
      return sb.ToString();
    }

    void DrawSegments(int2 basePosition, SpriteFont normalFont, SpriteFont linkFont, string wrappedText)
    {
      var color = GetColor();
      var bgDark = GetContrastColorDark();
      var bgLight = GetContrastColorLight();

      // Split wrapped text into lines for multi-line handling
      var lines = wrappedText.Split('\n');
      var lineY = basePosition.Y;
      var segmentIndex = 0;
      var charIndexInSegment = 0;

      foreach (var line in lines)
      {
        var lineX = basePosition.X;

        // Adjust line X for alignment
        if (Align == TextAlign.Center)
        {
          var lineWidth = normalFont.Measure(line).X;
          var fullWidth = normalFont.Measure(wrappedText.Split('\n')[0]).X;
          lineX = basePosition.X + (fullWidth - lineWidth) / 2;
        }
        else if (Align == TextAlign.Right)
        {
          var lineWidth = normalFont.Measure(line).X;
          var fullWidth = normalFont.Measure(wrappedText.Split('\n')[0]).X;
          lineX = basePosition.X + fullWidth - lineWidth;
        }

        var charIndex = 0;
        while (charIndex < line.Length && segmentIndex < segments.Count)
        {
          var segment = segments[segmentIndex];
          var remainingInSegment = segment.Text.Length - charIndexInSegment;
          var remainingInLine = line.Length - charIndex;
          var charsToTake = Math.Min(remainingInSegment, remainingInLine);

          if (charsToTake <= 0)
          {
            segmentIndex++;
            charIndexInSegment = 0;
            continue;
          }

          var segmentText = segment.Text.Substring(charIndexInSegment, charsToTake);

          // Determine if this is a valid link
          var isLink = segment.LinkTarget != null && segment.IsValid;
          var font = isLink ? linkFont : normalFont;

          // Determine link color: inherit from label if not specified
          Color linkTextColor;
          if (segment.LinkTarget == hoveredLink)
            linkTextColor = LinkHoverColor ?? color;
          else
            linkTextColor = LinkColor ?? color;

          var textColor = isLink ? linkTextColor : color;

          // Calculate baseline adjustment for different fonts
          // Align by bottom of text (baseline) rather than top
          var baselineOffset = isLink ? (normalFont.Measure("A").Y - linkFont.Measure("A").Y) : 0;

          // Draw the text segment
          var pos = new int2(lineX, lineY + baselineOffset);
          if (Contrast)
            font.DrawTextWithContrast(segmentText, pos, textColor, bgDark, bgLight, ContrastRadius);
          else if (Shadow)
            font.DrawTextWithShadow(segmentText, pos, textColor, bgDark, bgLight, 1);
          else
            font.DrawText(segmentText, pos, textColor);

          // Measure the actual rendered text width
          var segmentWidth = font.Measure(segmentText).X;

          // Track link region for click detection
          if (isLink)
          {
            var segmentHeight = font.Measure(segmentText).Y;
            linkRegions.Add(new LinkRegion
            {
              Bounds = new Rectangle(lineX, lineY, segmentWidth, segmentHeight),
              LinkTarget = segment.LinkTarget
            });
          }

          // Advance position using the actual font used for drawing
          lineX += segmentWidth;
          charIndex += charsToTake;
          charIndexInSegment += charsToTake;

          // Move to next segment if we've consumed this one
          if (charIndexInSegment >= segment.Text.Length)
          {
            segmentIndex++;
            charIndexInSegment = 0;
          }
        }

        lineY += normalFont.Measure("A").Y;

        // Skip the newline character in segment tracking
        if (segmentIndex < segments.Count)
        {
          charIndexInSegment++;
          if (charIndexInSegment >= segments[segmentIndex].Text.Length)
          {
            segmentIndex++;
            charIndexInSegment = 0;
          }
        }
      }
    }

    string GetLinkAtPosition(int2 pos)
    {
      foreach (var region in linkRegions)
      {
        if (region.Bounds.Contains(pos))
          return region.LinkTarget;
      }

      return null;
    }

    public override void MouseExited()
    {
      base.MouseExited();
      hoveredLink = null;
    }

    public override bool HandleMouseInput(MouseInput mi)
    {
      // Update hovered link state
      var linkAtPos = GetLinkAtPosition(mi.Location);
      hoveredLink = linkAtPos;

      // Handle Ctrl+Left-Click on links
      if (mi.Event == MouseInputEvent.Down &&
        mi.Button == MouseButton.Left &&
        mi.Modifiers.HasFlag(Modifiers.Ctrl) &&
        linkAtPos != null)
      {
        OnLinkClicked?.Invoke(linkAtPos);
        return true;
      }

      return base.HandleMouseInput(mi);
    }

    public override Widget Clone() => new LinkableLabelWidget(this);
  }
}
