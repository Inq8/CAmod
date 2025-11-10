
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
	local firstActivePlayer = GetFirstActivePlayer()
	local MSAIterator = 2
	Utils.Do(MissionPlayers, function(p)
		if p ~= firstActivePlayer then
			local ExtraMSA = Actor.Create(Actor196.Type, true, { Owner = p, Location = (Actor196.Location+CVec.New((MSAIterator), 0)) })
			MSAIterator = MSAIterator + 2
		end
	end)
end

AfterTick = function()

end

FreeSlaves = function(slaves)
	local baseSlaves = Utils.Where(slaves, function(s) return s.HasProperty("StartBuildingRepairs") or IsHarvester(s) or Utils.Any(WallTypes, function(t) return s.Type == t end)
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
end
