SavedCompositions = { }
SavedCash = { }

WorldLoaded = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")

    if Multi0 ~= nil then
        StartingCash = Multi0.Cash
    elseif Multi1 ~= nil then
        StartingCash = Multi1.Cash
    elseif Multi2 ~= nil then
        StartingCash = Multi2.Cash
    elseif Multi3 ~= nil then
        StartingCash = Multi3.Cash
    else
        StartingCash = 20000
    end

    Trigger.OnEnteredFootprint({ ResetBottomWH.Location }, function(a, id)
        Media.DisplayMessage("Resetting players 1/2...", "Notification", HSLColor.FromHex("FF0000"))
        Reset({ Multi0, Multi1 })
    end)

    Trigger.OnEnteredFootprint({ SaveBottomWH.Location }, function(a, id)
        Media.DisplayMessage("Player 1/2 compositions saved.", "Notification", HSLColor.FromHex("00FF00"))
        Save({ Multi0, Multi1 })
    end)

    Trigger.OnEnteredFootprint({ RestoreBottomWH.Location }, function(a, id)
        Media.DisplayMessage("Restoring player 1/2 compositions...", "Notification", HSLColor.FromHex("00FFFF"))
        Restore({ Multi0, Multi1 })
    end)

    Trigger.OnEnteredFootprint({ Reset1WH.Location }, function(a, id)
        Media.DisplayMessage("Resetting player 1...", "Notification", HSLColor.FromHex("FF0000"))
        Reset({ Multi0 })
    end)

    Trigger.OnEnteredFootprint({ Save1WH.Location }, function(a, id)
        Media.DisplayMessage("Player 1 composition saved.", "Notification", HSLColor.FromHex("00FF00"))
        Save({ Multi0 })
    end)

    Trigger.OnEnteredFootprint({ Restore1WH.Location }, function(a, id)
        Media.DisplayMessage("Restoring player 1 composition...", "Notification", HSLColor.FromHex("00FFFF"))
        Restore({ Multi0 })
    end)

    Trigger.OnEnteredFootprint({ Reset2WH.Location }, function(a, id)
        Media.DisplayMessage("Resetting player 2...", "Notification", HSLColor.FromHex("FF0000"))
        Reset({ Multi1 })
    end)

    Trigger.OnEnteredFootprint({ Save2WH.Location }, function(a, id)
        Media.DisplayMessage("Player 2 composition saved.", "Notification", HSLColor.FromHex("00FF00"))
        Save({ Multi1 })
    end)

    Trigger.OnEnteredFootprint({ Restore2WH.Location }, function(a, id)
        Media.DisplayMessage("Restoring player 2 composition...", "Notification", HSLColor.FromHex("00FFFF"))
        Restore({ Multi1 })
    end)

    Trigger.OnEnteredFootprint({ ResetTopWH.Location }, function(a, id)
        Media.DisplayMessage("Resetting players 3/4...", "Notification", HSLColor.FromHex("FF0000"))
        Reset({ Multi2, Multi3 })
    end)

    Trigger.OnEnteredFootprint({ SaveTopWH.Location }, function(a, id)
        Media.DisplayMessage("Player 3/4 compositions saved.", "Notification", HSLColor.FromHex("00FF00"))
        Save({ MMulti2, Multi3 })
    end)

    Trigger.OnEnteredFootprint({ RestoreTopWH.Location }, function(a, id)
        Media.DisplayMessage("Restoring player 3/4 compositions...", "Notification", HSLColor.FromHex("00FFFF"))
        Restore({ Multi2, Multi3 })
    end)

    Trigger.OnEnteredFootprint({ Reset3WH.Location }, function(a, id)
        Media.DisplayMessage("Resetting player 3...", "Notification", HSLColor.FromHex("FF0000"))
        Reset({ Multi2 })
    end)

    Trigger.OnEnteredFootprint({ Save3WH.Location }, function(a, id)
        Media.DisplayMessage("Player 3 composition saved.", "Notification", HSLColor.FromHex("00FF00"))
        Save({ Multi2 })
    end)

    Trigger.OnEnteredFootprint({ Restore3WH.Location }, function(a, id)
        Media.DisplayMessage("Restoring player 3 composition...", "Notification", HSLColor.FromHex("00FFFF"))
        Restore({ Multi2 })
    end)

    Trigger.OnEnteredFootprint({ Reset4WH.Location }, function(a, id)
        Media.DisplayMessage("Resetting player 4...", "Notification", HSLColor.FromHex("FF0000"))
        Reset({ Multi3 })
    end)

    Trigger.OnEnteredFootprint({ Save4WH.Location }, function(a, id)
        Media.DisplayMessage("Player 4 composition saved.", "Notification", HSLColor.FromHex("00FF00"))
        Save({ Multi3 })
    end)

    Trigger.OnEnteredFootprint({ Restore4WH.Location }, function(a, id)
        Media.DisplayMessage("Restoring player 4 composition...", "Notification", HSLColor.FromHex("00FFFF"))
        Restore({ Multi3 })
    end)

    RestoreTrucks({ Multi0, Multi1, Multi2, Multi3 })
end

Tick = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
        -- Do nothing
    end
end

IsUpgrade = function(a)
    return string.find(a.Type, ".upgrade") or string.find(a.Type, ".strat")
end

KillTrucks = function(players)
    Utils.Do(players, function(p)
        if p ~= nil then
            local trucks = p.GetActorsByType("truk")
            Utils.Do(trucks, function(a)
                a.Destroy()
            end)
        end
    end)
end

