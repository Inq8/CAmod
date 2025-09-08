MissionDir = "ca/missions/main-campaign/ca22-decimation"

PowerGrids = {
	{
		Providers = { AtomicPower1, AtomicPower2, AtomicPower3, TeslaPower1, TeslaPower2, TeslaPower3, TeslaPower4, TeslaPower5, TeslaPower6 },
		Consumers = { TeslaCoil1, TeslaCoil2, TeslaCoil3, TeslaCoil4, TeslaCoil5, TeslaCoil6, TeslaCoil7, TeslaCoil8, TeslaCoil9, TeslaCoil10, TeslaCoil11, TeslaCoil12, TeslaCoil13, TeslaCoil14, TeslaCoil15, TeslaCoil16, TeslaCoil17, TeslaCoil18, TeslaCoil19, TeslaCoil20, TeslaCoil21, TeslaCoil22, TeslaCoil23, TeslaCoil24, TeslaCoil25 },
	},
}

ForwardSAMs = { ForwardSAM1, ForwardSAM2, ForwardSAM3, ForwardSAM4, ForwardSAM5, ForwardSAM6, ForwardSAM7, ForwardSAM8 }

IslandAirfields = { IslandAirfield1, IslandAirfield2, IslandAirfield3, IslandAirfield4, IslandAirfield5 }

IslandSAMs = { IslandSAM1, IslandSAM2, IslandSAM3 }

EastAttackPaths = {
	{ EastAttack1.Location, EastAttack2.Location, EastAttack3.Location, EastAttack4.Location },
	{ EastAttack1.Location, EastAttack2.Location, EastAttack4.Location }
}

WestAttackPaths = {
	{ WestAttack1.Location, WestAttack2.Location, WestAttack3.Location, WestAttack4.Location }
}

AirFleetKillersThreshold = {
	normal = 6,
	hard = 4,
	vhard = 3,
	brutal = 2
}

MaxFleetKillers = {
	normal = 3,
	hard = 5,
	vhard = 8,
	brutal = 12
}

TripodKillersThreshold = {
	normal = 11,
	hard = 9,
	vhard = 7,
	brutal = 5
}

MaxTripodKillers = {
	normal = 3,
	hard = 5,
	vhard = 8,
	brutal = 12
}

ParabombsEnabledDelay = {
	easy = DateTime.Minutes(6),
	normal = DateTime.Minutes(5),
	hard = DateTime.Minutes(4),
	vhard = DateTime.Minutes(3),
	brutal = DateTime.Minutes(2)
}

ParatroopersEnabledDelay = {
	easy = DateTime.Minutes(5) + DateTime.Seconds(30),
	normal = DateTime.Minutes(4) + DateTime.Seconds(30),
	hard = DateTime.Minutes(3) + DateTime.Seconds(30),
	vhard = DateTime.Minutes(2) + DateTime.Seconds(30),
	brutal = DateTime.Minutes(1) + DateTime.Seconds(30)
}

IronCurtainEnabledDelay = {
	easy = DateTime.Minutes(25),
	normal = DateTime.Minutes(15),
	hard = DateTime.Minutes(8),
	vhard = DateTime.Minutes(5),
	brutal = DateTime.Minutes(5)
}

if IsVeryHardOrAbove() then
	table.insert(UnitCompositions.Soviet, {
		Infantry = { "deso", "deso", "deso", "deso", "deso", "deso", "deso", "deso", "deso" },
		Vehicles = { "apoc.erad", "apoc.erad", "apoc.erad", "apoc.erad", "4tnk.erad", "4tnk.erad", "4tnk.erad", "4tnk.erad" },
		MinTime = DateTime.Minutes(18),
		IsSpecial = true
	})
end

AdjustedSovietCompositions = AdjustCompositionsForDifficulty(UnitCompositions.Soviet)

