
SetupPlayers = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	Scrin = Player.GetPlayer("Scrin")
	Neutral = Player.GetPlayer("Neutral")

	DummyGuy = Player.GetPlayer("DummyGuy")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")

	MissionPlayers = { Greece, Multi1, Multi2, Multi3, Multi4, Multi5 }
	MissionEnemies = { USSR, Scrin }
    ReinforcementsPlayer = DummyGuy

    ORAMod = "ca"
	coopInfo =
	{
		Mainplayer = MissionPlayers[1],
		MainEnemies = MissionEnemies,
		Dummyplayer = DummyGuy
	}
	CoopInit25(coopInfo)

	Utils.Do(MissionPlayers, function(p)
		if #p.GetActorsByType("mcv") == 0 then
			local mcv = Actor.Create("mcv", true, { Owner = p, Location = PlayerMcv.Location })
			mcv.Scatter()
		end
	end)
end

AfterWorldLoaded = function()

end

AfterTick = function()

end
