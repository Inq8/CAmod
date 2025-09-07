MarineskoHardAndAboveCompositions = {
	-- 0 to 10 minutes
	{ Infantry = { "e3", "e1", "e1", "e1", "e1", "e1", "e2", "e3", "e4", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "e1" }, Vehicles = { "btr.ai", "btr.ai" }, MaxTime = DateTime.Minutes(10), },

	-- 10 to 16 minutes
	{ Infantry = { "e3", "e1", "e1", "e1", "e1", "e1", "e2", "e3", "e4", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "e1", "cmsr", "shok", "shok", "e1", "e1", "e1" }, Vehicles = { "3tnk", "btr.ai", "btr.ai", "btr.ai" }, MinTime = DateTime.Minutes(10), MaxTime = DateTime.Minutes(16), },

	-- 16 minutes onwards
	{ Infantry = { "e3", "e1", "e1", "e1", "e1", "e1", "e2", "e3", "e4", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "e1", "cmsr", "shok", "shok", "ttrp", "ttrp", "e1", "e1", "e1", "e1", "e1" }, Vehicles = { SovietMammothVariant, "btr.ai", "btr.ai", "btr.ai", "btr.ai" }, MinTime = DateTime.Minutes(16), },
}

MarineskoCompositions = {
	easy = {
		-- 0 to 14 minutes
		{ Infantry = { "e3", "e1", "e1", "e1", "e2", "e4", "e1", "e1",  }, Vehicles = { "btr" }, MaxTime = DateTime.Minutes(14), },

		-- 14 minutes onwards
		{ Infantry = { "e3", "e1", "e1", "shok", "shok", "e1", "e2", "e3", "e4" }, Vehicles = { "3tnk", "btr.ai", "btr.ai" }, MinTime = DateTime.Minutes(14), }
	},
	normal = {
		-- 0 to 12 minutes
		{ Infantry = { "e3", "e1", "e1", "e1", "e1", "e1", "e2", "e3", "e4", "e1", "e1", "e1" }, Vehicles = { "btr.ai", "btr" }, MaxTime = DateTime.Minutes(12), },

		-- 12 to 16 minutes
		{ Infantry = { "e3", "e1", "e1", "e1", "e1", "e1", "e2", "e3", "e4", "e1", "e1", "e1", "e3", "cmsr", "shok", "shok", "e1", "e1" }, Vehicles = { "btr.ai", "btr.ai" }, MinTime = DateTime.Minutes(12), MaxTime = DateTime.Minutes(16), },

		-- 16 minutes onwards
		{ Infantry = { "e3", "e1", "e1", "e1", "e1", "e1", "e2", "e3", "e4", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "e1", "cmsr", "shok", "ttrp", "e1", "e1", "e1" }, Vehicles = { "3tnk", "btr.ai", "btr.ai", "btr.ai"  }, MinTime = DateTime.Minutes(16), },
	},
	hard = MarineskoHardAndAboveCompositions,
	vhard = MarineskoHardAndAboveCompositions,
	brutal = MarineskoHardAndAboveCompositions
}

RomanovHardAndAboveCompositions = {
	-- 0 to 10 minutes
	{ Infantry = { }, Vehicles = { "3tnk.rhino", "btr.ai", "3tnk.rhino", "btr.ai" }, MaxTime = DateTime.Minutes(10), },

	-- 10 to 16 minutes
	{ Infantry = { }, Vehicles = { SovietMammothVariant, "btr.ai", SovietMammothVariant, "3tnk.rhino", "btr.ai", "3tnk.rhino" }, MinTime = DateTime.Minutes(10), MaxTime = DateTime.Minutes(16) },

	-- 16 minutes onwards
	{ Infantry = { }, Vehicles = { "apoc", SovietMammothVariant, SovietMammothVariant, SovietMammothVariant, "3tnk.rhino", "btr.ai", "btr.ai" }, MinTime = DateTime.Minutes(16) },
}