RestoreTrucks = function(players)
    Utils.Do(players, function(p)
        if p ~= nil and p == Multi0 then
            Actor.Create("truk", true, { Owner = Multi0, Location = Truck1A.Location, Facing = Angle.West })
            Actor.Create("truk", true, { Owner = Multi0, Location = Truck1B.Location, Facing = Angle.West })
            Actor.Create("truk", true, { Owner = Multi0, Location = Truck1C.Location, Facing = Angle.West })
        end

        if p ~= nil and p == Multi1 then
            Actor.Create("truk", true, { Owner = Multi1, Location = Truck2A.Location, Facing = Angle.East })
            Actor.Create("truk", true, { Owner = Multi1, Location = Truck2B.Location, Facing = Angle.East })
            Actor.Create("truk", true, { Owner = Multi1, Location = Truck2C.Location, Facing = Angle.East })
        end

        if p ~= nil and p == Multi2 then
            Actor.Create("truk", true, { Owner = Multi2, Location = Truck3A.Location, Facing = Angle.West })
            Actor.Create("truk", true, { Owner = Multi2, Location = Truck3B.Location, Facing = Angle.West })
            Actor.Create("truk", true, { Owner = Multi2, Location = Truck3C.Location, Facing = Angle.West })
        end

        if p ~= nil and p == Multi3 then
            Actor.Create("truk", true, { Owner = Multi3, Location = Truck3A.Location, Facing = Angle.East })
            Actor.Create("truk", true, { Owner = Multi3, Location = Truck3B.Location, Facing = Angle.East })
            Actor.Create("truk", true, { Owner = Multi3, Location = Truck3C.Location, Facing = Angle.East })
        end
    end)
end

KillUnits = function(players)
    Utils.Do(players, function(p)
        if p ~= nil then
            local actors = p.GetActors()

            Utils.Do(actors, function(a)
                KillUnit(a)
            end)
        end
    end)
end

ResetCash = function(players)
    Utils.Do(players, function(p)
        if p ~= nil then
            p.Cash = 0
            p.Resources = 0
            Trigger.AfterDelay(DateTime.Seconds(1), function()
                p.Cash = StartingCash
                p.Resources = 0
            end)
        end
    end)
end

ResetBuildings = function(players)
    Utils.Do(players, function(p)
        if p ~= nil then
            local buildings = p.GetActorsByTypes({ "weap", "tent", "afld", "syrd", "fact" })
            Utils.Do(buildings, function(b)
                local loc = b.Location
                Trigger.AfterDelay(5, function()
                    b.Destroy()
                    Trigger.AfterDelay(DateTime.Seconds(1), function()
                        Actor.Create(b.Type, true, { Owner = p, Location = loc })
                    end)
                end)
            end)
        end
    end)
end

KillUnit = function(a)
    if not a.HasProperty("StartBuildingRepairs") and a.Type ~= "player" and a.HasProperty("Kill") and not a.IsDead then
        a.Stop()
        a.Destroy()
        Trigger.AfterDelay(5, function()
            KillUnit(a)
        end)
    elseif IsUpgrade(a) then
        Trigger.AfterDelay(5, function()
            a.Destroy()
        end)
    end
end

Reset = function(players)
    ResetCash(players)
    ResetBuildings(players)
    KillUnits(players)
    RestoreTrucks(players)
end

Save = function(players)
    KillTrucks(players)
    RestoreTrucks(players)

    Utils.Do(players, function(p)
        if p ~= nil then
            SavedCompositions[p.InternalName] = { }
            SavedCash[p.InternalName] = p.Resources + p.Cash

            local units = p.GetActors()

            Utils.Do(units, function(a)
                if not a.HasProperty("StartBuildingRepairs") and a.Type ~= "player" and a.HasProperty("Kill") and not a.IsDead and a.Type ~= "truk" then
                    local unit = {
                        Type = a.Type,
                        Location = a.Location,
                        CenterPosition = a.CenterPosition,
                        Facing = a.Facing,
                    }

                    if a.HasProperty("HasPassengers") and a.HasPassengers then
                        unit.Cargo = {}
                        Utils.Do(a.Passengers, function(c)
                            table.insert(unit.Cargo, c.Type)
                        end)
                    end

                    table.insert(SavedCompositions[p.InternalName], unit)
                elseif IsUpgrade(a) then
                    local upg = {
                        Type = a.Type,
                    }

                    table.insert(SavedCompositions[p.InternalName], upg)
                end
            end)
        end
    end)
end

Restore = function(players)
    KillUnits(players)
    ResetBuildings(players)

    Trigger.AfterDelay(DateTime.Seconds(1) + 10, function()
        Utils.Do(players, function(p)
            if p ~= nil then
                if SavedCompositions[p.InternalName] ~= nil and #SavedCompositions[p.InternalName] > 0 then
                    Utils.Do(SavedCompositions[p.InternalName], function(u)
                        if u.Location ~= nil then
                            local newActor = Actor.Create(u.Type, true, { Owner = p, Location = u.Location, CenterPosition = u.CenterPosition, Facing = u.Facing })

                            if u.Cargo ~= nil then
                                Utils.Do(u.Cargo, function(c)
                                    local passenger = Actor.Create(c, true, { Owner = p })
                                    newActor.LoadPassenger(passenger)
                                end)
                            end
                        else
                            Actor.Create(u.Type, true, { Owner = p })
                        end
                    end)
                end

                if SavedCash[p.InternalName] ~= nil then
                    p.Cash = SavedCash[p.InternalName]
                    p.Resources = 0
                end
            end
        end)
        RestoreTrucks(players)
    end)
end
