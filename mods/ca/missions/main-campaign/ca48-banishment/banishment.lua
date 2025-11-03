MissionDir = "ca|missions/main-campaign/ca48-banishment"

SuperweaponsEnabledTime = {
	easy = DateTime.Minutes(300),
	normal = DateTime.Minutes(120),
	hard = DateTime.Minutes(60),
	vhard = DateTime.Minutes(40),
	brutal = DateTime.Minutes(30)
}

table.insert(UnitCompositions.Scrin, {
	Infantry = { "s1", "stlk", "s1", "s1", "s1", "stlk", "stlk", "s1", "s1", "stlk", "s1", "s1", "s1", "stlk", "stlk", "stlk", "s1", "s1", "s1" },
	Vehicles = { "dark", "gunw", "dark", "dark", "gunw", "dark" },
	Aircraft = { PacOrDevastator, "pac" },
	MinTime = DateTime.Minutes(17)
})

NorthAttackPaths = {}
EastAttackPaths = {}
WestAttackPaths = {}

MainSquadAttackValues = function(squad)
	local minValue
	local maxValue

	if NumBasesSecured == 1 then
		minValue = 10
		maxValue = 20
	else
		minValue = 45 / NumScrinBasesActive
		maxValue = 85 / NumScrinBasesActive
	end

	return AdjustAttackValuesForDifficulty({ Min = minValue, Max = maxValue })
end

Squads = {
	North = {
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin),
		AttackValuePerSecond = MainSquadAttackValues,
		FollowLeader = true,
		AttackPaths = NorthAttackPaths,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(1)),
		ProducerActors = { Infantry = { NorthPortal1, NorthPortal2 }, Vehicles = { NorthSphere1, NorthSphere2 }, Aircraft = { NorthGrav1, NorthGrav2 } }
	},
	West = {
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin),
		AttackValuePerSecond = MainSquadAttackValues,
		FollowLeader = true,
		AttackPaths = WestAttackPaths,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(1)),
		ProducerActors = { Infantry = { WestPortal }, Vehicles = { WestSphere1, WestSphere2 }, Aircraft = { WestGrav } }
	},
	East = {
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin),
		AttackValuePerSecond = MainSquadAttackValues,
		FollowLeader = true,
		AttackPaths = EastAttackPaths,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(1)),
		ProducerActors = { Infantry = { EastPortal }, Vehicles = { EastSphere }, Aircraft = { EastGrav } }
	},
	Roamers = {
		Compositions = AdjustCompositionsForDifficulty({
			{ Infantry = { "s1", "s1", "s1", "s1", "s3", { "mrdr", "s4" }, "stlk" }, Vehicles = { { "lace", "seek" }, { "lace", "seek" } } },
		}),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 10, Max = 20 }),
		FollowLeader = false,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(4)),
	},
	Devastators = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(16)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 6, Max = 14 }),
		Compositions = {
			easy = { { Aircraft = { "deva" } } },
			normal = { { Aircraft = { "deva" } } },
			hard = { { Aircraft = { "deva", "deva" } } },
			vhard = { { Aircraft = { "deva", "deva", "deva" } } },
			brutal = { { Aircraft = { "deva", "deva", "deva", "deva" } } },
		},
		FollowLeader = false,
	},
	Air = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(12)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Scrin,
	},
	AirToAir = AirToAirSquad({ "stmr", "enrv", "torm" }, AdjustAirDelayForDifficulty(DateTime.Minutes(10))),
}

SetupPlayers = function()
	Greece = Player.GetPlayer("Greece")
	MaleficScrin = Player.GetPlayer("MaleficScrin")
	England = Player.GetPlayer("England")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { Greece }
	MissionEnemies = { MaleficScrin }
end

