SavedCompositions = { }
SavedCash = { }

WorldLoaded = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")

    if Multi0 ~= nil then
        StartingCash = Multi0.Cash
    elseif Multi1 ~= nil then
        StartingCash = Multi1.Cash
    else
        StartingCash = 20000
    end

    RestoreTrucks()
end

Tick = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
        local resetActors = Map.ActorsInCircle(ResetWH.CenterPosition, WDist.New(1024), function(a)
            return a.Type == "truk"
        end)

        if #resetActors > 0 then
            Reset()
        else
            local resetActors = Map.ActorsInCircle(RestoreWH.CenterPosition, WDist.New(1024), function(a)
                return a.Type == "truk"
            end)

            if #resetActors > 0 then
                Utils.Do(resetActors, function(a)
                    a.Destroy()
                end)
                Restore()
            else
                local saveActors = Map.ActorsInCircle(SaveWH.CenterPosition, WDist.New(1024), function(a)
                    return a.Type == "truk"
                end)

                if #saveActors > 0 then
                    Utils.Do(resetActors, function(a)
                        a.Destroy()
                    end)
                    Save()
                end
            end
        end
    end
end

IsUpgrade = function(a)
    return string.find(a.Type, ".upgrade") or string.find(a.Type, ".strat")
end

KillTrucks = function()
    Utils.Do({ Multi0, Multi1 }, function(p)
        if p ~= nil then
            local trucks = p.GetActorsByType("truk")
            Utils.Do(trucks, function(a)
                a.Destroy()
            end)
        end
    end)
end

RestoreTrucks = function()
    if Multi0 ~= nil then
        Actor.Create("truk", true, { Owner = Multi0, Location = Save1.Location, Facing = Angle.East })
        Actor.Create("truk", true, { Owner = Multi0, Location = Restore1.Location, Facing = Angle.East })
        Actor.Create("truk", true, { Owner = Multi0, Location = Reset1.Location, Facing = Angle.East })
    end

    if Multi1 ~= nil then
        Actor.Create("truk", true, { Owner = Multi1, Location = Save2.Location, Facing = Angle.West })
        Actor.Create("truk", true, { Owner = Multi1, Location = Restore2.Location, Facing = Angle.West })
        Actor.Create("truk", true, { Owner = Multi1, Location = Reset2.Location, Facing = Angle.West })
    end
end

KillUnits = function()
    Utils.Do({ Multi0, Multi1 }, function(p)
        if p ~= nil then
            local actors = p.GetActors()

            Utils.Do(actors, function(a)
                KillUnit(a)
            end)
        end
    end)
end

ResetCash = function()
    Utils.Do({ Multi0, Multi1 }, function(p)
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

ResetBuildings = function()
    Utils.Do({ Multi0, Multi1 }, function(p)
        if p ~= nil then
            local buildings = p.GetActorsByTypes({ "weap", "tent", "afld", "syrd" })
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

Reset = function()
	Media.DisplayMessage("Resetting...", "Notification", HSLColor.FromHex("FF0000"))
    ResetCash()
    ResetBuildings()
    KillUnits()
    RestoreTrucks()
end

Save = function()
	Media.DisplayMessage("Compositions saved.", "Notification", HSLColor.FromHex("00FF00"))

    KillTrucks()
    RestoreTrucks()

    Utils.Do({ Multi0, Multi1 }, function(p)
        if p ~= nil then
            SavedCompositions[p.Name] = { }
            SavedCash[p.Name] = p.Resources + p.Cash

            local units = p.GetActors()

            Utils.Do(units, function(a)
                if not a.HasProperty("StartBuildingRepairs") and a.Type ~= "player" and a.HasProperty("Kill") and not a.IsDead and a.Type ~= "truk" then
                    local unit = {
                        Type = a.Type,
                        Location = a.Location,
                        CenterPosition = a.CenterPosition,
                        Facing = a.Facing
                    }

                    table.insert(SavedCompositions[p.Name], unit)
                elseif IsUpgrade(a) then
                    local upg = {
                        Type = a.Type,
                    }

                    table.insert(SavedCompositions[p.Name], upg)
                end
            end)
        end
    end)
end

Restore = function()
	Media.DisplayMessage("Restoring compositions...", "Notification", HSLColor.FromHex("00FFFF"))
    KillUnits()
    ResetBuildings()

    Trigger.AfterDelay(DateTime.Seconds(1) + 10, function()
        Utils.Do({ Multi0, Multi1 }, function(p)
            if p ~= nil then
                if SavedCompositions[p.Name] ~= nil and #SavedCompositions[p.Name] > 0 then
                    Utils.Do(SavedCompositions[p.Name], function(u)
                        if u.Location ~= nil then
                            Actor.Create(u.Type, true, { Owner = p, Location = u.Location, CenterPosition = u.CenterPosition, Facing = u.Facing })
                        else
                            Actor.Create(u.Type, true, { Owner = p })
                        end
                    end)
                end

                if SavedCash[p.Name] ~= nil then
                    p.Cash = SavedCash[p.Name]
                    p.Resources = 0
                end
            end
        end)
        RestoreTrucks()
    end)
end
