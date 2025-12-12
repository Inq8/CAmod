
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Scrin = Player.GetPlayer("Scrin")
	GDI = Player.GetPlayer("GDI")
	TibLifeforms = Player.GetPlayer("TibLifeforms")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Scrin }
	SinglePlayerPlayer = GDI
	CoopInit()
end

AfterWorldLoaded = function()
	local firstActivePlayer = GetFirstActivePlayer()
	TransferBaseToPlayer(SinglePlayerPlayer, firstActivePlayer)
	StartCashSpread(3500)

	local x = 46
	Utils.Do(GetMcvPlayers(), function(p)
		if p ~= firstActivePlayer then
			Reinforcements.Reinforce(p, { "amcv" }, { CPos.New(x, 96), CPos.New(x, 91)})
			x = x + 2
		end
	end)
end

AfterTick = function()

end
