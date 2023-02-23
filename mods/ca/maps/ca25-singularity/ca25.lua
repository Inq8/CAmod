
NWReactors = { NWPower1, NWPower2, NWPower3, NWPower4, NWPower5, NWPower6 }

NEReactors = { NEPower1, NEPower2, NEPower3, NEPower4, NEPower5, NEPower6 }

Squads = {
	ScrinWest = {
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 25 } },
			normal = { { MinTime = 0, Value = 40 } },
			hard = { { MinTime = 0, Value = 60 } },
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false,
			Aircraft = false,
		},
		FollowSquadLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { WestPortal }, Vehicles = { WestWarpSphere } },
		ProducerTypes = { Infantry = { "port" }, Vehicles = { "wsph" }, Aircraft = { "grav" } },
		Units = UnitCompositions.Scrin.Main,
		AttackPaths = { { WestAttackNode1.Location, WestAttackNode2.Location, WestAttackNode3.Location, WestAttackNode4.Location } },
	},
	ScrinEast = {
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 25 } },
			normal = { { MinTime = 0, Value = 40 } },
			hard = { { MinTime = 0, Value = 60 } },
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false,
			Aircraft = false,
		},
		FollowSquadLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { EastPortal }, Vehicles = { EastWarpSphere } },
		ProducerTypes = { Infantry = { "port" }, Vehicles = { "wsph" }, Aircraft = { "grav" } },
		Units = UnitCompositions.Scrin.Main,
		AttackPaths = { { EastAttackNode1.Location, EastAttackNode2.Location, EastAttackNode3.Location } },
	},
	ScrinCenter = {
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 25 } },
			normal = { { MinTime = 0, Value = 40 } },
			hard = { { MinTime = 0, Value = 60 } },
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false,
			Aircraft = false,
		},
		FollowSquadLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { CenterPortal }, Vehicles = { CenterWarpSphere } },
		ProducerTypes = { Infantry = { "port" }, Vehicles = { "wsph" }, Aircraft = { "grav" } },
		Units = UnitCompositions.Scrin.Main,
		AttackPaths = { { CenterAttackNode1.Location, CenterAttackNode2.Location } },
	},
	SovietSlaves = {
		Delay = {
			easy = DateTime.Seconds(210),
			normal = DateTime.Seconds(150),
			hard = DateTime.Seconds(90)
		},
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 10 }, { MinTime = DateTime.Minutes(14), Value = 20 } },
			normal = { { MinTime = 0, Value = 15 }, { MinTime = DateTime.Minutes(12), Value = 33 } },
			hard = { { MinTime = 0, Value = 26 }, { MinTime = DateTime.Minutes(10), Value = 50 } },
		},
		ActiveCondition = function()
			return not SovietsFreed
		end,
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false,
		},
		FollowSquadLeader = true,
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Infantry = { "barr" }, Vehicles = { "weap" } },
		Units = UnitCompositions.Soviet.Main,
		AttackPaths = { { WestAttackNode3.Location, WestAttackNode4.Location } },
	},
	NodSlaves = {
		Delay = {
			easy = DateTime.Seconds(210),
			normal = DateTime.Seconds(150),
			hard = DateTime.Seconds(90)
		},
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 10 }, { MinTime = DateTime.Minutes(14), Value = 20 } },
			normal = { { MinTime = 0, Value = 15 }, { MinTime = DateTime.Minutes(12), Value = 33 } },
			hard = { { MinTime = 0, Value = 26 }, { MinTime = DateTime.Minutes(10), Value = 50 } },
		},
		ActiveCondition = function()
			return not NodFreed
		end,
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false,
		},
		DispatchDelay = DateTime.Seconds(15),
		FollowSquadLeader = true,
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Infantry = { "hand" }, Vehicles = { "airs" } },
		Units = UnitCompositions.Nod.Main,
		AttackPaths = { { EastAttackNode2.Location, EastAttackNode3.Location } },
	},
	AlliedSlaves = {
		Delay = {
			easy = DateTime.Seconds(210),
			normal = DateTime.Seconds(150),
			hard = DateTime.Seconds(90)
		},
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 10 }, { MinTime = DateTime.Minutes(14), Value = 20 } },
			normal = { { MinTime = 0, Value = 15 }, { MinTime = DateTime.Minutes(12), Value = 33 } },
			hard = { { MinTime = 0, Value = 26 }, { MinTime = DateTime.Minutes(10), Value = 50 } },
		},
		ActiveCondition = function()
			return not AlliesFreed
		end,
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false,
		},
		FollowSquadLeader = true,
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Infantry = { "tent" }, Vehicles = { "weap" } },
		Units = UnitCompositions.Allied.Main,
		AttackPaths = { { CenterAttackNode2.Location } },
	},
	USSR = {
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 25 } },
			normal = { { MinTime = 0, Value = 25 } },
			hard = { { MinTime = 0, Value = 25 } },
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false,
		},
		FollowSquadLeader = true,
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Infantry = { "barr" }, Vehicles = { "weap" } },
		Units = UnitCompositions.Soviet.Main,
		AttackPaths = { { WestAttackNode2.Location, WestAttackNode1.Location, WormholeWP.Location } },
	},
	Nod = {
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 25 } },
			normal = { { MinTime = 0, Value = 25 } },
			hard = { { MinTime = 0, Value = 25 } },
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false,
		},
		DispatchDelay = DateTime.Seconds(15),
		FollowSquadLeader = true,
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Infantry = { "hand" }, Vehicles = { "airs" } },
		Units = UnitCompositions.Nod.Main,
		AttackPaths = { { EastAttackNode1.Location, WormholeWP.Location } },
	},
	Greece = {
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 25 } },
			normal = { { MinTime = 0, Value = 25 } },
			hard = { { MinTime = 0, Value = 25 } },
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false,
		},
		FollowSquadLeader = true,
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Infantry = { "tent" }, Vehicles = { "weap" } },
		Units = UnitCompositions.Allied.Main,
		AttackPaths = { { CenterAttackNode1.Location, WormholeWP.Location } },
	},
	ScrinAir = {
		Delay = {
			easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(5),
			hard = DateTime.Minutes(4)
		},
		Interval = {
			easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(2)
		},
		QueueProductionStatuses = {
			Aircraft = false
		},
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Aircraft = { "grav" } },
		Units = {
			easy = {
				{ Aircraft = { "stmr" } }
			},
			normal = {
				{ Aircraft = { "stmr", "stmr" } },
				{ Aircraft = { "enrv" } },
			},
			hard = {
				{ Aircraft = { "stmr", "stmr", "stmr" } },
				{ Aircraft = { "enrv", "enrv" } },
			}
		}
	},
}

