NodLightUnitTypes = { "bike", "bike", "bggy", "n1", "n1", "n1", "n4", "n4", "n3" }
NodHeavyUnitTypes = { "ltnk", "ltnk", "sapc.ai", "hftk", "mlrs" }
NodCyborgUnitTypes = { "n1c", "n1c", "n1c", "n1c", "n3c", "n3c", "rmbc", "rmbc" }
AlliedUnitTypes = { "jeep", "1tnk", "apc.ai", "1tnk", "gtnk", "ifv.ai", "ptnk", "e1", "e1", "e1", "e1", "e3", "e3", "e3", "seal" }
ScrinUnitTypes = { "seek", "atmz", "lchr", "devo", "tpod", "s1", "s1", "s1", "s1", "s3", "s3" }
ProxyType = "powerproxy.airborne"
ParadropWaypoints = { Airdrop1, Airdrop2, Airdrop3}
HelicopterUnitTypes = { "e1", "e1", "e1", "e1", "e3", "e3" };
ProducedUnitTypes =
{
	{ factory = Usainf, types = { "e1", "e3" } },
	{ factory = Usainf2, types = { "e1", "e3" } },
	{ factory = Scrininf, types = { "s1", "s3" } },
	{ factory = Nodinf, types = { "n1", "n3", "n4" } },
	{ factory = Nodinf2, types = { "n1", "n3", "n4" } },
	{ factory = Usaveh, types = { "1tnk", "2tnk", "ptnk", "jeep", "ifv.ai", "cryo", "2tnk" } },
	{ factory = Usaveh2, types = { "1tnk", "2tnk", "ptnk", "jeep", "ifv.ai", "cryo", "2tnk" } },
	{ factory = Scrinveh, types = { "seek", "lchr", "atmz", "devo", "corr" } },
	{ factory = Nodveh, types = { "ltnk", "hftk", "stnk.nod", "howi" } },
	{ factory = Nodveh2, types = { "ltnk", "hftk", "stnk.nod", "howi" } }
}

BindActorTriggers = function(a)
	if a.HasProperty("Hunt") then
		if a.Owner ~= usa then
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
	local units = Reinforcements.Reinforce(blackh, unitTypes, { entryCell }, interval)
	Utils.Do(units, function(unit)
		BindActorTriggers(unit)
	end)
	Trigger.OnAllKilled(units, function() SendNodUnits(entryCell, unitTypes, interval) end)
end

SendAlliedUnits = function(entryCell, unitTypes, interval)
	local units = Reinforcements.Reinforce(usa, unitTypes, { entryCell }, interval)
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

ParadropUSAUnits = function()
	local lz = Utils.Random(ParadropWaypoints)
	local aircraft = powerproxy.TargetParatroopers(lz.CenterPosition)

	Utils.Do(aircraft, function(a)
		Trigger.OnPassengerExited(a, function(t, p)
			BindActorTriggers(p)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(135), ParadropUSAUnits)
end

InsertAlliedChinookReinforcements = function(entry, hpad)
	local units = Reinforcements.ReinforceWithTransport(usa, "nhaw",
		HelicopterUnitTypes, { entry.Location, hpad.Location + CVec.New(1, 2) }, { entry.Location })[2]

	Utils.Do(units, function(unit)
		BindActorTriggers(unit)
	end)

	Trigger.AfterDelay(DateTime.Seconds(120), function() InsertAlliedChinookReinforcements(entry, hpad) end)
end

ChronoshiftAlliedUnits = function()
	local cells = Utils.ExpandFootprint({ helispawn2.Location }, false)
	local units = { }
	for i = 1, #cells do
		local unit = Actor.Create("gtnk", true, { Owner = usa, Facing = Angle.North })
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

ticks = 0
speed = 30

Tick = function()
	ticks = ticks + 1
	
	if (Utils.RandomInteger(1, 200) == 10) then
		local delay = Utils.RandomInteger(1, 10)
		Lighting.Flash("LightningStrike", delay)
		Trigger.AfterDelay(delay, function()
			Media.PlaySound("thunder" .. Utils.RandomInteger(1,6) .. ".aud")
		end)
	end
	if (Utils.RandomInteger(1, 200) == 10) then
		Media.PlaySound("thunder-ambient.aud")
	end

	local t = (ticks + 45) % (360 * speed) * (math.pi / 180) / speed;
	Camera.Position = viewportOrigin + WVec.New(19200 * math.sin(t), 20480 * math.cos(t), 0)
end

WorldLoaded = function()
	usa = Player.GetPlayer("Multi0")
	scrin = Player.GetPlayer("Multi2")
	blackh = Player.GetPlayer("Multi3")
	viewportOrigin = Camera.Position
	
	SetupFactories()
	InsertAlliedChinookReinforcements(Helispawn1, Helidrop1)
	InsertAlliedChinookReinforcements(helispawn2, Airdrop3)
	powerproxy = Actor.Create(ProxyType, false, { Owner = usa })
	ParadropUSAUnits()
	
	Trigger.AfterDelay(DateTime.Seconds(5), ChronoshiftAlliedUnits)
	Utils.Do(ProducedUnitTypes, ProduceUnits)

	SendNodUnits(Nodspawn.Location, NodLightUnitTypes, 50)
	SendNodUnits(Nodspawn2.Location, NodHeavyUnitTypes, 40)
	SendNodUnits(Nodspawn3.Location, NodCyborgUnitTypes, 70)
	SendNodUnits(Nodspawn3.Location, NodLightUnitTypes, 50)
	SendNodUnits(Nodspawn4.Location, NodHeavyUnitTypes, 40)
	SendNodUnits(Nodspawn4.Location, NodCyborgUnitTypes, 70)
	SendAlliedUnits(Usaspawn.Location, AlliedUnitTypes, 50)
	SendAlliedUnits(Helispawn1.Location, AlliedUnitTypes, 40)
	SendAlliedUnits(Usaspawn2.Location, AlliedUnitTypes, 40)
	SendScrinUnits(Scrinspawn.Location, ScrinUnitTypes, 50)
	SendScrinUnits(Scrinspawn2.Location, ScrinUnitTypes, 40)
	SendScrinUnits(wormhole1.Location, ScrinUnitTypes, 50)
	SendScrinUnits(wormhole2.Location, ScrinUnitTypes, 50)
end
