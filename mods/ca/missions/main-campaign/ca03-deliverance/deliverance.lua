MissionDir = "ca|missions/main-campaign/ca03-deliverance"

-- Locations

SovietMainAttackPaths = {
	{ EastAssembly.Location, NorthAttackRally.Location },
	{ EastAssembly.Location, NorthEastAttackRally.Location },
	{ EastAssembly.Location, EastAttackRally.Location }
}

SovietNorthernAttackPaths = {
	{ NorthAssembly.Location, NorthAttackRally.Location },
	{ NorthAssembly.Location, NorthEastAttackRally.Location },
	{ NorthAssembly.Location, EastAttackRally.Location }
}

SovietNavalAttackPaths = { { NavalSouthAssembly.Location, NavalSouthRally.Location, NavalForwardRally.Location } }

SovietNavalFallbackAttackPaths = { { NavalEastAssembly.Location, NavalSouthEastAssembly.Location, NavalSouthAssembly.Location, NavalSouthRally.Location, NavalForwardRally.Location  } }

SovietCenterPatrolPath = { CentralPatrol1.Location, CentralPatrol2.Location, CentralPatrol3.Location, CentralPatrol4.Location, CentralPatrol3.Location, CentralPatrol2.Location }

SovietShorePatrolPath = { ShorePatrol1.Location, ShorePatrol2.Location }

SovietHindPatrolPath = { NavalEastAssembly.Location, NavalSouthEastAssembly.Location, NavalSouthAssembly.Location, NavalSouthRally.Location, NavalForwardRally.Location, EastAssembly.Location, CentralPatrol3.Location }

HaloDropPaths = {
	{ HaloSpawn1.Location, HaloDrop1Mid.Location, HaloDrop1Landing.Location },
	{ HaloSpawn1.Location, HaloDrop2Landing.Location },
	{ HaloSpawn1.Location, HaloDrop1Mid.Location, HaloDrop3Landing.Location },
	{ HaloSpawn1.Location, HaloDrop1Mid.Location, HaloDrop4Landing.Location },
	{ HaloSpawn2.Location, HaloDrop5Landing.Location },
	{ HaloSpawn2.Location, HaloDrop6Landing.Location },
	{ HaloSpawn2.Location, HaloDrop6Landing.Location, HaloDrop7Landing.Location }
}

LateHaloDropPaths = {
	{ LateHaloDrop1Spawn.Location, LateHaloDrop1Landing.Location },
	{ LateHaloDrop2Spawn.Location, LateHaloDrop2Landing.Location },
	{ LateHaloDrop3Spawn.Location, LateHaloDrop3Landing.Location }
}

NavalDropPaths = {
	{ RaidSpawn.Location, RaidLanding1.Location },
	{ RaidSpawn.Location, RaidLanding2.Location },
	{ RaidSpawn.Location, RaidLanding3.Location }
}

-- Other Variables

HaloDropStart = AdjustDelayForDifficulty(DateTime.Minutes(15))
HaloDropAttackValue = AdjustAttackValuesForDifficulty({ Min = 12, Max = 24, RampDuration = DateTime.Minutes(5) })
NavalDropStart = AdjustDelayForDifficulty(DateTime.Minutes(17))
NavalDropAttackValue = AdjustAttackValuesForDifficulty({ Min = 6, Max = 14, RampDuration = DateTime.Minutes(5) })

HoldOutTime = {
	easy = DateTime.Minutes(7),
	normal = DateTime.Minutes(8),
	hard = DateTime.Minutes(9),
	vhard = DateTime.Minutes(10),
	brutal = DateTime.Minutes(11)
}

-- Squads