RomanovCompositions = {
	easy = {
		-- 0 to 14 minutes
		{ Infantry = { }, Vehicles = { "3tnk.rhino", "3tnk.rhino" }, MaxTime = DateTime.Minutes(14), },

		-- 14 minutes onwards
		{ Infantry = { }, Vehicles = { SovietMammothVariant, "3tnk.rhino", "3tnk.rhino" }, MinTime = DateTime.Minutes(14), }
	},
	normal = {
		-- 0 to 12 minutes
		{ Infantry = { }, Vehicles = { "3tnk.rhino", "3tnk.rhino" }, MaxTime = DateTime.Minutes(12), },

		-- 12 to 16 minutes
		{ Infantry = { }, Vehicles = { SovietMammothVariant, "3tnk.rhino", "3tnk.rhino" }, MinTime = DateTime.Minutes(12), MaxTime = DateTime.Minutes(16) },

		-- 15 minutes onwards
		{ Infantry = { }, Vehicles = { SovietMammothVariant, SovietMammothVariant, "3tnk.rhino", "3tnk.rhino"  }, MinTime = DateTime.Minutes(16) },
	},
	hard = RomanovHardAndAboveCompositions,
	vhard = RomanovHardAndAboveCompositions,
	brutal = RomanovHardAndAboveCompositions
}

KrukovHardAndAboveCompositions = {
	-- 0 to 10 minutes
	{ Infantry = { "e3", "e1", "e1", "e1", "e1", "e2", "e4" }, Vehicles = { "grad", "grad" }, MaxTime = DateTime.Minutes(10), },

	-- 10 to 16 minutes
	{ Infantry = { "e3", "e1", "e1", "shok", "shok", "e1", "e2", "e3", "e4", "e1", "e1" }, Vehicles = { SovietMammothVariant, "grad", "grad", SovietAdvancedArty }, MinTime = DateTime.Minutes(10), MaxTime = DateTime.Minutes(16), },

	-- 16 minutes onwards
	{ Infantry = { "e3", "e1", "e1", "shok", "shok", "e1", "e2", "e3", "e4", "e1", "e1" }, Vehicles = { SovietMammothVariant, "grad", "grad", SovietAdvancedArty, SovietAdvancedArty, SovietAdvancedArty }, MinTime = DateTime.Minutes(16), },
}

KrukovCompositions = {
	easy = {
		-- 0 to 14 minutes
		{ Infantry = { "e3", "e1", "e1", "e1", "e2", "e4" }, Vehicles = { "katy", "katy" }, MaxTime = DateTime.Minutes(14), },

		-- 14 minutes onwards
		{ Infantry = { "e3", "e1", "e1", "shok", "shok", "e1", "e2", "e3", "e4" }, Vehicles = { "grad", "grad", "v2rl" }, MinTime = DateTime.Minutes(14), }
	},
	normal = {
		-- 0 to 12 minutes
		{ Infantry = { "e3", "e1", "e1", "e1", "e1", "e2", "e4" }, Vehicles = { "katy", "katy" }, MaxTime = DateTime.Minutes(12), },

		-- 12 to 16 minutes
		{ Infantry = { "e3", "e1", "e1", "shok", "shok", "e1", "e2", "e3", "e4" }, Vehicles = { "grad", "grad", "v2rl" }, MinTime = DateTime.Minutes(12), MaxTime = DateTime.Minutes(16), },

		-- 15 minutes onwards
		{ Infantry = { "e3", "e1", "e1", "shok", "shok", "e1", "e2", "e3", "e4", "e1", "e1" }, Vehicles = { "grad", "grad", "v2rl", SovietAdvancedArty }, MinTime = DateTime.Minutes(16), },
	},
	hard = KrukovHardAndAboveCompositions,
	vhard = KrukovHardAndAboveCompositions,
	brutal = KrukovHardAndAboveCompositions
}