RiftEnabledTime = {
	easy = DateTime.Seconds((60 * 23) + 17),
	normal = DateTime.Seconds((60 * 18) + 17),
	hard = DateTime.Seconds((60 * 13) + 17),
}

MADTankAttackDelay = {
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(3),
	hard = DateTime.Minutes(3),
}

ChronoTanksDelay = {
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(3),
	hard = DateTime.Minutes(3),
}

HackersDelay = {
	easy = DateTime.Minutes(2),
	normal = DateTime.Minutes(2),
	hard = DateTime.Minutes(2),
}

WorldLoaded = function()
    GDI = Player.GetPlayer("GDI")
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	Nod = Player.GetPlayer("Nod")
    Scrin = Player.GetPlayer("Scrin")
	AlliedSlaves = Player.GetPlayer("AlliedSlaves")
	SovietSlaves = Player.GetPlayer("SovietSlaves")
	NodSlaves = Player.GetPlayer("NodSlaves")
	Kane = Player.GetPlayer("Kane")
	MissionPlayer = GDI
	TimerTicks = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(GDI)
	InitScrin()
	InitNodSlaves()
	InitSovietSlaves()
	InitAlliedSlaves()

	ObjectiveDestroyMothership = GDI.AddObjective("Destroy the Scrin Mothership.")

	Trigger.OnAllKilledOrCaptured(NWReactors, function()
		ScrinDefenseBuff1.Destroy()
		if ScrinDefenseBuff2.IsDead then
			IonConduits.Destroy()
			if NodFreed then
				InitHackers()
			end
		end

		if not MADTank.IsDead and MADTankInvulnToken ~= nil then
			MADTank.RevokeCondition(MADTankInvulnToken)
		end
	end)

	Trigger.OnAllKilledOrCaptured(NEReactors, function()
		ScrinDefenseBuff2.Destroy()
		if ScrinDefenseBuff1.IsDead then
			IonConduits.Destroy()
			if NodFreed then
				InitHackers()
			end
		end
	end)

	Trigger.OnKilled(AlliedMastermind, function()
		FlipSlaveFaction(AlliedSlaves)
	end)
	Trigger.OnKilled(SovietMastermind, function()
		FlipSlaveFaction(SovietSlaves)
	end)
	Trigger.OnKilled(NodMastermind, function()
		FlipSlaveFaction(NodSlaves)
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Notification("Beginning air attack run. Let's see what we're up against.")
	end)

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		DoInterceptors()
	end)

	Trigger.AfterDelay(DateTime.Seconds(25), function()
		Notification("We barely made a scratch! We'll need you to bring those shields down before we can do damage.")
	end)

	local wormholeFootprint = Utils.ExpandFootprint({ WormholeWP.Location }, true)
	wormholeFootprint = Utils.ExpandFootprint(wormholeFootprint, true)
	Trigger.OnEnteredFootprint(wormholeFootprint, function(a, id)
		if a.Owner == Kane and not a.IsDead then
			a.Stop()
			a.Destroy()
		end
	end)

	local revealPoints = { EntranceReveal1, EntranceReveal2, EntranceReveal3, GrandCannonReveal1, GrandCannonReveal2 }
	Utils.Do(revealPoints, function(p)
		Trigger.OnEnteredProximityTrigger(p.CenterPosition, WDist.New(11 * 1024), function(a, id)
			if a.Owner == GDI and a.Type ~= "smallcamera" then
				Trigger.RemoveProximityTrigger(id)
				local camera = Actor.Create("smallcamera", true, { Owner = GDI, Location = p.Location })
				Trigger.AfterDelay(DateTime.Seconds(4), function()
					camera.Destroy()
				end)
			end
		end)
	end)

	Trigger.OnKilled(Mothership, function(self, killer)
		DoFinale()
	end)
