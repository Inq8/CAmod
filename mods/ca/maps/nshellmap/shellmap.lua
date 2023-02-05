NodLightUnitTypes = { "bike", "bike", "bggy", "n1", "n1", "n1", "n4", "n4", "n3" }
NodHeavyUnitTypes = { "ltnk", "ltnk", "apc2.nodai", "hftk", "mlrs" }
NodCyborgUnitTypes = { "n1c", "n1c", "n1c", "n1c", "n3c", "enli", "rmbc", "rmbc" }
AlliedUnitTypes = { "hmmv", "mtnk", "vulc.ai", "mtnk", "msam", "vulc.ai", "htnk.ion", "n1", "n1", "n1", "n1", "n3", "n3", "n2", "rmbo" }
SovietUnitTypes = { "3tnk.atomic", "v2rl", "isu", "4tnk.atomic", "btr.ai", "e1", "e1", "e1", "e4", "e3", "e3" }
SovietHeavyUnitTypes = { "apoc.atomic", "3tnk.atomic", "4tnk.atomic", "btr.ai", "v2rl", "e1", "e1", "e4", "ivan", "e3", "e3" }
ShipUnitTypes = { "3tnk", "3tnk", "btr.ai", "btr.ai", "katy" }
ProxyType = "powerproxy.paratroopers.allies"
ProxySovietType = "powerproxy.paratroopers"
ProxyAlliesType = "powerproxy.airborne"
ParadropWaypoints = { Airdrop2}
SovietParadropWaypoints = { Airdrop1, Airdrop3}
AlliesParadropWaypoints = { Airdrop4}
Mig1Waypoints = { Mig11, Mig12, Mig13, Mig14 }
Mig2Waypoints = { Mig21, Mig22, Mig23, Mig24 }
HelicopterUnitTypes = { "n1", "n1", "n1", "n2", "n3", "n3" };
ProducedUnitTypes =
{
	{ factory = Usainf, types = { "n1", "n3" } },
	{ factory = Usainf2, types = { "n1", "n3" } },
	{ factory = Ainf, types = { "e1", "e3" } },
	{ factory = Sovietinf, types = { "e1", "e2" } },
	{ factory = Nodinf, types = { "n1", "n3", "n4" } },
	{ factory = Aveh, types = { "2tnk", "ptnk", "batf.ai" } },
	{ factory = Usaveh, types = { "mtnk", "disr", "titn.rail", "hmmv", "htnk.ion", "hsam", "mtnk" } },
	{ factory = Usaveh2, types = { "mtnk", "disr", "titn", "hmmv", "htnk.ion", "msam", "mtnk" } },
	{ factory = Sovietveh, types = { "3tnk.atomic", "4tnk.atomic", "katy", "v2rl", "btr.ai" } },
	{ factory = Nodveh, types = { "ltnk", "hftk", "stnk.nod", "howi" } }
}

GDIBase1Location = Chronosphere.Location
GDIBase2Location = Usaveh2.Location
AlliedBaseLocation = AlliedCenter.Location
NodBaseLocation = NodHQ.Location
SovietBaseLocation = Sovietinf.Location

BindActorTriggers = function(a)
	Trigger.AfterDelay(15, function()
		if not a.IsDead and a.HasProperty("Scatter") then
			a.Scatter()
			Trigger.OnIdle(a, function(a)
				if not a.IsDead and a.IsInWorld and a.HasProperty("AttackMove") then
					if a.Owner == usa then
						a.AttackMove(FuzzyLocation(NodBaseLocation))
					elseif a.Owner == allies then
						a.AttackMove(FuzzyLocation(SovietBaseLocation))
					elseif a.Owner == Soviet then
						a.AttackMove(FuzzyLocation(AlliedBaseLocation))
					elseif a.Owner == blackh then
						local randomTarget = Utils.Random({ GDIBase1Location, GDIBase2Location, AlliedBaseLocation, AlliedBaseLocation })
						a.AttackMove(FuzzyLocation(randomTarget))
					end
				end
			end)
		end
	end)
end

FuzzyLocation = function(loc)
	local expand1 = Utils.ExpandFootprint({ loc }, true)
	local expand2 = Utils.ExpandFootprint(expand1, true)
	local expand3 = Utils.ExpandFootprint(expand2, true)
	return Utils.Random(expand3)
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

SendSovietUnits = function(entryCell, unitTypes, interval)
	local units = Reinforcements.Reinforce(Soviet, unitTypes, { entryCell }, interval)
	Utils.Do(units, function(unit)
		BindActorTriggers(unit)
	end)
	Trigger.OnAllKilled(units, function() SendSovietUnits(entryCell, unitTypes, interval) end)
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