Squads = {
	East = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(6)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		FollowLeader = true,
		ProducerActors = { Infantry = { MainBarracks }, Vehicles = { MainFactory } },
		Compositions = AdjustedSovietCompositions,
		AttackPaths = EastAttackPaths,
	},
	West = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(4)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		FollowLeader = true,
		ProducerActors = { Infantry = { Barracks1, Barracks2 }, Vehicles = { Factory1, Factory2, Factory3, Factory4, Factory5 } },
		Compositions = AdjustedSovietCompositions,
		AttackPaths = WestAttackPaths,
	},
	AirMain = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		ActiveCondition = function()
			return not IslandAirfieldsEliminated
		end,
		Compositions = AirCompositions.Soviet,
	},
	AirFleetKillers = {
		ActiveCondition = function(squad)
			local scrinFleet = squad.TargetPlayer.GetActorsByTypes({ "pac", "deva" })
			return #scrinFleet > AirFleetKillersThreshold[Difficulty] and not IslandAirfieldsEliminated
		end,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 50, Max = 50 }),
		Compositions = function(squad)
			local migs = { "mig" }
			local numFleetShips = #squad.TargetPlayer.GetActorsByArmorTypes({ "Aircraft" })
			for i = 1, math.min(numFleetShips, MaxFleetKillers[Difficulty]) do
				table.insert(migs, "mig")
			end
			return { { Aircraft = migs } }
		end
	},
	TripodKillers = {
		ActiveCondition = function(squad)
			local tripods = squad.TargetPlayer.GetActorsByTypes({ "tpod", "rtpd" })
			return #tripods > TripodKillersThreshold[Difficulty] and not IslandAirfieldsEliminated
		end,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 30, Max = 30 }),
		Compositions = function(squad)
			local sukhois = { "suk" }
			local numTripods = #squad.TargetPlayer.GetActorsByTypes({ "tpod", "rtpd" })
			for i = 1, math.min(numTripods, MaxTripodKillers[Difficulty]) do
				table.insert(sukhois, "suk")
			end
			return { { Aircraft = sukhois } }
		end
	}
}

WorldLoaded = function()
	Scrin = Player.GetPlayer("Scrin")
	USSR = Player.GetPlayer("USSR")
	USSRUnmanned = Player.GetPlayer("USSRUnmanned")
	MissionPlayers = { Scrin }
	MissionEnemies = { USSR }
	IslandAirfieldsEliminated = false
	IslandSAMsDestroyed = false
	DefensesOffline = false

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Scrin)
	AdjustPlayerStartingCashForDifficulty()
	InitUSSR()

	if Difficulty == "brutal" then
		Trigger.AfterDelay(DateTime.Minutes(10), function()
			Actor.Create("ai.superweapons.enabled", true, { Owner = USSR })
		end)
	end

	ObjectiveDestroyBases = Scrin.AddObjective("Eliminate Soviet bases.")
	ObjectiveDestroyUncrewed = Scrin.AddObjective("Destroy all uncrewed Soviet vehicles.")
	ObjectiveDestroySAMs = Scrin.AddSecondaryObjective("Destroy front line of Soviet SAM Sites.")

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Media.PlaySpeechNotification(Scrin, "ReinforcementsArrived")
		Notification("Reinforcements have arrived.")
		Reinforcements.Reinforce(Scrin, { "devo" }, { ScrinReinforce1Spawn.Location, ScrinReinforce1Dest.Location }, 75)
		Reinforcements.Reinforce(Scrin, { "devo" }, { ScrinReinforce2Spawn.Location, ScrinReinforce2Dest.Location }, 75)
	end)

	Trigger.AfterDelay(DateTime.Minutes(20), function()
		Actor.Create("reaperaccess", true, { Owner = Scrin })
		Notification("You have been granted access to Reaper Tripods.")
		MediaCA.PlaySound(MissionDir .. "/s_reaperaccess.aud", 2)
	end)

	Utils.Do(PowerGrids, function(grid)
		Trigger.OnAllKilledOrCaptured(grid.Providers, function()
			Utils.Do(grid.Consumers, function(consumer)
				if not consumer.IsDead then
					consumer.GrantCondition("disabled")
				end
			end)
			DefensesOffline = true
			Notification("Soviet power supply neutralized; defenses are now offline.")
			MediaCA.PlaySound(MissionDir .. "/s_sovietpoweroffline.aud", 2)
		end)
	end)

	local unmannedVehicles = USSRUnmanned.GetActorsByTypes({ "btr", "3tnk", "4tnk", "apoc", "ttra", "ttnk" })

	Trigger.OnAllKilledOrCaptured(unmannedVehicles, function()
		Scrin.MarkCompletedObjective(ObjectiveDestroyUncrewed)
	end)

	Trigger.OnAllKilledOrCaptured(IslandSAMs, function()
		IslandSAMsDestroyed = true

		if IslandAirfieldsEliminated then
			DevastatorReinforcements()
		end
	end)

	Trigger.OnAllKilledOrCaptured(ForwardSAMs, function()
		Actor.Create("fleetaccess", true, { Owner = Scrin })
		Scrin.MarkCompletedObjective(ObjectiveDestroySAMs)
		Notification("Scrin fleet vessels now available.")
		MediaCA.PlaySound(MissionDir .. "/s_scrinfleet.aud", 2)

		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Notification("Reinforcements have arrived.")
			Media.PlaySpeechNotification(Scrin, "ReinforcementsArrived")
			Reinforcements.Reinforce(Scrin, { "pac" }, { ScrinReinforce1Spawn.Location, ScrinReinforce1Dest.Location }, 75)
			Reinforcements.Reinforce(Scrin, { "pac" }, { ScrinReinforce2Spawn.Location, ScrinReinforce2Dest.Location }, 75)
		end)
	end)

	Trigger.OnAllKilledOrCaptured(IslandAirfields, function()
		IslandAirfieldsEliminated = true

		if IslandSAMsDestroyed then
			DevastatorReinforcements()
		end
	end)

	Trigger.OnEnteredProximityTrigger(TankYardReveal.CenterPosition, WDist.New(11 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) and a.Type ~= "camera" then
			Trigger.RemoveProximityTrigger(id)

			Notification("The entrance to the Soviet equipment holding area has been located.")
			MediaCA.PlaySound(MissionDir .. "/s_sovietholdingarea.aud", 2)

			if not DefensesOffline then
				Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(5)), function()
					Notification("Substantial defenses detected. Recommened neutralizing power before beginning assault.")
					MediaCA.PlaySound(MissionDir .. "/s_neutralizepower.aud", 2)
				end)
			end

			Beacon.New(Scrin, TankYardReveal.CenterPosition)
			local camera = Actor.Create("camera", true, { Owner = Scrin, Location = TankYardReveal.Location })
			Trigger.AfterDelay(DateTime.Seconds(4), function()
				camera.Destroy()
			end)
		end
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
		USSR.Resources = USSR.ResourceCapacity - 500

		if not PlayerHasBuildings(USSR) then
			if not Scrin.IsObjectiveCompleted(ObjectiveDestroyBases) then
				Scrin.MarkCompletedObjective(ObjectiveDestroyBases)
			end
		end

		if MissionPlayersHaveNoRequiredUnits() then
			if ObjectiveDestroyBases ~= nil and not Scrin.IsObjectiveCompleted(ObjectiveDestroyBases) then
				Scrin.MarkFailedObjective(ObjectiveDestroyBases)
			end
			if ObjectiveDestroyUncrewed ~= nil and not Scrin.IsObjectiveCompleted(ObjectiveDestroyUncrewed) then
				Scrin.MarkFailedObjective(ObjectiveDestroyUncrewed)
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

