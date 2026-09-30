MissionDir = "ca|missions/main-campaign/ca51-exodus"

table.insert(UnitCompositions.Scrin, {
	Infantry = { "stlk", "stlk", "stlk", "stlk", "stlk", "stlk", "stlk" },
	Vehicles = { "dark", "gunw", "dark", "dark", "gunw" },
	MaxTime = DateTime.Minutes(10)
})

table.insert(UnitCompositions.Scrin, {
	Infantry = { "s1", "stlk", "s1", "s1", "s1", "stlk", "stlk", "s1", "s1", "stlk", "s1", "s1", "s1", "stlk", "stlk", "stlk", "s1", "s1", "s1" },
	Vehicles = { "dark", "gunw", "dark", "dark", "gunw", "dark" },
	Aircraft = { PacOrDevastator, "pac" },
	MinTime = DateTime.Minutes(17)
})

MaleficAttackPaths = {
	{ MaleficWaypoint1.Location, MaleficWaypoint3.Location, MaleficWaypoint4.Location, MaleficWaypoint5.Location },
	{ MaleficWaypoint2.Location, MaleficWaypoint6.Location, MaleficWaypoint5.Location },
	{ MaleficWaypoint3.Location, MaleficWaypoint4.Location, MaleficWaypoint6.Location },
	{ MaleficWaypoint2.Location, MaleficWaypoint4.Location, MaleficWaypoint5.Location },
	{ MaleficWaypoint1.Location, MaleficWaypoint3.Location, MaleficWaypoint4.Location, MaleficWaypoint7.Location },
}

NumEvacConvoys = {
	easy = 8, -- total time to evacuate = 20 mins
	normal = 10, -- total time to evacuate = 24 mins
	hard = 12, -- total time to evacuate = 28 mins
	vhard = 14, -- total time to evacuate = 32 mins
	brutal = 16 -- total time to evacuate = 36 mins
}

GatewayReorientationTime = {
	easy = DateTime.Minutes(2),
	normal = DateTime.Minutes(2),
	hard = DateTime.Minutes(3),
	vhard = DateTime.Minutes(3),
	brutal = DateTime.Minutes(4)
}

VoidspikeInterval = {
	easy = DateTime.Minutes(4), -- 52 mins to finish
	normal = DateTime.Minutes(3) + DateTime.Seconds(30), -- 48.5 mins to finish
	hard = DateTime.Minutes(3) + DateTime.Seconds(30), -- 45.5 min to finish
	vhard = DateTime.Minutes(3), -- 39 min to finish
	brutal = DateTime.Minutes(2) + DateTime.Seconds(30), -- 32.5 min to finish
}

VoidspikeTargets = {
	VoidspikeTarget1.Location,
	VoidspikeTarget2.Location,
	VoidspikeTarget3.Location,
	VoidspikeTarget4.Location,
	VoidspikeTarget5.Location,
	VoidspikeTarget6.Location,
	VoidspikeTarget7.Location,
	VoidspikeTarget8.Location,
	VoidspikeTarget9.Location,
	VoidspikeTarget10.Location,
	VoidspikeTarget11.Location,
	VoidspikeTarget12.Location,
	VoidspikeTarget13.Location
}

ConvoyUnits = {
	{ "n1", "n1", "n1", "n1", "n1", "n2", "n1", "n3", "n1", "n1", "n2", "n3", "mtnk", "mtnk", "mtnk", "mtnk" },
	{ "n1", "n1", "n1", "n1", "n1", "n2", "n1", "n3", "n1", "n1", "n2", "n3", "vulc", "vulc", "msam" },
	{ "n1", "n1", "n1", "n1", "n1", "n2", "n1", "n3", "n1", "n1", "n2", "n3", "wolv", "wolv", "wolv"  },
	{ "n1", "n1", "n1", "n1", "n1", "n2", "n1", "n3", "n1", "n1", "n2", "n3", "titn", "htnk", "ztrp", "ztrp" },
	{ "n1", "n1", "n1", "n1", "n1", "n2", "n1", "n3", "n1", "n1", "n2", "n3", "disr", "htnk", "ztrp", "zrai" },
}