MainAttackValues = {
	easy = { Min = 5, Max = 15 },
	normal = { Min = 15, Max = 33 },
	hard = { Min = 25, Max = 55 },
	vhard = { Min = 25, Max = 55 },
	brutal = { Min = 25, Max = 55 }
}

SecondaryAttackValues = {
	easy = { Min = 2, Max = 7 },
	normal = { Min = 8, Max = 16 },
	hard = { Min = 12, Max = 28 },
	vhard = { Min = 12, Max = 28 },
	brutal = { Min = 12, Max = 28 }
}

Squads = {
	MarineskoMain = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(5)),
		AttackValuePerSecond = MainAttackValues,
		FollowLeader = true,
		Compositions = MarineskoCompositions,
		AttackPaths = {
			{ MarineskoRally1.Location },
			{ MarineskoRally2.Location },
		},
	},
	MarineskoVsRomanov = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(6)),
		AttackValuePerSecond = SecondaryAttackValues,
		FollowLeader = true,
		Compositions = MarineskoCompositions,
		AttackPaths = {
			{ Middle.Location, RomanovBase.Location },
		},
	},
	MarineskoVsKrukov = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(6)),
		AttackValuePerSecond = SecondaryAttackValues,
		FollowLeader = true,
		Compositions = MarineskoCompositions,
		AttackPaths = {
			{ MarineskoRally4.Location, KrukovBase.Location },
		},
	},
	RomanovMain = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(5)),
		AttackValuePerSecond = MainAttackValues,
		FollowLeader = true,
		Compositions = RomanovCompositions,
		AttackPaths = {
			{ RomanovRally1.Location },
			{ RomanovRally2.Location },
		},
	},
	RomanovVsMarinesko = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(6)),
		AttackValuePerSecond = SecondaryAttackValues,
		FollowLeader = true,
		Compositions = RomanovCompositions,
		AttackPaths = {
			{ Middle.Location, MarineskoBase.Location },
		},
	},
	RomanovVsKrukov = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(6)),
		AttackValuePerSecond = SecondaryAttackValues,
		FollowLeader = true,
		Compositions = RomanovCompositions,
		AttackPaths = {
			{ RomanovRally4.Location, KrukovBase.Location },
		},
	},
	KrukovMain = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(6)),
		AttackValuePerSecond = MainAttackValues,
		FollowLeader = true,
		Compositions = KrukovCompositions,
		AttackPaths = {
			{ KrukovRally1.Location },
		},
	},
	KrukovVsMarinesko = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(6)),
		AttackValuePerSecond = SecondaryAttackValues,
		FollowLeader = true,
		Compositions = KrukovCompositions,
		AttackPaths = {
			{ KrukovRally2.Location, MarineskoBase.Location },
		},
	},
	KrukovVsRomanov = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(6)),
		AttackValuePerSecond = SecondaryAttackValues,
		FollowLeader = true,
		Compositions = KrukovCompositions,
		AttackPaths = {
			{ KrukovRally3.Location, RomanovBase.Location },
		},
	},
	KrukovAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Soviet,
	},
	KrukovAntiTankAir = AntiHeavyAirSquad({ "suk" }, AdjustAirDelayForDifficulty(DateTime.Minutes(10))),
	KrukovAirToAir = AntiHeavyAirSquad({ "mig", "yak" }, AdjustAirDelayForDifficulty(DateTime.Minutes(10))),
}

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Marinesko = Player.GetPlayer("Marinesko")
	Romanov = Player.GetPlayer("Romanov")
	Krukov = Player.GetPlayer("Krukov")
	MarineskoUnited = Player.GetPlayer("MarineskoUnited")
	RomanovUnited = Player.GetPlayer("RomanovUnited")
	KrukovUnited = Player.GetPlayer("KrukovUnited")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { USSR }
	TimerTicks = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(USSR)
	AdjustPlayerStartingCashForDifficulty()
	InitGenerals()

	ObjectiveEliminateMarinesko = USSR.AddObjective("Defeat General Marinesko's forces.")
	ObjectiveEliminateRomanov = USSR.AddObjective("Defeat Deputy Chairman Romanov's forces.")
	ObjectiveEliminateKrukov = USSR.AddObjective("Defeat General Krukov's forces.")

	Trigger.OnCapture(RomanovIndustrialPlant, function(self, captor, oldOwner, newOwner)
		Actor.Create("captured.indp", true, { Owner = USSR })
	end)

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("Romanov. Marinesko. Krukov. Comrade General, you must crush these pretenders. The Union must prevail!", "Premier Cherdenko", HSLColor.FromHex("FF0000"))
		MediaCA.PlaySound("cdko_crushtraitors.aud", 2)
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	OncePerThirtySecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Marinesko.Resources = Marinesko.ResourceCapacity - 500
		Romanov.Resources = Romanov.ResourceCapacity - 500
		Krukov.Resources = Krukov.ResourceCapacity - 500
		MarineskoUnited.Resources = MarineskoUnited.ResourceCapacity - 500
		RomanovUnited.Resources = RomanovUnited.ResourceCapacity - 500
		KrukovUnited.Resources = KrukovUnited.ResourceCapacity - 500

		if MissionPlayersHaveNoRequiredUnits() then
			if not USSR.IsObjectiveCompleted(ObjectiveEliminateMarinesko) then
				USSR.MarkFailedObjective(ObjectiveEliminateMarinesko)
			end
			if not USSR.IsObjectiveCompleted(ObjectiveEliminateRomanov) then
				USSR.MarkFailedObjective(ObjectiveEliminateRomanov)
			end
			if not USSR.IsObjectiveCompleted(ObjectiveEliminateKrukov) then
				USSR.MarkFailedObjective(ObjectiveEliminateKrukov)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()

		if not USSR.IsObjectiveCompleted(ObjectiveEliminateMarinesko) and not PlayerHasBuildings(Marinesko) and not PlayerHasBuildings(MarineskoUnited) then
			USSR.MarkCompletedObjective(ObjectiveEliminateMarinesko)
		end

		if not USSR.IsObjectiveCompleted(ObjectiveEliminateRomanov) and not PlayerHasBuildings(Romanov) and not PlayerHasBuildings(RomanovUnited) then
			USSR.MarkCompletedObjective(ObjectiveEliminateRomanov)
		end

		if not USSR.IsObjectiveCompleted(ObjectiveEliminateKrukov) and not PlayerHasBuildings(Krukov) and not PlayerHasBuildings(KrukovUnited) then
			USSR.MarkCompletedObjective(ObjectiveEliminateKrukov)
		end
	end
