MissionDir = "ca|missions/main-campaign/ca14-treachery"


GreeceMainAttackPaths = {
	{ SouthAttackRally.Location, SouthAttack1.Location },
	{ EastAttackRally.Location, EastAttack1.Location, EastAttack2.Location, EastAttack3a.Location },
	{ EastAttackRally.Location, EastAttack1.Location, EastAttack2.Location, EastAttack3b.Location },
}

CruiserPatrolPath = { CruiserPatrol1.Location, CruiserPatrol2.Location, CruiserPatrol3.Location, CruiserPatrol4.Location, CruiserPatrol5.Location, CruiserPatrol6.Location, CruiserPatrol7.Location, CruiserPatrol8.Location, CruiserPatrol7.Location, CruiserPatrol6.Location, CruiserPatrol5.Location, CruiserPatrol4.Location, CruiserPatrol3.Location, CruiserPatrol2.Location }

TraitorCompositions = {
	easy = {
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "btr" }, MaxTime = DateTime.Minutes(14) },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "shok" }, Vehicles = { "btr", "katy" }, MinTime = DateTime.Minutes(14) },
	},
	normal = {
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "3tnk" }, MaxTime = DateTime.Minutes(12) },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "shok" }, Vehicles = { "3tnk", "katy" }, MinTime = DateTime.Minutes(12) },
	},
	hard = {
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "3tnk", "btr" }, MaxTime = DateTime.Minutes(10) },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "shok" }, Vehicles = { "3tnk", "v2rl", "ttra" }, MinTime = DateTime.Minutes(10) },
	},
	vhard = {
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1" }, Vehicles = { "4tnk", "btr" }, MaxTime = DateTime.Minutes(9) },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "shok", "e1", "e1" }, Vehicles = { "4tnk", "btr.ai", "v2rl", "ttra" }, MinTime = DateTime.Minutes(9) },
	},
	brutal = {
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1" }, Vehicles = { "4tnk", "btr.ai" }, MaxTime = DateTime.Minutes(8) },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "ttrp", "e1", "e1", "e1", "ttrp" }, Vehicles = { "4tnk", "btr.ai", "v3rl", "v2rl", "ttra" }, MinTime = DateTime.Minutes(8) },
	}
}

ReinforcementsDelay = {
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(7),
	hard = DateTime.Minutes(10),
	vhard = DateTime.Minutes(11),
	brutal = DateTime.Minutes(12)
}

Squads = {
	Main = {
		Delay = AdjustDelayForDifficulty(DateTime.Seconds(210)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 28, Max = 55, RampDuration = DateTime.Minutes(15) }),
		FollowLeader = true,
		ProducerActors = { Infantry = { AlliedSouthBarracks }, Vehicles = { AlliedSouthFactory } },
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Allied),
		AttackPaths = GreeceMainAttackPaths,
	},
	Traitor = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(4)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 25, RampDuration = DateTime.Minutes(15) }),
		FollowLeader = true,
		ProducerActors = { Infantry = { TraitorBarracks }, Vehicles = { TraitorFactory } },
		Compositions = TraitorCompositions,
		AttackPaths = GreeceMainAttackPaths,
	},
	Air = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Allied,
	}
}

DefinePlayers = function()
	USSR = Player.GetPlayer("USSR")
	Greece = Player.GetPlayer("Greece")
	Traitor = Player.GetPlayer("Traitor")
	USSRAbandoned = Player.GetPlayer("USSRAbandoned")
	MissionPlayers = { USSR }
	MissionEnemies = { Greece, Traitor }
end