Squads = {
	Main = {
		Delay = AdjustDelayForDifficulty(DateTime.Seconds(80)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 26, Max = 32, RampDuration = DateTime.Minutes(14) }),
		FollowLeader = true,
		ProducerActors = { Infantry = { SovietMainBarracks1, SovietMainBarracks2 }, Vehicles = { SovietMainFactory1, SovietMainFactory2 } },
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Soviet),
		AttackPaths = SovietMainAttackPaths,
	},
	Northern = {
		Delay = AdjustDelayForDifficulty(DateTime.Seconds(60)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 24, RampDuration = DateTime.Minutes(14) }),
		FollowLeader = true,
		ProducerActors = { Infantry = { SovietNorthBarracks1, SovietNorthBarracks2 }, Vehicles = { SovietNorthFactory } },
		Compositions = {
			easy = {
				{
					Infantry = { "e3", "e1", "e1", "e1", "e2" },
					Vehicles = { "btr" }
				}
			},
			normal = {
				{
					Infantry = { "e3", "e1", "e1", "e1", "e1", "e2", "e4" },
					Vehicles = { "3tnk", "btr.ai" }
				}
			},
			hard = {
				{
					Infantry = { "e3", "e1", "e1", "e1", "e1", "e1", "e2", "e3", "e4" },
					Vehicles = { "3tnk", "btr.ai", "3tnk" }
				}
			},
			vhard = AdjustCompositionsForDifficulty(UnitCompositions.Soviet),
			brutal = AdjustCompositionsForDifficulty(UnitCompositions.Soviet)
		},
		AttackPaths = SovietNorthernAttackPaths
	},
	Migs = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
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
				{ Aircraft = { "mig", "mig", "mig" } }
			}
		},
	},
	Naval = {
		ActiveCondition = function()
			return MissionPlayersHaveNavalPresence()
		end,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 16, Max = 16 }),
		ProducerActors = { Ships = { SovietSouthSubPen1, SovietSouthSubPen2 } },
		Compositions = {
			easy = {
				{ Ships = { "ss", "seas" } }
			},
			normal = {
				{ Ships = { "ss", "seas" } }
			},
			hard = {
				{ Ships = { "ss", "ss", "seas" } }
			},
			vhard = {
				{ Ships = { "ss", "ss", "seas" } }
			},
			brutal = {
				{ Ships = { "ss", "ss", "seas" } }
			}
		},
		AttackPaths = SovietNavalAttackPaths
	}
}

-- Setup and Tick

SetupPlayers = function()
	Greece = Player.GetPlayer("Greece")
	GDI = Player.GetPlayer("GDI")
	USSR = Player.GetPlayer("USSR")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { Greece }
	MissionEnemies = { USSR }
end

