-- Locations

SovietBorder = { CPos.New(9,32), CPos.New(10,32), CPos.New(11,32), CPos.New(12,32), CPos.New(13,32), CPos.New(14,32), CPos.New(15,32), CPos.New(16,32), CPos.New(17,32), CPos.New(18,32) }
SovietMainBaseEntrance = { CPos.New(39,3), CPos.New(39,4), CPos.New(39,5), CPos.New(39,6), CPos.New(39,7), CPos.New(39,8), CPos.New(39,9), CPos.New(39,10), CPos.New(39,11), CPos.New(39,12), CPos.New(39,13) }
SovietChronosphereLocation = CPos.New(60,25)
TreesToTransform = { TreeToTransform1, TreeToTransform2, TreeToTransform3, TreeToTransform4 }
WormholeSpawns = { WormholeSpawn1.Location, WormholeSpawn2.Location, WormholeSpawn3.Location, WormholeSpawn4.Location, WormholeSpawn5.Location }

AttackPaths =
{
	{ AttackWaypoint1.Location, AttackWaypoint2.Location, AttackWaypoint3.Location, AttackWaypoint4.Location }
}

WestPatrolPath = { WestPatrolWaypoint1.Location, WestPatrolWaypoint2.Location }

-- Other Variables

WestPatrolUnits = { WestPatrolUnit1, WestPatrolUnit2, WestPatrolUnit3, WestPatrolUnit4 }

BasicAttackSquadUnits = { "e1", "e1", "e1", "e2", "e3", "e4", "3tnk", "btr" }

AdvancedAttackSquadUnits = {
	easy = { "e1", "e1", "shok", "shok", "e1", "e2", "e3", "e3", "e4", "4tnk", "btr" },
	normal = { "e1", "e1", "shok", "shok", "e1", "e2", "e3", "e3", "e4", "3tnk", "4tnk", "katy" },
	hard = { "e1", "e1", "e3", "shok", "e1", "shok", "e1", "e2", "e3", "e3", "e4", "3tnk", "4tnk", "btr", "katy", "ttra" }
}

ScrinInfantrySquads = {
	easy = { "s1", "s1", "s1", "s3" },
	normal = { "s1", "s1", "s1", "s2", "s3" },
	hard = { "s1", "s1", "s1", "s2", "s3", "s4" }
}

ScrinVehicleTypes = {
	easy = { "gunw", "seek", "intl", "gscr" },
	normal = { "gunw", "seek", "intl", "corr" },
	hard = { "seek", "corr", "devo", "ruin", "tpod" }
}

AttackSquadDelay =
{
	easy = DateTime.Seconds(130),
	normal = DateTime.Seconds(100),
	hard = DateTime.Seconds(70)
}

AttackSquadInterval =
{
	easy = DateTime.Seconds(40),
	normal = DateTime.Seconds(30),
	hard = DateTime.Seconds(20)
}

WestDefenseSquadInterval =
{
	easy = DateTime.Seconds(45),
	normal = DateTime.Seconds(30),
	hard = DateTime.Seconds(15)
}

ScrinInvasionInterval =
{
	easy = DateTime.Seconds(18),
	normal = DateTime.Seconds(12),
	hard = DateTime.Seconds(12)
}

ScrinVehiclesIntervalMultiplier =
{
	easy = 4,
	normal = 4,
	hard = 3
}

AdvancedAttackSquadStart =
{
	easy = 22500,
	normal = 15000,
	hard = 10500
}

EvacuationTime =
{
	easy = 210,
	normal = 270,
	hard = 330
}

HaloDropInterval =
{
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(2),
	hard = DateTime.Minutes(1)
}

NavalDropInterval =
{
	normal = DateTime.Minutes(4),
	hard = DateTime.Minutes(2)
}

-- Setup and Tick

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	Scrin = Player.GetPlayer("Scrin")
	EstablishBase = Greece.AddObjective("Establish a base.")

	Trigger.OnKilled(Church, function()
		Actor.Create("moneycrate", true, { Owner = Greece, Location = Church.Location })
	end)

	Trigger.OnDiscovered(SovietChronosphere, function()
		if not chronosphereDiscovered then
			chronosphereDiscovered = true
			Media.DisplayMessage("Commander, the Soviets have been attempting to reverse engineer stolen Chronosphere technology! Use whatever means necessary to cease their experiments.", "HQ")
			if InvestigateArea == nil then
				InvestigateArea = Greece.AddObjective("Investigate the area.")
			end
			CaptureOrDestroyChronosphere = Greece.AddObjective("Capture or destroy the Soviet Chronosphere.")
			Greece.MarkCompletedObjective(InvestigateArea)
			ChronoCamera = Actor.Create("smallcamera", true, { Owner = Greece, Location = SovietChronosphere.Location })

			Trigger.AfterDelay(1, function()
				ChronoCamera.Destroy()
			end)
		end
	end)

	InitObjectives(Greece)
	InitUSSR()
	Camera.Position = PlayerMcv.CenterPosition
