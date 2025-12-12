
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	USSR = Player.GetPlayer("USSR")
	Scrin = Player.GetPlayer("Scrin")
	Nod = Player.GetPlayer("Nod")
	ScrinRebels = Player.GetPlayer("ScrinRebels")
	ScrinRebelsOuter = Player.GetPlayer("ScrinRebelsOuter")
	MaleficScrin = Player.GetPlayer("MaleficScrin")
	SpyPlaneProvider = Player.GetPlayer("SpyPlaneProvider")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Nod, ScrinRebels, MaleficScrin }
	SinglePlayerPlayer = USSR
	CoopInit()
end

AfterWorldLoaded = function()
	local TeslaPlants = USSR.GetActorsByType("tpwr")

	if #MissionPlayers >= 2 then
		TeslaPlants[1].Owner = MissionPlayers[2]
		Actor1724.Owner = MissionPlayers[2]
		Actor820.Owner = MissionPlayers[2]
	end
	if #MissionPlayers >= 3 then
		TeslaPlants[2].Owner = MissionPlayers[3]
		Actor1803.Owner = MissionPlayers[3]
		Actor821.Owner = MissionPlayers[3]
	end
	if #MissionPlayers >= 4 then
		TeslaPlants[3].Owner = MissionPlayers[4]
		Actor1610.Owner = MissionPlayers[4]
		Actor822.Owner = MissionPlayers[4]
		Actor823.Owner = MissionPlayers[4]
	end
	if #MissionPlayers >= 5 then
		TeslaPlants[4].Owner = MissionPlayers[5]
		Actor1846.Owner = MissionPlayers[5]
		Actor823.Owner = MissionPlayers[5]
	end
	if #MissionPlayers >= 6 then
		TeslaPlants[5].Owner = MissionPlayers[6]
		Actor1611.Owner = MissionPlayers[6]
	end

	TransferBaseToPlayer(SinglePlayerPlayer, GetFirstActivePlayer())
	StartCashSpread(3500)
end

AfterTick = function()

end

TransferExterminator = function()
	Exterminator.Owner = GetFirstActivePlayer()
end

DoMcvArrival = function()
	local delay = 0
	Utils.Do(GetMcvPlayers(), function(p)
		Trigger.AfterDelay(delay, function()
			Reinforcements.Reinforce(p, { "mcv" }, { McvSpawn.Location, McvDest.Location })
		end)
		delay = delay + DateTime.Seconds(1)
	end)
end
