
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
	TransferBaseToPlayer(SinglePlayerPlayer, GetFirstActivePlayer())
	StartCashSpread(3000)
end

AfterTick = function()

end