InitUSSR = function()
	RebuildExcludes.USSR = { Types = { "npwr", "tpwr", "tsla" }, Actors = { IslandAirfield1, IslandAirfield2, IslandAirfield3, IslandAirfield4, IslandAirfield5 } }

	AutoRepairAndRebuildBuildings(USSR, 15)
	SetupRefAndSilosCaptureCredits(USSR)
	AutoReplaceHarvesters(USSR)
	AutoRebuildConyards(USSR)
	InitAiUpgrades(USSR)
	InitAttackSquad(Squads.West, USSR)
	InitAttackSquad(Squads.East, USSR)
	InitAirAttackSquad(Squads.AirMain, USSR)

	if IsVeryHardOrAbove() then
		SellOnCaptureAttempt({ SWBarracks, SEBarracks1, SEBarracks2 })

		if Difficulty == "brutal" then
			Trigger.AfterDelay(DateTime.Minutes(20), function()
				CompositionValueMultipliers.brutal = 1.4
			end)
		end
	end

	if Difficulty ~= "easy" then
		InitAirAttackSquad(Squads.AirFleetKillers, USSR, MissionPlayers, { "pac", "deva", "stmr", "enrv", "torm" })
	end

	if Difficulty ~= "easy" then
		InitAirAttackSquad(Squads.TripodKillers, USSR, MissionPlayers, { "tpod", "rtpd" })
	end

	Actor.Create("ai.unlimited.power", true, { Owner = USSR })

	local ussrGroundAttackers = USSR.GetGroundAttackers()

	Utils.Do(ussrGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsUSSRGroundHunterUnit)
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Utils.Do({ ShoreInf1, ShoreInf2, ShoreInf3, ShoreInf4, ShoreHeavyTank1, ShoreHeavyTank2 }, function(self)
			if not self.IsDead then
				self.AttackMove(Shore.Location)
				self.Hunt()
			end
		end)
	end)

	Trigger.AfterDelay(IronCurtainEnabledDelay[Difficulty], function()
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = USSR })
	end)

	Trigger.AfterDelay(ParabombsEnabledDelay[Difficulty], function()
		if not MainAirfield.IsDead then
			MainAirfield.GrantCondition("parabombs-enabled")
		end
	end)

	Trigger.AfterDelay(ParatroopersEnabledDelay[Difficulty], function()
		if not SovietRadar.IsDead then
			SovietRadar.GrantCondition("paratroopers-enabled")
		end
	end)
end

DevastatorReinforcements = function()
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Notification("Reinforcements have arrived.")
		Media.PlaySpeechNotification(Scrin, "ReinforcementsArrived")
		Reinforcements.Reinforce(Scrin, { "deva" }, { ScrinReinforce1Spawn.Location, ScrinReinforce1Dest.Location }, 75)
		Reinforcements.Reinforce(Scrin, { "deva" }, { ScrinReinforce2Spawn.Location, ScrinReinforce2Dest.Location }, 75)
	end)
end
