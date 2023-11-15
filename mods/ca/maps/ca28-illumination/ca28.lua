
Caves = {
    { WormholeLocation = Cave1Wormhole.Location, PatrolPath = { Cave1Patrol1.Location, Cave1Patrol2.Location, Cave1Patrol1.Location, Cave1Patrol3.Location, Cave1Patrol4.Location, Cave1Patrol5.Location, Cave1Patrol6.Location, Cave1Patrol5.Location, Cave1Patrol4.Location, Cave1Patrol3.Location }, Composition = {} },
    { WormholeLocation = Cave2Wormhole.Location, PatrolPath = { Cave2Patrol1.Location, Cave2Patrol2.Location, Cave2Patrol3.Location, Cave2Patrol4.Location, Cave2Patrol3.Location } },
    { WormholeLocation = Cave3Wormhole.Location, PatrolPath = { Cave3Patrol1.Location, Cave3Patrol2.Location, Cave3Patrol3.Location, Cave3Patrol4.Location, Cave3Patrol3.Location, Cave3Patrol2.Location } },
    { WormholeLocation = Cave5Wormhole.Location, PatrolPath = { Cave5Patrol1.Location, Cave5Patrol2.Location } },
    { WormholeLocation = Cave7Wormhole.Location, PatrolPath = { Cave7Patrol1.Location, Cave7Patrol2.Location, Cave7Patrol1.Location, Cave7Patrol3.Location } },
    { WormholeLocation = Cave8Wormhole.Location, PatrolPath = { Cave8Patrol1.Location, Cave8Patrol2.Location } },
    { WormholeLocation = Cave9Wormhole.Location, PatrolPath = { Cave9Patrol1.Location, Cave9Patrol2.Location, Cave9Patrol1.Location, Cave9Patrol3.Location, Cave9Patrol4.Location, Cave9Patrol3.Location } },
}

MaxContinuousSpawns = {
    easy = 1,
    normal = 2,
    hard = 3
}

ScrinCompositions = {
    easy = {
        { "s1", "s1", "s1", "s3", "s2", "s1", "gscr", { "gunw", "intl" }, { "gunw", "intl" } }
    },
    normal = {
        { "s1", "s1", "s1", "s3", "s2", "s1", "s1", "s3", "gscr", { "gunw", "intl", "shrw" }, { "devo", "dark", "lchr", "corr" }, { "tpod", "gunw", "intl" } }
    },
    hard = {
        { "s1", "s1", "s1", "s3", "s2", "s1", "s1", "s3", "s2", "gscr", "s4", { "gunw", "intl", "shrw" }, { "devo", "dark", "lchr", "corr" }, { "tpod", "rptp" }, { "intl", "devo" } }
    }
}

FinalBattleInfantryList = {
    easy = { "s1", "s1", "s1", "s3", "s1", "gscr" },
    normal = { "s1", "gscr", "s3", "s4", "s1", "s1", "s2", "s1" },
    hard = { "gscr", "s1", "s3", "s4", "s1", "gscr", "s3", "s1" }
}

FinalBattleVehiclesList = {
    easy = { "gunw", "intl", "corr" },
    normal = { "intl", "devo", "corr", "dark", "tpod" },
    hard = { "intl", "tpod", "devo", "dark", "rptp" }
}

FinalBattleInfantryInterval = {
    easy = { Min = DateTime.Seconds(8), Max = DateTime.Seconds(10) },
    normal = { Min = DateTime.Seconds(7), Max = DateTime.Seconds(9) },
    hard = { Min = DateTime.Seconds(6), Max = DateTime.Seconds(8) }
}

FinalBattleVehicleInterval = {
    easy = DateTime.Seconds(30),
    normal = DateTime.Seconds(25),
    hard = DateTime.Seconds(20)
}

WormholeRespawnTime = {
    easy = DateTime.Minutes(4), -- not used
    normal = DateTime.Minutes(3),
    hard = DateTime.Minutes(2)
}

ContinuousSpawnFrequency = {
    easy = DateTime.Seconds(100), -- not used
    normal = DateTime.Seconds(70),
    hard = DateTime.Seconds(40)
}

