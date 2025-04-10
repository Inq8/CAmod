ChemMissileEnabledTime = {
	easy = DateTime.Seconds((60 * 25) + 41),
	normal = DateTime.Seconds((60 * 20) + 41),
	hard = DateTime.Seconds((60 * 15) + 41),
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
		InitTime = 0 - DateTime.Minutes(10),
		Delay = {
			easy = DateTime.Minutes(5),
			normal = DateTime.Minutes(3),
			hard = DateTime.Minutes(1),
		},
		AttackValuePerSecond = {
			easy = { Min = 12, Max = 25, RampDuration = DateTime.Minutes(7) },
			normal = { Min = 25, Max = 50, RampDuration = DateTime.Minutes(5) },
			hard = { Min = 40, Max = 80, RampDuration = DateTime.Minutes(3) },
		},
		FollowLeader = true,
		DispatchDelay = DateTime.Seconds(15),
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = AdjustedNodCompositions,
		AttackPaths = {
			{ NodRally1.Location },
			{ NodRally2.Location },
			{ NodRally3.Location },
			{ NodRally4.Location },
			{ NodRally5.Location },
		},
	},
	Main2 = {
		InitTime = 0 - DateTime.Minutes(10),
		Delay = {
			easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(2),
		},
		AttackValuePerSecond = {
			easy = { Min = 12, Max = 25, RampDuration = DateTime.Minutes(7) },
			normal = { Min = 25, Max = 50, RampDuration = DateTime.Minutes(5) },
			hard = { Min = 40, Max = 80, RampDuration = DateTime.Minutes(3) },
		},
		FollowLeader = true,
		DispatchDelay = DateTime.Seconds(15),
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = AdjustedNodCompositions,
		AttackPaths = {
			{ NodRally1.Location },
			{ NodRally2.Location },
			{ NodRally3.Location },
			{ NodRally4.Location },
			{ NodRally5.Location },
		},
	},
	Air = {
		Delay = {
			easy = DateTime.Minutes(13),
			normal = DateTime.Minutes(11),
			hard = DateTime.Minutes(9)
		},
		AttackValuePerSecond = {
			easy = { Min = 7, Max = 7 },
			normal = { Min = 14, Max = 14 },
			hard = { Min = 21, Max = 21 },
		},
		ProducerTypes = { Aircraft = { "hpad.td" } },
		Units = {
			easy = {
				{ Aircraft = { "apch" } }
			},
			normal = {
				{ Aircraft = { "apch", "apch" } },
				{ Aircraft = { "scrn" } },
				{ Aircraft = { "rah" } }
			},
			hard = {
				{ Aircraft = { "apch", "apch", "apch" } },
				{ Aircraft = { "scrn", "scrn" } },
				{ Aircraft = { "rah", "rah" } }
			}
		},
	},
	AntiTankAir = {
		Delay = {
			hard = DateTime.Minutes(10)
		},
		ActiveCondition = function()
			return #USSR.GetActorsByTypes({ "4tnk", "4tnk.atomic", "apoc", "apoc.atomic", "ovld", "ovld.atomic" }) > 8
		end,
		AttackValuePerSecond = {
			hard = { Min = 35, Max = 35 },
		},
		ProducerTypes = { Aircraft = { "hpad.td" } },
		Units = {
			hard = {
				{ Aircraft = { "scrn", "scrn", "scrn", "scrn", "scrn", "scrn", "scrn", "scrn" } },
			}
		},
	}
}

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Nod = Player.GetPlayer("Nod")
	SpyPlaneProvider = Player.GetPlayer("SpyPlaneProvider")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { USSR }
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
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		Notification("Reinforcements have arrived.")
		Reinforcements.Reinforce(USSR, { "kiro" }, { KirovSpawn1.Location, KirovRally1.Location })
		Reinforcements.Reinforce(USSR, { "kiro" }, { KirovSpawn2.Location, KirovRally2.Location })
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	OncePerThirtySecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Nod.Resources = Nod.ResourceCapacity - 500

		if USSR.HasNoRequiredUnits() then
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

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)

	Trigger.AfterDelay(ChemMissileEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = Nod })
	end)

	if Difficulty == "hard" then
		Squads.Main1.InitTime = 0
		Squads.Main2.InitTime = 0
	elseif Difficulty == "normal" then
		Squads.Main1.InitTime = DateTime.Minutes(3)
		Squads.Main2.InitTime = DateTime.Minutes(3)
	end

    Trigger.AfterDelay(Squads.Main1.Delay[Difficulty], function()
        InitAttackSquad(Squads.Main1, Nod)
    end)

    Trigger.AfterDelay(Squads.Main2.Delay[Difficulty], function()
        InitAttackSquad(Squads.Main2, Nod)
    end)

    Trigger.AfterDelay(Squads.Air.Delay[Difficulty], function()
        InitAirAttackSquad(Squads.Air, Nod)
    end)

	if Difficulty == "hard" then
		Trigger.AfterDelay(Squads.AntiTankAir.Delay[Difficulty], function()
			InitAirAttackSquad(Squads.AntiTankAir, Nod, USSR, { "4tnk", "4tnk.atomic", "apoc", "apoc.atomic", "ovld", "ovld.atomic" })
		end)
	end
end
