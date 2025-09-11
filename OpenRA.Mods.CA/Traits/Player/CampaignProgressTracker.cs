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
	[TraitLocation(SystemActors.Player)]
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

		void INotifyWinStateChanged.OnPlayerWon(Player player)
		{
			if (developerCommandUsed)
				return;

			if (player != player.World.LocalPlayer)
				return;

			if (player.World.Players.Count(p => p.Playable) > 1)
				return;

			if (!player.World.Map.Categories.Contains("Campaign"))
				return;

			var missionTitle = GetMapTileWithoutNumber(player.World.Map.Title);
			var worldActor = player.World.WorldActor;
			var difficulty = worldActor.TraitsImplementing<ScriptLobbyDropdown>()
				.FirstOrDefault(sld => sld.Info.ID == "difficulty");

			var shroud = player.PlayerActor.TraitOrDefault<Shroud>();
			bool? fogEnabled = shroud?.FogEnabled;

			var mapBuildRadius = player.World.WorldActor.TraitOrDefault<MapBuildRadius>();
			bool? buildRadiusEnabled = mapBuildRadius?.BuildRadiusEnabled;

			var respawnDropdown = worldActor.TraitsImplementing<ScriptLobbyDropdown>()
				.FirstOrDefault(sld => sld.Info.ID == "respawn");

			bool? respawnEnabled = respawnDropdown != null ? respawnDropdown.Value == "enabled" : null;

			var campaignProgress = GetCampaignProgress();
			campaignProgress.TryGetValue(missionTitle, out var existingMissionResult);

			if (existingMissionResult != null)
			{
				if (difficulty != null)
				{
					var currentDifficultyIndex = -1;
					var existingDifficultyIndex = -1;

					var optionsList = difficulty.Info.Values.ToList();
					for (int i = 0; i < optionsList.Count; i++)
					{
						if (optionsList[i].Key == difficulty.Value)
							currentDifficultyIndex = i;

						if (optionsList[i].Key == existingMissionResult.Difficulty)
							existingDifficultyIndex = i;
					}

					// If existing result is on higher difficulty don't overwrite
					if (existingDifficultyIndex >= 0 && existingDifficultyIndex > currentDifficultyIndex)
						return;

					// If same difficulty, only store if new time is better
					if (currentDifficultyIndex == existingDifficultyIndex && existingMissionResult.Ticks <= player.World.WorldTick)
						return;
				}
				else if (existingMissionResult.Ticks <= player.World.WorldTick)
					return;
			}

			var speed = FluentProvider.GetMessage(player.World.GameSpeed.Name);

			campaignProgress[missionTitle] = new MissionVictoryResult()
			{
				Uid = player.World.Map.Uid,
				Version = Game.ModData.Manifest.Metadata.Version,
				MissionTitle = missionTitle,
				Difficulty = difficulty?.Value,
				Time = WidgetUtils.FormatTime(player.World.WorldTick, player.World.Timestep),
				Ticks = player.World.WorldTick,
				DateCompleted = DateTime.Now,
				Speed = speed,
				FogEnabled = fogEnabled,
				BuildRadiusEnabled = buildRadiusEnabled,
				RespawnEnabled = respawnEnabled
			};

			SaveCampaignProgress(campaignProgress);
		}

		void INotifyWinStateChanged.OnPlayerLost(Player player) { }

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
		public bool? FogEnabled;
		public bool? BuildRadiusEnabled;
		public bool? RespawnEnabled;
	}
}
