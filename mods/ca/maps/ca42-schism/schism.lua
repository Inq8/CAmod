PurificationInterval = DateTime.Minutes(3) + DateTime.Seconds(5)

DefendDuration = {
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(3) + DateTime.Seconds(30),
	hard = DateTime.Minutes(4),
	vhard = DateTime.Minutes(4),
	brutal = DateTime.Minutes(4),
}

MaleficSpawns = { MaleficSpawn1.Location, MaleficSpawn2.Location, MaleficSpawn3.Location, MaleficSpawn4.Location, MaleficSpawn5.Location, MaleficSpawn6.Location, MaleficSpawn7.Location }
OverlordSpawns = { OverlordSpawn1.Location, OverlordSpawn2.Location, OverlordSpawn3.Location }

if IsHardOrAbove() then
	table.insert(UnitCompositions.Nod, {
		Infantry = {},
		Vehicles = { "avtr", "avtr", "avtr", "avtr", "avtr", "avtr", "avtr" },
		MinTime = DateTime.Minutes(6),
		IsSpecial = true
	})

	table.insert(UnitCompositions.Scrin, {
		Infantry = { "s3", "s1", "mast", "s1", "s1", "s1", "s1", "s1", "s1", "s3", "s4", "s1", "s1", "s1", "s1", "s3", "s1", "s1", "s1", "s1", "s1", "s1", "s3", "s1", "s1", "s1", "s4", "s1", "s1", "s3", "s1", "s1", "s1", "s3", "s1", "s1", "s1", "s1", "s1", "s1" },
		Vehicles = { "intl.ai2", "gunw", "intl.ai2", "gunw", "intl.ai2", "gunw", "intl.ai2", "gunw", "intl.ai2", "gunw" },
		MinTime = DateTime.Minutes(14),
		IsSpecial = true
	})
end

Squads = {
	Nod = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(3)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 30, Max = 40 }),
		ActiveCondition = function()
			return not MaleficArrived
		end,
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Nod),
		AttackPaths = { { NodRally1.Location, NodRally2.Location } },
	},
	ScrinRebels = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(3)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 30, Max = 40 }),
		ActiveCondition = function()
			return not MaleficArrived
		end,
		FollowLeader = true,
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin),
		AttackPaths = { { RebelRally1.Location, RebelRally2.Location } },
	},
	ScrinRebelsAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(7)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 5, Max = 5 }),
		Compositions = AirCompositions.Scrin,
	},
	NodAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(11)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 5, Max = 5 }),
		Compositions = AirCompositions.Nod,
	},
	Banshees = {
		ActiveCondition = function()
			return not Exterminator.IsDead
		end,
		OnProducedAction = function(unit)
			unit.Patrol({ BansheePatrol1.Location, BansheePatrol2.Location, BansheePatrol3.Location }, true)
		end,
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(5)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 16, Max = 16 }),
		Compositions = {
			normal = {
				{ Aircraft = { "scrn", "scrn", "scrn" } },
			},
			hard = {
				{ Aircraft = { "scrn", "scrn", "scrn", "scrn", "scrn", "scrn" } },
			},
			vhard = {
				{ Aircraft = { "scrn", "scrn", "scrn", "scrn", "scrn", "scrn", "scrn" } },
			},
			brutal = {
				{ Aircraft = { "scrn", "scrn", "scrn", "scrn", "scrn", "scrn", "scrn", "scrn" } },
			}
		},
	},
	Enervators = {
		ActiveCondition = function()
			return not Exterminator.IsDead
		end,
		OnProducedAction = function(unit)
			unit.Patrol({ EnervatorPatrol1.Location, EnervatorPatrol2.Location, EnervatorPatrol3.Location, EnervatorPatrol4.Location }, true)
		end,
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(5)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 16, Max = 16 }),
		Compositions = {
			hard = {
				{ Aircraft = { "enrv", "enrv", "enrv", "enrv", "enrv" } },
			},
			vhard = {
				{ Aircraft = { "enrv", "enrv", "enrv", "enrv", "enrv", "enrv" } },
			},
			brutal = {
				{ Aircraft = { "enrv", "enrv", "enrv", "enrv", "enrv", "enrv", "enrv" } },
			}
		},
	},
	ScrinRebelsAirToAir = AirToAirSquad({ "stmr", "enrv", "torm" }, AdjustAirDelayForDifficulty(DateTime.Minutes(10))),
}

