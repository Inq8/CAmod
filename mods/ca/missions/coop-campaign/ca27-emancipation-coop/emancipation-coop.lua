
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	GDI = Player.GetPlayer("GDI")
	Scrin = Player.GetPlayer("Scrin")
	GDISlaves = Player.GetPlayer("GDISlaves")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Scrin }
	SinglePlayerPlayer = GDI
	CoopInit()
end

AfterWorldLoaded = function()
	local x = InitialSensor.Location.X
	local facing = InitialSensor.Facing
	InitialSensor.Destroy()
	Utils.Do(MissionPlayers, function(p)
		local extraSensor = Actor.Create("msar", true, { Owner = p, Location = CPos.New(x, 2), DeployState = 2, Facing = facing })
		x = x + 2
	end)
end

AfterTick = function()

end

FreeSlaves = function(slaves)
	local baseSlaves = Utils.Where(slaves, function(s) return IsBaseTransferActor(a) end)
	local otherSlaves = Utils.Where(slaves, function(s) return s.HasProperty("Move") and not IsHarvester(s) end)
	local firstActivePlayer = GetFirstActivePlayer()

	Utils.Do(baseSlaves, function(s)
		if not s.IsDead then
			s.Owner = firstActivePlayer
			Trigger.AfterDelay(1, function()
				if not s.IsDead then
					if s.HasProperty("Move") then
						s.Stop()
					end
					if s.HasProperty("FindResources") then
						s.FindResources()
					end
				end
			end)
		end
	end)

	Utils.Do(otherSlaves, function(s)
		if not s.IsDead then
			s.Owner = GDI
			Trigger.AfterDelay(1, function()
				if not s.IsDead then
					if s.HasProperty("Move") then
						s.Stop()
					end
				end
			end)
		end
	end)

	CACoopQueueSyncer()
end
