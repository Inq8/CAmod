MissionDir = "ca|missions/main-campaign/ca53-defiance"

NorthAttackPaths = {
	{ NorthWaypoint1.Location, NorthWaypoint2.Location },
	{ NorthWaypoint1.Location, NorthWaypoint3.Location }
}

EastAttackPaths = {
	{ EastWaypoint1.Location, EastWaypoint2.Location },
	{ EastWaypoint1.Location, EastWaypoint3.Location },
	{ EastWaypoint5.Location, EastWaypoint3.Location }
}

WestAttackPaths = {
	{ WestWaypoint1.Location, WestWaypoint2.Location },
	{ WestWaypoint1.Location, WestWaypoint3.Location }
}

NodAttackPaths = {
	{ NodWaypoint1.Location, NodWaypoint2.Location },
	{ NodWaypoint1.Location, NodWaypoint3.Location }
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

SupportPowersEnabledDelay = {
	easy = DateTime.Minutes(6),
	normal = DateTime.Minutes(5),
	hard = DateTime.Minutes(4),
	vhard = DateTime.Minutes(3),
	brutal = DateTime.Minutes(2)
}

IronCurtainEnabledDelay = {
	easy = DateTime.Minutes(25),
	normal = DateTime.Minutes(15),
	hard = DateTime.Minutes(8),
	vhard = DateTime.Minutes(5),
	brutal = DateTime.Minutes(5)
}

DominatorStartTime = {
	easy = DateTime.Minutes(13),
	normal = DateTime.Minutes(11),
	hard = DateTime.Minutes(9),
	vhard = DateTime.Minutes(7),
	brutal = DateTime.Minutes(5)
}

DominatorInterval = {
	easy = DateTime.Minutes(7),
	normal = DateTime.Minutes(6),
	hard = DateTime.Minutes(5),
	vhard = DateTime.Minutes(4),
	brutal = DateTime.Minutes(3) + DateTime.Seconds(20)
}

DominatorRevealDelay = {
	easy = DateTime.Seconds(5),
	normal = DateTime.Seconds(5),
	hard = DateTime.Seconds(30),
	vhard = DateTime.Seconds(999),
	brutal = DateTime.Seconds(999)
}

UnitCompositions.Soviet = Utils.Concat(UnitCompositions.Soviet, {
	{
		Infantry = { "brut", "brut", "brut", "brut", "brut", "brut", "brut", "brut", "brut" },
		Vehicles = { "btr.yuri.ai", "btr.yuri.ai", "3tnk.yuri", "3tnk.yuri", "3tnk.yuri", "3tnk.yuri", "3tnk.yuri", "v2rl" },
		MinTime = DateTime.Minutes(10),
	},
	{
		Infantry = { },
		Vehicles = { "cdrn", "cdrn", "cdrn", "cdrn" },
		MinTime = DateTime.Minutes(17),
		IsSpecial = true
	},
	{
		Infantry = { "brut", "brut", "brut", "yuri", "e1", "e1", "e1", "e1", "e1", "e1", "e1", "e3", "e3", "e3" },
		Vehicles = { "3tnk.yuri", "3tnk.yuri", "3tnk.yuri", "btr.yuri.ai", "btr.yuri.ai" },
		MinTime = DateTime.Minutes(16),
		IsSpecial = true,
	}
})

AdjustedSovietCompositions = AdjustCompositionsForDifficulty(UnitCompositions.Soviet)
AdjustedNodCompositions = AdjustCompositionsForDifficulty(UnitCompositions.Nod)

RecalculateSquad = function(squad)
	local activeBases = {}

	local northProducers = Map.ActorsInBox(NorthProdTopLeft.CenterPosition, NorthProdBottomRight.CenterPosition, function(a)
		return a.Owner == USSR and (a.Type == "weap" or a.Type == "barr")
	end)

	local eastProducers = Map.ActorsInBox(EastProdTopLeft.CenterPosition, EastProdBottomRight.CenterPosition, function(a)
		return a.Owner == USSR and (a.Type == "weap" or a.Type == "barr")
	end)

	local westProducers = Map.ActorsInBox(WestProdTopLeft.CenterPosition, WestProdBottomRight.CenterPosition, function(a)
		return a.Owner == USSR and (a.Type == "weap" or a.Type == "barr")
	end)

	if #northProducers > 0 then
		table.insert(activeBases, "North")
	end
	if #eastProducers > 0 then
		table.insert(activeBases, "East")
	end
	if #westProducers > 0 then
		table.insert(activeBases, "West")
	end

	if #activeBases > 0 then
		local selectedBase = Utils.Random(activeBases)

		if selectedBase == "North" then
			squad.ProducerActors = { Infantry = { SovietNorthBarracks1, SovietNorthBarracks2 }, Vehicles = { SovietNorthFactory1, SovietNorthFactory2 } }
			squad.AttackPaths = NorthAttackPaths
		elseif selectedBase == "East" then
			squad.ProducerActors = { Infantry = { SovietEastBarracks1, SovietEastBarracks2 }, Vehicles = { SovietEastFactory1, SovietEastFactory2 } }
			squad.AttackPaths = EastAttackPaths
		elseif selectedBase == "West" then
			squad.ProducerActors = { Infantry = { SovietWestBarracks1 }, Vehicles = { SovietWestFactory1 } }
			squad.AttackPaths = WestAttackPaths
		end
	else
		squad.ProducerActors = nil
		squad.AttackPaths = nil
	end
end

Squads = {
	Yuri = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(4)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 30, Max = 60 }),
		FollowLeader = true,
		ProducerActors = { Infantry = { SovietNorthBarracks1, SovietNorthBarracks2 }, Vehicles = { SovietNorthFactory1, SovietNorthFactory2 } },
		Compositions = AdjustedSovietCompositions,
		AttackPaths = NorthAttackPaths,
		AfterSendSquad = RecalculateSquad
	},
	Nod = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(4)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 10, Max = 20 }),
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		Compositions = AdjustedNodCompositions,
		AttackPaths = NodAttackPaths,
	},
	AirMain = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Soviet,
	},
	AirFleetKillers = {
		ActiveCondition = function(squad)
			local scrinFleet = GetMissionPlayersActorsByTypes({ "pac", "deva" })
			return #scrinFleet > AirFleetKillersThreshold[Difficulty]
		end,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 30, Max = 30 }),
		Compositions = function(squad)
			local migs = { "mig" }
			local numFleetShips = #GetMissionPlayersActorsByTypes({ "pac", "deva" })
			for i = 1, math.min(numFleetShips, MaxFleetKillers[Difficulty]) do
				table.insert(migs, "mig")
			end
			return { { Aircraft = migs } }
		end
	},
	TripodKillers = {
		ActiveCondition = function(squad)
			local tripods = GetMissionPlayersActorsByTypes({ "tpod", "rtpd" })
			return #tripods > TripodKillersThreshold[Difficulty]
		end,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 30, Max = 30 }),
		Compositions = function(squad)
			local sukhois = { "suk" }
			local numTripods = #GetMissionPlayersActorsByTypes({ "tpod", "rtpd" })
			for i = 1, math.min(numTripods, MaxTripodKillers[Difficulty]) do
				table.insert(sukhois, "suk")
			end
			return { { Aircraft = sukhois } }
		end
	},
	Discs = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 7, Max = 20 }),
		Compositions = {
			easy = {
				{ Aircraft = { "disc" } },
			},
			normal = {
				{ Aircraft = { "disc" }, MaxTime = DateTime.Minutes(20) },
				{ Aircraft = { "disc", "disc" }, MinTime = DateTime.Minutes(20) },
			},
			hard = {
				{ Aircraft = { "disc" }, MaxTime = DateTime.Minutes(10) },
				{ Aircraft = { "disc", "disc" }, MinTime = DateTime.Minutes(10), MaxTime = DateTime.Minutes(20) },
				{ Aircraft = { "disc", "disc", "disc" }, MinTime = DateTime.Minutes(20), MaxTime = DateTime.Minutes(30) },
				{ Aircraft = { "disc", "disc", "disc", "disc" }, MinTime = DateTime.Minutes(30) }
			},
			vhard = {
				{ Aircraft = { "disc" }, MaxTime = DateTime.Minutes(8) },
				{ Aircraft = { "disc", "disc" }, MinTime = DateTime.Minutes(8), MaxTime = DateTime.Minutes(18) },
				{ Aircraft = { "disc", "disc", "disc" }, MinTime = DateTime.Minutes(18), MaxTime = DateTime.Minutes(28) },
				{ Aircraft = { "disc", "disc", "disc", "disc" }, MinTime = DateTime.Minutes(28) }
			},
			brutal = {
				{ Aircraft = { "disc", "disc" }, MaxTime = DateTime.Minutes(6) },
				{ Aircraft = { "disc", "disc", "disc" }, MinTime = DateTime.Minutes(6), MaxTime = DateTime.Minutes(16) },
				{ Aircraft = { "disc", "disc", "disc", "disc" }, MinTime = DateTime.Minutes(16), MaxTime = DateTime.Minutes(26) },
				{ Aircraft = { "disc", "disc", "disc", "disc", "disc", "disc" }, MinTime = DateTime.Minutes(26) }
			},
		}
	},
	Dominators = {
		Delay = DominatorStartTime,
		Interval = DominatorInterval,
		ProducerActors = { Vehicles = { SovietNorthFactory1, SovietNorthFactory2 } },
		Compositions = {
			{ Vehicles = { "domi" } }
		},
		AttackPaths = NorthAttackPaths,
		AfterSendSquad = RecalculateSquad
	}
}

