PurificationInterval = {
	easy = DateTime.Minutes(3) + DateTime.Seconds(5),
	normal = DateTime.Minutes(3) + DateTime.Seconds(5),
	hard = DateTime.Minutes(3) + DateTime.Seconds(5),
}

DefendDuration = {
	easy = DateTime.Minutes(2),
	normal = DateTime.Minutes(3),
	hard = DateTime.Minutes(4),
}

MaleficSpawns = { MaleficSpawn1.Location, MaleficSpawn2.Location, MaleficSpawn3.Location, MaleficSpawn4.Location, MaleficSpawn5.Location, MaleficSpawn6.Location, MaleficSpawn7.Location }

if Difficulty == "hard" then
	table.insert(UnitCompositions.Nod, {
		Infantry = {},
		Vehicles = { "avtr", "avtr", "avtr", "avtr", "avtr", "avtr" },
		MinTime = DateTime.Minutes(5),
		IsSpecial = true
	})
end

Squads = {
	Nod = {
		Delay = {
			easy = DateTime.Minutes(5),
			normal = DateTime.Minutes(3),
			hard = DateTime.Minutes(1)
		},
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 25 },
			normal = { Min = 30, Max = 50 },
			hard = { Min = 50, Max = 80 },
		},
		ActiveCondition = function()
			return not MaleficArrived
		end,
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = AdjustCompositionsForDifficulty(UnitCompositions.Nod),
		AttackPaths = { { NodRally1.Location, NodRally2.Location } },
	},
	ScrinRebels = {
		Delay = {
			easy = DateTime.Minutes(5),
			normal = DateTime.Minutes(3),
			hard = DateTime.Minutes(1)
		},
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 25 },
			normal = { Min = 30, Max = 50 },
			hard = { Min = 50, Max = 80 },
		},
		ActiveCondition = function()
			return not MaleficArrived
		end,
		FollowLeader = true,
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = AdjustCompositionsForDifficulty(UnitCompositions.Scrin),
		AttackPaths = { { RebelRally1.Location, RebelRally2.Location } },
	},
	ScrinRebelsAir = {
		Delay = {
			easy = DateTime.Minutes(14),
			normal = DateTime.Minutes(10),
			hard = DateTime.Minutes(6)
		},
		AttackValuePerSecond = {
			easy = { Min = 3, Max = 3 },
			normal = { Min = 7, Max = 7 },
			hard = { Min = 12, Max = 12 },
		},
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
	NodAir = {
		Delay = {
			easy = DateTime.Minutes(15),
			normal = DateTime.Minutes(11),
			hard = DateTime.Minutes(7)
		},
		AttackValuePerSecond = {
			easy = { Min = 3, Max = 3 },
			normal = { Min = 7, Max = 7 },
			hard = { Min = 12, Max = 12 },
		},
		ProducerTypes = { Aircraft = { "hpad.td" } },
		Units = {
			easy = {
				{ Aircraft = { "apch" } }
			},
			normal = {
				{ Aircraft = { "apch", "apch" } },
				{ Aircraft = { "venm", "venm" } },
				{ Aircraft = { "scrn" } },
				{ Aircraft = { "rah" } }
			},
			hard = {
				{ Aircraft = { "apch", "apch", "apch" } },
				{ Aircraft = { "venm", "venm", "venm" } },
				{ Aircraft = { "scrn", "scrn" } },
				{ Aircraft = { "rah", "rah" } }
			}
		},
	},
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
	TimerTicks = PurificationInterval[Difficulty]

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(USSR)
	AdjustPlayerStartingCashForDifficulty()
	InitScrinRebels()
	InitNod()

	Actor.Create("hazmatsoviet.upgrade", true, { Owner = USSR })

	ObjectiveSecurePurifier = USSR.AddObjective("Use the Exterminator Tripod to secure\nthe purification device.")
	UpdateMissionText()

	local spyPlaneDummy1 = Actor.Create("spy.plane.dummy", true, { Owner = SpyPlaneProvider })

	Trigger.AfterDelay(DateTime.Seconds(20), function()
		spyPlaneDummy1.TargetAirstrike(Purifier.CenterPosition, Angle.NorthEast)
		spyPlaneDummy1.Destroy()
	end)

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

	Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(6)), function()
		Exterminator.Owner = USSR
	end)

	Trigger.AfterDelay(DateTime.Seconds(4), function()
		Media.DisplayMessage("Stop this madness. You have no idea what you are dealing with. You will be the end of us all!", "Kane", HSLColor.FromHex("FF0000"))
		MediaCA.PlaySound("kane_stopmadness.aud", 2)
	end)

	Trigger.AfterDelay(DateTime.Seconds(20), function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		Notification("Reinforcements have arrived.")
		Reinforcements.Reinforce(USSR, { "kiro" }, { KirovSpawn1.Location, KirovRally1.Location })
		Reinforcements.Reinforce(USSR, { "kiro" }, { KirovSpawn2.Location, KirovRally2.Location })
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
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
				else
					TimerTicks = 0
					PurificationWave()
				end
			else
				TimerTicks = PurificationInterval[Difficulty]
			end

			if Purifier.Owner == USSR then
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
	SetupRefAndSilosCaptureCredits(ScrinRebels)
	AutoReplaceHarvesters(ScrinRebels)
	InitAiUpgrades(ScrinRebels)

	AutoRepairBuildings(ScrinRebelsOuter)

	local scrinRebelsGroundAttackers = ScrinRebels.GetGroundAttackers()

	Utils.Do(scrinRebelsGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)

	Trigger.AfterDelay(Squads.ScrinRebels.Delay[Difficulty], function()
		InitAttackSquad(Squads.ScrinRebels, ScrinRebels)
	end)

	Trigger.AfterDelay(Squads.ScrinRebelsAir.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.ScrinRebelsAir, ScrinRebels, USSR, { "etpd", "harv", "4tnk", "4tnk.atomic", "3tnk", "3tnk.atomic", "3tnk.rhino", "3tnk.rhino.atomic",
			"katy", "v3rl", "ttra", "v3rl", "apwr", "tpwr", "npwr", "tsla", "proc", "nukc", "ovld", "apoc", "apoc.atomic", "ovld.atomic" })
	end)