Squads = {
	MaleficMain = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(2)),
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 40, Max = 80 }),
		FollowLeader = true,
		AttackPaths = MaleficAttackPaths,
	},
	MaleficAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Scrin,
	},
	MaleficCommandoKillers = {
		ActiveCondition = function(squad)
			local commandos = GetMissionPlayersActorsByTypes({ "mast", "rmbo" })
			return #commandos > 0
		end,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 5, Max = 10 }),
		Compositions = { { Aircraft = { "stmr" } } }
	},
}

SetupPlayers = function()
	ScrinRebels = Player.GetPlayer("ScrinRebels")
    Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")
	MaleficScrin = Player.GetPlayer("MaleficScrin")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { ScrinRebels }
	MissionEnemies = { MaleficScrin, GDI }

	Actor.Create("rebel.allegiance", true, { Owner = ScrinRebels })
end

WorldLoaded = function()
	SetupPlayers()

    Camera.Position = PlayerStart.CenterPosition

	InitObjectives(ScrinRebels)
	AdjustPlayerStartingCashForDifficulty()
	RemoveActorsBasedOnDifficultyTags()
	InitMaleficScrin()

	NextVoidspikeTargetIndex = 1
	NextVoidCannonRiftTargetIndex = 1
	NextConvoyCompositionIndex = 1
	NextConvoySpawnIndex = 1
	EvacuationTimerTicks = (DateTime.Minutes(2) * NumEvacConvoys[Difficulty]) + DateTime.Minutes(4)
	GatewayTimerTicks = 0
	UpdateCountdown()
	SetupGDIExit()
	InitGDI()

	Trigger.AfterDelay(VoidspikeInterval[Difficulty], function()
		PlaceNextVoidspike()
	end)

	if IsHardOrAbove() then
		Trigger.AfterDelay(VoidspikeInterval[Difficulty] * 2, function()
			CreateNextVoidCannonRift()
		end)
	end

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Utils.Do({ InitialCamera1, InitialCamera2, InitialCamera3, InitialCamera4 }, function(camera)
			camera.Destroy()
		end)
	end)

	ObjectiveCaptureNerveCenter = ScrinRebels.AddObjective("Capture the gateway Nerve Center.")
	GatewayNerveCenter.GrantCondition("is-objective")

	Trigger.OnCapture(GatewayNerveCenter, function(self, captor, oldOwner, newOwner)
		if IsMissionPlayer(newOwner) then
			ObjectiveHoldNerveCenter = ScrinRebels.AddObjective("Hold the gateway Nerve Center.")
			MediaCA.PlaySound(MissionDir .. "/s_protectnerve.aud", 2)
			Notification("Protect the Nerve Center until the gateway is reoriented.")
			ScrinRebels.MarkCompletedObjective(ObjectiveCaptureNerveCenter)
			Gateway.Owner = ScrinRebels

			local gdiUnits = Utils.Where(GDI.GetActors(), function(a)
				return a.HasProperty("AttackMove")
			end)
			Utils.Do(gdiUnits, function(unit)
				AssaultPlayerBaseOrHunt(unit)
			end)

			EvacuationTimerTicks = 0
			GatewayTimerTicks = GatewayReorientationTime[Difficulty]
			UpdateCountdown()

			Squads.MaleficMain.AttackPaths = {
				{ MaleficWaypoint1.Location, MaleficWaypoint3.Location, MaleficWaypoint8.Location, MaleficWaypoint9.Location },
				{ MaleficWaypoint2.Location, MaleficWaypoint8.Location, MaleficWaypoint9.Location },
			}
		end
	end)

	Trigger.OnKilled(GatewayNerveCenter, function(self, killer, oldOwner, newOwner)
		if ObjectiveCaptureNerveCenter ~= nil and not ScrinRebels.IsObjectiveCompleted(ObjectiveCaptureNerveCenter) then
			ScrinRebels.MarkFailedObjective(ObjectiveCaptureNerveCenter)
		end
		if not IsGatewayReoriented and ObjectiveHoldNerveCenter ~= nil and not ScrinRebels.IsObjectiveCompleted(ObjectiveHoldNerveCenter) then
			ScrinRebels.MarkFailedObjective(ObjectiveHoldNerveCenter)
		end
	end)

	Trigger.OnSold(GatewayNerveCenter, function(self)
		if not IsGatewayReoriented and ObjectiveHoldNerveCenter ~= nil and not ScrinRebels.IsObjectiveCompleted(ObjectiveHoldNerveCenter) then
			ScrinRebels.MarkFailedObjective(ObjectiveHoldNerveCenter)
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage("Attention rebel forces. We will not allow you to disrupt our evacuation. You will be fired upon if you approach. Once our forces have departed, the gateway is all yours.", "GDI Commander", HSLColor.FromHex("F2CF74"))
		MediaCA.PlaySound(MissionDir .. "/g_attention.aud", 2)
	end)

	if IsVeryHardOrAbove() then
		local gdiProductionBuildings = GDI.GetActorsByTypes({ "afac", "weap.td", "pyle", "afld.gdi" })
		for _, b in pairs(gdiProductionBuildings) do
			BuildDefenseOnCaptureAttempt(b, "gtwr", true)
		end

		local maleficProductionBuildings = MaleficScrin.GetActorsByTypes({ "port", "wsph", "sfac", "grav" })
		for _, b in pairs(maleficProductionBuildings) do
			BuildDefenseOnCaptureAttempt(b, "ptur", true)
		end
	end

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
		MaleficScrin.Resources = MaleficScrin.ResourceCapacity - 500
        GDI.Resources = GDI.ResourceCapacity - 500

		if EvacuationTimerTicks > 0 or GatewayTimerTicks > 0 then
			if EvacuationTimerTicks > 0 then
				if EvacuationTimerTicks > 25 then
					EvacuationTimerTicks = EvacuationTimerTicks - 25
				else
					EvacuationTimerTicks = 0
				end
			end
			if GatewayTimerTicks > 0 then
				if GatewayTimerTicks > 25 then
					GatewayTimerTicks = GatewayTimerTicks - 25
				else
					GatewayTimerTicks = 0
					if not IsGatewayReoriented then
						IsGatewayReoriented = true
						Utils.Do({ WormholeSpawn1.Location, WormholeSpawn2.Location, WormholeSpawn3.Location }, function(location)
							Actor.Create("rebelgateway", true, { Owner = ScrinRebels, Location = location })
						end)
						Trigger.AfterDelay(DateTime.Seconds(2), function()
							ScrinRebels.MarkCompletedObjective(ObjectiveHoldNerveCenter)
						end)
					end
				end
			end
			UpdateCountdown()
		end

		if MissionPlayersHaveNoRequiredUnits() then
			if ObjectiveCaptureNerveCenter ~= nil and not ScrinRebels.IsObjectiveCompleted(ObjectiveCaptureNerveCenter) then
				ScrinRebels.MarkFailedObjective(ObjectiveCaptureNerveCenter)
			end
			if ObjectiveHoldNerveCenter ~= nil and not ScrinRebels.IsObjectiveCompleted(ObjectiveHoldNerveCenter) then
				ScrinRebels.MarkFailedObjective(ObjectiveHoldNerveCenter)
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
	if DateTime.GameTime > 1 and DateTime.GameTime % 750 == 0 then
		CalculatePlayerCharacteristics()
	end
