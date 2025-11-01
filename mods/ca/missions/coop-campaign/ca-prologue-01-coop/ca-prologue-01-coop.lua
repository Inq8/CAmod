
SetupPlayers = function()
	Greece = Player.GetPlayer("Greece")
	England = Player.GetPlayer("England")
	USSR = Player.GetPlayer("USSR")
	Civilians = Player.GetPlayer("Civilians")

	DummyGuy = Player.GetPlayer("DummyGuy")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")

	MissionPlayers = { Greece, Multi1, Multi2, Multi3, Multi4, Multi5 }
	MissionEnemies = { USSR }
	ReinforcementsPlayer = DummyGuy

	ORAMod = "ca"
	coopInfo =
	{
		Mainplayer = MissionPlayers[1],
		MainEnemies = MissionEnemies,
		Dummyplayer = DummyGuy
	}
	CoopInit25(coopInfo)
end

AfterWorldLoaded = function()

end

AfterTick = function()

end