end

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % DateTime.Seconds(30) == 0 then
		CalculatePlayerCharacteristics()
		Unification()
	end
end

InitGenerals = function()
	Generals = { Marinesko, Romanov, Krukov }

	Utils.Do(Generals, function(g)
		AutoRepairAndRebuildBuildings(g)
		SetupRefAndSilosCaptureCredits(g)
		AutoReplaceHarvesters(g)
		AutoRebuildConyards(g)
		InitAiUpgrades(g)

		local groundAttackers = g.GetGroundAttackers()

		Utils.Do(groundAttackers, function(a)
			TargetSwapChance(a, 10)
			CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsUSSRGroundHunterUnit)
		end)
	end)

	InitAttackSquad(Squads.MarineskoMain, Marinesko)
	InitAttackSquad(Squads.MarineskoVsRomanov, Marinesko, Romanov)
	InitAttackSquad(Squads.MarineskoVsKrukov, Marinesko, Krukov)

	InitAttackSquad(Squads.RomanovMain, Romanov)
	InitAttackSquad(Squads.RomanovVsMarinesko, Romanov, Marinesko)
	InitAttackSquad(Squads.RomanovVsKrukov, Romanov, Krukov)

	InitAttackSquad(Squads.KrukovMain, Krukov)
	InitAttackSquad(Squads.KrukovVsMarinesko, Krukov, Marinesko)
	InitAttackSquad(Squads.KrukovVsRomanov, Krukov, Romanov)
	InitAirAttackSquad(Squads.KrukovAir, Krukov)

	if IsHardOrAbove() then
		Trigger.AfterDelay(DateTime.Minutes(15), function()
			Actor.Create("imppara.upgrade", true, { Owner = Marinesko })
			Actor.Create("rocketpods.upgrade", true, { Owner = Krukov })
			Actor.Create("reactive.upgrade", true, { Owner = Romanov })
		end)

		InitAirAttackSquad(Squads.KrukovAirToAir, Krukov, MissionPlayers, { "Aircraft" }, "ArmorType")
		InitAirAttackSquad(Squads.KrukovAntiTankAir, Krukov, MissionPlayers, { "4tnk", "4tnk.atomic", "apoc", "apoc.atomic" })
	end
