
PowerGrids = {
	{
		Providers = { NPower1, NPower2, NPower3, NPower4 },
		Consumers = { NPowered1, NPowered2, NPowered3, NPowered4, NPowered5, NPowered6, NPowered7, NPowered8, NPowered9, NPowered10, NPowered11 },
	},
	{
		Providers = { SPower1, SPower2, SPower3, SPower4 },
		Consumers = { SPowered1, SPowered2, SPowered3 },
	},
	{
		Providers = { WPower1, WPower2 },
		Consumers = { WPowered1, WPowered2 },
	},
}

NorthAttackPaths = {
	{ AttackNode1.Location, AttackNode2.Location, AttackNode3.Location, AttackNode5.Location },
	{ AttackNode1.Location, AttackNode2.Location, AttackNode4.Location, AttackNode5.Location },
	{ AttackNode1.Location, AttackNode2.Location, AttackNode15.Location, AttackNode5.Location },
	{ AttackNode1.Location, AttackNode2.Location, AttackNode4.Location, AttackNode6.Location, AttackNode5.Location },
	{ AttackNode13.Location, AttackNode14.Location, AttackNode16.Location, AttackNode10.Location, AttackNode5.Location },
	{ AttackNode13.Location, AttackNode14.Location, AttackNode6.Location, AttackNode5.Location },
	{ AttackNode13.Location, AttackNode11.Location, AttackNode10.Location, AttackNode5.Location },
	{ AttackNode13.Location, AttackNode12.Location, AttackNode10.Location, AttackNode5.Location },
}

SouthAttackPaths = {
	{ AttackNode10.Location, AttackNode5.Location },
	{ AttackNode9.Location, AttackNode5.Location },
	{ AttackNode8.Location, AttackNode7.Location, AttackNode5.Location },
}

HoldOutTime = {
	easy = DateTime.Minutes(2) - DateTime.Seconds(30),
	normal = DateTime.Minutes(2),
	hard = DateTime.Minutes(2) + DateTime.Seconds(30),
}

ChemMissileEnabledTime = {
	easy = DateTime.Seconds((60 * 25) + 41),
	normal = DateTime.Seconds((60 * 20) + 41),
	hard = DateTime.Seconds((60 * 15) + 41),
}

StructuresToSellToAvoidCapture = { SouthHand1, SouthHand2, SouthAirstrip, SouthConyard, WestHand, CenterHand, Helipad1, Helipad2 }

ShadowUnitCompositions = AdjustCompositionsForDifficulty({
	{ Infantry = {}, Vehicles = { "bike", "bike", "bike", "bike" }, MaxTime = DateTime.Minutes(10) },
	{ Infantry = { "n3", "n1", "n1", "n1", "n4" }, Vehicles = { "bggy", "bggy", "bike", "bike" }, MaxTime = DateTime.Minutes(10) },
	{ Infantry = { "n3", "n1", "n1", "n4" }, Vehicles = { "ltnk", "bggy", "bike" }, MaxTime = DateTime.Minutes(10) },

	{ Infantry = {}, Vehicles = { "stnk.nod", "stnk.nod", "stnk.nod", "sapc.ai" }, MinTime = DateTime.Minutes(10) },
	{ Infantry = { "n3", "n1", "n1", "n1", "n1", "n4", "n3", "shad" }, Vehicles = { "ltnk", "ltnk", "ftnk", "arty.nod" }, MinTime = DateTime.Minutes(10) },
	{ Infantry = { "n3", "n1", "shad", "n1", "shad", "shad", "n4", "n1" }, Vehicles = { "stnk.nod", "ltnk", "bggy", "bike" }, MinTime = DateTime.Minutes(10) },

	{ Infantry = { "n3", "n1", "n1", "n1", "n4", "n1", "shad" }, Vehicles = { "ltnk", "spec", "arty.nod", "stnk.nod" }, MinTime = DateTime.Minutes(13) },
	{ Infantry = { "n3", "n1", "shad", "n1", "shad", "shad", "n4", "n1" }, Vehicles = { "stnk.nod", "spec", "bike", "bggy" }, MinTime = DateTime.Minutes(13) },
})

