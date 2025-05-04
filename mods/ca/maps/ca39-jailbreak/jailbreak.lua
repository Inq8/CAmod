CruisersEnabledTime = {
	easy = DateTime.Minutes(25),
	normal = DateTime.Minutes(20),
	hard = DateTime.Minutes(15),
}

CruiserInterval = {
	easy = DateTime.Minutes(11),
	normal = DateTime.Minutes(8),
	hard = DateTime.Minutes(5),
}

ChronoTankInterval = {
	easy = DateTime.Minutes(7),
	normal = DateTime.Minutes(6),
	hard = DateTime.Minutes(3),
}

SuperweaponsEnabledTime = {
	easy = DateTime.Seconds((60 * 45) + 17),
	normal = DateTime.Seconds((60 * 30) + 17),
	hard = DateTime.Seconds((60 * 15) + 17),
}

Squads = {
	Main = {
		InitTime = 0 - DateTime.Minutes(5),
		Delay = {
            easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(4),
            hard = DateTime.Minutes(2),
		},
		AttackValuePerSecond = {
			easy = { Min = 20, Max = 50, RampDuration = DateTime.Minutes(11) },
			normal = { Min = 50, Max = 100, RampDuration = DateTime.Minutes(8) },
			hard = { Min = 80, Max = 160, RampDuration = DateTime.Minutes(5) },
		},
		ActiveCondition = function()
			return HasConyardAcrossRiver()
		end,
		FollowLeader = true,
		ProducerTypes = { Infantry = { "tent" }, Vehicles = { "weap" } },
		Units = AdjustCompositionsForDifficulty(UnitCompositions.Allied),
		AttackPaths = {
			{ Path1_1.Location, Path1_2.Location },
			{ Path2_1.Location, Path2_2.Location },
        },
	},
	Air = {
		Delay = {
			easy = DateTime.Minutes(11),
			normal = DateTime.Minutes(8),
			hard = DateTime.Minutes(5)
		},
		AttackValuePerSecond = {
			easy = { Min = 7, Max = 7 },
			normal = { Min = 21, Max = 21 },
			hard = { Min = 35, Max = 35 },
		},
		ProducerTypes = { Aircraft = { "hpad" } },
		Units = {
			easy = {
				{ Aircraft = { "heli", "heli" } },
				{ Aircraft = { "pmak" } },
			},
			normal = {
				{ Aircraft = { "heli", "heli", "heli" } },
				{ Aircraft = { "harr", "harr" } },
				{ Aircraft = { "pmak", "pmak" } }
			},
			hard = {
				{ Aircraft = { "heli", "heli", "heli", "heli" } },
				{ Aircraft = { "harr", "harr", "harr" } },
				{ Aircraft = { "pmak", "pmak", "pmak" } }
			}
		},
	},
	Air2 = {
		Delay = {
			easy = DateTime.Minutes(13),
			normal = DateTime.Minutes(10),
			hard = DateTime.Minutes(7)
		},
		AttackValuePerSecond = {
			easy = { Min = 7, Max = 7 },
			normal = { Min = 14, Max = 14 },
			hard = { Min = 21, Max = 21 },
		},
		ActiveCondition = function()
			return not HasConyardAcrossRiver()
		end,
		ProducerTypes = { Aircraft = { "hpad" } },
		Units = {
			easy = {
				{ Aircraft = { "heli", "heli" } },
				{ Aircraft = { "harr" } },
				{ Aircraft = { "pmak" } },
			},
			normal = {
				{ Aircraft = { "heli", "heli", "heli", "heli" } },
				{ Aircraft = { "harr", "harr", "harr" } },
				{ Aircraft = { "pmak", "pmak" } }
			},
			hard = {
				{ Aircraft = { "heli", "heli", "heli", "heli", "heli", "heli" } },
				{ Aircraft = { "harr", "harr", "harr", "harr", "harr", "harr" } },
				{ Aircraft = { "pmak", "pmak", "pmak", "pmak" } }
			}
		},
	}
}

