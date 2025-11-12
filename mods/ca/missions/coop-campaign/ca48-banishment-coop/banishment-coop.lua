
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Greece = Player.GetPlayer("Greece")
	MaleficScrin = Player.GetPlayer("MaleficScrin")
	England = Player.GetPlayer("England")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { MaleficScrin }
	SinglePlayerPlayer = Greece
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(3000)
end

AfterTick = function()

end

TransferBaseActors = function(base)
	local recipientPlayer = GetFirstActivePlayer()
	local highestAssetValue = 0

	Utils.Do(GetMcvPlayers(), function(p)
		local playerAssetsNearby = Utils.Where(Map.ActorsInCircle(base.Center.CenterPosition, WDist.New(20 * 1024)), function(a)
			return a.Owner == p
		end)

		local playerAssetValue = GetTotalCostOfUnits(playerAssetsNearby)
		if playerAssetValue > highestAssetValue then
			highestAssetValue = playerAssetValue
			recipientPlayer = p
		end
	end)

	local baseActors = Map.ActorsInBox(base.TopLeft.CenterPosition, base.BottomRight.CenterPosition, function(a)
		return not a.IsDead and (a.Owner == England or a.Type == "macs" or a.Type == "hosp")
	end)

	if base.Name == "McvBase" then
		local otherMcvPlayers = Utils.Where(GetMcvPlayers(), function(p) return p ~= recipientPlayer end)
		Utils.Do(otherMcvPlayers, function(p)
			local mcv = Actor.Create("mcv", true, { Owner = p, Location = AlliedMcv.Location })
			mcv.Scatter()
		end)
	end

	Utils.Do(baseActors, function(a)
		a.Owner = recipientPlayer
	end)
end

DoMcvArrival = function()
	local delay = 0
	Utils.Do(GetMcvPlayers(), function(p)
		Trigger.AfterDelay(delay, function()
			Reinforcements.Reinforce(p, { "lst.mcv" }, { McvSpawn.Location, McvRally.Location })
		end)
		delay = delay + DateTime.Seconds(3)
	end)
end
