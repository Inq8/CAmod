
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Nod = Player.GetPlayer("Nod")
	Scrin = Player.GetPlayer("Scrin")
	ScrinRebels = Player.GetPlayer("ScrinRebels")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Scrin }
	SinglePlayerPlayer = Nod
	CoopInit()
end

AfterWorldLoaded = function()
	local firstActivePlayer = GetFirstActivePlayer()
	TransferBaseToPlayer(SinglePlayerPlayer, firstActivePlayer)
	StartCashSpread(3500)

	local mcvPath = { CPos.New(74,1), CPos.New(72, 6) }

	Utils.Do(GetMcvPlayers(), function(p)
		if p ~= firstActivePlayer then
			Reinforcements.Reinforce(p, { "amcv" }, mcvPath)
		end
	end)
end

AfterTick = function()

end
