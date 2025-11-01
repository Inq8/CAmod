MissionDir = "ca|missions/main-campaign/ca45-multipolarity"

SuperweaponsEnabledTime = {
	easy = DateTime.Minutes(40),
	normal = DateTime.Minutes(25),
	hard = DateTime.Minutes(15),
	vhard = DateTime.Minutes(10),
	brutal = DateTime.Minutes(10)
}

McvDelayTime = {
	easy = DateTime.Seconds(10),
	normal = DateTime.Seconds(10),
	hard = DateTime.Seconds(10),
	vhard = DateTime.Seconds(10),
	brutal = DateTime.Seconds(10)
}

GDIWestAttackPaths = {
	{ WestPath1.Location, WestPath2.Location, WestPath3.Location, WestPath4a.Location },
	{ WestPath1.Location, WestPath2.Location, WestPath3.Location, WestPath4b.Location },
}

GDIMiddleAttackPaths = {
	{ MiddlePath1.Location, MiddlePath2.Location, MiddlePath3.Location, MiddlePath4a.Location },
	{ MiddlePath1.Location, MiddlePath2.Location, MiddlePath3.Location, MiddlePath4b.Location },
}

GDIEastAttackPaths = {
	{ EastPath1.Location, EastPath2.Location, EastPath3.Location, EastPath4.Location, EastPath5a.Location },
	{ EastPath1.Location, EastPath2.Location, EastPath3.Location, EastPath4.Location, EastPath5b.Location },
}

CommandoDropTime = {
	easy = DateTime.Minutes(16), -- not used
	normal = DateTime.Minutes(14), -- not used
	hard = DateTime.Minutes(12),
	vhard = DateTime.Minutes(10),
	brutal = DateTime.Minutes(8)
}

ZoneRaidTime = {
	easy = DateTime.Minutes(9),
	normal = DateTime.Minutes(8),
	hard = DateTime.Minutes(7),
	vhard = DateTime.Minutes(6),
	brutal = DateTime.Minutes(5)
}

EngiDropTime = {
	easy = DateTime.Minutes(12), -- not used
	normal = DateTime.Minutes(10), -- not used
	hard = DateTime.Minutes(8),
	vhard = DateTime.Minutes(6),
	brutal = DateTime.Minutes(4)
}

FirstAutoBaseClaimTime = {
	easy = DateTime.Minutes(30), -- not used
	normal = DateTime.Minutes(26), -- not used
	hard = DateTime.Minutes(22),
	vhard = DateTime.Minutes(18),
	brutal = DateTime.Minutes(14)
}

SecondAutoBaseClaimTime = {
	easy = DateTime.Minutes(40), -- not used
	normal = DateTime.Minutes(36), -- not used
	hard = DateTime.Minutes(32),
	vhard = DateTime.Minutes(28),
	brutal = DateTime.Minutes(24)
}

CaptureTargets = {}

if IsVeryHardOrAbove() then
	table.insert(UnitCompositions.GDI, {
		Infantry = { "zrai", "zrai", "zrai", "zrai", "zrai", "zrai", "zrai", "zrai", "zrai" },
		Vehicles = { "vulc", "vulc", "xo", "xo", "xo", "xo", "vulc", "vulc" },
		MinTime = DateTime.Minutes(14),
		IsSpecial = true
	})
end

