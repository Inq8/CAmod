MissionDir = "ca|missions/main-campaign/ca48-banishment"

SuperweaponsEnabledTime = {
	easy = DateTime.Minutes(60),
	normal = DateTime.Minutes(40),
	hard = DateTime.Minutes(25),
	vhard = DateTime.Minutes(18),
	brutal = DateTime.Minutes(15)
}

table.insert(UnitCompositions.Scrin, {
	Infantry = { "s1", "stlk", "s1", "s1", "s1", "stlk", "stlk", "s1", "s1", "stlk", "s1", "s1", "s1", "stlk", "stlk", "stlk", "s1", "s1", "s1" },
	Vehicles = { "dark", "gunw", "dark", "dark", "gunw", "dark" },
	Aircraft = { PacOrDevastator, "pac" },
	MinTime = DateTime.Minutes(17)
})

NorthAttackPaths = {}
EastAttackPaths = {}
WestAttackPaths = {}

MainSquadAttackValues = function(squad)
	local minValue
	local maxValue

	if NumBasesSecured == 1 then
		minValue = 10
		maxValue = 20
	else
		minValue = 45 / NumScrinBasesActive
		maxValue = 85 / NumScrinBasesActive
	end

	return AdjustAttackValuesForDifficulty({ Min = minValue, Max = maxValue })
end