WorldLoaded = function()
	SetupPlayers()

	TimerTicks = 0
	GDICommanderAlive = true
	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Greece)
	AdjustPlayerStartingCashForDifficulty()
	RemoveActorsBasedOnDifficultyTags()
	InitUSSR()

	if IsVeryHardOrBelow() then
		MissileSilo.Destroy()

		if IsNormalOrBelow() then
			SovietMammoth1.Destroy()
			SovietV22.Destroy()

			SovietV23.Destroy()
			SovietV24.Destroy()
			SovietMammoth3.Destroy()

			HardOnlySub1.Destroy()
			HardOnlySub2.Destroy()
			HardOnlySub3.Destroy()
			HardOnlySub4.Destroy()
			HardOnlySub5.Destroy()

			HardOnlyTeslaCoil1.Destroy()
			HardOnlyTeslaCoil2.Destroy()
			HardOnlyTeslaCoil3.Destroy()

			HardOnlyKatyusha1.Destroy()
			HardOnlyKatyusha2.Destroy()

			Trigger.AfterDelay(DateTime.Seconds(3), function()
				Tip("If you put a Mechanic inside an IFV it becomes a repair vehicle.")
			end)

			if Difficulty == "easy" then
				SovietMammoth2.Destroy()
				SovietV21.Destroy()
			end
		end
	end

	ObjectiveFindBase = Greece.AddObjective("Find besieged GDI base.")
	UserInterface.SetMissionText("Find besieged GDI base.", HSLColor.Yellow)

	-- On finding the GDI base, transfer ownership to player
	Trigger.OnEnteredProximityTrigger(GDIBaseTopRight.CenterPosition, WDist.New(16 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveProximityTrigger(id)
			GDIBaseFound()
		end
	end)

	-- On proximity to prison, reveal it and update objectives.
	Trigger.OnEnteredProximityTrigger(SovietPrison.CenterPosition, WDist.New(10 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveProximityTrigger(id)
			RevealPrison()
		end
	end)

	Trigger.OnDamaged(SovietPrison, function(self, attacker, damage)
		RevealPrison()
	end)

	Trigger.OnCapture(SovietPrison, function(self, captor, oldOwner, newOwner)
		if IsMissionPlayer(newOwner) then
			local commander = Reinforcements.Reinforce(GDI, { "gnrl" }, { GDICommanderSpawn.Location, GDICommanderRally.Location })[1]

			if ObjectiveLocateCommander ~= nil and not Greece.IsObjectiveCompleted(ObjectiveLocateCommander) then
				Greece.MarkCompletedObjective(ObjectiveLocateCommander)
			end

			Trigger.OnKilled(commander, function(self, killer)
				GDICommanderAlive = false
			end)

			Trigger.AfterDelay(DateTime.Seconds(3), function()
				if GDICommanderAlive then
					Notification("The GDI commander has been freed.")
					MediaCA.PlaySound(MissionDir .. "/r_gdicmdrfreed.aud", 2)
				end

				Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(3)), function()
					MediaCA.PlaySound(MissionDir .. "/r_gditraninbound.aud", 2)
					Reinforcements.ReinforceWithTransport(GDI, "tran.evac", nil, { GDIRescueSpawn.Location, GDIRescueRally.Location }, nil, function(transport, cargo)

						Trigger.AfterDelay(DateTime.Seconds(1), function()
							if not commander.IsDead then
								commander.EnterTransport(transport)
								Trigger.OnIdle(commander, function(c)
									if not c.IsDead then
										c.EnterTransport(transport)
									end
								end)
							end
						end)

						Trigger.OnPassengerEntered(transport, function(t, passenger)
							PlaySpeechNotificationToMissionPlayers("TargetRescued")
							t.Move(GDIReinforcementsEntry.Location)
							t.Destroy()
							Trigger.AfterDelay(DateTime.Seconds(7), function()
								Greece.MarkCompletedObjective(ObjectiveCapturePrison)

								if not IsHoldOutComplete then
									Notification("Continue holding your position, we need to keep the Soviets busy so they don't pursue the GDI commander.")
								end
							end)
						end)
					end)
				end)
			end)
		end
	end)

	Trigger.OnKilled(SovietPrison, function(self, killer)
		if not IsMissionPlayer(self.Owner) then
			GDICommanderAlive = false
		end
	end)

	SetupReveals({ EntranceReveal1, EntranceReveal2, EntranceReveal3, EntranceReveal4 })
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
		USSR.Resources = USSR.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
			UpdateReinforcementCountdown()
		end

		if MissionPlayersHaveNoRequiredUnits() or not GDICommanderAlive then
			if ObjectiveFindBase ~= nil and not Greece.IsObjectiveCompleted(ObjectiveFindBase) then
				Greece.MarkFailedObjective(ObjectiveFindBase)
			end
			if ObjectiveHoldOut ~= nil and not Greece.IsObjectiveCompleted(ObjectiveHoldOut) then
				Greece.MarkFailedObjective(ObjectiveHoldOut)
			end
			if ObjectiveLocateCommander ~= nil and not Greece.IsObjectiveCompleted(ObjectiveLocateCommander) then
				Greece.MarkFailedObjective(ObjectiveLocateCommander)
			end
			if ObjectiveCapturePrison ~= nil and not Greece.IsObjectiveCompleted(ObjectiveCapturePrison) then
				Greece.MarkFailedObjective(ObjectiveCapturePrison)
			end
		end

		NavalReinforcements()
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

-- Functions

