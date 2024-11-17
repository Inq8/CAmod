PurificationInterval = {
	easy = DateTime.Minutes(3) + DateTime.Seconds(5),
	normal = DateTime.Minutes(3) + DateTime.Seconds(5),
	hard = DateTime.Seconds(90) + DateTime.Seconds(5),
}

DefendDuration = {
	easy = DateTime.Minutes(2),
	normal = DateTime.Minutes(3),
	hard = DateTime.Minutes(4),
}

MaleficSpawns = { MaleficSpawn1.Location, MaleficSpawn2.Location, MaleficSpawn3.Location, MaleficSpawn4.Location, MaleficSpawn5.Location, MaleficSpawn6.Location, MaleficSpawn7.Location }

Squads = {
	Nod = {
		Delay = {
			easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(2)
		},
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 10 } },
			normal = { { MinTime = 0, Value = 30 } },
			hard = { { MinTime = 0, Value = 50 } },
		},
		ActiveCondition = function()
			return not MaleficArrived
		end,
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = UnitCompositions.Nod.Main,
		AttackPaths = { { NodRally1.Location, NodRally2.Location } },
	},
	ScrinRebels = {
		Delay = {
			easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(2)
		},
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 10 } },
			normal = { { MinTime = 0, Value = 30 } },
			hard = { { MinTime = 0, Value = 50 } },
		},
		ActiveCondition = function()
			return not MaleficArrived
		end,
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = UnitCompositions.Scrin.Main,
		AttackPaths = { { RebelRally1.Location, RebelRally2.Location } },
	}
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
	MissionPlayers = { USSR }
	TimerTicks = PurificationInterval[Difficulty]

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(USSR)
	InitScrinRebels()
	InitNod()

	Actor.Create("hazmatsoviet.upgrade", true, { Owner = USSR })

	ObjectiveSecurePurifier = USSR.AddObjective("Secure the purification device.")
	UpdateMissionText()

	Trigger.OnKilled(Purifier, function()
		if not USSR.IsObjectiveCompleted(ObjectiveSecurePurifier) then
			USSR.MarkFailedObjective(ObjectiveSecurePurifier)
		end
		if ObjectiveDefendPurifier ~= nil and not USSR.IsObjectiveCompleted(ObjectiveDefendPurifier) then
			USSR.MarkFailedObjective(ObjectiveDefendPurifier)
		end
	end)

	Trigger.OnKilled(Exterminator, function()
		if not USSR.IsObjectiveCompleted(ObjectiveSecurePurifier) then
			USSR.MarkFailedObjective(ObjectiveSecurePurifier)
		end
	end)

	Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(10)), function()
		Exterminator.Owner = USSR
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Media.DisplayMessage("Stop this madness. You have no idea what you are dealing with. You will be the end of us all!", "Kane", HSLColor.FromHex("FF0000"))
		MediaCA.PlaySound("kane_stopmadness.aud", 2)
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

	AutoRepairBuildings(ScrinRebelsOuter)

	local scrinRebelsGroundAttackers = ScrinRebels.GetGroundAttackers()

	Utils.Do(scrinRebelsGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)

	Trigger.AfterDelay(Squads.ScrinRebels.Delay[Difficulty], function()
		InitAttackSquad(Squads.ScrinRebels, ScrinRebels)
	end)
end

InitNod = function()
	AutoRepairBuildings(Nod)
	AutoRepairAndRebuildBuildings(Nod)
	SetupRefAndSilosCaptureCredits(Nod)
	AutoReplaceHarvesters(Nod)

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)

	Trigger.AfterDelay(Squads.Nod.Delay[Difficulty], function()
		InitAttackSquad(Squads.Nod, Nod)
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
