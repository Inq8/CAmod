
Fields = {
	{ Reinforced = false, Waypoint = NWField, Reinforcements = { "gunw", "intl", "s1", "s1", "s3", "s1", "s1", "s3", "s1", "s4" } },
	{ Reinforced = false, Waypoint = NField, Reinforcements = { "devo", "corr", "s1", "s1", "s3", "s1", "s2", "s1", "s1", "s3" } },
	{ Reinforced = false, Waypoint = NEField, Reinforcements = { "rtpd", "lchr", "s4", "s4", "s2", "s1", "s1", "s1", "s1", "s1" } },
	{ Reinforced = false, Waypoint = SEField, Reinforcements = { "tpod", "seek", "shrw", "s1", "s1", "s3", "s2", "s1", "s1", "s1" } },
	{ Reinforced = false, Waypoint = SField, Reinforcements = { "devo", "shrw", "seek", "s3", "s3", "s4", "s1", "s1", "s1", "s1" } },
	{ Reinforced = true, Waypoint = SWField, Reinforcements = nil }
}

AirReinforcements = {
	{
		SAMSites = { NWSAM1, NWSAM2 },
		Spawn = RaidSpawn1,
		Dest = NWField,
	},
	{
		SAMSites = { SSAM1, SSAM2 },
		Spawn = RaidSpawn8,
		Dest = SField,
	},
	{
		SAMSites = { SESAM1, SESAM2 },
		Spawn = RaidSpawn6,
		Dest = SEField,
	},
	{
		SAMSites = { NSAM1, NSAM2 },
		Spawn = NAirSpawn,
		Dest = NField,
	},
	{
		SAMSites = { RiverSAM1, RiverSAM2, RiverSAM3, RiverSAM4 },
		Spawn = SAirSpawn,
		Dest = SAirDest,
	},
}

NodHarvestActors = { NWRef, NRef, NERef, SRef1, SRef2, SERef1, SERef2, NWHarv, NHarv, NEHarv, SWHarv1, SWHarv2, SEHarv1, SEHarv2 }

MaintenanceDuration = {
	easy = DateTime.Minutes(4),
	normal = DateTime.Minutes(4),
	hard = DateTime.Minutes(4),
	vhard = DateTime.Minutes(4),
	brutal = DateTime.Minutes(4)
}

RaidStart = {
	easy = DateTime.Seconds(120),
	normal = DateTime.Seconds(90),
	hard = DateTime.Seconds(70),
	vhard = DateTime.Seconds(60),
	brutal = DateTime.Seconds(50)
}

RaidInterval = {
	easy = DateTime.Seconds(80),
	normal = DateTime.Seconds(70),
	hard = DateTime.Seconds(60),
	vhard = DateTime.Seconds(55),
	brutal = DateTime.Seconds(50)
}

ReinforcementInitialThreshold = {
	easy = 27500,
	normal = 35000,
	hard = 42500,
	vhard = 42500,
	brutal = 42500
}

ReinforcementFinalThreshold = {
	easy = 60000,
	normal = 70000,
	hard = 80000,
	vhard = 90000,
	brutal = 100000
}

ReinforcementThresholdIncrement = 5000

HardAndAboveCompositions = {
	{ Units = { "bike", "bike", "bggy" }, MaxTime = DateTime.Minutes(8) },

	{ Units = { "bike", "bike", "bike", "bggy", "bggy", "n1", "n1", "n3" }, MinTime = DateTime.Minutes(8) },
	{ Units = { "stnk.nod", "stnk.nod", "stnk.nod", "bggy" }, MinTime = DateTime.Minutes(8) },
	{ Units = { "ltnk", "ltnk", "ftnk", "ftnk", "n3", "n1", "n1", "n4", "n1", "n3" }, MinTime = DateTime.Minutes(8) },
	{ Units = { "arty.nod", "ltnk", "bggy", "hftk", "n4", "n4", "n1" }, MinTime = DateTime.Minutes(10) },
}

