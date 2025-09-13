MissionDir = "ca|missions/main-campaign/ca38-procurement"

OutpostStructures = { OutpostConyard, OutpostFactory, OutpostBarracks, OutpostRefinery, OutpostPower1, OutpostPower2, OutpostPower3, OutpostSilo1, OutpostSilo2, OutpostGuardTower1, OutpostGuardTower2, OutpostGuardTower3, OutpostGuardTower4 }

CommsCenters = { CommsCenter1, CommsCenter2, AdvancedComms }

SuperweaponsEnabledTime = {
	easy = DateTime.Seconds((60 * 45) + 17),
	normal = DateTime.Seconds((60 * 30) + 17),
	hard = DateTime.Seconds((60 * 16) + 17),
	vhard = DateTime.Seconds((60 * 15) + 17),
	brutal = DateTime.Seconds((60 * 14) + 17)
}

AdjustedGDICompositions = AdjustCompositionsForDifficulty(UnitCompositions.GDI)

Squads = {
	GDIMain1 = {
		InitTimeAdjustment = -DateTime.Minutes(1),
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(3)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		FollowLeader = true,
		Compositions = AdjustedGDICompositions,
		AttackPaths = {
			{ Path1_1.Location, Path1_2.Location },
			{ Path2_1.Location, Path2_2.Location },
			{ Path3_1.Location, Path3_2.Location },
		},
	},
	GDIMain2 = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(4)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		FollowLeader = true,
		Compositions = AdjustedGDICompositions,
		AttackPaths = {
			{ Path1_1.Location, Path1_2.Location },
			{ Path2_1.Location, Path2_2.Location },
			{ Path3_1.Location, Path3_2.Location },
		},
	},
	GDIAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.GDI,
	},
	AntiHeavyAir = AntiHeavyAirSquad({ "orcb" }, AdjustAirDelayForDifficulty(DateTime.Minutes(10))),
	AirToAir = AirToAirSquad({ "orca" }, AdjustAirDelayForDifficulty(DateTime.Minutes(10))),
}

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	GDI = Player.GetPlayer("GDI")
	China = Player.GetPlayer("China")
	ChinaHostile = Player.GetPlayer("ChinaHostile")
	MissionPlayers = { USSR }
	MissionEnemies = { GDI }
	TimerTicks = 0

	Camera.Position = McvRally.CenterPosition

	InitObjectives(USSR)
	AdjustPlayerStartingCashForDifficulty()
	InitGDI()
	InitChina()

	ObjectiveAcquireWeapons = USSR.AddObjective("Acquire Chinese weapons.")
	ObjectiveExpelGDI = USSR.AddObjective("Remove the GDI presence.")
	ObjectiveDestroyOutpost = USSR.AddSecondaryObjective("Destroy GDI outpost to receive reinforcements.")

	if IsHardOrAbove() then
		NonHardTroopCrawler.Destroy()
		NonHardOverlord.Destroy()
		NonHardNukeCannon.Destroy()
	end

	Trigger.OnEnteredProximityTrigger(WeaponsCache.CenterPosition, WDist.New(4 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveProximityTrigger(id)
			InitWeaponsCache(true)
		end
	end)

	Trigger.AfterDelay(DateTime.Minutes(10), function()
		InitWeaponsCache(false)
	end)

	Trigger.OnAllKilledOrCaptured(OutpostStructures, function()
		if not McvRequested then
			McvRequested = true
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
				Notification("Reinforcements have arrived.")
				Reinforcements.Reinforce(USSR, { "mcv" }, { McvSpawn.Location, McvRally.Location })
				Beacon.New(USSR, McvRally.CenterPosition)
				McvArrived = true
				USSR.MarkCompletedObjective(ObjectiveDestroyOutpost)
			end)

			InitGDIAttacks()
		end
	end)

	Trigger.AfterDelay(DateTime.Minutes(15), function()
		InitCommsCenterObjective()
	end)

	Utils.Do(CommsCenters, function(c)
		Trigger.OnEnteredProximityTrigger(c.CenterPosition, WDist.New(20 * 1024), function(a, id)
			if IsMissionPlayer(a.Owner) then
				Trigger.RemoveProximityTrigger(id)
				InitCommsCenterObjective()
			end
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

		if MissionPlayersHaveNoRequiredUnits() then
			USSR.MarkFailedObjective(ObjectiveExpelGDI)

			if not USSR.IsObjectiveCompleted(ObjectiveAcquireWeapons) then
				USSR.MarkFailedObjective(ObjectiveAcquireWeapons)
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
	if DateTime.GameTime > 1 and DateTime.GameTime % DateTime.Seconds(30) == 0 then
		CalculatePlayerCharacteristics()
	end
end

InitGDI = function()
	RebuildExcludes.GDI = { Actors = OutpostStructures, Types = { "hq", "eye" } }

	AutoRepairAndRebuildBuildings(GDI, 15)
	SetupRefAndSilosCaptureCredits(GDI)
	AutoReplaceHarvesters(GDI)
	AutoRebuildConyards(GDI)
	InitAiUpgrades(GDI)

	local gdiGroundAttackers = GDI.GetGroundAttackers()

	Utils.Do(gdiGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGDIGroundHunterUnit)
	end)

	Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = GDI })
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = GDI })
	end)

	if IsNormalOrAbove() then
		Trigger.AfterDelay(DateTime.Minutes(16), function()
			DoDisruptorDrop()
		end)
	end

	if IsHardOrAbove() then
		Trigger.AfterDelay(DateTime.Minutes(13), function()
			if not Carrier1.IsDead then
				Carrier1.Patrol({ CarrierPatrol1.Location, CarrierPatrol2.Location, CarrierPatrol3.Location, CarrierPatrol2.Location })
			end
			if not Carrier2.IsDead then
				Carrier2.Patrol({ CarrierPatrol1.Location, CarrierPatrol2.Location, CarrierPatrol3.Location, CarrierPatrol2.Location })
			end
		end)

		Utils.Do({ Carrier1, Carrier2 }, function(c)
			Trigger.OnKilled(c, function(self, killer)
				Trigger.AfterDelay(DateTime.Minutes(3), function()
					if not NavalYard.IsDead and NavalYard.Owner == GDI then
						NavalYard.Produce("cv")
					end
				end)
			end)
		end)

		Trigger.OnProduction(NavalYard, function(producer, produced)
			if produced.Type == "cv" and not produced.IsDead then
				produced.Patrol({ CarrierPatrol1.Location, CarrierPatrol2.Location, CarrierPatrol3.Location, CarrierPatrol2.Location })
			end
		end)
	end
