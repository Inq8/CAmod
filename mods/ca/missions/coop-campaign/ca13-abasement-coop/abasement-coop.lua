
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	USSR = Player.GetPlayer("USSR")
	Nod = Player.GetPlayer("Nod")
	NodAbandoned = Player.GetPlayer("NodAbandoned")
	Scrin = Player.GetPlayer("Scrin")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Scrin }
	SinglePlayerPlayer = Nod
	CoopInit()
end

AfterWorldLoaded = function()

end

AfterTick = function()

end