end

MoveToWormhole = function(a)
	if not a.IsDead then
		a.Stop()
		a.Scatter()
		a.Move(WormholeWP.Location)
		Trigger.AfterDelay(DateTime.Seconds(4), function()
			MoveToWormhole(a)
		end)
	end
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Scrin.Resources = Scrin.ResourceCapacity - 500
		NodSlaves.Resources = NodSlaves.ResourceCapacity - 500
		AlliedSlaves.Resources = AlliedSlaves.ResourceCapacity - 500
		SovietSlaves.Resources = SovietSlaves.ResourceCapacity - 500
		Nod.Resources = Nod.ResourceCapacity - 500
		USSR.Resources = USSR.ResourceCapacity - 500
		Greece.Resources = Greece.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocation()

		if not ShieldsOffline and not SignalTransmitter.IsDead and SignalTransmitter.Owner == GDI then
			ShieldsOffline = true
			MothershipShields.Destroy()
			Notification("The Mothership's shields are down! Air attacks resuming.")
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				DoInterceptors()
			end)
			Trigger.AfterDelay(DateTime.Minutes(5), function()
				DoInterceptors()
			end)
		end
	end
end

InitScrin = function()
	RebuildExcludes.Scrin = { Types = { "sign", "rift" }, Actors = { NWPower1, NWPower2, NWPower3, NWPower4, NWPower5, NWPower6, NWPower7, NWPower8, NEPower1, NEPower2, NEPower3, NEPower4, NEPower5, NEPower6, NEPower7, NEPower8 } }

	AutoRepairAndRebuildBuildings(Scrin, 15)
	SetupRefAndSilosCaptureCredits(Scrin)
	AutoReplaceHarvesters(Scrin)

	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)

	Mothership.Attack(Wormhole, true, true)
	Actor.Create("POWERCHEAT", true, { Owner = Scrin })
	Actor.Create("shields.upgrade", true, { Owner = Scrin })
	ScrinDefenseBuff1 = Actor.Create("scrindefensebuff1", true, { Owner = Scrin })
	ScrinDefenseBuff2 = Actor.Create("scrindefensebuff2", true, { Owner = Scrin })
	IonConduits = Actor.Create("ioncon.upgrade", true, { Owner = Scrin })
	MothershipShields = Actor.Create("mothership.shields", true, { Owner = Scrin })

	if Difficulty == "hard" then
		Trigger.AfterDelay(DateTime.Minutes(15), function()
			Actor.Create("carapace.upgrade", true, { Owner = Scrin })
		end)
	end

	Trigger.AfterDelay(RiftEnabledTime[Difficulty], function()
		if not RiftGenerator.IsDead then
			RiftGenerator.GrantCondition("rift-enabled")
		end
	end)

	Trigger.AfterDelay(Squads.ScrinAir.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.ScrinAir, Scrin, GDI, { "harv.td", "msam", "hsam", "nuke", "nuk2", "orca", "a10", "a10.upg", "auro", "htnk", "htnk.drone", "htnk.ion", "htnk.hover", "titn", "titn.rail" })
	end)