end

Tick = function()
	if not baseEstablished and CheckForConYard() then
		baseEstablished = true
		if InvestigateArea == nil then
			InvestigateArea = Greece.AddObjective("Investigate the area.")
		end
		Greece.MarkCompletedObjective(EstablishBase)
	end

	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		OncePerSecondChecks()
	end
end

OncePerSecondChecks = function()
	if Greece.HasNoRequiredUnits() then
		if EstablishBase ~= nil and not Greece.IsObjectiveCompleted(EstablishBase) then
			Greece.MarkFailedObjective(EstablishBase)
		end
		if InvestigateArea ~= nil and not Greece.IsObjectiveCompleted(InvestigateArea) then
			Greece.MarkFailedObjective(InvestigateArea)
		end
		if CaptureOrDestroyChronosphere ~= nil and not Greece.IsObjectiveCompleted(CaptureOrDestroyChronosphere) then
			Greece.MarkFailedObjective(CaptureOrDestroyChronosphere)
		end
		if DefendUntilEvacuation ~= nil and not Greece.IsObjectiveCompleted(DefendUntilEvacuation) then
			Greece.MarkFailedObjective(DefendUntilEvacuation)
		end
	end

	if USSR.Cash < 1000 then
		USSR.Cash = 2000
	end
end

-- Functions

CheckForConYard = function()
	local ConYards = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "fact" and actor.Owner == Greece end)
	return #ConYards >= 1
end

InitUSSR = function()
	-- Main base barracks as primary
	SovietBarracks.IsPrimaryBuilding = true

	-- Begin main attacks after difficulty based delay
	Trigger.AfterDelay(AttackSquadDelay[Difficulty], ProduceAttackSquad)

	-- Eastern Halo drops start at 10 mins
	Trigger.AfterDelay(DateTime.Minutes(10), function()
		local eastHaloDropEntryPath = { CPos.New(EastHaloDrop.Location.X + 35, EastHaloDrop.Location.Y - 25), EastHaloDrop.Location }
		DoHaloDrop(eastHaloDropEntryPath)
	end)

	-- Western Halo drops start at 10:40 (hard only)
	if Difficulty == "hard" then
		Trigger.AfterDelay(DateTime.Seconds(640), function()
			local westHaloDropEntryPath = { CPos.New(WestHaloDrop.Location.X - 35, WestHaloDrop.Location.Y - 25), WestHaloDrop.Location }
			DoHaloDrop(westHaloDropEntryPath)
		end)
	end

	-- Beach drop at 12:00
	if Difficulty ~= "easy" then
		Trigger.AfterDelay(DateTime.Minutes(12), DoNavalDrop)
	end

	-- Set western patrol
	Utils.Do(WestPatrolUnits, function(unit)
		if not unit.IsDead then
			unit.Patrol(WestPatrolPath, true, 100)
		end
	end)

	Trigger.OnEnteredFootprint(SovietBorder, function(a, id)
		-- Any Soviet/Scrin units that cross the border go into hunt mode
		if a.Owner == USSR or a.Owner == Scrin then
			ClearTriggersStopAndHunt(a)
		end

		-- On player crossing Soviet border start making infantry at western barracks
		if not sovietBorderCrossed and a.Owner == Greece then
			sovietBorderCrossed = true
			ProduceWestDefenseSquad()
		end
	end)

	-- On destroying or capturing Chronosphere
	Trigger.OnKilled(SovietChronosphere, function()
		Actor.Create("pdox.crossrip", true, { Owner = USSR, Location = SovietChronosphereLocation})
		InterdimensionalCrossrip()
	end)

	Trigger.OnCapture(SovietChronosphere, function()
		SovietChronosphere.Kill()
	end)

	AutoRepairBuildings(USSR)
end

