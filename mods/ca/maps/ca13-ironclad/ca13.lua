
GreeceAttackPaths = {
	{ AlliedAttackRally.Location, AlliedAttack1a.Location },
	{ AlliedAttackRally.Location, AlliedAttack1b.Location },
	{ AlliedAttackRally.Location, AlliedAttack1c.Location },
	{ AlliedAttackRally.Location, AlliedAttack1c.Location, AlliedAttack2.Location },
}

GDIAttackPaths = {
	{ GDIAttackRally.Location, GDIAttack1a.Location, GDIAttack2a.Location },
	{ GDIAttackRally.Location, GDIAttack1b.Location, GDIAttack2b.Location },
}

Squads = {
	GreeceMain = {
		Player = nil,
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 10 }, { MinTime = DateTime.Minutes(14), Value = 25 } },
			normal = { { MinTime = 0, Value = 25 }, { MinTime = DateTime.Minutes(12), Value = 45 } },
			hard = { { MinTime = 0, Value = 35 }, { MinTime = DateTime.Minutes(10), Value = 75 } },
		},
		QueueProductionStatuses = { Infantry = false, Vehicles = false },
		FollowLeader = true,
		IdleUnits = { },
		ProducerTypes = { Infantry = { "tent" }, Vehicles = { "weap" } },
		Units = UnitCompositions.Allied.Main,
		AttackPaths = GreeceAttackPaths,
	},
	GDIMain = {
		Player = nil,
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 10 }, { MinTime = DateTime.Minutes(14), Value = 25 } },
			normal = { { MinTime = 0, Value = 25 }, { MinTime = DateTime.Minutes(12), Value = 45 } },
			hard = { { MinTime = 0, Value = 35 }, { MinTime = DateTime.Minutes(10), Value = 75 } },
		},
		QueueProductionStatuses = { Infantry = false, Vehicles = false },
		FollowLeader = true,
		IdleUnits = { },
		ProducerTypes = { Infantry = { "pyle" }, Vehicles = { "weap.td" } },
		Units = UnitCompositions.GDI.Main,
		AttackPaths = GDIAttackPaths,
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
			return not GDIHelipad1.IsDead or not GDIHelipad2.IsDead or not GDIHelipad3.IsDead or not GDIHelipad4.IsDead
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

SiegeBreakThreshold = {
	easy = 90,
	normal = 75,
	hard = 60
}

AutoSiegeBreakTime = {
	easy = DateTime.Minutes(30),
	normal = DateTime.Minutes(20),
	hard = DateTime.Minutes(10)
}

AutoAttackStartTime = {
	easy = DateTime.Minutes(16),
	normal = DateTime.Minutes(12),
	hard = DateTime.Minutes(8)
}

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	GDI = Player.GetPlayer("GDI")
	Greece = Player.GetPlayer("Greece")
	MissionPlayers = { USSR }
	TimerTicks = 0
	SiegeLosses = 0
	SiegeBroken = false
	AttacksStarted = false

	Camera.Position = PlayerStart.CenterPosition
	USSR.PlayLowPowerNotification = false

	InitObjectives(USSR)
	InitGDI()
	InitGreece()

	ObjectiveDestroyBases = USSR.AddObjective("Break the siege and destroy the enemy bases.")
	ObjectiveProtectIronCurtain = USSR.AddObjective("Do not lose the Iron Curtain.")
	EngineerDrop()

	Trigger.AfterDelay(5, function()
		SiegeActors = Map.ActorsInBox(SiegeTopLeft.CenterPosition, SiegeBottomRight.CenterPosition, function(a)
			return (a.Owner == Greece or a.Owner == GDI) and a.Type ~= "camera" and a.HasProperty("Move")
		end)

		Utils.Do(SiegeActors, function(a)
			Trigger.OnKilled(a, function(self, killer)
				SiegeLosses = SiegeLosses + 1
			end)
		end)
	end)

	Trigger.AfterDelay(AutoAttackStartTime[Difficulty], function()
		StartAttacks()
	end)

	Trigger.OnRemovedFromWorld(IronCurtain, function(a)
		USSR.MarkFailedObjective(ObjectiveProtectIronCurtain)
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		GDI.Resources =  GDI.ResourceCapacity - 500
		Greece.Resources =  Greece.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
		end

		if GDI.HasNoRequiredUnits() and Greece.HasNoRequiredUnits() then
			USSR.MarkCompletedObjective(ObjectiveDestroyBases)
			USSR.MarkCompletedObjective(ObjectiveProtectIronCurtain)
		end

		if USSR.HasNoRequiredUnits() then
			USSR.MarkFailedObjective(ObjectiveDestroyBases)
		end
	end

	if SiegeBroken == false and (SiegeLosses >= SiegeBreakThreshold[Difficulty] or DateTime.GameTime > AutoSiegeBreakTime[Difficulty]) then
		SiegeBroken = true

		Utils.Do(SiegeActors, function(a)
			AssaultPlayerBaseOrHunt(a)
		end)

		StartAttacks()
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()
	end
end

InitGreece = function()
	if Difficulty == "easy" then
		RebuildExcludes.Greece = { Types = { "gun", "pbox", "pris" } }
	end

	AutoRepairAndRebuildBuildings(Greece, 15)
	SetupRefAndSilosCaptureCredits(Greece)
	AutoReplaceHarvesters(Greece)

	local alliedGroundAttackers = Greece.GetGroundAttackers()

	Utils.Do(alliedGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(6656), IsGreeceGroundHunterUnit)
	end)

	Actor.Create("hazmat.upgrade", true, { Owner = Greece })
	Actor.Create("apb.upgrade", true, { Owner = Greece })

	if Difficulty == "hard" then
		Actor.Create("cryw.upgrade", true, { Owner = Greece })

		Trigger.AfterDelay(DateTime.Minutes(20), function()
			Actor.Create("flakarmor.upgrade", true, { Owner = Greece })
		end)
	end
