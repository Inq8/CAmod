Players = Player.GetPlayers(function(p) return p.Team == 1 end)

AttackPaths =
{
	  { NW1.Location, NW2.Location, NW3.Location, NW4.Location, NW5.Location, NW6.Location, NW7.Location, NW8.Location, NW9.Location },
      { NE1.Location, NE2.Location, NE3.Location, NE4.Location, NE5.Location, NE6.Location, NE7.Location },
      { SE1.Location, SE2.Location, NE3.Location, NE4.Location, NE5.Location, NE6.Location, NE7.Location },
}

Wormholes = { WormholeNW, WormholeNE, WormholeSE }

ScrinSquads = {
    {"s1", "s1", "s1", "s2", "gscr"},
    {"s1", "s1", "s1", "s3", "gscr"},
    {"s1", "s1", "s1", "s4", "gscr"},
    {"gscr", "gscr", "gscr"},
    {"s4", "s4", "s4"},
    {"s1", "s1", "s1", "s1", "s1"},
    {"s2", "s2", "s2"},
    {"s3", "s3", "s1", "s1"},
}

GetCommandosAlive = function()
    local num = 0

    for i,player in pairs(Players) do
        local commandos = player.GetActorsByType("rmbo")
        for j,rmbo in pairs(commandos) do
            num = num + 1
        end
    end

    return num
end

IdleHunt = function(actor)
	if actor.HasProperty("Hunt") and not actor.IsDead then
		Trigger.OnIdle(actor, actor.Hunt)
	end
end

MoveAndHunt = function(actors, path)
	Utils.Do(actors, function(actor)
		if not actor or actor.IsDead then
			return
		end

		Utils.Do(path, function(point)
			actor.AttackMove(point)
		end)

		IdleHunt(actor)
	end)
end

SendScrinUnits = function(entryCell, attackPath)
    local interval = math.floor((180 / GetCommandosAlive()) + 0.5) + Utils.RandomInteger(-3,3)
    local unitTypes = Utils.Random(ScrinSquads);
	local units = Reinforcements.Reinforce(Scrin, unitTypes, { entryCell }, 15)

    Utils.Do(units, function(unit)
        unit.Patrol(attackPath, true, 50)

        Trigger.OnDamaged(unit, function()

            Utils.Do(units, function(unit)
                if unit.HasProperty("Hunt") and not unit.IsDead then
                    unit.Stop()
                    unit.Hunt()
                    Trigger.ClearAll(unit)
                end
            end)
        end)
    end)

    Trigger.AfterDelay(DateTime.Seconds(interval), function()
        SendScrinUnits(entryCell, attackPath, interval)
    end)
end

WorldLoaded = function()
	Scrin = Player.GetPlayer("Scrin")
    local initialPlayers = GetCommandosAlive()

	SendScrinUnits(AttackPaths[1][1], AttackPaths[1])

    if initialPlayers > 1 then
        SendScrinUnits(AttackPaths[2][1], AttackPaths[2])
    end

    if initialPlayers > 2 then
        SendScrinUnits(AttackPaths[3][1], AttackPaths[3])
    end

    Trigger.OnAllKilledOrCaptured(Wormholes, function()
        local actors = Scrin.GetActors()
        Utils.Do(actors, function(actor)
            if actor.HasProperty("Kill") and not actor.IsDead then actor.Kill() end
        end)
    end)

end
