Players = Player.GetPlayers(function(p) return p.Team == 1 end)

AttackPaths =
{
	{
        { NW2.Location, NW3.Location, NW4.Location, NW5.Location, NW6.Location, NW7.Location, NW8.Location, NW9.Location },
        { NE4.Location, NE5.Location, NE6.Location, NE7.Location },
    },
	{
	    { NE2.Location, NE3.Location, NE4.Location, NE5.Location, NE6.Location, NE7.Location },
        { NE2.Location, NE3.Location, NE4.Location, NW4.Location, NW5.Location, NW6.Location, NW7.Location, NW8.Location, NW9.Location },
	},
	{
        { SE2.Location, NE3.Location, NE4.Location, NE5.Location, NE6.Location, NE7.Location },
        { SE2.Location, NE3.Location, NE4.Location, NW4.Location, NW5.Location, NW6.Location, NW7.Location, NW8.Location, NW9.Location },
    },
}

Wormholes = { WormholeNW, WormholeNE, WormholeSE }

ScrinSquads = {
	{"s1", "s1", "s1", "s2", "gscr"},
	{"s1", "s1", "s1", "s3", "gscr"},
	{"s1", "s1", "s1", "s4", "gscr"},
    {"s1", "s1", "s1", "feed2", "gscr"},
	{"gscr", "gscr", "gscr"},
	{"s4", "s4", "s4"},
	{"s1", "s1", "s1", "s1", "s1"},
	{"s2", "s2", "s2"},
	{"s3", "s3", "s1", "s1"},
    {"feed2", "feed2", "s1", "s1"},
}

GetNumPlayers = function()
    local num = 0

	Utils.Do(Players, function(player)
		if player.InternalName ~= "Neutral" then
			local spawns = player.GetActorsByType("rmbospawn")
			num = num + #spawns
		end
	end)

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

SendScrinUnits = function(wormhole, attackPaths)
	if not wormhole or wormhole.IsDead then
		return
	end

	local interval = math.floor((120 / GetNumPlayers()) + 0.5) + Utils.RandomInteger(-3,3)
	local unitTypes = Utils.Random(ScrinSquads);
	local units = Reinforcements.Reinforce(Scrin, unitTypes, { wormhole.Location }, 15)
    local attackPath = attackPaths[1]

    if GetNumPlayers() > 2 then
        attackPath = Utils.Random(attackPaths)
    end

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
		SendScrinUnits(wormhole, attackPaths)
	end)
end

WorldLoaded = function()
	Scrin = Player.GetPlayer("Scrin")
	Neutral = Player.GetPlayer("Neutral")

	local neutralSpawns = Neutral.GetActorsByType("rmbospawn")
	Utils.Do(neutralSpawns, function(a)
		a.Destroy()
	end)

	local initialPlayers = GetNumPlayers()

	SendScrinUnits(WormholeNE, AttackPaths[2])

	if initialPlayers > 1 then
		SendScrinUnits(WormholeNW, AttackPaths[1])
	end

	if initialPlayers > 2 then
		SendScrinUnits(WormholeSE, AttackPaths[3])
	end

    local scrinUnits = Scrin.GetActorsByTypes({"gunw", "corr", "ruin", "lchr", "dark", "ptur"})

    Utils.Do(scrinUnits, function(unit)
        Trigger.OnDamaged(unit, function(self, attacker, damage)
            if attacker.EffectiveOwner == Scrin then
                return
            end
            local rand = Utils.RandomInteger(1,100)
            if rand > 90 then
                if unit.HasProperty("Attack") and not unit.IsDead then
                    unit.Stop()
                    unit.Attack(attacker)
                end
            end
        end)
    end)

	Trigger.OnAllKilledOrCaptured(Wormholes, function()
		local actors = Scrin.GetActors()
		Utils.Do(actors, function(actor)
			if actor.HasProperty("Kill") and not actor.IsDead then actor.Kill("BulletDeath") end
		end)
	end)
end
