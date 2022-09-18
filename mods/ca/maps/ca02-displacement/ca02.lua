-- Locations and Paths

Convoys = {
	{
		Spawn = { FirstConvoyPath1.Location, FirstConvoyPath2.Location },
		Path = { FirstConvoyPath3.Location, FirstConvoyPath4.Location, FirstConvoyPath5.Location, FirstConvoyPath6.Location, FirstConvoyPath7.Location, FirstConvoyPath8.Location, FirstConvoyPath9.Location, FirstConvoyPath10.Location, FirstConvoyPath11.Location, FirstConvoyPath12.Location, FirstConvoyPath13.Location, FirstConvoyPath14.Location, FirstConvoyPath15.Location, FirstConvoyPath16.Location, FirstConvoyPath17.Location, FirstConvoyPath18.Location },
		FlareWaypoint = FirstConvoyPath2
	},
	{
		Spawn = { SecondConvoyPath1.Location, SecondConvoyPath2.Location },
		Path = { SecondConvoyPath3.Location, SecondConvoyPath4.Location, SecondConvoyPath5.Location, SecondConvoyPath6.Location, SecondConvoyPath7.Location, SecondConvoyPath8.Location, SecondConvoyPath9.Location },
		FlareWaypoint = SecondConvoyFlare
	},
	{
		Spawn = { ThirdConvoyPath1.Location, ThirdConvoyPath2.Location },
		Path = { ThirdConvoyPath3.Location, ThirdConvoyPath4.Location, ThirdConvoyPath5.Location, ThirdConvoyPath6.Location, ThirdConvoyPath7.Location, ThirdConvoyPath8.Location, ThirdConvoyPath9.Location, ThirdConvoyPath10.Location, SecondConvoyPath2.Location, SecondConvoyPath3.Location, SecondConvoyPath4.Location, SecondConvoyPath5.Location, SecondConvoyPath6.Location, SecondConvoyPath7.Location, SecondConvoyPath8.Location, SecondConvoyPath9.Location },
		FlareWaypoint = ThirdConvoyPath2
	},
	{
		Spawn = { FourthConvoyPath1.Location, FourthConvoyPath2.Location },
		Path = { FourthConvoyPath3.Location, FourthConvoyPath4.Location, FourthConvoyPath5.Location, FourthConvoyPath6.Location, FourthConvoyPath7.Location, FourthConvoyPath8.Location, FourthConvoyPath9.Location, FourthConvoyPath10.Location, FourthConvoyPath11.Location, FourthConvoyPath12.Location, FourthConvoyPath13.Location, FourthConvoyPath14.Location, FourthConvoyPath15.Location, FourthConvoyPath16.Location, ThirdConvoyPath6.Location, ThirdConvoyPath7.Location, ThirdConvoyPath8.Location, ThirdConvoyPath9.Location, ThirdConvoyPath10.Location, SecondConvoyPath2.Location, SecondConvoyPath3.Location, SecondConvoyPath4.Location, SecondConvoyPath5.Location, SecondConvoyPath6.Location, SecondConvoyPath7.Location, SecondConvoyPath8.Location, SecondConvoyPath9.Location },
		FlareWaypoint = FourthConvoyPath2
	}
}

ConvoyExits = {
	{ FirstConvoyPath18.Location, CPos.New(FirstConvoyPath18.Location.X - 1, FirstConvoyPath18.Location.Y), CPos.New(FirstConvoyPath18.Location.X + 1, FirstConvoyPath18.Location.Y) },
	{ SecondConvoyPath9.Location, CPos.New(SecondConvoyPath9.Location.X - 1, SecondConvoyPath9.Location.Y), CPos.New(SecondConvoyPath9.Location.X + 1, SecondConvoyPath9.Location.Y) }
}

ScrinAttackAssemblyLocations = { ScrinAttackAssembly1.Location, ScrinAttackAssembly2.Location, ScrinAttackAssembly3.Location }

-- Other Variables

ConvoyUnits = { "truk", "truk", "truk", "truk", "truk" }

MaxLosses = {
	easy = 10,
	normal = 5,
	hard = 0
}

