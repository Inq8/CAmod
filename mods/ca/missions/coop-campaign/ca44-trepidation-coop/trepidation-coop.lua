
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
	England = Player.GetPlayer("England")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { USSR, Scrin }
	SinglePlayerPlayer = Greece
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(3000)

	if #MissionPlayers > 4 then
		Actor.Create("ctnk", true, { Owner = Greece, Location = CPos.New(84, 95) })

		if #MissionPlayers > 5 then
			Actor.Create("ctnk", true, { Owner = Greece, Location = CPos.New(92, 90) })
		end
	end
end

AfterTick = function()

end

CreateSpy = function()
	local spyPlayer
	if MissionPlayers[2] ~= nil then
		spyPlayer = MissionPlayers[2]
	else
		spyPlayer = Greece
	end

	Spy = Actor.Create("spy.noinfil", true, { Owner = spyPlayer, Location = Gateway.Location })
end

DoMcvArrival = function()
	local interval = 75
	local delay = 1

	Utils.Do(GetMcvPlayers(), function(p)
		Trigger.AfterDelay(delay, function()
			Reinforcements.Reinforce(p, { "mcv" }, { McvSpawn.Location, McvDest.Location })
		end)
		delay = delay + interval
	end)

	Trigger.AfterDelay(delay, function()
		local defenders = { "2tnk", "2tnk", "arty", "arty", "e1", "e1", "e1", "e1", "e3", "medi" }
		Reinforcements.Reinforce(Greece, defenders, { McvSpawn.Location, McvDest.Location }, interval)
	end)
end
