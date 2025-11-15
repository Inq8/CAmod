
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Scrin = Player.GetPlayer("Scrin")
	Greece = Player.GetPlayer("Greece")
	GDI = Player.GetPlayer("GDI")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Greece, GDI }
	SinglePlayerPlayer = Scrin
	CoopInit()
end

AfterWorldLoaded = function()
	local extraLocations = {
		CPos.New(5, 30),
		CPos.New(5, 32)
	}

	if IsHardOrAbove() and #MissionPlayers >= 5 then
		-- spawn an extra intruder for each extra player above 4
		for i = 1, #MissionPlayers - 4 do
			local loc = extraLocations[i]
			local extraIntruder = Actor.Create("s4", true, { Owner = Scrin, Location = loc, Facing = Angle.East })
			extraIntruder.GrantCondition("difficulty-" .. Difficulty)
			IntruderDeathTrigger(extraIntruder)
		end
	end
end

AfterTick = function()

end