TimeBetweenConvoys = {
	easy = { DateTime.Minutes(1), DateTime.Minutes(8), DateTime.Minutes(5), DateTime.Minutes(4)  },
	normal = { DateTime.Minutes(1), DateTime.Minutes(7), DateTime.Minutes(4), DateTime.Minutes(3) },
	hard = { DateTime.Minutes(1), DateTime.Minutes(6), DateTime.Seconds(150), DateTime.Minutes(3) }
}

ScrinAttackInterval = {
	easy = DateTime.Minutes(4),
	normal = DateTime.Seconds(150),
	hard = DateTime.Seconds(70)
}

ScrinAirAttackInterval = {
	easy = DateTime.Minutes(6),
	normal = DateTime.Minutes(4),
	hard = DateTime.Minutes(2)
}

AdvancedAttackSquadStart = {
	easy = DateTime.Minutes(20),
	normal = DateTime.Minutes(15),
	hard = DateTime.Minutes(10)
}

BasicAttackSquadUnits = { "s1", "s1", "s1", "s3", "s3", "intl.ai2", "intl.ai2", "gunw" }

AdvancedAttackSquadUnits = {
	easy = { "s1", "s1", "s1", "s3", "s3", "intl.ai2", "intl.ai2", "gunw", "gunw", "corr" },
	normal = { "s1", "s1", "s1", "s1", "s3", "s3", "s4", "intl.ai2", "intl.ai2", "gunw", "corr", "devo" },
	hard = { "s1", "s1", "s1", "s1", "s1", "s1", "s2", "s3", "s3", "s4", "intl.ai2", "intl.ai2", "gunw", "corr", "devo", "seek", "tpod" }
}

AirAttackSquadUnits = {
	easy = { "stmr", "stmr" },
	normal = { "stmr", "stmr", "stmr" },
	hard = { "stmr", "stmr", "stmr", "stmr" }
}

-- Setup and Tick

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	England = Player.GetPlayer("England")
	Scrin = Player.GetPlayer("Scrin")
	USSR = Player.GetPlayer("USSR")
	Timer = 0
	TrucksLost = 0
	NextConvoyIdx = 1
	PlayerBaseLocation = PlayerRefinery.Location

	InitObjectives(Greece)
	InitScrin()
	Camera.Position = PlayerBarracks.CenterPosition

	ObjectiveClearPath = Greece.AddObjective("Clear a path for inbound convoys")

	if Difficulty == "hard" then
		ObjectiveProtectConvoys = Greece.AddObjective("Do not lose any convoy trucks")
	else
		ObjectiveProtectConvoys = Greece.AddObjective("Do not lose more than " .. MaxLosses[Difficulty] .. " convoy trucks")
	end

	Trigger.AfterDelay(DateTime.Seconds(15), function()
		InitConvoy()
	end)

	-- When convoy units reach destination, remove them
	Utils.Do(ConvoyExits, function(exitCells)
		Trigger.OnEnteredFootprint(exitCells, function(a, id)
			if a.Owner == England then
				a.Destroy()
			end
		end)
	end)
end

Tick = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		OncePerSecondChecks()
	end
end

OncePerSecondChecks = function()
	if Scrin.Cash < 1000 then
		Scrin.Cash = 2000
	end

	if Timer > 0 then
		if Timer > 25 then
			Timer = Timer - 25
		else
			Timer = 0
		end
		UpdateConvoyCountdown()
	end

	if Greece.HasNoRequiredUnits() then
		if ObjectiveClearPath ~= nil and not Greece.IsObjectiveCompleted(ObjectiveClearPath) then
			Greece.MarkFailedObjective(ObjectiveClearPath)
		end
		if ObjectiveProtectConvoys ~= nil and not Greece.IsObjectiveCompleted(ObjectiveProtectConvoys) then
			Greece.MarkFailedObjective(ObjectiveProtectConvoys)
		end
		if ObjectiveDestroyScrinBase ~= nil and not Greece.IsObjectiveCompleted(ObjectiveDestroyScrinBase) then
			Greece.MarkFailedObjective(ObjectiveDestroyScrinBase)
		end
	end

	if ObjectiveDestroyScrinBase ~= nil and not HasOneOf(Scrin, { "reac", "rea2", "sfac", "proc.scrin", "port", "wsph", "nerv", "grav", "scrt", "srep" }) then
		Greece.MarkCompletedObjective(ObjectiveDestroyScrinBase)
	end
