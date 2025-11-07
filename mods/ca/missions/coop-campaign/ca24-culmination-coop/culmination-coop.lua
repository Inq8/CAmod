
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Scrin = Player.GetPlayer("Scrin")
	ScrinNoControl = Player.GetPlayer("ScrinNoControl")
	USSR = Player.GetPlayer("USSR")
	Greece = Player.GetPlayer("Greece")
	Nod = Player.GetPlayer("Nod")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { USSR, Greece, Nod }

	ORAMod = "ca"
	coopInfo =
	{
		Mainplayer = Scrin,-- The original single player player
		Dummyplayer = Scrin,
		MainEnemies = MissionEnemies,
	}
	CoopInit25(coopInfo)
end

AfterWorldLoaded = function()

end

AfterTick = function()

end
