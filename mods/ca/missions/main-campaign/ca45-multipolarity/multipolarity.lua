MissionDir = "ca|missions/main-campaign/ca45-multipolarity"

SuperweaponsEnabledTime = {
	easy = DateTime.Minutes(40),
	normal = DateTime.Minutes(25),
	hard = DateTime.Minutes(15),
	vhard = DateTime.Minutes(10),
	brutal = DateTime.Minutes(10)
}

GDIAttackPaths = {
	{ WestPath1.Location, WestPath2.Location, WestPath3.Location, WestPath4a.Location },
	{ WestPath1.Location, WestPath2.Location, WestPath3.Location, WestPath4b.Location },
	{ EastPath1.Location, EastPath2.Location, EastPath3.Location, EastPath4.Location, EastPath5a.Location },
	{ EastPath1.Location, EastPath2.Location, EastPath3.Location, EastPath4.Location, EastPath5b.Location },
	{ MiddlePath1.Location, MiddlePath2.Location, MiddlePath3.Location, MiddlePath4a.Location },
	{ MiddlePath1.Location, MiddlePath2.Location, MiddlePath3.Location, MiddlePath4b.Location }
}

Squads = {
	Main = {
		InitTimeAdjustment = -DateTime.Minutes(3),
		Compositions = UnitCompositions.GDI,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		FollowLeader = true,
		AttackPaths = GDIAttackPaths,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(2)),
		ProducerTypes = { Infantry = { "pyle" }, Vehicles = { "weap.td" } },
		RandomProducerActor = false,
	},
	Secondary = {
		InitTimeAdjustment = -DateTime.Minutes(3),
		Compositions = UnitCompositions.GDI,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		FollowLeader = true,
		AttackPaths = GDIAttackPaths,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(3)),
		ProducerTypes = { Infantry = { "pyle" }, Vehicles = { "weap.td" } },
		RandomProducerActor = false,
	},
	Soviet = {
		InitTimeAdjustment = -DateTime.Minutes(3),
		Compositions = UnitCompositions.Soviet,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 10, Max = 20 }),
		FollowLeader = true,
		AttackPaths = { { SovietRally.Location } },
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(2)),
		ProducerTypes = { Infantry = { "barr" }, Vehicles = { "weap" } },
		RandomProducerActor = false,
	},
	Nod = {
		InitTimeAdjustment = -DateTime.Minutes(3),
		Compositions = UnitCompositions.Nod,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 10, Max = 20 }),
		FollowLeader = true,
		AttackPaths = { { NodRally.Location } },
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(2)),
		DispatchDelay = DateTime.Seconds(15),
		ProducerTypes = { Infantry = { "hand" }, Vehicles = { "airs" } },
		RandomProducerActor = false,
	},
	Air = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.GDI,
	},
	AirToAir = AirToAirSquad({ "orca" }, AdjustAirDelayForDifficulty(DateTime.Minutes(10))),
}

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	GDI = Player.GetPlayer("GDI")
	Scrin = Player.GetPlayer("Scrin")
	England = Player.GetPlayer("England")
	USSR = Player.GetPlayer("USSR")
	Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")

	MissionPlayers = { Greece }

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Greece)
	AdjustPlayerStartingCashForDifficulty()
	RemoveActorsBasedOnDifficultyTags()
	InitGDI()

	ObjectiveSecureBase = Greece.AddObjective("Secure the decommissioned Allied base.")

	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage("This area is under GDI jurisdiction. Remove your forces immediately commander. If you advance, we will open fire.", "Gen. Hawthorne", HSLColor.FromHex("F2CF74"))
		MediaCA.PlaySound(MissionDir .. "/hth_jurisdiction.aud", 2)
	end)

	Trigger.OnEnteredProximityTrigger(AlliedBaseCenter.CenterPosition, WDist.New(15 * 1024), function(a, id)
		if a.Owner == Greece and a.Type ~= "flare" then
			Trigger.RemoveProximityTrigger(id)
			if not AlliedBaseFlare.IsDead then
				AlliedBaseFlare.Destroy()
			end
		end
	end)

	Trigger.AfterDelay(1, function()
		local alliedBaseDefenders = Map.ActorsInCircle(AlliedBaseCenter.CenterPosition, WDist.New(10 * 1024), function(a)
			return not a.IsDead and a.Owner == GDI
		end)

		Trigger.OnAllKilled(alliedBaseDefenders, function()
			if not Greece.IsObjectiveCompleted(ObjectiveSecureBase) then
				ObjectiveCaptureHQ = Greece.AddObjective("Capture Gen. Hawthorne's Command Center.")
				Greece.MarkCompletedObjective(ObjectiveSecureBase)

				Trigger.AfterDelay(DateTime.Seconds(3), function()
					Media.DisplayMessage("You will pay dearly for this transgression! Prepare to witness the full force of the GDI war machine!", "Gen. Hawthorne", HSLColor.FromHex("F2CF74"))
					MediaCA.PlaySound(MissionDir .. "/hth_paydearly.aud", 2)
				end)

				FlipAlliedBase()
			end
		end)

		local sovietBaseDefenders = Map.ActorsInCircle(SovietBaseCenter.CenterPosition, WDist.New(15 * 1024), function(a)
			return not a.IsDead and a.Owner == GDI
		end)

		Trigger.OnAllKilled(sovietBaseDefenders, function()
			FlipSovietBase()
		end)

		local nodBaseDefenders = Map.ActorsInCircle(NodBaseCenter.CenterPosition, WDist.New(15 * 1024), function(a)
			return not a.IsDead and a.Owner == GDI
		end)

		Trigger.OnAllKilled(nodBaseDefenders, function()
			FlipNodBase()
		end)
	end)

	Trigger.OnKilled(HawthorneHQ, function(self, killer)
		if not Greece.IsObjectiveCompleted(ObjectiveCaptureHQ) then
			Greece.MarkFailedObjective(ObjectiveCaptureHQ)
		end
	end)

	Trigger.OnCapture(HawthorneHQ, function(self, captor, oldOwner, newOwner)
		if newOwner == Greece and not Greece.IsObjectiveCompleted(ObjectiveCaptureHQ) then
			Greece.MarkCompletedObjective(ObjectiveCaptureHQ)
		end
	end)

	Trigger.OnEnteredProximityTrigger(HawthorneHQ.CenterPosition, WDist.New(15 * 1024), function(a, id)
		if a.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)
			if not FinalTaunt then
				FinalTaunt = true
				Media.DisplayMessage("You not stop me from bringing Kane and his minions to justice!", "Gen. Hawthorne", HSLColor.FromHex("F2CF74"))
				MediaCA.PlaySound(MissionDir .. "/hth_notstop.aud", 2)
			end
		end
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

		if MissionPlayersHaveNoRequiredUnits() then
			if not Greece.IsObjectiveCompleted(ObjectiveSecureBase) then
				Greece.MarkFailedObjective(ObjectiveSecureBase)
			end
			if ObjectiveCaptureHQ ~= nil and not Greece.IsObjectiveCompleted(ObjectiveCaptureHQ) then
				Greece.MarkFailedObjective(ObjectiveCaptureHQ)
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

