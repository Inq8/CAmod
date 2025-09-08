MissionDir = "ca/missions/main-campaign/ca31-foothold"

SensorZones = { SensorZone1, SensorZone2, SensorZone3, SensorZone4 }

PowerGrids = {
	{
		Providers = { SPower1, SPower2, SPower3 },
		Consumers = { SPowered1, SPowered2, SPowered3, SPowered4, SPowered5 },
	},
}

SpawnedBlobPatrolPaths = {
	{
		BlobPatrol8.Location, BlobPatrol7.Location, BlobPatrol6.Location, BlobPatrol5.Location, BlobPatrol4.Location, BlobPatrol3.Location, BlobPatrol2.Location, BlobPatrol1.Location,
		BlobExtraPatrol1.Location, BlobExtraPatrol2.Location, BlobExtraPatrol3.Location, BlobExtraPatrol4.Location, BlobExtraPatrol5.Location, BlobExtraPatrol1.Location,
		BlobPatrol11.Location, BlobPatrol10.Location, BlobPatrol9.Location
	},
	{
		BlobPatrol9.Location, BlobPatrol10.Location, BlobPatrol11.Location, BlobPatrol1.Location,
		BlobExtraPatrol1.Location, BlobExtraPatrol5.Location, BlobExtraPatrol4.Location, BlobExtraPatrol3.Location, BlobExtraPatrol2.Location, BlobExtraPatrol1.Location,
		BlobPatrol2.Location, BlobPatrol3.Location, BlobPatrol4.Location, BlobPatrol5.Location, BlobPatrol6.Location, BlobPatrol7.Location, BlobPatrol8.Location
	},
}

SpawnLifeformsDelay = {
	vhard = DateTime.Minutes(10),
	brutal = DateTime.Minutes(8)
}

SpawnLifeformsInterval = {
	vhard = DateTime.Minutes(2),
	brutal = DateTime.Minutes(1)
}

ReinforcementsInterval = {
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(4),
	hard = DateTime.Minutes(5),
	vhard = DateTime.Minutes(5),
	brutal = DateTime.Minutes(5)
}

HarvesterDeathDelayTime = {
	easy = DateTime.Seconds(30),
	normal = DateTime.Seconds(25),
	hard = DateTime.Seconds(20),
	vhard = DateTime.Seconds(20),
	brutal = DateTime.Seconds(20)
}

