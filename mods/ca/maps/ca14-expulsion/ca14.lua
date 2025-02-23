GDIMainAttackPaths = {
	{ GDIRallyAlpha.Location, GDIAttack1.Location, GDIAttack2.Location, GDIAttack3.Location, GDIAttack4.Location, GDIAttack5.Location, GDIAttack6.Location },
	{ GDIRallyAlpha.Location, GDIAttack7.Location, GDIAttack4.Location, GDIAttack5.Location, GDIAttack6.Location },
	{ GDIRallyAlpha.Location, GDIAttack7.Location, GDIAttack4.Location, GDIAttack8.Location, GDIAttack9.Location, GDIAttack6.Location },
	{ GDIRallyBravo.Location, GDIAttack10.Location, GDIAttack8.Location, GDIAttack9.Location, GDIAttack6.Location },
	{ GDIRallyBravo.Location, GDIAttack10.Location, GDIAttack11.Location, GDIAttack12.Location, GDIAttack9.Location, GDIAttack6.Location },
}

GDISouthAttackPaths = {
	{ GDIAttack2.Location, GDIAttack3.Location, GDIAttack4.Location, GDIAttack5.Location, GDIAttack6.Location },
}

GDINorthEastAttackPaths = {
	{ GDIAttack10.Location, GDIAttack8.Location, GDIAttack9.Location, GDIAttack6.Location },
	{ GDIAttack11.Location, GDIAttack12.Location, GDIAttack9.Location, GDIAttack6.Location },
}

Squads = {
	GDIMain = {
		Delay = {
			easy = DateTime.Seconds(330),
			normal = DateTime.Seconds(210),
			hard = DateTime.Seconds(90)
		},
		AttackValuePerSecond = {
			easy = { Min = 8, Max = 35, RampDuration = DateTime.Minutes(20) },
			normal = { Min = 20, Max = 60, RampDuration = DateTime.Minutes(20) },
			hard = { Min = 30, Max = 100, RampDuration = DateTime.Minutes(20) },
		},
		FollowLeader = true,
		ProducerActors = { Infantry = { GDIMainBarracks }, Vehicles = { GDIFactory } },
		Units = UnitCompositions.GDI.Main,
		AttackPaths = GDIMainAttackPaths,
	},
	GDISouth = {
		Delay = {
			easy = DateTime.Minutes(10),
			normal = DateTime.Minutes(8),
			hard = DateTime.Minutes(6)
		},
		AttackValuePerSecond = {
			easy = { Min = 6, Max = 12, RampDuration = DateTime.Minutes(20) },
			normal = { Min = 10, Max = 20, RampDuration = DateTime.Minutes(20) },
			hard = { Min = 16, Max = 32, RampDuration = DateTime.Minutes(20) },
		},
		FollowLeader = true,
		ProducerActors = { Infantry = { GDISouthBarracks } },
		Units = {
			easy = { { Infantry = { "n3", "n1", "n1", "n1", "n2" } } } ,
			normal = { { Infantry = { "n3", "n1", "n1", "n1", "n3", "n2" } } },
			hard = { { Infantry = { "n3", "n1", "n1", "n1", "n3", "n2", "n1", "n1" } } },
		},
		AttackPaths = GDISouthAttackPaths,
	},
	GDINorthEast = {
		Delay = {
			easy = DateTime.Minutes(10),
			normal = DateTime.Minutes(8),
			hard = DateTime.Minutes(6)
		},
		AttackValuePerSecond = {
			easy = { Min = 6, Max = 12, RampDuration = DateTime.Minutes(15) },
			normal = { Min = 10, Max = 20, RampDuration = DateTime.Minutes(13) },
			hard = { Min = 16, Max = 32, RampDuration = DateTime.Minutes(11) },
		},
		FollowLeader = true,
		ProducerActors = { Infantry = { GDINorthEastBarracks } },
		Units = {
			easy = { { Infantry = { "n3", "n1", "n1", "n1", "n2" } } },
			normal = { { Infantry = { "n3", "n1", "n1", "n1", "n3", "n2" } } },
			hard = { { Infantry = { "n3", "n1", "n1", "n1", "n3", "n2", "n1", "n1" } } },
		},
		AttackPaths = GDINorthEastAttackPaths,
	},
	GDIAir = {
		Delay = {
			easy = DateTime.Minutes(15),
			normal = DateTime.Minutes(13),
			hard = DateTime.Minutes(11)
		},
		AttackValuePerSecond = {
			easy = { Min = 6, Max = 6 },
			normal = { Min = 11, Max = 11 },
			hard = { Min = 18, Max = 18 },
		},
		ProducerTypes = { Aircraft = { "afld.gdi" } },
		Units = {
			easy = {
				{ Aircraft = { "orca" } }
			},
			normal = {
				{ Aircraft = { "orca", "orca" } },
				{ Aircraft = { "a10" } }
			},
			hard = {
				{ Aircraft = { "orca", "orca", "orca" } },
				{ Aircraft = { "a10", "a10" } }
			}
		},
	}
}

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	GDI = Player.GetPlayer("GDI")
	MissionPlayers = { USSR }
	TimerTicks = 0
	McvArrived = false

	Camera.Position = McvRally.CenterPosition

	InitObjectives(USSR)
	AdjustStartingCash()
	InitGDI()

	ObjectiveExpelGDI = USSR.AddObjective("Remove the GDI presence.")

	if Difficulty == "easy" then
		BattleTank1.Destroy()
		BattleTank2.Destroy()
	end

	local initialAttackers = { InitialAttacker1, InitialAttacker2, InitialAttacker3, InitialAttacker4, InitialAttacker5, InitialAttacker6 }

	Trigger.AfterDelay(DateTime.Seconds(8), function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		Notification("Reinforcements have arrived.")
		Reinforcements.Reinforce(USSR, { "mcv" }, { McvSpawn.Location, McvRally.Location }, 75)
		McvArrived = true

		Utils.Do(initialAttackers, function(a)
			if not a.IsDead then
				a.Hunt()
			end
		end)
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		GDI.Resources = GDI.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
		end

		if GDI.HasNoRequiredUnits() then
			USSR.MarkCompletedObjective(ObjectiveExpelGDI)
		end

		if McvArrived and USSR.HasNoRequiredUnits() then
			USSR.MarkFailedObjective(ObjectiveExpelGDI)
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()
	end
end

InitGDI = function()
	AutoRepairAndRebuildBuildings(GDI, 15)
	SetupRefAndSilosCaptureCredits(GDI)
	AutoReplaceHarvesters(GDI)
	InitAiUpgrades(GDI)

	local gdiGroundAttackers = GDI.GetGroundAttackers()

	Utils.Do(gdiGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGDIGroundHunterUnit)
	end)

	Trigger.AfterDelay(Squads.GDIMain.Delay[Difficulty], function()
		InitAttackSquad(Squads.GDIMain, GDI)
	end)

	Trigger.AfterDelay(Squads.GDISouth.Delay[Difficulty], function()
		InitAttackSquad(Squads.GDISouth, GDI)
	end)

	Trigger.AfterDelay(Squads.GDINorthEast.Delay[Difficulty], function()
		InitAttackSquad(Squads.GDINorthEast, GDI)
	end)

	Trigger.AfterDelay(Squads.GDIAir.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.GDIAir, GDI, USSR, { "harv", "v2rl", "apwr", "tsla", "ttra", "v3rl", "mig", "hind", "suk", "suk.upg", "kiro", "apoc" })
	end)
end
