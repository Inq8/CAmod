
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Greece = Player.GetPlayer("Greece")
	Nod = Player.GetPlayer("Nod")
	England = Player.GetPlayer("England")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Nod }
	SinglePlayerPlayer = Greece
	StopSpread = true
	TechShared = false
	CoopInit()
	
end

AfterWorldLoaded = function()
	SpreadType = Map.LobbyOptionOrDefault("squadcomp", "class")

	if SpreadType == "class" then
		local teamSnipers = Greece.GetActorsByType("snip")
		local teamMirages = Greece.GetActorsByType("rtnk")
		local teamRangers = Greece.GetActorsByType("jeep")
		local teamAPCs = Greece.GetActorsByType("apc")
		local teamHealers = Greece.GetActorsByTypes({"medi","mech"})
		Utils.Do(teamSnipers,function(UID)
			UID.Owner = MissionPlayers[1]
		end)
		Utils.Do(teamMirages,function(UID)
			UID.Owner = MissionPlayers[1]
		end)
		Utils.Do(teamRangers,function(UID)
			UID.Owner = MissionPlayers[1]
		end)
		Utils.Do(teamAPCs,function(UID)
			UID.Owner = MissionPlayers[1]
		end)
		Utils.Do(teamHealers,function(UID)
			UID.Owner = MissionPlayers[1]
		end)

		if #MissionPlayers >= 2 then
			Utils.Do(teamSnipers,function(UID)
				UID.Owner = MissionPlayers[2]
			end)
			Utils.Do(teamHealers,function(UID)
				UID.Owner = MissionPlayers[2]
			end)
			Utils.Do(teamAPCs,function(UID)
				UID.Owner = MissionPlayers[2]
			end)
		end
		if #MissionPlayers >= 3 then
			Utils.Do(teamRangers,function(UID)
				UID.Owner = MissionPlayers[3]
			end)
			Utils.Do(teamAPCs,function(UID)
				UID.Owner = MissionPlayers[3]
			end)
			Utils.Do(teamHealers,function(UID)
				UID.Owner = MissionPlayers[3]
			end)
		end
		if #MissionPlayers >= 4 then
			Utils.Do(teamRangers,function(UID)
				UID.Owner = MissionPlayers[4]
			end)
		end
		if #MissionPlayers >= 5 then
			local SpreadIterator = 5
			Utils.Do(teamSnipers,function(UID)
				if SpreadIterator == 5 then
					UID.Owner = MissionPlayers[SpreadIterator]
					SpreadIterator = 2
				else
					UID.Owner = MissionPlayers[SpreadIterator]
					SpreadIterator = 5
				end
			end)
		end
		if #MissionPlayers >= 6 then
			local SpreadIterator = 6
			Utils.Do(teamMirages,function(UID)
				if SpreadIterator == 6 then
					UID.Owner = MissionPlayers[SpreadIterator]
					SpreadIterator = 1
				else
					UID.Owner = MissionPlayers[SpreadIterator]
					SpreadIterator = 6
				end
			end)
		end
	else
		local allUnits = Greece.GetActorsByTypes({ "snip", "rtnk", "jeep", "apc", "medi", "mech" })
		AssignToCoopPlayers()
	end
end

AfterTick = function()

end
