TimeLimit = {
	normal = DateTime.Minutes(75),
	hard = DateTime.Minutes(45),
}

CyborgSquad = { "rmbc", "rmbc", "enli", "tplr", "tplr", "tplr", "reap", "n1c", "n1c", "n1c", "n1c", "n1c", "n3c", "n3c" }
CyborgSquadInterval = {
	normal = DateTime.Minutes(2),
	hard = DateTime.Minutes(1),
}

AdjustedNodCompositions = AdjustCompositionsForDifficulty(UnitCompositions.Nod)

Squads = {
	Main1 = {
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 25 },
			normal = { Min = 25, Max = 50 },
			hard = { Min = 40, Max = 80 },
		},
		ActiveCondition = function()
			return NodEastOrWestNeutralized()
		end,
		FollowLeader = true,
		DispatchDelay = DateTime.Seconds(15),
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = AdjustedNodCompositions,
		AttackPaths = {
			{ NodRally1.Location },
			{ NodRally2.Location },
			{ NodRally3.Location },
			{ NodRally4.Location },
			{ NodRally5.Location },
			{ NodRally6.Location },
		},
	},
	Main2 = {
		Delay = {
			easy = DateTime.Minutes(5),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(3),
		},
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 25 },
			normal = { Min = 25, Max = 50 },
			hard = { Min = 40, Max = 80 },
		},
		FollowLeader = true,
		DispatchDelay = DateTime.Seconds(15),
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = AdjustedNodCompositions,
		AttackPaths = {
			{ NodRally4.Location },
			{ NodRally5.Location },
			{ NodRally6.Location },
		},
	},
	Main3 = {
		Delay = {
			easy = DateTime.Minutes(8),
			normal = DateTime.Minutes(6),
			hard = DateTime.Minutes(4),
		},
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 25 },
			normal = { Min = 25, Max = 50 },
			hard = { Min = 40, Max = 80 },
		},
		FollowLeader = true,
		DispatchDelay = DateTime.Seconds(15),
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = AdjustedNodCompositions,
		AttackPaths = {
			{ NodRally1.Location },
			{ NodRally2.Location },
			{ NodRally3.Location },
			{ NodRally4.Location },
		},
	},
	Air = {
		Delay = {
			easy = DateTime.Minutes(13),
			normal = DateTime.Minutes(12),
			hard = DateTime.Minutes(11)
		},
		AttackValuePerSecond = {
			easy = { Min = 7, Max = 7 },
			normal = { Min = 14, Max = 14 },
			hard = { Min = 21, Max = 21 },
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
	AntiTankAir = {
		Delay = {
			hard = DateTime.Minutes(15)
		},
		ActiveCondition = function()
			return #USSR.GetActorsByTypes({ "4tnk", "4tnk.atomic", "apoc", "apoc.atomic", "ovld", "ovld.atomic" }) > 8
		end,
		AttackValuePerSecond = {
			hard = { Min = 35, Max = 35 },
		},
		ProducerTypes = { Aircraft = { "hpad.td" } },
		Units = {
			hard = {
				{ Aircraft = { "scrn", "scrn", "scrn", "scrn", "scrn", "scrn" } },
			}
		},
	}
}

-- Setup and Tick

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Nod1 = Player.GetPlayer("Nod1")
	Nod2 = Player.GetPlayer("Nod2")
	Nod3 = Player.GetPlayer("Nod3")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { USSR }
	TimerTicks = TimeLimit[Difficulty]

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(USSR)
	AdjustStartingCash()
	InitNod()

	ObjectiveSecureGateway = USSR.AddObjective("Eliminate Nod forces near gateway.")

	Trigger.OnAllKilledOrCaptured({ NodEastAirstrip, NodEastHand }, function()
		InitNodSouth()
	end)

	Trigger.OnAllKilledOrCaptured({ NodWestAirstrip, NodWestHand }, function()
		InitNodSouth()
	end)

	Trigger.OnKilledOrCaptured(MainTemple, function()
		MainTempleKilledOrCaptured = true
	end)

	SetupSubterraneanStrikes()
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Nod1.Resources = Nod1.ResourceCapacity - 500

		if TimerTicks > 0 and Difficulty ~= "easy" then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
				UserInterface.SetMissionText("Kane's forces will begin returning in " .. UtilsCA.FormatTimeForGameSpeed(TimerTicks), HSLColor.Yellow)
			else
				TimerTicks = 0
				UserInterface.SetMissionText("")
				InitKaneReturn()
			end
		end

		if USSR.HasNoRequiredUnits() then
			if not USSR.IsObjectiveCompleted(ObjectiveSecureGateway) then
				USSR.MarkFailedObjective(ObjectiveSecureGateway)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()

		if MainTempleKilledOrCaptured then
			local nodForces = Map.ActorsInBox(NodMainTopLeft.CenterPosition, NodMainBottomRight.CenterPosition, function(a)
				return a.Owner == Nod1 and not a.IsDead and a.HasProperty("Health") and a.Type ~= "brik"
			end)

			if #nodForces == 0 and not USSR.IsObjectiveCompleted(ObjectiveSecureGateway) then
				USSR.MarkCompletedObjective(ObjectiveSecureGateway)
			end
		end
	end
end

-- Functions

InitNod = function()
	NodPlayers = { Nod1, Nod2, Nod3 }

	Utils.Do(NodPlayers, function(p)
		AutoRepairAndRebuildBuildings(p)
		SetupRefAndSilosCaptureCredits(p)
		AutoReplaceHarvesters(p)

		local nodGroundAttackers = p.GetGroundAttackers()

		Utils.Do(nodGroundAttackers, function(a)
			TargetSwapChance(a, 10)
			CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
		end)
	end)

	Trigger.AfterDelay(Squads.Main2.Delay[Difficulty], function()
		InitAttackSquad(Squads.Main2, Nod2)
	end)

	Trigger.AfterDelay(Squads.Main3.Delay[Difficulty], function()
		InitAttackSquad(Squads.Main3, Nod3)
	end)

	Trigger.AfterDelay(Squads.Air.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.Air, Nod1, USSR, { "harv", "4tnk", "4tnk.atomic", "3tnk", "3tnk.atomic", "3tnk.rhino", "3tnk.rhino.atomic",
			"katy", "v3rl", "ttra", "v3rl", "apwr", "tpwr", "npwr", "tsla", "proc", "nukc", "ovld", "apoc", "apoc.atomic", "ovld.atomic" })
	end)

	if Difficulty == "hard" then
		Trigger.AfterDelay(Squads.AntiTankAir.Delay[Difficulty], function()
			InitAirAttackSquad(Squads.AntiTankAir, Nod1, USSR, { "4tnk", "4tnk.atomic", "apoc", "apoc.atomic", "ovld", "ovld.atomic" })
		end)
	end
