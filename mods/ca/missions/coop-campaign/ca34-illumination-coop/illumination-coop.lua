
SetupPlayers = function()
	Scrin = Player.GetPlayer("Scrin")
	Nod = Player.GetPlayer("Nod")
	TibLifeforms = Player.GetPlayer("TibLifeforms")
	Neutral = Player.GetPlayer("Neutral")

	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")

	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Scrin }

    ORAMod = "ca"
	coopInfo =
	{
		Mainplayer = Nod, -- The original single player player
		Dummyplayer = Nod,
		MainEnemies = MissionEnemies,
	}
	CoopInit25(coopInfo)
end

AfterWorldLoaded = function()

end

AfterTick = function()

end