end

InitNodSlaves = function()
	AutoRepairAndRebuildBuildings(NodSlaves, 15)
	SetupRefAndSilosCaptureCredits(NodSlaves)
	AutoReplaceHarvesters(NodSlaves)

	local nodGroundAttackers = NodSlaves.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)

	Trigger.AfterDelay(Squads.NodSlaves.Delay[Difficulty], function()
		InitAttackSquad(Squads.NodSlaves, NodSlaves)
	end)
end

InitSovietSlaves = function()
	AutoRepairAndRebuildBuildings(SovietSlaves, 15)
	SetupRefAndSilosCaptureCredits(SovietSlaves)
	AutoReplaceHarvesters(SovietSlaves)

	local sovietGroundAttackers = SovietSlaves.GetGroundAttackers()

	Utils.Do(sovietGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsUSSRGroundHunterUnit)
	end)

	Trigger.AfterDelay(Squads.SovietSlaves.Delay[Difficulty], function()
		InitAttackSquad(Squads.SovietSlaves, SovietSlaves)
	end)
end

InitAlliedSlaves = function()
	AutoRepairAndRebuildBuildings(AlliedSlaves, 15)
	SetupRefAndSilosCaptureCredits(AlliedSlaves)
	AutoReplaceHarvesters(AlliedSlaves)

	local alliedGroundAttackers = AlliedSlaves.GetGroundAttackers()

	Utils.Do(alliedGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGreeceGroundHunterUnit)
	end)

	Trigger.AfterDelay(Squads.AlliedSlaves.Delay[Difficulty], function()
		InitAttackSquad(Squads.AlliedSlaves, AlliedSlaves)
	end)
end

InitNod = function()
	AutoRepairAndRebuildBuildings(Nod, 15)
	SetupRefAndSilosCaptureCredits(Nod)
	AutoReplaceHarvesters(Nod)
	InitAttackSquad(Squads.Nod, Nod, Scrin)
end