WorldLoaded = function()
    Scrin = Player.GetPlayer("Scrin")
    Nod = Player.GetPlayer("Nod")
    TibLifeforms = Player.GetPlayer("TibLifeforms")
    Neutral = Player.GetPlayer("Neutral")
	MissionPlayer = Nod
	TimerTicks = 0
    FragmentsAcquired = {}
    FragmentsAcquiredCount = 0
    FragmentsDetected = {}

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Nod)

    ObjectiveFindFragments = Nod.AddObjective("Find the five hidden artifact fragments.")
    ObjectiveKaneSurvives = Nod.AddObjective("Kane must survive.")

    local fragments = TibLifeforms.GetActorsByType("fragment")

    UpdateMissionText()

    Actor.Create("hazmat.upgrade", true, { Owner = Nod })
    Actor.Create("quantum.upgrade", true, { Owner = Nod })
    Actor.Create("cyborgarmor.upgrade", true, { Owner = Nod })
    Actor.Create("cyborgspeed.upgrade", true, { Owner = Nod })

    Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(2)), function()
        Media.DisplayMessage("There are five fragments of an artifact hidden within these caverns. We must find them all, then the assembled artifact will lead us to our goal.", "Kane", HSLColor.FromHex("FF0000"))
        MediaCA.PlaySound("kane_findfragments.aud", 2)
        Trigger.AfterDelay(DateTime.Seconds(2), function()
            Tip("Kane is able to create wormholes which can be used to travel between neighbouring chambers. Only Kane can detect the hidden artifact fragments.")
        end)
    end)

    Utils.Do(fragments, function(fragment)
        local loc = fragment.Location
        local pos = fragment.CenterPosition
        local fragmentId = tostring(fragment)

        Trigger.OnEnteredProximityTrigger(pos, WDist.New(5 * 1024), function(a, id)
            if a.Owner == Nod and a.Type == "kane" then
                FragmentsDetected[fragmentId] = true
            end
        end)

        Trigger.OnEnteredFootprint({ loc }, function(a, id)
            if not fragment.IsDead and a.Owner == Nod and FragmentsDetected[fragmentId] ~= nil and FragmentsAcquired[fragmentId] == nil then
                Trigger.RemoveFootprintTrigger(id)
                fragment.Kill()
                FragmentsAcquired[tostring(fragment)] = true
                FragmentsAcquiredCount = FragmentsAcquiredCount + 1
                Media.PlaySound("fragment.aud")
                Notification("Aritfact fragment found.")
                UpdateMissionText()

                if FragmentsAcquiredCount == 5 then
                    Nod.MarkCompletedObjective(ObjectiveFindFragments)

                    Trigger.AfterDelay(DateTime.Seconds(5), function()
                        CaveShroud1.Destroy()
                        CaveShroud2.Destroy()
                        CaveShroud3.Destroy()
                        CaveShroud4.Destroy()
                        CaveShroud5.Destroy()
                        CaveShroud6.Destroy()
                        CaveShroud7.Destroy()
                        Beacon.New(Nod, HiddenChamberEntrance.CenterPosition)
                        Notification("A hidden chamber has been revealed.")
                        ObjectiveExploreHiddenChamber = Nod.AddObjective("Explore the hidden chamber.")

                        Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(4)), function()
                            Media.DisplayMessage("With the fragments combined the path to our goal is revealed. Now we must get to the chamber before the Scrin.", "Kane", HSLColor.FromHex("FF0000"))
                            MediaCA.PlaySound("kane_fragmentscombined.aud", 2)
                        end)
                    end)
                end
            end
        end)
    end)

    Utils.Do(Caves, function(c)
        SpawnWormhole(c)
        SpawnScrinSquad(c, false)
    end)

    Trigger.OnKilled(Kane, function(self, killer)
        Nod.MarkFailedObjective(ObjectiveKaneSurvives)
    end)

    Trigger.OnCapture(Purifier, function(self, captor, oldOwner, newOwner)
        Actor.Create("purifierlight", true, { Owner = Neutral, Location = Purifier.Location, CenterPosition = Purifier.CenterPosition })
        Media.PlaySound("purification.aud")
        Nod.MarkCompletedObjective(ObjectiveActivatePurifier)
        Trigger.AfterDelay(DateTime.Seconds(1), function()
            Media.DisplayMessage("The Scrin have no doubt located us by now. Protect the device!", "Kane", HSLColor.FromHex("FF0000"))
            MediaCA.PlaySound("kane_protect.aud", 2)
            Trigger.AfterDelay(DateTime.Seconds(2), function()
                ObjectiveDefendPurifier = Nod.AddObjective("Protect the ancient device.")
                InitFinalBattle()
            end)
        end)
    end)

    Trigger.OnKilled(Purifier, function(self, killer)
        Nod.MarkFailedObjective(ObjectiveDefendPurifier)
    end)

    Trigger.OnEnteredProximityTrigger(Purifier.CenterPosition, WDist.New(7 * 1024), function(a, id)
        if a.Owner == Nod and not PurifierFound and Nod.IsObjectiveCompleted(ObjectiveFindFragments) then
            PurifierFound = true
            Trigger.RemoveProximityTrigger(id)
            Beacon.New(Nod, Purifier.CenterPosition)
            ObjectiveActivatePurifier = Nod.AddObjective("Activate the ancient device.")
            Nod.MarkCompletedObjective(ObjectiveExploreHiddenChamber)
            Media.DisplayMessage("We found it! The Scrin rulers believed it to be destroyed long ago, but its creators hid it well. Quickly, let us activate it, we must make sure it still functions.", "Kane", HSLColor.FromHex("FF0000"))
            MediaCA.PlaySound("kane_foundit.aud", 2)
        end
    end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then

	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
        if DoFinalBattleChecks then
            local finalScrinUnits = Map.ActorsInCircle(HiddenChamberCenter.CenterPosition, WDist.New(12 * 1024), function(a) return a.Owner == Scrin and (a.HasProperty("Move") or a.Type == "scrinwormhole") end)
            if #finalScrinUnits == 0 then
                DoFinalBattleChecks = false
                Media.DisplayMessage("Our forces on the surface have triumphed. The device is ours, and soon it will be ready to do what had been intended for it millennia ago. Excellent work commander, our ultimate victory draws ever closer.", "Kane", HSLColor.FromHex("FF0000"))
                MediaCA.PlaySound("kane_victory.aud", 2)
                Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(12)), function()
                    Nod.MarkCompletedObjective(ObjectiveDefendPurifier)
                    Nod.MarkCompletedObjective(ObjectiveKaneSurvives)
                end)
            end
        end
	end