ParadropSovietUnits = function()
	local lz = Utils.Random(SovietParadropWaypoints)
	local aircraft = powersovietproxy.TargetParatroopers(lz.CenterPosition)

	Utils.Do(aircraft, function(a)
		Trigger.OnPassengerExited(a, function(t, p)
			BindActorTriggers(p)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(35), ParadropSovietUnits)
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

ParadropAlliesUnits = function()
	local lz = Utils.Random(AlliesParadropWaypoints)
	local aircraft = poweralliesproxy.TargetParatroopers(lz.CenterPosition)

	Utils.Do(aircraft, function(a)
		Trigger.OnPassengerExited(a, function(t, p)
			BindActorTriggers(p)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(135), ParadropAlliesUnits)
end

SendMigs = function(waypoints)
	local migEntryPath = { waypoints[1].Location, waypoints[2].Location }
	local migs = Reinforcements.Reinforce(Soviet, { "suk" }, migEntryPath, 4)
	Utils.Do(migs, function(mig)
		mig.Move(waypoints[3].Location)
		mig.Move(waypoints[4].Location)
		mig.Destroy()
	end)

	Trigger.AfterDelay(DateTime.Seconds(40), function() SendMigs(waypoints) end)
end

ShipAlliedUnits = function()
	local units = Reinforcements.ReinforceWithTransport(Soviet, "lst",
		ShipUnitTypes, { LstEntry.Location, LstUnload.Location }, { LstEntry.Location })[2]

	Utils.Do(units, function(unit)
		BindActorTriggers(unit)
	end)

	Trigger.AfterDelay(DateTime.Seconds(60), ShipAlliedUnits)
end

InsertAlliedChinookReinforcements = function(entry, hpad)
	local units = Reinforcements.ReinforceWithTransport(usa, "tran",
		HelicopterUnitTypes, { entry.Location, hpad.Location + CVec.New(1, 2) }, { entry.Location })[2]

	Utils.Do(units, function(unit)
		BindActorTriggers(unit)
	end)

	Trigger.AfterDelay(DateTime.Seconds(120), function() InsertAlliedChinookReinforcements(entry, hpad) end)
end

SetupFactories = function()
	Utils.Do(ProducedUnitTypes, function(production)
		Trigger.OnProduction(production.factory, function(_, a) BindActorTriggers(a) end)
	end)
end

ChronoshiftAlliedUnits = function()
	if not Chronosphere.IsDead then
		local cells = Utils.ExpandFootprint({ ChronoshiftLocation.Location }, false)
		local units = { }
		for i = 1, #cells do
			local unit = Actor.Create("gtnk", true, { Owner = allies, Facing = Angle.East })
			BindActorTriggers(unit)
			units[unit] = cells[i]
		end
		Chronosphere.Chronoshift(units)
		Trigger.AfterDelay(DateTime.Seconds(60), ChronoshiftAlliedUnits)
	end
end

ticks = 0
speed = 30

Tick = function()
	ticks = ticks + 1

	local t = (ticks + 45) % (360 * speed) * (math.pi / 180) / speed;
	Camera.Position = viewportOrigin + WVec.New(19200 * math.sin(t), 20480 * math.cos(t), 0)
end

WorldLoaded = function()
	usa = Player.GetPlayer("GDI")
	Soviet = Player.GetPlayer("Soviet")
	blackh = Player.GetPlayer("Nod")
	allies = Player.GetPlayer("Allies")
	viewportOrigin = Camera.Position

	SetupFactories()
	ShipAlliedUnits()
	InsertAlliedChinookReinforcements(Helispawn1, Helidrop1)
	InsertAlliedChinookReinforcements(Helispawn2, Helidrop2)
	powerproxy = Actor.Create(ProxyType, false, { Owner = usa })
	powersovietproxy = Actor.Create(ProxySovietType, false, { Owner = Soviet })
	poweralliesproxy = Actor.Create(ProxyAlliesType, false, { Owner = allies })
	ParadropUSAUnits()
	ParadropSovietUnits()
	ParadropAlliesUnits()
	Trigger.AfterDelay(DateTime.Seconds(5), ChronoshiftAlliedUnits)
	Utils.Do(ProducedUnitTypes, ProduceUnits)

	Trigger.AfterDelay(DateTime.Seconds(30), function() SendMigs(Mig1Waypoints) end)
	Trigger.AfterDelay(DateTime.Seconds(30), function() SendMigs(Mig2Waypoints) end)

	SendNodUnits(Nodspawn.Location, NodLightUnitTypes, 50)
	SendNodUnits(Nodspawn2.Location, NodHeavyUnitTypes, 40)
	SendNodUnits(Nodspawn3.Location, NodCyborgUnitTypes, 70)
	SendNodUnits(Nodspawn3.Location, NodLightUnitTypes, 50)
	SendNodUnits(Nodspawn4.Location, NodHeavyUnitTypes, 40)
	SendNodUnits(Nodspawn4.Location, NodCyborgUnitTypes, 70)
	SendAlliedUnits(Usaspawn.Location, AlliedUnitTypes, 50)
	SendAlliedUnits(Helispawn1.Location, AlliedUnitTypes, 40)
	SendAlliedUnits(Usaspawn2.Location, AlliedUnitTypes, 40)
	SendSovietUnits(Sovietspawn.Location, SovietUnitTypes, 50)
	SendSovietUnits(Sovietspawn2.Location, SovietHeavyUnitTypes, 40)
	SendSovietUnits(wormhole1.Location, SovietUnitTypes, 50)
	SendSovietUnits(wormhole2.Location, SovietUnitTypes, 50)
	SendSovietUnits(wormhole3.Location, SovietHeavyUnitTypes, 50)
end
