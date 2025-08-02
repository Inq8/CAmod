Ticks = 0
Speed = 30
Difficulty = "normal"

SealDropPaths = { { SealDropSpawn.Location, SealDrop1.Location }, { SealDropSpawn.Location, SealDrop2.Location }, { SealDropSpawn.Location, SealDrop3.Location }, { SealDropSpawn.Location, SealDrop4.Location } }
SealPatrolPath = { AlliedCamp.Location, ScrinAttack1.Location, SovietAttack3a.Location }

UnitCompositionsShellmap = {
	Soviet = {
		{ Infantry = { "e3", "e1", "e1", "e3", "e4" }, Vehicles = { "btr.ai" } },
		{ Infantry = { "e3", "e1", "e1", "ttrp", "shok", "e1", "shok", "e3", "e4" }, Vehicles = { TeslaVariant, "btr.ai", SovietMammothVariant, "grad" } },
		{ Infantry = { "e3", "e1", "e1", "ttrp", "shok", "e1", "e2", "e3", "e4" }, Vehicles = { "3tnk.rhino.atomic", "btr.ai", SovietMammothVariant, "grad" } },
		{ Infantry = { "e3", "e1", "e1", "ttrp", "e8", "e1", "e2", "e3", "e4" }, Vehicles = { "3tnk.rhino.yuri", "btr.ai", SovietMammothVariant, "grad" } },
		{ Infantry = { "e3", "e1", "e1", "ttrp", "deso", "e1", "e2", "e3", "e4" }, Vehicles = { "3tnk.rhino", "btr.yuri.ai", SovietMammothVariant, SovietBasicArty } },
		{ Infantry = { "e3", "e1", "e1", "ttrp", "deso", "e1", "e2", "e3", "e4" }, Vehicles = { "3tnk", "btr.yuri.ai", "isu", SovietBasicArty } }
	},
	Allies = {
		{ Infantry = { "e3", "e1", "e1", "e3", "e1" }, Vehicles = { "apc.ai" } },
		{ Infantry = { "e3", "e1", "e1", "cryt", "cryt", "e1", "cryt", "e3", "snip" }, Vehicles = { "arty", "apc.ai", "pcan", "ptnk" } },
		{ Infantry = { "e3", "e1", "e1", "cryt", "cryt", "e1", "e1", "e3", "snip" }, Vehicles = { "2tnk", "apc.ai", "batf.ai", "ptnk" } },
		{ Infantry = { "e3", "e1", "e1", "cryt", "enfo", "e1", "e1", "e3", "snip" }, Vehicles = { "2tnk", "apc.ai", "chpr", "ptnk" } },
		{ Infantry = { "e3", "e1", "e1", "cryt", "cryt", "e1", "e1", "e3", "snip" }, Vehicles = { "tnkd", "rapc.ai", "batf.ai", "ptnk" } },
		{ Infantry = { "e3", "e1", "e1", "cryt", "cryt", "e1", "e1", "e3", "snip" }, Vehicles = { "2tnk", "rapc.ai", "cryo", "ptnk" } }
	},
	GDI = {
		{ Infantry = { "n1", "n1", "n1", "n3", "n2", "n1", "n1" }, Vehicles = { "vulc.ai", "gdrn", "gdrn", "hmmv"  } },
		{ Infantry = { "n1", "n1", "n1", "n3", "n2", "n1", "n1" }, Vehicles = { "mtnk", "vulc", "gdrn", "mtnk" } },
		{ Infantry = { "n1", "n1", "n1", "n3", "n2", "n1", "n1" }, Vehicles = { "hsam", "hsam", "hsam", "mtnk" } },
		{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n2", "n1", "n1", "n3" }, Vehicles = { "mtnk", "vulc", "apc2.gdiai", "vulc.ai" } },
		{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", "n1", "n1", "n3" }, Vehicles = { "mtnk", "vulc", "apc2.gdiai", "msam" } },
		{ Infantry = { "jjet", "jjet", "jjet", "bjet" }, Vehicles = { "vulc", "vulc", "hmmv.tow", "hmmv.tow" } },
		{ Infantry = { "n1", "n1", "n3" }, Vehicles = { GDIMammothVariant, "vulc", "hsam", "disr", "xo", "xo" } },
		{ Infantry = { "n1", "n3", "n1", "n1", "n2", "n3", "n1", "n1" }, Vehicles = { GDIMammothVariant, "vulc", GDIMammothVariant, "xo" } },
	},
	Nod = {
		{ Infantry = {}, Vehicles = { "bggy", "bike", "bike", "bike", "bggy" } },
		{ Infantry = { "n1", "n3", "n4", "n1" }, Vehicles = { "bggy", "bike", "bike", "stnk.nod", "bike", "bggy" } },
		{ Infantry = { "n3", "n1", "n1", "n4", "n1", "bh", "bh" }, Vehicles = { "ltnk", "ltnk", "hftk", "hftk", "bike" } },
		{ Infantry = { "n3", "n1", "n1", "n4", "n1", "n1c", "acol", "n1" }, Vehicles = { "ltnk", "arty.nod", "ltnk", "stnk.nod" } },
		{ Infantry = { AdvancedCyborg, AdvancedCyborg, BasicCyborg, BasicCyborg, BasicCyborg, BasicCyborg, BasicCyborg }, Vehicles = { "ftnk", "ltnk", "bike" } },
		{ Infantry = { "n3", "n1", "n3", "n1", "n4", "n1", BasicCyborg, AdvancedCyborg }, Vehicles = { "ltnk", "arty.nod", "wtnk" } },
	},
	Scrin = {
		{ Infantry = { "s3", "s1", "s1", "s1", "s3", "s1", "s4", "s4" }, Vehicles = { "intl.ai2", "intl.ai2", GunWalkerSeekerOrLacerator, CorrupterOrDevourer, CorrupterOrDevourer, GunWalkerSeekerOrLacerator } },
		{ Infantry = { "s3", "s1", "s1", "s1", "s3", "s1", "s4", "s4" }, Vehicles = { "intl.ai2", "tpod", GunWalkerSeekerOrLacerator, CorrupterOrDevourer, CorrupterOrDevourer, GunWalkerSeekerOrLacerator } },
		{ Infantry = {}, Vehicles = { "lace", "lace", "lace", "seek", "seek" }, Aircraft = {} },
		{ Infantry = {}, Vehicles = {}, Aircraft = { PacOrDevastator, "deva", "pac" } }
	}
}

Squads = {
	NodVsGDI = {
		AttackValuePerSecond = {
			normal = { Min = 80, Max = 80 },
		},
		DispatchDelay = DateTime.Seconds(30),
		FollowLeader = false,
		Compositions = UnitCompositionsShellmap.Nod,
		AttackPaths = { { NodAttack1.Location, GDIBase.Location }, { NodGroup1.Location, GDIBase.Location }, { GDIAttack1.Location, GDIBase.Location } },
	},
	NodVsScrin = {
		AttackValuePerSecond = {
			normal = { Min = 90, Max = 90 },
		},
		DispatchDelay = DateTime.Seconds(30),
		FollowLeader = false,
		Compositions = UnitCompositionsShellmap.Nod,
		AttackPaths = { { GDICamp.Location, ScrinBase.Location } },
	},
	GDIVsNod = {
		AttackValuePerSecond = {
			normal = { Min = 80, Max = 80 },
		},
		FollowLeader = false,
		ProducerActors = { Infantry = { GDISouthBarracks }, Vehicles = { GDISouthFactory } },
		Compositions = UnitCompositionsShellmap.GDI,
		AttackPaths = { { NodAttack1.Location, NodBase.Location }, { GDIGroup1.Location, NodBase.Location }, { GDIAttack1.Location, NodBase.Location } },
	},
	GDIVsScrin = {
		AttackValuePerSecond = {
			normal = { Min = 80, Max = 80 },
		},
		FollowLeader = false,
		ProducerActors = { Infantry = { GDINorthEastBarracks }, Vehicles = { GDINorthEastFactory } },
		Compositions = UnitCompositionsShellmap.GDI,
		AttackPaths = { { SovietAttack3a.Location, SovietBase.Location }, { NodAttack1.Location, NodBase.Location } },
	},
	GDIVsSoviets = {
		AttackValuePerSecond = {
			normal = { Min = 80, Max = 80 },
		},
		FollowLeader = false,
		ProducerActors = { Infantry = { GDINorthWestBarracks }, Vehicles = { GDINorthWestFactory } },
		Compositions = UnitCompositionsShellmap.GDI,
		AttackPaths = { { SovietGroup1.Location, SovietBase.Location } },
	},
	AlliesVsSoviets = {
		AttackValuePerSecond = {
			normal = { Min = 80, Max = 80 },
		},
		FollowLeader = false,
		ProducerActors = { Infantry = { AlliesNorthWestBarracks }, Vehicles = { AlliesNorthWestFactory } },
		Compositions = UnitCompositionsShellmap.Allies,
		AttackPaths = { { SovietGroup1.Location, SovietBase.Location } },
	},
	SovietVsAllies = {
		AttackValuePerSecond = {
			normal = { Min = 110, Max = 110 },
		},
		FollowLeader = false,
		ProducerActors = { Infantry = { SovietNorthBarracks }, Vehicles = { SovietNorthFactory } },
		Compositions = UnitCompositionsShellmap.Soviet,
		AttackPaths = { { SovietAttack1.Location, SovietAttack2.Location, GDIBase.Location }, { SovietAttack3a.Location, SovietAttack3b.Location, AlliesBase.Location} },
	},
	SovietVsGDI = {
		AttackValuePerSecond = {
			normal = { Min = 110, Max = 110 },
		},
		FollowLeader = false,
		ProducerActors = { Infantry = { SovietSouthBarracks }, Vehicles = { SovietSouthFactory } },
		Compositions = UnitCompositionsShellmap.Soviet,
		AttackPaths = { { SovietAttack1.Location, SovietAttack2.Location, GDIBase.Location }, { SovietAttack3a.Location, SovietAttack3b.Location, GDIBase.Location} },
	},
	ScrinVsSoviets = {
		AttackValuePerSecond = {
			normal = { Min = 125, Max = 125 },
		},
		FollowLeader = false,
		Compositions = UnitCompositionsShellmap.Scrin,
		AttackPaths = { { ScrinAttack1.Location, AlliedCamp.Location, ScrinAttack2a.Location, SovietBase.Location }, { ScrinAttack1.Location, AlliedCamp.Location, ScrinAttack2b.Location, SovietBase.Location } },
	},
	ScrinVsGDI = {
		AttackValuePerSecond = {
			normal = { Min = 90, Max = 90 },
		},
		FollowLeader = false,
		Compositions = UnitCompositionsShellmap.Scrin,
		AttackPaths = { { GDICamp.Location, GDIBase.Location } },
	},
	GDIAir = {
		Interval = {
			normal = DateTime.Minutes(1),
		},
		Compositions = {
			normal = {
				{ Aircraft = { "orca", "orca" } },
				{ Aircraft = { "a10" } }
			},
		},
	},
	NodAir = {
		Interval = {
			normal = DateTime.Minutes(1),
		},
		Compositions = {
			normal = {
				{ Aircraft = { "scrn", "scrn" } },
				{ Aircraft = { "apch", "apch" } },
				{ Aircraft = { "rah", "rah" } },
				{ Aircraft = { "venm", "venm" } }
			},
		},
	},
	ScrinAir = {
		Interval = {
			normal = DateTime.Minutes(1),
		},
		Compositions = {
			normal = {
				{ Aircraft = { "stmr", "stmr" } },
				{ Aircraft = { "enrv", "enrv" } },
			},
		},
	},
	SovietAir = {
		Interval = {
			normal = DateTime.Minutes(1),
		},
		Compositions = {
			normal = {
				{ Aircraft = { "mig", "mig" } },
				{ Aircraft = { "suk", "suk" } },
				{ Aircraft = { "yak", "yak" } },
				{ Aircraft = { "hind", "hind" } },
			},
		},
	},
}

Tick = function()
	Ticks = Ticks + 1

	if Ticks > 1 or not Map.IsPausedShellmap then
		local t = (Ticks + 45) % (360 * Speed) * (math.pi / 180) / Speed;
		Camera.Position = ViewportOrigin + WVec.New(19200 * 1.5 * math.sin(t), 20480 * 1.1 * math.cos(t), 0)
	end

	OncePerSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Greece.Resources = Greece.ResourceCapacity - 500
		USSR.Resources = USSR.ResourceCapacity - 500
		GDI.Resources = GDI.ResourceCapacity - 500
		Nod.Resources = Nod.ResourceCapacity - 500
		Scrin.Resources = Scrin.ResourceCapacity - 500
	end
end

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")
	Scrin = Player.GetPlayer("Scrin")
	Civilian = Player.GetPlayer("Civilian")

	Camera.Position = CameraStart.CenterPosition
	ViewportOrigin = Camera.Position

	Actor.Create("ai.unlimited.power", true, { Owner = Greece })
	Actor.Create("ai.unlimited.power", true, { Owner = USSR })
	Actor.Create("ai.unlimited.power", true, { Owner = GDI })
	Actor.Create("ai.unlimited.power", true, { Owner = Nod })
	Actor.Create("ai.unlimited.power", true, { Owner = Scrin })

	Actor.Create("sonic.upgrade", true, { Owner = GDI })
	Actor.Create("empgren.upgrade", true, { Owner = GDI })
	Actor.Create("sidewinders.upgrade", true, { Owner = GDI })
	Actor.Create("shields.upgrade", true, { Owner = Scrin })
	Actor.Create("tibcore.upgrade", true, { Owner = Nod })

	XODropProxy = Actor.Create("powerproxy.paratroopers.xo", true, { Owner = GDI, })

	AutoRepairAndRebuildBuildings(Greece)
	AutoReplaceHarvesters(Greece)
	AutoRepairAndRebuildBuildings(USSR)
	AutoReplaceHarvesters(USSR)
	AutoRepairAndRebuildBuildings(GDI)
	AutoReplaceHarvesters(GDI)
	AutoRepairAndRebuildBuildings(Nod)
	AutoReplaceHarvesters(Nod)
	AutoRepairAndRebuildBuildings(Scrin)
	AutoReplaceHarvesters(Scrin)

	local civilians = Civilian.GetActorsByTypes({ "c1", "c2", "c3", "c4", "c5", "c6", "c7", "c8", "c9", "c10", "tecn" })
	Utils.Do(civilians, function(a)
		local rand = Utils.RandomInteger(1,3)
		a.Wait(Utils.RandomInteger(1,50))
		if rand == 1 then
			a.Move(CivilianRetreat1.Location)
		else
			a.Move(CivilianRetreat2.Location)
		end
		a.Move(GDIBase.Location, 3)
		a.Move(CivilianEvac.Location)
		a.Destroy()
	end)

	-- setup defenders
	local alliedGroundAttackers = Greece.GetGroundAttackers()

	Utils.Do(alliedGroundAttackers, function(a)
		TargetSwapChance(a, 10, function(p) return not p.IsAlliedWith(Greece) end)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGreeceGroundHunterUnit, function(p) return not p.IsAlliedWith(Greece) end)
	end)

	local sovietGroundAttackers = USSR.GetGroundAttackers()

	Utils.Do(sovietGroundAttackers, function(a)
		TargetSwapChance(a, 10, function(p) return not p.IsAlliedWith(USSR) end)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsUSSRGroundHunterUnit, function(p) return not p.IsAlliedWith(USSR) end)
	end)

	local gdiGroundAttackers = GDI.GetGroundAttackers()

	Utils.Do(gdiGroundAttackers, function(a)
		TargetSwapChance(a, 10, function(p) return not p.IsAlliedWith(GDI) end)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGDIGroundHunterUnit, function(p) return not p.IsAlliedWith(GDI) end)
	end)

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, 10, function(p) return not p.IsAlliedWith(Nod) end)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit, function(p) return not p.IsAlliedWith(Nod) end)
	end)

	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10, function(p) return not p.IsAlliedWith(Scrin) end)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit, function(p) return not p.IsAlliedWith(Scrin) end)
	end)

	-- initial battles
	Trigger.AfterDelay(1, function()
		local gdiGroup1 = Map.ActorsInCircle(GDIGroup1.CenterPosition, WDist.FromCells(10), function(a)
			return a.Owner == GDI and not a.IsDead and a.HasProperty("AttackMove")
		end)
		Utils.Do(gdiGroup1, function(a)
			a.AttackMove(SovietGroup1.Location)
		end)
		local nodGroup1 = Map.ActorsInCircle(NodGroup1.CenterPosition, WDist.FromCells(10), function(a)
			return a.Owner == Nod and not a.IsDead and a.HasProperty("AttackMove")
		end)
		Utils.Do(nodGroup1, function(a)
			a.AttackMove(GDIGroup1.Location)
		end)
		local nodGroup2 = Map.ActorsInCircle(NodGroup2.CenterPosition, WDist.FromCells(10), function(a)
			return a.Owner == Nod and not a.IsDead and a.HasProperty("AttackMove")
		end)
		Utils.Do(nodGroup2, function(a)
			a.AttackMove(GDICamp.Location)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(8), function()
		local nodGroup3 = Map.ActorsInCircle(NodGroup3.CenterPosition, WDist.FromCells(4), function(a)
			return a.Owner == Nod and not a.IsDead and a.HasProperty("AttackMove")
		end)
		Utils.Do(nodGroup3, function(a)
			IdleHunt(a)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(35), function()
		local scrinGroup1 = Map.ActorsInCircle(ScrinGroup1.CenterPosition, WDist.FromCells(10), function(a)
			return a.Owner == Scrin and not a.IsDead and a.HasProperty("AttackMove")
		end)
		Utils.Do(scrinGroup1, function(a)
			a.AttackMove(GDICamp.Location)
			a.AttackMove(GDIBase.Location)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(20), function()
		Utils.Do({ Warthog1, Warthog2, Warthog3, Aurora1, Aurora2 }, function(a)
			if not a.IsDead then
				if a.Type == "a10" then
					a.GrantCondition("speed-boost")
				end
				a.Stance = "HoldFire"
				a.Move(AirstrikeDest.Location)
				a.Destroy()
			end
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(120), function()
		DoSealDrop()
	end)

	Trigger.AfterDelay(DateTime.Seconds(140), function()
		local scrinGroup2 = Map.ActorsInCircle(ScrinGroup2.CenterPosition, WDist.FromCells(10), function(a)
			return a.Owner == Scrin and not a.IsDead and a.HasProperty("AttackMove")
		end)
		Utils.Do(scrinGroup2, function(a)
			a.AttackMove(GDIBase.Location)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		InitAttackSquad(Squads.NodVsGDI, Nod, GDI)
		InitAttackSquad(Squads.GDIVsNod, GDI, Nod)

		Trigger.AfterDelay(DateTime.Seconds(30), function()
			InitAttackSquad(Squads.ScrinVsGDI, Scrin, GDI)
		end)

		Trigger.AfterDelay(DateTime.Seconds(85), function()
			InitAttackSquad(Squads.GDIVsSoviets, GDI, USSR)
			InitAttackSquad(Squads.AlliesVsSoviets, Greece, USSR)
			InitAttackSquad(Squads.GDIVsScrin, GDI, Scrin)
			InitAttackSquad(Squads.SovietVsGDI, USSR, GDI)
			InitAttackSquad(Squads.SovietVsAllies, USSR, Greece)
			InitAttackSquad(Squads.NodVsScrin, Nod, Scrin)
			InitAttackSquad(Squads.ScrinVsSoviets, Scrin, Nod)

			InitAirAttackSquad(Squads.GDIAir, GDI, Scrin, { "stmr", "enrv", "tpod", "devo", "ruin", "pac", "deva" })
			InitAirAttackSquad(Squads.SovietAir, USSR, GDI, { "orca", "a10", "msam", "htnk", "titn", "htnk.ion", "htnk.hover", "htnk.drone", "jugg" })
			InitAirAttackSquad(Squads.ScrinAir, Scrin, GDI, { "orca", "a10", "msam", "htnk", "titn", "htnk.ion", "htnk.hover", "htnk.drone", "jugg" })
		end)

		InitAirAttackSquad(Squads.GDIAir, GDI, Nod, { "arty.nod", "mlrs", "scrn", "apch", "venm", "rah", "rmbc", "ltnk" })
		InitAirAttackSquad(Squads.NodAir, Nod, GDI, { "orca", "a10", "msam", "htnk", "titn", "htnk.ion", "htnk.hover", "htnk.drone", "jugg" })
	end)

	DoXODrop()
end

DoSealDrop = function()
	local entryPath = Utils.Random(SealDropPaths)

	if not FirstSealDropDone then
		FirstSealDropDone = true
		entryPath = { SealDropSpawn.Location, SealDrop3.Location }
	end

	DoHelicopterDrop(Greece, entryPath, "nhaw", { "seal", "seal", "seal", "seal", "seal" },
		function(u)
			if not u.IsDead then
				u.Scatter()
				u.Patrol(SealPatrolPath, true, DateTime.Seconds(8))
			end
		end,
		function(t)
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				if not t.IsDead then
					t.Move(entryPath[1])
					t.Destroy()
				end
			end)
		end
	)
	Trigger.AfterDelay(DateTime.Minutes(4), function()
		DoSealDrop()
	end)
end

DoXODrop = function()
	local aircraft = XODropProxy.TargetParatroopers(GDIAttack1.CenterPosition, Angle.East)

	Utils.Do(aircraft, function(a)
		Trigger.OnPassengerExited(a, function(t, p)
			IdleHunt(p)
		end)
	end)
end