end

InitGDIAttacks = function()
	if not GDIAttacksStarted then
		GDIAttacksStarted = true
		InitAttackSquad(Squads.GDIMain1, GDI)
		InitAttackSquad(Squads.GDIMain2, GDI)
		InitAirAttackSquad(Squads.GDIAir, GDI)
		if IsHardOrAbove() then
			InitAirAttackSquad(Squads.AntiHeavyAir, GDI, MissionPlayers, { "4tnk", "4tnk.atomic", "apoc", "apoc.atomic", "ovld", "ovld.atomic" })
			InitAirAttackSquad(Squads.AirToAir, GDI, MissionPlayers, { "Aircraft" }, "ArmorType")
		end
	end
end

InitWeaponsCache = function(withOutpostFlare)
	if not CacheFound then
		CacheFound = true
		local cacheUnits = China.GetActorsByTypes({"ovld", "trpc.empty", "nukc"})
		Utils.Do(cacheUnits, function(u)
			u.Owner = USSR
		end)

		USSR.MarkCompletedObjective(ObjectiveAcquireWeapons)

		if withOutpostFlare then
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				local outpostFlare = Actor.Create("flare", true, { Owner = USSR, Location = GDIOutpostFlare.Location })
				Media.PlaySpeechNotification(USSR, "SignalFlare")
				Notification("Signal flare detected.")
				Beacon.New(USSR, GDIOutpostFlare.CenterPosition)

				Trigger.OnEnteredProximityTrigger(GDIOutpostFlare.CenterPosition, WDist.New(6 * 1024), function(a, id)
					if IsMissionPlayer(a.Owner) and a.Type ~= "flare" then
						Trigger.RemoveProximityTrigger(id)
						outpostFlare.Destroy()
					end
				end)
			end)
		end
	end