end

Unification = function()
	if UnificationComplete then
		return
	end

	for _, g in pairs(Generals) do
		local units = Utils.Where(g.GetActors(), function(a)
			return a.HasProperty("Attack")
		end)

		local unitValue = 0
		for _, u in pairs(units) do
			if UnitCosts[u.Type] == nil then
				UnitCosts[u.Type] = ActorCA.CostOrDefault(u.Type)
			end
			unitValue = unitValue + UnitCosts[u.Type]
		end

		if unitValue < 20000 then
			SubduedGeneral = g
			break
		end
	end

	if not SubduedGeneral then
		return
	end

	UnificationComplete = true

	local unifiedGeneralMap = {
		{ From = Marinesko, To = MarineskoUnited },
		{ From = Romanov, To = RomanovUnited },
		{ From = Krukov, To = KrukovUnited }
	}

	-- unify
	for _, m in pairs(unifiedGeneralMap) do
		for _, a in pairs(m.From.GetActors()) do
			if a.Type ~= "player" then
				a.Owner = m.To
			end
		end

		BuildingQueues[m.To.InternalName]= { }

		if BuildingQueues[m.From.InternalName] ~= nil then
			for _, queueItem in pairs (BuildingQueues[m.From.InternalName]) do
				local copiedQueueItem = {
					Actor = queueItem.Actor,
					Player = m.To,
					Location = queueItem.Location,
					CenterPosition = queueItem.CenterPosition,
					AttemptsRemaining = queueItem.AttemptsRemaining,
					MaxAttempts = queueItem.MaxAttemps
				}

				table.insert(BuildingQueues[m.To.InternalName], copiedQueueItem)
			end

			BuildingQueues[m.From.InternalName] = { }
		end

		-- copy squads
		for _, squad in pairs(Squads) do
			if squad.Player == m.From then
				local copiedSquad = {
					Name = squad.Name .. "United",
					Player = m.To,
					TargetPlayer = USSR,
					InitTime = squad.InitTime,
					AttackValuePerSecond = squad.AttackValuePerSecond,
					FollowLeader = squad.FollowLeader,
					Compositions = squad.Compositions,
					AttackPaths = squad.AttackPaths,
				}

				if squad.IsAirSquad then
					copiedSquad.IsAirSquad = true
					copiedSquad.AirTargetList = squad.AirTargetList
					copiedSquad.AirTargetType = squad.AirTargetType
				end

				Squads[copiedSquad.Name] = copiedSquad
				InitAttackSquad(Squads[copiedSquad.Name], m.To)

				-- disble old squad
				squad.ActiveCondition = function()
					return false
				end
			end
		end

		Trigger.AfterDelay(1, function()
			AutoRepairAndRebuildBuildings(m.To)
			AutoReplaceHarvesters(m.To)
			AutoRebuildConyards(m.To)
			RebuildNextBuilding(m.To)
		end)
	end
end
