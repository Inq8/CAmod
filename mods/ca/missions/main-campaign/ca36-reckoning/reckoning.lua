MissionDir = "ca|missions/main-campaign/ca36-reckoning"

ExterminatorsStartTime = {
	easy = DateTime.Minutes(10),
	normal = DateTime.Minutes(8),
	hard = DateTime.Minutes(6),
	vhard = DateTime.Minutes(6),
	brutal = DateTime.Minutes(6)
}

ExterminatorsInterval = {
	easy = DateTime.Minutes(7),
	normal = DateTime.Minutes(6),
	hard = DateTime.Minutes(5),
	vhard = DateTime.Minutes(4),
	brutal = DateTime.Minutes(3) + DateTime.Seconds(20)
}

ExterminatorAttackCount = {
	easy = 4,
	normal = 4,
	hard = 50,
	vhard = 50,
	brutal = 50
}

Exterminators = {
	{ SpawnLocation = ExterminatorFirstSpawn.Location, Path = { Exterminator1Patrol1.Location, Exterminator1Patrol2.Location, Exterminator1Patrol3.Location, Exterminator1Patrol4.Location } },
	{ SpawnLocation = ExterminatorSpawnWest.Location, Path = { Exterminator2Patrol1.Location, Exterminator2Patrol2.Location, Exterminator2Patrol3.Location, Exterminator2Patrol4.Location } },
	{ SpawnLocation = ExterminatorSpawnWest.Location, Path = { Exterminator3Patrol1.Location, Exterminator3Patrol2.Location, Exterminator3Patrol3.Location, Exterminator3Patrol4.Location } },
	{ SpawnLocation = ExterminatorSpawnEast.Location, Path = { Exterminator4Patrol1.Location, Exterminator4Patrol2.Location, Exterminator4Patrol3.Location, Exterminator4Patrol4.Location } },
}

SuperweaponsEnabledTime = {
	easy = DateTime.Seconds((60 * 50) + 17),
	normal = DateTime.Seconds((60 * 35) + 17),
	hard = DateTime.Seconds((60 * 25) + 17),
	vhard = DateTime.Seconds((60 * 20) + 17),
	brutal = DateTime.Seconds((60 * 15) + 17)
}

if IsHardOrAbove() then
	table.insert(UnitCompositions.Scrin, {
		Infantry = { "s3", "s4", "evis", "evis", "evis", "evis", "s1", "s1", "s4", "s1", "s4", "s1", "s4", "s1", "mast" },
		Vehicles = { "shrw", TripodVariant, TripodVariant, "shrw", CorrupterOrDevourer, "oblt", "shrw" },
		Aircraft = { PacOrDevastator, "pac" },
		MinTime = DateTime.Minutes(22)
	})
end

AdjustedScrinCompositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin)

local friendlyDifficulty = Difficulty
if Difficulty == "brutal" then
	friendlyDifficulty = "vhard"
end

