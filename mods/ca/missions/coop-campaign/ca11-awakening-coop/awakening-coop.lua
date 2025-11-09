
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
	local initialHarvesters = Nod.GetActorsByType("harv.td")
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

	TemplePrime.Owner = firstActivePlayer
	Utils.Do(CyborgFactories, function(f)
		f.Owner = firstActivePlayer
	end)

	if #MissionPlayers >= 2 then
		Actor202.Owner = MissionPlayers[2]
		Trigger.AfterDelay(2, function()
			initialHarvesters[2].Owner = MissionPlayers[2]
		end)
	end
end

AfterTick = function()

end
