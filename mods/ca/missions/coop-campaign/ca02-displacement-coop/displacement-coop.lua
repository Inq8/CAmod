
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Greece = Player.GetPlayer("Greece")
	England = Player.GetPlayer("England")
	Scrin = Player.GetPlayer("Scrin")
	USSR = Player.GetPlayer("USSR")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Scrin }
	SinglePlayerPlayer = Greece
	CoopInit()
end

AfterWorldLoaded = function()
	TransferBaseToPlayer(SinglePlayerPlayer, GetFirstActivePlayer())
	StartCashSpread(0)
end

AfterTick = function()
	Utils.Do(CoopPlayers,function(PID)
		if PID ~= CoopPlayers[1] then
			Utils.Do(PID.GetActorsByTypes({"harv","proc","powr","silo"}),function(UID)
				UID.Owner = CoopPlayers[1]
			end)
		end
	end)
end