WorldLoaded = function()
	SetupPlayers()

	Camera.Position = PlayerStart.CenterPosition
	NumBasesSecured = 0
	NumScrinBasesActive = 1

	InitObjectives(Greece)
	AdjustPlayerStartingCashForDifficulty()
	RemoveActorsBasedOnDifficultyTags()
	InitMaleficScrin()
	SetupLightning()
	SetupChurchMoneyCrates()

	ObjectiveSecureAllBases = Greece.AddObjective("Secure the four abandoned Allied bases.")
	ObjectiveDestroyScrinBases = Greece.AddObjective("Destroy all Scrin Nerve Centers.")

	Utils.Do(MissionPlayers, function(p)
		Actor.Create("radar.dummy", true, { Owner = p })
	end)

	local nerveCenters = MaleficScrin.GetActorsByType("nerv")
	Trigger.OnAllKilled(nerveCenters, function()
		Greece.MarkCompletedObjective(ObjectiveDestroyScrinBases)
	end)

	Utils.Do(nerveCenters, function(n)
		Trigger.OnKilled(n, function()
			UpdateMissionText()
		end)
	end)

	Trigger.AfterDelay(1, function()
		local englandHarvs = England.GetActorsByType("harv")
		Utils.Do(englandHarvs, function(h)
			h.Stop()
		end)

		Utils.Do({ SEBaseCenter, SWBaseCenter, NEBaseCenter, NWBaseCenter }, function(center)
			local defenders = Map.ActorsInCircle(center.CenterPosition, WDist.New(10 * 1024), function(a)
				return not a.IsDead and a.Owner == MaleficScrin and a.Type ~= "camera"
			end)

			Trigger.OnAllKilled(defenders, function()
				SecureBase(center)
			end)
		end)

		Utils.Do({ MiniBase1, MiniBase2, MiniBase3, McvReveal }, function(loc)
			local miniBaseDefenders = Map.ActorsInCircle(loc.CenterPosition, WDist.New(7 * 1024), function(a)
				return not a.IsDead and a.Owner == MaleficScrin and a.Type ~= "camera"
			end)
			Trigger.OnAllKilled(miniBaseDefenders, function()
				local miniBaseActors = Map.ActorsInCircle(loc.CenterPosition, WDist.New(7 * 1024), function(a)
					return not a.IsDead and (a.Owner == England or a.Type == "macs" or a.Type == "hosp")
				end)
				Utils.Do(miniBaseActors, function(a)
					a.Owner = Greece
				end)
				if IsVeryHardOrAbove() and loc == McvReveal then
					InitNorthScrinBase()
					InitWestScrinBase()
					InitMcvObjective()
					Trigger.AfterDelay(1, function()
						Greece.MarkCompletedObjective(ObjectiveRecoverMcv)
					end)
					Trigger.AfterDelay(DateTime.Seconds(120), function()
						Utils.Do(MissionPlayers, function(p)
							Actor.Create("mcv.allowed", true, { Owner = p })
						end)
						Notification("MCV production now available.")
					end)
					if McvFlare ~= nil and not McvFlare.IsDead then
						McvFlare.Destroy()
					end
				end
			end)
		end)
	end)

	local productionBuildings = MaleficScrin.GetActorsByTypes({ "port", "wsph", "sfac", "grav" })
	for _, b in pairs(productionBuildings) do
		SellOnCaptureAttempt(b)
	end

	if IsHardOrAbove() then
		Trigger.OnAnyKilled(productionBuildings, function()
			Trigger.AfterDelay(DateTime.Minutes(5), function()
				if not RoamersInitialized then
					RoamersInitialized = true
					InitAttackSquad(Squads.Roamers, MaleficScrin)
				end
			end)
		end)
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

		if MissionPlayersHaveNoRequiredUnits() then
			if not Greece.IsObjectiveCompleted(ObjectiveSecureAllBases) then
				Greece.MarkFailedObjective(ObjectiveSecureAllBases)
			end
			if not Greece.IsObjectiveCompleted(ObjectiveDestroyScrinBases) then
				Greece.MarkFailedObjective(ObjectiveDestroyScrinBases)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()

		if #MaleficScrin.GetActorsByType("sfac") == 0 then
			InitVoidEngines()
		end
	end