InitUSSR = function()
	AutoRepairAndRebuildBuildings(USSR, 15)
	SetupRefAndSilosCaptureCredits(USSR)
	AutoReplaceHarvesters(USSR)
	InitAttackSquad(Squads.USSR, USSR, Scrin)
end

InitGreece = function()
	AutoRepairAndRebuildBuildings(Greece, 15)
	SetupRefAndSilosCaptureCredits(Greece)
	AutoReplaceHarvesters(Greece)
	InitAttackSquad(Squads.Greece, Greece, Scrin)
end

InitHackers = function()
	Trigger.AfterDelay(HackersDelay[Difficulty], function()
		if SignalTransmitter.IsDead then
			return
		end

		Media.DisplayMessage("Commander, we are sending you a squad of hackers. Use them to hack into the Scrin Signal Transmitter and we will be able to bring down the Mothership's shields.", "Nod Commander", HSLColor.FromHex("FF0000"))

		Media.PlaySpeechNotification(GDI, "SignalFlare")
		local hackerFlare = Actor.Create("flare", true, { Owner = GDI, Location = HackerDropLanding.Location })
		Trigger.AfterDelay(DateTime.Seconds(10), function()
			hackerFlare.Destroy()
		end)

		Beacon.New(GDI, SignalTransmitter.CenterPosition)
		Beacon.New(GDI, HackerDropLanding.CenterPosition)

		local entryPath = { HackerDropSpawn.Location, HackerDropLanding.Location }
		DoHelicopterDrop(Nod, entryPath, "tran.evac", { "hack", "hack", "hack" }, nil, function(t)
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				if not t.IsDead then
					t.Move(entryPath[1])
					t.Destroy()
				end

				local hackers = Nod.GetActorsByType("hack")
				Utils.Do(hackers, function(a)
					a.Owner = GDI
				end)
			end)
		end)
	end)
end

InitChronoTanks = function()
	Trigger.AfterDelay(ChronoTanksDelay[Difficulty], function()
		if ScrinDefenseBuff2.IsDead then
			return
		end

		Notification("The Allies have provided us with a squadron of Chrono Tanks. We can use them to disrupt Scrin power in the north east.")
		Media.PlaySpeechNotification(GDI, "SignalFlare")
		local northEastPowerFlare = Actor.Create("flare", true, { Owner = GDI, Location = NorthEastPowerBeacon.Location })
		Trigger.AfterDelay(DateTime.Seconds(10), function()
			northEastPowerFlare.Destroy()
		end)

		Beacon.New(GDI, NorthEastPowerBeacon.CenterPosition)
		Beacon.New(GDI, ChronoTankBeacon.CenterPosition)

		Lighting.Flash("Chronoshift", 10)
		Media.PlaySound("chrono2.aud")
		Actor.Create("ctnk.reinforce", true, { Owner = GDI, Location = ChronoTankSpawn1.Location, Facing = Angle.NorthEast })
		Actor.Create("ctnk.reinforce", true, { Owner = GDI, Location = ChronoTankSpawn2.Location, Facing = Angle.NorthEast })
		Actor.Create("ctnk.reinforce", true, { Owner = GDI, Location = ChronoTankSpawn3.Location, Facing = Angle.NorthEast })
		Actor.Create("ctnk.reinforce", true, { Owner = GDI, Location = ChronoTankSpawn4.Location, Facing = Angle.NorthEast })
		Actor.Create("ctnk.reinforce", true, { Owner = GDI, Location = ChronoTankSpawn5.Location, Facing = Angle.NorthEast })
		Actor.Create("ctnk.reinforce", true, { Owner = GDI, Location = ChronoTankSpawn6.Location, Facing = Angle.NorthEast })
	end)
end