SetupPlayers = function()
	ScrinRebels = Player.GetPlayer("ScrinRebels")
	USSR = Player.GetPlayer("USSR")
    Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { ScrinRebels }
	MissionEnemies = { USSR, Nod }

	Actor.Create("rebel.allegiance", true, { Owner = ScrinRebels })
end

WorldLoaded = function()
	SetupPlayers()

    Camera.Position = PlayerStart.CenterPosition

	InitObjectives(ScrinRebels)
	AdjustPlayerStartingCashForDifficulty()
	RemoveActorsBasedOnDifficultyTags()
	InitUSSR()
	InitNod()

    ObjectiveDestroySoviets = ScrinRebels.AddObjective("Destroy all Soviet forces.")
	ObjectiveDestroyNoNodStructures = ScrinRebels.AddSecondaryObjective("Do not destroy any Nod structures.")

	local nodStructures = Utils.Where(Nod.GetActors(), function(a) return a.HasProperty("StartBuildingRepairs") end)
	Trigger.OnAnyKilled(nodStructures, function(self)
		NodStructureDestroyed = true
	end)

	Trigger.OnAnyProduction(function(producer, produced, productionType)
		if produced.Owner == USSR and produced.Type == "domi" then
			MediaCA.PlaySound(MissionDir .. "/domispawn.aud", 2)
			Trigger.AfterDelay(DateTime.Seconds(1), function()
				Notification("Warning, powerful psionic signature detected.")
				MediaCA.PlaySound(MissionDir .. "/s_psionic.aud", 2)
				if IsHardOrBelow() then
					Trigger.AfterDelay(DominatorRevealDelay[Difficulty], function()
						if not produced.IsDead then
							Beacon.New(ScrinRebels, produced.CenterPosition)
							Media.PlaySound("beacon.aud")
							produced.GrantCondition("domi-reveal")
						end
					end)
				end
			end)
			if not YuriDispleased then
				YuriDispleased = true
				Trigger.OnKilled(produced, function(self, killer)
					Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(3)), function()
						Media.DisplayMessage("Your defiance is most displeasing.", "Yuri", HSLColor.FromHex("FF00BB"))
						MediaCA.PlaySound(MissionDir .. "/yuri_defiance.aud", 2)
					end)
				end)
			end
		end
	end)

	Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(4)), function()
		Media.DisplayMessage("Ah, new test subjects. The Scrin will make excellent slaves.", "Yuri", HSLColor.FromHex("FF00BB"))
		MediaCA.PlaySound(MissionDir .. "/yuri_testsubjects.aud", 2)
	end)

	if IsVeryHardOrAbove() then
		local yuriProductionBuildings = USSR.GetActorsByTypes({ "fact", "weap", "barr", "afld" })
		for _, b in pairs(yuriProductionBuildings) do
			BuildDefenseOnCaptureAttempt(b, "ftur", true)
		end

		local nodConyards = Nod.GetActorsByType("afac")
		for _, c in pairs(nodConyards) do
			BuildDefenseOnCaptureAttempt(c, "ltur", false)
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
        USSR.Resources = USSR.ResourceCapacity - 500

		if not PlayerHasBuildings(USSR) then
			ScrinRebels.MarkCompletedObjective(ObjectiveDestroySoviets)
			if not NodStructureDestroyed then
				ScrinRebels.MarkCompletedObjective(ObjectiveDestroyNoNodStructures)
			end
		end

		if MissionPlayersHaveNoRequiredUnits() then
			ScrinRebels.MarkFailedObjective(ObjectiveDestroySoviets)
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

