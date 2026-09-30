MissionDir = "ca|missions/main-campaign/ca50-preservation"

ScrinAttackPaths = {
	{ ScrinWaypoint1.Location, ScrinWaypoint2.Location },
    { ScrinWaypoint3.Location, ScrinWaypoint4.Location },
    { ScrinWaypoint5.Location, ScrinWaypoint6.Location },
    { ScrinWaypoint5.Location, ScrinWaypoint6.Location, ScrinWaypoint7.Location },
    { ScrinWaypoint5.Location, ScrinWaypoint8.Location },
}

SovietAttackPaths = {
	{ SovietWaypoint1.Location, SovietWaypoint2.Location },
    { SovietWaypoint1.Location, SovietWaypoint2.Location },
    { SovietWaypoint4.Location, SovietWaypoint5.Location },
}

SuperweaponsEnabledTime = {
	easy = DateTime.Seconds((60 * 50) + 17),
	normal = DateTime.Seconds((60 * 35) + 17),
	hard = DateTime.Seconds((60 * 25) + 17),
	vhard = DateTime.Seconds((60 * 20) + 17),
	brutal = DateTime.Seconds((60 * 15) + 17)
}

AirFleetKillersThreshold = {
	normal = 6,
	hard = 4,
	vhard = 3,
	brutal = 2
}

MaxFleetKillers = {
	normal = 3,
	hard = 5,
	vhard = 8,
	brutal = 12
}

TripodKillersThreshold = {
	normal = 11,
	hard = 9,
	vhard = 7,
	brutal = 5
}

MaxTripodKillers = {
	normal = 3,
	hard = 5,
	vhard = 8,
	brutal = 12
}

MaleficFleetSpawns = Utils.Shuffle({
	MaleficSpawnWest.Location,
	MaleficSpawnEast.Location,
	MaleficSpawnMiddle.Location,
})

MaleficFleetSpawnDelay = {
	easy = DateTime.Minutes(25),
	normal = DateTime.Minutes(18),
	hard = DateTime.Minutes(15),
	vhard = DateTime.Minutes(12),
	brutal = DateTime.Minutes(10)
}

MaleficFleetSpawnInterval = {
	easy = DateTime.Minutes(5),
	normal = DateTime.Minutes(5),
	hard = DateTime.Minutes(4),
	vhard = DateTime.Minutes(4),
	brutal = DateTime.Minutes(3),
}

MaleficFleetCompositions = {
	easy = { "pac", "deva" },
	normal = { "pac", "deva" },
	hard = { "pac", "deva", "deva" },
	vhard = { "pac", "deva", "pac", "deva" },
	brutal = { "pac", "deva", "pac", "deva", "pac" },
}

if IsHardOrAbove() then
	table.insert(UnitCompositions.Scrin, {
		Infantry = { "s3", "s4", "evis", "evis", "evis", "evis", "s1", "s1", "s4", "s1", "s4", "s1", "s4", "s1", "mast" },
		Vehicles = { "shrw", TripodVariant, TripodVariant, "shrw", CorrupterOrDevourer, "oblt", "shrw" },
		Aircraft = { PacOrDevastator, "pac" },
		MinTime = DateTime.Minutes(22)
	})

	if IsVeryHardOrAbove() then
		table.insert(UnitCompositions.Scrin, {
			Infantry = { "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis" },
			Vehicles = { "shrw", "shrw", "ruin", "ruin", "ruin", "ruin", "rtpd", "rtpd" },
			Aircraft = { "deva" },
			MinTime = DateTime.Minutes(18),
			RequiredTargetCharacteristics = { "MassInfantry" }
		})
	end
end

NextMaleficFleetSpawnIndex = 1

