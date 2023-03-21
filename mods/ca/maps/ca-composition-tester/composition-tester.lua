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

    if Multi0 ~= nil then
        Actor.Create("truk", true, { Owner = Multi0, Location = Spawn1.Location, Facing = Angle.East })
    end

    if Multi1 ~= nil then
        Actor.Create("truk", true, { Owner = Multi1, Location = Spawn2.Location, Facing = Angle.West })
    end
end

Tick = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
        local resetActors = Map.ActorsInCircle(Reset.CenterPosition, WDist.New(1024), function(a)
            return a.Type ~= "resetbox" and not a.HasProperty("Land")
        end)

        if #resetActors > 0 then
            Utils.Do(resetActors, function(a)
                a.Destroy()
            end)

            ResetUnits()
            ResetBuildings()
        end
    end
end

ResetUnits = function()
    if Multi0 ~= nil then
        local playerOneActors = Multi0.GetActors()

        Utils.Do(playerOneActors, function(a)
            KillUnit(a)
        end)

        Multi0.Cash = 0
        Multi0.Resources = 0
        Trigger.AfterDelay(DateTime.Seconds(1), function()
            Multi0.Cash = StartingCash
            Multi0.Resources = 0
        end)
        Actor.Create("truk", true, { Owner = Multi0, Location = Spawn1.Location, Facing = Angle.East })
    end

    if Multi1 ~= nil then
        local playerTwoActors = Multi1.GetActors()

        Utils.Do(playerTwoActors, function(a)
            KillUnit(a)
        end)

        Multi1.Cash = 0
        Multi1.Resources = 0
        Trigger.AfterDelay(DateTime.Seconds(1), function()
            Multi1.Cash = StartingCash
            Multi1.Resources = 0
        end)
        Actor.Create("truk", true, { Owner = Multi1, Location = Spawn2.Location, Facing = Angle.West })
    end
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
        a.Kill()
        Trigger.AfterDelay(5, function()
            KillUnit(a)
        end)
    elseif string.find(a.Type, ".upgrade") then
        Trigger.AfterDelay(5, function()
            a.Destroy()
        end)
    end
end