-- Setup and Tick

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Greece = Player.GetPlayer("Greece")
	GreeceNorth = Player.GetPlayer("GreeceNorth")
	Scrin = Player.GetPlayer("Scrin")
	GDI = Player.GetPlayer("GDI")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { USSR }
	TimerTicks = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(USSR)
	AdjustPlayerStartingCashForDifficulty()
	InitGreece()

	ObjectiveClearPath = USSR.AddObjective("Clear a path for reinforcements.")
	ObjectiveCapturePrison = USSR.AddObjective("Capture Allied prison to free Yuri.")

	Trigger.AfterDelay(1, function()
		local topPathUnits = Map.ActorsInBox(TopPathTopLeft.CenterPosition, TopPathBottomRight.CenterPosition, function(a)
			return a.Owner == Greece and not a.IsDead and a.HasProperty("Health")
		end)

		Trigger.OnAllKilled(topPathUnits, function()
			PathCleared()
		end)

		local bottomPathUnits = Map.ActorsInBox(BottomPathTopLeft.CenterPosition, BottomPathBottomRight.CenterPosition, function(a)
			return a.Owner == Greece and not a.IsDead and a.HasProperty("Health")
		end)

		Trigger.OnAllKilled(bottomPathUnits, function()
			PathCleared()
		end)

		local northShoreTurrets = { NorthShoreTurret1, NorthShoreTurret2, NorthShoreTurret3, NorthShoreTurret4 }

		Trigger.OnAllKilled(northShoreTurrets, function()
			SendLandingCraft()
		end)
	end)

	Trigger.OnCapture(Prison, function(self, captor, oldOwner, newOwner)
		if newOwner == USSR then
			local yuri = Reinforcements.Reinforce(USSR, { "yuri" }, { PrisonerSpawn.Location, YuriRally.Location })[1]
			local prodigy = Reinforcements.Reinforce(Scrin, { "pdgy" }, { PrisonerSpawn.Location, ProdigyRally.Location })[1]

			Trigger.AfterDelay(DateTime.Seconds(2), function()

				if AlliedBuildingsEliminated() then
					Media.DisplayMessage("Ah, Comrade General, thank you for releasing us. I have a proposal that you may find interesting...", "Yuri", HSLColor.FromHex("FF00BB"))
					MediaCA.PlaySound("yuri_releasedwin.aud", 2)

					Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(10)), function()
						if ObjectiveCapturePrison ~= nil and not USSR.IsObjectiveCompleted(ObjectiveCapturePrison) then
							USSR.MarkCompletedObjective(ObjectiveCapturePrison)
						end
					end)
				else
					ObjectiveEliminateAllies = USSR.AddObjective("Eliminate remaining Allied presence.")
					ObjectiveKeepYuriAndProdigyAlive = USSR.AddObjective("Yuri and the Prodigy must survive.")

					Trigger.OnAnyKilled({ yuri, prodigy }, function(self, killer)
						if not USSR.IsObjectiveCompleted(ObjectiveKeepYuriAndProdigyAlive) then
							USSR.MarkFailedObjective(ObjectiveKeepYuriAndProdigyAlive)
						end
					end)

					if ObjectiveCapturePrison ~= nil and not USSR.IsObjectiveCompleted(ObjectiveCapturePrison) then
						USSR.MarkCompletedObjective(ObjectiveCapturePrison)
					end

					Media.DisplayMessage("Ah, Comrade General, thank you for releasing us. I have a proposal that you may find interesting. But first, we must deal with these pests.", "Yuri", HSLColor.FromHex("FF00BB"))
					MediaCA.PlaySound("yuri_released.aud", 2)

					Trigger.AfterDelay(DateTime.Seconds(5), function()
						prodigy.Owner = USSR
					end)
				end
			end)
		end
	end)

	Trigger.OnKilled(Prison, function(self, killer)
		if not USSR.IsObjectiveCompleted(ObjectiveCapturePrison) then
			USSR.MarkFailedObjective(ObjectiveCapturePrison)
		end
	end)

	Trigger.OnEnteredProximityTrigger(AlliedOutpost.CenterPosition, WDist.New(6 * 1024), function(a, id)
		if a.Owner == USSR and a.Type == "e6" then
			Trigger.RemoveProximityTrigger(id)

			Utils.Do({NorthConyard, NorthFactory, NorthBarracks}, function(b)
				if not b.IsDead and b.Owner == GreeceNorth then
					b.Sell()
				end
			end)
		end
	end)

	Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = Greece })
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	OncePerThirtySecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Greece.Resources = Greece.ResourceCapacity - 500

		if USSR.HasNoRequiredUnits() then
			if not USSR.IsObjectiveCompleted(ObjectiveCapturePrison) then
				USSR.MarkFailedObjective(ObjectiveCapturePrison)
			end
			if ObjectiveEliminateAllies ~= nil and not USSR.IsObjectiveCompleted(ObjectiveEliminateAllies) then
				USSR.MarkFailedObjective(ObjectiveEliminateAllies)
			end
		end

		if ObjectiveEliminateAllies ~= nil and AlliedBuildingsEliminated() then
			if not USSR.IsObjectiveCompleted(ObjectiveEliminateAllies) then
				USSR.MarkCompletedObjective(ObjectiveEliminateAllies)
			end
			if not USSR.IsObjectiveCompleted(ObjectiveKeepYuriAndProdigyAlive) then
				USSR.MarkCompletedObjective(ObjectiveKeepYuriAndProdigyAlive)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()

		if not AlliedGroundAttacksStarted and HasConyardAcrossRiver() then
			AlliedGroundAttacksStarted = true
			Trigger.AfterDelay(Squads.Main.Delay[Difficulty], function()

				if Difficulty == "hard" then
					Squads.Main.InitTime = math.max(DateTime.GameTime - DateTime.Minutes(7), 0)
				elseif Difficulty == "normal" then
					Squads.Main.InitTime = math.max(DateTime.GameTime - DateTime.Minutes(4), 0)
				else
					Squads.Main.InitTime = math.max(DateTime.GameTime - DateTime.Minutes(1), 0)
				end

				InitAttackSquad(Squads.Main, Greece)
			end)
		end
	end