Squads = {
	North = {
		Compositions = UnitCompositions.Scrin,
		AttackValuePerSecond = MainSquadAttackValues,
		FollowLeader = true,
		AttackPaths = NorthAttackPaths,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(1)),
		RandomProducerActor = false,
		ProducerActors = { Infantry = { NorthPortal }, Vehicles = { NorthSphere1, NorthSphere2 }, Aircraft = { NorthGrav1, NorthGrav2 } }
	},
	West = {
		Compositions = UnitCompositions.Scrin,
		AttackValuePerSecond = MainSquadAttackValues,
		FollowLeader = true,
		AttackPaths = WestAttackPaths,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(1)),
		RandomProducerActor = false,
		ProducerActors = { Infantry = { WestPortal }, Vehicles = { WestSphere1, WestSphere2 }, Aircraft = { WestGrav } }
	},
	East = {
		Compositions = UnitCompositions.Scrin,
		AttackValuePerSecond = MainSquadAttackValues,
		FollowLeader = true,
		AttackPaths = EastAttackPaths,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(1)),
		RandomProducerActor = false,
		ProducerActors = { Infantry = { EastPortal }, Vehicles = { EastSphere }, Aircraft = { EastGrav } }
	},
	Air = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(12)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Scrin,
	},
	AirToAir = AirToAirSquad({ "stmr", "enrv", "torm" }, AdjustAirDelayForDifficulty(DateTime.Minutes(10))),
}

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	MaleficScrin = Player.GetPlayer("MaleficScrin")
	England = Player.GetPlayer("England")
	Neutral = Player.GetPlayer("Neutral")

	MissionPlayers = { Greece }

	Camera.Position = PlayerStart.CenterPosition

	NumBasesSecured = 0
	NumScrinBasesActive = 1

	InitObjectives(Greece)
	AdjustPlayerStartingCashForDifficulty()
	RemoveActorsBasedOnDifficultyTags()
	InitMaleficScrin()
	SetupLightning()
	SetupChurchMoneyCrates()

	ObjectiveSecureAllBases = Greece.AddObjective("Secure the four abandoned Allied bases.")
	ObjectiveDestroyScrinBases = Greece.AddObjective("Destroy all Scrin Nerve Centers.")

	local nerveCenters = MaleficScrin.GetActorsByType("nerv")
	Trigger.OnAllKilled(nerveCenters, function()
		Greece.MarkCompletedObjective(ObjectiveDestroyScrinBases)
	end)

	Trigger.AfterDelay(1, function()
		local englandHarvs = England.GetActorsByType("harv")
		Utils.Do(englandHarvs, function(h)
			h.Stop()
		end)

		Utils.Do({ SEBaseCenter, SWBaseCenter, NEBaseCenter, NWBaseCenter }, function(center)
			local defenders = Map.ActorsInCircle(center.CenterPosition, WDist.New(10 * 1024), function(a)
				return not a.IsDead and a.Owner == MaleficScrin and a.Type ~= "camera"
			end)

			Trigger.OnAllKilled(defenders, function()
				SecureBase(center)
			end)
		end)

		Utils.Do({ MiniBase1, MiniBase2, MiniBase3 }, function(loc)
			local miniBaseDefenders = Map.ActorsInCircle(loc.CenterPosition, WDist.New(7 * 1024), function(a)
				return not a.IsDead and a.Owner == MaleficScrin and a.Type ~= "camera"
			end)
			Trigger.OnAllKilled(miniBaseDefenders, function()
				local miniBaseActors = Map.ActorsInCircle(loc.CenterPosition, WDist.New(7 * 1024), function(a)
					return not a.IsDead and (a.Owner == England or a.Type == "macs" or a.Type == "hosp")
				end)
				Utils.Do(miniBaseActors, function(a)
					a.Owner = Greece
				end)
			end)
		end)

		BlockMcvConditionToken = WarFactory.GrantCondition("has-mcv")
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	OncePerThirtySecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		MaleficScrin.Resources = MaleficScrin.ResourceCapacity - 500

		if MissionPlayersHaveNoRequiredUnits() then
			if not Greece.IsObjectiveCompleted(ObjectiveSecureAllBases) then
				Greece.MarkFailedObjective(ObjectiveSecureAllBases)
			end
			if not Greece.IsObjectiveCompleted(ObjectiveDestroyScrinBases) then
				Greece.MarkFailedObjective(ObjectiveDestroyScrinBases)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()
	end
end

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 750 == 0 then
		CalculatePlayerCharacteristics()
	end
end

InitMaleficScrin = function()
	RebuildExcludes.MaleficScrin = { Types = { "nerv" } }

	AutoRepairAndRebuildBuildings(MaleficScrin, 10)
	SetupRefAndSilosCaptureCredits(MaleficScrin)
	AutoReplaceHarvesters(MaleficScrin)
	AutoRebuildConyards(MaleficScrin)

	local maleficScrinGroundAttackers = MaleficScrin.GetGroundAttackers()
	Utils.Do(maleficScrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)
end

SetupLightning = function()
	local nextStrikeDelay = Utils.RandomInteger(DateTime.Seconds(4), DateTime.Seconds(30))
	Trigger.AfterDelay(nextStrikeDelay, function()
		LightningStrike()
		SetupLightning()
	end)
end

LightningStrike = function()
	local duration = Utils.RandomInteger(5, 8)
	local thunderDelay = Utils.RandomInteger(5, 65)
	local soundNumber
	Lighting.Flash("LightningStrike", duration)

	repeat
		soundNumber = Utils.RandomInteger(1, 7)
	until(soundNumber ~= LastSoundNumber)
	LastSoundNumber = soundNumber

	Trigger.AfterDelay(thunderDelay, function()
		Media.PlaySound("thunder" .. soundNumber .. ".aud")
	end)
end

SecureBase = function(baseCenter)
	if baseCenter == SEBaseCenter then
		SecureSouthEastBase()
	elseif baseCenter == SWBaseCenter then
		SecureSouthWestBase()
	elseif baseCenter == NEBaseCenter then
		SecureNorthEastBase()
	elseif baseCenter == NWBaseCenter then
		SecureNorthWestBase()
	end

	NumBasesSecured = NumBasesSecured + 1

	if NumBasesSecured == 1 then
		Trigger.AfterDelay(DateTime.Seconds(20), function()
			Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
			Notification("Reinforcements have arrived. Press [" .. UtilsCA.Hotkey("ToLastEvent") .. "] to view location.")
			Reinforcements.Reinforce(Greece, { "lst.mcv" }, { McvSpawn.Location, McvDest.Location }, 75)
			Beacon.New(Greece, McvDest.CenterPosition)

			if not WarFactory.IsDead then
				WarFactory.RevokeCondition(BlockMcvConditionToken)
			end
		end)
	end

	if NumBasesSecured >= 4 then
		Greece.MarkCompletedObjective(ObjectiveSecureAllBases)
	end

	local baseActors = Map.ActorsInCircle(baseCenter.CenterPosition, WDist.New(22 * 1024), function(a)
		return not a.IsDead and a.Owner == England
	end)

	Utils.Do(baseActors, function(a)
		a.Owner = Greece
	end)
end

SecureNorthEastBase = function()
	if not NorthEastBaseSecured then
		NorthEastBaseSecured = true
		NEFlare.Destroy()

		table.insert(NorthAttackPaths, { N1a.Location, N2a.Location })
		table.insert(NorthAttackPaths, { N1b.Location, N2b.Location })
		table.insert(NorthAttackPaths, { N2d.Location })

		table.insert(EastAttackPaths, { E1.Location, E2b.Location, E3a.Location })
		table.insert(EastAttackPaths, { E1.Location, E2c.Location, E3b.Location })

		InitNorthScrinBase()
		InitEastScrinBase()
	end
end

SecureNorthWestBase = function()
	if not NorthWestBaseSecured then
		NorthWestBaseSecured = true
		NWFlare.Destroy()

		table.insert(NorthAttackPaths, { N1b.Location, N3b.Location })

		table.insert(WestAttackPaths, { W1.Location, W2a.Location })
		table.insert(WestAttackPaths, { W1.Location, W2d.Location })

		InitNorthScrinBase()
		InitWestScrinBase()
	end
end

SecureSouthEastBase = function()
	if not SouthEastBaseSecured then
		SouthEastBaseSecured = true
		SEFlare.Destroy()

		table.insert(NorthAttackPaths, { N2d.Location, N3a.Location })
		table.insert(NorthAttackPaths, { N2d.Location, N3b.Location })

		table.insert(EastAttackPaths, { E1.Location, E2a.Location })
		table.insert(EastAttackPaths, { E1.Location, E2b.Location })

		InitEastScrinBase()
		InitNorthScrinBase()
	end
end

SecureSouthWestBase = function()
	if not SouthWestBaseSecured then
		SouthWestBaseSecured = true
		SWFlare.Destroy()

		table.insert(WestAttackPaths, { W1.Location, W2b.Location })
		table.insert(WestAttackPaths, { W1.Location, W2c.Location })

		InitWestScrinBase()
		InitEastScrinBase()
	end
end

InitFirstScrinBase = function()
	if not FirstScrinBaseInitialized then
		FirstScrinBaseInitialized = true
		InitAiUpgrades(MaleficScrin)
		InitAirAttackSquad(Squads.Air, MaleficScrin)
		if IsHardOrAbove() then
			InitAirAttackSquad(Squads.AirToAir, MaleficScrin, MissionPlayers, { "Aircraft" }, "ArmorType")
		end
		Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
			Actor.Create("ai.superweapons.enabled", true, { Owner = MaleficScrin })
			Actor.Create("ai.minor.superweapons.enabled", true, { Owner = MaleficScrin })
		end)
	end
end

InitNorthScrinBase = function()
	if not NorthScrinBaseInitialized then
		InitFirstScrinBase()
		NorthScrinBaseInitialized = true
		NumScrinBasesActive = NumScrinBasesActive + 1
		InitAttackSquad(Squads.North, MaleficScrin)
	end
end

InitWestScrinBase = function()
	if not WestScrinBaseInitialized then
		InitFirstScrinBase()
		WestScrinBaseInitialized = true
		NumScrinBasesActive = NumScrinBasesActive + 1
		InitAttackSquad(Squads.West, MaleficScrin)
	end
end

InitEastScrinBase = function()
	if not EastScrinBaseInitialized then
		InitFirstScrinBase()
		EastScrinBaseInitialized = true
		NumScrinBasesActive = NumScrinBasesActive + 1
		InitAttackSquad(Squads.East, MaleficScrin)
	end
end