end

InitNodSouth = function()
	InitAttackSquad(Squads.Main1, Nod1)
end

NodEastOrWestNeutralized = function()
	local westCount = Nod2.GetActorsByTypes({ "airs", "hand" })
	local eastCount = Nod3.GetActorsByTypes({ "airs", "hand" })
	return #westCount == 0 or #eastCount == 0
end

InitKaneReturn = function()
	if not KaneReturnInitiated then
		KaneReturnInitiated = true
		Media.DisplayMessage("The Overlord will not be your salvation. Your empire is dead. Surrender, or be destroyed. My return will not be stopped.", "Kane", HSLColor.FromHex("FF0000"))
		MediaCA.PlaySound("kane_return.aud", 2.5)
		DeployCyborgs()
	end
end

DeployCyborgs = function()
	local units = Reinforcements.Reinforce(Nod1, Utils.Shuffle(CyborgSquad), { Gateway.Location }, 5)
	Utils.Do(units, function(unit)
		unit.Scatter()
		Trigger.AfterDelay(5, function()
			AssaultPlayerBaseOrHunt(unit, USSR)
		end)
	end)
	Trigger.AfterDelay(CyborgSquadInterval[Difficulty], DeployCyborgs)
end

SetupSubterraneanStrikes = function()

	-- Subterranean Strikes
	local subStrike1Spawns = { SubStrike1_1.Location }

	if Difficulty ~= "easy" then
		table.insert(subStrike1Spawns, SubStrike1_3.Location)

		if Difficulty == "hard" then
			subStrike1Spawns = Utils.Concat(subStrike1Spawns, { SubStrike1_2.Location, SubStrike1_4.Location })
		end
	end

	local subStrike2Spawns = { SubStrike2_1.Location }

	if Difficulty ~= "easy" then
		table.insert(subStrike2Spawns, SubStrike2_3.Location)

		if Difficulty == "hard" then
			subStrike2Spawns = Utils.Concat(subStrike2Spawns, { SubStrike2_2.Location, SubStrike2_4.Location })
		end
	end

	local subStrike3Spawns = { SubStrike3_2.Location }

	if Difficulty ~= "easy" then
		subStrike3Spawns = Utils.Concat(subStrike3Spawns, { SubStrike3_5.Location, SubStrike3_8.Location })

		if Difficulty == "hard" then
			subStrike3Spawns = Utils.Concat(subStrike3Spawns, { SubStrike3_1.Location, SubStrike3_3.Location, SubStrike3_4.Location, SubStrike3_6.Location, SubStrike3_7.Location, SubStrike3_9.Location })
		end
	end

	local subStrike4Spawns = { SubStrike4_1.Location, SubStrike4_2.Location }

	local allSubStrikeSpawns = Utils.Concat(subStrike1Spawns, subStrike2Spawns)
	allSubStrikeSpawns = Utils.Concat(allSubStrikeSpawns, subStrike3Spawns)
	allSubStrikeSpawns = Utils.Concat(allSubStrikeSpawns, subStrike4Spawns)

	local leftBaseEntrance = {}
	for x = 13, 19 do
		table.insert(leftBaseEntrance, CPos.New(x, 54))
	end

	local rightBaseEntrance = {}
	for x = 85, 91 do
		table.insert(rightBaseEntrance, CPos.New(x, 11))
	end
	for x = 84, 91 do
		table.insert(rightBaseEntrance, CPos.New(x, 34))
	end

	Trigger.OnEnteredFootprint(leftBaseEntrance, function(a, id)
		if a.Owner == USSR then
			Trigger.RemoveFootprintTrigger(id)
			if not LeftBaseSubStrikeTriggered then
				LeftBaseSubStrikeTriggered = true
				Media.PlaySound("subrumble.aud")
				Trigger.AfterDelay(DateTime.Seconds(3), function()
					Utils.Do(subStrike1Spawns, function(s)
						local spawner = Actor.Create("substrike.spawner", true, { Owner = Nod1, Location = s })
					end)
				end)
			end
		end
	end)

	Trigger.OnEnteredFootprint(rightBaseEntrance, function(a, id)
		if a.Owner == USSR then
			Trigger.RemoveFootprintTrigger(id)
			if not RightBaseSubStrikeTriggered then
				RightBaseSubStrikeTriggered = true
				Media.PlaySound("subrumble.aud")
				Trigger.AfterDelay(DateTime.Seconds(3), function()
					Utils.Do(subStrike2Spawns, function(s)
						local spawner = Actor.Create("substrike.spawner", true, { Owner = Nod1, Location = s })
					end)
				end)
			end
		end
	end)

	Trigger.OnEnteredProximityTrigger(NodMainBaseCenter.CenterPosition, WDist.FromCells(15), function(a, id)
		if a.Owner == USSR and not a.HasProperty("Land") then
			Trigger.RemoveProximityTrigger(id)
			if not MainBaseSubStrikeTriggered then
				MainBaseSubStrikeTriggered = true
				Media.PlaySound("subrumble.aud")
				Trigger.AfterDelay(DateTime.Seconds(3), function()
					Utils.Do(subStrike3Spawns, function(s)
						local spawner = Actor.Create("substrike.spawner", true, { Owner = Nod1, Location = s })
					end)
				end)
			end
		end
	end)

	if Difficulty ~= "easy" then
		Trigger.AfterDelay(DateTime.Minutes(15), function()
			Utils.Do(subStrike4Spawns, function(s)
				local spawner = Actor.Create("substrike.spawner", true, { Owner = Nod1, Location = s })
			end)
		end)
	end

	Utils.Do(allSubStrikeSpawns, function(s)
		Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(s), WDist.FromCells(2), function(a, id)
			if a.Owner == Nod1 and a.Type == "mole.upg" then
				Trigger.RemoveProximityTrigger(id)
				if not a.IsDead then
					Trigger.OnPassengerExited(a, function(transport, passenger)
						IdleHunt(passenger)
					end)
				end
			end
		end)
	end)
end