RaidCompositions = {
	easy = {
		{ Units = { "bike", "bggy" }, MaxTime = DateTime.Minutes(9) },

		{ Units = { "bike", "bike", "bggy", "n1" }, MinTime = DateTime.Minutes(9) },
		{ Units = { "stnk.nod", "bggy" }, MinTime = DateTime.Minutes(9) },
		{ Units = { "ltnk", "ftnk", "n3", "n1", "n1", "n4" }, MinTime = DateTime.Minutes(9) },
	},
	normal = {
		{ Units = { "bike", "bggy", "bggy" }, MaxTime = DateTime.Minutes(8) },

		{ Units = { "bike", "bike", "bggy", "bggy", "n1", "n3" }, MinTime = DateTime.Minutes(8) },
		{ Units = { "stnk.nod", "stnk.nod", "bggy" }, MinTime = DateTime.Minutes(8) },
		{ Units = { "ltnk", "ftnk", "ftnk", "n3", "n1", "n1", "n4", "n1" }, MinTime = DateTime.Minutes(8) },

		{ Units = { "arty.nod", "ltnk", "bggy", "n4", "n4", "n1" }, MinTime = DateTime.Minutes(10) },
	},
	hard = AdjustCompositionsForDifficulty(HardAndAboveCompositions),
	vhard = AdjustCompositionsForDifficulty(HardAndAboveCompositions),
	brutal = AdjustCompositionsForDifficulty(HardAndAboveCompositions),
}

RaidEntryPaths = {
	{ RaidSpawn1.Location, RaidDest1.Location },
	{ RaidSpawn2.Location, RaidDest2.Location },
	{ RaidSpawn3.Location, RaidDest3.Location },
	{ RaidSpawn4.Location, RaidDest4.Location },
	{ RaidSpawn5.Location, RaidDest5.Location },
	{ RaidSpawn6.Location, RaidDest6.Location },
	{ RaidSpawn7.Location, RaidDest7.Location },
	{ RaidSpawn8.Location, RaidDest8.Location },
	{ RaidSpawn9.Location, RaidDest9.Location },
	{ RaidSpawn10.Location, RaidDest10.Location },
}

Squads = {
	Air = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 10, Max = 10 }),
		Compositions = {
			easy = {
				{ Aircraft = { "scrn" } }
			},
			normal = {
				{ Aircraft = { "scrn" } },
			},
			hard = {
				{ Aircraft = { "scrn", "scrn" } },
			},
			vhard = {
				{ Aircraft = { "scrn", "scrn" } },
			},
			brutal = {
				{ Aircraft = { "scrn", "scrn", "scrn" } },
			},
		},
	},
}