InitUSSR = function()
	AutoRepairAndRebuildBuildings(USSR, 15)
	SetupRefAndSilosCaptureCredits(USSR)
	AutoReplaceHarvesters(USSR)
	AutoRebuildConyards(USSR)
	InitAiUpgrades(USSR)
	InitAttackSquad(Squads.Yuri, USSR)
	InitAirAttackSquad(Squads.AirMain, USSR)
	InitAttackSquad(Squads.Discs, USSR)
	InitAttackSquad(Squads.Dominators, USSR)
	SetupUnitDefenders(USSR)

	if IsVeryHardOrAbove() then
		SellOnCaptureAttempt({ NorthConyard, WestConyard, EastConyard })

		if Difficulty == "brutal" then
			Trigger.AfterDelay(DateTime.Minutes(20), function()
				CompositionValueMultipliers.brutal = 1.4
			end)
		end
	end

	if Difficulty ~= "easy" then
		InitAirAttackSquad(Squads.AirFleetKillers, USSR, MissionPlayers, { "pac", "deva" })
	end

	if Difficulty ~= "easy" then
		InitAirAttackSquad(Squads.TripodKillers, USSR, MissionPlayers, { "tpod", "rtpd" })
	end

	Trigger.AfterDelay(IronCurtainEnabledDelay[Difficulty], function()
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = USSR })
	end)

	Trigger.AfterDelay(SupportPowersEnabledDelay[Difficulty], function()
		Actor.Create("ai.supportpowers.enabled", true, { Owner = USSR })
	end)
end

InitNod = function()
	AutoRepairAndRebuildBuildings(Nod, 15)
	SetupRefAndSilosCaptureCredits(Nod)
	AutoReplaceHarvesters(Nod)
	AutoRebuildConyards(Nod)
	InitAiUpgrades(Nod)
	SetupUnitDefenders(Nod)

	InitAttackSquad(Squads.Nod, Nod)
end
