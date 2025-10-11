MissionDir = "ca|missions/main-campaign/ca44-trepidation"

SuperweaponsEnabledTime = {
	easy = DateTime.Minutes(40),
	normal = DateTime.Minutes(25),
	hard = DateTime.Minutes(15),
	vhard = DateTime.Minutes(10),
	brutal = DateTime.Minutes(10)
}

DogInterval = {
	easy = DateTime.Seconds(60),
	normal = DateTime.Seconds(30),
	hard = DateTime.Seconds(15),
	vhard = DateTime.Seconds(12),
	brutal = DateTime.Seconds(8)
}

SovetAttackPaths = {
	{ SovietPath1a.Location, SovietPath1b.Location },
	{ SovietPath2a.Location, SovietPath2b.Location },
}

ScrinAttackPaths = {
	{ ScrinRally1.Location },
	{ ScrinRally2.Location },
}

Squads = {
	SovietMain = {
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Soviet),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 30, Max = 60 }),
		FollowLeader = true,
		AttackPaths = SovietAttackPaths,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(7)),
	},
	ScrinMain = {
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 10, Max = 20 }),
		FollowLeader = true,
		AttackPaths = ScrinAttackPaths,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(8)),
	},
	Air = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(18)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Soviet,
	}
}

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	Scrin = Player.GetPlayer("Scrin")
	England = Player.GetPlayer("England")
	Neutral = Player.GetPlayer("Neutral")

	MissionPlayers = { Greece }

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Greece)
	AdjustPlayerStartingCashForDifficulty()
	RemoveActorsBasedOnDifficultyTags()
	InitUSSR()
	InitScrin()

	SetupReveals({ Reveal1, Reveal2, Reveal3, Reveal4 })
	SetupChurchMoneyCrates(Neutral)

	Actor.Create("optics.upgrade", true, { Owner = Greece })

	ObjectiveExtractSpy = Greece.AddObjective("Get spy to safety.")

	Trigger.AfterDelay(DateTime.Seconds(20), function()
		Spy = Actor.Create("spy.noinfil", true, { Owner = Greece, Location = Gateway.Location })
		Spy.DisguiseAs(SpyTarget)
		Spy.Move(SpyDest.Location)
		MediaCA.PlaySound(MissionDir .. "/r_spydetected.aud", 2)
		Notification("Allied spy detected. Press [" .. UtilsCA.Hotkey("ToLastEvent") .. "] to view location.")
		Beacon.New(Greece, SpyDest.CenterPosition)

		Trigger.OnKilled(Spy, function()
			if not Greece.IsObjectiveCompleted(ObjectiveExtractSpy) then
				Greece.MarkFailedObjective(ObjectiveExtractSpy)
			end
		end)

		Trigger.OnExitedProximityTrigger(Gateway.CenterPosition, WDist.New(22 * 1024), function(a, id)
			if a == Spy then
				Trigger.RemoveProximityTrigger(id)
				InitHuntSpy()
			end
		end)

		Trigger.OnEnteredFootprint({ SpySafety1.Location, SpySafety2.Location, SpySafety3.Location, SpySafety4.Location, SpySafety5.Location  }, function(a, id)
			if a == Spy then
				Trigger.RemoveFootprintTrigger(id)
				Spy.Owner = England
				SpyDeparture()
			end
		end)
	end)

	Trigger.OnProduction(Kennel, function(producer, produced)
		if produced.Type == "dog" and not produced.IsDead and not Spy.IsDead then
			produced.Attack(Spy)
		end
	end)

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

		if not PlayerHasBuildings(USSR) and not PlayerHasBuildings(Scrin) then
			if ObjectiveEliminateEnemy == nil then
				ObjectiveEliminateEnemy = Greece.AddObjective("Eliminate Soviet & Scrin presence.")
			end
			Greece.MarkCompletedObjective(ObjectiveEliminateEnemy)
		end

		if MissionPlayersHaveNoRequiredUnits() then
			if ObjectiveEliminateEnemy ~= nil and not Greece.IsObjectiveCompleted(ObjectiveEliminateEnemy) then
				Greece.MarkFailedObjective(ObjectiveEliminateEnemy)
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
	if DateTime.GameTime > 1 and DateTime.GameTime % 750 == 0 then
		CalculatePlayerCharacteristics()
	end
end

InitUSSR = function()
	AutoRepairAndRebuildBuildings(USSR)
	SetupRefAndSilosCaptureCredits(USSR)
	AutoReplaceHarvesters(USSR)
	AutoRebuildConyards(USSR)

	Actor.Create("hazmatsoviet.upgrade", true, { Owner = USSR })

	local ussrGroundAttackers = USSR.GetGroundAttackers()
	Utils.Do(ussrGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsUSSRGroundHunterUnit)
	end)
end

InitUSSRAttacks = function()
	InitAiUpgrades(USSR)
	InitAttackSquad(Squads.SovietMain, USSR)
	InitAirAttackSquad(Squads.Air, USSR)

	Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = USSR })
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = USSR })
	end)
end

InitScrin = function()
	AutoRepairAndRebuildBuildings(Scrin)
	SetupRefAndSilosCaptureCredits(Scrin)
	AutoReplaceHarvesters(Scrin)
	AutoRebuildConyards(Scrin)

	local scrinGroundAttackers = Scrin.GetGroundAttackers()
	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)
end

InitScrinAttacks = function()
	InitAiUpgrades(Scrin)
	InitAttackSquad(Squads.ScrinMain, Scrin)
end

InitHuntSpy = function()
	local dogs = USSR.GetActorsByType("dog")
	Utils.Do(dogs, function(d) d.Attack(Spy) end)
	ReleaseDog()
end

ReleaseDog = function()
	if Kennel.IsDead or Spy.IsDead then
		return
	end
	Kennel.Produce("dog")
	Trigger.AfterDelay(DogInterval[Difficulty], ReleaseDog)
end

SpyDeparture = function()
	Trigger.AfterDelay(1, function()
		Spy.Stop()
		Spy.Move(McvDest.Location)
		Spy.Move(McvSpawn.Location)
		Spy.DisguiseAsType("spy", England)

		local spyExitCells = { CPos.New(87,98), CPos.New(88,98), CPos.New(89,98), CPos.New(90,98), CPos.New(91,98), CPos.New(92,98), CPos.New(93,98) }

		Trigger.OnIdle(Spy, function()
			if SpyDeparted then
				return
			end
			Spy.Move(Utils.Random(spyExitCells))
		end)

		Trigger.OnEnteredFootprint(spyExitCells, function(a, id)
			if a == Spy and not SpyDeparted then
				Trigger.RemoveFootprintTrigger(id)
				SpyDeparted = true
				if ObjectiveEliminateEnemy == nil then
					ObjectiveEliminateEnemy = Greece.AddObjective("Eliminate Soviet & Scrin presence.")
				end
				Greece.MarkCompletedObjective(ObjectiveExtractSpy)
				Spy.Stop()
				Spy.Destroy()

				for _, p in ipairs(MissionPlayers) do
					p.GrantCondition("spy-extracted")
				end

				Trigger.AfterDelay(DateTime.Seconds(2), function()
					Beacon.New(Greece, McvDest.CenterPosition)
					Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
					Notification("Reinforcements have arrived.")
					Reinforcements.Reinforce(Greece, { "mcv", "2tnk", "2tnk", "arty", "arty", "e1", "e1", "e1", "e1", "e3", "medi" }, { McvSpawn.Location, McvDest.Location }, 75)
					InitUSSRAttacks()
					InitScrinAttacks()
				end)
			end
		end)
	end)
end
