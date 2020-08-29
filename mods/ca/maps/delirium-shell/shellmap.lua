--[[
   Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

NodUnitTypes = { "bike", "bike", "bggy", "ltnk", "ltnk", "n1", "n1", "n1", "n1", "n3", "n3" }
AlliedUnitTypes = { "jeep", "1tnk", "ifv.ai", "1tnk", "2tnk", "ifv.ai", "ptnk", "e1", "e1", "e1", "e1", "e3", "e3", "e3", "e3" }
ScrinUnitTypes = { "seek", "seek", "gunw", "devo", "tpod", "s1", "s1", "s1", "s1", "s3", "s3" }
ScrinAirUnitTypes = {"deva", "stmr", "stmr"}
ProducedUnitTypes =
{
	{ factory = AlliedBarracks1, types = { "e1", "e3" } },
	{ factory = ScrinBarracks1, types = { "s1", "s3" } },
	{ factory = NodBarracks1, types = { "n1", "n3", "n5" } },
	{ factory = AlliedWarFactory1, types = { "1tnk", "2tnk", "ptnk", "jeep", "ifv.ai", "cryo", "arty" } },
	{ factory = ScrinWarFactory1, types = { "seek", "tpod", "gunw", "devo", "corr" } },
	{ factory = NodWarFactory1, types = { "ltnk", "ftnk", "stnk.nod", "arty.nod", "mlrs" } }
}

BindActorTriggers = function(a)
	if a.HasProperty("Hunt") then
		if a.Owner ~= allies then
			Trigger.OnIdle(a, function(a)
				if a.IsInWorld then
					a.Hunt()
				end
			end)
		else
			Trigger.OnIdle(a, function(a)
				if a.IsInWorld then
					a.Hunt()
				end
			end)
		end
	end

	if a.HasProperty("HasPassengers") then
		Trigger.OnDamaged(a, function()
			if a.HasPassengers then
				a.Stop()
				a.UnloadPassengers()
			end
		end)
	end
end

SendNodUnits = function(entryCell, unitTypes, interval)
	local units = Reinforcements.Reinforce(nod, unitTypes, { entryCell }, interval)
	Utils.Do(units, function(unit)
		BindActorTriggers(unit)
	end)
	Trigger.OnAllKilled(units, function() SendNodUnits(entryCell, unitTypes, interval) end)
end

SendAlliedUnits = function(entryCell, unitTypes, interval)
	local units = Reinforcements.Reinforce(allies, unitTypes, { entryCell }, interval)
	Utils.Do(units, function(unit)
		BindActorTriggers(unit)
	end)
	Trigger.OnAllKilled(units, function() SendAlliedUnits(entryCell, unitTypes, interval) end)
end

SendScrinUnits = function(entryCell, unitTypes, interval)
	local units = Reinforcements.Reinforce(scrin, unitTypes, { entryCell }, interval)
	Utils.Do(units, function(unit)
		BindActorTriggers(unit)
	end)
	Trigger.OnAllKilled(units, function() SendScrinUnits(entryCell, unitTypes, interval) end)
end

ProduceUnits = function(t)
	local factory = t.factory
	if not factory.IsDead then
		local unitType = t.types[Utils.RandomInteger(1, #t.types + 1)]
		factory.Wait(Actor.BuildTime(unitType))
		factory.Produce(unitType)
		factory.CallFunc(function() ProduceUnits(t) end)
	end
end

SetupAlliedUnits = function()
	Utils.Do(Map.NamedActors, function(a)
		if a.Owner == allies and a.HasProperty("AcceptsCondition") and a.AcceptsCondition("unkillable") then
			a.GrantCondition("unkillable")
			a.Stance = "Defend"
		end
	end)
end

SetupFactories = function()
	Utils.Do(ProducedUnitTypes, function(production)
		Trigger.OnProduction(production.factory, function(_, a) BindActorTriggers(a) end)
	end)
end

ticks = 0
speed = 5

Tick = function()
	ticks = ticks + 1

	local t = (ticks + 45) % (360 * speed) * (math.pi / 180) / speed;
	Camera.Position = viewportOrigin + WVec.New(8200 * math.sin(t), 9480 * math.cos(t), 0)
end

WorldLoaded = function()
	allies = Player.GetPlayer("Multi0")
	nod = Player.GetPlayer("Multi1")
	scrin = Player.GetPlayer("Multi2")
	nodbase = Player.GetPlayer("Multi3")
	viewportOrigin = Camera.Position

	SetupAlliedUnits()
	SetupFactories()
	Utils.Do(ProducedUnitTypes, ProduceUnits)

	SendNodUnits(NodSpawn1.Location, NodUnitTypes, 50)
	SendNodUnits(NodSpawn2.Location, NodUnitTypes, 40)
	SendAlliedUnits(AlliesSpawn1.Location, AlliedUnitTypes, 50)
	SendAlliedUnits(AlliesSpawn2.Location, AlliedUnitTypes, 40)
	SendScrinUnits(ScrinSpawn1.Location, ScrinUnitTypes, 50)
	SendScrinUnits(ScrinSpawn2.Location, ScrinUnitTypes, 40)
	SendScrinUnits(ScrinSpawn1.Location, ScrinAirUnitTypes, 300)
end
