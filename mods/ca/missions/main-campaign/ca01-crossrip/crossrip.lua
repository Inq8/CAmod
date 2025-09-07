-- Locations

SovietBorder = { CPos.New(9,32), CPos.New(10,32), CPos.New(11,32), CPos.New(12,32), CPos.New(13,32), CPos.New(14,32), CPos.New(15,32), CPos.New(16,32), CPos.New(17,32), CPos.New(18,32) }
SovietMainBaseEntrance = { CPos.New(39,3), CPos.New(39,4), CPos.New(39,5), CPos.New(39,6), CPos.New(39,7), CPos.New(39,8), CPos.New(39,9), CPos.New(39,10), CPos.New(39,11), CPos.New(39,12), CPos.New(39,13) }
SovietChronosphereLocation = CPos.New(60,25)
WormholeSpawns = { WormholeSpawn1.Location, WormholeSpawn2.Location, WormholeSpawn3.Location, WormholeSpawn4.Location, WormholeSpawn5.Location }

TreesToTransform = {
	{ Actor = TreeToTransform1, Location = TreeToTransform1.Location },
	{ Actor = TreeToTransform2, Location = TreeToTransform2.Location },
	{ Actor = TreeToTransform3, Location = TreeToTransform3.Location },
	{ Actor = TreeToTransform4, Location = TreeToTransform4.Location }
}

SovietAttackPaths = {
	{ AttackWaypoint1.Location, AttackWaypoint2.Location, AttackWaypoint3.Location, AttackWaypoint5.Location },
	{ AttackWaypoint1.Location, AttackWaypoint2.Location, AttackWaypoint3.Location, AttackWaypoint4.Location, AttackWaypoint5.Location }
}

AirAttackPaths = {
	{ DevastatorSpawn1.Location, DevastatorDestination1.Location },
	{ DevastatorSpawn2.Location, DevastatorDestination2.Location },
	{ DevastatorSpawn3.Location, DevastatorDestination3.Location }
}

ScrinAttackPaths = {
	{ AttackWaypoint4.Location, AttackWaypoint5.Location },
	{ AttackWaypoint5.Location },
}

WestPatrolPath = { WestPatrolWaypoint1.Location, WestPatrolWaypoint2.Location }

-- Other Variables

WestPatrolUnits = { WestPatrolUnit1, WestPatrolUnit2, WestPatrolUnit3, WestPatrolUnit4 }

ScrinInfantrySquads = {
	easy = { "s1", "s1", "s1", "s3" },
	normal = { "s1", "s1", "s1", "s2", "s3" },
	hard = { "s1", "s1", "s1", "s2", "s3", "s4" },
	vhard = { "s1", "s1", "s1", "s2", "s3", "s4" },
	brutal = { "s1", "s1", "s1", "s2", "s3", "s4", "s1" }
}

ScrinVehicleTypes = {
	easy = { "gunw", "seek", "intl", "gscr" },
	normal = { "gunw", "seek", "intl", "corr" },
	hard = { "gunw", "seek", "corr", "devo", "intl", "tpod" },
	vhard = { "gunw", "seek", "corr", "devo", "intl", "tpod" },
	brutal = { "shrw", "seek", "corr", "devo", "intl", "rtpd" }
}

ScrinVehiclesIntervalMultiplier = {
	easy = 4,
	normal = 4,
	hard = 3,
	vhard = 3,
	brutal = 3
}

EvacuationTime = {
	easy = DateTime.Minutes(4) + DateTime.Seconds(30),
	normal = DateTime.Minutes(5),
	hard = DateTime.Minutes(5) + DateTime.Seconds(30),
	vhard = DateTime.Minutes(5) + DateTime.Seconds(30),
	brutal = DateTime.Minutes(5) + DateTime.Seconds(30),
}

HaloDropEntryPaths = {
	{ HaloSpawn1.Location, HaloLanding1.Location },
	{ HaloSpawn2.Location, HaloLanding2.Location },
	{ HaloSpawn3.Location, HaloLanding3.Location }
}

