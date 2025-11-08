MissionDir = "ca|missions/main-campaign/ca25-enmity"


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
	vhard = DateTime.Minutes(2) + DateTime.Seconds(30),
	brutal = DateTime.Minutes(2) + DateTime.Seconds(30)
}

SuperweaponsEnabledTime = {
	easy = DateTime.Seconds((60 * 40) + 41),
	normal = DateTime.Seconds((60 * 25) + 41),
	hard = DateTime.Seconds((60 * 18) + 41),
	vhard = DateTime.Seconds((60 * 15) + 41),
	brutal = DateTime.Seconds((60 * 13) + 41)
}

StructuresToSellToAvoidCapture = { SouthHand1, SouthHand2, SouthAirstrip, SouthConyard, WestHand, CenterHand, Helipad1, Helipad2 }

ShadowUnitCompositions = {
	{ Infantry = {}, Vehicles = { "bike", "bike", "bike", "bike" }, MaxTime = DateTime.Minutes(10) },
	{ Infantry = { "n3", "n1", "n1", "n1", "n4" }, Vehicles = { "bggy", "bggy", "bike", "bike" }, MaxTime = DateTime.Minutes(10) },
	{ Infantry = { "n3", "n1", "n1", "n4" }, Vehicles = { "ltnk", "bggy", "bike" }, MaxTime = DateTime.Minutes(10) },

	{ Infantry = {}, Vehicles = { "stnk.nod", "stnk.nod", "stnk.nod", "sapc.ai", "sapc.ai" }, MinTime = DateTime.Minutes(10) },
	{ Infantry = { "n3", "n1", "n1", "n1", "n1", "n4", "n3", "shad" }, Vehicles = { "ltnk", "ltnk", "ftnk", "arty.nod", "ltnk" }, MinTime = DateTime.Minutes(10) },
	{ Infantry = { "n3", "n1", "shad", "n1", "shad", "shad", "n4", "n1" }, Vehicles = { "stnk.nod", "ltnk", "bggy", "bike", "ltnk" }, MinTime = DateTime.Minutes(10) },

	{ Infantry = { "n3", "n1", "n1", "n1", "n4", "n1", "shad", "n1", "n1", "n1", "n1" }, Vehicles = { "ltnk", "spec", "arty.nod", "stnk.nod", "stnk.nod", "ltnk" }, MinTime = DateTime.Minutes(13) },
	{ Infantry = { "n3", "n1", "shad", "n1", "shad", "shad", "n4", "n1", "n1", "n1", "n1", "n1" }, Vehicles = { "stnk.nod", "spec", "bike", "bggy", "ltnk" }, MinTime = DateTime.Minutes(13) },
}

if IsVeryHardOrAbove() then
	ShadowUnitCompositions = Utils.Concat(ShadowUnitCompositions, {
		{
			Infantry = { "shad", "shad", "shad", "shad", "shad", "shad", "shad" },
			Vehicles = { "spec", "spec", "spec", "spec", "spec", "spec", "spec" },
			MinTime = DateTime.Minutes(16),
			IsSpecial = true
		},
		{
			Infantry = { "n3", "n1", "n1", "n1", "n1", "n1", "n3", "rmbo", "n1", "n1", "n1", "n1", "n1", "n1", "n1", "n1", "n1", "n3", "n1", "n1", "n1", "n3" },
			Vehicles = { "ftnk", "ftnk", "howi", "howi", "howi", "howi", "howi" },
			MinTime = DateTime.Minutes(16),
			RequiredTargetCharacteristics = { "MassInfantry" }
		},
		{
			Infantry = { "n3", "n1", "n1", "n1", "n1", "n3", "n3", "rmbo", "n1", "n1", "n3", "n1", "n1", "n3", "n1", "n1", "n1", "n3", "n1", "n3", "n1", "n3" },
			Vehicles = { "ltnk", "ltnk", "ltnk", "bike", "bike", "bike", "bike", "bike", "stnk.nod", "stnk.nod" },
			MinTime = DateTime.Minutes(16),
			RequiredTargetCharacteristics = { "MassHeavy" }
		},
	})
end

AdjustedShadowUnitCompositions = AdjustCompositionsForDifficulty(ShadowUnitCompositions)

Squads = {
	North = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(6)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		ProducerActors = { Infantry = { NorthHand1, NorthHand2 }, Vehicles = { NorthAirstrip } },
		Compositions = AdjustedShadowUnitCompositions,
		AttackPaths = NorthAttackPaths,
	},
	South = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(4)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		ProducerActors = { Infantry = { SouthHand1, SouthHand2 }, Vehicles = { SouthAirstrip } },
		Compositions = AdjustedShadowUnitCompositions,
		AttackPaths = SouthAttackPaths,
	},
	Air = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Nod,
	},
	BrutalComanches = {
		Delay = DateTime.Minutes(10),
		ActiveCondition = function(squad)
			for _, player in pairs(MissionPlayers) do
				if #player.GetActorsByTypes({ "gtek", "upgc", "eye" }) > 0 then
					return true
				end
			end
			return false
		end,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 24, Max = 24 }),
		Compositions = {
			brutal = {
				Aircraft = { "rah", "rah", "rah", "rah", "rah" }
			}
		}
	}
}

SetupPlayers = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { GDI }
	MissionEnemies = { Nod }
end

WorldLoaded = function()
	SetupPlayers()

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
		PlaySpeechNotificationToMissionPlayers("SignalFlare")
		Notification("Signal flare detected. Reinforcements inbound.")
		Beacon.New(GDI, McvRally.CenterPosition)
		Trigger.AfterDelay(DateTime.Seconds(20), function()
			mcvFlare.Destroy()
		end)
	end)

	Trigger.AfterDelay(HoldOutTime[Difficulty], function()
		PlaySpeechNotificationToMissionPlayers("ReinforcementsArrived")
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
		Utils.Do(MissionPlayers, function(p)
			Actor.Create("recondronedetection", true, { Owner = p })
		end)
		Notification("Recon Drones are now equipped with stealth detection. This should help you locate the Nod bases in the area.")
		MediaCA.PlaySound("c_recondrones", 2)
	end)

	AfterWorldLoaded()
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	OncePerThirtySecondChecks()
	AfterTick()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Nod.Resources = Nod.ResourceCapacity - 500

		if not PlayerHasBuildings(Nod) then
			GDI.MarkCompletedObjective(ObjectiveEliminateNod)
		end

		if MissionPlayersHaveNoRequiredUnits() then
			GDI.MarkFailedObjective(ObjectiveEliminateNod)
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()
	end
end

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % DateTime.Seconds(30) == 0 then
		CalculatePlayerCharacteristics()
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
	AutoRebuildConyards(Nod)
	SellOnCaptureAttempt(StructuresToSellToAvoidCapture)
	InitAiUpgrades(Nod)
	InitAttackSquad(Squads.North, Nod)
	InitAttackSquad(Squads.South, Nod)
	InitAirAttackSquad(Squads.Air, Nod)

	if Difficulty == "brutal" then
		InitAirAttackSquad(Squads.BrutalComanches, Nod, nil, { "gtek", "rmbo", "medi", "upgc", "eye" })
	end

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)

	Actor.Create("ai.unlimited.power", true, { Owner = Nod })

	Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = Nod })
		Actor.Create("ai.superweapons.enabled", true, { Owner = Nod })
	end)
end
