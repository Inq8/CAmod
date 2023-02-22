
ScrinWaterAttackPaths = {
	{ NorthAttackRally.Location, NorthAttack1.Location, NorthAttack2.Location, NorthAttack3.Location },
	{ EastAttackRally.Location, EastAttack1.Location },
}

ScrinGroundAttackPaths = {
	{ SouthAttackRally.Location, SouthAttack1.Location, SouthAttack2a.Location, SouthAttack3.Location },
	{ SouthAttackRally.Location, SouthAttack1.Location, SouthAttack2b.Location, SouthAttack3.Location },
}

SignalTransmitterLocation = SignalTransmitter.Location

Squads = {
	ScrinMain = {
		Delay = {
			easy = DateTime.Seconds(210),
			normal = DateTime.Seconds(150),
			hard = DateTime.Seconds(90)
		},
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 20 }, { MinTime = DateTime.Minutes(14), Value = 40 } },
			normal = { { MinTime = 0, Value = 34 }, { MinTime = DateTime.Minutes(12), Value = 68 } },
			hard = { { MinTime = 0, Value = 52 }, { MinTime = DateTime.Minutes(10), Value = 105 } },
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false,
			Aircraft = false,
		},
		FollowSquadLeader = true,
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Infantry = { "port" }, Vehicles = { "wsph" }, Aircraft = { "grav" } },
		Units = UnitCompositions.Scrin.Main,
		AttackPaths = ScrinGroundAttackPaths,
	},
	ScrinWater = {
		Delay = {
			easy = DateTime.Seconds(240),
			normal = DateTime.Seconds(180),
			hard = DateTime.Seconds(120)
		},
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 10 }, { MinTime = DateTime.Minutes(14), Value = 20 } },
			normal = { { MinTime = 0, Value = 16 }, { MinTime = DateTime.Minutes(12), Value = 32 } },
			hard = { { MinTime = 0, Value = 28 }, { MinTime = DateTime.Minutes(10), Value = 55 } },
		},
		QueueProductionStatuses = {
			Vehicles = false
		},
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Vehicles = { "wsph" } },
		Units = {
			easy = {
				{ Vehicles = { "intl", "seek" }, },
				{ Vehicles = { "seek", "seek" }, },
				{ Vehicles = { "lace", "lace" }, }
			},
			normal = {
				{ Vehicles = { "seek", "intl.ai2" }, },
				{ Vehicles = { "seek", "seek", "seek" }, },
				{ Vehicles = { "lace", "lace", "lace" }, },
			},
			hard = {
				{ Vehicles = { "intl", "intl.ai2", "seek" }, },
				{ Vehicles = { "seek", "seek", "seek" }, },
				{ Vehicles = { "lace", "lace", "seek", "seek" }, },
				{ Vehicles = { "devo", "intl.ai2", "ruin" }, MinTime = DateTime.Minutes(7) },
			}
		},
		AttackPaths = ScrinWaterAttackPaths,
	},
	ScrinAir = {
		Delay = {
			easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(5),
			hard = DateTime.Minutes(4)
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
	NodNorth = {
		Player = nil,
		Interval = {
			easy = DateTime.Seconds(40),
			normal = DateTime.Seconds(80),
			hard = DateTime.Seconds(120)
		},
		ActiveCondition = function()
			local portals = Scrin.GetActorsByType("port")
			local warpSpheres = Scrin.GetActorsByType("wsph")
			return #portals > 0 and #warpSpheres > 0
		end,
		QueueProductionStatuses = { Infantry = false },
		FollowLeader = false,
		IdleUnits = { },
		ProducerActors = { Infantry = { NorthHand } },
		Units = {
			easy = { { Infantry = { "n3", "n1", "n1", "n4", "n3", "n1", "n1" } } },
			normal = { { Infantry = { "n3", "n1", "n1", "n4", "n3", "n1", "n1" } } },
			hard = { { Infantry = { "n3", "n1", "n1", "n4", "n3", "n1", "n1" } } }
		},
		AttackPaths = { { NodRally.Location, ScrinBaseCenter.Location } }
	},
	NodSouth = {
		Player = nil,
		Interval = {
			easy = DateTime.Seconds(40),
			normal = DateTime.Seconds(80),
			hard = DateTime.Seconds(120)
		},
		ActiveCondition = function()
			local portals = Scrin.GetActorsByType("port")
			local warpSpheres = Scrin.GetActorsByType("wsph")
			return #portals > 0 and #warpSpheres > 0
		end,
		DispatchDelay = DateTime.Seconds(15),
		QueueProductionStatuses = { Infantry = false, Vehicles = false },
		FollowLeader = false,
		IdleUnits = { },
		ProducerActors = { Infantry = { SouthHand }, Vehicles = { SouthAirstrip } },
		Units = {
			easy = {
				{ Infantry = { "n3", "n1", "n1", "n4", "n3", "n1", "n1" }, Vehicles = { "ltnk", "ftnk", "arty.nod" } },
				{ Infantry = {}, Vehicles = { "bike", "bike", "bike", "bike", "bggy" } }
			},
			normal = {
				{ Infantry = { "n3", "n1", "n1", "n4", "n3", "n1", "n1" }, Vehicles = { "ltnk", "ftnk", "howi" } },
				{ Infantry = {}, Vehicles = { "bike", "bike", "bike", "bggy" } }
			},
			hard = {
				{ Infantry = { "n3", "n1", "n1", "n4", "n3", "n1", "n1" }, Vehicles = { "ltnk", "ftnk", "bggy" } },
				{ Infantry = {}, Vehicles = { "bike", "bike", "bike" } }
			}
		},
		AttackPaths = { { NodRally.Location, ScrinBaseCenter.Location } }
	},
}

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Nod = Player.GetPlayer("Nod")
	NodAbandoned = Player.GetPlayer("NodAbandoned")
    Scrin = Player.GetPlayer("Scrin")
	MissionPlayer = USSR
	TimerTicks = 0

	NodAbandoned.Cash = 0
	NodAbandoned.Resources = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(USSR)
	InitScrin()
	InitNod()
	SetupLightning()

	ObjectiveCaptureSignalTransmitter = USSR.AddObjective("Locate and capture Scrin Signal Transmitter.")
	ObjectiveSecureNorthNodBase = USSR.AddSecondaryObjective("Secure northern Nod base.")
	ObjectiveSecureSouthNodBase = USSR.AddSecondaryObjective("Secure southern Nod base.")

	Trigger.OnCapture(SignalTransmitter, function(self, captor, oldOwner, newOwner)
		if newOwner == USSR then
			USSR.MarkCompletedObjective(ObjectiveCaptureSignalTransmitter)
		end
	end)

	Trigger.OnKilled(SignalTransmitter, function(self, killer)
		if not USSR.IsObjectiveCompleted(ObjectiveCaptureSignalTransmitter) then
			USSR.MarkFailedObjective(ObjectiveCaptureSignalTransmitter)
		end
	end)

	Trigger.OnEnteredProximityTrigger(SignalTransmitter.CenterPosition, WDist.New(8 * 1024), function(a, id)
		if a.Owner == USSR then
			Trigger.RemoveProximityTrigger(id)
			SignalTransmitterDiscovered()
		end
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Scrin.Resources = Scrin.ResourceCapacity - 500
		Nod.Resources = Nod.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
		end

		if USSR.HasNoRequiredUnits() then
			if ObjectiveCaptureSignalTransmitter ~= nil and not USSR.IsObjectiveCompleted(ObjectiveCaptureSignalTransmitter) then
				USSR.MarkFailedObjective(ObjectiveCaptureSignalTransmitter)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocation()
		CheckNorthBase()
		CheckSouthBase()
	end
end

InitScrin = function()
	if Difficulty == "easy" then
		RebuildExcludes.Scrin = { Types = { "scol", "ptur" } }
	end

	AutoRepairAndRebuildBuildings(Scrin, 15)
	SetupRefAndSilosCaptureCredits(Scrin)
	AutoReplaceHarvesters(Scrin)

	Trigger.AfterDelay(Squads.ScrinMain.Delay[Difficulty], function()
		InitAttackSquad(Squads.ScrinMain, Scrin)
	end)

	Trigger.AfterDelay(Squads.ScrinWater.Delay[Difficulty], function()
		InitAttackSquad(Squads.ScrinWater, Scrin)
	end)

	Trigger.AfterDelay(Squads.ScrinAir.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.ScrinAir, Scrin, USSR, { "harv", "v2rl", "powr", "apwr", "tsla", "ttra", "v3rl", "mig", "hind", "suk", "suk.upg", "kiro", "apoc" })
	end)

	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)

	if Difficulty == "hard" then
		Actor.Create("ioncon.upgrade", true, { Owner = Scrin })

		Trigger.AfterDelay(DateTime.Minutes(20), function()
			Actor.Create("carapace.upgrade", true, { Owner = Scrin })
		end)
	end
