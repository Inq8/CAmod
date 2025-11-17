
if CoopAttackStrengthMultiplier ~= nil and RaidInterval ~= nil and RaidInterval[Difficulty] ~= nil then
	RaidInterval[Difficulty] = math.max(RaidInterval[Difficulty] / CoopAttackStrengthMultiplier, 25)
end

SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Scrin = Player.GetPlayer("Scrin")
	Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Nod }
	SinglePlayerPlayer = Scrin
	CoopInit()
end

AfterWorldLoaded = function()
	local firstActivePlayer = GetFirstActivePlayer()
	TransferBaseToPlayer(SinglePlayerPlayer, firstActivePlayer)
	TotalFundsDisplay = 0
	ColonyPlatformsBeingReplaced = {}

	if #MissionPlayers > 1 then
		Actor3.Owner = MissionPlayers[2]
		Actor599.Owner = MissionPlayers[2]
		Actor11.Owner = MissionPlayers[2]
		Trigger.AfterDelay(1, function()
			Scrin.GetActorsByType("harv.scrin")[2].Owner = MissionPlayers[2]
		end)
	end

	if #MissionPlayers > 2 then
		Actor342.Owner = MissionPlayers[3]
		Actor11.Owner = MissionPlayers[3]
	end

	Utils.Do(MissionPlayers, function(p)
		if p ~= firstActivePlayer then
			p.Cash = 3000
		end
	end)
end

AfterTick = function()

end

UpdateObjectiveMessage = function()
	if FieldsClearedAndBeingHarvested == 6 then
		UserInterface.SetMissionText("6 of 6 fields occupied.\n   Maintain for " .. UtilsCA.FormatTimeForGameSpeed(TimerTicks), HSLColor.Lime)
	else
		local missionText = FieldsClearedAndBeingHarvested .. " of 6 fields occupied  -  Next reinforcement threshold: $" .. TotalFundsDisplay .. "/" .. NextReinforcementThreshold
		UserInterface.SetMissionText(missionText, HSLColor.Yellow)
	end
end

CheckReinforcementThreshold = function()
	local CoopTotalFunds = 0
	Utils.Do(MissionPlayers, function(p)
		local playerTotalFunds = p.Resources
		CoopTotalFunds = CoopTotalFunds + playerTotalFunds
	end)

	TotalFundsDisplay = CoopTotalFunds

	if CoopTotalFunds >= NextReinforcementThreshold then
		Utils.Do(MissionPlayers, function(p)
			p.Resources = p.Resources - (NextReinforcementThreshold / #MissionPlayers)
		end)

		if NextReinforcementThreshold < ReinforcementFinalThreshold[Difficulty] then
			NextReinforcementThreshold = NextReinforcementThreshold + ReinforcementThresholdIncrement
		end

		DoReinforcements()
	end
end

CheckColonyPlatform = function()
	Utils.Do(GetMcvPlayers(), function(p)
		local colonyPlatformsAndMcvs = p.GetActorsByTypes({ "smcv", "sfac" })
		if #colonyPlatformsAndMcvs == 0 and not ColonyPlatformsBeingReplaced[p.InternalName] then
			ColonyPlatformsBeingReplaced[p.InternalName] = true
			Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(7,16)), function()
				local wormhole = Actor.Create("wormhole", true, { Owner = p, Location = McvReplace.Location })
				Trigger.AfterDelay(DateTime.Seconds(1), function()
					if p.IsLocalPlayer then
						Media.PlaySpeechNotification(p, "ReinforcementsArrived")
						Notification("Reinforcements have arrived.")
						Beacon.New(p, McvReplace.CenterPosition)
					end
					Reinforcements.Reinforce(p, { "smcv" }, { McvReplace.Location })
					Trigger.AfterDelay(1, function()
						ColonyPlatformsBeingReplaced[p.InternalName] = false
					end)
					if DateTime.GameTime < DateTime.Minutes(1) then
						p.Cash = p.Cash + 2500
					end
				end)
				Trigger.AfterDelay(DateTime.Seconds(3), function()
					wormhole.Kill()
				end)
			end)
		end
	end)
end