Squads = {
	Main = {
		InitTimeAdjustment = -DateTime.Minutes(3),
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.GDI),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		FollowLeader = true,
		AttackPaths = Utils.Concat(GDIWestAttackPaths, GDIMiddleAttackPaths),
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(2)),
		ProducerActors = { Infantry = { WestBarracks }, Vehicles = { WestFactory } },
		ProducerTypes = { Infantry = { "pyle" }, Vehicles = { "weap.td" } },
	},
	Secondary = {
		InitTimeAdjustment = -DateTime.Minutes(3),
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.GDI),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		FollowLeader = true,
		AttackPaths = Utils.Concat(GDIMiddleAttackPaths, GDIEastAttackPaths),
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(3)),
		ProducerActors = { Infantry = { EastBarracks }, Vehicles = { EastFactory } },
		ProducerTypes = { Infantry = { "pyle" }, Vehicles = { "weap.td" } },
	},
	Soviet = {
		InitTimeAdjustment = -DateTime.Minutes(3),
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Soviet),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 10, Max = 20 }),
		FollowLeader = true,
		AttackPaths = { { SovietRally.Location } },
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(2)),
		ProducerTypes = { Infantry = { "barr" }, Vehicles = { "weap" } },
	},
	Nod = {
		InitTimeAdjustment = -DateTime.Minutes(3),
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Nod),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 10, Max = 20 }),
		FollowLeader = true,
		AttackPaths = { { NodRally.Location } },
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(2)),
		DispatchDelay = DateTime.Seconds(15),
		ProducerTypes = { Infantry = { "hand" }, Vehicles = { "airs" } },
	},
	Air = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(10)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 20 }),
		Compositions = AirCompositions.GDI,
	},
	AirToAir = AirToAirSquad({ "orca" }, AdjustAirDelayForDifficulty(DateTime.Minutes(10))),
	BrutalAirAntiDefense = {
		Delay = DateTime.Minutes(12),
		ActiveCondition = function(squad)
			return MissionPlayersDefenseValue > 3000
		end,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 30 }),
		Compositions = function(squad)
			local unitTypes = { "orca", "orcb", "auro" }
			local units = { unitTypes }
			local desiredCount = MissionPlayersDefenseValue / 2000
			for i = 1, math.min(desiredCount, MaxSpecialistAir[Difficulty]) do
				table.insert(units, unitTypes)
			end
			return { { Aircraft = units } }
		end
	}
}

DefinePlayers = function()
	Greece = Player.GetPlayer("Greece")
	GDI = Player.GetPlayer("GDI")
	Scrin = Player.GetPlayer("Scrin")
	England = Player.GetPlayer("England")
	USSR = Player.GetPlayer("USSR")
	Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { Greece }
	MissionEnemies = { GDI }
end

WorldLoaded = function()
	DefinePlayers()

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Greece)
	AdjustPlayerStartingCashForDifficulty()
	RemoveActorsBasedOnDifficultyTags()
	InitGDI()

	MissionPlayersDefenseValue = 0

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
			return not a.IsDead and a.Owner == GDI and a.Type ~= "camera"
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
			return not a.IsDead and a.Owner == GDI and a.Type ~= "camera"
		end)

		Trigger.OnAllKilled(sovietBaseDefenders, function()
			FlipSovietBase()
		end)

		local nodBaseDefenders = Map.ActorsInCircle(NodBaseCenter.CenterPosition, WDist.New(15 * 1024), function(a)
			return not a.IsDead and a.Owner == GDI and a.Type ~= "camera"
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
		if IsMissionPlayer(newOwner) and not Greece.IsObjectiveCompleted(ObjectiveCaptureHQ) then
			DoFinale()
		end
	end)

	Trigger.OnEnteredProximityTrigger(HawthorneHQ.CenterPosition, WDist.New(15 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveProximityTrigger(id)
			if not FinalTaunt then
				FinalTaunt = true
				Media.DisplayMessage("You not stop me from bringing Kane and his minions to justice!", "Gen. Hawthorne", HSLColor.FromHex("F2CF74"))
				MediaCA.PlaySound(MissionDir .. "/hth_notstop.aud", 2)
			end
		end
	end)

	Trigger.AfterDelay(ZoneRaidTime[Difficulty], DoZoneRaid)

	if IsHardOrAbove() then
		Trigger.AfterDelay(CommandoDropTime[Difficulty], DoCommandoDrop)

		Trigger.AfterDelay(FirstAutoBaseClaimTime[Difficulty], function()
			HawthorneClaimRandomBase()
		end)

		Trigger.AfterDelay(SecondAutoBaseClaimTime[Difficulty], function()
			HawthorneClaimRandomBase()

			if IsVeryHardOrAbove() then
				Squads.Main.AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 })
				Squads.Secondary.AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 })
			end
		end)
	end

	if Difficulty == "brutal" then
		DoCommandoDrop()
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

		if Difficulty == "brutal" then
			CalculateDefensesValue()
		end
	end
end

CalculateDefensesValue = function()
	local defenseValue = 0
	Utils.Do(MissionPlayers, function(p)
		local defenses = p.GetActorsByArmorTypes({ "Concrete" })

		Utils.Do(defenses, function(d)
			if UnitCosts[d.Type] == nil then
				UnitCosts[d.Type] = ActorCA.CostOrDefault(d.Type)
			end
			defenseValue = defenseValue + UnitCosts[d.Type]
		end)
	end)
	MissionPlayersDefenseValue = defenseValue
end