end

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % DateTime.Seconds(30) == 0 then
		CalculatePlayerCharacteristics()
	end
end

-- Functions

InitGreece = function()
	AutoRepairBuildings(GreeceNorth)
	SetupRefAndSilosCaptureCredits(GreeceNorth)

	AutoRepairAndRebuildBuildings(Greece, 15)
	SetupRefAndSilosCaptureCredits(Greece)
	AutoReplaceHarvesters(Greece)
	AutoRebuildConyards(Greece)
	InitAiUpgrades(Greece)

	Utils.Do({ Greece, GreeceNorth }, function(p)
		local greeceGroundAttackers = p.GetGroundAttackers()

		Utils.Do(greeceGroundAttackers, function(a)
			TargetSwapChance(a, 10)
			CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGreeceGroundHunterUnit)
		end)
	end)

	local gdiGroundAttackers = GDI.GetGroundAttackers()
	Utils.Do(gdiGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGDIGroundHunterUnit)
	end)

	Trigger.OnProduction(AlliedNavalYard, function(producer, produced)
		if produced.Type == "ca" and not produced.IsDead then
			produced.Patrol({ CruiserPatrol1.Location, CruiserPatrol2.Location })
		end
	end)

	Trigger.AfterDelay(CruisersEnabledTime[Difficulty], function()
		InitCruisers()
	end)

	Trigger.AfterDelay(Squads.Air.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.Air, Greece)
	end)

	Trigger.AfterDelay(Squads.Air2.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.Air2, Greece)
	end)
end

