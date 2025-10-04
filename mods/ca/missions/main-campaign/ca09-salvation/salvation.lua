MissionDir = "ca|missions/main-campaign/ca09-salvation"

Wormholes = {
	{ Locations = { WormholeSpawn1a.Location, WormholeSpawn1b.Location, WormholeSpawn1c.Location, WormholeSpawn1d.Location }, Actor = nil, SpawnCount = 0, Phase = 1 },
	{ Locations = { WormholeSpawn4a.Location, WormholeSpawn4b.Location, WormholeSpawn4c.Location, WormholeSpawn4d.Location, WormholeSpawn4e.Location }, Actor = nil, SpawnCount = 0, Phase = 1 },
	{ Locations = { WormholeSpawn7a.Location, WormholeSpawn7b.Location, WormholeSpawn7c.Location, WormholeSpawn7d.Location, WormholeSpawn7e.Location }, Actor = nil, SpawnCount = 0, Phase = 1 },

	{ Locations = { WormholeSpawn9a.Location, WormholeSpawn9b.Location, WormholeSpawn9c.Location }, Actor = nil, SpawnCount = 0, Phase = 2 }, -- Bottom outer
	{ Locations = { WormholeSpawn5a.Location, WormholeSpawn5b.Location, WormholeSpawn5c.Location }, Actor = nil, SpawnCount = 0, Phase = 2 }, -- Center main
	{ Locations = { WormholeSpawn2a.Location, WormholeSpawn2b.Location, WormholeSpawn2c.Location, WormholeSpawn2d.Location }, Actor = nil, SpawnCount = 0, Phase = 2 }, -- North-east corner

	{ Locations = { WormholeSpawn3a.Location, WormholeSpawn3b.Location, WormholeSpawn3c.Location }, Actor = nil, SpawnCount = 0, Phase = 3 }, -- North-east inner
	{ Locations = { WormholeSpawn6a.Location, WormholeSpawn6b.Location, WormholeSpawn6c.Location }, Actor = nil, SpawnCount = 0, Phase = 3 }, -- Bottom inner
	{ Locations = { WormholeSpawn8a.Location, WormholeSpawn8b.Location, WormholeSpawn8c.Location }, Actor = nil, SpawnCount = 0, Phase = 3 }, -- Far east

}

WormholeDelay = {
	easy = DateTime.Minutes(6),
	normal = DateTime.Minutes(5) + DateTime.Seconds(30),
	hard = DateTime.Minutes(5),
	vhard = DateTime.Minutes(4) + DateTime.Seconds(30),
	brutal = DateTime.Minutes(4)
}

WormholeInterval = {
	easy = DateTime.Minutes(1) + DateTime.Seconds(40),
	normal = DateTime.Minutes(1) + DateTime.Seconds(30),
	hard = DateTime.Minutes(1) + DateTime.Seconds(20),
	vhard = DateTime.Minutes(1) + DateTime.Seconds(10),
	brutal = DateTime.Minutes(1),
}

WormholeUnitsDelay = {
	easy = DateTime.Seconds(70),
	normal = DateTime.Seconds(50),
	hard = DateTime.Seconds(35),
	vhard = DateTime.Seconds(30),
	brutal = DateTime.Seconds(25)
}

WormholeUnitsInterval = {
	easy = DateTime.Seconds(140),
	normal = DateTime.Seconds(120),
	hard = DateTime.Seconds(100),
	vhard = DateTime.Seconds(80),
	brutal = DateTime.Seconds(60)
}

Phase2Start = {
	easy = DateTime.Minutes(13),
	normal = DateTime.Minutes(11),
	hard = DateTime.Minutes(9),
	vhard = DateTime.Minutes(8),
	brutal = DateTime.Minutes(7)
}

Phase3Start = {
	easy = DateTime.Minutes(16),
	normal = DateTime.Minutes(14),
	hard = DateTime.Minutes(12),
	vhard = DateTime.Minutes(11),
	brutal = DateTime.Minutes(10)
}