end

InitNod = function()
	Actor.Create("POWERCHEAT", true, { Owner = Nod })
	Actor.Create("hazmat.upgrade", true, { Owner = Nod })

	-- Prevent Nod forces destroying Signal Transmitter
	Trigger.OnEnteredProximityTrigger(NodAttackLimiter.CenterPosition, WDist.New(8 * 1024), function(a, id)
		if a.Owner == Nod then
			Trigger.ClearAll(a)
			Trigger.AfterDelay(1, function()
				if not a.IsDead then
					a.Stop()
					a.Move(NodRally.Location)
					a.Hunt()
				end
			end)
			EndNodAttacks = true
		end
	end)
end

SetupLightning = function()
	local nextStrikeDelay = Utils.RandomInteger(DateTime.Seconds(4), DateTime.Seconds(30))
	Trigger.AfterDelay(nextStrikeDelay, function()
		LightningStrike()
		SetupLightning()
	end)
end

LightningStrike = function()
	local duration = Utils.RandomInteger(5, 8)
	local thunderDelay = Utils.RandomInteger(5, 65)
	local soundNumber
	Lighting.Flash("LightningStrike", duration)

	repeat
		soundNumber = Utils.RandomInteger(1, 6)
	until(soundNumber ~= lastSoundNumber)
	lastSoundNumber = soundNumber

	Trigger.AfterDelay(thunderDelay, function()
		Media.PlaySound("thunder" .. soundNumber .. ".aud")
	end)