WorldLoaded = function()
	Scrin = Player.GetPlayer("Scrin")
	Nod = Player.GetPlayer("Nod")
	MissionPlayers = { Scrin }
	MissionEnemies = { Nod }
	TimerTicks = MaintenanceDuration[Difficulty]
	FieldsClearedAndBeingHarvested = 0
	NextReinforcementThreshold = ReinforcementInitialThreshold[Difficulty]

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Scrin)
	InitNod()

	ObjectiveEliminateNodHarvesting = Scrin.AddObjective("Eliminate all enemy harvesting operations.")
	ObjectiveHarvestFields = Scrin.AddObjective("Establish and maintain harvesting operations\nat all six blue ichor fields.")

	Trigger.AfterDelay(DateTime.Seconds(7), function()
		Tip("A tiberium field is considered occupied when it has been cleared of Nod forces and when you have both a refinery and an active harvester nearby.")
		Trigger.AfterDelay(DateTime.Seconds(7), function()
			Tip("The more lucrative your harvesting operation becomes, the more reinforcements will be provided to you.")
		end)
	end)

	Trigger.AfterDelay(1, function()
		CheckFields()
		UpdateObjectiveMessage()
	end)

	Trigger.OnAllKilledOrCaptured(NodHarvestActors, function()
		Scrin.MarkCompletedObjective(ObjectiveEliminateNodHarvesting)
	end)

	local powerPlants = Nod.GetActorsByType("nuk2")
	local poweredDefenses = Nod.GetActorsByTypes({ "obli", "nsam" })

	Trigger.OnAllKilledOrCaptured(powerPlants, function()
		Utils.Do(poweredDefenses, function(self)
			if not self.IsDead then
				self.GrantCondition("disabled")
			end
		end)
	end)

	Utils.Do(AirReinforcements, function(r)
		Trigger.OnAllKilled(r.SAMSites, function()
			Trigger.AfterDelay(DateTime.Seconds(2), function()
				Media.PlaySpeechNotification(Scrin, "ReinforcementsArrived")
				Notification("Reinforcements have arrived.")
				Beacon.New(Scrin, r.Spawn.CenterPosition)
				Reinforcements.Reinforce(Scrin, { "stmr" }, { r.Spawn.Location, r.Dest.Location }, 25)
			end)
		end)
	end)

	AfterWorldLoaded()
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Nod.Cash = Nod.ResourceCapacity - 1000
		Nod.Resources = Nod.ResourceCapacity - 1000

		if MissionPlayersHaveNoRequiredUnits() then
			if ObjectiveEliminateNodHarvesting ~= nil and not Scrin.IsObjectiveCompleted(ObjectiveEliminateNodHarvesting) then
				Scrin.MarkFailedObjective(ObjectiveDestroyFactories)
			end
			if ObjectiveHarvestFields ~= nil and not Scrin.IsObjectiveCompleted(ObjectiveHarvestFields) then
				Scrin.MarkFailedObjective(ObjectiveHarvestFields)
			end
		end

		if FieldsClearedAndBeingHarvested == 6 and TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
				Scrin.MarkCompletedObjective(ObjectiveHarvestFields)
			end
		end

		UpdateObjectiveMessage()
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		local playerTotalFunds = Scrin.Cash + Scrin.Resources
		Scrin.Cash = 0
		Scrin.Resources = playerTotalFunds

		if Scrin.Resources >= NextReinforcementThreshold then
			Scrin.Resources = Scrin.Resources - NextReinforcementThreshold

			if NextReinforcementThreshold < ReinforcementFinalThreshold[Difficulty] then
				NextReinforcementThreshold = NextReinforcementThreshold + ReinforcementThresholdIncrement
			end

			DoReinforcements()
		end

		UpdateRaidTarget()
		CheckFields()
		CheckColonyPlatform()
	end
end

InitNod = function()
	Actor.Create("ai.unlimited.power", true, { Owner = Nod })

	AutoRepairBuildings(Nod)
	SetupRefAndSilosCaptureCredits(Nod)
	InitAiUpgrades(Nod)
	InitAirAttackSquad(Squads.Air, Nod, MissionPlayers, { "harv", "harv.td", "proc", "proc.scrin" })

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(6 * 1024), IsNodGroundHunterUnit)
	end)

	Trigger.AfterDelay(RaidStart[Difficulty], function()
		DoRaid()
	end)
end

CheckFields = function()
	PreviousFieldsClearedAndBeingHarvested = FieldsClearedAndBeingHarvested
	FieldsClearedAndBeingHarvested = 0

	Utils.Do(Fields, function(field)
		local nodStructuresCleared = true
		local hasScrinHarv = false
		local hasScrinRef = false

		local actors = Map.ActorsInCircle(field.Waypoint.CenterPosition, WDist.New(18 * 1024), function(a)
			return not a.IsDead and (a.Type == "harv" or a.Type == "harv.scrin" or a.HasProperty("StartBuildingRepairs") and a.Type ~= "nsam")
		end)

		Utils.Do(actors, function(a)
			if IsMissionPlayer(a.Owner) then
				if a.Type == "harv" or a.Type == "harv.scrin" then
					hasScrinHarv = true
				elseif a.Type == "proc.scrin" or a.Type == "proc.td" then
					hasScrinRef = true
				end
			elseif a.Owner == Nod then
				nodStructuresCleared = false
			end
		end)

		if not field.Reinforced and nodStructuresCleared then
			field.Reinforced = true
			Trigger.AfterDelay(DateTime.Seconds(2), function()
				local wormhole = Actor.Create("wormhole", true, { Owner = Scrin, Location = field.Waypoint.Location })

				Trigger.AfterDelay(DateTime.Seconds(2), function()
					Media.PlaySpeechNotification(Scrin, "ReinforcementsArrived")
					Notification("Reinforcements have arrived.")
					Beacon.New(Scrin, field.Waypoint.CenterPosition)

					local reinforcements = Reinforcements.Reinforce(Scrin, field.Reinforcements, { field.Waypoint.Location }, 10, function(a)
						a.Scatter()
					end)
				end)

				Trigger.AfterDelay(DateTime.Seconds(10), function()
					wormhole.Kill()
				end)
			end)
		end

		if hasScrinHarv and hasScrinRef and nodStructuresCleared then
			FieldsClearedAndBeingHarvested = FieldsClearedAndBeingHarvested + 1
		end
	end)

	if FieldsClearedAndBeingHarvested < PreviousFieldsClearedAndBeingHarvested then
		Notification("You have lost control of an ichor field.")
		MediaCA.PlaySound("s_ichorfieldlost.aud", 2)
	end