Squads = {
	ScrinMain = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(5)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 40, Max = 80, RampDuration = DateTime.Minutes(12) }),
		FollowLeader = true,
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin),
		AttackPaths = {
			{ ScrinAttack1.Location, ScrinAttack2.Location, ScrinAttack3.Location, ScrinAttack4.Location }
		},
	},
	ScrinWater = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(7)),
		AttackValuePerSecond = {
			normal = { Min = 20, Max = 20,  },
			hard = { Min = 28, Max = 55, RampDuration = DateTime.Minutes(11) },
		},
		FollowLeader = true,
		Compositions = ScrinWaterCompositions,
		AttackPaths = {
			{ ScrinAttack1.Location, ScrinAttack2b.Location, ScrinAttack4.Location }
		},
	},
	ScrinAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(8)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Scrin
	},
}

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Scrin = Player.GetPlayer("Scrin")
	TibLifeforms = Player.GetPlayer("TibLifeforms")
	GatewayOwner = Player.GetPlayer("GatewayOwner")
	MissionPlayers = { GDI }
	MissionEnemies = { Scrin }
	TimerTicks = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(GDI)
	AdjustPlayerStartingCashForDifficulty()
	InitScrin()
	InitTibLifeforms()

	ObjectiveDeploySensorArrays = GDI.AddObjective("Deploy Sensor Arrays at target locations.")
	ObjectiveCaptureNerveCenter = GDI.AddObjective("Capture Scrin Nerve Center.")
	SetupReveals({ Reveal1, Reveal2 })
	CheckSensors()

	if Difficulty ~= "easy" then
		Trigger.AfterDelay(DateTime.Seconds(10), function()
			MediaCA.PlaySound(MissionDir .. "/c_tiblifeforms.aud", 2)
			Notification("Dangerous Tiberium-based lifeforms detected. Recommend keeping your units at a safe distance.")
		end)
	end

	if IsNormalOrBelow() then
		HardOnlyUnit1.Destroy()
		HardOnlyUnit2.Destroy()
		HardOnlyUnit3.Destroy()
		HardOnlyUnit4.Destroy()
		HardOnlyUnit5.Destroy()
	end

	if IsHardOrAbove() then
		NormalEasyOnlyUnit1.Destroy()
		NormalEasyOnlyUnit2.Destroy()
		NormalEasyOnlyUnit3.Destroy()
	end

	Trigger.OnKilled(NerveCenter1, function(self, killer)
		NerveCenterLost()
	end)

	Trigger.OnSold(NerveCenter1, function(self)
		NerveCenterLost()
	end)

	Trigger.OnCapture(NerveCenter1, function(self, captor, oldOwner, newOwner)
		if GDI.IsObjectiveCompleted(ObjectiveCaptureNerveCenter) then
			return
		end

		Trigger.AfterDelay(DateTime.Minutes(1), function()
			Utils.Do({ ScrinRef1, ScrinRef2 }, function(a)
				if not a.IsDead and a.Owner == Scrin then
					a.Sell()
				end
			end)
		end)

		ObjectiveProtectNerveCenter = GDI.AddObjective("Protect the captured Nerve Center.")
		ObjectiveDestroyScrinBase = GDI.AddObjective("Destroy the Scrin base.")
		GDI.MarkCompletedObjective(ObjectiveCaptureNerveCenter)
		BeginScrinAttacks()
		PeriodicReinforcements()

		Trigger.AfterDelay(DateTime.Seconds(3), function()
			MediaCA.PlaySound(MissionDir .. "/c_gatewaystabilized.aud", 2)
			Notification("Interstellar gateway stabilized.")
			GatewayStable = Actor.Create("wormholestable", true, { Owner = GatewayOwner, Location = Gateway.Location })
			Gateway.Destroy()

			Trigger.AfterDelay(DateTime.Seconds(5), function()
				Media.PlaySpeechNotification(GDI, "ReinforcementsArrived")
				Notification("Reinforcements have arrived.")
				Beacon.New(GDI, GatewayStable.CenterPosition)
				Reinforcements.Reinforce(GDI, { "hmmv", "mtnk", "mtnk", "n1", "n1", "n1", "n1", "n3", "amcv" }, { GatewayStable.Location, PlayerStart.Location }, 30)

				Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(4)), function()
					MediaCA.PlaySound(MissionDir .. "/c_protectnervecenter.aud", 2)
					Notification("Do not allow the Nerve Center to be destroyed, the gateway must remain stable.")
				end)
			end)
		end)
	end)

	Trigger.OnAnyKilled({ Sensor1, Sensor2, Sensor3, Sensor4 }, function(killed)
		if not GDI.IsObjectiveCompleted(ObjectiveDeploySensorArrays) then
			GDI.MarkFailedObjective(ObjectiveDeploySensorArrays)
		end
	end)

	Utils.Do(PowerGrids, function(grid)
		Trigger.OnAllKilledOrCaptured(grid.Providers, function()
			Utils.Do(grid.Consumers, function(consumer)
				if not consumer.IsDead then
					consumer.GrantCondition("disabled")
				end
			end)
		end)
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
		Scrin.Resources = Scrin.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
		end

		if not PlayerHasBuildings(Scrin) and ObjectiveDestroyScrinBase ~= nil and not GDI.IsObjectiveCompleted(ObjectiveDestroyScrinBase) then
			GDI.MarkCompletedObjective(ObjectiveProtectNerveCenter)
			GDI.MarkCompletedObjective(ObjectiveDestroyScrinBase)
		end

		CheckSensors()
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

CheckSensors = function()
	if GDI.IsObjectiveCompleted(ObjectiveDeploySensorArrays) then
		return
	end

	NumSensorsDeployed = 0

	Utils.Do(SensorZones, function(z)
		local deployedSensors = Map.ActorsInCircle(z.CenterPosition, WDist.New(10 * 1024), function(a)
			return a.Type == "deployedsensortoken"
		end)

		if #deployedSensors > 0 then
			NumSensorsDeployed = NumSensorsDeployed + 1
		end
	end)

	if NumSensorsDeployed == 4 then
		UserInterface.SetMissionText("")
		GDI.MarkCompletedObjective(ObjectiveDeploySensorArrays)
		SensorZone1.Destroy()
		SensorZone2.Destroy()
		SensorZone3.Destroy()
		SensorZone4.Destroy()

		Trigger.AfterDelay(DateTime.Seconds(2), function()

			local nerveCenterCamera = Actor.Create("camera", true, { Owner = GDI, Location = NerveCenter1.Location })
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				nerveCenterCamera.Destroy()
			end)

			Beacon.New(GDI, NerveCenter1.CenterPosition)
			MediaCA.PlaySound(MissionDir .. "/c_nervecenterlocated.aud", 2)
			Notification("Nerve Center located.")

			Trigger.AfterDelay(DateTime.Seconds(4), function()
				Media.PlaySpeechNotification(GDI, "ReinforcementsArrived")
				Notification("Reinforcements have arrived.")
				Beacon.New(GDI, Gateway.CenterPosition)
				local reinforcements = Reinforcements.Reinforce(GDI, { "n1", "n1", "medi", "n6", "n6", "n2", "n2", "n2" }, { Gateway.Location, PlayerStart.Location }, 6)

				Utils.Do(reinforcements, function(a)
					if a.Type == "n6" then
						Trigger.OnKilled(a, function(self, killer)
							local engis = GDI.GetActorsByType("n6")
							if #engis == 0 and not GDI.IsObjectiveCompleted(ObjectiveCaptureNerveCenter) then
								GDI.MarkFailedObjective(ObjectiveCaptureNerveCenter)
							end
						end)
					end
				end)
			end)

			Trigger.AfterDelay(DateTime.Seconds(9), function()
				if not SPower2.IsDead then
					local powerCamera = Actor.Create("smallcamera", true, { Owner = GDI, Location = SPower2.Location })
					Notification("The Nerve Center is well protected by Storm Columns. Sensors have detected Scrin reactors to the south-east which are powering these defenses.")
					MediaCA.PlaySound(MissionDir .. "/c_nervecenterprotected.aud", 2)
					Beacon.New(GDI, SPower2.CenterPosition)

					Trigger.AfterDelay(DateTime.Seconds(10), function()
						powerCamera.Destroy()
					end)
				end
			end)
		end)
	else
		UserInterface.SetMissionText("Sensor arrays deployed: " .. NumSensorsDeployed .. "/4", HSLColor.Yellow)
	end
