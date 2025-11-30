MissionDir = "ca|missions/main-campaign/ca12-supremacy"

IonCannonEnabledTime = {
	easy = DateTime.Seconds((60 * 40) + 48),
	normal = DateTime.Seconds((60 * 25) + 48),
	hard = DateTime.Seconds((60 * 15) + 48),
	vhard = DateTime.Seconds((60 * 10) + 48),
	brutal = DateTime.Seconds((60 * 8) + 48)
}

-- overrides
RampDurationMultipliers.vhard = 0.82
RampDurationMultipliers.brutal = 0.76

AdjustedGDICompositions = AdjustCompositionsForDifficulty(UnitCompositions.GDI)

Squads = {
	Main = {
		InitTimeAdjustment = -DateTime.Minutes(3),
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(5)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40, RampDuration = DateTime.Minutes(15) }),
		FollowLeader = true,
		ProducerActors = { Infantry = { GDIBarracks1 }, Vehicles = { GDIFactory1 } },
		Compositions = AdjustedGDICompositions,
		AttackPaths = {
			-- set on init
		},
	},
	Forward = {
		InitTimeAdjustment = -DateTime.Minutes(2),
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(3)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40, RampDuration = DateTime.Minutes(15) }),
		FollowLeader = true,
		ProducerActors = { Infantry = { GDIBarracks2 }, Vehicles = { GDIFactory2 } },
		Compositions = AdjustedGDICompositions,
		AttackPaths = {
			-- set on init
		},
	},
	GDIAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.GDI,
	},
	AntiHeavyAir = AntiHeavyAirSquad({ "orcb" }, AdjustAirDelayForDifficulty(DateTime.Minutes(10))),
	AirToAir = AirToAirSquad({ "orca" }, AdjustAirDelayForDifficulty(DateTime.Minutes(10))),
}

-- Setup and Tick

SetupPlayers = function()
	Nod = Player.GetPlayer("Nod")
	Nod2 = Player.GetPlayer("Nod2")
	Nod3 = Player.GetPlayer("Nod3")
	GDI = Player.GetPlayer("GDI")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { Nod }
	MissionEnemies = { GDI }
end

