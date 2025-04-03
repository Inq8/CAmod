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
	normal = 5,
	hard = 3
}

ParabombsEnabledDelay = {
	easy = DateTime.Minutes(5),
	normal = DateTime.Minutes(4),
	hard = DateTime.Minutes(3)
}

ParatroopersEnabledDelay = {
	easy = DateTime.Minutes(4),
	normal = DateTime.Minutes(3),
	hard = DateTime.Minutes(2)
}

AdjustedSovietCompositions = AdjustCompositionsForDifficulty(UnitCompositions.Soviet)

Squads = {
	East = {
		Delay = {
			easy = DateTime.Minutes(8),
			normal = DateTime.Minutes(6),
			hard = DateTime.Minutes(4)
		},
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 25 },
			normal = { Min = 25, Max = 50 },
			hard = { Min = 40, Max = 80 },
		},
		FollowLeader = true,
		ProducerActors = { Infantry = { MainBarracks }, Vehicles = { MainFactory } },
		Units = AdjustedSovietCompositions,
		AttackPaths = EastAttackPaths,
	},
	West = {
		Delay = {
			easy = DateTime.Minutes(5),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(3)
		},
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 25 },
			normal = { Min = 25, Max = 50 },
			hard = { Min = 40, Max = 80 },
		},
		FollowLeader = true,
		ProducerActors = { Infantry = { Barracks1, Barracks2 }, Vehicles = { Factory1, Factory2, Factory3, Factory4, Factory5 } },
		Units = AdjustedSovietCompositions,
		AttackPaths = WestAttackPaths,
	},
	AirMain = {
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
		ActiveCondition = function()
			return not IslandAirfieldsEliminated
		end,
		ProducerTypes = { Aircraft = { "afld" } },
		Units = {
			easy = {
				{ Aircraft = { "mig" } },
				{ Aircraft = { "hind" } },
			},
			normal = {
				{ Aircraft = { "mig", "mig" } },
				{ Aircraft = { "hind", "hind" } },
			},
			hard = {
				{ Aircraft = { "mig", "mig", "mig" } },
				{ Aircraft = { "mig", "hind", "hind" } },
			}
		},
	},
	AirFleetKillers = {
		Interval = {
			normal = DateTime.Seconds(10),
			hard = DateTime.Seconds(10)
		},
		ActiveCondition = function()
			local scrinFleet = Scrin.GetActorsByTypes({ "pac", "deva" })
			return #scrinFleet > AirFleetKillersThreshold[Difficulty]
		end,
		ProducerTypes = { Aircraft = { "afld" } },
		Units = {
			normal = {
				{ Aircraft = { "mig", "mig", "mig" } }
			},
			hard = {
				{ Aircraft = { "mig", "mig", "mig", "mig" } }
			}
		},
	},
}

WorldLoaded = function()
	Scrin = Player.GetPlayer("Scrin")
	USSR = Player.GetPlayer("USSR")
	USSRUnmanned = Player.GetPlayer("USSRUnmanned")
	MissionPlayers = { Scrin }
	IslandAirfieldsEliminated = false
	IslandSAMsDestroyed = false
	DefensesOffline = false

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Scrin)
	AdjustPlayerStartingCashForDifficulty()
	InitUSSR()

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
		MediaCA.PlaySound("s_reaperaccess.aud", 2)
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
			MediaCA.PlaySound("s_sovietpoweroffline.aud", 2)
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
		MediaCA.PlaySound("s_scrinfleet.aud", 2)

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
		if a.Owner == Scrin and a.Type ~= "camera" then
			Trigger.RemoveProximityTrigger(id)

			Notification("The entrance to the Soviet equipment holding area has been located.")
			MediaCA.PlaySound("s_sovietholdingarea.aud", 2)

			if not DefensesOffline then
				Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(5)), function()
					Notification("Substantial defenses detected. Recommened neutralizing power before beginning assault.")
					MediaCA.PlaySound("s_neutralizepower.aud", 2)
				end)
			end

			Beacon.New(Scrin, TankYardReveal.CenterPosition)
			local camera = Actor.Create("camera", true, { Owner = Scrin, Location = TankYardReveal.Location })
			Trigger.AfterDelay(DateTime.Seconds(4), function()
				camera.Destroy()
			end)
		end
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		USSR.Resources = USSR.ResourceCapacity - 500

		if not PlayerHasBuildings(USSR) then
			if not Scrin.IsObjectiveCompleted(ObjectiveDestroyBases) then
				Scrin.MarkCompletedObjective(ObjectiveDestroyBases)
			end
		end

		if Scrin.HasNoRequiredUnits() then
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

InitUSSR = function()
	RebuildExcludes.USSR = { Types = { "npwr", "tpwr", "tsla" }, Actors = { IslandAirfield1, IslandAirfield2, IslandAirfield3, IslandAirfield4, IslandAirfield5 } }

	AutoRepairAndRebuildBuildings(USSR, 15)
	SetupRefAndSilosCaptureCredits(USSR)
	AutoReplaceHarvesters(USSR)
	InitAiUpgrades(USSR)

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

	Trigger.AfterDelay(Squads.West.Delay[Difficulty], function()
		InitAttackSquad(Squads.West, USSR)
	end)

	Trigger.AfterDelay(Squads.East.Delay[Difficulty], function()
		InitAttackSquad(Squads.East, USSR)
	end)

	Trigger.AfterDelay(Squads.AirMain.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.AirMain, USSR, Scrin, { "harv.scrin", "scol", "proc.scrin", "ptur", "shar", "stmr", "enrv", "tpod" })
	end)

	if Difficulty ~= "easy" then
		InitAirAttackSquad(Squads.AirFleetKillers, USSR, Scrin, { "pac", "deva", "stmr", "enrv" })
	end

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