GDIBaseFound = function()
	if not IsGDIBaseFound then
		IsGDIBaseFound = true
		MediaCA.PlaySound(MissionDir .. "/r_gdibasediscovered.aud", 2)

		Greece.PlayLowPowerNotification = false

		TransferGDIUnits()

		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Greece.PlayLowPowerNotification = true
		end)

		InitUSSRAttacks()
		TimerTicks = HoldOutTime[Difficulty]

		Trigger.AfterDelay(DateTime.Seconds(1), function()
			ObjectiveHoldOut = Greece.AddObjective("Hold out until reinforcements arrive.")
			UpdateReinforcementCountdown()
			Greece.MarkCompletedObjective(ObjectiveFindBase)
		end)

		Trigger.AfterDelay(HoldOutTime[Difficulty] - DateTime.Seconds(20), function()
			McvFlare = Actor.Create("flare", true, { Owner = Greece, Location = McvRally.Location })
			PlaySpeechNotificationToMissionPlayers("SignalFlare")
			Notification("Signal flare detected, reinforcements inbound. Press [" .. UtilsCA.Hotkey("ToLastEvent") .. "] to view location.")
			Beacon.New(Greece, McvRally.CenterPosition)
			Trigger.AfterDelay(DateTime.Seconds(20), function()
				McvFlare.Destroy()
			end)
		end)

		Trigger.AfterDelay(HoldOutTime[Difficulty], function()
			HoldOutComplete()
		end)

		if IsNormalOrBelow() then
			Trigger.AfterDelay(DateTime.Seconds(792), function()
				PlaySpeechNotificationToMissionPlayers("ReinforcementsArrived")
				Beacon.New(Greece, GDIReinforcementsEntry.CenterPosition)
				local gdiReinforcements = { "mtnk", "htnk" }
				if Difficulty == "easy" then
					gdiReinforcements = { "htnk" , "htnk" }
				end
				Reinforcements.Reinforce(Greece, gdiReinforcements, { GDIReinforcementsEntry.Location, GDIReinforcementsRally.Location }, 75)
			end)
		end
	end
end

-- overridden in co-op version
TransferGDIUnits = function()
	local gdiForces = GDI.GetActors()
	Utils.Do(gdiForces, function(a)
		if a.Type ~= "player" then
			a.Owner = Greece
		end
	end)

	Trigger.AfterDelay(1, function()
		Actor.Create("QueueUpdaterDummy", true, { Owner = Greece })
	end)
end

UpdateReinforcementCountdown = function()
	if not IsHoldOutComplete and (not IsPrisonRevealed or (ObjectiveCapturePrison ~= nil and Greece.IsObjectiveCompleted(ObjectiveCapturePrison))) then
		UserInterface.SetMissionText("Hold out until reinforcements arrive: " .. UtilsCA.FormatTimeForGameSpeed(TimerTicks), HSLColor.Yellow)
	end
end

HoldOutComplete = function()
	if not IsHoldOutComplete then
		IsHoldOutComplete = true

		if ObjectiveLocateCommander == nil then
			ObjectiveLocateCommander = Greece.AddObjective("Locate the GDI commander.")
			UserInterface.SetMissionText("Locate the GDI commander.", HSLColor.Yellow)
		end

		Greece.MarkCompletedObjective(ObjectiveHoldOut)

		if ObjectiveCapturePrison == nil or not Greece.IsObjectiveCompleted(ObjectiveCapturePrison) then
			Trigger.AfterDelay(DateTime.Seconds(1), function()
				PlaySpeechNotificationToMissionPlayers("ReinforcementsArrived")
				Notification("Reinforcements have arrived.")
				DoMcvArrival()
				Beacon.New(Greece, McvRally.CenterPosition)
			end)
		end

		if Difficulty == "brutal" then
			Trigger.AfterDelay(DateTime.Minutes(10), function()
				Actor.Create("ai.superweapons.enabled", true, { Owner = USSR })
			end)
		end
	end
end

-- overridden in co-op version
DoMcvArrival = function()
	Reinforcements.Reinforce(Greece, { "2tnk", "mcv", "2tnk" }, { McvEntry.Location, McvRally.Location }, 75)
end