HaloDropStart = AdjustDelayForDifficulty(DateTime.Minutes(12))
HaloDropAttackValue = AdjustAttackValuesForDifficulty({ Min = 12, Max = 24, RampDuration = DateTime.Minutes(10) })
NavalDropStart = AdjustDelayForDifficulty(DateTime.Minutes(18))
NavalDropAttackValue = AdjustAttackValuesForDifficulty({ Min = 6, Max = 6 })

-- Squads

HardAndAboveMainCompositions = {
	{
		Infantry = { "e3", "e1", "e1", "e1", "e1", "e1", "e2", "e3", "e4" },
		Vehicles = { "3tnk", "btr.ai", "3tnk" },
		MaxTime = DateTime.Minutes(6)
	},
	{
		Infantry = { "e3", "e1", "e1", "e3", "shok", "e1", { "shok", "e8" }, "e1", "e2", "e3", "e4" },
		Vehicles = { { "3tnk", "3tnk.atomic" }, "4tnk", "btr.ai", { "katy", "v2rl" }, "ttra" },
		MinTime = DateTime.Minutes(6),
		MaxTime = DateTime.Minutes(16)
	},
}

if Difficulty == "brutal" then
	HardAndAboveMainCompositions[2].MaxTime = DateTime.Minutes(16)

	table.insert(HardAndAboveMainCompositions, {
		Infantry = { "e3", "e1", "e1", "e3", "shok", "e1", { "ttrp", "deso" }, "e1", "e2", "e3", "e4", "e1", "e1", "e1", "e1", "e3", "e1", { "ttrp", "deso" } },
		Vehicles = { { "3tnk", "3tnk.atomic" }, "4tnk", "btr.ai", "v2rl", "v2rl", "grad", "ttra" },
		MinTime = DateTime.Minutes(16)
	})
end

Squads = {
	Main = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(3)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 35, Max = 45 }),
		FollowLeader = true,
		ProducerActors = { Infantry = { SovietBarracks }, Vehicles = { SovietWarFactory } },
		Compositions = {
			easy = {
				{
					Infantry = { "e3", "e1", "e1", "e1", "e2", "e4" },
					Vehicles = { "3tnk", "btr" },
					MaxTime = DateTime.Minutes(12)
				},
				{
					Infantry = { "e3", "e1", "e1", "shok", "shok", "e1", "e2", "e3", "e4" },
					Vehicles = { "4tnk", "btr.ai" },
					MinTime = DateTime.Minutes(12)
				}
			},
			normal = {
				{
					Infantry = { "e3", "e1", "e1", "e1", "e1", "e2", "e4" },
					Vehicles = { "3tnk", "btr.ai" },
					MaxTime = DateTime.Minutes(9)
				},
				{
					Infantry = { "e3", "e1", "e1", "shok", "e1", "e2", "e3", "e4" },
					Vehicles = { { "3tnk", "3tnk.atomic" }, "4tnk", "katy" },
					MinTime = DateTime.Minutes(9)
				}
			},
			hard = HardAndAboveMainCompositions,
			vhard = HardAndAboveMainCompositions,
			brutal = HardAndAboveMainCompositions
		},
		AttackPaths = SovietAttackPaths,
	},
	Western = {
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 10, Max = 20 }),
		FollowLeader = true,
		ProducerActors = { Infantry = { SovietWestBarracks } },
		Compositions = AdjustCompositionsForDifficulty({
			{ Infantry = { "e3", "e1", "e2", "e1", "e4", "e1", "shok", "e1" } }
		}),
		AttackPaths = {
			{ AttackWaypoint4.Location, AttackWaypoint5.Location }
		}
	},
	Migs = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(11)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 8, Max = 12 }),
		Compositions = {
			easy = {
				{ Aircraft = { "mig" } }
			},
			normal = {
				{ Aircraft = { "mig", "mig" } }
			},
			hard = {
				{ Aircraft = { "mig", "mig", "mig" } }
			},
			vhard = {
				{ Aircraft = { "mig", "mig", "mig" } }
			},
			brutal = {
				{ Aircraft = { "mig", "mig", "mig", "mig" } }
			}
		}
	}
}

