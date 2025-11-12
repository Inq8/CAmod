
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Greece = Player.GetPlayer("Greece")
	GDI = Player.GetPlayer("GDI")
	Scrin = Player.GetPlayer("Scrin")
	England = Player.GetPlayer("England")
	USSR = Player.GetPlayer("USSR")
	Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { GDI }
	SinglePlayerPlayer = Greece
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(3000)
end

AfterTick = function()

end

DoMcvArrival = function()
	local x = 70
	Utils.Do(GetMcvPlayers(), function(p)
		Reinforcements.Reinforce(p, { "mcv" }, { CPos.New(x, 132), CPos.New(x, 128)})
		x = x + 2
	end)
end

TransferAlliedAssets = function()
	local recipientPlayer = GetFirstActivePlayer()
	local highestAssetValue = 0

	Utils.Do(GetMcvPlayers(), function(p)
		local playerAssetsNearby = Utils.Where(Map.ActorsInCircle(AlliedBaseCenter.CenterPosition, WDist.New(20 * 1024)), function(a)
			return a.Owner == p
		end)

		local playerAssetValue = GetTotalCostOfUnits(playerAssetsNearby)
		if playerAssetValue > highestAssetValue then
			highestAssetValue = playerAssetValue
			recipientPlayer = p
		end
	end)

	TransferBaseToPlayer(England, recipientPlayer)
end

TransferSovietAssets = function()
	local recipientPlayer = GetFirstActivePlayer()
	local highestAssetValue = 0

	Utils.Do(GetMcvPlayers(), function(p)
		local playerAssetsNearby = Utils.Where(Map.ActorsInCircle(SovietBaseCenter.CenterPosition, WDist.New(20 * 1024)), function(a)
			return a.Owner == p
		end)

		local playerAssetValue = GetTotalCostOfUnits(playerAssetsNearby)
		if playerAssetValue > highestAssetValue then
			highestAssetValue = playerAssetValue
			recipientPlayer = p
		end
	end)

	TransferBaseToPlayer(USSR, recipientPlayer)
end

TransferNodAssets = function()
	local recipientPlayer = GetFirstActivePlayer()
	local highestAssetValue = 0

	Utils.Do(GetMcvPlayers(), function(p)
		local playerAssetsNearby = Utils.Where(Map.ActorsInCircle(NodBaseCenter.CenterPosition, WDist.New(20 * 1024)), function(a)
			return a.Owner == p
		end)

		local playerAssetValue = GetTotalCostOfUnits(playerAssetsNearby)
		if playerAssetValue > highestAssetValue then
			highestAssetValue = playerAssetValue
			recipientPlayer = p
		end
	end)

	TransferBaseToPlayer(Nod, recipientPlayer)
end