end

-- Functions

UpdateConvoyCountdown = function()
	if Timer == 0 then
		if Difficulty == "hard" then
			UserInterface.SetMissionText("Protect the convoy. All trucks must survive." , HSLColor.White)
		else
			UserInterface.SetMissionText("Protect the convoy. Acceptable losses: " .. TrucksLost .. " / " ..  MaxLosses[Difficulty] , HSLColor.White)
		end
	else
		UserInterface.SetMissionText("Next convoy arrives in " .. Utils.FormatTime(Timer), HSLColor.Yellow)
	end
end

InitConvoy = function()
	local nextConvoy = Convoys[NextConvoyIdx]

	-- Spawn and announce flare
	ConvoyFlare = Actor.Create("flare", true, { Owner = Greece, Location = nextConvoy["FlareWaypoint"].Location })
	Media.PlaySpeechNotification(Greece, "SignalFlare")
	Beacon.New(Greece, nextConvoy["FlareWaypoint"].CenterPosition)

	-- Set the timer
	Timer = TimeBetweenConvoys[Difficulty][NextConvoyIdx]
	UpdateConvoyCountdown()

	-- Schedule convoy to arrive after timer expires
	Trigger.AfterDelay(Timer, function()
		ConvoyFlare.Destroy()
		UpdateConvoyCountdown()
		Media.PlaySpeechNotification(Greece, "ConvoyApproaching")

		local trucks = Reinforcements.Reinforce(England, ConvoyUnits, nextConvoy["Spawn"], 50, function(truck)
			Utils.Do(nextConvoy["Path"], function(waypoint)
				truck.Move(waypoint)
			end)
		end)

		Utils.Do(trucks, function(truck)
			Trigger.OnKilled(truck, function()
				TrucksLost = TrucksLost + 1
				Media.PlaySpeechNotification(Greece, "ConvoyUnitLost")
				Media.PlaySoundNotification(Greece, "AlertBuzzer")

				if Timer == 0 then
					UpdateConvoyCountdown()
				end

				if TrucksLost > MaxLosses[Difficulty] then
					Greece.MarkFailedObjective(ObjectiveClearPath)
					Greece.MarkFailedObjective(ObjectiveProtectConvoys)
				end
			end)

			Trigger.OnRemovedFromWorld(truck, function()
				local numTrucks = #England.GetActorsByType("truk")
				if numTrucks == 0 then
					NextConvoyIdx = NextConvoyIdx + 1
					if NextConvoyIdx <= #Convoys then
						UserInterface.SetMissionText("Awaiting next convoy.")
						Trigger.AfterDelay(DateTime.Seconds(15), function()
							InitConvoy()
						end)
					else
						ObjectiveDestroyScrinBase = Greece.AddObjective("Destroy the alien stronghold")
						Greece.MarkCompletedObjective(ObjectiveClearPath)
						Greece.MarkCompletedObjective(ObjectiveProtectConvoys)
						UserInterface.SetMissionText("Destroy the alien stronghold", HSLColor.Yellow)
					end
				end
			end)
		end)
	end)
end

InitScrin = function()
	AutoRepairBuildings(Scrin)

	StormriderAttacker1.Attack(PlayerRefinery)
	StormriderAttacker2.Attack(PlayerRefinery)

	StormriderPatroller1.Patrol({ ScrinAirPatrol1a.Location, ScrinAirPatrol1b.Location, ScrinAirPatrol1c.Location, ScrinAirPatrol1b.Location })
	StormriderPatroller2.Patrol({ ScrinAirPatrol1a.Location, ScrinAirPatrol1b.Location, ScrinAirPatrol1c.Location, ScrinAirPatrol1b.Location })

	StormriderPatroller3.Patrol({ ScrinAirPatrol2a.Location, ScrinAirPatrol2b.Location })
	StormriderPatroller4.Patrol({ ScrinAirPatrol2a.Location, ScrinAirPatrol2b.Location })

	SeekerPatroller1.Patrol({ SeekerPatrol1a.Location, SeekerPatrol1b.Location })
	SeekerPatroller2.Patrol({ SeekerPatrol1a.Location, SeekerPatrol1b.Location })
	SeekerPatroller3.Patrol({ SeekerPatrol1a.Location, SeekerPatrol1b.Location })

	Trigger.AfterDelay(ScrinAttackInterval[Difficulty], function()
		ScrinAttackWave()
	end)

	Trigger.AfterDelay(ScrinAirAttackInterval[Difficulty], function()
		ScrinAirAttackWave()
	end)

	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	TargetSwapChance(scrinGroundAttackers, Scrin, 10)
	CallForHelpOnDamaged(scrinGroundAttackers)

	if Difficulty == "hard" then
		Trigger.AfterDelay(DateTime.Minutes(20), SendDevastators)
	end