Squads = {
	ScrinMain = {
		InitTimeAdjustment = -DateTime.Minutes(4),
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40, RampDuration = DateTime.Minutes(15) }),
		FollowLeader = true,
		AttackPaths = ScrinAttackPaths,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(1)),
	},
	SovietMain = {
		InitTimeAdjustment = -DateTime.Minutes(4),
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Soviet),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40, RampDuration = DateTime.Minutes(15) }),
		FollowLeader = true,
		AttackPaths = SovietAttackPaths,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(2)),
	},
	ScrinAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 6, Max = 12 }),
		Compositions = AirCompositions.Scrin,
	},
	SovietAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 6, Max = 12 }),
		Compositions = AirCompositions.Soviet,
	},
	ScrinAirToAir = AirToAirSquad({ "stmr", "enrv", "torm" }, AdjustAirDelayForDifficulty(DateTime.Minutes(10))),
	AirFleetKillers = {
		ActiveCondition = function(squad)
			local scrinFleet = GetMissionPlayersActorsByTypes({ "pac", "deva" })
			return #scrinFleet > AirFleetKillersThreshold[Difficulty]
		end,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 30, Max = 30 }),
		Compositions = function(squad)
			local enervators = { "enrv" }
			local numFleetShips = #GetMissionPlayersActorsByTypes({ "pac", "deva" })
			for i = 1, math.min(numFleetShips, MaxFleetKillers[Difficulty]) do
				table.insert(enervators, "enrv")
			end
			return { { Aircraft = enervators } }
		end
	},
	TripodKillers = {
		ActiveCondition = function(squad)
			local tripods = GetMissionPlayersActorsByTypes({ "tpod", "rtpd" })
			return #tripods > TripodKillersThreshold[Difficulty]
		end,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 30, Max = 30 }),
		Compositions = function(squad)
			local sukhois = { "suk" }
			local numTripods = #GetMissionPlayersActorsByTypes({ "tpod", "rtpd" })
			for i = 1, math.min(numTripods, MaxTripodKillers[Difficulty]) do
				table.insert(sukhois, "suk")
			end
			return { { Aircraft = sukhois } }
		end
	},
	SovietCommandoKillers = {
		ActiveCondition = function(squad)
			local commandos = GetMissionPlayersActorsByTypes({ "mast", "rmbo" })
			return #commandos > 0
		end,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 10, Max = 20 }),
		Compositions = { { Aircraft = { "yak", "yak" } } }
	}
}

SetupPlayers = function()
	ScrinRebels = Player.GetPlayer("ScrinRebels")
	USSR = Player.GetPlayer("USSR")
	Scrin = Player.GetPlayer("Scrin")
    Nod = Player.GetPlayer("Nod")
	MaleficScrin = Player.GetPlayer("MaleficScrin")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { ScrinRebels }
	MissionEnemies = { USSR, Scrin }

	Actor.Create("rebel.allegiance", true, { Owner = ScrinRebels })
end

WorldLoaded = function()
	SetupPlayers()

    Camera.Position = PlayerStart.CenterPosition

	InitObjectives(ScrinRebels)
	AdjustPlayerStartingCashForDifficulty()
	RemoveActorsBasedOnDifficultyTags()
	InitUSSR()
	InitScrin()

    ObjectiveProtectTemples = ScrinRebels.AddObjective("Protect the Nod Temples until gateways are operational.")

    UpdateGatewayStatus()

    Trigger.OnAnyKilled({ WestTemple, MiddleTemple, EastTemple }, function(self)
        if not ScrinRebels.IsObjectiveCompleted(ObjectiveProtectTemples) then
            ScrinRebels.MarkFailedObjective(ObjectiveProtectTemples)
        end
    end)

	WestTemple.GrantCondition("is-objective")
	MiddleTemple.GrantCondition("is-objective")
	EastTemple.GrantCondition("is-objective")

    local initialAttackWaves = Utils.Shuffle({ SovietInitialAttack1, SovietInitialAttack2, SovietInitialAttack3, ScrinInitialAttack1, ScrinInitialAttack2, ScrinInitialAttack3 })
    local initialAttackDelay = 0

    Utils.Do(initialAttackWaves, function(w)
        Trigger.AfterDelay(initialAttackDelay, function()
            local units = Map.ActorsInCircle(w.CenterPosition, WDist.New(9 * 1024), function(a)
                return (a.Owner == USSR or a.Owner == Scrin) and a.HasProperty("Hunt")
            end)
            Utils.Do(units, function(u)
                u.Hunt()
            end)
        end)
        initialAttackDelay = initialAttackDelay + DateTime.Seconds(20)
    end)

	ShowGatewayChargeTip()

	Trigger.AfterDelay(MaleficFleetSpawnDelay[Difficulty], function()
		Notification("Warning, Malefic fleet detected, approaching from the south.")
		MediaCA.PlaySound("s_maleficfleet.aud", 2)
		Trigger.AfterDelay(DateTime.Seconds(30), function()
			SpawnNextMaleficFleetWave()
		end)
	end)

	WestToMiddleWormhole.RallyPoint = CPos.New(WestToMiddleWormhole.Location.X - 3, WestToMiddleWormhole.Location.Y - 1)
	MiddleToWestWormhole.RallyPoint = CPos.New(MiddleToWestWormhole.Location.X + 2, MiddleToWestWormhole.Location.Y - 2)
	MiddleToEastWormhole.RallyPoint = CPos.New(MiddleToEastWormhole.Location.X - 3, MiddleToEastWormhole.Location.Y + 2)
	EastToMiddleWormhole.RallyPoint = CPos.New(EastToMiddleWormhole.Location.X, EastToMiddleWormhole.Location.Y + 3)

    AfterWorldLoaded()
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	OncePerThirtySecondChecks()
	AfterTick()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Scrin.Resources = Scrin.ResourceCapacity - 500
        USSR.Resources = USSR.ResourceCapacity - 500

        UpdateGatewayStatus()

		if Scrin.HasNoRequiredUnits() and USSR.HasNoRequiredUnits() then
            ScrinRebels.MarkCompletedObjective(ObjectiveProtectTemples)
		end

		if MissionPlayersHaveNoRequiredUnits() then
            if not ScrinRebels.IsObjectiveCompleted(ObjectiveProtectTemples) then
                ScrinRebels.MarkFailedObjective(ObjectiveProtectTemples)
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