InitGDI = function()
	AutoRepairAndRebuildBuildings(GDI, 10)
	SetupRefAndSilosCaptureCredits(GDI)
	AutoReplaceHarvesters(GDI)
	AutoRebuildConyards(GDI)

	local GDIGroundAttackers = GDI.GetGroundAttackers()
	Utils.Do(GDIGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), function(a)
			return IsGDIGroundHunterUnit(a) and a.Type ~= "zrai" -- exclude zone raiders so they aren't interrupted before the jump
		end)
	end)

	local productionBuildings = GDI.GetActorsByTypes({ "pyle", "afac", "weap.td", "afld.gdi" })
	for _, b in pairs(productionBuildings) do
		SellOnCaptureAttempt(b)
	end

	UpgradeCenter.GrantCondition("tower.rocket")
end

InitGDIAttacks = function()
	if not GDIAttacksInitialized then
		GDIAttacksInitialized = true
		InitAiUpgrades(GDI)
		InitAttackSquad(Squads.Main, GDI)
		InitAirAttackSquad(Squads.Air, GDI)

		if IsHardOrAbove() then
			InitAirAttackSquad(Squads.AirToAir, GDI, MissionPlayers, { "Aircraft" }, "ArmorType")

			Trigger.AfterDelay(DateTime.Minutes(16), function()
				DoDisruptorDrop()
			end)

			if IsVeryHardOrAbove() then
				Actor.Create("ai.supportpowers.enabled", true, { Owner = GDI })

				if Difficulty == "brutal" then
					InitAirAttackSquad(Squads.BrutalAirAntiDefense, GDI, MissionPlayers, { "Concrete" }, "ArmorType")
				end
			end
		end

		Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
			Actor.Create("ai.superweapons.enabled", true, { Owner = GDI })
			Actor.Create("ai.minor.superweapons.enabled", true, { Owner = GDI })
		end)
	end
end

FlipAlliedBase = function()
	local alliedBaseActors = Utils.Where(England.GetActors(), function(a)
		return not a.IsDead and a.Type ~= "player"
	end)

	Utils.Do(alliedBaseActors, function(a)
		a.Owner = Greece
	end)

	Trigger.AfterDelay(1, function()
		Actor.Create("QueueUpdaterDummy", true, { Owner = Greece })
	end)

	Trigger.AfterDelay(McvDelayTime[Difficulty], function()
		Beacon.New(Greece, McvDest.CenterPosition)
		Media.PlaySpeechNotification(nil, "ReinforcementsArrived")
		Notification("Reinforcements have arrived.")
		Reinforcements.Reinforce(Greece, { "mcv" }, { McvSpawn.Location, McvDest.Location }, 75)
		Actor.Create("mcv.allowed", true, { Owner = Greece })
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

	Trigger.AfterDelay(1, function()
		Actor.Create("QueueUpdaterDummy", true, { Owner = Greece })
	end)

	Trigger.AfterDelay(DateTime.Seconds(6), function()
		HawthorneClaimSovietBase()
	end)

	InitGDIAttacks()

	if IsHardOrAbove() then
		Trigger.AfterDelay(EngiDropTime[Difficulty], DoEngiDrop)
	end
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

	Trigger.AfterDelay(1, function()
		Actor.Create("QueueUpdaterDummy", true, { Owner = Greece })
	end)

	Trigger.AfterDelay(DateTime.Seconds(6), function()
		HawthorneClaimNodBase()
	end)

	InitGDIAttacks()

	if IsHardOrAbove() then
		Trigger.AfterDelay(EngiDropTime[Difficulty], DoEngiDrop)
	end
end

HawthorneClaimRandomBase = function()
	if HawthorneClaimedSovietBase and HawthorneClaimedNodBase then
		return
	elseif HawthorneClaimedSovietBase then
		HawthorneClaimNodBase()
	elseif HawthorneClaimedNodBase then
		HawthorneClaimSovietBase()
	else
		local choice = Utils.Random({ "Soviet", "Nod" })
		if choice == "Soviet" then
			HawthorneClaimSovietBase()
		else
			HawthorneClaimNodBase()
		end
	end
end

HawthorneClaimSovietBase = function()
	if HawthorneClaimedSovietBase then
		return
	end

	HawthorneClaimedSovietBase = true

	if NodBaseFlipped then
		Media.DisplayMessage("Two can play that game commander. I think we can put that Soviet equipment to good use!", "Gen. Hawthorne", HSLColor.FromHex("F2CF74"))
		MediaCA.PlaySound(MissionDir .. "/hth_sovequip.aud", 2)
	else
		Media.DisplayMessage("I think it's high time I got some use out of that old Soviet base!", "Gen. Hawthorne", HSLColor.FromHex("F2CF74"))
		MediaCA.PlaySound(MissionDir .. "/hth_sovequipauto.aud", 2)
	end

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
end

HawthorneClaimNodBase = function()
	if HawthorneClaimedNodBase then
		return
	end

	HawthorneClaimedNodBase = true

	if SovietBaseFlipped then
		Media.DisplayMessage("Two can play that game commander. I think we can put that Nod equipment to good use!", "Gen. Hawthorne", HSLColor.FromHex("F2CF74"))
		MediaCA.PlaySound(MissionDir .. "/hth_nodequip.aud", 2)
	else
		Media.DisplayMessage("That Nod base has been sitting idle for too long. It's time I got some use out of it!", "Gen. Hawthorne", HSLColor.FromHex("F2CF74"))
		MediaCA.PlaySound(MissionDir .. "/hth_nodequipauto.aud", 2)
	end

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
			local exitPath =  { LeftDropExit.Location }
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

DoCommandoDrop = function()
	local entryPath
	entryPath = { RightDropSpawn.Location, CommandoDropWp1.Location, CommandoDropDest.Location }
	local chinookDropUnits = { "rmbo" }

	DoHelicopterDrop(GDI, entryPath, "tran.paradrop", chinookDropUnits, AssaultPlayerBaseOrHunt, function(t)
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			if not t.IsDead then
				t.Move(RightDropExit.Location)
				t.Destroy()
			end
		end)
	end)
