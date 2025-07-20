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

EmpMissileEnabledTime = {
	normal = DateTime.Minutes(20),
	hard = DateTime.Minutes(10),
	vhard = DateTime.Minutes(10),
	brutal = DateTime.Minutes(10)
}

Squads = {
	GDIMain = {
		Delay = AdjustDelayForDifficulty(DateTime.Seconds(210)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 24, Max = 48 }),
		FollowLeader = true,
		ProducerActors = { Infantry = { GDIMainBarracks }, Vehicles = { GDIFactory } },
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.GDI),
		AttackPaths = GDIMainAttackPaths,
	},
	GDISouth = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(8)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 8, Max = 16 }),
		FollowLeader = true,
		ProducerActors = { Infantry = { GDISouthBarracks } },
		Compositions = {
			easy = { { Infantry = { "n3", "n1", "n1", "n1", "n2" } } } ,
			normal = { { Infantry = { "n3", "n1", "n1", "n1", "n3", "n2" } } },
			hard = { { Infantry = { "n3", "n1", "n1", "n1", "n3", "n2", "n1", "n1" } } },
			vhard = { { Infantry = { "n3", "n1", "n1", "n1", "n3", "n2", ZoneTrooperVariant } } },
			brutal = { { Infantry = { "n3", "n1", "n1", "n1", "n3", "n2", "n1", ZoneTrooperVariant, ZoneTrooperVariant } } },
		},
		AttackPaths = GDISouthAttackPaths,
	},
	GDINorthEast = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(8)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 8, Max = 16 }),
		FollowLeader = true,
		ProducerActors = { Infantry = { GDINorthEastBarracks } },
		Compositions = {
			easy = { { Infantry = { "n3", "n1", "n1", "n1", "n2" } } },
			normal = { { Infantry = { "n3", "n1", "n1", "n1", "n3", "n2" } } },
			hard = { { Infantry = { "n3", "n1", "n1", "n1", "n3", "n2", "n1", "n1" } } },
			vhard = { { Infantry = { "n3", "n1", "n1", "n1", "n3", "n2", ZoneTrooperVariant } } },
			brutal = { { Infantry = { "n3", "n1", "n1", "n1", "n3", "n2", "n1", ZoneTrooperVariant, ZoneTrooperVariant } } },
		},
		AttackPaths = GDINorthEastAttackPaths,
	},
	GDIAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 10, Max = 10 }),
		Compositions = AirCompositions.GDI,
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
	AdjustPlayerStartingCashForDifficulty()
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
	OncePerThirtySecondChecks()
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

		if not PlayerHasBuildings(GDI) then
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

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % DateTime.Seconds(30) == 0 then
		CalculatePlayerCharacteristics()
	end
end

InitGDI = function()
	AutoRepairAndRebuildBuildings(GDI, 15)
	SetupRefAndSilosCaptureCredits(GDI)
	AutoReplaceHarvesters(GDI)
	AutoRebuildConyards(GDI)
	InitAiUpgrades(GDI)
	InitAttackSquad(Squads.GDIMain, GDI)
	InitAttackSquad(Squads.GDISouth, GDI)
	InitAttackSquad(Squads.GDINorthEast, GDI)
	InitAirAttackSquad(Squads.GDIAir, GDI)

	local gdiGroundAttackers = GDI.GetGroundAttackers()

	Utils.Do(gdiGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGDIGroundHunterUnit)
	end)

	if Difficulty ~= "easy" then
		Trigger.AfterDelay(EmpMissileEnabledTime[Difficulty], function()
			Actor.Create("ai.minor.superweapons.enabled", true, { Owner = GDI })
		end)
	end
end