end

InitScrin = function()
	RebuildExcludes.Scrin = { Actors = { SPower1, SPower2, SPower3, SPowered1, SPowered2, SPowered3, SPowered4, SPowered5, ScrinSilo1, ScrinSilo2, ScrinSilo3, ScrinRef1, ScrinRef2, StormColumn1, StormColumn2, ShardLauncher1, ShardLauncher2 } }

	AutoRepairAndRebuildBuildings(Scrin, 15)
	SetupRefAndSilosCaptureCredits(Scrin)
	AutoReplaceHarvesters(Scrin)
	AutoRebuildConyards(Scrin)
	InitAiUpgrades(Scrin)

	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)
end

BeginScrinAttacks = function()
	InitAttackSquad(Squads.ScrinMain, Scrin)
	InitAirAttackSquad(Squads.ScrinAir, Scrin)

	if IsNormalOrAbove() then
		InitAttackSquad(Squads.ScrinWater, Scrin)
	end
end

InitTibLifeforms = function()
	if IsNormalOrBelow() then
		Blob1.Destroy()
	end

	if Difficulty == "easy" then
		Blob2.Destroy()
		Blob3.Destroy()
		return
	end

	local blobs = TibLifeforms.GetActorsByType("tbcl")

	local patrolPath2 = { BlobPatrol4.Location, BlobPatrol5.Location, BlobPatrol6.Location, BlobPatrol7.Location, BlobPatrol8.Location, BlobPatrol9.Location, BlobPatrol10.Location, BlobPatrol11.Location, BlobPatrol1.Location, BlobPatrol2.Location, BlobPatrol3.Location }
	local patrolPath3 = { BlobPatrolB1.Location, BlobPatrolB2.Location, BlobPatrolB3.Location, BlobPatrolB4.Location, BlobPatrolB5.Location, BlobPatrolB6.Location }

	Blob2.Patrol(patrolPath2, true)
	Blob3.Patrol(patrolPath3, true)

	if IsHardOrAbove() then
		local patrolPath1 = { BlobPatrol1.Location, BlobPatrol2.Location, BlobPatrol3.Location, BlobPatrol4.Location, BlobPatrol5.Location, BlobPatrol6.Location, BlobPatrol7.Location, BlobPatrol8.Location, BlobPatrol9.Location, BlobPatrol10.Location, BlobPatrol11.Location }
		Blob1.Patrol(patrolPath1, true)

		if IsVeryHardOrAbove() then
			Trigger.AfterDelay(SpawnLifeformsDelay[Difficulty], function()
				SpawnTibLifeform()
			end)
		end
	end
