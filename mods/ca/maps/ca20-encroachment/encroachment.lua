
AlliedAttackPaths = {
	{ AlliedAttack1.Location, AlliedAttack2a.Location, AlliedAttack3.Location, AlliedAttack4.Location, AlliedAttack5a.Location },
	{ AlliedAttack1.Location, AlliedAttack2a.Location, AlliedAttack3.Location, AlliedAttack4.Location, AlliedAttack5b.Location },
	{ AlliedAttack1.Location, AlliedAttack2b.Location, AlliedAttack3.Location, AlliedAttack4.Location, AlliedAttack5a.Location },
	{ AlliedAttack1.Location, AlliedAttack2b.Location, AlliedAttack3.Location, AlliedAttack4.Location, AlliedAttack5b.Location },
}

GDIAttackPaths = {
	{ GDIAttack1.Location, GDIAttack2a.Location, GDIAttack3.Location},
	{ GDIAttack1.Location, GDIAttack2b.Location, GDIAttack3.Location},
}

ReinforcementGroups = {
	{
		Waypoint = NEWormhole,
		Targets = { NEBuilding1, NEBuilding2, NEBuilding3, NEBuilding4, NEBuilding5, NEBuilding6, NEBuilding7, NEBuilding8 },
		Units = { "gunw", "s1", "s1", "s1", "s3", "s3", "intl" }
	},
	{
		Waypoint = NWWormhole,
		Targets = { NWBuilding1, NWBuilding2, NWBuilding3, NWBuilding4 },
		Units = { "intl", "s1", "s1", "s1", "s3", "s3", "lace", "lace" }
	},
	{
		Waypoint = MWormhole,
		Targets = { MBuilding1, MBuilding2, MBuilding3, MBuilding4, MBuilding5, MBuilding6 },
		Units = { "rtpd", "s1", "s1", "s4", "s2", "ruin" }
	},
}

PowerGrids = {
	{
		Providers = { WPower1, WPower2, WPower3, WPower4, WPower5, WPower6 },
		Consumers = { WPowered1, WPowered2, WPowered3, WPowered4, WPowered5, WPowered6, WPowered7, WPowered8, WPowered9, WPowered10, WPowered11 },
	},
	{
		Providers = { SWPower1, SWPower2, SWPower3 },
		Consumers = { SWPowered1, SWPowered2, SWPowered3, SWPowered4, SWPowered5, SWPowered6, SWPowered7, SWPowered8 },
	},
	{
		Providers = { SEPower1, SEPower2, SEPower3, SEPower4, SEPower5, SEPower6, SEPower7 },
		Consumers = { SEPowered1, SEPowered2, SEPowered3, SEPowered4, SEPowered5, SEPowered6, SEPowered7, SEPowered8, SEPowered9, SEPowered10, SEPowered11, SEPowered12 },
	},
}

WeatherStormEnabledTime = {
	easy = DateTime.Seconds((60 * 25) + 17),
	normal = DateTime.Seconds((60 * 20) + 17),
	hard = DateTime.Seconds((60 * 15) + 17),
	vhard = DateTime.Seconds((60 * 10) + 17),
	brutal = DateTime.Seconds((60 * 10) + 17)
}

IonCannonEnabledTime = {
	easy = DateTime.Seconds((60 * 28) + 48),
	normal = DateTime.Seconds((60 * 23) + 48),
	hard = DateTime.Seconds((60 * 18) + 48),
	vhard = DateTime.Seconds((60 * 13) + 48),
	brutal = DateTime.Seconds((60 * 13) + 48)
}

Squads = {
	AlliedMain = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(5)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		FollowLeader = true,
		ProducerActors = { Infantry = { AlliedSouthBarracks }, Vehicles = { AlliedSouthFactory } },
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Allied),
		AttackPaths = AlliedAttackPaths,
	},
	GDIMain = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(5)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		FollowLeader = true,
		ProducerActors = { Infantry = { GDISouthBarracks }, Vehicles = { GDISouthFactory } },
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.GDI),
		AttackPaths = GDIAttackPaths,
	},
	AlliedAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Allied,
	},
	GDIAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		ActiveCondition = function()
			return (not GDIHelipad1.IsDead and GDIHelipad1.Owner == GDI) or (not GDIHelipad2.IsDead and GDIHelipad2.Owner == GDI) or (not GDIHelipad3.IsDead and GDIHelipad3.Owner == GDI)
		end,
		Compositions = AirCompositions.GDI,
	}
}

