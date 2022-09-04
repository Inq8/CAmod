-- Locations

SovietBorder = { CPos.New(8,37), CPos.New(9,37), CPos.New(10,37), CPos.New(11,37), CPos.New(12,37), CPos.New(13,37), CPos.New(14,37), CPos.New(15,37), CPos.New(16,37), CPos.New(17,37), CPos.New(18,37), CPos.New(19,37), CPos.New(20,37), CPos.New(21,37) }
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

BasicAttackSquadUnits = { "e1", "e1", "e1", "e2", "e3", "e4", "3tnk", "btr" }
AdvancedAttackSquadUnits = { "e1", "e1", "shok", "shok", "e1", "e2", "e3", "e3", "e4", "4tnk", "btr", "katy" }
WestPatrolUnits = { WestPatrolUnit1, WestPatrolUnit2, WestPatrolUnit3, WestPatrolUnit4 }

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
	hard = DateTime.Seconds(8)
}

AdvancedAttackSquadStart =
{
	easy = 22500,
	normal = 15000,
	hard = 10500
}

EvacuationTime =
{
	easy = 180,
	normal = 240,
	hard = 300
}

-- Setup and Tick

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	Scrin = Player.GetPlayer("Scrin");
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
		Media.PlaySoundNotification(Greece, "AlertBleep")
	end

	if DateTime.GameTime > DateTime.Seconds(30) and Greece.HasNoRequiredUnits() then
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

	-- Drop after 10 mins
	Trigger.AfterDelay(DateTime.Minutes(10), DoHaloDrop)

	-- Set western patrol
	Utils.Do(WestPatrolUnits, function(unit)
		if not unit.IsDead then
			unit.Patrol(WestPatrolPath, true, 100)
		end
	end)

	-- Any Soviet units that cross the border go into hunt mode
	Trigger.OnEnteredFootprint(SovietBorder, function(a, id)
		if a.Owner == USSR then
			ClearTriggersStopAndHunt(a)
		end
	end)

	-- On damaging first flame tower, start making infantry at western barracks
	Trigger.OnDamaged(SovietWestFlameTower1, function()
		if not westAttacked then
			westAttacked = true
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
		attackSquad = AdvancedAttackSquadUnits
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
	local scrinUnits = { "s1", "s1", "s1", "s3" }
	local wormholes = Scrin.GetActorsByType("wormhole")

	Utils.Do(wormholes, function(wormhole)
		local units = Reinforcements.Reinforce(Scrin, scrinUnits, { wormhole.Location }, 1)
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
		Trigger.AfterDelay(ScrinInvasionInterval[Difficulty] * 2, ScrinVehicles)
		invasionStarted = true
	end
end

ScrinVehicles = function()
	local scrinUnits = { "gunw", "seek", "intl", "gscr" }
	local wormholes = Scrin.GetActorsByType("wormhole")

	Utils.Do(wormholes, function(wormhole)
		local units = Reinforcements.Reinforce(Scrin, { Utils.Random(scrinUnits) }, { wormhole.Location }, 1)
		Utils.Do(units, function(unit)
			unit.Scatter()
			Trigger.AfterDelay(5, function()
				AssaultPlayerBase(unit)
			end)
			IdleHunt(unit)
		end)
	end)

	Trigger.AfterDelay(ScrinInvasionInterval[Difficulty] * 4, ScrinVehicles)
end

ClearTriggersStopAndHunt = function(a)
	if a.HasProperty("Hunt") and not a.IsDead then
		Trigger.ClearAll(a)
		a.Stop()
		a.Hunt()
	end
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

	Media.PlaySoundNotification(Greece, "AlertBleep")
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
		actor.AttackMove(CPos.New(25,48))
	end
end

DoHaloDrop = function()
	local entryPath = { CPos.New(HaloDrop.Location.X + 35, HaloDrop.Location.Y - 25), HaloDrop.Location }

	Reinforcements.ReinforceWithTransport(USSR, "halo.paradrop", { "e1", "e1", "e1", "e2", "e3", "e4" }, entryPath, nil, function(transport, cargo)
		transport.UnloadPassengers()
		Utils.Do(cargo, IdleHunt)
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			transport.Move(entryPath[1])
		end)
		Trigger.AfterDelay(DateTime.Seconds(12), function()
			transport.Destroy()
		end)
	end)
end