end

PeriodicReinforcements = function()
	local groups = {
		{ "vulc", "mtnk", "n1", "n1", "n1", "n3" },
		{ "n1", "n1", "n1", "n3", "htnk", "msam"  },
		{ "xo", "xo", "titn" },
		{ "titn", "jugg", "n1", "n1", "n1", "n2", "n2" },
		{ "mtnk", "n1", "n3", "xo", "msam" },
	}

	local groupDelay = ReinforcementsInterval[Difficulty]

	Utils.Do(groups, function(g)
		Trigger.AfterDelay(groupDelay, function()
			Media.PlaySpeechNotification(GDI, "ReinforcementsArrived")
			Notification("Reinforcements have arrived.")
			Beacon.New(GDI, GatewayStable.CenterPosition)
			Reinforcements.Reinforce(GDI, g, { GatewayStable.Location, PlayerStart.Location }, 30)
		end)

		groupDelay = groupDelay + ReinforcementsInterval[Difficulty]
	end)

	Trigger.AfterDelay(groupDelay, function()
		PeriodicReinforcements()
	end)
end

NerveCenterLost = function()
	if not GDI.IsObjectiveCompleted(ObjectiveCaptureNerveCenter) then
		GDI.MarkFailedObjective(ObjectiveCaptureNerveCenter)
	end
	if ObjectiveProtectNerveCenter ~= nil and not GDI.IsObjectiveCompleted(ObjectiveProtectNerveCenter) then
		GDI.MarkFailedObjective(ObjectiveProtectNerveCenter)
	end
end

SpawnTibLifeform = function()
	local lifeform = Actor.Create("tbcl", true, { Owner = TibLifeforms, Location = Utils.Random({ TibSpawn1.Location, TibSpawn2.Location }) })
	lifeform.Patrol(Utils.Random(SpawnedBlobPatrolPaths), true)

	Trigger.AfterDelay(SpawnLifeformsInterval[Difficulty], function()
		SpawnTibLifeform()
	end)
end
