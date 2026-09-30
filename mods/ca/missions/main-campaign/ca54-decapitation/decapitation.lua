MissionDir = "ca|missions/main-campaign/ca54-decapitation"

SuperweaponsEnabledTime = {
	easy = DateTime.Seconds((60 * 50) + 17),
	normal = DateTime.Seconds((60 * 35) + 17),
	hard = DateTime.Seconds((60 * 25) + 17),
	vhard = DateTime.Seconds((60 * 20) + 17),
	brutal = DateTime.Seconds((60 * 15) + 17)
}

MeteorStartTime = DateTime.Minutes(4)

MeteorInitialInterval = {
	easy = DateTime.Minutes(9),
	normal = DateTime.Minutes(8),
	hard = DateTime.Minutes(7),
	vhard = DateTime.Minutes(6),
	brutal = DateTime.Minutes(5),
}

MeteorIntervalDecrement = {
	easy = DateTime.Seconds(15),
	normal = DateTime.Seconds(15),
	hard = DateTime.Seconds(15),
	vhard = DateTime.Seconds(20),
	brutal = DateTime.Seconds(20),
}

MeteorMinInterval = {
	easy = DateTime.Minutes(6),
	normal = DateTime.Minutes(5),
	hard = DateTime.Minutes(4),
	vhard = DateTime.Minutes(3),
	brutal = DateTime.Minutes(2),
}

AirFleetKillersThreshold = {
	normal = 6,
	hard = 4,
	vhard = 3,
	brutal = 2
}

MaxFleetKillers = {
	normal = 6,
	hard = 8,
	vhard = 12,
	brutal = 16
}

ScrinNorthAttackPaths = {
	{ ScrinWaypoint1.Location, ScrinWaypoint3.Location, ScrinWaypoint5.Location, ScrinWaypoint8.Location },
	{ ScrinWaypoint2.Location, ScrinWaypoint4.Location, ScrinWaypoint6.Location, ScrinWaypoint9.Location },
	{ ScrinWaypoint2.Location, ScrinWaypoint4.Location, ScrinWaypoint7.Location, ScrinWaypoint11.Location },
}

ScrinWestAttackPaths = {
	{ ScrinWaypoint3.Location, ScrinWaypoint5.Location, ScrinWaypoint8.Location },
	{ ScrinWaypoint3.Location, ScrinWaypoint6.Location, ScrinWaypoint9.Location },
	{ ScrinWaypoint3.Location, ScrinWaypoint6.Location, ScrinWaypoint10.Location },
}

ScrinCentralAttackPaths = {
	{ ScrinWaypoint6.Location, ScrinWaypoint9.Location },
	{ ScrinWaypoint7.Location, ScrinWaypoint10.Location },
	{ ScrinWaypoint5.Location, ScrinWaypoint8.Location },
}

ScrinEastAttackPaths = {
	{ ScrinWaypoint11.Location },
	{ ScrinWaypoint7.Location, ScrinWaypoint9.Location },
	{ ScrinWaypoint7.Location, ScrinWaypoint10.Location },
}

Spires = {
	Spire2, Spire1, Spire4, Spire3, Spire5
}

NextSpireSummonAvailable = 0