end

UpdateMissionText = function()
    if FragmentsAcquiredCount == 5 then
        UserInterface.SetMissionText("")
    else
        UserInterface.SetMissionText("Artifact fragments collected: " .. FragmentsAcquiredCount .. "/5", HSLColor.Yellow)
    end
end

SpawnWormhole = function(cave)
    cave.Wormhole = Actor.Create("scrinwormhole", true, { Owner = Scrin, Location = cave.WormholeLocation })
    cave.ContinuousSpawn = false
    cave.NumSpawns = 0

    -- if wormhole is destroyed, respawn after a delay (unless on easy)
    Trigger.OnKilled(cave.Wormhole, function(a)
        if Difficulty ~= "easy" then
            Trigger.AfterDelay(WormholeRespawnTime[Difficulty], function()
                SpawnWormhole(cave)
                local currentWormhole = cave.Wormhole
                Trigger.AfterDelay(DateTime.Seconds(10), function()
                    if not currentWormhole.IsDead then
                        SpawnScrinSquad(cave, false)
                    end
                end)
            end)
        end
    end)

    SpawnScrinSquad(cave, true)
end

SpawnScrinSquad = function(cave, continuous)

    -- only spawn when wormhole is active
    if cave.Wormhole.IsDead then
        return
    end

    if continuous then
        Trigger.AfterDelay(ContinuousSpawnFrequency[Difficulty], function()
            SpawnScrinSquad(cave, continuous)
        end)

        -- if continuous spawn isn't active yet (no units killed), defer to next attempt
        if not cave.ContinuousSpawn then
            return
        end

        if cave.NumSpawns < MaxContinuousSpawns[Difficulty] then
            cave.NumSpawns = cave.NumSpawns + 1
        else
            return
        end
    end

    local units = Reinforcements.Reinforce(Scrin, GetSquadComposition(), { cave.WormholeLocation }, 1)

	Utils.Do(units, function(a)
        a.Scatter()
        a.Wait(Utils.RandomInteger(1, 40))
        a.Scatter()
		TargetSwapChance(a, 10)
		ca28_CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
        Trigger.OnIdle(a, function(self)
            a.Patrol(cave.PatrolPath)
            local selfId = tostring(self);
            AlertedUnits[selfId] = nil
        end)
	end)

    -- if any units are killed, activate continuous spawn and reduce the count
    Trigger.OnAllKilled(units, function()
        if not cave.Wormhole.IsDead then
            cave.ContinuousSpawn = true
            cave.NumSpawns = cave.NumSpawns - 1
            if cave.NumSpawns < 0 then
                cave.NumSpawns = 0
            end
        end
    end)
end