InitScrin = function()
	AutoRepairAndRebuildBuildings(Scrin)
	SetupRefAndSilosCaptureCredits(Scrin)
	AutoReplaceHarvesters(Scrin)
	AutoRebuildConyards(Scrin)
	SetupUnitDefenders(Scrin)

    InitScrinAttacks()
end

InitScrinAttacks = function()
	InitAiUpgrades(Scrin)
	InitAttackSquad(Squads.ScrinMain, Scrin)
	InitAirAttackSquad(Squads.ScrinAir, Scrin)

    Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = Scrin })
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = Scrin })
	end)

	if IsHardOrAbove() then
		InitAirAttackSquad(Squads.AirFleetKillers, Scrin, MissionPlayers, { "pac", "deva" })
		InitAirAttackSquad(Squads.ScrinAirToAir, Scrin, MissionPlayers, { "Aircraft" }, "ArmorType")
	end
end

InitUSSR = function()
	AutoRepairAndRebuildBuildings(USSR)
	SetupRefAndSilosCaptureCredits(USSR)
	AutoReplaceHarvesters(USSR)
	AutoRebuildConyards(USSR)
	SetupUnitDefenders(USSR)

    InitUSSRAttacks()
end

InitUSSRAttacks = function()
	InitAiUpgrades(USSR)
	InitAttackSquad(Squads.SovietMain, USSR)
	InitAirAttackSquad(Squads.SovietAir, USSR)

	Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = USSR })
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = USSR })
	end)

	if IsHardOrAbove() then
		InitAirAttackSquad(Squads.TripodKillers, USSR, MissionPlayers, { "tpod", "rtpd" })

		if IsVeryHardOrAbove() then
			InitAirAttackSquad(Squads.SovietCommandoKillers, USSR, MissionPlayers, { "mast", "rmbo" })
		end
	end
end

UpdateGatewayStatus = function()
    local chargePerc = 100
    if not MiddleGateway.IsDead then
        chargePerc = MiddleGateway.ChargePercentage
    end

	SetChargeStatusText(chargePerc)

    if chargePerc == 100 then
        ScrinRebels.MarkCompletedObjective(ObjectiveProtectTemples)
    end
end

SpawnNextMaleficFleetWave = function()
	local spawnLocation = MaleficFleetSpawns[NextMaleficFleetSpawnIndex]
	local delay = 0

	Utils.Do(MaleficFleetCompositions[Difficulty], function(actorType)
		-- add a random X offset between -3 and +3
		local xOffset = Utils.RandomInteger(-3, 4)
		local actorSpawnLocation = CPos.New(spawnLocation.X + xOffset, spawnLocation.Y)
		Trigger.AfterDelay(delay, function()
			local a = Actor.Create(actorType, true, { Location = actorSpawnLocation, Owner = MaleficScrin, Facing = Angle.North })
			a.Hunt()
		end)
		delay = delay + DateTime.Seconds(1)
	end)

	NextMaleficFleetSpawnIndex = NextMaleficFleetSpawnIndex + 1
	if NextMaleficFleetSpawnIndex > #MaleficFleetSpawns then
		NextMaleficFleetSpawnIndex = 1
	end

	Trigger.AfterDelay(MaleficFleetSpawnInterval[Difficulty], SpawnNextMaleficFleetWave)
end

-- overridden in co-op version
SetChargeStatusText = function(chargePerc)
	local text = "Gateway charge progress: " .. chargePerc .. "%"
	local textColor = HSLColor.Yellow
	local isCharging = ScrinRebels.HasPrerequisites({ "gatewayscharging" })

	if isCharging then
		text = text .. " (Charging)"
		textColor = HSLColor.Lime
	else
		text = text .. " (Not Charging)"
	end

	UserInterface.SetMissionText(text, textColor)
end

-- overridden in co-op version
ShowGatewayChargeTip = function()
    Trigger.AfterDelay(DateTime.Seconds(5), function()
        Tip("Use the Charge Gateway power to use resources to charge the three gateways.")
    end)
end