-- Setup and Tick

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	Scrin = Player.GetPlayer("Scrin")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { Greece }
	MissionEnemies = { USSR, Scrin }
	TimerTicks = 0

	Camera.Position = PlayerMcv.CenterPosition

	InitObjectives(Greece)
	AdjustPlayerStartingCashForDifficulty()
	RemoveActorsBasedOnDifficultyTags()
	InitUSSR()

	Trigger.AfterDelay(1, function()
		ObjectiveEstablishBase = Greece.AddObjective("Establish a base.")
		UserInterface.SetMissionText("Establish a base.", HSLColor.Yellow)
	end)

	Trigger.OnKilled(Church, function(self, killer)
		Actor.Create("moneycrate", true, { Owner = Greece, Location = Church.Location })
	end)

	Trigger.OnEnteredProximityTrigger(SovietChronosphere.CenterPosition, WDist.New(7 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveProximityTrigger(id)
			ChronosphereDiscovered()
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		BaseFlare = Actor.Create("flare", true, { Owner = Greece, Location = DeploySuggestion.Location })
		Media.PlaySpeechNotification(Greece, "SignalFlare")
		Notification("Signal flare detected.")
		Beacon.New(Greece, DeploySuggestion.CenterPosition)
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Tip("Press [" .. UtilsCA.Hotkey("ToLastEvent") .. "] to center the screen on the most recent notification beacon.")
			Trigger.AfterDelay(DateTime.Seconds(2), function()
				Tip("Press [" .. UtilsCA.Hotkey("OpenTeamChat") .. "] to open the chat panel to read previous notification messages.")
			end)
		end)

		Trigger.OnEnteredProximityTrigger(DeploySuggestion.CenterPosition, WDist.New(6 * 1024), function(a, id)
			if IsMissionPlayer(a.Owner) and a.Type ~= "waypoint" and a.Type ~= "flare" and not IsDeploySuggestionReached then
				IsDeploySuggestionReached = true
				Trigger.RemoveProximityTrigger(id)
				BaseFlare.Destroy()
			end
		end)
	end)

	SetupReveals({ EntranceReveal1, EntranceReveal2 })
	AfterWorldLoaded()
end

Tick = function()
	if not IsBaseEstablished and HasConyard(Greece) then
		IsBaseEstablished = true
		if ObjectiveInvestigateArea == nil then
			ObjectiveInvestigateArea = Greece.AddObjective("Investigate the area.")
			UserInterface.SetMissionText("")
		end
		Greece.MarkCompletedObjective(ObjectiveEstablishBase)

		if IsNormalOrBelow() then
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				Tip("Build a barracks for access to static defenses which should allow you to hold off any early attacks. Use Pillboxes against infantry and Turrets against vehicles.")
			end)

			Trigger.AfterDelay(DateTime.Minutes(2), function()
				Tip("Mechanics can repair your vehicles in the field. Putting a Mechanic inside an IFV turns it into a repair vehicle. Build a Supply Depot for access to Mechanics.")
			end)

			Trigger.AfterDelay(DateTime.Minutes(3), function()
				Tip("Prism Tanks are excellent long range support units that are effective against infantry, defenses and light vehicles. Build a Radar Dome for access to Prism Tanks.")
			end)
		end
	end

	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		USSR.Resources = USSR.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
				UserInterface.SetMissionText("Evacuation begins in " .. UtilsCA.FormatTimeForGameSpeed(TimerTicks), HSLColor.Yellow)
			else
				TimerTicks = 0
				UserInterface.SetMissionText("Evacuation underway.", HSLColor.Yellow)
				Greece.MarkCompletedObjective(ObjectiveDefendUntilEvacuation)
			end
		end

		if MissionPlayersHaveNoRequiredUnits() then
			if ObjectiveEstablishBase ~= nil and not Greece.IsObjectiveCompleted(ObjectiveEstablishBase) then
				Greece.MarkFailedObjective(ObjectiveEstablishBase)
			end
			if ObjectiveInvestigateArea ~= nil and not Greece.IsObjectiveCompleted(ObjectiveInvestigateArea) then
				Greece.MarkFailedObjective(ObjectiveInvestigateArea)
			end
			if ObjectiveCaptureOrDestroyChronosphere ~= nil and not Greece.IsObjectiveCompleted(ObjectiveCaptureOrDestroyChronosphere) then
				Greece.MarkFailedObjective(ObjectiveCaptureOrDestroyChronosphere)
			end
			if ObjectiveDefendUntilEvacuation ~= nil and not Greece.IsObjectiveCompleted(ObjectiveDefendUntilEvacuation) then
				Greece.MarkFailedObjective(ObjectiveDefendUntilEvacuation)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()
	end