-- Setup and Tick

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Scrin = Player.GetPlayer("Scrin")
	Nod = Player.GetPlayer("Nod")
	ScrinRebels = Player.GetPlayer("ScrinRebels")
    ScrinRebelsOuter = Player.GetPlayer("ScrinRebelsOuter")
	MaleficScrin = Player.GetPlayer("MaleficScrin")
	Neutral = Player.GetPlayer("Neutral")
	SpyPlaneProvider = Player.GetPlayer("SpyPlaneProvider")
	MissionPlayers = { USSR }
	MissionEnemies = { Nod, ScrinRebels, MaleficScrin }
	TimerTicks = PurificationInterval

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(USSR)
	AdjustPlayerStartingCashForDifficulty()
	InitScrinRebels()
	InitNod()

	Actor.Create("hazmatsoviet.upgrade", true, { Owner = USSR })

	ObjectiveSecurePurifier = USSR.AddObjective("Use the Exterminator Tripod to secure\nthe purification device.")
	UpdateMissionText()

	local spyPlaneDummy1 = Actor.Create("spy.plane.dummy", true, { Owner = SpyPlaneProvider })

	Trigger.OnKilled(Purifier, function(self, killer)
		if not USSR.IsObjectiveCompleted(ObjectiveSecurePurifier) then
			USSR.MarkFailedObjective(ObjectiveSecurePurifier)
		end
		if ObjectiveDefendPurifier ~= nil and not USSR.IsObjectiveCompleted(ObjectiveDefendPurifier) then
			USSR.MarkFailedObjective(ObjectiveDefendPurifier)
		end
	end)

	Trigger.OnKilled(Exterminator, function(self, killer)
		if not USSR.IsObjectiveCompleted(ObjectiveSecurePurifier) then
			USSR.MarkFailedObjective(ObjectiveSecurePurifier)
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		Exterminator.Owner = USSR
		Exterminator.GrantCondition("difficulty-" .. Difficulty)
	end)

	Trigger.AfterDelay(DateTime.Seconds(4), function()
		Media.DisplayMessage("Stop this madness. You have no idea what you are dealing with. You will be the end of us all!", "Kane", HSLColor.FromHex("FF0000"))
		MediaCA.PlaySound("kane_stopmadness.aud", 2)
		Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(7)), function()
			Media.DisplayMessage("Your foolish quest ends here Kane. The Overlord will have your head.", "Premier Cherdenko", HSLColor.FromHex("FF0000"))
			MediaCA.PlaySound("cdko_quest.aud", 2)
			Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(7)), function()
				spyPlaneDummy1.TargetAirstrike(Purifier.CenterPosition, Angle.NorthEast)
				spyPlaneDummy1.Destroy()

				Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
				Notification("Reinforcements have arrived.")
				Reinforcements.Reinforce(USSR, { "kiro" }, { KirovSpawn1.Location, KirovRally1.Location })
				Reinforcements.Reinforce(USSR, { "kiro" }, { KirovSpawn2.Location, KirovRally2.Location })

				Utils.Do({ SSMNorth, SSMEast1, SSMEast2 }, function(s)
					if not s.IsDead then
						s.Hunt()
					end
				end)

				Trigger.AfterDelay(DateTime.Seconds(5), function()
					Tip("The Iron Curtain must be intact and powered to shield the Exterminator Tripod from purification waves.")
				end)
			end)
		end)
	end)

	AfterWorldLoaded()
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	OncePerThirtySecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		ScrinRebels.Resources = ScrinRebels.ResourceCapacity - 500
		ScrinRebelsOuter.Resources = ScrinRebelsOuter.ResourceCapacity - 500

		if not USSR.IsObjectiveCompleted(ObjectiveSecurePurifier) then
			if TimerTicks > 0 then
				if TimerTicks > 25 then
					TimerTicks = TimerTicks - 25
					if TimerTicks == 125 then
						Media.PlaySound("buzzy1.aud")
					end
					if TimerTicks == 75 then
						ApplyIronCurtain()
					end
				else
					TimerTicks = 0
					PurificationWave()
				end
			else
				TimerTicks = PurificationInterval
			end

			if IsMissionPlayer(Purifier.Owner) then
				TimerTicks = DefendDuration[Difficulty]

				if ObjectiveDefendPurifier == nil then
					ObjectiveDefendPurifier = USSR.AddObjective("Defend the purification device.")
				end

				USSR.MarkCompletedObjective(ObjectiveSecurePurifier)

				Trigger.AfterDelay(DateTime.Seconds(20), function()
					MaleficInit()
				end)
			end
		else
			if TimerTicks > 0 then
				if TimerTicks > 25 then
					TimerTicks = TimerTicks - 25
				else
					TimerTicks = 0
					if not USSR.IsObjectiveCompleted(ObjectiveDefendPurifier) then
						USSR.MarkCompletedObjective(ObjectiveDefendPurifier)
					end
				end
			end
		end

		UpdateMissionText()
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

