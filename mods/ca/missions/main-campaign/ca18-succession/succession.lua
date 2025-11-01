MissionDir = "ca|missions/main-campaign/ca18-succession"

SuperweaponsEnabledTime = {
	easy = DateTime.Seconds((60 * 45) + 41),
	normal = DateTime.Seconds((60 * 30) + 41),
	hard = DateTime.Seconds((60 * 15) + 41),
	vhard = DateTime.Seconds((60 * 12) + 41),
	brutal = DateTime.Seconds((60 * 10) + 41)
}

MaxAntiTankAir = {
	hard = 8,
	vhard = 12,
	brutal = 16
}

MaxAirToAir = {
	vhard = 12,
	brutal = 18
}

table.insert(UnitCompositions.Nod, {
	Infantry = { "n3", "n1", "n1", "n1", "n4", "n1", "n3", "n1", "n1", "n1", "n1", "n1", "n1", "n3", "n1", "n1" },
	Vehicles = { "mlrs", "mlrs", "mlrs", "mlrs", "mlrs", "mlrs", "mlrs" },
	MinTime = DateTime.Minutes(15),
	IsSpecial = true
})

AdjustedNodCompositions = AdjustCompositionsForDifficulty(UnitCompositions.Nod)

Squads = {
	Main1 = {
		InitTimeAdjustment = -DateTime.Minutes(10),
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(1) + DateTime.Seconds(30)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40, RampDuration = DateTime.Minutes(15) }),
		FollowLeader = true,
		DispatchDelay = DateTime.Seconds(15),
		Compositions = AdjustedNodCompositions,
		AttackPaths = {
			{ NodRally1.Location },
			{ NodRally2.Location },
			{ NodRally3.Location },
			{ NodRally4.Location },
			{ NodRally5.Location },
		},
	},
	Main2 = {
		InitTimeAdjustment = -DateTime.Minutes(10),
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(4)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40, RampDuration = DateTime.Minutes(15) }),
		FollowLeader = true,
		DispatchDelay = DateTime.Seconds(15),
		Compositions = AdjustedNodCompositions,
		AttackPaths = {
			{ NodRally1.Location },
			{ NodRally2.Location },
			{ NodRally3.Location },
			{ NodRally4.Location },
			{ NodRally5.Location },
		},
	},
	Air = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(11)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Nod,
	},
	AntiHeavyAir = AntiHeavyAirSquad({ "scrn" }, AdjustAirDelayForDifficulty(DateTime.Minutes(10))),
	AirToAir = AirToAirSquad({ "scrn", "apch", "venm" }, AdjustAirDelayForDifficulty(DateTime.Minutes(10))),
}

SetupPlayers = function()
	USSR = Player.GetPlayer("USSR")
	Nod = Player.GetPlayer("Nod")
	SpyPlaneProvider = Player.GetPlayer("SpyPlaneProvider")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { USSR }
	MissionEnemies = { Nod }
end

