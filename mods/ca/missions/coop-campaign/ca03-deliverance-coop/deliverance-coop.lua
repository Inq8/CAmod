ExtraPrerequisiteFactions = { "gdi" }

SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Greece = Player.GetPlayer("Greece")
	GDI = Player.GetPlayer("GDI")
	USSR = Player.GetPlayer("USSR")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { USSR }
	SinglePlayerPlayer = Greece
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(3500)
end

AfterTick = function()

end

TransferGDIUnits = function()
	local gdiForces = GDI.GetActors()
	Utils.Do(gdiForces, function(a)
		if a.Type ~= "player" then
			a.Owner = Greece
		end
	end)

	Trigger.AfterDelay(1, function()
		TransferBaseToPlayer(SinglePlayerPlayer, GetFirstActivePlayer())
		CACoopQueueSyncer()
	end)
end

DoMcvArrival = function()
	Reinforcements.Reinforce(Greece, { "2tnk", "2tnk" }, { McvEntry.Location, McvRally.Location }, 75)
	local delay = 150
	Utils.Do(GetMcvPlayers(), function(p)
		Trigger.AfterDelay(delay, function()
			Reinforcements.Reinforce(p, { "mcv" }, { McvEntry.Location, McvRally.Location })
		end)
		delay = delay + DateTime.Seconds(1)
	end)
end
