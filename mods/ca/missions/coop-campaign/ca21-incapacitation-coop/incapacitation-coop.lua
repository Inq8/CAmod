
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
	if IsHardOrAbove() then
		local extraIntruder1 = Actor.Create("s4", true, { Owner = Scrin, Location = CPos.New(5, 30), Facing = Angle.East })
		extraIntruder1.GrantCondition("difficulty-" .. Difficulty)
		IntruderDeathTrigger(extraIntruder1)

		if #MissionPlayers >= 5 then
			local extraIntruder2 = Actor.Create("s4", true, { Owner = Scrin, Location = CPos.New(5, 32), Facing = Angle.East })
			extraIntruder2.GrantCondition("difficulty-" .. Difficulty)
			IntruderDeathTrigger(extraIntruder2)

			if #MissionPlayers >= 6 then
				local extraIntruder3 = Actor.Create("s4", true, { Owner = Scrin, Location = CPos.New(5, 31), Facing = Angle.East })
				extraIntruder3.GrantCondition("difficulty-" .. Difficulty)
				IntruderDeathTrigger(extraIntruder3)
			end
		end
	end
end

AfterTick = function()

end