end

UpdateObjectiveMessage = function()
	if FieldsClearedAndBeingHarvested == 6 then
		UserInterface.SetMissionText("6 of 6 fields occupied.\n   Maintain for " .. UtilsCA.FormatTimeForGameSpeed(TimerTicks), HSLColor.Lime)
	else
		local missionText = FieldsClearedAndBeingHarvested .. " of 6 fields occupied  -  Next reinforcement threshold: $" .. NextReinforcementThreshold
		UserInterface.SetMissionText(missionText, HSLColor.Yellow)
	end
end

DoRaid = function()
	local randomEntryPath = Utils.Random(RaidEntryPaths)
	local difficultyCompositions = RaidCompositions[Difficulty]
	local validCompositions = Utils.Where(difficultyCompositions, function(c)
		return (c.MinTime == nil or DateTime.GameTime >= c.MinTime) and (c.MaxTime == nil or DateTime.GameTime <= c.MaxTime)
	end)
	if #validCompositions > 0 then
		local randomComposition = Utils.Random(validCompositions)
		local units = Reinforcements.Reinforce(Nod, randomComposition.Units, randomEntryPath, 25, function(a)
			Trigger.AfterDelay(DateTime.Seconds(2), function()
				AssaultPlayerBaseOrHunt(a)
			end)
		end)
	end

	Trigger.AfterDelay(RaidInterval[Difficulty], function()
		DoRaid()
	end)
end

UpdateRaidTarget = function()
	local possibleRaidTargets = { PlayerStart }

	Utils.Do(Fields, function(field)
		if field.Reinforced then
			table.insert(possibleRaidTargets, field.Waypoint)
		end
	end)

	local randomRaidTarget = Utils.Random(possibleRaidTargets)
	PlayerBaseLocations[Scrin.InternalName] = randomRaidTarget.Location
end

DoReinforcements = function()
	local reinforcementsWaypoint = Utils.Random({ WormholePoint1, WormholePoint2 })
	local wormhole = Actor.Create("wormhole", true, { Owner = Scrin, Location = reinforcementsWaypoint.Location })

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Media.PlaySpeechNotification(Scrin, "ReinforcementsArrived")
		Notification("Reinforcements have arrived.")
		Beacon.New(Scrin, reinforcementsWaypoint.CenterPosition)

		local reinforcements = Reinforcements.Reinforce(Scrin, { "s1", "s1", "s1", "s3", "s3", "gunw", "seek", "intl", "s1", "s1", "s4", "s1" }, { reinforcementsWaypoint.Location }, 10, function(a)
			a.Scatter()
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		wormhole.Kill()
	end)

	UpdateObjectiveMessage()
end

CheckColonyPlatform = function()
	local colonyPlatformsAndMcvs = Scrin.GetActorsByTypes({ "smcv", "sfac" })
	if #colonyPlatformsAndMcvs == 0 and not ColonyPlatformBeingReplaced then
		ColonyPlatformBeingReplaced = true
		Trigger.AfterDelay(DateTime.Seconds(15), function()
			local wormhole = Actor.Create("wormhole", true, { Owner = Scrin, Location = McvReplace.Location })

			Trigger.AfterDelay(DateTime.Seconds(2), function()
				Media.PlaySpeechNotification(Scrin, "ReinforcementsArrived")
				Notification("Reinforcements have arrived.")
				Beacon.New(Scrin, McvReplace.CenterPosition)
				ColonyPlatformBeingReplaced = false
				Reinforcements.Reinforce(Scrin, { "smcv" }, { McvReplace.Location })
			end)

			Trigger.AfterDelay(DateTime.Seconds(5), function()
				wormhole.Kill()
			end)
		end)
	end
end