end

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 750 == 0 then
		CalculatePlayerCharacteristics()
	end
end

UpdateMissionText = function(text)
	local nerveCenterCount = #MaleficScrin.GetActorsByType("nerv")

	if nerveCenterCount > 0 then
		UserInterface.SetMissionText(nerveCenterCount .. " Nerve Centers remaining.", HSLColor.Yellow)
	else
		UserInterface.SetMissionText("")
	end
end

InitMaleficScrin = function()
	RebuildExcludes.MaleficScrin = { Types = { "nerv" } }

	AutoRepairAndRebuildBuildings(MaleficScrin, 10)
	SetupRefAndSilosCaptureCredits(MaleficScrin)
	AutoReplaceHarvesters(MaleficScrin)
	AutoRebuildConyards(MaleficScrin)

	local maleficScrinGroundAttackers = Utils.Where(MaleficScrin.GetGroundAttackers(), function (a) return a.Type ~= "veng" end)
	Utils.Do(maleficScrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)

	Utils.Do({ VoidEngine, VoidEngine2 }, function(v)
		Trigger.OnDamaged(v, function(self, attacker, damage)
			InitVoidEngines()
		end)
	end)
end

InitVoidEngines = function()
	if not VoidEnginesHunting then
		VoidEnginesHunting = true
		MediaCA.PlaySound("veng-spawn.aud", 2)
		Utils.Do({ VoidEngine, VoidEngine2 }, function(ve)
			AssaultPlayerBaseOrHunt(v)
		end)
	end
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

SecureBase = function(baseCenter)
	if baseCenter == SEBaseCenter then
		SecureSouthEastBase()
	elseif baseCenter == SWBaseCenter then
		SecureSouthWestBase()
	elseif baseCenter == NEBaseCenter then
		SecureNorthEastBase()
	elseif baseCenter == NWBaseCenter then
		SecureNorthWestBase()
	end

	NumBasesSecured = NumBasesSecured + 1

	if NumBasesSecured == 1 then
		for _, p in ipairs(MissionPlayers) do
			p.GrantCondition("first-base-secured")
		end
	end

	if NumBasesSecured == 2 then
		Trigger.AfterDelay(DateTime.Seconds(20), function()
			if IsVeryHardOrAbove() then
				InitMcvObjective()
				McvFlare = Actor.Create("flare", true, { Owner = Greece, Location = McvReveal.Location })
				Beacon.New(Greece, McvReveal.CenterPosition)
				Notification("Abandoned MCV located. Press [" .. UtilsCA.Hotkey("ToLastEvent") .. "] to view.")
			else
				PlaySpeechNotificationToMissionPlayers("ReinforcementsArrived")
				Notification("Reinforcements have arrived. Press [" .. UtilsCA.Hotkey("ToLastEvent") .. "] to view location.")
				Reinforcements.Reinforce(Greece, { "lst.mcv" }, { McvSpawn.Location, McvDest.Location }, 75)
				Beacon.New(Greece, McvDest.CenterPosition)
				Utils.Do(MissionPlayers, function(p)
					Actor.Create("mcv.allowed", true, { Owner = p })
				end)
			end
		end)
	end

	if NumBasesSecured >= 4 then
		Greece.MarkCompletedObjective(ObjectiveSecureAllBases)
	end

	local baseActors = Map.ActorsInCircle(baseCenter.CenterPosition, WDist.New(22 * 1024), function(a)
		return not a.IsDead and a.Owner == England
	end)

	Utils.Do(baseActors, function(a)
		a.Owner = Greece
	end)
end