RevealPrison = function()
	if not IsPrisonRevealed then
		Beacon.New(Greece, GDICommanderSpawn.CenterPosition)

		if ObjectiveLocateCommander == nil then
			ObjectiveLocateCommander = Greece.AddObjective("Locate the GDI commander.")
		end

		Trigger.AfterDelay(DateTime.Seconds(1), function()
			ObjectiveCapturePrison = Greece.AddObjective("Take control of prison and rescue GDI commander.")

			if not Greece.IsObjectiveCompleted(ObjectiveLocateCommander) then
				Greece.MarkCompletedObjective(ObjectiveLocateCommander)
			end

			UserInterface.SetMissionText("Take control of prison and rescue GDI commander.", HSLColor.Yellow)
			PrisonCamera = Actor.Create("camera.paradrop", true, { Owner = Greece, Location = SovietPrison.Location })

			Trigger.AfterDelay(DateTime.Seconds(5), function()
				PrisonCamera.Destroy()
			end)
		end)

		IsPrisonRevealed = true
	end
end

InitUSSR = function()
	if Difficulty == "easy" then
		RebuildExcludes.USSR = { Types = { "tsla", "ftur" }, Actors = { NorthSAM1, NorthSAM2 } }
	elseif Difficulty == "normal" then
		RebuildExcludes.USSR = { Types = { "tsla" }, Actors = { NorthSAM1, NorthSAM2 } }
	end

	AutoRepairAndRebuildBuildings(USSR, 15)
	SetupRefAndSilosCaptureCredits(USSR)
	AutoReplaceHarvesters(USSR)
	AutoRebuildConyards(USSR)
	InitAiUpgrades(USSR)
	InitUSSRPatrols()

	local ussrGroundAttackers = USSR.GetGroundAttackers()

	Utils.Do(ussrGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsUSSRGroundHunterUnit)
	end)

	-- If main sub pens are destroyed, update naval attack path
	Utils.Do({ SovietSouthSubPen1, SovietSouthSubPen2 }, function(a)
		Trigger.OnRemovedFromWorld(a, function(self)
			if SovietSouthSubPen1.IsDead and SovietSouthSubPen2.IsDead and not SovietNorthSubPen.IsDead then
				Squads.Naval.AttackPaths = SovietNavalFallbackAttackPaths
				Squads.Naval.ProducerActors.Ships = { SovietNorthSubPen }
			end
		end)
	end)

	local hinds = USSR.GetActorsByType("hind")
	Utils.Do(hinds, function(a)
		Trigger.OnDamaged(a, function(self, attacker, damage)
			if not self.IsDead and self.AmmoCount() == 0 then
				Trigger.ClearAll(self)
				self.Stop()
				self.ReturnToBase()
				Trigger.AfterDelay(DateTime.Seconds(1), function()
					if not self.IsDead then
						self.Patrol(SovietHindPatrolPath, true)
					end
				end)
			end
		end)
	end)
end

InitUSSRPatrols = function()
	local centerPatrollers = { SovietCenterPatroller1, SovietCenterPatroller2, SovietCenterPatroller3, SovietCenterPatroller4, SovietCenterPatroller5, SovietCenterPatroller6, SovietCenterPatroller7, SovietCenterPatroller8 }
	local shorePatrollers = { SovietShorePatroller1, SovietShorePatroller2, SovietShorePatroller3, SovietShorePatroller4, SovietShorePatroller5, SovietShorePatroller6, SovietShorePatroller7 }
	local hindPatrollers = { SovietHindPatroller1, SovietHindPatroller2 }

	Utils.Do(centerPatrollers, function(unit)
		if not unit.IsDead then
			unit.Patrol(SovietCenterPatrolPath, true, 100)
		end
	end)

	Utils.Do(shorePatrollers, function(unit)
		if not unit.IsDead then
			unit.Patrol(SovietShorePatrolPath, true, 100)
		end
	end)

	Utils.Do(hindPatrollers, function(unit)
		if not unit.IsDead then
			unit.Patrol(SovietHindPatrolPath, true)
		end
	end)
end