end

ScrinAttackWave = function()
	if DateTime.GameTime >= AdvancedAttackSquadStart[Difficulty] then
		attackSquad = AdvancedAttackSquadUnits[Difficulty]
	else
		attackSquad = BasicAttackSquadUnits
	end

	Scrin.Build(attackSquad, function(units)
		Utils.Do(units, function(unit)
			IdleHunt(unit)
			Trigger.OnPassengerExited(unit, function(t, p)
				IdleHunt(p)
			end)
		end)

		TargetSwapChance(units, Scrin, 10)

		local assemblyPoint = Utils.Random(ScrinAttackAssemblyLocations)

		Utils.Do(units, function(unit)
			if not unit.IsDead then
				unit.AttackMove(assemblyPoint)
				unit.AttackMove(PlayerBaseLocation)
			end
		end)

		Trigger.AfterDelay(ScrinAttackInterval[Difficulty], ScrinAttackWave)
	end)
end

ScrinAirAttackWave = function()
	Scrin.Build(AirAttackSquadUnits[Difficulty], function(units)
		Utils.Do(units, function(unit)
			Trigger.OnIdle(unit, function()
				local target = ChooseRandomBuildingTarget(unit, Greece)
				if target ~= nil then
					unit.Attack(target)
				end
			end)
		end)

		local assemblyPoint = Utils.Random(ScrinAttackAssemblyLocations)

		Utils.Do(units, function(unit)
			unit.AttackMove(assemblyPoint)
			local target = ChooseRandomBuildingTarget(unit, Greece)
			if target ~= nil then
				unit.Attack(target)
			end
		end)
	end)

	Trigger.AfterDelay(ScrinAirAttackInterval[Difficulty], ScrinAirAttackWave)
end

CallForHelpOnDamaged = function(actors)
	Utils.Do(actors, function(actor)
		Trigger.OnDamaged(actor, function(self, attacker, damage)

			if not self.HasTag("helpCalled") then
				self.AddTag("helpCalled")
				local nearbyUnits = Map.ActorsInCircle(self.CenterPosition, WDist.New(5120), IsScrinGroundHunterUnit)

				Utils.Do(nearbyUnits, function(nearbyUnit)
					if not actor.IsDead and not actor.HasTag("idleHunt") then
						nearbyUnit.AddTag("idleHunt")
						IdleHunt(nearbyUnit)
					end
				end)
			end
		end)
	end)
end

SendDevastators = function()
	if not GravityStabilizer1.IsDead and GravityStabilizer1.Owner == Scrin then
		GravityStabilizer1.Produce("deva")
	end

	if not GravityStabilizer2.IsDead and GravityStabilizer2.Owner == Scrin then
		GravityStabilizer1.Produce("deva")
	end

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		IdleDevastators = Utils.Where(Scrin.GetActorsByType("deva"), function(actor) return actor.IsIdle end)
		Utils.Do(IdleDevastators, function(unit)
			local target = ChooseRandomBuildingTarget(unit, Greece)
			if target ~= nil then
				unit.Attack(target)
				IdleHunt(unit)
			end
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(2), SendDevastators)
end

-- Filters

IsScrinGroundHunterUnit = function(actor)
	return actor.Owner == Scrin and actor.HasProperty("Move") and not actor.HasProperty("Land") and actor.HasProperty("Hunt")
end
