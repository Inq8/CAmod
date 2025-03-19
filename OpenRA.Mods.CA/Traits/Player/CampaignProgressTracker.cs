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
using System.Security.Cryptography;
using Newtonsoft.Json;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Stores campaign progress.")]
	public class CampaignProgressTrackerInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new CampaignProgressTracker(); }
	}

	public class CampaignProgressTracker : INotifyWinStateChanged, IResolveOrder
	{
		private const string encryptionKey = "b14ca5898a4e4133bbce2ea2315a1916";
		private static string campaignProgressFilePath = Path.Combine(Platform.SupportDir + "Logs", "ca-campaign.log");
		private bool developerCommandUsed = false;

		public CampaignProgressTracker() {}

		void INotifyWinStateChanged.OnPlayerWon(Player player)
		{
			if (developerCommandUsed)
				return;

			if (player != player.World.LocalPlayer)
				return;

			if (!player.World.Map.Categories.Contains("Campaign"))
				return;

			var missionTitle = GetMapTileWithoutNumber(player.World.Map.Title);
			var difficulty = player.World.WorldActor.TraitsImplementing<ScriptLobbyDropdown>()
				.FirstOrDefault(sld => sld.Info.ID == "difficulty");

			var campaignProgress = GetCampaignProgress();
			campaignProgress.TryGetValue(missionTitle, out var existingMissionResult);

			if (existingMissionResult != null)
			{
				// If mission has difficulty settings, only store victory if mission is not already completed on same or higher difficulty.
				if (difficulty != null)
				{
					var difficultyOptions = difficulty.Info.Values.Reverse();
					foreach (var option in difficultyOptions)
					{
						// Mission beaten on a higher difficulty, store the new details.
						if (option.Key == difficulty.Value && option.Key != existingMissionResult.Difficulty)
							break;

						// Mission beaten on same difficulty, only store the new details if the new time is better.
						if (option.Key == existingMissionResult.Difficulty && existingMissionResult.Ticks <= player.World.WorldTick)
							return;
					}
				}
				else if (existingMissionResult.Ticks <= player.World.WorldTick)
					return;
			}

			var speed = TranslationProvider.GetString(player.World.GameSpeed.Name);

			var result = new MissionVictoryResult()
			{
				Uid = player.World.Map.Uid,
				Version = Game.ModData.Manifest.Metadata.Version,
				MissionTitle = missionTitle,
				Difficulty = difficulty?.Value,
				Time = WidgetUtils.FormatTime(player.World.WorldTick, player.World.Timestep),
				Ticks = player.World.WorldTick,
				DateCompleted = DateTime.Now,
				Speed = speed
			};

			campaignProgress[missionTitle] = result;
			SaveCampaignProgress(campaignProgress);
		}

		void INotifyWinStateChanged.OnPlayerLost(Player player)	{ }

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString.StartsWith("Dev"))
				developerCommandUsed = true;
		}

		public static string GetMapTileWithoutNumber(string mapTitle)
		{
			var firstColonIndex = mapTitle.IndexOf(": ");
			var firstPeriodIndex = mapTitle.IndexOf(". ");
			var splitIndex = firstColonIndex >= 0 ? firstColonIndex : firstPeriodIndex;

			if (splitIndex >= 0)
				return mapTitle.Substring(splitIndex + 2);

			return mapTitle;
		}

		public static Dictionary<string, MissionVictoryResult> GetCampaignProgress()
		{
			var campaignProgress = new Dictionary<string, MissionVictoryResult>();

			if (File.Exists(campaignProgressFilePath))
			{
				try
				{
					var campaignProgressFileContents = File.ReadAllText(campaignProgressFilePath);
					var decryptedJson = DecryptString(campaignProgressFileContents, encryptionKey);
					campaignProgress = JsonConvert.DeserializeObject<Dictionary<string, MissionVictoryResult>>(decryptedJson);
				}
				catch
				{
					// do nothing
				}
			}

			return campaignProgress;
		}

		static void SaveCampaignProgress(Dictionary<string, MissionVictoryResult> campaignProgress)
		{
			try
			{
				var json = JsonConvert.SerializeObject(campaignProgress);
				var encryptedJson = EncryptString(json, encryptionKey);
				File.WriteAllText(campaignProgressFilePath, encryptedJson);
			}
			catch
			{
				// do nothing
			}
		}

		static string EncryptString(string plainText, string key)
		{
			byte[] iv = new byte[16];
			byte[] array;

			using (Aes aes = Aes.Create())
			{
				aes.Key = System.Text.Encoding.UTF8.GetBytes(key);
				aes.IV = iv;

				ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

				using (MemoryStream memoryStream = new MemoryStream())
				{
					using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
					{
						using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
						{
							streamWriter.Write(plainText);
						}

						array = memoryStream.ToArray();
					}
				}
			}

			return Convert.ToBase64String(array);
		}

		static string DecryptString(string cipherText, string key)
		{
			byte[] iv = new byte[16];
			byte[] buffer = Convert.FromBase64String(cipherText);

			using (Aes aes = Aes.Create())
			{
				aes.Key = System.Text.Encoding.UTF8.GetBytes(key);
				aes.IV = iv;
				ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

				using (MemoryStream memoryStream = new MemoryStream(buffer))
				{
					using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
					{
						using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
						{
							return streamReader.ReadToEnd();
						}
					}
				}
			}
		}
	}

	public class MissionVictoryResult
	{
		public string Uid;
		public string Version;
		public string MissionTitle;
		public string Difficulty;
		public string Time;
		public int Ticks;
		public DateTime DateCompleted;
		public string Speed;
	}
}
