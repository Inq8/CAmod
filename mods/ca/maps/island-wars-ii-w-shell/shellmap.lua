--[[
   Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

UnitTypes = { "3tnk", "ttnk" }
GDIUnitTypes = { "mtnk", "mtnk" }
GDIMLRSTypes = { "hsam", "hsam" }
BeachUnitTypes = { "e1", "e2", "e3", "e4", "e1", "e2", "e3", "e4", "e1", "e2", "e3", "e4", "e1", "e2", "e3", "shok" }
GDIBeachUnitTypes = { "n1", "n2", "n3", "n2", "n1", "n2", "n3", "n1", "n1", "n2", "n3", "n1", "n1", "n1", "n1", "n1" }
ProducedUnitTypes =
{
	{ factory = AlliedBarracks1, types = { "n1", "n3" } },
	{ factory = SovietBarracks1, types = { "e1", "e2", "e3" } },
	{ factory = SKennel1, types = { "dog" } },
	{ factory = ANavalYard1, types = { "pt2" } },
	{ factory = SSubPen1, types = { "ss" } },
	{ factory = AlliedWarFactory1, types = { "mtnk", "htnk", "msam" } },
	{ factory = SovietWarFactory1, types = { "3tnk", "4tnk", "ttnk" } }
}

BindActorTriggers = function(a)
	if a.HasProperty("Hunt") then
		if a.Owner == gdi then
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

SendSovietUnits = function(entryCell, unitTypes, interval)
	local units = Reinforcements.Reinforce(soviets, unitTypes, { entryCell }, interval)
	Utils.Do(units, function(unit)
		BindActorTriggers(unit)
	end)
	Trigger.OnAllKilled(units, function() SendSovietUnits(entryCell, unitTypes, interval) end)
end

SendGDIUnits = function(entryCell, unitTypes, interval)
	local units = Reinforcements.Reinforce(gdi, unitTypes, { entryCell }, interval)
	Utils.Do(units, function(unit)
		BindActorTriggers(unit)
	end)
	Trigger.OnAllKilled(units, function() SendGDIUnits(entryCell, unitTypes, interval) end)
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
		if a.Owner == gdi and a.HasProperty("AcceptsCondition") and a.AcceptsCondition("unkillable") then
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
	gdi = Player.GetPlayer("Multi0")
	soviets = Player.GetPlayer("Multi1")
	viewportOrigin = Camera.Position

	SetupAlliedUnits()
	SetupFactories()
	Utils.Do(ProducedUnitTypes, ProduceUnits)
	
	SendSovietUnits(Entry2.Location, UnitTypes, 50)
	SendSovietUnits(Entry3.Location, UnitTypes, 50)
	SendGDIUnits(Entry1.Location, GDIUnitTypes, 50)
	SendGDIUnits(Entry4.Location, GDIMLRSTypes, 50)
	SendSovietUnits(Entry2.Location, BeachUnitTypes, 15)
	SendGDIUnits(Entry1.Location, GDIBeachUnitTypes, 15)
end
