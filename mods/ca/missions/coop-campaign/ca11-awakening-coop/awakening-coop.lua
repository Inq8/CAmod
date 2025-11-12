
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Nod = Player.GetPlayer("Nod")
	USSR = Player.GetPlayer("USSR")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { USSR }
	SinglePlayerPlayer = Nod
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(2000)

	local firstActivePlayer = GetFirstActivePlayer()
	local EnergyBuildingslist = Nod.GetActorsByTypes({"nuk2"})
	local DefenseBuildingslist = Nod.GetActorsByTypes({"gun.nod","obli","nsam","ltur"})
	local ProducerBuildingslist = Nod.GetActorsByTypes({"hand","airs","rep","hpad.td"})

	TransferBaseToPlayer(SinglePlayerPlayer, firstActivePlayer)

	local mcvSpawnCell = CPos.New(60, 65)
	Utils.Do(GetMcvPlayers(), function(p)
		if p ~= firstActivePlayer then
			local mcv = Actor.Create("amcv", true, { Owner = p, Location = mcvSpawnCell })
			mcv.Scatter()
		end
	end)

	if #MissionPlayers >= 6 then
		--table.remove(EnergyBuildingslist, 1)
	end

	table.sort(DefenseBuildingslist, function(a, b)
		return a.Type < b.Type
	end)

	table.sort(ProducerBuildingslist, function(a, b)
		return a.Type < b.Type
	end)

	local BuildinglistList = {EnergyBuildingslist, DefenseBuildingslist, ProducerBuildingslist}

	local Assignmentiterator = 1
	Utils.Do(BuildinglistList,function(AID)
		Utils.Do(AID,function(BID)
			if Assignmentiterator > #MissionPlayers then
				Assignmentiterator = 1
			end
			BID.Owner = MissionPlayers[Assignmentiterator]
			Assignmentiterator = Assignmentiterator + 1
		end)
	end)

	if #MissionPlayers >= 2 then
		Actor202.Owner = MissionPlayers[2]
		Trigger.AfterDelay(5, function()
			local initialHarvesters = firstActivePlayer.GetActorsByType("harv.td")
			initialHarvesters[2].Owner = MissionPlayers[2]
			
			TemplePrime.Owner = Nod
			Utils.Do(CyborgFactories, function(f)
				f.Owner = Nod
			end)
			
		end)
	end
	
	Trigger.AfterDelay(10, function()
		WaveAdjuster()
	end)
end

WaveAdjuster = function()
	Groundinterv = Map.LobbyOption("groundint")
	Airinterv = Map.LobbyOption("airint")
	
	if Groundinterv == 999 then
		Groundinterv = 1 - (0.1 * #CoopPlayers)
	end
	if Groundinterv == 998 then
		Groundinterv = 1 - (0.15 * #CoopPlayers)
	end
	
	if Airinterv == 999 then
		Airinterv = 1 - (0.1 * #CoopPlayers)
	end
	if Airinterv == 998 then
		Airinterv = 1 - (0.15 * #CoopPlayers)
	end
	
	GroundAttackInterval[Difficulty] = GroundAttackInterval[Difficulty] * Groundinterv
	HaloDropStart = HaloDropStart * Groundinterv
	AirAttackInterval[Difficulty] = AirAttackInterval[Difficulty] * Airinterv
	
end

AfterTick = function()

end