Squads = {
	ScrinMain = {
		ActiveCondition = function()
			return DateTime.GameTime < DateTime.Minutes(35) or DateTime.GameTime > DateTime.Minutes(40)
		end,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(4)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 40, Max = 80 }),
		FollowLeader = true,
		ProducerActors = { Infantry = { Portal1 }, Vehicles = { WarpSphere1 }, Aircraft = { GravityStabilizer1, GravityStabilizer2 } },
		Compositions = AdjustedScrinCompositions,
		AttackPaths = {
			{ L1.Location, L2.Location, L3.Location, L4.Location },
			{ L1.Location, L2.Location, M3.Location, M4.Location },
			{ M1.Location, M2.Location, M3.Location, M4.Location },
			{ M1.Location, M2.Location, M5.Location, M4.Location },
		},
	},
	ScrinRebelKiller = {
		Delay = DateTime.Minutes(1),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 32, Max = 32 }),
		FollowLeader = true,
		ProducerActors = { Infantry = { Portal2 }, Vehicles = { WarpSphere2 }, Aircraft = { GravityStabilizer1, GravityStabilizer2, GravityStabilizer3 } },
		Compositions = AdjustedScrinCompositions,
		AttackPaths = {
			{ M1.Location, M2.Location, R4.Location, M5.Location },
			{ R1.Location, R2.Location, R3.Location, R4.Location, R5.Location }
		},
	},
	ScrinGDIKiller = {
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 60, Max = 60 }),
		FollowLeader = true,
		ProducerActors = { Infantry = { Portal3 }, Vehicles = { WarpSphere4 }, Aircraft = { GravityStabilizer3 } },
		Compositions = AdjustedScrinCompositions,
		AttackPaths = {
			{ R7.Location, R6.Location, GDIBase.Location },
			{ R10.Location, R6.Location, GDIBase.Location },
		},
	},
	ScrinRebelsMain = {
		Delay = DateTime.Minutes(1),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 28, Max = 28 }, friendlyDifficulty),
		FollowLeader = true,
		ProducerActors = { Infantry = { RebelPortal1 }, Vehicles = { RebelWarpSphere1 }, Aircraft = { RebelGravityStabilizer1 } },
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin, friendlyDifficulty),
		AttackPaths = {
			{ M5.Location, M2.Location, M1.Location },
			{ M5.Location, R4.Location, M2.Location },
			{ R5.Location, R3.Location, R2.Location, R1.Location }
		},
	},
	GDIMain = {
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 64, Max = 64 }, friendlyDifficulty),
		FollowLeader = true,
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.GDI, friendlyDifficulty),
		AttackPaths = {
			{ R6.Location, R7.Location, ScrinBase2.Location },
			{ R6.Location, R10.Location, R9.Location, ScrinBase2.Location },
		},
	},
	ScrinAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(5)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		ProducerActors = nil,
		Compositions = AirCompositions.Scrin,
	},
	ScrinAirToAir = AirToAirSquad(
		{ "stmr", "enrv", "torm" },
		AdjustAirDelayForDifficulty(DateTime.Minutes(8)),
		function(a)
			a.Patrol({ A2APatrol1.Location, A2APatrol2.Location, A2APatrol3.Location, A2APatrol4.Location, A2APatrol5.Location, A2APatrol6.Location, A2APatrol7.Location, A2APatrol8.Location })
		end
	),
	ScrinRebelsAir = {
		Delay = DateTime.Minutes(5),
		AttackValuePerSecond = { Min = 14, Max = 14 },
		Compositions = {
			{ Aircraft = { "stmr", "stmr" } },
			{ Aircraft = { "enrv" } },
		}
	},
	GDIAir = {
		Delay = DateTime.Minutes(10),
		AttackValuePerSecond = {
			easy = { Min = 20, Max = 20 },
			normal = { Min = 14, Max = 14 },
			hard = { Min = 8, Max = 8 },
			vhard = { Min = 8, Max = 8 },
			brutal = { Min = 8, Max = 8 }
		},
		Compositions = {
			{ Aircraft = { "orca", "orca" } },
			{ Aircraft = { "a10" } },
			{ Aircraft = { "auro" } },
			{ Aircraft = { "orcb" } }
		},
	}
}

SetupPlayers = function()
	Nod = Player.GetPlayer("Nod")
	Scrin = Player.GetPlayer("Scrin")
	ScrinRebels = Player.GetPlayer("ScrinRebels")
	GDIHostile = Player.GetPlayer("GDIHostile")
	GDI = Player.GetPlayer("GDI")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { Nod }
	MissionEnemies = { Scrin }
end

WorldLoaded = function()
	SetupPlayers()

	NextExterminatorIndex = 1
	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Nod)
	AdjustPlayerStartingCashForDifficulty()
	InitScrin()
	InitScrinRebels()

	ObjectiveDestroyOverlordForces = Nod.AddObjective("Destroy Scrin forces loyal to the Overlord.")

	if not IsCoop then
		ObjectiveDefendRebels = Nod.AddObjective("Protect Scrin rebel forces.")
	end

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("The Overlord's tyranny will die today commander, and a new era will begin. Elsewhere, battles are still raging, but the decisive blow must be dealt here where his most elite forces are gathered. Show no mercy commander. Peace through power.", "Kane", HSLColor.FromHex("FF0000"))
		MediaCA.PlaySound(MissionDir .. "/kane_nomercy.aud", 2)

		Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(14)), function()
			Media.DisplayMessage("Foolish humans! Your armies will be crushed, the rebellion will fall, and you will die here!", "Scrin Overlord", HSLColor.FromHex("7700FF"))
			MediaCA.PlaySound(MissionDir .. "/overlordwarning.aud", 2)

			Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(60)), function()
				InitGDI()
			end)
		end)
	end)

	Trigger.OnEnteredProximityTrigger(GDIBase.CenterPosition, WDist.New(18 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveProximityTrigger(id)
			InitGDI()
		end
	end)

	RadarProviders = {}

	Utils.Do(MissionPlayers, function(p)
		table.insert(RadarProviders, Actor.Create("radar.dummy", true, { Owner = p }))
	end)

	Trigger.OnKilled(RebelMainNerveCenter, function(self, killer)
		Utils.Do(RadarProviders, function(p)
			p.Destroy()
		end)
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
		Scrin.Resources = Scrin.ResourceCapacity - 500
		ScrinRebels.Resources = ScrinRebels.ResourceCapacity - 500
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()

		if not PlayerHasBuildings(Scrin) and #Scrin.GetActorsByType("etpd") == 0 and not Victory then
			Victory = true
			Media.DisplayMessage("The Overlord's fate is sealed, and the Scrin are liberated. Now we must return to Earth and forge a new beginning for mankind. With purified Tiberium the possibilites are truly limitless, and those who embrace its light will share in its blessings. Those who do not, will be left in the darkness.", "Kane", HSLColor.FromHex("FF0000"))
			MediaCA.PlaySound(MissionDir .. "/kane_newbeginning.aud", 2)
			Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(19)), function()
				Nod.MarkCompletedObjective(ObjectiveDestroyOverlordForces)
				if ObjectiveDefendRebels ~= nil then
					Nod.MarkCompletedObjective(ObjectiveDefendRebels)
				end
			end)
		end

		if ObjectiveDefendRebels ~= nil and not PlayerHasBuildings(ScrinRebels) and not Victory then
			Nod.MarkFailedObjective(ObjectiveDefendRebels)
		end

		if MissionPlayersHaveNoRequiredUnits() and not Victory then
			Nod.MarkFailedObjective(ObjectiveDestroyOverlordForces)
		end
	end
