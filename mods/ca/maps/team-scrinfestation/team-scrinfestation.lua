Players = Player.GetPlayers(function(p) return p.Team == 1 or p.Team == 2 end)
GDIPlayers = Player.GetPlayers(function(p) return p.Team == 1 end)
NodPlayers = Player.GetPlayers(function(p) return p.Team == 2 end)
ScrinActorTypes = {"gunw", "corr", "ruin", "lchr", "dark", "ptur", "s1", "s2", "s3", "s4", "gscr", "feed"}

GDIAttackPaths =
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

NodAttackPaths =
{
	{
		{ Nod_NW2.Location, Nod_NW3.Location, Nod_NW4.Location, Nod_NW5.Location, Nod_NW6.Location, Nod_NW7.Location, Nod_NW8.Location, Nod_NW9.Location },
		{ Nod_NE4.Location, Nod_NE5.Location, Nod_NE6.Location, Nod_NE7.Location },
	},
	{
		{ Nod_NE2.Location, Nod_NE3.Location, Nod_NE4.Location, Nod_NE5.Location, Nod_NE6.Location, Nod_NE7.Location },
		{ Nod_NE2.Location, Nod_NE3.Location, Nod_NE4.Location, Nod_NW4.Location, Nod_NW5.Location, Nod_NW6.Location, Nod_NW7.Location, Nod_NW8.Location, Nod_NW9.Location },
	},
	{
		{ Nod_SE2.Location, Nod_NE3.Location, Nod_NE4.Location, Nod_NE5.Location, Nod_NE6.Location, Nod_NE7.Location },
		{ Nod_SE2.Location, Nod_NE3.Location, Nod_NE4.Location, Nod_NW4.Location, Nod_NW5.Location, Nod_NW6.Location, Nod_NW7.Location, Nod_NW8.Location, Nod_NW9.Location },
	},
}

Wormholes = { WormholeNW, WormholeNE, WormholeSE }
NodWormholes = { Nod_WormholeNW, Nod_WormholeNE, Nod_WormholeSE }

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

GetNumPlayers = function(players)
	local num = 0

	for i,player in pairs(players) do
		local spawns = player.GetActorsByType("rmbospawn")
		for j,spawn in pairs(spawns) do
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

SendScrinUnits = function(wormhole, attackPaths, numPlayers)
	if not wormhole or wormhole.IsDead then
		return
	end

	local interval = math.floor((120 / numPlayers) + 0.5) + Utils.RandomInteger(-3,3)
	local unitTypes = Utils.Random(ScrinSquads);
	local units = Reinforcements.Reinforce(Scrin, unitTypes, { wormhole.Location }, 15)
	local attackPath = attackPaths[1]

	if numPlayers > 2 then
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
		SendScrinUnits(wormhole, attackPaths, numPlayers)
	end)
end

WorldLoaded = function()
	Scrin = Player.GetPlayer("Scrin")
    GDI = Player.GetPlayer("Scrin")
    Nod = Player.GetPlayer("Nod")
	local initialGdiPlayers = GetNumPlayers(GDIPlayers)
    local initialNodPlayers = GetNumPlayers(NodPlayers)

	SendScrinUnits(WormholeNE, GDIAttackPaths[2], initialGdiPlayers)
    SendScrinUnits(Nod_WormholeNE, NodAttackPaths[2], initialNodPlayers)

	if initialGdiPlayers > 1 then
		SendScrinUnits(WormholeNW, GDIAttackPaths[1], initialGdiPlayers)
	end

	if initialGdiPlayers > 2 then
		SendScrinUnits(WormholeSE, GDIAttackPaths[3], initialGdiPlayers)
	end

	if initialNodPlayers > 1 then
		SendScrinUnits(Nod_WormholeNW, NodAttackPaths[1], initialNodPlayers)
	end

	if initialNodPlayers > 2 then
		SendScrinUnits(Nod_WormholeSE, NodAttackPaths[3], initialNodPlayers)
	end

	local scrinUnits = Scrin.GetActorsByTypes(ScrinActorTypes)

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
		local actors = Scrin.GetActorsByTypes(ScrinActorTypes)
		Utils.Do(actors, function(actor)
			if actor.HasProperty("Kill") and not actor.IsDead then actor.Kill("BulletDeath") end
		end)

        Trigger.AfterDelay(DateTime.Seconds(5), function()
            Utils.Do(NodPlayers, function(nodPlayer)
                local nodActors = nodPlayer.GetActors()
                Utils.Do(nodActors, function(nodActor)
                    if nodActor.HasProperty("Kill") and not nodActor.IsDead then nodActor.Kill("BulletDeath") end
                end)
            end)
        end)
	end)

	Trigger.OnAllKilledOrCaptured(NodWormholes, function()
		local actors = Scrin.GetActorsByTypes(ScrinActorTypes)
		Utils.Do(actors, function(actor)
			if actor.HasProperty("Kill") and not actor.IsDead then actor.Kill("BulletDeath") end
		end)

        Trigger.AfterDelay(DateTime.Seconds(5), function()
            Utils.Do(GDIPlayers, function(gdiPlayer)
                local gdiActors = gdiPlayer.GetActors()
                Utils.Do(gdiActors, function(gdiActor)
                    if gdiActor.HasProperty("Kill") and not gdiActor.IsDead then gdiActor.Kill("BulletDeath") end
                end)
            end)
        end)
	end)
end
