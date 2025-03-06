ChemMissileEnabledTime = {
	easy = DateTime.Seconds((60 * 25) + 41),
	normal = DateTime.Seconds((60 * 20) + 41),
	hard = DateTime.Seconds((60 * 15) + 41),
}

Squads = {
	Main1 = {
		Delay = {
			easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(2),
		},
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 25 },
			normal = { Min = 25, Max = 50 },
			hard = { Min = 40, Max = 80 },
		},
		FollowLeader = true,
		DispatchDelay = DateTime.Seconds(15),
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = UnitCompositions.Nod.Main,
		AttackPaths = {
			{ NodRally1.Location },
			{ NodRally2.Location },
			{ NodRally3.Location },
			{ NodRally4.Location },
			{ NodRally5.Location },
		},
	},
	Main2 = {
		Delay = {
			easy = DateTime.Minutes(7),
			normal = DateTime.Minutes(5),
			hard = DateTime.Minutes(3),
		},
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 25 },
			normal = { Min = 25, Max = 50 },
			hard = { Min = 40, Max = 80 },
		},
		FollowLeader = true,
		DispatchDelay = DateTime.Seconds(15),
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = UnitCompositions.Nod.Main,
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
			normal = DateTime.Minutes(12),
			hard = DateTime.Minutes(11)
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
}

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Nod = Player.GetPlayer("Nod")
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

    ObjectiveCaptureTemplePrime = USSR.AddObjective("Capture Temple Prime.")
    ObjectiveCaptureFactories = USSR.AddObjective("Capture all four cyborg manufacturing facilities.")
	ObjectiveYuriMustSurvive = USSR.AddObjective("Yuri must survive.")

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

InitNod = function()
	AutoRepairAndRebuildBuildings(Nod, 15)
	SetupRefAndSilosCaptureCredits(Nod)
	AutoReplaceHarvesters(Nod)
    InitAiUpgrades(Nod)

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)

	Trigger.AfterDelay(ChemMissileEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = Nod })
	end)

    Trigger.AfterDelay(Squads.Main1.Delay[Difficulty], function()
        InitAttackSquad(Squads.Main1, Nod)
    end)

    Trigger.AfterDelay(Squads.Main2.Delay[Difficulty], function()
        InitAttackSquad(Squads.Main2, Nod)
    end)

    Trigger.AfterDelay(Squads.Air.Delay[Difficulty], function()
        InitAirAttackSquad(Squads.Air, Nod, USSR, { "harv", "4tnk", "4tnk.atomic", "3tnk", "3tnk.atomic", "3tnk.rhino", "3tnk.rhino.atomic",
			"katy", "v3rl", "ttra", "v3rl", "apwr", "tpwr", "npwr", "tsla", "proc", "nukc", "ovld", "apoc", "apoc.atomic", "ovld.atomic" })
    end)
end