end

PlaceNextVoidspike = function()
	local targetLocation = VoidspikeTargets[NextVoidspikeTargetIndex]
	Actor.Create("VoidSpikeSpawner", true, { Location = targetLocation, Owner = MaleficScrin })
	Media.PlaySound("malefic.aud")

	NextVoidspikeTargetIndex = NextVoidspikeTargetIndex + 1
	if NextVoidspikeTargetIndex > #VoidspikeTargets then
		NextVoidspikeTargetIndex = nil
	end

	if NextVoidspikeTargetIndex == nil then
		return
	end

	Trigger.AfterDelay(VoidspikeInterval[Difficulty], function()
		PlaceNextVoidspike()
	end)
end

CreateNextVoidCannonRift = function()
	local targetLocation = VoidspikeTargets[NextVoidCannonRiftTargetIndex]
	Trigger.AfterDelay(DateTime.Seconds(7), function()
		if not VoidCannon.IsDead then
			local targetActor = Actor.Create("VoidCannonTarget", true, { Location = targetLocation, Owner = ScrinRebels })

			Trigger.AfterDelay(1, function()
				VoidCannon.Stop()
				VoidCannon.GrantCondition("charged", 50)
				VoidCannon.Attack(targetActor)
			end)

			Trigger.AfterDelay(DateTime.Seconds(3), function()
				if not VoidCannonAnnounced then
					VoidCannonAnnounced = true
					MediaCA.PlaySound("s_voidcannon.aud", 2)
					Notification("Warning, a Malefic Void Cannon is creating powerful rifts that threaten to consume the entire area. Your forces will not survive an assault against the cannon. You must fall back from any rifts it creates.")
				end
			end)
		end
	end)

	NextVoidCannonRiftTargetIndex = NextVoidCannonRiftTargetIndex + 1
	if NextVoidCannonRiftTargetIndex > #VoidspikeTargets then
		NextVoidCannonRiftTargetIndex = nil
	end

	if NextVoidCannonRiftTargetIndex == nil then
		return
	end

	Trigger.AfterDelay(VoidspikeInterval[Difficulty], function()
		CreateNextVoidCannonRift()
	end)