end

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % DateTime.Seconds(30) == 0 then
		CalculatePlayerCharacteristics()
	end
end

InitScrin = function()
	RebuildExcludes.Scrin = { Types = { "rfgn" } }

	AutoRepairAndRebuildBuildings(Scrin, 15)
	SetupRefAndSilosCaptureCredits(Scrin)
	AutoReplaceHarvesters(Scrin)
	AutoRebuildConyards(Scrin)
	InitAiUpgrades(Scrin)
	InitAttackSquad(Squads.ScrinMain, Scrin)
	InitAirAttackSquad(Squads.ScrinAir, Scrin)
	InitAttackSquad(Squads.ScrinRebelKiller, Scrin, ScrinRebels)

	if IsHardOrAbove() then
		InitAirAttackSquad(Squads.ScrinAirToAir, Scrin, MissionPlayers, { "Aircraft" }, "ArmorType")
	end

	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnitExcludingExterminators, function(p) return p == Nod or p == ScrinRebels or p == GDI end)
	end)

	Trigger.AfterDelay(ExterminatorsStartTime[Difficulty], function()
		SendNextExterminator()
	end)

	Trigger.OnEnteredProximityTrigger(FirstExterminatorDetector.CenterPosition, WDist.New(5 * 1024), function(a, id)
		if a.Owner == Scrin and a.Type == "etpd" then
			Trigger.RemoveProximityTrigger(id)
			local camera = Actor.Create("camera", true, { Owner = Nod, Location = a.Location })
			Beacon.New(Nod, a.CenterPosition)
			Media.PlaySound("beacon.aud")
			Trigger.AfterDelay(DateTime.Seconds(6), function()
				camera.Destroy()
			end)
			local rebelDefenders = Utils.Where(Map.ActorsInCircle(Exterminator1Patrol1.CenterPosition, WDist.New(13 * 1024)), function(a)
				return a.Owner == ScrinRebels and not a.IsDead and a.HasProperty("Hunt")
			end)
			Utils.Do(rebelDefenders, function(a)
				a.AttackMove(Exterminator1Patrol1.Location)
				a.Hunt()
			end)
		end
	end)

	Actor.Create("loyalist.allegiance", true, { Owner = Scrin })

	Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = Scrin })
		Actor.Create("ai.superweapons.enabled", true, { Owner = Scrin })
	end)

	Utils.Do({ Exterminator1, Exterminator2, Exterminator3, Exterminator4, Exterminator5, Exterminator6, Exterminator7, Exterminator8 }, function(a)
		a.GrantCondition("difficulty-" .. Difficulty)
		Trigger.OnDamaged(a, function(self, attacker, damage)
			if IsMissionPlayer(attacker.Owner) and damage > 500 then
				AggroExterminator(self)
			end
		end)
	end)

	if Difficulty == "easy" then
		Exterminator2.Destroy()
		Exterminator4.Destroy()
	end
end