InitMADTankAttack = function()
	Trigger.AfterDelay(MADTankAttackDelay[Difficulty] - DateTime.Seconds(20), function()
		if ScrinDefenseBuff1.IsDead then
			return
		end
		Notification("Signal flare detected. The Soviets are sending a MAD Tank to disrupt Scrin power in the north west. They have requested we rendezvous and provide escort.")

		Media.PlaySpeechNotification(GDI, "SignalFlare")
		local northWestPowerFlare = Actor.Create("flare", true, { Owner = GDI, Location = MADTankPath9.Location })
		local madTankFlare = Actor.Create("flare", true, { Owner = GDI, Location = MADTankPath1.Location })
		Trigger.AfterDelay(DateTime.Seconds(20), function()
			madTankFlare.Destroy()
		end)

		Beacon.New(GDI, MADTankPath9.CenterPosition)
		Beacon.New(GDI, MADTankPath1.CenterPosition)
	end)

	Trigger.AfterDelay(MADTankAttackDelay[Difficulty], function()
		if ScrinDefenseBuff1.IsDead then
			return
		end

		MADTank = Actor.Create("qtnk", true, { Owner = USSR, Location = MADTankSpawn.Location, Facing = Angle.East })
		MADTank.Move(MADTankPath1.Location)
		MADTank.Move(MADTankPath2.Location)
		MADTank.Move(MADTankPath3.Location)
		MADTank.Move(MADTankPath4.Location)
		MADTank.Move(MADTankPath5.Location)
		MADTank.Move(MADTankPath6.Location)
		MADTank.Move(MADTankPath7.Location)
		MADTank.Move(MADTankPath8.Location)
		MADTank.Move(MADTankPath9.Location)
		MADTank.Wait(25)
		MADTank.MadTankDetonate()

		Trigger.OnDamaged(MADTank, function(self, attacker, damage)
			if self.Health < self.MaxHealth / 3 and not IsMADTankIronCurtained and not MADTank.IsDead then
				IsMADTankIronCurtained = true
				MADTankInvulnToken = MADTank.GrantCondition("invulnerability")
				Media.PlaySound("ironcur9.aud")
				Trigger.AfterDelay(DateTime.Minutes(3), function()
					if not MADTank.IsDead and MADTankInvulnToken ~= nil then
						MADTank.RevokeCondition(MADTankInvulnToken)
					end
				end)
			end
		end)
	end)
end

