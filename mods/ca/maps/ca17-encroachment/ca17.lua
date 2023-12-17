
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
		Units = { "rptp", "s1", "s1", "s4", "s2", "ruin" }
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
	easy = DateTime.Seconds((60 * 20) + 17),
	normal = DateTime.Seconds((60 * 15) + 17),
	hard = DateTime.Seconds((60 * 10) + 17),
}

IonCannonEnabledTime = {
	easy = DateTime.Seconds((60 * 23) + 48),
	normal = DateTime.Seconds((60 * 18) + 48),
	hard = DateTime.Seconds((60 * 13) + 48),
}

Squads = {
	AlliedMain = {
		Player = nil,
		Delay = {
			easy = DateTime.Minutes(7),
			normal = DateTime.Minutes(5),
			hard = DateTime.Minutes(3)
		},
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 10 }, { MinTime = DateTime.Minutes(14), Value = 25 } },
			normal = { { MinTime = 0, Value = 25 }, { MinTime = DateTime.Minutes(12), Value = 35 }, { MinTime = DateTime.Minutes(16), Value = 50 } },
			hard = { { MinTime = 0, Value = 40 }, { MinTime = DateTime.Minutes(10), Value = 60 }, { MinTime = DateTime.Minutes(14), Value = 80 } },
		},
		QueueProductionStatuses = { Infantry = false, Vehicles = false },
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { AlliedSouthBarracks }, Vehicles = { AlliedSouthFactory } },
		Units = UnitCompositions.Allied.Main,
		AttackPaths = AlliedAttackPaths,
	},
	GDIMain = {
		Player = nil,
		Delay = {
			easy = DateTime.Minutes(7),
			normal = DateTime.Minutes(5),
			hard = DateTime.Minutes(3)
		},
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 10 }, { MinTime = DateTime.Minutes(14), Value = 25 } },
			normal = { { MinTime = 0, Value = 25 }, { MinTime = DateTime.Minutes(12), Value = 35 }, { MinTime = DateTime.Minutes(16), Value = 50 } },
			hard = { { MinTime = 0, Value = 40 }, { MinTime = DateTime.Minutes(10), Value = 60 }, { MinTime = DateTime.Minutes(14), Value = 80 } },
		},
		QueueProductionStatuses = { Infantry = false, Vehicles = false },
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { GDISouthBarracks }, Vehicles = { GDISouthFactory } },
		Units = UnitCompositions.GDI.Main,
		AttackPaths = GDIAttackPaths,
	},
	AlliedAir = {
		Player = nil,
		Delay = {
			easy = DateTime.Minutes(13),
			normal = DateTime.Minutes(12),
			hard = DateTime.Minutes(11)
		},
		Interval = {
			easy = DateTime.Minutes(3),
			normal = DateTime.Seconds(150),
			hard = DateTime.Minutes(2)
		},
		QueueProductionStatuses = {
			Aircraft = false
		},
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Aircraft = { "hpad" } },
		Units = {
			easy = {
				{ Aircraft = { "heli" } }
			},
			normal = {
				{ Aircraft = { "heli", "heli" } },
				{ Aircraft = { "harr" } }
			},
			hard = {
				{ Aircraft = { "heli", "heli", "heli" } },
				{ Aircraft = { "harr", "harr" } }
			}
		},
	},
	GDIAir = {
		Player = nil,
		Delay = {
			easy = DateTime.Minutes(13),
			normal = DateTime.Minutes(12),
			hard = DateTime.Minutes(11)
		},
		Interval = {
			easy = DateTime.Minutes(3),
			normal = DateTime.Seconds(150),
			hard = DateTime.Minutes(2)
		},
		ActiveCondition = function()
			return (not GDIHelipad1.IsDead and GDIHelipad1.Owner == GDI) or (not GDIHelipad2.IsDead and GDIHelipad2.Owner == GDI) or (not GDIHelipad3.IsDead and GDIHelipad3.Owner == GDI)
		end,
		QueueProductionStatuses = {
			Aircraft = false
		},
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Aircraft = { "afld.gdi" } },
		Units = {
			easy = {
				{ Aircraft = { "orca" } }
			},
			normal = {
				{ Aircraft = { "orca", "orca" } },
				{ Aircraft = { "a10" } }
			},
			hard = {
				{ Aircraft = { "orca", "orca", "orca" } },
				{ Aircraft = { "a10", "a10" } }
			}
		},
	}
}