WorldLoaded = function()
	SetupPlayers()

	TimerTicks = 0
	NumFactoriesCaptured = 0
	Camera.Position = PlayerStart.CenterPosition

    InitNod()

    for y = 62, 120 do
        for x = 20, 110 do

            -- random 10% chance
            if Utils.RandomInteger(1, 10) == 1 then

                local actor = Utils.Random({ "CraterMaker1", "CraterMaker2", "ScorchMaker1", "ScorchMaker2" })
                local loc = CPos.New(x, y)

                Actor.Create(actor, true, { Owner = Neutral, Location = loc })
            end
        end
    end

	local spyPlaneDummy1 = Actor.Create("spy.plane.dummy", true, { Owner = SpyPlaneProvider })

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		spyPlaneDummy1.TargetAirstrike(TemplePrime.CenterPosition, Angle.South)
		spyPlaneDummy1.Destroy()
	end)

    ObjectiveCaptureTemplePrime = USSR.AddObjective("Capture Temple Prime.")
    ObjectiveCaptureFactories = USSR.AddObjective("Capture all four cyborg manufacturing facilities.")
	ObjectiveYuriMustSurvive = USSR.AddSecondaryObjective("Protect Yuri.")

    local factories = { CyborgFactory1, CyborgFactory2, CyborgFactory3, CyborgFactory4 }
    Utils.Do(factories, function(f)
        Trigger.OnCapture(f, function(self, captor, oldOwner, newOwner)
            NumFactoriesCaptured = NumFactoriesCaptured + 1
            if NumFactoriesCaptured == 4 then
                USSR.MarkCompletedObjective(ObjectiveCaptureFactories)

				if USSR.IsObjectiveCompleted(ObjectiveCaptureTemplePrime) then
					USSR.MarkCompletedObjective(ObjectiveYuriMustSurvive)
				end
            end
        end)

        Trigger.OnKilled(f, function(self, killer)
            if not USSR.IsObjectiveCompleted(ObjectiveCaptureFactories) then
                USSR.MarkFailedObjective(ObjectiveCaptureFactories)
            end
        end)
    end)

    Trigger.OnCapture(TemplePrime, function(self, captor, oldOwner, newOwner)
        USSR.MarkCompletedObjective(ObjectiveCaptureTemplePrime)

		if USSR.IsObjectiveCompleted(ObjectiveCaptureFactories) then
			USSR.MarkCompletedObjective(ObjectiveYuriMustSurvive)
		end
    end)

    Trigger.OnKilled(TemplePrime, function(self, killer)
        if not USSR.IsObjectiveCompleted(ObjectiveCaptureTemplePrime) then
            USSR.MarkFailedObjective(ObjectiveCaptureTemplePrime)
        end
    end)

	Trigger.OnKilled(Yuri, function(self, killer)
		if not USSR.IsObjectiveCompleted(ObjectiveYuriMustSurvive) then
			USSR.MarkFailedObjective(ObjectiveYuriMustSurvive)
		end
		Notification("Yuri used his psionic powers to cheat death and has fled the battlefield to recuperate.")
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Media.PlaySpeechNotification(nil, "ReinforcementsArrived")
		Notification("Reinforcements have arrived.")
		Reinforcements.Reinforce(USSR, { "kiro" }, { KirovSpawn1.Location, KirovRally1.Location })
		Reinforcements.Reinforce(USSR, { "kiro" }, { KirovSpawn2.Location, KirovRally2.Location })
	end)

	Trigger.AfterDelay(1, function()
		for k, v in pairs({ InitSquad1, InitSquad2, InitSquad3, InitSquad4, InitSquad5 }) do
			local actors = Map.ActorsInCircle(v.CenterPosition, WDist.New(8 * 1024));
			local attackers = Utils.Where(actors, function(a)
				return a.Owner == Nod and a.HasProperty("Hunt")
			end)

			if (k > 3 and Difficulty == "easy") or (k > 4 and Difficulty == "normal") then
				Utils.Do(attackers, function(a)
					a.Destroy()
				end)
			else
				Trigger.AfterDelay(DateTime.Seconds(k * 8), function()
					Utils.Do(attackers, function(a)
						if not a.IsDead then
							a.Hunt()
						end
					end)
				end)
			end
		end
	end)

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
		Nod.Resources = Nod.ResourceCapacity - 500

		if MissionPlayersHaveNoRequiredUnits() then
			if not Nod.IsObjectiveCompleted(ObjectiveCaptureFactories) then
				Nod.MarkFailedObjective(ObjectiveCaptureFactories)
			end

			if not Nod.IsObjectiveCompleted(ObjectiveCaptureTemplePrime) then
				Nod.MarkFailedObjective(ObjectiveCaptureTemplePrime)
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
	if DateTime.GameTime > 1 and DateTime.GameTime % DateTime.Seconds(30) == 0 then
		CalculatePlayerCharacteristics()
	end
end

InitNod = function()
	AutoRepairAndRebuildBuildings(Nod, 15)
	SetupRefAndSilosCaptureCredits(Nod)
	AutoReplaceHarvesters(Nod)
	AutoRebuildConyards(Nod)
    InitAiUpgrades(Nod)
	InitAttackSquad(Squads.Main1, Nod)
	InitAttackSquad(Squads.Main2, Nod)
	InitAirAttackSquad(Squads.Air, Nod)

	if IsHardOrAbove() then
		InitAirAttackSquad(Squads.AntiHeavyAir, Nod, MissionPlayers, { "4tnk", "4tnk.atomic", "apoc", "apoc.atomic" })
		InitAirAttackSquad(Squads.AirToAir, Nod, MissionPlayers, { "Aircraft" }, "ArmorType")
	end

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)

	Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = Nod })
		Actor.Create("ai.superweapons.enabled", true, { Owner = Nod })
	end)
end