InitUSSRAttacks = function()
	Trigger.AfterDelay(DateTime.Seconds(4), function()
		local siegeUnits = Map.ActorsInBox(SiegeUnitsBoxTopLeft.CenterPosition, SiegeUnitsBoxBottomRight.CenterPosition, function(a)
			return a.Owner == USSR and not a.IsDead and a.HasProperty("Hunt")
		end)

		Utils.Do(siegeUnits, function(a)
			a.Hunt()
		end)
	end)

	InitAttackSquad(Squads.Main, USSR)
	InitAttackSquad(Squads.Northern, USSR)
	InitAirAttackSquad(Squads.Migs, USSR)
	InitNavalAttackSquad(Squads.Naval, USSR)

	Trigger.AfterDelay(HaloDropStart, DoHaloDrop)
	Trigger.AfterDelay(NavalDropStart, DoNavalDrop)
end

DoHaloDrop = function()
	local entryPath

	if SovietNorthFactory.IsDead or SovietNorthFactory.Owner ~= USSR then
		entryPath = Utils.Random(LateHaloDropPaths)
	else
		entryPath = Utils.Random(HaloDropPaths)
	end

	local haloDropUnits = { "e1", "e1", "e1", "e2", "e3", "e4" }

	if IsHardOrAbove() and DateTime.GameTime > DateTime.Minutes(15) then
		haloDropUnits = { "e1", "e1", "e1", "e1", "e2", "e2", "e3", "e3", "e4", "shok" }
	end

	DoHelicopterDrop(USSR, entryPath, "halo.paradrop", haloDropUnits, AssaultPlayerBaseOrHunt, function(t)
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			if not t.IsDead then
				t.Move(entryPath[1])
				t.Destroy()
			end
		end)
	end)

	local delayUntilNext = CalculateInterval(GetTotalCostOfUnits(haloDropUnits), HaloDropAttackValue, HaloDropStart)
	Trigger.AfterDelay(delayUntilNext, DoHaloDrop)
end

DoNavalDrop = function()
	if SovietSouthSubPen1.IsDead and SovietSouthSubPen2.IsDead then
		return
	end

	local navalDropPath = Utils.Random(NavalDropPaths)
	local navalDropExitPath = { navalDropPath[2], navalDropPath[1] }
	local navalDropUnits = { "3tnk", "btr.ai" }

	if Difficulty == "normal" then
		navalDropUnits = { "3tnk", "v2rl", "btr.ai" }
	elseif IsHardOrAbove() then
		if DateTime.GameTime > DateTime.Minutes(18) then
			navalDropUnits = { "3tnk", "v3rl", "3tnk", "btr.ai" }
		else
			navalDropUnits = { "3tnk", "v2rl", "3tnk", "btr.ai" }
		end
	end

	DoNavalTransportDrop(USSR, navalDropPath, navalDropExitPath, "lst", navalDropUnits, AssaultPlayerBaseOrHunt)

	local delayUntilNext = CalculateInterval(GetTotalCostOfUnits(navalDropUnits), NavalDropAttackValue, NavalDropStart)
	Trigger.AfterDelay(delayUntilNext, DoNavalDrop)
end

NavalReinforcements = function()
	if not NavalReinforcementsArrived and PlayerHasNavalProduction(Greece) then
		NavalReinforcementsArrived = true
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			local cruisers = { "ca", "ca" }

			if IsHardOrAbove() then
				cruisers = { "ca" }
			end

			local destroyers = { "dd", "dd", "dd" }

			if Difficulty == "easy" then
				destroyers = { "dd", "dd", "dd", "dd" }
			end

			PlaySpeechNotificationToMissionPlayers("ReinforcementsArrived")
			Beacon.New(Greece, CruiserSpawn.CenterPosition)
			Reinforcements.Reinforce(Greece, cruisers, { CruiserSpawn.Location, CruiserDestination.Location }, 75)
			Reinforcements.Reinforce(Greece, destroyers, { DestroyerSpawn.Location, DestroyerDestination.Location }, 75)
		end)
	end
end