WorldLoaded = function()
	Scrin = Player.GetPlayer("Scrin")
	Greece = Player.GetPlayer("Greece")
	GDI = Player.GetPlayer("GDI")
	MissionPlayer = Scrin

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Scrin)
	AdjustStartingCash()
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
		UpdatePlayerBaseLocation()
	end
end

InitGreece = function()
	Actor.Create("POWERCHEAT", true, { Owner = Greece })
	Actor.Create("hazmat.upgrade", true, { Owner = Greece })

	if Difficulty == "hard" then
		Actor.Create("cryr.upgrade", true, { Owner = Greece })
	end

	if Difficulty == "easy" then
		RebuildExcludes.Greece = { Types = { "gun", "pbox", "pris", "awpr", "weat" } }
	else
		RebuildExcludes.Greece = { Types = { "awpr", "weat" } }
	end

	AutoRepairAndRebuildBuildings(Greece, 15)
	SetupRefAndSilosCaptureCredits(Greece)
	AutoReplaceHarvesters(Greece)

	local alliedGroundAttackers = Greece.GetGroundAttackers()

	Utils.Do(alliedGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGreeceGroundHunterUnit)
	end)

	Trigger.AfterDelay(WeatherStormEnabledTime[Difficulty], function()
		if not WeatherControl.IsDead then
			WeatherControl.GrantCondition("weather-storm-enabled")
		end
	end)

	Trigger.AfterDelay(Squads.AlliedMain.Delay[Difficulty], function()
		InitAttackSquad(Squads.AlliedMain, Greece)
	end)

	Trigger.AfterDelay(Squads.AlliedAir.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.AlliedAir, Greece, Scrin, { "harv.scrin", "scol", "proc.scrin", "ptur", "shar", "stmr", "enrv", "tpod" })
	end)
end

InitGDI = function()
	Actor.Create("POWERCHEAT", true, { Owner = GDI })
	Actor.Create("hazmat.upgrade", true, { Owner = GDI })
	Actor.Create("hold.strat", true, { Owner = GDI })

	if Difficulty == "hard" then
		Actor.Create("sonic.upgrade", true, { Owner = GDI, })
		Actor.Create("hammerhead.upgrade", true, { Owner = GDI, })
		Actor.Create("hold2.strat", true, { Owner = GDI, })
		Actor.Create("hold3.strat", true, { Owner = GDI })
	end

	if Difficulty == "easy" then
		RebuildExcludes.GDI = { Types = { "gtwr", "atwr", "stwr", "nuk2", "eye" } }
	else
		RebuildExcludes.GDI = { Types = { "nuk2", "eye" } }
	end

	AutoRepairAndRebuildBuildings(GDI, 15)
	SetupRefAndSilosCaptureCredits(GDI)
	AutoReplaceHarvesters(GDI)

	local gdiGroundAttackers = GDI.GetGroundAttackers()

	Utils.Do(gdiGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGDIGroundHunterUnit)
	end)

	Trigger.AfterDelay(IonCannonEnabledTime[Difficulty], function()
		if not AdvancedComms.IsDead then
			AdvancedComms.GrantCondition("ion-cannon-enabled")
		end
	end)

	Trigger.AfterDelay(Squads.GDIMain.Delay[Difficulty], function()
		InitAttackSquad(Squads.GDIMain, GDI)
	end)

	Trigger.AfterDelay(Squads.GDIAir.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.GDIAir, GDI, Scrin, { "harv.scrin", "scol", "proc.scrin", "ptur", "shar", "stmr", "enrv", "tpod" })
	end)
end