WorldLoaded = function()
	SetupPlayers()

	TimerTicks = 0
	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Nod)
	AdjustPlayerStartingCashForDifficulty()
	RemoveActorsBasedOnDifficultyTags()
	InitGDI()
	InitNod()

	NodRadarProviders = {}

	Utils.Do(MissionPlayers, function(p)
		table.insert(NodRadarProviders, Actor.Create("radar.dummy", true, { Owner = p }))
	end)

	ObjectiveReinforce = Nod.AddObjective("Reinforce one of the two Nod bases.")

	local eastAttackTriggerCells = {}
	for x = 9, 18 do
		local cell = CPos.New(x, 78)
		table.insert(eastAttackTriggerCells, cell)
	end

	local westAttackTriggerCells = {}
	for x = 47, 56 do
		local cell = CPos.New(x, 101)
		table.insert(westAttackTriggerCells, cell)
	end

	Trigger.OnEnteredFootprint(eastAttackTriggerCells, function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveProximityTrigger(id)
			InitGDIEast()
		end
	end)

	Trigger.OnEnteredFootprint(westAttackTriggerCells, function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveProximityTrigger(id)
			InitGDIWest()
		end
	end)

	Trigger.OnEnteredProximityTrigger(WestBaseCenter.CenterPosition, WDist.New(15 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveProximityTrigger(id)
			FlipWestBase()
		end
	end)

	Trigger.OnEnteredProximityTrigger(EastBaseCenter.CenterPosition, WDist.New(15 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveProximityTrigger(id)
			FlipEastBase()
		end
	end)

	Banshee1.ReturnToBase(Helipad1)
	Banshee2.ReturnToBase(Helipad2)

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
		GDI.Resources = GDI.ResourceCapacity - 500

		if MissionPlayersHaveNoRequiredUnits() then
			if not Nod.IsObjectiveCompleted(ObjectiveReinforce) then
				Nod.MarkFailedObjective(ObjectiveReinforce)
			end

			if ObjectiveDestroyGDI ~= nil and not Nod.IsObjectiveCompleted(ObjectiveDestroyGDI) then
				Nod.MarkFailedObjective(ObjectiveDestroyGDI)
			end
		end

		if not Nod.IsObjectiveCompleted(ObjectiveReinforce) then
			if not PlayerHasBuildings(Nod2) and not PlayerHasBuildings(Nod3) then
				Nod.MarkFailedObjective(ObjectiveReinforce)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()

		if ObjectiveDestroyGDI ~= nil and not PlayerHasBuildings(GDI) then
			Nod.MarkCompletedObjective(ObjectiveDestroyGDI)
		end
	end
end

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % DateTime.Seconds(30) == 0 then
		CalculatePlayerCharacteristics()
	end
end

-- Functions

InitNod = function()
	local nod2Forces = Nod2.GetGroundAttackers()
	Utils.Do(nod2Forces, function(a)
		CallForHelpOnDamagedOrKilled(a, WDist.New(10240), IsGroundHunterUnit, function(p) return true end)
	end)
	local nod3Forces = Nod3.GetGroundAttackers()
	Utils.Do(nod3Forces, function(a)
		CallForHelpOnDamagedOrKilled(a, WDist.New(10240), IsGroundHunterUnit, function(p) return true end)
	end)
end

InitGDI = function()
	AutoRepairAndRebuildBuildings(GDI)
	SetupRefAndSilosCaptureCredits(GDI)
	AutoReplaceHarvesters(GDI)
	AutoRebuildConyards(GDI)

	if IsHardOrAbove() then
		InitAirAttackSquad(Squads.AntiHeavyAir, GDI, MissionPlayers, { "rmbc", "enli", "reap", "avtr" })
		InitAirAttackSquad(Squads.AirToAir, GDI, MissionPlayers, { "Aircraft" }, "ArmorType")
	end

	local gdiGroundAttackers = GDI.GetGroundAttackers()

	Utils.Do(gdiGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGDIGroundHunterUnit)
	end)

	Trigger.AfterDelay(DateTime.Minutes(8), function()
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = GDI })
	end)

	Trigger.AfterDelay(IonCannonEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = GDI })
	end)
end

InitGDIWest = function()
	if GDIEastInitialized or GDIWestInitialized then
		return
	end

	GDIWestInitialized = true

	InitWestAttackers()

	-- main attacks will head to east base

	Squads.Main.AttackPaths = {
		{ EastRally1.Location },
		{ EastRally2.Location },
		{ EastRally3.Location },
	}

	Squads.Forward.AttackPaths = {
		{ EastRally2.Location },
		{ EastRally4.Location },
	}

	InitGDIAttacks()
end

InitWestAttackers = function()
	local westAttackers = Utils.Where(Map.ActorsInCircle(GDIWestAttack.CenterPosition, WDist.New(7 * 1024)), function(a)
		return a.HasProperty("AttackMove")
	end)

	Utils.Do(westAttackers, function(a)
		if a.Owner == GDI and not a.IsDead then
			local dest = CPos.New(WestBaseCenter.Location.X + Utils.RandomInteger(-5,5), WestBaseCenter.Location.Y + Utils.RandomInteger(-5,5))
			a.AttackMove(dest)
			a.Hunt()
			Trigger.AfterDelay(DateTime.Seconds(75), function()
				if not a.IsDead then
					a.Stop()
					Trigger.AfterDelay(DateTime.Minutes(1), function()
						if not a.IsDead then
							a.AttackMove(EastRally4.Location)
							if IsHardOrAbove() then
								Trigger.AfterDelay(DateTime.Minutes(1), function()
									if not a.IsDead then
										a.Hunt()
									end
								end)
							end
						end
					end)
				end
			end)
		end
	end)

	local westDefenders = Nod3.GetGroundAttackers()
	Utils.Do(westDefenders, function(a)
		if not a.IsDead then
			a.Hunt()
		end
	end)