ProduceWestDefenseSquad = function()
	if SovietWestBarracks.IsDead or SovietWestBarracks.Owner ~= USSR then
		return
	end

	local westDefenseUnits = { "e1", "e2", "e1", "e3" }

	SovietWestBarracks.Build(westDefenseUnits, function(units)
		Utils.Do(units, function(unit)
			IdleHunt(unit)
		end)
		Trigger.AfterDelay(WestDefenseSquadInterval[Difficulty], ProduceWestDefenseSquad)
	end)
end

ProduceAttackSquad = function()
	if SovietBarracks.IsDead or SovietBarracks.Owner ~= USSR or SovietWarFactory.IsDead or SovietWarFactory.Owner ~= USSR then
		return
	end

	if DateTime.GameTime  >= AdvancedAttackSquadStart[Difficulty] then
		attackSquad = AdvancedAttackSquadUnits[Difficulty]
	else
		attackSquad = BasicAttackSquadUnits
	end

	USSR.Build(attackSquad, function(units)
		SendAttackSquad(units)
		Trigger.AfterDelay(AttackSquadInterval[Difficulty], ProduceAttackSquad)
	end)
end

SendAttackSquad = function(units)
	Utils.Do(units, function(unit)
		IdleHunt(unit)
	end)

	local attackPath = Utils.Random(AttackPaths)

	Utils.Do(units, function(unit)
		if not unit.IsDead then
			unit.Patrol(attackPath, true, 50)
		end
	end)
end

ScrinInvasion = function()
	local wormholes = Scrin.GetActorsByType("wormhole")

	Utils.Do(wormholes, function(wormhole)
		local units = Reinforcements.Reinforce(Scrin, ScrinInfantrySquads[Difficulty], { wormhole.Location }, 1)
		Utils.Do(units, function(unit)
			unit.Scatter()
			Trigger.AfterDelay(5, function()
				AssaultPlayerBase(unit)
			end)
			IdleHunt(unit)
		end)
	end)

	Trigger.AfterDelay(ScrinInvasionInterval[Difficulty], ScrinInvasion)
	if not invasionStarted then
		Trigger.AfterDelay(ScrinInvasionInterval[Difficulty] * 2, CreateScrinVehicles)
		invasionStarted = true
	end
end

CreateScrinVehicles = function()
	local wormholes = Scrin.GetActorsByType("wormhole")

	Utils.Do(wormholes, function(wormhole)
		local units = Reinforcements.Reinforce(Scrin, { Utils.Random(ScrinVehicleTypes[Difficulty]) }, { wormhole.Location }, 1)
		Utils.Do(units, function(unit)
			unit.Scatter()
			Trigger.AfterDelay(5, function()
				AssaultPlayerBase(unit)
			end)
			IdleHunt(unit)
		end)
	end)

	Trigger.AfterDelay(ScrinInvasionInterval[Difficulty] * ScrinVehiclesIntervalMultiplier[Difficulty], CreateScrinVehicles)
end

InterdimensionalCrossrip = function()
	if crossRipped then
		return
	end

	crossRipped = true

	Lighting.Ambient = 0.8
	Lighting.Red = 0.8
	Lighting.Blue = 0.9
	Lighting.Green = 1.2

	Trigger.AfterDelay(1, SpawnWormhole)
	Trigger.AfterDelay(2, SpawnTibTree)

	Media.DisplayMessage("What in God's name!? Fall back to your base, prepare for evacuation. We'll need whatever intel you found to fix .. whatever this is.", "HQ")
	DefendUntilEvacuation = Greece.AddObjective("Defend your base until evacuation is prepared.")

	if CaptureOrDestroyChronosphere ~= nil then
		Greece.MarkCompletedObjective(CaptureOrDestroyChronosphere)
	end

	DateTime.TimeLimit = DateTime.Seconds(EvacuationTime[Difficulty])

	Trigger.OnTimerExpired(function()
		if Greece.HasNoRequiredUnits() then
			Greece.MarkFailedObjective(DefendUntilEvacuation)
		else
			Greece.MarkCompletedObjective(DefendUntilEvacuation)
		end
	end)

	Trigger.AfterDelay(12, ScrinInvasion)

	Actor.Create("flare", true, { Owner = Greece, Location = Evac1.Location })
	Actor.Create("flare", true, { Owner = Greece, Location = Evac2.Location })
	Actor.Create("flare", true, { Owner = Greece, Location = Evac3.Location })

	Trigger.AfterDelay(DateTime.Seconds(EvacuationTime[Difficulty] - 12), function()
		Reinforcements.ReinforceWithTransport(Greece, "nhaw.paradrop", nil, { CPos.New(Evac1.Location.X - 10, Evac1.Location.Y + 15), CPos.New(Evac1.Location.X, Evac1.Location.Y - 1) })
		Reinforcements.ReinforceWithTransport(Greece, "nhaw.paradrop", nil, { CPos.New(Evac2.Location.X - 10, Evac2.Location.Y + 15), CPos.New(Evac2.Location.X, Evac2.Location.Y - 1) })
		Reinforcements.ReinforceWithTransport(Greece, "nhaw.paradrop", nil, { CPos.New(Evac2.Location.X - 10, Evac3.Location.Y + 15), CPos.New(Evac3.Location.X, Evac3.Location.Y - 1) })
	end)

	Trigger.AfterDelay(DateTime.Seconds(EvacuationTime[Difficulty]) - DateTime.Seconds(30), SendDevastators)
