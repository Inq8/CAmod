
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	USSR = Player.GetPlayer("USSR")
	Greece = Player.GetPlayer("Greece")
	Traitor = Player.GetPlayer("Traitor")
	USSRAbandoned = Player.GetPlayer("USSRAbandoned")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Greece, Traitor }
	SinglePlayerPlayer = USSR
	StopSpread = true
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(2000)
	local engineers = USSR.GetActorsByType("e6")
	local firstActivePlayer = GetFirstActivePlayer()
	Boris.Owner = firstActivePlayer
	engineers[1].Owner = firstActivePlayer

	if #MissionPlayers > 1 then
		engineers[2].Owner = MissionPlayers[2]
	end

	local x = 88
	Utils.Do(MissionPlayers, function(p)
		if p ~= firstActivePlayer then
			local loc = CPos.New(x, 4)
			Actor.Create("bori.clone", true, { Owner = p, Location = loc, Facing = Angle.South })
			x = x - 2
		end
	end)
end

AfterTick = function()

end