WorldLoaded = function()
	DefinePlayers()

	TimerTicks = 0
	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(USSR)
	AdjustPlayerStartingCashForDifficulty()
	InitGreece()

	HaloDropper = Actor.Create("powerproxy.halodrop", false, { Owner = USSR })
	ShockDropper = Actor.Create("powerproxy.shockdrop", false, { Owner = USSR })

	ObjectiveKillTraitor = USSR.AddObjective("Find and kill the traitor General Yegorov.")
	ObjectiveFindSovietBase = USSR.AddSecondaryObjective("Take control of abandoned Soviet base.")

	AbandonedHalo.ReturnToBase(AbandonedHelipad)
	SetupRefAndSilosCaptureCredits(Traitor)

	if IsHardOrAbove() then
		Cruiser.Patrol(CruiserPatrolPath)
		AbandonedAirfield.Destroy()
	else
		Cruiser.Destroy()
		HardOnlyMGG.Destroy()
		HardOnlyTurret1.Destroy()
		HardOnlyTurret2.Destroy()
		HardOnlyTurret3.Destroy()
		HardOnlyTurret4.Destroy()
		HardOnlyTurret5.Destroy()
		HardOnlyGapGenerator.Destroy()
		HardOnlyTeslaCoil.Destroy()
		HardOnlyCryoLauncher.Destroy()
	end

	Trigger.OnCapture(AbandonedHelipad, function(self, captor, oldOwner, newOwner)
		if newOwner == USSR then
			AbandonedHalo.Owner = USSR

			if IsNormalOrBelow() then
				Trigger.AfterDelay(DateTime.Seconds(5), function()
					local islandFlare = Actor.Create("flare", true, { Owner = USSR, Location = ReinforcementsDestination.Location })
					Trigger.AfterDelay(DateTime.Seconds(10), islandFlare.Destroy)
					Beacon.New(USSR, ReinforcementsDestination.CenterPosition)
					Media.PlaySpeechNotification(USSR, "SignalFlare")
				end)
			end
		end
	end)

	Trigger.OnEnteredProximityTrigger(GuardsReveal1.CenterPosition, WDist.New(11 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) and not a.HasProperty("Land") then
			Trigger.RemoveProximityTrigger(id)
			local camera = Actor.Create("smallcamera", true, { Owner = USSR, Location = GuardsReveal1.Location })
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				camera.Destroy()
			end)
		end
	end)

	Trigger.OnEnteredProximityTrigger(TraitorTechCenter.CenterPosition, WDist.New(9 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) and a.Type ~= "smig" then
			Trigger.RemoveProximityTrigger(id)
			TraitorTechCenterDiscovered()
		end
	end)

	Trigger.OnEnteredProximityTrigger(AbandonedBaseCenter.CenterPosition, WDist.New(10 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) and not a.HasProperty("Land") then
			Trigger.RemoveProximityTrigger(id)
			AbandonedBaseDiscovered()
		end
	end)

	Trigger.OnKilled(Boris, function(self, killer)
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Notification("Boris has been killed.")
			MediaCA.PlaySound(MissionDir .. "/r2_boriskilled.aud", 2)
		end)
	end)

	Trigger.OnKilled(Bodyguard1, function(self, killer)
		Trigger.AfterDelay(DateTime.Seconds(4), function()
			if not TraitorGeneral.IsDead and TraitorGeneral.IsInWorld then
				TraitorGeneral.Move(TraitorGeneralSafePoint.Location)
			end
		end)
	end)

	Trigger.OnKilled(TraitorGeneral, function(self, killer)
		USSR.MarkCompletedObjective(ObjectiveKillTraitor)
		MediaCA.PlaySound(MissionDir .. "/r2_yegeroveliminated.aud", 2)
	end)

	Trigger.OnAllKilled({ TraitorSAM1, TraitorSAM2 }, function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		HaloDropper.TargetParatroopers(EastParadrop1.CenterPosition, Angle.West)

		Trigger.AfterDelay(DateTime.Seconds(2), function()
			ShockDropper.TargetParatroopers(EastParadrop2.CenterPosition, Angle.West)
		end)

		Trigger.AfterDelay(DateTime.Seconds(4), function()
			HaloDropper.TargetParatroopers(EastParadrop3.CenterPosition, Angle.West)
		end)
	end)

	Trigger.OnCapture(TraitorConyard, function(self, captor, oldOwner, newOwner)
		Trigger.AfterDelay(DateTime.Minutes(1), function()
			InitAlliedAttacks()
		end)
	end)

	Trigger.OnCapture(TraitorTechCenter, function(self, captor, oldOwner, newOwner)
		if newOwner == USSR then
			if ObjectiveCaptureTraitorTechCenter == nil then
				ObjectiveCaptureTraitorTechCenter = USSR.AddSecondaryObjective("Capture Traitor's Tech Center.")
			end
			USSR.MarkCompletedObjective(ObjectiveCaptureTraitorTechCenter)
			Trigger.AfterDelay(DateTime.Seconds(2), function()
				Notification("The traitor's tech center is ours! Let us rain down V3 rockets on the traitor, or perhaps crush him under the tracks of a Mammoth Tank!")
			end)
		end
	end)

	Trigger.OnKilled(TraitorTechCenter, function(self, killer)
		if ObjectiveCaptureTraitorTechCenter ~= nil and not USSR.IsObjectiveCompleted(ObjectiveCaptureTraitorTechCenter) then
			USSR.MarkFailedObjective(ObjectiveCaptureTraitorTechCenter)
		end
	end)

	Trigger.OnKilled(TraitorHQ, function(self, killer)
		TraitorHQKilledOrCaptured()
	end)

	Trigger.OnCapture(TraitorHQ, function(self, captor, oldOwner, newOwner)
		TraitorHQKilledOrCaptured()
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
		Greece.Resources = Greece.ResourceCapacity - 500
		Traitor.Resources = Traitor.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()

		if MissionPlayersHaveNoRequiredUnits() then
			if ObjectiveKillTraitor ~= nil and not USSR.IsObjectiveCompleted(ObjectiveKillTraitor) then
				USSR.MarkFailedObjective(ObjectiveKillTraitor)
			end
		end
	end
end

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % DateTime.Seconds(30) == 0 then
		CalculatePlayerCharacteristics()
	end
end

InitGreece = function()
	if Difficulty == "easy" then
		RebuildExcludes.Greece = { Types = { "gun", "pbox", "pris" } }
	end

	AutoRepairAndRebuildBuildings(Greece, 15)
	SetupRefAndSilosCaptureCredits(Greece)
	AutoReplaceHarvesters(Greece)
	AutoRebuildConyards(Greece)
	InitAiUpgrades(Greece)
	InitAiUpgrades(Traitor)

	Actor.Create("ai.unlimited.power", true, { Owner = Traitor })

	local alliedGroundAttackers = Greece.GetGroundAttackers()

	Utils.Do(alliedGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGreeceGroundHunterUnit)
	end)

	local traitorGroundAttackers = Traitor.GetGroundAttackers()

	Utils.Do(traitorGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGroundHunterUnit)
	end)
