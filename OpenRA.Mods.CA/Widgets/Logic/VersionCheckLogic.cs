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
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public enum ReleaseType { Full, PreRelease, DevTest };

	public class VersionCheckLogic : ChromeLogic
	{
		readonly Widget widget;
		readonly string versionCheckFilePath;
		bool updateAvailable;
		LabelWidget newVersionLabel;
		ExternalLinkButtonWidget downloadButton;
		string currentVersion;

		[ObjectCreator.UseCtor]
		public VersionCheckLogic(Widget widget, ModData modData)
		{
			this.widget = widget;
			var logDir = Platform.SupportDir + "Logs";
			versionCheckFilePath = Path.Combine(logDir, "ca-versioncheck.log");
			updateAvailable = false;
			widget.IsVisible = () => !Game.Settings.Debug.PerfGraph && updateAvailable;

			newVersionLabel = widget.Get<LabelWidget>("NEW_VERSION_LABEL");
			downloadButton = widget.Get<ExternalLinkButtonWidget>("NEW_VERSION_BUTTON");
			currentVersion = modData.Manifest.Metadata.Version;

			if (currentVersion == "prep-CA")
				return;

			var versionCheck = GetLastVersionCheck();
			if (versionCheck.FromVersion == currentVersion && (DateTime.UtcNow - versionCheck.LastChecked).TotalHours < 3)
			{
				if (versionCheck.Release != null)
					DisplayAvailableUpdate(versionCheck.Release);

				return;
			}

			var currentReleaseType = GetCurrentReleaseType();

			Task.Run(async () =>
			{
				try
				{
					var releases = await GetReleases(false);

					if (currentReleaseType == ReleaseType.DevTest)
					{
						var devReleases = await GetReleases(true);
						releases = releases.Concat(devReleases).OrderByDescending(r => r.created_at).ToList();
					}

					foreach (var release in releases)
					{
						if (release.draft)
							continue;

						if (currentReleaseType == ReleaseType.Full && (release.prerelease || release.tag_name.Contains("PreRelease") || release.tag_name.Contains("DevTest")))
							continue;

						if (currentReleaseType == ReleaseType.PreRelease && release.tag_name.Contains("DevTest"))
							continue;

						if (release.tag_name == currentVersion)
							break;

						DisplayAvailableUpdate(release);
						versionCheck.Release = release;
						break;
					}

					SetLastVersionCheck(versionCheck);
				}
				catch
				{
					return;
				}
			});
		}

		async Task<IList<Release>> GetReleases(bool dev)
		{
			var apiUrl = dev ? "https://api.github.com/repos/Darkademic/CAmod/releases" : "https://api.github.com/repos/Inq8/CAmod/releases";
			var client = HttpClientFactory.Create();
			client.DefaultRequestHeaders.Add("User-Agent","OpenRA-CombinedArms");
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

		ReleaseType GetCurrentReleaseType()
		{
			if (currentVersion.Contains("PreRelease"))
				return ReleaseType.PreRelease;
			else if (currentVersion == "prep-CA" || currentVersion.Contains("DevTest"))
				return ReleaseType.DevTest;

			return ReleaseType.Full;
		}

		VersionCheck GetLastVersionCheck()
		{
			var versionCheck = new VersionCheck() {
				LastChecked = DateTime.MinValue
			};

			if (!File.Exists(versionCheckFilePath))
				return versionCheck;

			try {
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
			public string tag_name { get; set; }
			public string html_url { get; set; }
			public bool prerelease { get; set; }
			public bool draft { get; set; }
			public DateTime created_at { get; set; }
		}
	}
}