end

SpawnTibTree = function()
	local tibTree = TreesToTransform[#TreesToTransform]
	local loc = tibTree.Location
	tibTree.Destroy()
	Actor.Create("split2", true, { Owner = Scrin, Location = loc})
	table.remove(TreesToTransform, #TreesToTransform)

	if #TreesToTransform > 0 then
		Trigger.AfterDelay(1, SpawnTibTree)
	end
end

SpawnWormhole = function()
	local loc = WormholeSpawns[#WormholeSpawns]
	Actor.Create("wormhole", true, { Owner = Scrin, Location = loc})
	table.remove(WormholeSpawns, #WormholeSpawns)

	if #WormholeSpawns > 0 then
		Trigger.AfterDelay(1, SpawnWormhole)
	end
end

IdleHunt = function(actor)
	if actor.HasProperty("Hunt") and not actor.IsDead then
		Trigger.OnIdle(actor, actor.Hunt)
	end
end

AssaultPlayerBase = function(actor)
	if not actor.IsDead then
		actor.AttackMove(CPos.New(18,41))
	end
end

DoHaloDrop = function(entryPath)
	if crossRipped then
		return
	end

	local haloDropUnits = { "e1", "e1", "e1", "e2", "e3", "e4" }
	if Difficulty == "hard" and DateTime.GameTime > DateTime.Minutes(15) then
		haloDropUnits = { "e1", "e1", "e1", "e1", "e2", "e2", "e3", "e3", "e4", "shok" }
	end

	Reinforcements.ReinforceWithTransport(USSR, "halo.paradrop", haloDropUnits, entryPath, nil, function(transport, cargo)
		if not transport.IsDead then
			transport.UnloadPassengers()
			Utils.Do(cargo, IdleHunt)

			Trigger.AfterDelay(DateTime.Seconds(5), function()
				if not transport.IsDead then
					transport.Move(entryPath[1])
				end
			end)
			Trigger.AfterDelay(DateTime.Seconds(12), function()
				if not transport.IsDead then
					transport.Destroy()
				end
			end)
		end
	end)

	Trigger.AfterDelay(HaloDropInterval[Difficulty], function()
		DoHaloDrop(entryPath)
	end)
end

DoNavalDrop = function()
	if crossRipped then
		return
	end

	local navalDropPath = { CPos.New(NavalDrop.Location.X - 3, NavalDrop.Location.Y - 1), NavalDrop.Location }
	local navalDropUnits = { "3tnk", "v2rl", "3tnk" }

	Reinforcements.ReinforceWithTransport(USSR, "lst", navalDropUnits, navalDropPath, { navalDropPath[2], navalDropPath[1] }, function(transport, cargo)
		if not transport.IsDead then
			transport.UnloadPassengers()
			Utils.Do(cargo, function(a)
				Trigger.OnAddedToWorld(a, function()
					AssaultPlayerBase(a)
					IdleHunt(a)
				end)
			end)
		end
	end)

	Trigger.AfterDelay(NavalDropInterval[Difficulty], DoNavalDrop)
end

SendDevastators = function()
	local units = Reinforcements.Reinforce(Scrin, { "deva", "deva", "deva" }, { CPos.New(WestHaloDrop.Location.X - 25, WestHaloDrop.Location.Y - 25) }, 5)
	Utils.Do(units, function(unit)
		Trigger.AfterDelay(5, function()
			AssaultPlayerBase(unit)
		end)
		IdleHunt(unit)
	end)
end