InitScrinRebels = function()
	if not ScrinRebelsActive then
		ScrinRebelsActive = true

		RebuildExcludes.ScrinRebels = { Actors = { FallenRebel1, FallenRebel2, FallenRebel3, FallenRebel4, FallenRebel5, FallenRebel6, FallenRebel7, FallenRebel8, FallenRebel9, FallenRebel10 } }

		AutoRepairAndRebuildBuildings(ScrinRebels, 15)
		AutoReplaceHarvesters(ScrinRebels)
		AutoRebuildConyards(Scrin, true)
		InitAiUpgrades(ScrinRebels)
		InitAirAttackSquad(Squads.ScrinRebelsAir, ScrinRebels, Scrin)
		InitAttackSquad(Squads.ScrinRebelsMain, ScrinRebels, Scrin)

		local scrinRebelGroundAttackers = ScrinRebels.GetGroundAttackers()

		Utils.Do(scrinRebelGroundAttackers, function(a)
			TargetSwapChance(a, 10)
			CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinRebelGroundHunterUnit, function(p) return p == Scrin end)
		end)
	end
end

InitGDI = function()
	if not GDIActive then
		GDIActive = true

		Beacon.New(Nod, GDIBase.CenterPosition)
		Media.PlaySound("beacon.aud")

		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.DisplayMessage("Our forces were successful in luring GDI here and they have established a base. The situation has been explained to them and they have agreed to a cease fire, but remain vigilant commander, our old enemy cannot be trusted.", "Kane", HSLColor.FromHex("FF0000"))
			MediaCA.PlaySound(MissionDir .. "/kane_gdibase.aud", 2)
		end)

		AutoRepairAndRebuildBuildings(GDI, 15)
		AutoReplaceHarvesters(GDI)
		AutoRebuildConyards(GDI, true)
		InitAiUpgrades(GDI)
		InitAttackSquad(Squads.ScrinGDIKiller, Scrin, GDI)
		InitAttackSquad(Squads.GDIMain, GDI, Scrin)
		InitAirAttackSquad(Squads.GDIAir, GDI, Scrin)

		local gdiUnits = GDIHostile.GetActors()
		Utils.Do(gdiUnits, function(a)
			if not a.IsDead and a.IsInWorld and a.Type ~= "player" then
				a.Owner = GDI
			end
		end)
	end
end

SendNextExterminator = function()
	if NextExterminatorIndex <= ExterminatorAttackCount[Difficulty] and not Victory then
		local exterminator

		if Exterminators[NextExterminatorIndex] ~= nil then
			exterminator = Exterminators[NextExterminatorIndex]
		else
			exterminator = { SpawnLocation = Utils.Random({ ExterminatorSpawnWest.Location, ExterminatorSpawnEast.Location }) }
		end

		local wormhole = Actor.Create("wormholelg", true, { Owner = Scrin, Location = exterminator.SpawnLocation })

		Trigger.AfterDelay(DateTime.Seconds(2), function()
			MediaCA.PlaySound("etpd-aggro.aud", 2)

			if NextExterminatorIndex == 1 then
				Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(2)), function()
					Media.DisplayMessage("Commander, the Overlord's most powerful weapons are being deployed. Use everything at your disposal to destroy them.", "Kane", HSLColor.FromHex("FF0000"))
					MediaCA.PlaySound(MissionDir .. "/kane_exterminators.aud", 2)
				end)
			else
				Notification("Exterminator Tripod detected.")
			end

			local reinforcements = Reinforcements.Reinforce(Scrin, { "etpd" }, { exterminator.SpawnLocation }, 10, function(a)
				if exterminator.Path ~= nil then
					local path = exterminator.Path
					a.Patrol(path)

					Trigger.OnIdle(a, function(self)
						self.Patrol(path)
					end)
				else
					AssaultPlayerBaseOrHunt(a)
				end

				a.GrantCondition("difficulty-" .. Difficulty)

				Trigger.AfterDelay(ExterminatorsInterval[Difficulty] * 5, function()
					AggroExterminator(a)
				end)

				Trigger.OnDamaged(a, function(self, attacker, damage)
					if IsMissionPlayer(attacker.Owner) and damage > 500 then
						AggroExterminator(self)
					end
				end)

				Trigger.AfterDelay(DateTime.Seconds(10), function()
					wormhole.Kill()
				end)
			end)

			NextExterminatorIndex = NextExterminatorIndex + 1

			Trigger.AfterDelay(ExterminatorsInterval[Difficulty], function()
				SendNextExterminator()
			end)
		end)
	end
end

AggroExterminator = function(a)
	if not a.IsDead then
		Trigger.ClearAll(a)
		a.Stop()
		Trigger.AfterDelay(1, function()
			if not a.IsDead then
				AssaultPlayerBaseOrHunt(a)
			end
		end)
	end
end

IsScrinRebelGroundHunterUnit = function(actor)
	return actor.Owner == ScrinRebels and IsGroundHunterUnit(actor) and actor.Type ~= "mast"
end

IsScrinGroundHunterUnitExcludingExterminators = function(actor)
	return IsScrinGroundHunterUnit(actor) and actor.Type ~= "etpd"
end