Squads = {
	North = {
		Delay = {
			easy = DateTime.Minutes(8),
			normal = DateTime.Minutes(6),
			hard = DateTime.Minutes(4)
		},
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 25 },
			normal = { Min = 25, Max = 50 },
			hard = { Min = 40, Max = 80 },
		},
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		ProducerActors = { Infantry = { NorthHand1, NorthHand2 }, Vehicles = { NorthAirstrip } },
		ProducerTypes = { Infantry = { "hand" } },
		Units = ShadowUnitCompositions,
		AttackPaths = NorthAttackPaths,
	},
	South = {
		Delay = {
			easy = DateTime.Minutes(5),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(3)
		},
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 25 },
			normal = { Min = 25, Max = 50 },
			hard = { Min = 40, Max = 80 },
		},
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		ProducerActors = { Infantry = { SouthHand1, SouthHand2 }, Vehicles = { SouthAirstrip } },
		ProducerTypes = { Infantry = { "hand" } },
		Units = ShadowUnitCompositions,
		AttackPaths = SouthAttackPaths,
	},
	Air = {
		Delay = {
			easy = DateTime.Minutes(13),
			normal = DateTime.Minutes(12),
			hard = DateTime.Minutes(11)
		},
		AttackValuePerSecond = {
			easy = { Min = 7, Max = 7 },
			normal = { Min = 14, Max = 14 },
			hard = { Min = 21, Max = 21 },
		},
		ProducerTypes = { Aircraft = { "hpad.td" } },
		Units = {
			easy = {
				{ Aircraft = { "apch" } }
			},
			normal = {
				{ Aircraft = { "apch", "apch" } },
				{ Aircraft = { "scrn" } },
				{ Aircraft = { "rah" } }
			},
			hard = {
				{ Aircraft = { "apch", "apch", "apch" } },
				{ Aircraft = { "scrn", "scrn" } },
				{ Aircraft = { "rah", "rah" } }
			}
		},
	},
}

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")
	MissionPlayers = { GDI }
	EnforceAiBuildRadius = true

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(GDI)
	InitNod()

	if Difficulty == "easy" then
		NormalHardOnlyArty.Destroy()
		NormalHardOnlyStnk.Destroy()
		NormalHardOnlyLtnk.Destroy()
	end

	ObjectiveEliminateNod = GDI.AddObjective("Eliminate all Nod forces.")

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		local mainAmbushers = Map.ActorsInBox(MainAmbushTopLeft.CenterPosition, MainAmbushBottomRight.CenterPosition, function(a)
			return a.Owner == Nod and not a.IsDead and a.HasProperty("Hunt")
		end)

		local secondaryAmbushers = Map.ActorsInBox(SecondaryAmbushTopLeft.CenterPosition, SecondaryAmbushBottomRight.CenterPosition, function(a)
			return a.Owner == Nod and not a.IsDead and a.HasProperty("Hunt")
		end)

		Utils.Do(mainAmbushers, function(a)
			a.Hunt()
		end)

		Utils.Do(secondaryAmbushers, function(a)
			a.Hunt()
		end)
	end)

	Trigger.AfterDelay(HoldOutTime[Difficulty] - DateTime.Seconds(20), function()
		local mcvFlare = Actor.Create("flare", true, { Owner = GDI, Location = McvRally.Location })
		Media.PlaySpeechNotification(GDI, "SignalFlare")
		Notification("Signal flare detected. Reinforcements inbound.")
		Beacon.New(GDI, McvRally.CenterPosition)
		Trigger.AfterDelay(DateTime.Seconds(20), function()
			mcvFlare.Destroy()
		end)
	end)

	Trigger.AfterDelay(HoldOutTime[Difficulty], function()
		Media.PlaySpeechNotification(GDI, "ReinforcementsArrived")
		Notification("Reinforcements have arrived.")
		Reinforcements.Reinforce(GDI, { "hmmv", "mtnk", "amcv", "mtnk" }, { McvSpawn.Location, McvRally.Location }, 75)
		Beacon.New(GDI, McvRally.CenterPosition)
		GDI.Cash = 6000 + CashAdjustments[Difficulty]
	end)

	Trigger.OnKilled(Church1, function(self, killer)
		Actor.Create("moneycrate", true, { Owner = GDI, Location = Church1.Location })
	end)

	Trigger.OnKilled(Church2, function(self, killer)
		Actor.Create("moneycrate", true, { Owner = GDI, Location = Church2.Location })
	end)

	Utils.Do(PowerGrids, function(grid)
		Trigger.OnAllKilledOrCaptured(grid.Providers, function()
			Utils.Do(grid.Consumers, function(consumer)
				if not consumer.IsDead then
					consumer.GrantCondition("disabled")
				end
			end)
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(22), function()
		Actor.Create("recondronedetection", true, { Owner = GDI })
		Notification("Recon Drones are now equipped with stealth detection. This should help you locate the Nod bases in the area.")
		MediaCA.PlaySound("c_recondrones", 2)
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Nod.Resources = Nod.ResourceCapacity - 500

		if not PlayerHasBuildings(Nod) then
			GDI.MarkCompletedObjective(ObjectiveEliminateNod)
		end

		if GDI.HasNoRequiredUnits() then
			GDI.MarkFailedObjective(ObjectiveEliminateNod)
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()
	end
end

InitNod = function()
	if Difficulty == "easy" then
		RebuildExcludes.Nod = { Types = { "obli", "gun.nod", "nuke", "nuk2", "mslo.nod" } }
	else
		RebuildExcludes.Nod = { Types = { "nuke", "nuk2", "mslo.nod" } }
	end

	AutoRepairAndRebuildBuildings(Nod, 15)
	SetupRefAndSilosCaptureCredits(Nod)
	AutoReplaceHarvesters(Nod)
	InitAiUpgrades(Nod)

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)

	Actor.Create("ai.unlimited.power", true, { Owner = Nod })

	Trigger.AfterDelay(ChemMissileEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = Nod })
	end)

	Trigger.AfterDelay(Squads.North.Delay[Difficulty], function()
		InitAttackSquad(Squads.North, Nod)
	end)

	Trigger.AfterDelay(Squads.South.Delay[Difficulty], function()
		InitAttackSquad(Squads.South, Nod)
	end)

	Trigger.AfterDelay(Squads.Air.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.Air, Nod, GDI, { "harv.td", "atwr", "msam", "htnk", "htnk.ion", "gtek", "dome" })
	end)

	Utils.Do(StructuresToSellToAvoidCapture, function(self)
		Trigger.OnEnteredProximityTrigger(self.CenterPosition, WDist.New(3 * 1024), function(a, id)
			if a.Owner == GDI and a.Type == "n6" then
				Trigger.RemoveProximityTrigger(id)
				if not self.IsDead then
					self.Sell()
				end
			end
		end)
	end)
end
