
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Scrin = Player.GetPlayer("Scrin")
	Greece = Player.GetPlayer("Greece")
	GDI = Player.GetPlayer("GDI")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Greece, GDI }
	SinglePlayerPlayer = Scrin
	CoopInit()
end

AfterWorldLoaded = function()
	local firstActivePlayer = GetFirstActivePlayer()
	TransferBaseToPlayer(SinglePlayerPlayer, firstActivePlayer)
	StartCashSpread(3500)

	local mcvSpawnCell = CPos.New(45, 3)
	Utils.Do(GetMcvPlayers(), function(p)
		if p ~= firstActivePlayer then
			local mcv = Actor.Create("smcv", true, { Owner = p, Location = mcvSpawnCell })
			mcv.Scatter()
		end
	end)
end

AfterTick = function()

end
