
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Greece = Player.GetPlayer("Greece")
	Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Nod }
	SinglePlayerPlayer = Greece
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(3500)
end

AfterTick = function()

end

DoMcvArrival = function()
	InitialUnits = {
		easy = { "jeep", "2tnk", "e1", "e1", "e1", "e3" },
		normal = { "jeep", "e1", "e1", "e1", "e3"  },
		hard = { "jeep", "e1", "e1", "e3" },
		vhard = { "jeep", "e1", "e1" },
		brutal = { "jeep" }
	}

	while #InitialUnits[Difficulty] <= #MissionPlayers do
		table.insert(InitialUnits[Difficulty], "e1")
	end

	local mcvArrivalPath = { McvEntry.Location, McvLanding.Location }
	local mcvExitPath = { McvEntry.Location }
	local reinforcements = DoNavalTransportDrop(Greece, mcvArrivalPath, mcvExitPath, "lst.reinforce", InitialUnits[Difficulty], function(a)
		a.Move(McvRally.Location)
	end)
	local transport = reinforcements[1]

	Trigger.AfterDelay(2, function()
		Utils.Do(transport.Passengers, function(p)
			if not IsMcv(p) then
				p.Owner = Greece
			end
		end)
		Utils.Do(GetMcvPlayers(), function(p)
			local mcv = Actor.Create("mcv", false, { Owner = p })
			transport.LoadPassenger(mcv)
		end)
	end)
end