WorldLoaded = function()
	Scrin = Player.GetPlayer("Scrin")
	Greece = Player.GetPlayer("Greece")
	GDI = Player.GetPlayer("GDI")
	MissionPlayers = { Scrin }

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Scrin)
	AdjustPlayerStartingCashForDifficulty()
	InitGreece()
	InitGDI()

	ObjectiveDestroyAdvComms = Scrin.AddObjective("Destroy GDI Advanced Communications Center.")
	ObjectiveDestroyWeatherControl = Scrin.AddObjective("Destroy Allied Weather Control Device.")

	Trigger.OnKilledOrCaptured(AdvancedComms, function()
		Scrin.MarkCompletedObjective(ObjectiveDestroyAdvComms)
	end)

	Trigger.OnKilledOrCaptured(WeatherControl, function()
		Scrin.MarkCompletedObjective(ObjectiveDestroyWeatherControl)
	end)

	Utils.Do(PowerGrids, function(grid)
		Trigger.OnAllKilledOrCaptured(grid.Providers, function()
			Utils.Do(grid.Consumers, function(consumer)
				if not consumer.IsDead then
					consumer.GrantCondition("disabled")
				end
			end)
		end)
	end)

	Utils.Do(ReinforcementGroups, function(group)
		Trigger.OnAllKilledOrCaptured(group.Targets, function()
			Trigger.AfterDelay(DateTime.Seconds(2), function()
				local wormhole = Actor.Create("wormhole", true, { Owner = Scrin, Location = group.Waypoint.Location })

				Trigger.AfterDelay(DateTime.Seconds(2), function()
					Media.PlaySpeechNotification(Scrin, "ReinforcementsArrived")
					Notification("Reinforcements have arrived.")
					Beacon.New(Scrin, group.Waypoint.CenterPosition)

					local reinforcements = Reinforcements.Reinforce(Scrin, group.Units, { group.Waypoint.Location }, 10, function(a)
						a.Scatter()
					end)
				end)

				Trigger.AfterDelay(DateTime.Seconds(10), function()
					wormhole.Kill()
				end)
			end)
		end)
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
		GDI.Resources = GDI.ResourceCapacity - 500

		if Scrin.HasNoRequiredUnits() then
			if ObjectiveDestroyAdvComms ~= nil and not Scrin.IsObjectiveCompleted(ObjectiveDestroyAdvComms) then
				Scrin.MarkFailedObjective(ObjectiveDestroyAdvComms)
			end
			if ObjectiveDestroyWeatherControl ~= nil and not Scrin.IsObjectiveCompleted(ObjectiveDestroyWeatherControl) then
				Scrin.MarkFailedObjective(ObjectiveDestroyWeatherControl)
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

InitGreece = function()
	if Difficulty == "easy" then
		RebuildExcludes.Greece = { Types = { "gun", "pbox", "pris", "awpr", "weat" } }
	else
		RebuildExcludes.Greece = { Types = { "awpr", "weat" } }
	end

	AutoRepairAndRebuildBuildings(Greece, 15)
	SetupRefAndSilosCaptureCredits(Greece)
	AutoReplaceHarvesters(Greece)
	AutoRebuildConyards(Greece)
	InitAiUpgrades(Greece)
	InitAttackSquad(Squads.AlliedMain, Greece)
	InitAirAttackSquad(Squads.AlliedAir, Greece)

	Actor.Create("ai.unlimited.power", true, { Owner = Greece })

	Trigger.AfterDelay(WeatherStormEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = Greece })
	end)

	local alliedGroundAttackers = Greece.GetGroundAttackers()

	Utils.Do(alliedGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGreeceGroundHunterUnit)
	end)
end

InitGDI = function()
	Actor.Create("ai.unlimited.power", true, { Owner = GDI })

	if Difficulty == "easy" then
		RebuildExcludes.GDI = { Types = { "gtwr", "atwr", "stwr", "nuk2", "eye" } }
	else
		RebuildExcludes.GDI = { Types = { "nuk2", "eye" } }
	end

	AutoRepairAndRebuildBuildings(GDI, 15)
	SetupRefAndSilosCaptureCredits(GDI)
	AutoReplaceHarvesters(GDI)
	AutoRebuildConyards(GDI)
	InitAiUpgrades(GDI)
	InitAttackSquad(Squads.GDIMain, GDI)
	InitAirAttackSquad(Squads.GDIAir, GDI)

	local gdiGroundAttackers = GDI.GetGroundAttackers()

	Utils.Do(gdiGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGDIGroundHunterUnit)
	end)

	Trigger.AfterDelay(IonCannonEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = GDI })
	end)
end