InitGDI = function()
	AutoRepairAndRebuildBuildings(GDI, 10)
	SetupRefAndSilosCaptureCredits(GDI)
	AutoReplaceHarvesters(GDI)
	AutoRebuildConyards(GDI)

	local GDIGroundAttackers = GDI.GetGroundAttackers()
	Utils.Do(GDIGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGDIGroundHunterUnit)
	end)
end

InitGDIAttacks = function()
	if not GDIAttacksInitialized then
		GDIAttacksInitialized = true
		InitAiUpgrades(GDI)
		InitAttackSquad(Squads.Main, GDI)
		InitAirAttackSquad(Squads.Air, GDI)

		if IsHardOrAbove() then
			Trigger.AfterDelay(DateTime.Minutes(16), function()
				DoDisruptorDrop()
			end)
		end

		Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
			Actor.Create("ai.superweapons.enabled", true, { Owner = GDI })
			Actor.Create("ai.minor.superweapons.enabled", true, { Owner = GDI })
		end)
	end
end

FlipAlliedBase = function()
	local alliedBaseActors = England.GetActors()
	Utils.Do(alliedBaseActors, function(a)
		if not a.IsDead then
			a.Owner = Greece
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(12), function()
		Beacon.New(Greece, McvDest.CenterPosition)
		Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
		Notification("Reinforcements have arrived.")
		Reinforcements.Reinforce(Greece, { "mcv" }, { McvSpawn.Location, McvDest.Location }, 75)
	end)

	InitGDIAttacks()
end

