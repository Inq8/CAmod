
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	USSR = Player.GetPlayer("USSR")
	Nod = Player.GetPlayer("Nod")
	SpyPlaneProvider = Player.GetPlayer("SpyPlaneProvider")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Nod, Nod2 }
	SinglePlayerPlayer = USSR
	CoopInit()
end

AfterWorldLoaded = function()
	TransferMcvsToPlayers()
	StartCashSpread(3500)
end

AfterTick = function()

end

SendKirovs = function()
	local firstActivePlayer = GetFirstActivePlayer()
	Reinforcements.Reinforce(firstActivePlayer, { "kiro" }, { KirovSpawn1.Location, KirovRally1.Location })

	if #MissionPlayers > 1 then
		local kirovIterator = 0
		Utils.Do(MissionPlayers, function(p)
			if p ~= firstActivePlayer then
				Reinforcements.Reinforce(p, { "kiro" }, { (KirovSpawn2.Location - CVec.New((kirovIterator), 0)), (KirovRally2.Location - CVec.New((kirovIterator), 0)) })
				kirovIterator = kirovIterator + 4
			end
		end)
	else
		Reinforcements.Reinforce(firstActivePlayer, { "kiro" }, { KirovSpawn2.Location, KirovRally2.Location })
	end
end