ca28_CallForHelpOnDamagedOrKilled = function(actor, range, filter, isMissionPlayerFunc)
	if isMissionPlayerFunc == nil then
		isMissionPlayerFunc = function(p) return p == MissionPlayer end
	end
	Trigger.OnDamaged(actor, function(self, attacker, damage)
		if isMissionPlayerFunc(attacker.Owner) then
			ca28_CallForHelp(self, range, attacker, filter, isMissionPlayerFunc)
		end
	end)
	Trigger.OnKilled(actor, function(self, killer)
		if isMissionPlayerFunc(killer.Owner) then
			ca28_CallForHelp(self, range, killer, filter, isMissionPlayerFunc)
		end
	end)
end

ca28_CallForHelp = function(self, range, attacker, filter, isMissionPlayerFunc)
	if isMissionPlayerFunc == nil then
		isMissionPlayerFunc = function(p) return p == MissionPlayer end
	end
	if isMissionPlayerFunc(self.Owner) then
		return
	end
    if attacker.IsDead then
        return
    end

	local selfId = tostring(self);
	if AlertedUnits[selfId] == nil then
		if not self.IsDead then
			AlertedUnits[selfId] = true
			if filter(self) then
				self.Stop()
				self.AttackMove(attacker.Location)
			end
		end

		local nearbyUnits = Map.ActorsInCircle(self.CenterPosition, range, filter)

		Utils.Do(nearbyUnits, function(nearbyUnit)
			local nearbyUnitId = tostring(nearbyUnit)
			if not nearbyUnit.IsDead and AlertedUnits[nearbyUnitId] == nil then
				AlertedUnits[nearbyUnitId] = true
				nearbyUnit.Stop()
				nearbyUnit.AttackMove(attacker.Location)
			end
		end)
	end
end

InitFinalBattle = function()
    if not FinalBattleStarted then
        FinalBattleStarted = true
        FinalBattleWormholes = { }

        Trigger.AfterDelay(DateTime.Seconds(8), function()
            local finalWormholeLocations = { FinalWormhole1.Location, FinalWormhole2.Location, FinalWormhole3.Location, FinalWormhole4.Location, FinalWormhole5.Location }
            Utils.Do(finalWormholeLocations, function(loc)
                Trigger.AfterDelay(Utils.RandomInteger(25, 150), function()
                    local wormhole = Actor.Create("scrinwormhole", true, { Owner = Scrin, Location = loc })
                    table.insert(FinalBattleWormholes, wormhole)
                    SpawnFinalBattleInfantry(wormhole, 1)
                end)
            end)
            Trigger.AfterDelay(151, function()
                DoFinalBattleChecks = true
                SpawnFinalBattleVehicle(1)
            end)
        end)
    end
end

SpawnFinalBattleInfantry = function(wormhole, nextUnitIndex)
    if not wormhole.IsDead then
        if nextUnitIndex > #FinalBattleInfantryList[Difficulty] then
            nextUnitIndex = 1
        end

        local nextUnit = FinalBattleInfantryList[Difficulty][nextUnitIndex]
        if type(nextUnit) == "table" then
            nextUnit = Utils.Random(nextUnit)
        end

        local units = Reinforcements.Reinforce(Scrin, { nextUnit }, { wormhole.Location }, 1)
        Utils.Do(units, function(u)
            u.AttackMove(HiddenChamberCenter.Location)
        end)

        Trigger.AfterDelay(Utils.RandomInteger(FinalBattleInfantryInterval[Difficulty].Min, FinalBattleInfantryInterval[Difficulty].Max), function()
            SpawnFinalBattleInfantry(wormhole, nextUnitIndex + 1)
        end)
    end
end

SpawnFinalBattleVehicle = function(nextUnitIndex)
    local activeWormholes = Utils.Where(FinalBattleWormholes, function(w) return not w.IsDead end)
    if #activeWormholes > 0 then
        local wormhole = Utils.Random(activeWormholes)

        if nextUnitIndex > #FinalBattleVehiclesList[Difficulty] then
            nextUnitIndex = 1
        end

        local nextUnit = FinalBattleVehiclesList[Difficulty][nextUnitIndex]

        local units = Reinforcements.Reinforce(Scrin, { nextUnit }, { wormhole.Location }, 1)
        Utils.Do(units, function(u)
            u.AttackMove(HiddenChamberCenter.Location)
        end)

        Trigger.AfterDelay(FinalBattleVehicleInterval[Difficulty], function()
            SpawnFinalBattleVehicle(nextUnitIndex + 1)
        end)
    end
end

GetSquadComposition = function()
    local rawComposition = Utils.Random(ScrinCompositions[Difficulty])
    local composition = {}
    Utils.Do(rawComposition, function(c)
        if type(c) == "table" then
            table.insert(composition, Utils.Random(c))
        else
            table.insert(composition, c)
        end
    end)
    return composition
end