end

DoEngiDrop = function()
	local entryPath
	local exitLoc

	if SovietBaseFlipped then
		entryPath = { LeftDropSpawn.Location, LeftEngiDropDest.Location }
		exitLoc = LeftDropExit.Location
	else
		entryPath = { RightDropSpawn.Location, RightEngiDropDest.Location }
		exitLoc = RightDropExit.Location
	end

	local chinookDropUnits = { "n6", "n6", "n6", "n6", "n6" }

	DoHelicopterDrop(GDI, entryPath, "tran.paradrop", chinookDropUnits, function(a)
		if not a.IsDead then
			CaptureRandomBuilding(a)
		end
	end, function(t)
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			if not t.IsDead then
				t.Move(exitLoc)
				t.Destroy()
			end
		end)
	end)
end

DoZoneRaid = function()
	local zoneRaiders = { ZR1, ZR2, ZR3, ZR4, ZR5, ZR6, ZR7, ZR8 }
	Utils.Do(zoneRaiders, function(a)
		if not a.IsDead then
			a.Move(ZRWP1.Location)
			a.TargetedLeap(ZRWP2.Location)
			local finalDest = Utils.Random({ ZRWP5a.Location, ZRWP5b.Location, ZRWP5c.Location })
			a.TargetedLeap(finalDest)
			AssaultPlayerBaseOrHunt(a)
		end
	end)
end

CaptureRandomBuilding = function(engi)
	local buildings = Map.ActorsInCircle(engi.CenterPosition, WDist.New(15 * 1024), function(a)
		return not a.IsDead and IsMissionPlayer(a.Owner) and a.HasProperty("StartBuildingRepairs") and engi.CanCapture(a) and not CaptureTargets[tostring(a)]
	end)

	if #buildings == 0 then
		buildings = Greece.GetActorsByTypes({ "fact", "proc" })
	end

	if #buildings == 0 then
		return
	end

	local target = Utils.Random(buildings)
	CaptureTargets[tostring(target)] = true
	engi.Capture(target)
end

DoFinale = function()
	Media.DisplayMessage("This is far from over! You will regret making an enemy of me!", "Gen. Hawthorne", HSLColor.FromHex("F2CF74"))
	MediaCA.PlaySound(MissionDir .. "/hth_farfromover.aud", 2)

	Hawthorne = Actor.Create("xo.hawthorne", true, { Owner = GDI, Location = HawthorneSpawn.Location })
	Hawthorne.TargetedLeap(HawthorneJumpDest.Location)
	Hawthorne.Move(Gateway.Location)
	Hawthorne.Destroy()

	Trigger.OnRemovedFromWorld(Hawthorne, function(a)
		Greece.MarkCompletedObjective(ObjectiveCaptureHQ)
	end)

	Trigger.AfterDelay(DateTime.Seconds(30), function()
		Greece.MarkCompletedObjective(ObjectiveCaptureHQ)
	end)
end