end

-- Functions

InitUSSR = function()
	if Difficulty == "easy" then
		RebuildExcludes.USSR = { Types = { "tsla", "ftur" } }
	elseif Difficulty == "normal" then
		RebuildExcludes.USSR = { Types = { "tsla" } }
	end

	AutoRepairAndRebuildBuildings(USSR, 10)
	SetupRefAndSilosCaptureCredits(USSR)
	AutoReplaceHarvesters(USSR)
	AutoRebuildConyards(USSR)
	InitAttackSquad(Squads.Main, USSR)
	InitAirAttackSquad(Squads.Migs, USSR)

	-- Set western patrol
	Utils.Do(WestPatrolUnits, function(unit)
		if not unit.IsDead then
			unit.Patrol(WestPatrolPath, true, 100)
		end
	end)

	-- Halo drops
	Trigger.AfterDelay(HaloDropStart, DoHaloDrop)

	-- Beach landings
	if Difficulty ~= "easy" then
		Trigger.AfterDelay(NavalDropStart, DoSovietNavalDrop)
	end

	-- On player crossing Soviet border start making infantry at western barracks
	Trigger.OnEnteredFootprint(SovietBorder, function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveFootprintTrigger(id)
			if not IsWesternSquadActive then
				IsWesternSquadActive = true
				InitAttackSquad(Squads.Western, USSR)
			end
		end
	end)

	-- On destroying or capturing Chronosphere
	Trigger.OnKilled(SovietChronosphere, function(self, killer)
		Actor.Create("pdox.crossrip", true, { Owner = USSR, Location = SovietChronosphereLocation})
		ChronosphereDiscovered()
		InterdimensionalCrossrip()
	end)

	Trigger.OnCapture(SovietChronosphere, function(self, captor, oldOwner, newOwner)
		SovietChronosphere.Kill()
	end)
end

ScrinInvasion = function()
	local wormholes = Scrin.GetActorsByType("wormhole")

	Utils.Do(wormholes, function(wormhole)
		local units = Reinforcements.Reinforce(Scrin, ScrinInfantrySquads[Difficulty], { wormhole.Location }, 1)
		Utils.Do(units, function(unit)
			unit.Scatter()
			Trigger.AfterDelay(5, function()
				AssaultPlayerBaseOrHunt(unit, MissionPlayers, GetScrinAssaultWaypoints())
			end)
		end)
	end)

	Trigger.AfterDelay(GetInvasionInterval(), ScrinInvasion)
	if not IsInvasionStarted then
		Trigger.AfterDelay(GetInvasionInterval() * 2, CreateScrinVehicles)
		IsInvasionStarted = true
	end
end

CreateScrinVehicles = function()
	local wormholes = Scrin.GetActorsByType("wormhole")

	Utils.Do(wormholes, function(wormhole)
		local units = Reinforcements.Reinforce(Scrin, { Utils.Random(ScrinVehicleTypes[Difficulty]) }, { wormhole.Location }, 1)
		Utils.Do(units, function(unit)
			unit.Scatter()
			Trigger.AfterDelay(5, function()
				AssaultPlayerBaseOrHunt(unit, MissionPlayers, GetScrinAssaultWaypoints())
			end)
		end)
	end)

	Trigger.AfterDelay(GetInvasionInterval() * ScrinVehiclesIntervalMultiplier[Difficulty], CreateScrinVehicles)
end

GetScrinAssaultWaypoints = function()
	local assaultWaypoints = { AttackWaypoint5.Location }

	if Utils.RandomInteger(0,2) == 1 then
		assaultWaypoints = { AttackWaypoint4.Location, AttackWaypoint5.Location }
	end

	return assaultWaypoints
end