SecureNorthEastBase = function()
	if not NorthEastBaseSecured then
		NorthEastBaseSecured = true
		NEFlare.Destroy()

		table.insert(NorthAttackPaths, { N1a.Location, N2a.Location })
		table.insert(NorthAttackPaths, { N1b.Location, N2b.Location })
		table.insert(NorthAttackPaths, { N2d.Location })

		table.insert(EastAttackPaths, { E1.Location, E2b.Location, E3a.Location })
		table.insert(EastAttackPaths, { E1.Location, E2c.Location, E3b.Location })

		InitNorthScrinBase()
		InitEastScrinBase()
	end
end

SecureNorthWestBase = function()
	if not NorthWestBaseSecured then
		NorthWestBaseSecured = true
		NWFlare.Destroy()

		table.insert(NorthAttackPaths, { N1c.Location, N2c.Location, N3c.Location })

		table.insert(WestAttackPaths, { W1.Location, W2a.Location })
		table.insert(WestAttackPaths, { W1.Location, W2d.Location })

		InitNorthScrinBase()
		InitWestScrinBase()
	end
end

SecureSouthEastBase = function()
	if not SouthEastBaseSecured then
		SouthEastBaseSecured = true
		SEFlare.Destroy()

		table.insert(NorthAttackPaths, { N2d.Location, N3a.Location })
		table.insert(NorthAttackPaths, { N2d.Location, N3b.Location })
		table.insert(NorthAttackPaths, { N1a.Location, N2a.Location, N3d.Location })

		table.insert(EastAttackPaths, { E1.Location, E2a.Location })
		table.insert(EastAttackPaths, { E1.Location, E2b.Location })

		InitEastScrinBase()
		InitNorthScrinBase()
	end
end

SecureSouthWestBase = function()
	if not SouthWestBaseSecured then
		SouthWestBaseSecured = true
		SWFlare.Destroy()

		table.insert(WestAttackPaths, { W1.Location, W2b.Location })
		table.insert(WestAttackPaths, { W1.Location, W2c.Location })

		InitWestScrinBase()
		InitEastScrinBase()
	end
end

InitFirstScrinBase = function()
	if not FirstScrinBaseInitialized then
		FirstScrinBaseInitialized = true
		InitAiUpgrades(MaleficScrin)
		InitAirAttackSquad(Squads.Air, MaleficScrin)
		if IsHardOrAbove() then
			InitAirAttackSquad(Squads.AirToAir, MaleficScrin, MissionPlayers, { "Aircraft" }, "ArmorType")
			InitAttackSquad(Squads.Devastators, MaleficScrin)
			if not RoamersInitialized then
				RoamersInitialized = true
				InitAttackSquad(Squads.Roamers, MaleficScrin)
			end
		end
		Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
			Actor.Create("ai.superweapons.enabled", true, { Owner = MaleficScrin })
			Actor.Create("ai.minor.superweapons.enabled", true, { Owner = MaleficScrin })
		end)
	end
end

InitNorthScrinBase = function()
	if not NorthScrinBaseInitialized then
		InitFirstScrinBase()
		NorthScrinBaseInitialized = true
		NumScrinBasesActive = NumScrinBasesActive + 1
		InitAttackSquad(Squads.North, MaleficScrin)
	end
end

InitWestScrinBase = function()
	if not WestScrinBaseInitialized then
		InitFirstScrinBase()
		WestScrinBaseInitialized = true
		NumScrinBasesActive = NumScrinBasesActive + 1
		InitAttackSquad(Squads.West, MaleficScrin)
	end
end

InitEastScrinBase = function()
	if not EastScrinBaseInitialized then
		InitFirstScrinBase()
		EastScrinBaseInitialized = true
		NumScrinBasesActive = NumScrinBasesActive + 1
		InitAttackSquad(Squads.East, MaleficScrin)
	end
end

InitMcvObjective = function()
	if ObjectiveRecoverMcv == nil then
		ObjectiveRecoverMcv = Greece.AddSecondaryObjective("Recover Allied MCV.")
	end
end