WormholeUnitGroups = {
	{ "seek", "seek", "gscr", "gscr", "s3", "s3", "s3", "s1" },
	{ "intl", "intl", "gscr", "gscr", "s3", "s3", "s4", "s1" },
	{ "corr", "corr", "gscr", "gscr", "s2", "s2", "s1", "s1" },
	{ "lchr", "lchr", "gscr", "gscr", "s2", "s2", "s1", "s1" },
	{ "tpod", "devo", "gscr", "gscr", "s3", "s3", "s3", "s1" },
	{ "gunw", "atmz", "gscr", "gscr", "s3", "s3", "s3", "s4" },
	{ "ruin", "shrw", "gscr", "gscr", "s1", "s1", "s2", "s2" },
	{ "gunw", "gunw", "gscr", "gscr", "s1", "s1", "s2", "s2" },
}

-- Setup and Tick

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	Scrin = Player.GetPlayer("Scrin")
	Civilian = Player.GetPlayer("Civilian")
	MissionPlayers = { Nod }
	MissionEnemies = { Scrin }
	TimerTicks = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Nod)
	InitScrin()

	ObjectivePurgeScrin = Nod.AddObjective("Eliminate the Scrin presence.")
	ObjectiveSaveAllCivilians = Nod.AddSecondaryObjective("Allow no civilians to be killed.")
	Notification("The Scrin are preparing reinforcements, we must eliminate their foothold here quickly.")

	NodCamera1.Destroy()
	NodCamera2.Destroy()
	NodCamera3.Destroy()
	NodCamera4.Destroy()
	NodCamera5.Destroy()
	NodCamera6.Destroy()

	Actor.Create("tibcore.upgrade", true, { Owner = Nod })

	if Difficulty == "easy" then
		Nod.Cash = 4800
	elseif Difficulty == "normal" then
		Nod.Cash = 2300
	end

	Trigger.AfterDelay(WormholeDelay[Difficulty], function()
		SpawnWormhole()
	end)

	local scrinActors = Scrin.GetActors()
	local scrinRemaining = Utils.Where(scrinActors, function(a)
		return not a.IsDead and a.HasProperty("Kill")
	end)

	Utils.Do(scrinRemaining, function(a)
		Trigger.OnKilled(a, function(self, killer)
			UpdateScrinCounter()
		end)
	end)

	local civilianActors = Civilian.GetActors()
	Utils.Do(civilianActors, function(a)
		Trigger.OnKilled(a, function(self, killer)
			if ObjectiveSaveAllCivilians ~= nil and not Nod.IsObjectiveCompleted(ObjectiveSaveAllCivilians) then
				Nod.MarkFailedObjective(ObjectiveSaveAllCivilians)
			end
		end)
	end)

	Trigger.OnEnteredProximityTrigger(TopBoundary.CenterPosition, WDist.New(5 * 1024), function(a, id)
		if a.Owner == Civilian and a.HasProperty("Move") then
			a.Stop()
			a.Move(TopTownCenter.Location)
		end
	end)

	Trigger.OnEnteredProximityTrigger(BottomBoundary.CenterPosition, WDist.New(5 * 1024), function(a, id)
		if a.Owner == Civilian and a.HasProperty("Move") then
			a.Stop()
			a.Move(BottomTownCenter.Location)
		end
	end)

	UpdateScrinCounter()
	AfterWorldLoaded()
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	AfterTick()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Scrin.Resources = Scrin.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
		end

		if Scrin.HasNoRequiredUnits() then
			if ObjectivePurgeScrin ~= nil and not Nod.IsObjectiveCompleted(ObjectivePurgeScrin) then
				Nod.MarkCompletedObjective(ObjectivePurgeScrin)
			end
			if ObjectiveSaveAllCivilians ~= nil and not Nod.IsObjectiveFailed(ObjectiveSaveAllCivilians) then
				Nod.MarkCompletedObjective(ObjectiveSaveAllCivilians)
			end
		end

		if MissionPlayersHaveNoRequiredUnits() then
			if ObjectivePurgeScrin ~= nil and not Nod.IsObjectiveCompleted(ObjectivePurgeScrin) then
				Nod.MarkFailedObjective(ObjectivePurgeScrin)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()
	end
end

InitScrin = function()
	AutoRepairAndRebuildBuildings(Scrin, 15)
	SetupRefAndSilosCaptureCredits(Scrin)

	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)