ChronosphereDiscovered = function()
	if not IsChronosphereDiscovered then
		IsBaseEstablished = true
		IsChronosphereDiscovered = true
		Notification("Commander, the Soviets have been attempting to reverse engineer stolen Chronosphere technology! Use whatever means necessary to cease their experiments.")
		MediaCA.PlaySound("r_chronodisc.aud", 2)

		local autoCamera = Actor.Create("smallcamera", true, { Owner = Greece, Location = SovietChronosphereLocation })
		Trigger.AfterDelay(DateTime.Seconds(5), autoCamera.Destroy)

		ObjectiveCaptureOrDestroyChronosphere = Greece.AddObjective("Capture or destroy the Soviet Chronosphere.")
		UserInterface.SetMissionText("Capture or destroy the Soviet Chronosphere.", HSLColor.Yellow)

		if ObjectiveEstablishBase ~= nil and not Greece.IsObjectiveCompleted(ObjectiveEstablishBase) then
			Greece.MarkCompletedObjective(ObjectiveEstablishBase)
		end
		if ObjectiveInvestigateArea ~= nil and not Greece.IsObjectiveCompleted(ObjectiveInvestigateArea) then
			Greece.MarkCompletedObjective(ObjectiveInvestigateArea)
		end
	end
end

InterdimensionalCrossrip = function()
	if IsCrossRipped then
		return
	end

	IsCrossRipped = true

	if not SovietWarFactory.IsDead then
		SovietWarFactory.Kill()
	end

	local sovietGroundAttackers = USSR.GetGroundAttackers()
	Utils.Do(sovietGroundAttackers, function(a)
		Trigger.AfterDelay(Utils.RandomInteger(5,250), function()
			if not a.IsDead then
				a.Kill("ExplosionDeath")
			end
		end)
	end)

	Lighting.Ambient = 0.8
	Lighting.Red = 0.8
	Lighting.Blue = 0.9
	Lighting.Green = 1.2

	Trigger.AfterDelay(1, SpawnWormhole)
	Trigger.AfterDelay(2, SpawnTibTree)

	ObjectiveDefendUntilEvacuation = Greece.AddObjective("Defend your base until evacuation is prepared.")

	if ObjectiveCaptureOrDestroyChronosphere ~= nil then
		Greece.MarkCompletedObjective(ObjectiveCaptureOrDestroyChronosphere)
	end

	local unitLostSilencer = Actor.Create("unitlostsilencer", true, { Owner = Greece })

	Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(2)), function()
		ScrinInvasion()
		Notification("Unidentified hostile forces detected. Fall back to your base, and prepare for evacuation.")
		MediaCA.PlaySound("r_evac.aud", 2)
		TimerTicks = EvacuationTime[Difficulty]
		Trigger.AfterDelay(DateTime.Seconds(7), function()
			unitLostSilencer.Destroy()
		end)
	end)

	Actor.Create("flare", true, { Owner = Greece, Location = Evac1.Location })
	Actor.Create("flare", true, { Owner = Greece, Location = Evac2.Location })
	Actor.Create("flare", true, { Owner = Greece, Location = Evac3.Location })

	Trigger.AfterDelay(EvacuationTime[Difficulty] - DateTime.Seconds(12), function()
		Reinforcements.ReinforceWithTransport(Greece, "nhaw.paradrop", nil, { CPos.New(Evac1.Location.X - 10, Evac1.Location.Y + 15), CPos.New(Evac1.Location.X, Evac1.Location.Y - 1) })
		Reinforcements.ReinforceWithTransport(Greece, "nhaw.paradrop", nil, { CPos.New(Evac2.Location.X - 10, Evac2.Location.Y + 15), CPos.New(Evac2.Location.X, Evac2.Location.Y - 1) })
		Reinforcements.ReinforceWithTransport(Greece, "nhaw.paradrop", nil, { CPos.New(Evac3.Location.X - 10, Evac3.Location.Y + 15), CPos.New(Evac3.Location.X, Evac3.Location.Y - 1) })
	end)

	Trigger.AfterDelay(EvacuationTime[Difficulty] - DateTime.Seconds(40), SendDevastators)
end