end

InitNod = function()
	AutoRepairBuildings(Nod)
	AutoRepairAndRebuildBuildings(Nod)
	SetupRefAndSilosCaptureCredits(Nod)
	AutoReplaceHarvesters(Nod)
	InitAiUpgrades(Nod)

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)

	Trigger.AfterDelay(Squads.Nod.Delay[Difficulty], function()
		InitAttackSquad(Squads.Nod, Nod)
	end)

	Trigger.AfterDelay(Squads.NodAir.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.NodAir, Nod, USSR, { "etpd", "harv", "4tnk", "4tnk.atomic", "3tnk", "3tnk.atomic", "3tnk.rhino", "3tnk.rhino.atomic",
			"katy", "v3rl", "ttra", "v3rl", "apwr", "tpwr", "npwr", "tsla", "proc", "nukc", "ovld", "apoc", "apoc.atomic", "ovld.atomic" })
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
		end)

		MaleficSpawn()
	end
end

MaleficSpawn = function()
	local invasionInterval = {
		easy = DateTime.Seconds(35),
		normal = DateTime.Seconds(25),
		hard = DateTime.Seconds(15),
	}

	local invasionCompositions = {
		{ "intl", "s1", "s1", "s1", "s1", "s3", "s3", "s4", "s4", "muti", "muti", "muti" },
		{ "dark", "s1", "s1", "s1", "s1", "s3", "s4", "muti", "muti", "muti" },
		{ "tpod", "s1", "s1", "s1", "s3", "s3", "s4", "muti", "muti" },
		{ "devo", "s1", "s1", "s1", "s3", "s3", "s4", "muti", "muti", "muti" },
	}

	Utils.Do(MaleficSpawns, function(s)
		local units = Reinforcements.Reinforce(MaleficScrin, Utils.Shuffle(Utils.Random(invasionCompositions)), { s }, 1)
		Utils.Do(units, function(unit)
			unit.Scatter()
			Trigger.AfterDelay(5, function()
				if not unit.IsDead then
					unit.Hunt()
				end
			end)
		end)
	end)

	Trigger.AfterDelay(GetInvasionInterval(), MaleficSpawn)
end

GetPlayerArmyValue = function()
	local value = 0
	Utils.Do(USSR.GetActors(), function(a)
		if a.HasProperty("Attack") then
			value = value + Actor.Cost(a.Type)
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