RecalculateBetaSquad = function(squad)
	local activeBases = {}

	local centralFactories = Map.ActorsInBox(CentralProdTopLeft.CenterPosition, CentralProdBottomRight.CenterPosition, function(a)
		return a.Owner == Scrin and a.Type == "wsph"
	end)

	local centralPortals = Map.ActorsInBox(CentralProdTopLeft.CenterPosition, CentralProdBottomRight.CenterPosition, function(a)
		return a.Owner == Scrin and a.Type == "port"
	end)

	local centralGravs = Map.ActorsInBox(CentralProdTopLeft.CenterPosition, CentralProdBottomRight.CenterPosition, function(a)
		return a.Owner == Scrin and a.Type == "grav"
	end)

	local eastFactories = Map.ActorsInBox(EastProdTopLeft.CenterPosition, EastProdBottomRight.CenterPosition, function(a)
		return a.Owner == Scrin and a.Type == "wsph"
	end)

	local eastPortals = Map.ActorsInBox(EastProdTopLeft.CenterPosition, EastProdBottomRight.CenterPosition, function(a)
		return a.Owner == Scrin and a.Type == "port"
	end)

	local eastGravs = Map.ActorsInBox(EastProdTopLeft.CenterPosition, EastProdBottomRight.CenterPosition, function(a)
		return a.Owner == Scrin and a.Type == "grav"
	end)

	local westFactories = Map.ActorsInBox(WestProdTopLeft.CenterPosition, WestProdBottomRight.CenterPosition, function(a)
		return a.Owner == Scrin and a.Type == "wsph"
	end)

	local westPortals = Map.ActorsInBox(WestProdTopLeft.CenterPosition, WestProdBottomRight.CenterPosition, function(a)
		return a.Owner == Scrin and a.Type == "port"
	end)

	local westGravs = Map.ActorsInBox(WestProdTopLeft.CenterPosition, WestProdBottomRight.CenterPosition, function(a)
		return a.Owner == Scrin and a.Type == "grav"
	end)

	if #centralFactories == 1 and #centralPortals == 1 and #centralGravs == 1 then
		table.insert(activeBases, "Central")
	end
	if #eastFactories == 1 and #eastPortals == 1 and #eastGravs == 1 then
		table.insert(activeBases, "East")
	end
	if #westFactories == 1 and #westPortals == 1 and #westGravs == 1 then
		table.insert(activeBases, "West")
	end

	if #activeBases > 0 then
		local selectedBase = Utils.Random(activeBases)

		if selectedBase == "Central" then
			squad.ProducerActors = { Infantry = { centralPortals[1] }, Vehicles = { centralFactories[1] }, Aircraft = { centralGravs[1] } }
			squad.AttackPaths = ScrinCentralAttackPaths
		elseif selectedBase == "East" then
			squad.ProducerActors = { Infantry = { eastPortals[1] }, Vehicles = { eastFactories[1] }, Aircraft = { eastGravs[1] } }
			squad.AttackPaths = ScrinEastAttackPaths
		elseif selectedBase == "West" then
			squad.ProducerActors = { Infantry = { westPortals[1] }, Vehicles = { westFactories[1] }, Aircraft = { westGravs[1] } }
			squad.AttackPaths = ScrinWestAttackPaths
		end
	else
		squad.ProducerActors = nil
		squad.AttackPaths = nil
	end
end

if IsHardOrAbove() then
	table.insert(UnitCompositions.Scrin, {
		Infantry = { "s3", "s4", "evis", "evis", "evis", "evis", "s1", "s1", "s4", "s1", "s4", "s1", "s4", "s1", "mast" },
		Vehicles = { "shrw", TripodVariant, TripodVariant, "shrw", CorrupterOrDevourer, "oblt", "shrw" },
		Aircraft = { PacOrDevastator, "pac" },
		MinTime = DateTime.Minutes(22)
	})

	if IsVeryHardOrAbove() then
		table.insert(UnitCompositions.Scrin, {
			Infantry = { "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis", "evis" },
			Vehicles = { "shrw", "shrw", "ruin", "ruin", "ruin", "ruin", "rtpd", "rtpd" },
			Aircraft = { "deva" },
			MinTime = DateTime.Minutes(18),
			RequiredTargetCharacteristics = { "MassInfantry" }
		})

		table.insert(UnitCompositions.Scrin, {
			Infantry = { "s2", "s2", "s2", "s2", "s2", "s2", "s2", "s2", "evis", "evis", "s2", "s2", "s2", "s2" },
			Vehicles = { "shrw", "shrw", "shrw", "shrw", "shrw", "shrw", "shrw", "shrw", "shrw", "shrw" },
			MinTime = DateTime.Minutes(16),
			RequiredTargetCharacteristics = { "MassAir" },
			IsSpecial = true
		})
	end
end

AdjustedScrinCompositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin)

