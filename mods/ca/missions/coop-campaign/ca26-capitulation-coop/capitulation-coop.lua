
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	GDI = Player.GetPlayer("GDI")
	USSR = Player.GetPlayer("USSR")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { USSR }
	SinglePlayerPlayer = GDI
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(3000)

	local StartAPCs = {Actor1010, Actor1015}
	Utils.Do(StartAPCs, function(UID)
		UID.UnloadPassengers()
	end)
end

AfterTick = function()

end

DoMcvArrival = function()
	local delay = 0
	Utils.Do(GetMcvPlayers(), function(p)
		Trigger.AfterDelay(delay, function()
			Reinforcements.Reinforce(p, { "amcv" }, { McvSpawn.Location, PlayerStart.Location })
		end)
		delay = delay + DateTime.Seconds(1)
	end)
end