end

InitGDI = function()
	if Difficulty == "easy" then
		RebuildExcludes.GDI = { Types = { "gtwr", "atwr" } }
	end

	AutoRepairAndRebuildBuildings(GDI, 15)
	SetupRefAndSilosCaptureCredits(GDI)
	AutoReplaceHarvesters(GDI)

	local gdiGroundAttackers = GDI.GetGroundAttackers()

	Utils.Do(gdiGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(6656), IsGDIGroundHunterUnit)
	end)

	Actor.Create("hazmat.upgrade", true, { Owner = GDI })

	if Difficulty == "hard" then
		Trigger.AfterDelay(DateTime.Minutes(10), function()
			local strategyUpgrades = {
				{ "bombard.strat", "bombard2.strat", "hailstorm.upgrade" },
				{ "seek.strat", "seek2.strat", "hypersonic.upgrade" },
				{ "hold.strat", "hold2.strat", "hammerhead.upgrade" },
			}

			local selectedStrategyUpgrades = Utils.Random(strategyUpgrades)
			Utils.Do(selectedStrategyUpgrades, function(u)
				Actor.Create(u, true, { Owner = GDI })
			end)
		end)

		Trigger.AfterDelay(DateTime.Minutes(20), function()
			Actor.Create("flakarmor.upgrade", true, { Owner = GDI })
		end)
	end
end

EngineerDrop = function()
	local entryPath
	entryPath = { EngiDropSpawn.Location, EngiDropLanding.Location }

	local haloDropUnits = { "e6", "e6", "e6", "e6", "e6", "e6", "e6", "e6" }

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Notification("Engineering team inbound.")
		MediaCA.PlaySound("r2_engineeringteam.aud", 2)
	end)

	DoHelicopterDrop(USSR, entryPath, "halo.engis", haloDropUnits, nil, function(t)
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			USSR.PlayLowPowerNotification = true
			if not t.IsDead then
				t.Move(entryPath[1])
				t.Destroy()
			end
		end)
	end)
end

StartAttacks = function()
	if AttacksStarted == false then
		AttacksStarted = true
		InitAttackSquad(Squads.GreeceMain, Greece)
		InitAttackSquad(Squads.GDIMain, GDI)

		Trigger.AfterDelay(Squads.GDIAir.Delay[Difficulty], function()
			InitAirAttackSquad(Squads.GDIAir, GDI, USSR, { "harv", "v2rl", "apwr", "tsla", "ttra", "v3rl", "mig", "hind", "suk", "suk.upg", "kiro", "apoc" })
		end)
	end
end