end

InitCommsCenterObjective = function()
	if ObjectiveCaptureComms ~= nil then
		return
	end

	Media.DisplayMessage("Comrade General, we have reason to believe vital information can be found within the GDI comms network. Capture one of their Communications Centers at all costs!", "Premier Cherdenko", HSLColor.FromHex("FF0000"))

	ObjectiveCaptureComms = USSR.AddObjective("Capture a GDI Communications Center.")
	Media.PlaySound("beacon.aud")

	Utils.Do(CommsCenters, function(c)
		local camera = Actor.Create("smallcamera", true, { Owner = USSR, Location = c.Location })
		Beacon.New(USSR, c.CenterPosition)
		Trigger.AfterDelay(DateTime.Seconds(10), function()
			camera.Destroy()
		end)

		Trigger.OnCapture(c, function(self, captor, oldOwner, newOwner)
			if ObjectiveCaptureComms ~= nil and not USSR.IsObjectiveCompleted(ObjectiveCaptureComms) then
				USSR.MarkCompletedObjective(ObjectiveCaptureComms)
			end
		end)
	end)

	Trigger.OnAllKilled(CommsCenters, function()
		if not USSR.IsObjectiveCompleted(ObjectiveCaptureComms) then
			USSR.MarkFailedObjective(ObjectiveCaptureComms)
		end
	end)
end

InitChina = function()
	local chinaUnits = Utils.Where(China.GetActors(), function(a)
		return a.HasProperty("Attack") or a.HasProperty("StartBuildingRepairs")
	end)

	ChineseUnitsKilled = 0

	Utils.Do(chinaUnits, function(a)

		Trigger.OnKilled(a, function(self, killer)
			if self.Owner == China and IsMissionPlayer(killer.Owner) then
				ChineseUnitsKilled = ChineseUnitsKilled + 1
			end

			if ChineseUnitsKilled >= 3 then
				InitChinaRevenge()
			end
		end)
	end)
end

InitChinaRevenge = function()
	if ChinaRevengeStarted then
		return
	end

	ChinaRevengeStarted = true

	Notification("The Chinese are retaliating!")

	local chinaUnits = Utils.Where(China.GetActors(), function(a)
		return a.HasProperty("Attack") or a.HasProperty("StartBuildingRepairs")
	end)

	Utils.Do(chinaUnits, function(a)
		a.Owner = ChinaHostile

		if a.HasProperty("Hunt") then
			a.Hunt()
		end
	end)

	DeployChinese()
end

DeployChinese = function()
	local units = Reinforcements.Reinforce(ChinaHostile, { "ovld", "ovld", "ovld", "ovld", "e1", "e1", "e1", "e1", "e1", "e1", "e1", "e1", "e3", "e3" }, { ChinaHostileSpawn.Location, ChinaHostileRally1.Location, ChinaHostileRally2.Location, ChinaHostileRally3.Location }, 25)
	Utils.Do(units, function(unit)
		Trigger.AfterDelay(5, function()
			AssaultPlayerBaseOrHunt(unit)
		end)
	end)
	Trigger.AfterDelay(DateTime.Seconds(20), DeployChinese)
end

DoDisruptorDrop = function()
	local dropPoints = { DisruptorDropDest1.Location, DisruptorDropDest2.Location }

	if IsVeryHardOrAbove() then
		table.insert(dropPoints, DisruptorDropDest3.Location)
		table.insert(dropPoints, DisruptorDropDest4.Location)
	end

	local delay = 1

	Utils.Do(dropPoints, function(p)
		Trigger.AfterDelay(delay, function()
			local entryPath = { DisruptorDropSpawn.Location, DisruptorDropRally.Location, p }
			local exitPath =  { DisruptorDropExit.Location }
			ReinforcementsCA.ReinforceWithTransport(GDI, "ocar.disr", nil, entryPath, exitPath)
		end)
		delay = delay + DateTime.Seconds(1)
		Trigger.OnEnteredFootprint({p}, function(a, id)
			if a.Owner == GDI and a.Type == "disr" and not a.IsDead then
				Trigger.RemoveFootprintTrigger(id)
				AssaultPlayerBaseOrHunt(a)
			end
		end)
	end)
end
