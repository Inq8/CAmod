
SensorZones = { SensorZone1, SensorZone2, SensorZone3, SensorZone4 }

PowerGrids = {
	{
		Providers = { SPower1, SPower2, SPower3 },
		Consumers = { SPowered1, SPowered2, SPowered3, SPowered4, SPowered5 },
	},
}

ReinforcementsInterval = {
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(4),
	hard = DateTime.Minutes(5)
}

HarvesterDeathDelayTime = {
	easy = DateTime.Seconds(30),
	normal = DateTime.Seconds(25),
	hard = DateTime.Seconds(20),
}

Squads = {
	ScrinMain = {
		Delay = {
			easy = DateTime.Minutes(7),
			normal = DateTime.Minutes(5),
			hard = DateTime.Minutes(3)
		},
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 20 }, { MinTime = DateTime.Minutes(13), Value = 50 } },
			normal = { { MinTime = 0, Value = 50 }, { MinTime = DateTime.Minutes(11), Value = 100 } },
			hard = { { MinTime = 0, Value = 80 }, { MinTime = DateTime.Minutes(9), Value = 160 } },
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false,
			Aircraft = false,
		},
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Infantry = { "port" }, Vehicles = { "wsph" }, Aircraft = { "grav" } },
		Units = UnitCompositions.Scrin.Main,
		AttackPaths = {
			{ ScrinAttack1.Location, ScrinAttack2.Location, ScrinAttack3.Location, ScrinAttack4.Location }
		},
	},
	ScrinWater = {
		Delay = {
			normal = DateTime.Minutes(7),
			hard = DateTime.Minutes(6)
		},
		AttackValuePerSecond = {
			normal = { { MinTime = 0, Value = 20 } },
			hard = { { MinTime = 0, Value = 28 }, { MinTime = DateTime.Minutes(10), Value = 55 } },
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false,
		},
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Infantry = { "port" }, Vehicles = { "wsph" } },
		Units = {
			normal = {
				{ Vehicles = { "intl.ai2", { "seek", "lace" } }, },
				{ Vehicles = { { "seek", "lace" }, { "seek", "lace" }, { "seek", "lace" } }, },
			},
			hard = {
				{ Vehicles = { "intl", "intl.ai2", "seek" }, },
				{ Vehicles = { "seek", "seek", "seek" }, },
				{ Vehicles = { "lace", "lace", "seek", "seek" }, },
				{ Vehicles = { "devo", "intl.ai2", "ruin" }, MinTime = DateTime.Minutes(7) },
			}
		},
		AttackPaths = {
			{ ScrinAttack1.Location, ScrinAttack2b.Location, ScrinAttack4.Location }
		},
	},
	ScrinAir = {
		Delay = {
			easy = DateTime.Minutes(10),
			normal = DateTime.Minutes(8),
			hard = DateTime.Minutes(6)
		},
		Interval = {
			easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(2)
		},
		QueueProductionStatuses = {
			Aircraft = false
		},
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Aircraft = { "grav" } },
		Units = {
			easy = {
				{ Aircraft = { "stmr" } }
			},
			normal = {
				{ Aircraft = { "stmr", "stmr" } },
				{ Aircraft = { "enrv" } },
			},
			hard = {
				{ Aircraft = { "stmr", "stmr", "stmr" } },
				{ Aircraft = { "enrv", "enrv" } },
			}
		}
	},
}

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Scrin = Player.GetPlayer("Scrin")
	TibLifeforms = Player.GetPlayer("TibLifeforms")
	GatewayOwner = Player.GetPlayer("GatewayOwner")
	MissionPlayers = { GDI }
	TimerTicks = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(GDI)
	AdjustStartingCash()
	InitScrin()
	InitTibLifeforms()

	ObjectiveDeploySensorArrays = GDI.AddObjective("Deploy Sensor Arrays at target locations.")
	ObjectiveCaptureNerveCenter = GDI.AddObjective("Capture Scrin Nerve Center.")
	SetupReveals({ Reveal1, Reveal2 })
	CheckSensors()

	if Difficulty ~= "easy" then
		Trigger.AfterDelay(DateTime.Seconds(10), function()
			MediaCA.PlaySound("c_tiblifeforms.aud", 2)
			Notification("Dangerous Tiberium-based lifeforms detected. Recommend keeping your units at a safe distance.")
		end)
	end

	if Difficulty ~= "hard" then
		HardOnlyUnit1.Destroy()
		HardOnlyUnit2.Destroy()
		HardOnlyUnit3.Destroy()
		HardOnlyUnit4.Destroy()
		HardOnlyUnit5.Destroy()
	end

	if Difficulty == "hard" then
		NormalEasyOnlyUnit1.Destroy()
		NormalEasyOnlyUnit2.Destroy()
		NormalEasyOnlyUnit3.Destroy()
	end

	Trigger.OnRemovedFromWorld(NerveCenter1, function()
		if not GDI.IsObjectiveCompleted(ObjectiveCaptureNerveCenter) then
			GDI.MarkFailedObjective(ObjectiveCaptureNerveCenter)
		end
		if ObjectiveProtectNerveCenter ~= nil and not GDI.IsObjectiveCompleted(ObjectiveProtectNerveCenter) then
			GDI.MarkFailedObjective(ObjectiveProtectNerveCenter)
		end
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
			MediaCA.PlaySound("c_gatewaystabilized.aud", 2)
			Notification("Interstellar gateway stabilized.")
			GatewayStable = Actor.Create("wormholestable", true, { Owner = GatewayOwner, Location = Gateway.Location })
			Gateway.Destroy()

			Trigger.AfterDelay(DateTime.Seconds(5), function()
				Media.PlaySpeechNotification(GDI, "ReinforcementsArrived")
				Notification("Reinforcements have arrived.")
				Beacon.New(GDI, GatewayStable.CenterPosition)
				Reinforcements.Reinforce(GDI, { "hmmv", "mtnk", "mtnk", "n1", "n1", "n1", "n1", "n3", "amcv" }, { GatewayStable.Location, PlayerStart.Location }, 30)

				Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(4)), function()
					MediaCA.PlaySound("c_protectnervecenter.aud", 2)
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
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
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

		if Scrin.HasNoRequiredUnits() and ObjectiveDestroyScrinBase ~= nil and not GDI.IsObjectiveCompleted(ObjectiveDestroyScrinBase) then
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
			MediaCA.PlaySound("c_nervecenterlocated.aud", 2)
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
					MediaCA.PlaySound("c_nervecenterprotected.aud", 2)
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

	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)

	IonConduits = Actor.Create("ioncon.upgrade", true, { Owner = Scrin })

	if Difficulty == "hard" then
		Trigger.AfterDelay(DateTime.Minutes(15), function()
			Actor.Create("carapace.upgrade", true, { Owner = Scrin })
		end)
	end
