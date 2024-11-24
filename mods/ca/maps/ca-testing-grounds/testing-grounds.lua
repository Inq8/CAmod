DeadDummies = { }

WorldLoaded = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
    Dummy = Player.GetPlayer("Dummy")

    local dummyActors = GetDummyActors()

    Utils.Do(dummyActors, function(a)
        if a.HasProperty("Kill") then
            Trigger.OnKilled(a, function(self, killer)
                StoreDeadDummy(self)
            end)
        end
    end)

    Trigger.OnAnyProduction(function(producer, produced, productionType)

        if produced.Type == "action.respawn" then
            Media.DisplayMessage("Regenerating dummies.", "Notification", HSLColor.FromHex("00FFFF"))
            RespawnDummies()
        end

        if produced.Type == "action.clear.upgrades" then
            Media.DisplayMessage("Clearing upgrades.", "Notification", HSLColor.FromHex("FF8800"))
            ClearUpgrades()
        end

        if produced.Type == "action.enable.hazmat" then
            Actor.Create("hazmat.upgrade", true, { Owner = Dummy })
            Media.DisplayMessage("Hazmat applied to dummies.", "Notification", HSLColor.FromHex("00FF00"))
        end

        if produced.Type == "action.disable.hazmat" then
            Media.DisplayMessage("Hazmat removed from dummies.", "Notification", HSLColor.FromHex("FF0000"))
            Utils.Do(Dummy.GetActorsByType("hazmat.upgrade"), function(a)
                a.Destroy()
            end)
        end

        if produced.Type == "action.enable.flakarmor" then
            Actor.Create("flakarmor.upgrade", true, { Owner = Dummy })
            Media.DisplayMessage("Flak armor applied to dummies.", "Notification", HSLColor.FromHex("00FF00"))
        end

        if produced.Type == "action.disable.flakarmor" then
            Media.DisplayMessage("Flak armor removed from dummies.", "Notification", HSLColor.FromHex("FF0000"))
            Utils.Do(Dummy.GetActorsByType("flakarmor.upgrade"), function(a)
                a.Destroy()
            end)
        end

        if produced.Type == "action.enable.cyborgarmor" then
            Actor.Create("cyborgarmor.upgrade", true, { Owner = Dummy })
            Media.DisplayMessage("Cyborg armor applied to dummies.", "Notification", HSLColor.FromHex("00FF00"))
        end

        if produced.Type == "action.disable.cyborgarmor" then
            Media.DisplayMessage("Cyborg armor removed from dummies.", "Notification", HSLColor.FromHex("FF0000"))
            Utils.Do(Dummy.GetActorsByType("cyborgarmor.upgrade"), function(a)
                a.Destroy()
            end)
        end
    end)
end

StoreDeadDummy = function(a)
    local actorDetails = {
        Type = a.Type,
        Location = a.Location,
        CenterPosition = a.CenterPosition,
    }

    if not a.HasProperty("StartBuildingRepairs") and a.Type ~= "fenc" and a.Type ~= "barb" and a.Type ~= "chain" and a.Type ~= "brik" and a.Type ~= "sbag" then
        actorDetails.Facing = a.Facing
    end

    table.insert(DeadDummies, actorDetails)
end

RespawnDummies = function()
    Utils.Do(DeadDummies, function(d)
        local randomDelay = Utils.RandomInteger(1, 100)

        Trigger.AfterDelay(randomDelay, function()
            local respawnedActor

            if d.Facing ~= nil then
                respawnedActor = Actor.Create(d.Type, true, { Owner = Dummy, Location = d.Location, CenterPosition = d.CenterPosition, Facing = d.Facing })
            else
                respawnedActor = Actor.Create(d.Type, true, { Owner = Dummy, Location = d.Location, CenterPosition = d.CenterPosition })
            end

            Trigger.OnKilled(respawnedActor, function(self, killer)
                StoreDeadDummy(self)
            end)
        end)
    end)

    local dummyActors = GetDummyActors()
    Utils.Do(dummyActors, function(a)
        if a.HasProperty("Health") then
            if a.Health < a.MaxHealth then
                a.Health = a.MaxHealth
            end
        end
    end)

    DeadDummies = { }
end

GetDummyActors = function()
    local dummyActors = Utils.Where(Dummy.GetActors(), function(a)
        return a.Type ~= "minv" and a.Type ~= "player"
    end)
    return dummyActors
end

ClearUpgrades = function()
    local p = Player.GetPlayer("Multi0")
    local actors = p.GetActors()

    Utils.Do(actors, function(a)
        if IsUpgrade(a) then
            a.Destroy()
        end
    end)
end

IsUpgrade = function(a)
	return string.find(a.Type, ".upgrade") or string.find(a.Type, ".strat")
end