end

InitGDIEast = function()
	if GDIEastInitialized or GDIWestInitialized then
		return
	end

	GDIEastInitialized = true

	InitEastAttackers()

	-- main attacks will head to west base

	Squads.Main.AttackPaths = {
		{ WestRally1.Location },
		{ WestRally2.Location },
		{ WestRally3.Location },
		{ WestRally4.Location, WestRally1.Location },
	}

	Squads.Forward.AttackPaths = {
		{ WestRally1.Location },
		{ WestRally2.Location },
		{ WestRally3.Location },
	}

	InitGDIAttacks()
end

InitEastAttackers = function()
	local eastAttackers = Utils.Where(Map.ActorsInCircle(GDIEastAttack.CenterPosition, WDist.New(7 * 1024)), function(a)
		return a.HasProperty("AttackMove")
	end)

	Utils.Do(eastAttackers, function(a)
		if a.Owner == GDI and not a.IsDead then
			local dest = CPos.New(EastBaseCenter.Location.X + Utils.RandomInteger(-5,5), EastBaseCenter.Location.Y + Utils.RandomInteger(-5,5))
			a.AttackMove(dest)
			a.Hunt()
			Trigger.AfterDelay(DateTime.Seconds(75), function()
				if not a.IsDead then
					a.Stop()
					Trigger.AfterDelay(DateTime.Minutes(1), function()
						if not a.IsDead then
							a.AttackMove(WestRally1.Location)
							if IsHardOrAbove() then
								Trigger.AfterDelay(DateTime.Minutes(1), function()
									if not a.IsDead then
										a.Hunt()
									end
								end)
							end
						end
					end)
				end
			end)
		end
	end)

	local eastDefenders = Nod2.GetGroundAttackers()
	Utils.Do(eastDefenders, function(a)
		if not a.IsDead then
			a.Hunt()
		end
	end)
end

FlipEastBase = function()
    if not Nod.IsObjectiveCompleted(ObjectiveReinforce) then
		Utils.Do(MissionPlayers, function(p)
			p.Cash = 6000 + CashAdjustments[Difficulty]
		end)

		Utils.Do(NodRadarProviders, function(p)
			p.Destroy()
		end)

		ObjectiveDestroyGDI = Nod.AddObjective("Destroy GDI forces.")
        Nod.MarkCompletedObjective(ObjectiveReinforce)
		TransferEastNod()

		if IsNormalOrAbove() then
			Trigger.AfterDelay(DateTime.Seconds(15), function()
				InitEastAttackers()
			end)
		end
    end
end

-- overridden in co-op version
TransferEastNod = function()
	local nod2Assets = Nod2.GetActors()
	Utils.Do(nod2Assets, function(a)
		if not a.IsDead and a.Type ~= "player" then
			a.Owner = Nod
		end
	end)
end

FlipWestBase = function()
    if not Nod.IsObjectiveCompleted(ObjectiveReinforce) then
		Utils.Do(MissionPlayers, function(p)
			p.Cash = 6000 + CashAdjustments[Difficulty]
		end)

		Utils.Do(NodRadarProviders, function(p)
			p.Destroy()
		end)

		ObjectiveDestroyGDI = Nod.AddObjective("Destroy GDI forces.")
        Nod.MarkCompletedObjective(ObjectiveReinforce)
		TransferWestNod()

		if IsNormalOrAbove() then
			Trigger.AfterDelay(DateTime.Seconds(15), function()
				InitWestAttackers()
			end)
		end
    end
end

-- overridden in co-op version
TransferWestNod = function()
	local nod3Assets = Nod3.GetActors()
	Utils.Do(nod3Assets, function(a)
		if not a.IsDead and a.Type ~= "player" then
			a.Owner = Nod
		end
	end)
end

InitGDIAttacks = function()
	InitAttackSquad(Squads.Main, GDI)
	InitAttackSquad(Squads.Forward, GDI)
	InitAirAttackSquad(Squads.GDIAir, GDI)
end
