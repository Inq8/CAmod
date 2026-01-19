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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public enum ReleaseType { Full, PreRelease, DevTest, Invalid };

	public class MenuNotificationsLogic : ChromeLogic
	{
		readonly Widget widget;
		readonly string versionCheckFilePath;
		readonly string currentVersion;
		bool updateAvailable;
		bool hasNotifications;

		Widget newVersionWidget;
		Widget notificationsWidget;
		LabelWidget newVersionLabel;
		ExternalLinkButtonWidget downloadButton;
		ScrollPanelWidget notificationsPanel;
		Widget notificationTemplate;

		[ObjectCreator.UseCtor]
		public MenuNotificationsLogic(Widget widget, ModData modData)
		{
			this.widget = widget;
			var logDir = Platform.SupportDir + "Logs";
			versionCheckFilePath = Path.Combine(logDir, "ca-versioncheck.log");
			currentVersion = modData.Manifest.Metadata.Version;

			newVersionWidget = widget.Get("NEW_VERSION");
			notificationsWidget = widget.Get("NOTIFICATIONS");

			newVersionLabel = newVersionWidget.Get<LabelWidget>("NEW_VERSION_LABEL");
			downloadButton = newVersionWidget.Get<ExternalLinkButtonWidget>("NEW_VERSION_BUTTON");

			notificationsPanel = notificationsWidget.Get<ScrollPanelWidget>("NOTIFICATIONS_PANEL");
			notificationTemplate = notificationsPanel.Get("NOTIFICATION_TEMPLATE");
			notificationTemplate.IsVisible = () => false;

			// Show update widget if update available, otherwise show notifications widget if available
			newVersionWidget.IsVisible = () => !Game.Settings.Debug.PerfGraph && updateAvailable;
			notificationsWidget.IsVisible = () => !Game.Settings.Debug.PerfGraph && !updateAvailable && hasNotifications
				&& widget.Parent.GetOrNull("MENUS").Children.Any(c => c.IsVisible());

			CheckForUpdatesAndNotifications();
		}

		async void CheckForUpdatesAndNotifications()
		{
			var versionCheck = GetLastVersionCheck();
			if (versionCheck.FromVersion == currentVersion
				&& (DateTime.UtcNow - versionCheck.LastChecked).TotalHours < 3
				&& versionCheck.Release != null
				&& versionCheck.Release.tag_name != currentVersion)
			{
				DisplayAvailableUpdate(versionCheck.Release);
				return;
			}

			await Task.Run(async () =>
			{
				try
				{
					await CheckForUpdates(versionCheck);

					// Only check for notifications if no update was found
					if (!updateAvailable)
						await FetchNotifications();
				}
				catch
				{
					// If version check fails, still try to fetch notifications
					if (!updateAvailable)
					{
						try
						{
							await FetchNotifications();
						}
						catch
						{
							// Silently fail - this is not critical functionality
						}
					}
				}
			});
		}

		async Task CheckForUpdates(VersionCheck versionCheck)
		{
			if (currentVersion == "prep-CA")
				return;

			var releases = await GetReleases(false);
			var currentReleaseType = GetCurrentReleaseType();

			if (currentReleaseType == ReleaseType.DevTest)
			{
				var devReleases = await GetReleases(true);
				releases = releases.Concat(devReleases).ToList();
			}

			releases = releases.Where(r => r.ReleaseType != ReleaseType.Invalid).OrderByDescending(r => r.created_at).ToList();

			foreach (var release in releases)
			{
				if (release.draft || release.name.Contains("Draft"))
					continue;

				// For devtest releases, only consider versions that are at least 30 minutes old
				if (release.ReleaseType == ReleaseType.DevTest && (DateTime.UtcNow - release.created_at).TotalMinutes < 30)
					continue;

				// If the current release is a full release, ignore pre-releases and dev tests
				if (currentReleaseType == ReleaseType.Full && (release.ReleaseType == ReleaseType.PreRelease || release.ReleaseType == ReleaseType.DevTest))
					continue;

				// If the current release is a pre-release, ignore dev tests
				if (currentReleaseType == ReleaseType.PreRelease && release.ReleaseType == ReleaseType.DevTest)
					continue;

				// If the current release matches the release we are checking, we can stop here since all subsequent releases will be older
				if (release.tag_name == currentVersion)
				{
					versionCheck.Release = release;
					break;
				}

				var currentRelease = releases.FirstOrDefault(r => r.tag_name == currentVersion);

				// If the current release is newer than the release we are checking, we can stop here since all subsequent releases will be older
				if (currentRelease != null && release.created_at < currentRelease.created_at)
					break;

				DisplayAvailableUpdate(release);
				versionCheck.Release = release;
				break;
			}

			SetLastVersionCheck(versionCheck);
		}

		async Task<IList<Release>> GetReleases(bool dev)
		{
			var apiUrl = dev ? "https://api.github.com/repos/Darkademic/CAmod/releases" : "https://api.github.com/repos/Inq8/CAmod/releases";
			var client = HttpClientFactory.Create();
			client.DefaultRequestHeaders.Add("User-Agent", "OpenRA-CombinedArms");
			client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
			var httpResponseMessage = await client.GetAsync(apiUrl);
			httpResponseMessage.EnsureSuccessStatusCode();
			var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<IList<Release>>(responseContent);
		}

		void DisplayAvailableUpdate(Release release)
		{
			updateAvailable = true;
			newVersionLabel.Text = "Version " + release.tag_name + " is available!";
			downloadButton.Url = release.html_url;
		}

		async Task FetchNotifications()
		{
			var apiUrl = $"https://raw.githubusercontent.com/darkademic/ca-notifications/refs/heads/main/{currentVersion}.json";
			var client = HttpClientFactory.Create();
			client.DefaultRequestHeaders.Add("User-Agent", "OpenRA-CombinedArms");
			client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

			var httpResponseMessage = await client.GetAsync(apiUrl);

			// If 404, just return silently - no notifications for this version
			if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
				return;

			httpResponseMessage.EnsureSuccessStatusCode();
			var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();
			var data = JsonConvert.DeserializeObject<NotificationsData>(responseContent);

			if (data?.Notifications != null && data.Notifications.Count > 0)
			{
				Game.RunAfterTick(() => DisplayNotifications(data.Notifications));
			}
		}

		void DisplayNotifications(List<Notification> notifications)
		{
			notificationsPanel.RemoveChildren();

			var yOffset = 0;
			foreach (var notification in notifications.OrderByDescending(n => n.Date))
			{
				var template = notificationTemplate.Clone();
				template.IsVisible = () => true;

				var titleLabel = template.Get<LabelWidget>("NOTIFICATION_TITLE");
				var bodyLabel = template.Get<LabelWidget>("NOTIFICATION_BODY");

				var titleFont = Game.Renderer.Fonts[titleLabel.Font];
				var bodyFont = Game.Renderer.Fonts[bodyLabel.Font];

				// Measure title height
				var titleTextHeight = titleFont.Measure(notification.Title).Y;

				// Measure body height with proper wrapping
				var bodyWidth = bodyLabel.Bounds.Width;
				var wrappedBody = WidgetUtils.WrapText(notification.Body, bodyWidth, bodyFont);
				var bodyTextHeight = bodyFont.Measure(wrappedBody).Y;

				// Set text
				titleLabel.GetText = () => notification.Title;
				bodyLabel.GetText = () => wrappedBody;

				if (Color.TryParse(notification.TitleColor, out var color))
					titleLabel.TextColor = color;

				// Position labels
				titleLabel.Bounds.Y = 0;
				titleLabel.Bounds.Height = titleTextHeight + 4;

				bodyLabel.Bounds.Y = titleLabel.Bounds.Height;
				bodyLabel.Bounds.Height = bodyTextHeight + 8;

				// Set container position and height
				template.Bounds.Y = yOffset;
				template.Bounds.Height = titleLabel.Bounds.Height + bodyLabel.Bounds.Height;

				notificationsPanel.AddChild(template);
				yOffset += template.Bounds.Height;
			}

			notificationsPanel.Layout.AdjustChildren();
			hasNotifications = true;
		}

		ReleaseType GetCurrentReleaseType()
		{
			if (currentVersion.Contains("DevTest") || currentVersion == "prep-CA")
				return ReleaseType.DevTest;
			else if (currentVersion.Contains("PreRelease"))
				return ReleaseType.PreRelease;

			return ReleaseType.Full;
		}

		VersionCheck GetLastVersionCheck()
		{
			var versionCheck = new VersionCheck()
			{
				LastChecked = DateTime.MinValue
			};

			if (!File.Exists(versionCheckFilePath))
				return versionCheck;

			try
			{
				var lastVersionCheckFileContents = File.ReadAllText(versionCheckFilePath);
				versionCheck = JsonConvert.DeserializeObject<VersionCheck>(lastVersionCheckFileContents);
			}
			catch
			{
				// do nothing
			}

			return versionCheck;
		}

		void SetLastVersionCheck(VersionCheck versionCheck)
		{
			versionCheck.FromVersion = currentVersion;
			versionCheck.LastChecked = DateTime.UtcNow;
			var json = JsonConvert.SerializeObject(versionCheck);
			File.WriteAllText(versionCheckFilePath, json);
		}

		class VersionCheck
		{
			public string FromVersion { get; set; }
			public DateTime LastChecked { get; set; }
			public Release Release { get; set; }
		}

		class Release
		{
			public string name { get; set; }
			public string tag_name { get; set; }
			public string html_url { get; set; }
			public bool prerelease { get; set; }
			public bool draft { get; set; }
			public DateTime created_at { get; set; }

			public ReleaseType ReleaseType
			{
				get
				{
					if (prerelease && tag_name.Contains("PreRelease"))
						return ReleaseType.PreRelease;
					if (prerelease && tag_name.Contains("DevTest"))
						return ReleaseType.DevTest;
					if (!prerelease && System.Text.RegularExpressions.Regex.IsMatch(tag_name, @"^\d+\.\d+(\.\d+)?$"))
						return ReleaseType.Full;
					return ReleaseType.Invalid;
				}
			}
		}

		class NotificationsData
		{
			[JsonProperty("notifications")]
			public List<Notification> Notifications { get; set; }
		}

		class Notification
		{
			[JsonProperty("date")]
			public DateTime Date { get; set; }

			[JsonProperty("title")]
			public string Title { get; set; }

			[JsonProperty("titleColor")]
			public string TitleColor { get; set; }

			[JsonProperty("body")]
			public string Body { get; set; }
		}
	}
}