end

InitMaleficScrin = function()
	RebuildExcludes.MaleficScrin = { Types = { "vcan" } }

	AutoRepairAndRebuildBuildings(MaleficScrin)
	SetupRefAndSilosCaptureCredits(MaleficScrin)
	AutoReplaceHarvesters(MaleficScrin)
	AutoRebuildConyards(MaleficScrin)
	SetupUnitDefenders(MaleficScrin)

    InitMaleficScrinAttacks()
end

InitMaleficScrinAttacks = function()
	InitAiUpgrades(MaleficScrin)
	InitAttackSquad(Squads.MaleficMain, MaleficScrin)
	InitAirAttackSquad(Squads.MaleficAir, MaleficScrin)

	if IsVeryHardOrAbove() then
		InitAirAttackSquad(Squads.MaleficCommandoKillers, Scrin, MissionPlayers, { "mast", "rmbo" })
	end
end

InitGDI = function()
	AutoRepairBuildings(GDI)
	SetupRefAndSilosCaptureCredits(GDI)
	SetupUnitDefenders(GDI)

	InitConvoys()
end

InitConvoys = function()
	local spawnLocations = { EvacSpawn1.Location, EvacSpawn2.Location }

	for convoyIndex = 1, NumEvacConvoys[Difficulty] do
		Trigger.AfterDelay(DateTime.Minutes(2 * (convoyIndex - 1)), function()
			local composition = ConvoyUnits[NextConvoyCompositionIndex]
			local spawnLocation = spawnLocations[NextConvoySpawnIndex]
			local unitDelay = 0
			local firstVehicle = true

			Utils.Do(Utils.Shuffle(composition), function(unitType)
				local scatteredSpawnLoc
				local initFacing
				if spawnLocation == EvacSpawn1.Location then
					scatteredSpawnLoc = CPos.New(
						spawnLocation.X,
						spawnLocation.Y + Utils.RandomInteger(-2, 2)
					)
					initFacing = Angle.West
				else
					scatteredSpawnLoc = CPos.New(
						spawnLocation.X + Utils.RandomInteger(-2, 2),
						spawnLocation.Y
					)
					initFacing = Angle.North
				end
				Trigger.AfterDelay(unitDelay, function()
					local unit = Actor.Create(unitType, true, { Owner = GDI, Location = scatteredSpawnLoc, Health = Utils.RandomInteger(20, 101), Facing = initFacing })
					unit.AttackMoveCA(Gateway.Location)
					if unit.HasTargetType("Vehicle") and firstVehicle then
						Trigger.AfterDelay(1, function()
							local playerActor = ScrinRebels.GetActorsByType("player")[1]
							UtilsCA.DetonateWeapon("WatcherDart", unit.CenterPosition, playerActor)
						end)
						firstVehicle = false
					end
				end)
				unitDelay = unitDelay + 18
			end)

			NextConvoyCompositionIndex = NextConvoyCompositionIndex + 1
			if NextConvoyCompositionIndex > #ConvoyUnits then
				NextConvoyCompositionIndex = 1
			end

			NextConvoySpawnIndex = NextConvoySpawnIndex + 1
			if NextConvoySpawnIndex > #spawnLocations then
				NextConvoySpawnIndex = 1
			end
		end)
	end

	Trigger.AfterDelay(DateTime.Minutes(2 * NumEvacConvoys[Difficulty]), EvacuateRemainingUnits)