Squads = {
	ScrinAlpha = {
		InitTimeAdjustment = -DateTime.Minutes(5),
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(2)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		FollowLeader = true,
		Compositions = AdjustedScrinCompositions,
		AttackPaths = ScrinNorthAttackPaths,
		ProducerActors = { Infantry = { NorthPortal1, NorthPortal2 }, Vehicles = { NorthSphere1, NorthSphere2 }, Aircraft = { NorthGrav } }
	},
	ScrinBeta = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(1)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		FollowLeader = true,
		Compositions = AdjustedScrinCompositions,
		AttackPaths = ScrinCentralAttackPaths,
		AfterSendSquad = RecalculateBetaSquad,
		ProducerActors = { Infantry = { CentralPortal }, Vehicles = { CentralSphere }, Aircraft = { CentralGrav } }
	},
	ScrinAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(8)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Scrin
	},
	ScrinAirToAir = AirToAirSquad(
		{ "stmr", "enrv", "torm" },
		AdjustAirDelayForDifficulty(DateTime.Minutes(8)),
		function(a)
			a.Patrol({ A2APatrol1.Location, A2APatrol2.Location, A2APatrol3.Location, A2APatrol4.Location, A2APatrol5.Location, A2APatrol6.Location })
		end
	),
	ScrinFleetKillers = {
		ActiveCondition = function(squad)
			local scrinFleet = GetMissionPlayersActorsByTypes({ "pac", "deva" })
			return #scrinFleet > AirFleetKillersThreshold[Difficulty]
		end,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 30, Max = 30 }),
		Compositions = function(squad)
			local tormentors = { "torm" }
			local numFleetShips = #GetMissionPlayersActorsByTypes({ "pac", "deva" })
			for i = 1, math.min(numFleetShips * 2, MaxFleetKillers[Difficulty]) do
				table.insert(tormentors, "torm")
			end
			return { { Aircraft = tormentors } }
		end
	},
	ScrinCommandoKillers = {
		ActiveCondition = function(squad)
			local commandos = GetMissionPlayersActorsByTypes({ "mast", "rmbo" })
			return #commandos > 0
		end,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 10, Max = 20 }),
		Compositions = { { Aircraft = { "stmr", "stmr" } } }
	},
	Nod1 = {
		Delay = DateTime.Minutes(2),
		AttackValuePerSecond = { Min = 10, Max = 20 },
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		Compositions = UnitCompositions.Nod,
		AttackPaths = { { Nod1Waypoint1.Location, Nod1Waypoint2.Location } },
	},
	Nod2 = {
		Delay = DateTime.Minutes(2),
		AttackValuePerSecond = { Min = 5, Max = 10 },
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		Compositions = UnitCompositions.Nod,
		AttackPaths = { { Nod2Waypoint1.Location, Nod2Waypoint2.Location } },
	},
}

SetupPlayers = function()
	ScrinRebels = Player.GetPlayer("ScrinRebels")
	Scrin = Player.GetPlayer("Scrin")
    Nod1 = Player.GetPlayer("Nod1")
	Nod2 = Player.GetPlayer("Nod2")
	Nod3 = Player.GetPlayer("Nod3")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { ScrinRebels }
	MissionEnemies = { Scrin }

	Actor.Create("rebel.allegiance", true, { Owner = ScrinRebels })
end