end

SpawnWormhole = function()
	ShuffleInPlace(Wormholes)

	local dormantWormholes = Utils.Where(Wormholes, function(w)
		return
			(w.Actor == nil or w.Actor.IsDead)
			and ((w.Phase == 1 and DateTime.GameTime < Phase2Start[Difficulty])
			or (w.Phase == 2 and DateTime.GameTime >= Phase2Start[Difficulty] and DateTime.GameTime < Phase3Start[Difficulty])
			or (w.Phase == 3 and DateTime.GameTime >= Phase3Start[Difficulty])
			or DateTime.GameTime > DateTime.Minutes(25))
	end)

	if #dormantWormholes > 0 then
		local randomDormantWormhole = Utils.Random(dormantWormholes)
		local randomLocation = Utils.Random(randomDormantWormhole.Locations)

		randomDormantWormhole.Actor = Actor.Create("wormhole", true, { Owner = Scrin, Location = randomLocation })
		randomDormantWormhole.SpawnCount = 0
		local camera = Actor.Create("smallcamera", true, { Owner = Nod, Location = randomLocation })
		Beacon.New(Nod, randomDormantWormhole.Actor.CenterPosition)
		Notification("Scrin portal detected. Destroy it before Scrin reinforcements arrive.")
		MediaCA.PlaySound(MissionDir .. "/n_scrinportal.aud", 2)

		UpdateScrinCounter()

		Trigger.AfterDelay(DateTime.Seconds(10), function()
			camera.Destroy()
		end)

		Trigger.OnKilled(randomDormantWormhole.Actor, function(self, killer)
			UpdateScrinCounter()
		end)

		-- Begin spawning units
		Trigger.AfterDelay(WormholeUnitsDelay[Difficulty], function()
			SpawnWormholeUnits(randomDormantWormhole)
		end)

		-- Wormhole is active, spawn another after interval
		Trigger.AfterDelay(WormholeInterval[Difficulty], function()
			SpawnWormhole()
		end)

	-- If there aren't any dormant wormholes, try again every 10 seconds
	else
		Trigger.AfterDelay(DateTime.Seconds(10), function()
			SpawnWormhole()
		end)
	end
end

SpawnWormholeUnits = function(wormhole)

	if wormhole.Actor == nil or wormhole.Actor.IsDead then
		return
	end

	local unitTypes = Utils.Random(WormholeUnitGroups)
	local units = Reinforcements.Reinforce(Scrin, unitTypes, { wormhole.Actor.Location }, 15)

	Utils.Do(units, function(a)
		-- If units have been spawned already, send this group to attack
		if wormhole.SpawnCount > 0 then
			AssaultPlayerBaseOrHunt(a)

		-- Otherwise scatter
		else
			Trigger.AfterDelay(135, function()
				if not a.IsDead then
					a.Scatter()
				end
			end)
			Trigger.AfterDelay(160, function()
				if not a.IsDead then
					a.Scatter()
					TargetSwapChance(a, 10)
					CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
				end
			end)
		end

		Trigger.OnKilled(a, function(self, killer)
			UpdateScrinCounter()
		end)
	end)

	Trigger.AfterDelay(135, function()
		UpdateScrinCounter()
	end)

	wormhole.SpawnCount = wormhole.SpawnCount + 1

	-- Continue spawning units
	Trigger.AfterDelay(WormholeUnitsDelay[Difficulty], function()
		SpawnWormholeUnits(wormhole)
	end)
end

ShuffleInPlace = function(t)
	for i = #t, 2, -1 do
		local j = Utils.RandomInteger(1, i)
		t[i], t[j] = t[j], t[i]
	end
end

UpdateScrinCounter = function()
	Trigger.AfterDelay(1, function()
		local scrinActors = Scrin.GetActors()
		local scrinRemaining = Utils.Where(scrinActors, function(a)
			return not a.IsDead and a.HasProperty("Kill") and not string.match(a.Type, "husk")
		end)

		UserInterface.SetMissionText("Scrin remaining: " .. #scrinRemaining, HSLColor.Yellow)
	end)
end
