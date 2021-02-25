--[[
   Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

NodLightUnitTypes = { "bike", "bike", "bggy", "rmbc", "rmbc", "n1c", "n1c", "n1c", "n1c", "n3c", "n3c" }
NodHeavyUnitTypes = { "mtnk", "mtnk", "ftnk", "ftnk", "wtnk", "wtnk", "cdrn" }
NodAirUnitTypes = {"scrn", "scrn", "scrn"}
AlliedUnitTypes = { "jeep", "1tnk", "ifv.ai", "1tnk", "rtnk", "ifv.ai", "ptnk", "e1", "e1", "e1", "e1", "e3", "e3", "e3", "e3" }
GDIUnitTypes = { "hmmv", "mtnk", "vulc", "msam", "titn", "htnk.ion", "msam", "n1", "n1", "n2", "n2", "n3", "n3", "n3", "n3" }
ScrinUnitTypes = { "seek", "seek", "gunw", "devo", "tpod", "s1", "s1", "s1", "s1", "s3", "s3" }
ScrinAirUnitTypes = {"deva", "stmr", "stmr"}
ProducedUnitTypes =
{
	{ factory = AlliedBarracks1, types = { "e1", "e3" } },
	{ factory = ScrinBarracks1, types = { "s1", "s3" } },
	{ factory = NodBarracks1, types = { "n1", "n3", "n4" } },
	{ factory = GDIBarracks1, types = { "n1", "n3", "n2" } },
	{ factory = AlliedWarFactory1, types = { "1tnk", "2tnk", "ptnk", "jeep", "ifv.ai", "cryo", "rtnk" } },
	{ factory = GDIWarFactory1, types = { "htnk", "msam", "titn.rail", "jugg", "htnk.ion" } },
	{ factory = ScrinWarFactory1, types = { "seek", "tpod", "gunw", "devo", "corr" } },
	{ factory = NodWarFactory1, types = { "ltnk", "ftnk", "stnk.nod", "arty.nod", "mlrs" } }
}

HelicopterUnitTypes = { "e1", "e1", "e1", "e1", "e3", "e3" };
CarryAllUnitTypes = { "n1", "n1", "n1", "n2", "n3", "n3" };

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

SendGDIUnits = function(entryCell, unitTypes, interval)
	local units = Reinforcements.Reinforce(gdi, unitTypes, { entryCell }, interval)
	Utils.Do(units, function(unit)
		BindActorTriggers(unit)
	end)
	Trigger.OnAllKilled(units, function() SendGDIUnits(entryCell, unitTypes, interval) end)
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

InsertAlliedChinookReinforcements = function(entry, waypoint)
	local units = Reinforcements.ReinforceWithTransport(allies, "tran",
		HelicopterUnitTypes, { entry.Location, waypoint.Location + CVec.New(1, 2) }, { entry.Location })[2]

	Utils.Do(units, function(unit)
		BindActorTriggers(unit)
	end)

	Trigger.AfterDelay(DateTime.Seconds(60), function() InsertAlliedChinookReinforcements(entry, waypoint) end)
end

InsertGDICarryAllReinforcements = function(entry, waypoint)
	local units = Reinforcements.ReinforceWithTransport(gdi, "ocar.pod",
		CarryAllUnitTypes, { entry.Location, waypoint.Location + CVec.New(1, 2) }, { entry.Location })[2]

	Utils.Do(units, function(unit)
		BindActorTriggers(unit)
	end)

	Trigger.AfterDelay(DateTime.Seconds(60), function() InsertGDICarryAllReinforcements(entry, waypoint) end)
end

ChronoshiftAlliedUnits = function()
	local cells = Utils.ExpandFootprint({ AlliesOuterBase.Location }, false)
	local units = { }
	for i = 1, #cells do
		local unit = Actor.Create("rtnk", true, { Owner = allies, Facing = Angle.North })
		BindActorTriggers(unit)
		units[unit] = cells[i]
	end
	Chronosphere.Chronoshift(units)
	Trigger.AfterDelay(DateTime.Seconds(60), ChronoshiftAlliedUnits)
end

SetupFactories = function()
	Utils.Do(ProducedUnitTypes, function(production)
		Trigger.OnProduction(production.factory, function(_, a) BindActorTriggers(a) end)
	end)
end

Morning = function()
	if (Lighting.Red < 1.0) then
		red = red + 0.001
	end

	if (Lighting.Green < 1.0) then
		green = green + 0.001
	end
	
	if (Lighting.Blue > 1.0) then
		blue = blue - 0.001
	end
	
	if (Lighting.Ambient < 1) then
		ambient = ambient + 0.001
	end
	Trigger.AfterDelay(DateTime.Seconds(10), function() Morning() end)
end

ticks = 0
speed = 8

Tick = function()
	ticks = ticks + 1
	
	Trigger.AfterDelay(DateTime.Seconds(30), Morning)
	
	Lighting.Red = red
	Lighting.Green = green
	Lighting.Blue = blue
	Lighting.Ambient = ambient

	local t = (ticks + 45) % (360 * speed) * (math.pi / 180) / speed;
	Camera.Position = viewportOrigin + WVec.New(16200 * math.sin(t), 10480 * math.cos(t), 0)
end

WorldLoaded = function()
	allies = Player.GetPlayer("Multi0")
	gdi = Player.GetPlayer("Multi1")
	scrin = Player.GetPlayer("Multi2")
	nod = Player.GetPlayer("Multi3")
	viewportOrigin = Camera.Position
	
	red = 0.95
	green = 0.85
	blue = 1.25
	ambient = 0.4

	SetupAlliedUnits()
	SetupFactories()
	Utils.Do(ProducedUnitTypes, ProduceUnits)
	
	Trigger.AfterDelay(DateTime.Seconds(30), ChronoshiftAlliedUnits)
	
	InsertAlliedChinookReinforcements(AlliesSpawn2, DropZone2)
	InsertGDICarryAllReinforcements(AlliesSpawn1, DropZone1)

	SendNodUnits(NodSpawn1.Location, NodLightUnitTypes, 50)
	SendNodUnits(NodSpawn2.Location, NodHeavyUnitTypes, 40)
	SendNodUnits(AlliesSpawn2.Location, NodAirUnitTypes, 300)
	SendAlliedUnits(AlliesSpawn1.Location, AlliedUnitTypes, 50)
	SendAlliedUnits(AlliesSpawn2.Location, AlliedUnitTypes, 40)
	SendGDIUnits(AlliesSpawn1.Location, GDIUnitTypes, 50)
	SendScrinUnits(ScrinSpawn1.Location, ScrinUnitTypes, 50)
	SendScrinUnits(ScrinSpawn2.Location, ScrinUnitTypes, 40)
	SendScrinUnits(ScrinSpawn1.Location, ScrinAirUnitTypes, 300)
end
