
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
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 25 },
			normal = { Min = 25, Max = 45 },
			hard = { Min = 35, Max = 75 },
		},
		FollowLeader = true,
		ProducerTypes = { Infantry = { "tent" }, Vehicles = { "weap" } },
		Units = AdjustCompositionsForDifficulty(UnitCompositions.Allied),
		AttackPaths = GreeceAttackPaths,
	},
	GDIMain = {
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 25 },
			normal = { Min = 25, Max = 45 },
			hard = { Min = 35, Max = 75 },
		},
		FollowLeader = true,
		ProducerTypes = { Infantry = { "pyle" }, Vehicles = { "weap.td" } },
		Units = AdjustCompositionsForDifficulty(UnitCompositions.GDI),
		AttackPaths = GDIAttackPaths,
	},
	GDIAir = {
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
			return not GDIHelipad1.IsDead or not GDIHelipad2.IsDead or not GDIHelipad3.IsDead or not GDIHelipad4.IsDead
		end,
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
	OncePerThirtySecondChecks()
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

		if not PlayerHasBuildings(GDI) and not PlayerHasBuildings(Greece) then
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

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % DateTime.Seconds(30) == 0 then
		CalculatePlayerCharacteristics()
	end
end

InitGreece = function()
	if Difficulty == "easy" then
		RebuildExcludes.Greece = { Types = { "gun", "pbox", "pris" } }
	end

	AutoRepairAndRebuildBuildings(Greece, 15)
	SetupRefAndSilosCaptureCredits(Greece)
	AutoReplaceHarvesters(Greece)
	AutoRebuildConyards(Greece)
	InitAiUpgrades(Greece)

	local alliedGroundAttackers = Greece.GetGroundAttackers()

	Utils.Do(alliedGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(6656), IsGreeceGroundHunterUnit)
	end)
end

InitGDI = function()
	if Difficulty == "easy" then
		RebuildExcludes.GDI = { Types = { "gtwr", "atwr" } }
	end

	AutoRepairAndRebuildBuildings(GDI, 15)
	SetupRefAndSilosCaptureCredits(GDI)
	AutoReplaceHarvesters(GDI)
	AutoRebuildConyards(GDI)
	InitAiUpgrades(GDI)

	local gdiGroundAttackers = GDI.GetGroundAttackers()

	Utils.Do(gdiGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(6656), IsGDIGroundHunterUnit)
	end)
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
			InitAirAttackSquad(Squads.GDIAir, GDI)
		end)
	end
end