SpawnTibTree = function()
	local tibTree = TreesToTransform[#TreesToTransform]
	if not tibTree.Actor.IsDead then
		tibTree.Actor.Destroy()
	end
	Actor.Create("split2", true, { Owner = Neutral, Location = tibTree.Location})
	table.remove(TreesToTransform, #TreesToTransform)

	if #TreesToTransform > 0 then
		Trigger.AfterDelay(1, SpawnTibTree)
	end
end

SpawnWormhole = function()
	local loc = WormholeSpawns[#WormholeSpawns]
	Actor.Create("wormhole", true, { Owner = Scrin, Location = loc})
	table.remove(WormholeSpawns, #WormholeSpawns)

	if #WormholeSpawns > 0 then
		Trigger.AfterDelay(1, SpawnWormhole)
	end
end

DoHaloDrop = function()
	if SovietWarFactory.IsDead or IsCrossRipped then
		return
	end

	local entryPath = Utils.Random(HaloDropEntryPaths)

	local haloDropUnits = { "e1", "e1", "e1", "e2", "e3", "e4" }
	if IsHardOrAbove() and DateTime.GameTime > DateTime.Minutes(15) then
		haloDropUnits = { "e1", "e1", "e1", "e1", "e2", "e2", "e3", "e3", "e4", "shok" }
	end

	DoHelicopterDrop(USSR, entryPath, "halo.paradrop", haloDropUnits, AssaultPlayerBaseOrHunt, function(t)
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			if not t.IsDead then
				t.Move(entryPath[1])

			end
		end)
		Trigger.AfterDelay(DateTime.Seconds(12), function()
			if not t.IsDead then
				t.Destroy()
			end
		end)
	end)

	local delayUntilNext = CalculateInterval(GetTotalCostOfUnits(haloDropUnits), HaloDropAttackValue, HaloDropStart)
	Trigger.AfterDelay(delayUntilNext, DoHaloDrop)
end

DoSovietNavalDrop = function()
	if SovietWarFactory.IsDead or IsCrossRipped then
		return
	end

	local navalDropPath = { CPos.New(NavalDrop.Location.X - 3, NavalDrop.Location.Y - 1), NavalDrop.Location }
	local navalDropExitPath = { navalDropPath[2], navalDropPath[1] }
	local navalDropUnits = { "3tnk", "v2rl" }

	if IsHardOrAbove() then
		navalDropUnits = { "3tnk", "v2rl", "3tnk" }
	end

	DoNavalTransportDrop(USSR, navalDropPath, navalDropExitPath, "lst", navalDropUnits, AssaultPlayerBaseOrHunt)

	local delayUntilNext = CalculateInterval(GetTotalCostOfUnits(navalDropUnits), NavalDropAttackValue, NavalDropStart)
	Trigger.AfterDelay(delayUntilNext, DoSovietNavalDrop)
end

SendDevastators = function()
	local deva1 = Reinforcements.Reinforce(Scrin, { "deva" }, { DevastatorSpawn1.Location, DevastatorDestination1.Location })
	local deva2 = Reinforcements.Reinforce(Scrin, { "deva" }, { DevastatorSpawn2.Location, DevastatorDestination2.Location })
	local deva3 = Reinforcements.Reinforce(Scrin, { "deva" }, { DevastatorSpawn3.Location, DevastatorDestination3.Location })
	local units = { deva1[1], deva2[1], deva3[1] }

	Utils.Do(units, function(unit)
		Trigger.AfterDelay(5, function()
			AssaultPlayerBaseOrHunt(unit)
		end)
	end)
end

GetInvasionInterval = function()
	local armyValue = GetMissionPlayersArmyValue()

	if Difficulty == "easy" then
		if armyValue >= 10000 then
			return DateTime.Seconds(22)
		else
			return DateTime.Seconds(24)
		end
	else
		if armyValue >= 48000 then
			return DateTime.Seconds(13)
		elseif armyValue >= 38000 then
			return DateTime.Seconds(15)
		elseif armyValue >= 28000 then
			return DateTime.Seconds(17)
		elseif armyValue >= 18000 then
			return DateTime.Seconds(19)
		elseif armyValue >= 10000 then
			return DateTime.Seconds(22)
		else
			return DateTime.Seconds(24)
		end
	end
end