end

TraitorTechCenterDiscovered = function()
	if IsTraitorTechCenterDiscovered then
		return
	end

	IsTraitorTechCenterDiscovered = true

	local traitorTechCenterFlare = Actor.Create("flare", true, { Owner = USSR, Location = TraitorTechCenterFlare.Location })
	Trigger.AfterDelay(DateTime.Seconds(10), traitorTechCenterFlare.Destroy)
	Beacon.New(USSR, TraitorTechCenterFlare.CenterPosition)
	Media.PlaySpeechNotification(USSR, "SignalFlare")

	if ObjectiveCaptureTraitorTechCenter == nil then
		ObjectiveCaptureTraitorTechCenter = USSR.AddSecondaryObjective("Capture Traitor's Tech Center.")
		if TraitorTechCenter.IsDead then
			USSR.MarkFailedObjective(ObjectiveCaptureTraitorTechCenter)
		end
	end
end

AbandonedBaseDiscovered = function()
	if IsAbandonedBaseDiscovered then
		return
	end

	IsAbandonedBaseDiscovered = true

	-- Yegorov retreats to HQ
	if TraitorGeneral.IsInWorld then
		TraitorGeneral.Destroy()
	end

	TransferAbandonedBase()

	USSR.MarkCompletedObjective(ObjectiveFindSovietBase)

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		TraitorTechCenterDiscovered()
	end)

	InitAlliedAttacks()

	Trigger.AfterDelay(1, function()
		Actor.Create("QueueUpdaterDummy", true, { Owner = USSR })
	end)

	Trigger.AfterDelay(ReinforcementsDelay[Difficulty], function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		Beacon.New(USSR, ReinforcementsDestination.CenterPosition)
		local reinforcements = { "4tnk", "4tnk", "v3rl", "v3rl", "btr" }
		if IsHardOrAbove() then
			reinforcements = { "4tnk", "4tnk", "v2rl", "btr" }
		end
		Reinforcements.Reinforce(USSR, reinforcements, { ReinforcementsSpawn.Location, ReinforcementsDestination.Location }, 75)
	end)

	Trigger.AfterDelay(DateTime.Seconds(30), function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		ShockDropper.TargetParatroopers(AbandonedBaseCenter.CenterPosition, Angle.SouthWest)
	end)
end

TransferAbandonedBase = function()
	local baseBuildings = Map.ActorsInBox(AbandonedBaseTopLeft.CenterPosition, AbandonedBaseBottomRight.CenterPosition, function(a)
		return a.Owner == USSRAbandoned
	end)

	Utils.Do(baseBuildings, function(a)
		a.Owner = USSR
	end)
end

TraitorHQKilledOrCaptured = function()
	-- Spawn Yegorov (unless he's outside already)
	if not TraitorGeneral.IsInWorld then
		local traitorGeneral = Actor.Create("gnrl", true, { Owner = Traitor, Location = TraitorHQSpawn.Location })
		traitorGeneral.Move(TraitorGeneralSafePoint.Location)
		Trigger.OnKilled(traitorGeneral, function(self, killer)
			MediaCA.PlaySound(MissionDir .. "/r2_yegeroveliminated.aud", 2)
			Trigger.AfterDelay(DateTime.Seconds(2), function()
				USSR.MarkCompletedObjective(ObjectiveKillTraitor)
			end)
		end)
	end
end

InitAlliedAttacks = function()
	if not AttacksStarted then
		AttacksStarted = true
		InitAttackSquad(Squads.Main, Greece)
		InitAttackSquad(Squads.Traitor, Traitor)
		InitAirAttackSquad(Squads.Air, Greece)
	end
end