FlipNodBase = function()
	if NodBaseFlipped or SovietBaseFlipped then
		return
	end

	NodBaseFlipped = true

	local nodBaseActors = Utils.Where(Nod.GetActors(), function(a)
		return not a.IsDead and a.Type ~= "player"
	end)

	Utils.Do(nodBaseActors, function(a)
		a.Owner = Greece
	end)

	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage("Two can play that game commander. I think we can put that Soviet equipment to good use!", "Gen. Hawthorne", HSLColor.FromHex("F2CF74"))
		MediaCA.PlaySound(MissionDir .. "/hth_sovequip.aud", 2)

		local sovietBaseActors = Utils.Where(USSR.GetActors(), function(a)
			return not a.IsDead and a.Type ~= "player"
		end)

		Utils.Do(sovietBaseActors, function(a)
			a.Owner = GDI

			Trigger.AfterDelay(1, function()
				if not a.IsDead and a.HasProperty("StartBuildingRepairs") then
					AutoRepairBuilding(a, GDI)
					AutoRebuildBuilding(a, GDI, 10)
					a.StartBuildingRepairs()
				end
			end)
		end)

		InitAttackSquad(Squads.Soviet, GDI)

		Utils.Do({ Coil1.Location, Coil2.Location, Coil3.Location, Coil4.Location, Coil5.Location }, function(wp)
			local tsla = Actor.Create("tsla", true, { Owner = GDI, Location = wp })
			AutoRepairBuilding(tsla, GDI)
			AutoRebuildBuilding(tsla, GDI, 10)
		end)

		Utils.Do({ FlameTower1.Location, FlameTower2.Location, FlameTower3.Location, FlameTower4.Location }, function(wp)
			local ftur = Actor.Create("ftur", true, { Owner = GDI, Location = wp })
			AutoRepairBuilding(ftur, GDI)
			AutoRebuildBuilding(ftur, GDI, 10)
		end)

		local sam = Actor.Create("sam", true, { Owner = GDI, Location = SovietSAM1.Location })
		AutoRepairBuilding(sam, GDI)
		AutoRebuildBuilding(sam, GDI, 10)

		Squads.Main.AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 15, Max = 30 })
		Squads.Secondary.AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 15, Max = 30 })

		Trigger.OnAllKilledOrCaptured({ SovietFactory, SovietBarracks }, function()
			Squads.Main.AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 })
			Squads.Secondary.AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 })
		end)
	end)

	InitGDIAttacks()
end

FlipSovietBase = function()
	if SovietBaseFlipped or NodBaseFlipped then
		return
	end

	SovietBaseFlipped = true

	local sovietBaseActors = Utils.Where(USSR.GetActors(), function(a)
		return not a.IsDead and a.Type ~= "player"
	end)

	Utils.Do(sovietBaseActors, function(a)
		a.Owner = Greece
	end)

	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage("Two can play that game commander. I think we can put that Nod equipment to good use!", "Gen. Hawthorne", HSLColor.FromHex("F2CF74"))
		MediaCA.PlaySound(MissionDir .. "/hth_nodequip.aud", 2)
		InitAttackSquad(Squads.Nod, GDI)

		local nodBaseActors = Utils.Where(Nod.GetActors(), function(a)
			return not a.IsDead and a.Type ~= "player"
		end)

		Utils.Do(nodBaseActors, function(a)
			a.Owner = GDI

			Trigger.AfterDelay(1, function()
				if not a.IsDead and a.HasProperty("StartBuildingRepairs") then
					AutoRepairBuilding(a, GDI)
					AutoRebuildBuilding(a, GDI, 10)
					a.StartBuildingRepairs()
				end
			end)
		end)

		InitAttackSquad(Squads.Nod, GDI)

		Utils.Do({ Obelisk1.Location, Obelisk2.Location, Obelisk3.Location, Obelisk4.Location, Obelisk5.Location }, function(wp)
			local obli = Actor.Create("obli", true, { Owner = GDI, Location = wp })
			AutoRepairBuilding(obli, GDI)
			AutoRebuildBuilding(obli, GDI, 10)
		end)

		Utils.Do({ LasTur1.Location, LasTur2.Location, LasTur3.Location }, function(wp)
			local ltur = Actor.Create("ltur", true, { Owner = GDI, Location = wp })
			AutoRepairBuilding(ltur, GDI)
			AutoRebuildBuilding(ltur, GDI, 10)
		end)

		local nsam = Actor.Create("nsam", true, { Owner = GDI, Location = NodSAM1.Location })
		AutoRepairBuilding(nsam, GDI)
		AutoRebuildBuilding(nsam, GDI, 10)

		Squads.Main.AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 15, Max = 30 })
		Squads.Secondary.AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 15, Max = 30 })

		Trigger.OnAllKilledOrCaptured({ NodAirstrip, NodHand }, function()
			Squads.Main.AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 })
			Squads.Secondary.AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 })
		end)
	end)

	InitGDIAttacks()
end

DoDisruptorDrop = function()
	local gdiFactories = GDI.GetActorsByType("weap.td")
	if #gdiFactories == 0 then
		return
	end

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

	if Difficulty == "brutal" then
		Trigger.AfterDelay(DateTime.Minutes(7), DoDisruptorDrop)
	end
end
