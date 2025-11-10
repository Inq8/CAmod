
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	GDI = Player.GetPlayer("GDI")
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	Nod = Player.GetPlayer("Nod")
	Scrin = Player.GetPlayer("Scrin")
	AlliedSlaves = Player.GetPlayer("AlliedSlaves")
	SovietSlaves = Player.GetPlayer("SovietSlaves")
	NodSlaves = Player.GetPlayer("NodSlaves")
	CyborgSlaves = Player.GetPlayer("CyborgSlaves")
	Kane = Player.GetPlayer("Kane")
	NeutralGDI = Player.GetPlayer("NeutralGDI")
	NeutralScrin = Player.GetPlayer("NeutralScrin")
	SignalTransmitterPlayer = Player.GetPlayer("SignalTransmitterPlayer") -- separate player to prevent AI from attacking it
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Scrin, SovietSlaves, AlliedSlaves, NodSlaves }
	SinglePlayerPlayer = GDI
	CoopInit()
end

AfterWorldLoaded = function()
	local firstActivePlayer = GetFirstActivePlayer()
	TransferBaseToPlayer(SinglePlayerPlayer, firstActivePlayer)
	StartCashSpread(3000)

	local x = 66
	Utils.Do(GetMcvPlayers(), function(p)
		if p ~= firstActivePlayer then
			Reinforcements.Reinforce(p, { "amcv" }, { CPos.New(x, 132), CPos.New(x, 129)})
			x = x + 2
		end
	end)
end

AfterTick = function()

end