-- Functions

UpdateMissionText = function()

	if USSR.IsObjectiveCompleted(ObjectiveSecurePurifier) then
		UserInterface.SetMissionText("Purifier teleportation in " .. UtilsCA.FormatTimeForGameSpeed(TimerTicks), HSLColor.Yellow)
	else
		if not Purifier.IsDead and Purifier.Owner == ScrinRebels then
			local color = HSLColor.Yellow
			if TimerTicks <= 125 then
				color = HSLColor.Red
			end
			UserInterface.SetMissionText("Purification wave in " .. UtilsCA.FormatTimeForGameSpeed(TimerTicks), color)
		else
			UserInterface.SetMissionText("")
		end
	end
end

InitScrinRebels = function()
	AutoRepairAndRebuildBuildings(ScrinRebels)
	AutoRepairBuildings(ScrinRebelsOuter)
	SetupRefAndSilosCaptureCredits(ScrinRebels)
	AutoReplaceHarvesters(ScrinRebels)
	AutoRebuildConyards(ScrinRebels)
	InitAiUpgrades(ScrinRebels)
	InitAttackSquad(Squads.ScrinRebels, ScrinRebels)
	InitAirAttackSquad(Squads.ScrinRebelsAir, ScrinRebels)

	if IsHardOrAbove() then
		InitAirAttackSquad(Squads.Enervators, ScrinRebels, MissionPlayers, { "etpd" })
		InitAirAttackSquad(Squads.ScrinRebelsAirToAir, Scrin, MissionPlayers, { "Aircraft" }, "ArmorType")
	end

	local scrinRebelsGroundAttackers = ScrinRebels.GetGroundAttackers()

	Utils.Do(scrinRebelsGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)
end

InitNod = function()
	AutoRepairAndRebuildBuildings(Nod)
	SetupRefAndSilosCaptureCredits(Nod)
	AutoReplaceHarvesters(Nod)
	AutoRebuildConyards(Nod)
	InitAiUpgrades(Nod)
	InitAttackSquad(Squads.Nod, Nod)
	InitAirAttackSquad(Squads.NodAir, Nod)

	if IsNormalOrAbove() then
		InitAirAttackSquad(Squads.Banshees, Nod, MissionPlayers, { "etpd" })
	end

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)
end

PurificationWave = function()
	Lighting.Flash("Purification", AdjustTimeForGameSpeed(10))
	MediaCA.PlaySound("purificationsm.aud", 2)

	local exterminators = USSR.GetActorsByType("etpd")
	if #exterminators > 0 then
		local exterminator = exterminators[1]
		local dummy = Actor.Create("purification.dummy", true, { Owner = ScrinRebels, Location = exterminator.Location })

		Trigger.AfterDelay(1, function()
			if exterminator.Owner == ScrinRebels and not USSR.IsObjectiveCompleted(ObjectiveSecurePurifier) then
				USSR.MarkFailedObjective(ObjectiveSecurePurifier)
			end
		end)

		Trigger.AfterDelay(DateTime.Seconds(5), function()
			dummy.Destroy()
		end)
	end
end

