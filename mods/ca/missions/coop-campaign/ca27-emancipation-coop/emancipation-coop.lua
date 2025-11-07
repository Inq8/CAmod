
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	GDI = Player.GetPlayer("GDI")
	Scrin = Player.GetPlayer("Scrin")
	GDISlaves = Player.GetPlayer("GDISlaves")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Scrin }

	ORAMod = "ca"
	coopInfo =
	{
		Mainplayer = GDI, -- The original single player player
		Dummyplayer = GDI,
		MainEnemies = MissionEnemies,
	}
	CoopInit25(coopInfo)
end

AfterWorldLoaded = function()

end

AfterTick = function()

end