end

CheckNorthBase = function()
	if not USSR.IsObjectiveCompleted(ObjectiveSecureNorthNodBase) then
		local scrinNearby = Map.ActorsInBox(NorthBaseTopLeft.CenterPosition, NorthBaseBottomRight.CenterPosition, function(a)
			return a.Owner == Scrin and a.Type ~= "camera"
		end)

		if #scrinNearby == 0 then
			USSR.MarkCompletedObjective(ObjectiveSecureNorthNodBase)
			FlipNorthBase()
		end
	end
end

CheckSouthBase = function()
	if not USSR.IsObjectiveCompleted(ObjectiveSecureSouthNodBase) then
		local scrinNearby = Map.ActorsInBox(SouthBaseTopLeft.CenterPosition, SouthBaseBottomRight.CenterPosition, function(a)
			return a.Owner == Scrin and a.Type ~= "camera"
		end)

		if #scrinNearby == 0 then
			USSR.MarkCompletedObjective(ObjectiveSecureSouthNodBase)
			FlipSouthBase()
		end
	end
end

FlipNorthBase = function()
	local northBaseStructures = Map.ActorsInBox(NorthBaseTopLeft.CenterPosition, NorthBaseBottomRight.CenterPosition, function(a)
		return a.Owner == NodAbandoned and not a.IsDead
	end)
	Utils.Do(northBaseStructures, function(a)
		a.Owner = Nod
		if a.HasProperty("StartBuildingRepairs") then
			AutoRepairBuilding(a, Nod)
			AutoRebuildBuilding(a, Nod)
			Trigger.AfterDelay(5, function()
				if not a.IsDead then
					a.StartBuildingRepairs()
				end
			end)
		end
	end)

	InitAttackSquad(Squads.NodNorth, Nod, Scrin)

	local turret1 = Actor.Create("gun.nod", true, { Owner = Nod, Location = NodTurret1.Location })
	AutoRepairBuilding(turret1, Nod)
	AutoRebuildBuilding(turret1, Nod)

	Trigger.AfterDelay(15, function()
		local turret2 = Actor.Create("gun.nod", true, { Owner = Nod, Location = NodTurret2.Location })
		AutoRepairBuilding(turret2, Nod)
		AutoRebuildBuilding(turret2, Nod)
	end)

	Trigger.AfterDelay(30, function()
		local turret3 = Actor.Create("gun.nod", true, { Owner = Nod, Location = NodTurret3.Location })
		AutoRepairBuilding(turret3, Nod)
		AutoRebuildBuilding(turret3, Nod)
	end)
end

FlipSouthBase = function()
	local southBaseStructures = Map.ActorsInBox(SouthBaseTopLeft.CenterPosition, SouthBaseBottomRight.CenterPosition, function(a)
		return a.Owner == NodAbandoned and not a.IsDead
	end)
	Utils.Do(southBaseStructures, function(a)
		a.Owner = Nod
		if a.HasProperty("StartBuildingRepairs") then
			AutoRepairBuilding(a, Nod)
			AutoRebuildBuilding(a, Nod)

			Trigger.AfterDelay(5, function()
				if not a.IsDead then
					a.StartBuildingRepairs()
				end
			end)
		end
	end)

	InitAttackSquad(Squads.NodSouth, Nod, Scrin)

	local airstrips = Nod.GetActorsByType("airs")
	if #airstrips > 0 then
		airstrips[1].Produce("harv.td")
	end

	local turret4 = Actor.Create("gun.nod", true, { Owner = Nod, Location = NodTurret4.Location })
	AutoRepairBuilding(turret4, Nod)
	AutoRebuildBuilding(turret4, Nod)

	Trigger.AfterDelay(15, function()
		local turret5 = Actor.Create("gun.nod", true, { Owner = Nod, Location = NodTurret5.Location })
		AutoRepairBuilding(turret5, Nod)
		AutoRebuildBuilding(turret5, Nod)
	end)
end

SignalTransmitterDiscovered = function()
	if not IsSignalTransmitterDiscovered then
		IsSignalTransmitterDiscovered = true
		Beacon.New(USSR, SignalTransmitter.CenterPosition)
		Notification("Signal Transmitter located.")
		local autoCamera = Actor.Create("smallcamera", true, { Owner = USSR, Location = SignalTransmitterLocation })
		Trigger.AfterDelay(DateTime.Seconds(5), autoCamera.Destroy)
	end
end