MaleficInit = function()
	if not MaleficArrived then
		MaleficArrived = true
		Lighting.Flash("Purification", AdjustTimeForGameSpeed(10))
		Lighting.Ambient = 0.4
		Lighting.Red = 0.9
		Lighting.Blue = 1.1
		Lighting.Green = 0.8

		Utils.Do(MaleficSpawns, function(loc)
			Actor.Create("wormhole", true, { Owner = MaleficScrin, Location = loc})
		end)

		MediaCA.PlaySound("malefic.aud", 2)
		Trigger.AfterDelay(DateTime.Seconds(8), function()
			Media.DisplayMessage("Impossible! These Scrin are not..  Do not allow the device to be destroyed!", "Scrin Overlord", HSLColor.FromHex("7700FF"))
			MediaCA.PlaySound("ovld_impossible.aud", 2)

			Trigger.AfterDelay(DateTime.Seconds(8), function()
				Utils.Do(OverlordSpawns, function(loc)
					Actor.Create("wormhole", true, { Owner = Scrin, Location = loc})
				end)
				OverlordSpawn(true)
			end)
		end)

		MaleficSpawn(true)
	end
end

MaleficSpawn = function(isInitial)
	local invasionCompositions = {
		{ "intl", "s1", "s1", "s1", "s1", "s3", "s3", "s4", "s4", "stlk", "stlk", "stlk" },
		{ "dark", "s1", "s1", "s1", "s1", "s3", "s4", "stlk", "stlk", "stlk" },
		{ "tpod", "s1", "s1", "s1", "s3", "s3", "s4", "stlk", "stlk" },
		{ "dark", "s1", "s1", "s1", "s3", "s3", "s4", "stlk", "stlk", "stlk" },
	}

	Utils.Do(MaleficSpawns, function(s)
		local units = Reinforcements.Reinforce(MaleficScrin, Utils.Shuffle(Utils.Random(invasionCompositions)), { s }, 1)
		Utils.Do(units, function(unit)
			unit.Scatter()
			Trigger.AfterDelay(5, function()
				if not unit.IsDead then
					if not Purifier.IsDead then
						unit.AttackMove(Purifier.Location)
					end
					unit.Hunt()
				end
			end)
		end)
	end)

	local prepTime = 0
	if isInitial then
		prepTime = DateTime.Seconds(20)
	end

	Trigger.AfterDelay(GetInvasionInterval() + prepTime, MaleficSpawn)
end

OverlordSpawn = function(isInitial)
	local overlordCompositions = {
		{ "devo", "s1", "s1", "s1", "s3", "s4", "evis", "evis", "evis" },
		{ "tpod", "s1", "s1", "s1", "s3", "evis", "evis" },
		{ "ruin","s1", "s1", "s1", "s1", "s3", "s4", "evis", "evis" },
	}

	Utils.Do(OverlordSpawns, function(s)
		local units = Reinforcements.Reinforce(Scrin, Utils.Shuffle(Utils.Random(overlordCompositions)), { s }, 1)
		Utils.Do(units, function(unit)
			unit.Scatter()
			Trigger.AfterDelay(5, function()
				if not unit.IsDead then
					unit.Hunt()
				end
			end)
		end)
	end)

	local prepTime = 0
	if isInitial then
		prepTime = DateTime.Seconds(20)
	end

	Trigger.AfterDelay(DateTime.Seconds(30) + prepTime, OverlordSpawn)
end

GetPlayerArmyValue = function()
	local value = 0
	Utils.Do(USSR.GetActors(), function(a)
		if a.HasProperty("Attack") then
			if UnitCosts[a.Type] == nil then
				UnitCosts[a.Type] = ActorCA.CostOrDefault(a.Type)
			end
			value = value + UnitCosts[a.Type]
		end
	end)
	return value
end

GetInvasionInterval = function()
	local armyValue = GetPlayerArmyValue()

	if Difficulty == "easy" then
		if armyValue >= 10000 then
			return DateTime.Seconds(30)
		else
			return DateTime.Seconds(40)
		end
	else
		if armyValue >= 48000 then
			return DateTime.Seconds(15)
		elseif armyValue >= 38000 then
			return DateTime.Seconds(18)
		elseif armyValue >= 28000 then
			return DateTime.Seconds(21)
		elseif armyValue >= 18000 then
			return DateTime.Seconds(24)
		elseif armyValue >= 10000 then
			return DateTime.Seconds(27)
		else
			return DateTime.Seconds(30)
		end
	end
end

ApplyIronCurtain = function()
	if USSR.PowerState ~= "Normal" then
		return
	end
	local ics = USSR.GetActorsByType("iron")
	if #ics > 0 then
		local ic = ics[1]
		Media.PlaySound("ironcur9.aud")
		Exterminator.GrantCondition("invulnerability", DateTime.Seconds(8))
	end
end
