MissionDir = "ca|missions/main-campaign/ca13-abasement"


ScrinWaterAttackPaths = {
	{ NorthAttackRally.Location, NorthAttack1.Location, NorthAttack2.Location, NorthAttack3.Location },
	{ EastAttackRally.Location, EastAttack1.Location },
}

ScrinGroundAttackPaths = {
	{ SouthAttackRally.Location, SouthAttack1.Location, SouthAttack2a.Location, SouthAttack3.Location },
	{ SouthAttackRally.Location, SouthAttack1.Location, SouthAttack2b.Location, SouthAttack3.Location },
}

SignalTransmitterLocation = SignalTransmitter.Location

if Difficulty == "brutal" then
	table.insert(ScrinWaterCompositions.brutal, { Aircraft = { "deva", "deva", "deva", "deva", "deva", "deva", "deva", "deva" }, MinTime = DateTime.Minutes(20) })
end

Squads = {
	ScrinMain = {
		Delay = AdjustDelayForDifficulty(DateTime.Seconds(150)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 28, Max = 55 }),
		FollowLeader = true,
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin),
		AttackPaths = ScrinGroundAttackPaths,
	},
	ScrinWater = {
		Delay = AdjustDelayForDifficulty(DateTime.Seconds(180)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 25 }),
		Compositions = ScrinWaterCompositions,
		AttackPaths = ScrinWaterAttackPaths,
	},
	ScrinAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(5)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Scrin
	},
	ScrinAirToAir = AirToAirSquad({ "stmr", "enrv", "torm" }, AdjustAirDelayForDifficulty(DateTime.Minutes(10))),
	NodNorth = {
		AttackValuePerSecond = { Min = 10, Max = 10 },
		ActiveCondition = function()
			local portals = Scrin.GetActorsByType("port")
			local warpSpheres = Scrin.GetActorsByType("wsph")
			return #portals > 0 and #warpSpheres > 0
		end,
		FollowLeader = false,
		ProducerActors = { Infantry = { NorthHand } },
		Compositions = { { Infantry = { "n3", "n1", "n1", "n4", "n3", "n1", "n1" } } },
		AttackPaths = { { NodRally.Location, ScrinBaseCenter.Location } }
	},
	NodSouth = {
		AttackValuePerSecond = { Min = 20, Max = 20 },
		ActiveCondition = function()
			local portals = Scrin.GetActorsByType("port")
			local warpSpheres = Scrin.GetActorsByType("wsph")
			return #portals > 0 and #warpSpheres > 0
		end,
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = false,
		ProducerActors = { Infantry = { SouthHand }, Vehicles = { SouthAirstrip } },
		Compositions = {
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
			},
			vhard = {
				{ Infantry = { "n3", "n1", "n1", "n4", "n3", "n1", "n1" }, Vehicles = { "ltnk", "ftnk", "bggy" } },
				{ Infantry = {}, Vehicles = { "bike", "bike", "bike" } }
			},
			brutal = {
				{ Infantry = { "n3", "n1", "n1", "n4", "n3", "n1", "n1" }, Vehicles = { "ltnk", "ftnk", "bggy" } },
				{ Infantry = {}, Vehicles = { "bike", "bike", "bike" } }
			}
		},
		AttackPaths = { { NodRally.Location, ScrinBaseCenter.Location } }
	},
}

SetupPlayers = function()
	USSR = Player.GetPlayer("USSR")
	Nod = Player.GetPlayer("Nod")
	NodAbandoned = Player.GetPlayer("NodAbandoned")
	Scrin = Player.GetPlayer("Scrin")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { USSR }
	MissionEnemies = { Scrin }
end