WorldLoaded = function()
	SetupPlayers()

    Camera.Position = PlayerStart.CenterPosition

	InitObjectives(ScrinRebels)
	AdjustPlayerStartingCashForDifficulty()
	RemoveActorsBasedOnDifficultyTags()
	InitScrin()
	InitNod()
	SetupLightning()

    Trigger.OnKilled(Vanquisher, function(self, killer)
        if ObjectiveDestroyVanquisher ~= nil and not ScrinRebels.IsObjectiveCompleted(ObjectiveDestroyVanquisher) then
			Trigger.AfterDelay(DateTime.Seconds(3), function()
            	ScrinRebels.MarkCompletedObjective(ObjectiveDestroyVanquisher)
			end)
        end
    end)

	ObjectiveDestroySpires = ScrinRebels.AddObjective("Destroy all Overlord spires.")

	Utils.Do(Spires, function(spire)
		Trigger.OnKilled(spire, function(self, killer)
			NextMeteorInterval = NextMeteorInterval + DateTime.Seconds(60)
		end)
	end)

	Trigger.OnAllKilled(Spires, function(self)
		if not ScrinRebels.IsObjectiveCompleted(ObjectiveDestroySpires) then
			ScrinRebels.MarkCompletedObjective(ObjectiveDestroySpires)
			Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(2)), function()
				Media.DisplayMessage("My will is eternal! As will be your suffering!", "Scrin Overlord", HSLColor.FromHex("7700FF"))
				MediaCA.PlaySound(MissionDir .. "/ovld_eternal.aud", 2)
			end)
		end
	end)

	Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(4)), function()
		Media.DisplayMessage("The total destruction of your treacherous kind is at hand! The rebellion will burn!", "Scrin Overlord", HSLColor.FromHex("7700FF"))
		MediaCA.PlaySound(MissionDir .. "/ovld_totaldestruction.aud", 2)
	end)

	if IsVeryHardOrAbove() then
		local scrinProductionBuildings = Scrin.GetActorsByTypes({ "port", "wsph", "sfac", "grav" })
		for _, b in pairs(scrinProductionBuildings) do
			BuildDefenseOnCaptureAttempt(b, "ptur", true)
		end
	end

	if IsVeryHardOrAbove() then
		SWTibTree1.Destroy()

		if Difficulty == "brutal" then
			STibTree.Destroy()
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
		Scrin.Resources = Scrin.ResourceCapacity - 500
		Nod1.Resources = Nod1.ResourceCapacity - 500
		Nod2.Resources = Nod2.ResourceCapacity - 500
		Nod3.Resources = Nod3.ResourceCapacity - 500

		if MissionPlayersHaveNoRequiredUnits() then
			if not ScrinRebels.IsObjectiveCompleted(ObjectiveDestroySpires) then
				ScrinRebels.MarkFailedObjective(ObjectiveDestroySpires)
			end
			if ObjectiveDestroyVanquisher ~= nil and not ScrinRebels.IsObjectiveCompleted(ObjectiveDestroyVanquisher) then
				ScrinRebels.MarkFailedObjective(ObjectiveDestroyVanquisher)
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