DoInterceptors = function()
	if Mothership.IsDead then
		return
	end

	local mothershipCamera = Actor.Create("camera", true, { Owner = GDI, Location = Mothership.Location })

	Trigger.AfterDelay(1, function()
		Media.PlaySound("interceptors.aud")
		local interceptor1 = Actor.Create("yf23.interceptor", true, { Owner = GDI, Location = InterceptorSpawn1.Location, CenterPosition = InterceptorSpawn1.CenterPosition + WVec.New(0, 0, Actor.CruiseAltitude("yf23.interceptor")), Facing = Angle.North })

		if not Mothership.IsDead then
			interceptor1.Attack(Mothership)
		end

		Trigger.OnIdle(interceptor1, function(a)
			a.Move(InterceptorExit1.Location)
			a.Destroy()
		end)

		Trigger.AfterDelay(8, function()
			local interceptor2 = Actor.Create("yf23.interceptor", true, { Owner = GDI, Location = InterceptorSpawn2.Location, CenterPosition = InterceptorSpawn2.CenterPosition + WVec.New(0, 0, Actor.CruiseAltitude("yf23.interceptor")), Facing = Angle.North })
			local interceptor3 = Actor.Create("yf23.interceptor", true, { Owner = GDI, Location = InterceptorSpawn3.Location, CenterPosition = InterceptorSpawn3.CenterPosition + WVec.New(0, 0, Actor.CruiseAltitude("yf23.interceptor")), Facing = Angle.North })
			interceptor2.Move(InterceptorWP2.Location)
			interceptor3.Move(InterceptorWP3.Location)

			if not Mothership.IsDead then
				interceptor2.Attack(Mothership)
				interceptor3.Attack(Mothership)
			end

			Trigger.OnIdle(interceptor2, function(a)
				a.Move(InterceptorExit2.Location)
				a.Destroy()
			end)
			Trigger.OnIdle(interceptor3, function(a)
				a.Move(InterceptorExit3.Location)
				a.Destroy()
			end)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(20), function()
		mothershipCamera.Destroy()
	end)
end

FlipSlaveFaction = function(player)
	local attackPath

	if player == NodSlaves then
		targetPlayer = Nod
		NodFreed = true
		attackPath = { EastAttackNode1.Location, WormholeWP.Location }
		InitNod()
		InitAttackSquad(Squads.ScrinEast, Scrin)
		if ScrinDefenseBuff1.IsDead and ScrinDefenseBuff2.IsDead then
			InitHackers()
		end
	elseif player == SovietSlaves then
		targetPlayer = USSR
		SovietsFreed = true
		attackPath = { WestAttackNode1.Location, WormholeWP.Location }
		InitUSSR()
		InitAttackSquad(Squads.ScrinWest, Scrin)
		InitMADTankAttack()
	elseif player == AlliedSlaves then
		targetPlayer = Greece
		AlliesFreed = true
		attackPath = { CenterAttackNode1.Location, WormholeWP.Location }
		InitGreece()
		InitAttackSquad(Squads.ScrinCenter, Scrin)
		InitChronoTanks()
	end

	local actors = player.GetActors()

	Utils.Do(actors, function(a)
		if not a.IsDead and a.IsInWorld and a.Type ~= "player" and a.Type ~= "rmbc" and a.Type ~= "enli" and a.Type ~= "tplr" and a.Type ~= "n3c" then
			a.Owner = targetPlayer
			Trigger.ClearAll(a)
			Trigger.AfterDelay(1, function()
				if not a.IsDead then
					if a.HasProperty("AttackMove") then
						a.Stop()
						a.AttackMove(attackPath[1])
						a.AttackMove(attackPath[2])
					elseif a.HasProperty("FindResources") then
						a.Stop()
						a.FindResources()
					end
				end
			end)
		end
	end)
end

DoFinale = function()
	Lighting.Flash("Chronoshift", 10)

	Lighting.Ambient = 0.8
	Lighting.Red = 1
	Lighting.Blue = 1.2
	Lighting.Green = 0.8

	Wormhole.Destroy()
	Actor.Create("wormholelg", true, { Owner = Scrin, Location = WormholeWP.Location })
	Actor.Create("camera", true, { Owner = GDI, Location = WormholeWP.Location })

	local kane = Actor.Create("kane", true, { Owner = Kane, Location = KaneSpawn.Location, Facing = Angle.South })
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Media.DisplayMessage("Well commander, we meet at last, and will again. You have played your part impeccably. The final domino falls and the way is open. Come brothers, our future awaits!", "Kane", HSLColor.FromHex("FF0000"))
		Beacon.New(GDI, kane.CenterPosition)

		local cyborgs = NodSlaves.GetActorsByTypes({ "rmbc", "enli", "tplr", "n3c" })

		Utils.Do(cyborgs, function(a)
			a.Owner = Kane

			Trigger.AfterDelay(1, function()
				if not a.IsDead then
					a.Stop()
					a.GrantCondition("kane-revealed")
					a.Scatter()
				end
				Trigger.AfterDelay(DateTime.Seconds(7), function()
					--a.Stance = "HoldFire"
					MoveToWormhole(a)
				end)
			end)
		end)

		Trigger.AfterDelay(DateTime.Seconds(10), function()
			kane.Move(WormholeWP.Location)
		end)

		Trigger.AfterDelay(DateTime.Seconds(20), function()
			UserInterface.SetMissionText("To be continued...", HSLColor.Red)
		end)

		Trigger.AfterDelay(DateTime.Seconds(30), function()
			GDI.MarkCompletedObjective(ObjectiveDestroyMothership)
		end)
	end)
end