WorldLoaded = function()
	SetupPlayers()

	TimerTicks = 0
	NodAbandoned.Cash = 0
	NodAbandoned.Resources = 0
	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(USSR)
	AdjustPlayerStartingCashForDifficulty()
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
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveProximityTrigger(id)
			SignalTransmitterDiscovered()
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
		Scrin.Resources = Scrin.ResourceCapacity - 500
		Nod.Resources = Nod.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
		end

		if MissionPlayersHaveNoRequiredUnits() then
			if ObjectiveCaptureSignalTransmitter ~= nil and not USSR.IsObjectiveCompleted(ObjectiveCaptureSignalTransmitter) then
				USSR.MarkFailedObjective(ObjectiveCaptureSignalTransmitter)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()
		CheckNorthBase()
		CheckSouthBase()
	end
end

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % DateTime.Seconds(30) == 0 then
		CalculatePlayerCharacteristics()
	end
end

InitScrin = function()
	if Difficulty == "easy" then
		RebuildExcludes.Scrin = { Types = { "scol", "ptur" } }
	end

	AutoRepairAndRebuildBuildings(Scrin, 15)
	SetupRefAndSilosCaptureCredits(Scrin)
	AutoReplaceHarvesters(Scrin)
	AutoRebuildConyards(Scrin)
	InitAiUpgrades(Scrin)
	InitAttackSquad(Squads.ScrinMain, Scrin)
	InitAttackSquad(Squads.ScrinWater, Scrin)
	InitAirAttackSquad(Squads.ScrinAir, Scrin)

	if IsHardOrAbove() then
		InitAirAttackSquad(Squads.ScrinAirToAir, Scrin, MissionPlayers, { "Aircraft" }, "ArmorType")
	end

	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)
end

InitNod = function()
	Actor.Create("ai.unlimited.power", true, { Owner = Nod })
	Actor.Create("hazmat.upgrade", true, { Owner = Nod })

	-- Prevent Nod forces destroying Signal Transmitter
	Trigger.OnEnteredProximityTrigger(NodAttackLimiter.CenterPosition, WDist.New(8 * 1024), function(a, id)
		if a.Owner == Nod then
			Trigger.ClearAll(a)
			Trigger.AfterDelay(1, function()
				if not a.IsDead and a.HasProperty("Hunt") then
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
		soundNumber = Utils.RandomInteger(1, 7)
	until(soundNumber ~= LastSoundNumber)
	LastSoundNumber = soundNumber

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

	MediaCA.PlaySound(MissionDir .. "/r2_northernnodbasesecured.aud", 2)
	BaseFlipNotification()
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

	MediaCA.PlaySound(MissionDir .. "/r2_southernnodbasesecured.aud", 2)
	BaseFlipNotification()
end

SignalTransmitterDiscovered = function()
	if not IsSignalTransmitterDiscovered then
		IsSignalTransmitterDiscovered = true
		Beacon.New(USSR, SignalTransmitter.CenterPosition)
		Notification("Signal Transmitter located.")
		MediaCA.PlaySound(MissionDir .. "/r2_signaltransmitterlocated.aud", 2)
		local autoCamera = Actor.Create("smallcamera", true, { Owner = USSR, Location = SignalTransmitterLocation })
		Trigger.AfterDelay(DateTime.Seconds(5), autoCamera.Destroy)
	end
end

BaseFlipNotification = function()
	Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(4)), function()
		if not IsFirstBaseFlipped then
			IsFirstBaseFlipped = true
			MediaCA.PlaySound(MissionDir .. "/seth_appreciate.aud", 2)
			Media.DisplayMessage("The Brotherhood appreciates your efforts. We will begin deploying our troops to assist you.", "Nod Commander", HSLColor.FromHex("FF0000"))
		elseif not IsSecondBaseFlipped then
			IsSecondBaseFlipped = true
			MediaCA.PlaySound(MissionDir .. "/seth_kanepleased.aud", 2)
			Media.DisplayMessage("Kane will be pleased. Now focus your efforts on securing the Signal Transmitter.", "Nod Commander", HSLColor.FromHex("FF0000"))
		end
	end)
end
