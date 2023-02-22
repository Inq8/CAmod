
GreeceMainAttackPaths = {
	{ SouthAttackRally.Location, SouthAttack1.Location },
	{ EastAttackRally.Location, EastAttack1.Location, EastAttack2.Location, EastAttack3a.Location },
	{ EastAttackRally.Location, EastAttack1.Location, EastAttack2.Location, EastAttack3b.Location },
}

CruiserPatrolPath = { CruiserPatrol1.Location, CruiserPatrol2.Location, CruiserPatrol3.Location, CruiserPatrol4.Location, CruiserPatrol5.Location, CruiserPatrol6.Location, CruiserPatrol7.Location, CruiserPatrol8.Location, CruiserPatrol7.Location, CruiserPatrol6.Location, CruiserPatrol5.Location, CruiserPatrol4.Location, CruiserPatrol3.Location, CruiserPatrol2.Location }

TraitorUnits = {
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
}

ReinforcementsDelay = {
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(7),
	hard = DateTime.Minutes(11),
}

Squads = {
	Main = {
		Player = nil,
		Delay = {
			easy = DateTime.Seconds(270),
			normal = DateTime.Seconds(210),
			hard = DateTime.Seconds(150)
		},
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 20 }, { MinTime = DateTime.Minutes(15), Value = 40 } },
			normal = { { MinTime = 0, Value = 34 }, { MinTime = DateTime.Minutes(13), Value = 68 } },
			hard = { { MinTime = 0, Value = 52 }, { MinTime = DateTime.Minutes(11), Value = 105 } },
		},
		QueueProductionStatuses = { Infantry = false, Vehicles = false },
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { AlliedSouthBarracks }, Vehicles = { AlliedSouthFactory } },
		Units = UnitCompositions.Allied.Main,
		AttackPaths = GreeceMainAttackPaths,
	},
	Traitor = {
		Player = nil,
		Delay = {
			easy = DateTime.Minutes(5),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(3)
		},
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 10 }, { MinTime = DateTime.Minutes(15), Value = 20 } },
			normal = { { MinTime = 0, Value = 16 }, { MinTime = DateTime.Minutes(13), Value = 32 } },
			hard = { { MinTime = 0, Value = 28 }, { MinTime = DateTime.Minutes(11), Value = 55 } },
		},
		QueueProductionStatuses = { Infantry = false, Vehicles = false },
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { TraitorBarracks }, Vehicles = { TraitorFactory } },
		Units = TraitorUnits,
		AttackPaths = GreeceMainAttackPaths,
	},
	Air = {
		Player = nil,
		Delay = {
			easy = DateTime.Minutes(13),
			normal = DateTime.Minutes(12),
			hard = DateTime.Minutes(11)
		},
		Interval = {
			easy = DateTime.Minutes(3),
			normal = DateTime.Seconds(150),
			hard = DateTime.Minutes(2)
		},
		QueueProductionStatuses = {
			Aircraft = false
		},
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Aircraft = { "hpad" } },
		Units = {
			easy = {
				{ Aircraft = { "heli" } }
			},
			normal = {
				{ Aircraft = { "heli", "heli" } },
				{ Aircraft = { "harr" } }
			},
			hard = {
				{ Aircraft = { "heli", "heli", "heli" } },
				{ Aircraft = { "harr", "harr" } }
			}
		},
	}
}

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
    Greece = Player.GetPlayer("Greece")
	Traitor = Player.GetPlayer("Traitor")
	USSRAbandoned = Player.GetPlayer("USSRAbandoned")
	MissionPlayer = USSR
	TimerTicks = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(USSR)
	InitGreece()

	HaloDropper = Actor.Create("powerproxy.halodrop", false, { Owner = USSR })
	ShockDropper = Actor.Create("powerproxy.shockdrop", false, { Owner = USSR })

	ObjectiveKillTraitor = USSR.AddObjective("Find and kill the traitor General Yegorov.")
	ObjectiveFindSovietBase = USSR.AddSecondaryObjective("Take control of abandoned Soviet base.")

	AbandonedHalo.ReturnToBase(AbandonedHelipad)
	SetupRefAndSilosCaptureCredits(Traitor)

	if Difficulty == "hard" then
		Cruiser.Patrol(CruiserPatrolPath)
		AbandonedAirfield.Destroy()
		--local traitorConyardLocation = TraitorConyard.Location
		--TraitorConyard.Destroy()
		--Actor.Create("weap", true, { Owner = Traitor, Location = traitorConyardLocation })
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

			if Difficulty ~= "hard" then
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
		if a.Owner == USSR and not a.HasProperty("Land") then
			Trigger.RemoveProximityTrigger(id)
			local camera = Actor.Create("smallcamera", true, { Owner = USSR, Location = GuardsReveal1.Location })
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				camera.Destroy()
			end)
		end
	end)

	Trigger.OnEnteredProximityTrigger(TraitorTechCenter.CenterPosition, WDist.New(9 * 1024), function(a, id)
		if a.Owner == USSR and a.Type ~= "smig" then
			Trigger.RemoveProximityTrigger(id)
			TraitorTechCenterDiscovered()
		end
	end)

	Trigger.OnEnteredProximityTrigger(AbandonedBaseCenter.CenterPosition, WDist.New(10 * 1024), function(a, id)
		if a.Owner == USSR and not a.HasProperty("Land") then
			Trigger.RemoveProximityTrigger(id)
			AbandonedBaseDiscovered()
		end
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

	Trigger.OnCapture(TraitorTechCenter, function(self, captor, oldOwner, newOwner)
		if newOwner == USSR then
			if ObjectiveCaptureTraitorTechCenter == nil then
				ObjectiveCaptureTraitorTechCenter = USSR.AddSecondaryObjective("Capture Traitor's Tech Center.")
			end
			USSR.MarkCompletedObjective(ObjectiveCaptureTraitorTechCenter)
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
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
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
		UpdatePlayerBaseLocation()

		if USSR.HasNoRequiredUnits() then
			if ObjectiveKillTraitor ~= nil and not USSR.IsObjectiveCompleted(ObjectiveKillTraitor) then
				USSR.MarkFailedObjective(ObjectiveKillTraitor)
			end
		end
	end
end

InitGreece = function()
	if Difficulty == "easy" then
		RebuildExcludes.Greece = { Types = { "gun", "pbox", "pris" } }
	end

	AutoRepairAndRebuildBuildings(Greece, 15)
	SetupRefAndSilosCaptureCredits(Greece)
	AutoReplaceHarvesters(Greece)

	Actor.Create("POWERCHEAT", true, { Owner = Traitor })
	Actor.Create("hazmatsoviet.upgrade", true, { Owner = Traitor })

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

	Actor.Create("hazmat.upgrade", true, { Owner = Greece })
	Actor.Create("apb.upgrade", true, { Owner = Greece })

	if Difficulty == "hard" then
		Actor.Create("cryr.upgrade", true, { Owner = Greece })

		Trigger.AfterDelay(DateTime.Minutes(20), function()
			Actor.Create("flakarmor.upgrade", true, { Owner = Greece })
		end)
	end
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

	local baseBuildings = Map.ActorsInBox(AbandonedBaseTopLeft.CenterPosition, AbandonedBaseBottomRight.CenterPosition, function(a)
		return a.Owner == USSRAbandoned
	end)

	Utils.Do(baseBuildings, function(a)
		a.Owner = USSR
	end)

	USSR.MarkCompletedObjective(ObjectiveFindSovietBase)
	TraitorTechCenterDiscovered()

	Trigger.AfterDelay(Squads.Main.Delay[Difficulty], function()
		InitAttackSquad(Squads.Main, Greece)
	end)

	Trigger.AfterDelay(Squads.Traitor.Delay[Difficulty], function()
		InitAttackSquad(Squads.Traitor, Traitor)
	end)

	Trigger.AfterDelay(Squads.Air.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.Air, Greece, Nod, { "harv", "v2rl", "apwr", "tsla", "ttra", "v3rl", "mig", "hind", "suk", "suk.upg", "kiro", "apoc" })
	end)

	Trigger.AfterDelay(1, function()
		Actor.Create("QueueUpdaterDummy", true, { Owner = USSR })
	end)

	if not Boris.IsDead then
		Boris.GrantCondition("autoattack-enabled")
	end

	Trigger.AfterDelay(ReinforcementsDelay[Difficulty], function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		Beacon.New(USSR, ReinforcementsDestination.CenterPosition)
		local reinforcements = { "4tnk", "4tnk", "v3rl", "v3rl", "btr" }
		if Difficulty == "hard" then
			reinforcements = { "4tnk", "4tnk", "v2rl", "btr" }
		end
		Reinforcements.Reinforce(USSR, reinforcements, { ReinforcementsSpawn.Location, ReinforcementsDestination.Location }, 75)
	end)

	Trigger.AfterDelay(DateTime.Seconds(30), function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		ShockDropper.TargetParatroopers(AbandonedBaseCenter.CenterPosition, Angle.SouthWest)
	end)
end

TraitorHQKilledOrCaptured = function()
	-- Spawn Yegorov (unless he's outside already)
	if not TraitorGeneral.IsInWorld then
		local traitorGeneral = Actor.Create("gnrl", true, { Owner = Traitor, Location = TraitorHQSpawn.Location })
		traitorGeneral.Move(TraitorGeneralSafePoint.Location)
		Trigger.OnKilled(traitorGeneral, function(self, killer)
			USSR.MarkCompletedObjective(ObjectiveKillTraitor)
		end)
	end
end