end

EvacuateRemainingUnits = function()
	local units = Utils.Where(GDI.GetActors(), function(a)
		return a.HasProperty("Move")
	end)

	table.sort(units, function(a, b)
		return a.Location.X > b.Location.X
	end)

	local evacuationDelay = 0
	Utils.Do(units, function(a)
		Trigger.AfterDelay(evacuationDelay, function()
			if not a.IsDead then
				if a.Type == "msar" then
					a.Undeploy()
				end
				if a.HasProperty("AttackMoveCA") then
					a.AttackMoveCA(Gateway.Location)
				else
					a.MoveCA(Gateway.Location)
				end
			end
		end)
		evacuationDelay = evacuationDelay + DateTime.Seconds(1)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(30), function()
		Utils.Do(GDI.GetActors(), function(a)
			if a.HasProperty("Move") then
				a.MoveCA(Gateway.Location)
			end
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(2), function()
		Utils.Do(GDI.GetActorsByTypes({ "cram", "atwr", "gtwr" }), function(a)
			a.Sell()
		end)

		Utils.Do(GDI.GetActorsByTypes({ "hq", "eye" }), function(a)
			a.GrantCondition("powerdown")
		end)

		Utils.Do(GDI.GetActors(), function(a)
			if a.HasProperty("StartBuildingRepairs") or a.Type == "hosp" or a.Type == "macs" then
				a.Owner = Neutral
			elseif a.HasProperty("Move") then
				a.MoveCA(Gateway.Location)
			end
		end)

		Notification("Most GDI forces have now departed.")
		MediaCA.PlaySound(MissionDir .. "/s_gdideparted.aud", 2)
	end)
end

SetupGDIExit = function()
	Trigger.OnEnteredProximityTrigger(Gateway.CenterPosition, WDist.FromCells(3), function(a)
		if a.Owner == GDI and a ~= Gateway and not a.IsDead then
			a.Destroy()
		end
	end)
end

UpdateCountdown = function()
	if GatewayTimerTicks > 0 then
		UserInterface.SetMissionText("Gateway reorientation in: " .. UtilsCA.FormatTimeForGameSpeed(GatewayTimerTicks), HSLColor.Yellow)
	elseif EvacuationTimerTicks > 0 then
		UserInterface.SetMissionText("Estimated evacuation time remaining: " .. UtilsCA.FormatTimeForGameSpeed(EvacuationTimerTicks), HSLColor.Yellow)
	else
		UserInterface.SetMissionText("")
	end
end