InitScrin = function()
	RebuildExcludes.Scrin = { Types = { "ospi" } }

	AutoRepairAndRebuildBuildings(Scrin)
	SetupRefAndSilosCaptureCredits(Scrin)
	AutoReplaceHarvesters(Scrin)
	AutoRebuildConyards(Scrin)
	InitAiUpgrades(Scrin)
	SetupUnitDefenders(Scrin)

	InitAttackSquad(Squads.ScrinAlpha, Scrin)
	InitAttackSquad(Squads.ScrinBeta, Scrin)
	InitAirAttackSquad(Squads.ScrinAir, Scrin)

	if Difficulty ~= "easy" then
		InitAirAttackSquad(Squads.ScrinFleetKillers, Scrin, MissionPlayers, { "pac", "deva" })
	end

	if IsHardOrAbove() then
		InitAirAttackSquad(Squads.ScrinAirToAir, Scrin, MissionPlayers, { "Aircraft" }, "ArmorType")
	end

	if IsVeryHardOrAbove() then
		InitAirAttackSquad(Squads.ScrinCommandoKillers, Scrin, MissionPlayers, { "mast", "rmbo" })
	end

	TargetSwapChance(Vanquisher, 2)

	Trigger.OnDamaged(Vanquisher, function(self, attacker, damage)
		if IsMissionPlayer(attacker.Owner) then
			if Vanquisher.ShieldStrengthPercent == 0 then
				RecallVanquisher()
			end
		end
	end)

	Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = Scrin })
		Actor.Create("ai.superweapons.enabled", true, { Owner = Scrin })
	end)

	Utils.Do(Spires, function(spire, attacker, damage)
		Trigger.OnDamaged(spire, function(self, attacker, damage)
			if NextSpireSummonAvailable < DateTime.GameTime and IsMissionPlayer(attacker.Owner) then
				local maxHp = self.MaxHealth
				local currentHp = self.Health
				local healthPercentage = (currentHp / maxHp) * 100
				if healthPercentage < 50 then
					local destinationLoc = CPos.New(spire.Location.X + 1, spire.Location.Y)
					TeleportVanquisher(destinationLoc)
					NextSpireSummonAvailable = DateTime.GameTime + DateTime.Seconds(120)
					Vanquisher.Hunt()
					MediaCA.PlaySound(MissionDir .. "/vanquisher.aud", 1)

					Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(3)), function()
						if not FirstTauntUsed then
							FirstTauntUsed = true
							Media.DisplayMessage("Traitors, meet your end!", "Scrin Overlord", HSLColor.FromHex("7700FF"))
							MediaCA.PlaySound(MissionDir .. "/ovld_traitors.aud", 2)

							Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(5)), function()
								ObjectiveDestroyVanquisher = ScrinRebels.AddObjective("Destroy the Overlord's flagship.")
								Notification("The Overlord is protecting the spires with his flagship, the Vanquisher. In turn, the spires are acting as shield batteries and recall nodes for the battleship. Destroy all the spires, then you will be able to eliminate the Vanquisher.")
								MediaCA.PlaySound(MissionDir .. "/s_vanquisher.aud", 2)
							end)

						elseif not SecondTauntUsed then
							SecondTauntUsed = true
							Media.DisplayMessage("Feel my wrath!", "Scrin Overlord", HSLColor.FromHex("7700FF"))
							MediaCA.PlaySound(MissionDir .. "/ovld_wrath.aud", 2)

						elseif not ThirdTauntUsed then
							ThirdTauntUsed = true
							Media.DisplayMessage("Earth will fall!", "Scrin Overlord", HSLColor.FromHex("7700FF"))
							MediaCA.PlaySound(MissionDir .. "/ovld_earth.aud", 2)
						end
					end)
				end
			end
		end)
	end)

	Actor.Create("loyalist.allegiance", true, { Owner = Scrin })

	Trigger.AfterDelay(MeteorStartTime, function()
		Actor.Create("owrath.provider", true, { Owner = Scrin })

		Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(8)), function()
			Notification("The Overlord's spires are pulling down Tiberium meteors with increasing frequency. We must destroy the spires before we are overwhelmed.")
			MediaCA.PlaySound(MissionDir .. "/s_meteors.aud", 2)
		end)

		NextMeteorInterval = MeteorInitialInterval[Difficulty]
		QueueNextMeteor()
	end)
end

InitNod = function()
	Utils.Do({ InitialBike1, InitialBike2, InitialBike3, InitialBike4 }, function(bike)
		bike.Hunt()
	end)

	Utils.Do({ Nod1, Nod2 }, function(p)
		AutoRepairAndRebuildBuildings(p)
		SetupRefAndSilosCaptureCredits(p)
		AutoReplaceHarvesters(p)
		AutoRebuildConyards(p)
		InitAiUpgrades(p)
		SetupUnitDefenders(p, nil, nil, function(p) return p == Scrin end)
	end)

	InitAttackSquad(Squads.Nod1, Nod1, Scrin)
	InitAttackSquad(Squads.Nod2, Nod2, Scrin)
end

RecallVanquisher = function()
	for _, spire in ipairs(Spires) do
		if not spire.IsDead then
			TeleportVanquisher(spire.Location)
			Vanquisher.FullyRestoreShields()
			return
		end
	end

	-- if no spires left
	Vanquisher.Hunt()
end

TeleportVanquisher = function(destinationLoc)
	if not Vanquisher.IsDead then
		local effect = Actor.Create("recall.effect", true, { Owner = Scrin, Location = Vanquisher.Location })
		local effect = Actor.Create("recall.effect", true, { Owner = Scrin, Location = destinationLoc })
		Vanquisher.Stop()
		Vanquisher.Teleport(destinationLoc)
	end
end

QueueNextMeteor = function()
	Trigger.AfterDelay(NextMeteorInterval, function()
		if ObjectiveDestroySpires ~= nil and not ScrinRebels.IsObjectiveCompleted(ObjectiveDestroySpires) then
			Actor.Create("owrath.provider", true, { Owner = Scrin })
			NextMeteorInterval = math.max(NextMeteorInterval - MeteorIntervalDecrement[Difficulty], MeteorMinInterval[Difficulty])
			QueueNextMeteor()
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