end

BeginScrinAttacks = function()
	Trigger.AfterDelay(Squads.ScrinMain.Delay[Difficulty], function()
		InitAttackSquad(Squads.ScrinMain, Scrin)
	end)

	Trigger.AfterDelay(Squads.ScrinAir.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.ScrinAir, Scrin, GDI, { "harv.td", "msam", "hsam", "nuke", "nuk2", "orca", "a10", "a10.sw", "a10.gau", "auro", "htnk", "htnk.drone", "htnk.ion", "htnk.hover", "titn", "titn.rail" })
	end)

	if Difficulty ~= "easy" then
		Trigger.AfterDelay(Squads.ScrinWater.Delay[Difficulty], function()
			InitAttackSquad(Squads.ScrinWater, Scrin)
		end)
	end
end

InitTibLifeforms = function()

	if Difficulty ~= "hard" then
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

	if Difficulty == "hard" then
		local patrolPath1 = { BlobPatrol1.Location, BlobPatrol2.Location, BlobPatrol3.Location, BlobPatrol4.Location, BlobPatrol5.Location, BlobPatrol6.Location, BlobPatrol7.Location, BlobPatrol8.Location, BlobPatrol9.Location, BlobPatrol10.Location, BlobPatrol11.Location }
		Blob1.Patrol(patrolPath1, true)
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
