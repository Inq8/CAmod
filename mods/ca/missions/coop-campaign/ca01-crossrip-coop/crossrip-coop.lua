
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	Scrin = Player.GetPlayer("Scrin")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { USSR, Scrin }

    ORAMod = "ca"
	coopInfo =
	{
		Mainplayer = Greece, -- The original single player player
		Dummyplayer = Greece,
		MainEnemies = MissionEnemies,
	}
	CoopInit25(coopInfo)

	Utils.Do(MissionPlayers, function(p)
		if p == MissionPlayers[1] then
			PlayerMcv.Owner = p
		else
			if #p.GetActorsByType("mcv") == 0 then
				local mcv = Actor.Create("mcv", true, { Owner = p, Location = PlayerMcv.Location })
				mcv.Scatter()
			end
		end
	end)
end

AfterWorldLoaded = function()

end

AfterTick = function()

end