HasConyardAcrossRiver = function()
	local conyards = USSR.GetActorsByType("fact")

	local conyardsAcrossRiver = Utils.Where(conyards, function(c)
		return c.Owner == USSR and c.Location.Y > 40
	end)

	return #conyardsAcrossRiver > 0
end

PathCleared = function()
	if not USSR.IsObjectiveCompleted(ObjectiveClearPath) then
		USSR.MarkCompletedObjective(ObjectiveClearPath)
	end

	if not McvRequested then
		McvRequested = true

		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
			Notification("Reinforcements have arrived.")
			Reinforcements.Reinforce(USSR, { "mcv" }, { McvSpawn.Location, McvRally.Location })
			Beacon.New(USSR, McvRally.CenterPosition)
			McvArrived = true

			Trigger.AfterDelay(ChronoTankInterval[Difficulty], function()
				InitChronoTankAttack()
			end)
		end)
	end
end

SendLandingCraft = function()
	Trigger.AfterDelay(DateTime.Seconds(4), function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		Notification("Reinforcements have arrived.")
		Beacon.New(USSR, LandingCraftSpawn.CenterPosition)
		Reinforcements.Reinforce(USSR, { "ss" }, { SubSpawn1.Location, SubRally1.Location })
		Reinforcements.Reinforce(USSR, { "ss" }, { SubSpawn2.Location, SubRally2.Location })

		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Reinforcements.Reinforce(USSR, { "lst" }, { LandingCraftSpawn.Location, LandingCraftRally.Location })
		end)
	end)
end

InitCruisers = function()
	if AlliedNavalYard.IsDead then
		return
	end

	local currentCruisers = Greece.GetActorsByType("ca")

	if #currentCruisers == 0 then
		local cruiserCount
		if Difficulty == "easy" then
			cruiserCount = 1
		elseif Difficulty == "normal" then
			cruiserCount = 2
		elseif Difficulty == "hard" then
			cruiserCount = 3
		end

		for i = 1, cruiserCount do
			Trigger.AfterDelay(DateTime.Seconds(10 * (i - 1)) + 1, function()
				if not AlliedNavalYard.IsDead and AlliedNavalYard.Owner == Greece then
					AlliedNavalYard.Produce("ca")
				end
			end)
		end
	end

	Trigger.AfterDelay(CruiserInterval[Difficulty], function()
		InitCruisers()
	end)
end

InitChronoTankAttack = function()
	local alliedFactories = Greece.GetActorsByType("weap")
	if #alliedFactories == 0 then
		return
	end

	local chronoTanksSquad = { "ctnk", "ctnk", "ctnk" }

	if Difficulty == "normal" then
		table.insert(chronoTanksSquad, "ctnk")
	end

	if Difficulty == "hard" then
		table.insert(chronoTanksSquad, "ctnk")
		table.insert(chronoTanksSquad, "ctnk")
	end

	local chronoTanks = Reinforcements.Reinforce(Greece, chronoTanksSquad, { ChronoTankSpawn.Location, ChronoTankRally.Location })
	local dest = Utils.Random({ ChronoDest1.Location, ChronoDest2.Location, ChronoDest3.Location, ChronoDest4.Location,
		ChronoDest5.Location, ChronoDest6.Location, ChronoDest7.Location, ChronoDest8.Location, ChronoDest9.Location
	})

	if HasConyardAcrossRiver() then
		dest = Utils.Random({ ChronoDest10.Location, ChronoDest11.Location, ChronoDest12.Location })
	end

	Utils.Do(chronoTanks, function(t)
		t.Move(ChronoTankRally.Location)
		t.PortableChronoTeleport(dest, true)
		t.Hunt()
	end)

	Trigger.AfterDelay(ChronoTankInterval[Difficulty], function()
		InitChronoTankAttack()
	end)
end

AlliedBuildingsEliminated = function()
	local alliedBuildings = Utils.Where(Greece.GetActors(), function(a)
		return a.HasProperty("StartBuildingRepairs") and not a.HasProperty("Attack") and a.Type ~= "silo"
	end)
	return #alliedBuildings == 0
end
