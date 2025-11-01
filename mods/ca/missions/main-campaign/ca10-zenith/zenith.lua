MissionDir = "ca|missions/main-campaign/ca10-zenith"

NukeSilos = { NukeSilo1, NukeSilo2, NukeSilo3, NukeSilo4 }

NukeTimer = {
	easy = DateTime.Minutes(60),
	normal = DateTime.Minutes(45),
	hard = DateTime.Minutes(35),
	vhard = DateTime.Minutes(30),
	brutal = DateTime.Minutes(25),
}

HaloDropStart = AdjustDelayForDifficulty(DateTime.Minutes(6))
HaloDropAttackValue = AdjustAttackValuesForDifficulty({ Min = 5, Max = 8, RampDuration = DateTime.Minutes(6) })

TeslaReactors = { TeslaReactor1, TeslaReactor2, TeslaReactor3, TeslaReactor4, TeslaReactor5, TeslaReactor6 }
AirbaseStructures = { Airfield1, Airfield2, Airfield3, Airfield4, Airfield5, Helipad1, Helipad2, Helipad3 }
PatrolPath = { Patrol1.Location, Patrol2.Location, Patrol3.Location, Patrol4.Location, Patrol5.Location, Patrol6.Location, Patrol7.Location, Patrol8.Location, Patrol9.Location }

Squads = {
	Planes = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(8)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = {
			easy = {
				{ Aircraft = { "yak" } },
				{ Aircraft = { "mig" } }
			},
			normal = {
				{ Aircraft = { "yak", "yak" } },
				{ Aircraft = { "mig", "yak" } }
			},
			hard = {
				{ Aircraft = { "mig", "mig", "yak" } },
				{ Aircraft = { "mig", "yak", "yak" } }
			},
			vhard = {
				{ Aircraft = { "mig", "mig", "mig" } },
				{ Aircraft = { "yak", "yak", MigOrSukhoi } }
			},
			brutal = {
				{ Aircraft = { "mig", "mig", "yak", MigOrSukhoi } },
				{ Aircraft = { "mig", "yak", "yak", MigOrSukhoi } }
			}
		},
	},
	Helicopters = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(6)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = {
			easy = {
				{ Aircraft = { "hind" } }
			},
			normal = {
				{ Aircraft = { "hind", "hind" } }
			},
			hard = {
				{ Aircraft = { "hind", "hind", "hind" } }
			},
			vhard = {
				{ Aircraft = { "hind", "hind", "hind" } }
			},
			brutal = {
				{ Aircraft = { "hind", "hind", "hind", "hind" } }
			}
		},
	},
	Naval = {
		ActiveCondition = function()
			return PlayerHasICBMSubs()
		end,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 15, Max = 20 }),
		Compositions = {
			{ Ships = { "ss" } }
		},
		AttackPaths = {
			{ SubPatrol1.Location, SubPatrol2.Location, SubPatrol3.Location, SubPatrol4.Location, SubPatrol5.Location, SubPatrol6.Location },
			{ SubPatrol1.Location, SubPatrol6.Location, SubPatrol5.Location, SubPatrol4.Location, SubPatrol3.Location, SubPatrol2.Location },
		},
	},
	MissileSubs = {
		Delay = DateTime.Minutes(10),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 15, Max = 30 }),
		Compositions = {
			brutal = {
				{ Ships = { "msub", "msub" } }
			}
		},
		AttackPaths = {
			{ Bombard1.Location },
			{ Bombard2.Location },
		}
	},
	AirToAir = AirToAirSquad({ "mig" }, AdjustAirDelayForDifficulty(DateTime.Minutes(10)))
}

-- Setup and Tick

SetupPlayers = function()
	Nod = Player.GetPlayer("Nod")
	USSR = Player.GetPlayer("USSR")
	USSRUnits = Player.GetPlayer("USSRUnits")
	MissionPlayers = { Nod }
	MissionEnemies = { USSR }
end

WorldLoaded = function()
	SetupPlayers()

	TimerTicks = 0
	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Nod)
	AdjustPlayerStartingCashForDifficulty()
	InitUSSR()

	ObjectiveKillSilos = Nod.AddObjective("Destroy Soviet missile silos before launch.")
	ObjectiveKillReactors = Nod.AddSecondaryObjective("Destroy reactors on north-west of island.")
	ObjectiveKillAirbase = Nod.AddSecondaryObjective("Destroy airbase on north-east of island.")

	if Difficulty == "brutal" then
		NukeDummy = Actor.Create("NukeDummyBrutal", true, { Owner = USSR, Location = NukeSilo1.Location })
	elseif Difficulty == "vhard" then
		NukeDummy = Actor.Create("NukeDummyVeryHard", true, { Owner = USSR, Location = NukeSilo1.Location })
	elseif Difficulty == "hard" then
		NukeDummy = Actor.Create("NukeDummyHard", true, { Owner = USSR, Location = NukeSilo1.Location })
	elseif Difficulty == "normal" then
		NukeDummy = Actor.Create("NukeDummyNormal", true, { Owner = USSR, Location = NukeSilo1.Location })
	else
		NukeDummy = Actor.Create("NukeDummyEasy", true, { Owner = USSR, Location = NukeSilo1.Location })
	end

	local satHack = Actor.Create("camera.sathack", true, { Owner = Nod, Location = SatHack.Location })

	Trigger.AfterDelay(DateTime.Seconds(4), function()
		satHack.Destroy()
	end)

	Trigger.AfterDelay(NukeTimer[Difficulty] + DateTime.Seconds(3), function()
		if not NukeDummy.IsDead then
			if not NukeSilo1.IsDead then
				NukeSilo1.ActivateNukePower(PlayerStart.Location)
			elseif not NukeSilo2.IsDead then
				NukeSilo2.ActivateNukePower(PlayerStart.Location)
			elseif not NukeSilo3.IsDead then
				NukeSilo3.ActivateNukePower(PlayerStart.Location)
			elseif not NukeSilo4.IsDead then
				NukeSilo4.ActivateNukePower(PlayerStart.Location)
			end
			NukeDummy.Destroy()
			Media.PlaySound("nukelaunch.aud")
			Media.PlaySpeechNotification(nil, "AbombLaunchDetected")
			Notification("A-Bomb launch detected.")

			Trigger.AfterDelay(DateTime.Seconds(3), function()
				if not Nod.IsObjectiveCompleted(ObjectiveKillSilos) then
					Nod.MarkFailedObjective(ObjectiveKillSilos)
				end
			end)
		end
	end)

	Trigger.OnEnteredFootprint({ HaloTrigger1.Location, HaloTrigger2.Location, HaloTrigger3.Location, HaloTrigger4.Location }, function(a, id)
		if IsMissionPlayer(a.Owner) and not HaloDropsTriggered then
			HaloDropsTriggered = true
			Trigger.RemoveFootprintTrigger(id)
			DoHaloDrop()
		end
	end)

	AfterWorldLoaded()
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
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
		end

		if MissionPlayersHaveNoRequiredUnits() then
			if not Nod.IsObjectiveCompleted(ObjectiveKillSilos) then
				Nod.MarkFailedObjective(ObjectiveKillSilos)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		-- nothing
	end
end

InitUSSR = function()
	RebuildExcludes.USSR = { Types = { "tsla", "ftur", "tpwr", "afld", "hpad", "mslo" } }

	if Difficulty == "easy" then
		AutoRepairBuildings(USSR)
	else
		AutoRepairAndRebuildBuildings(USSR, 5)
	end

	SetupRefAndSilosCaptureCredits(USSR)
	InitAiUpgrades(USSR)
	InitAirAttackSquad(Squads.Planes, USSR)
	InitAirAttackSquad(Squads.Helicopters, USSR)

	if IsNormalOrAbove() then
		InitNavalAttackSquad(Squads.Naval, USSR)

		if IsHardOrAbove() then
			InitAirAttackSquad(Squads.AirToAir, USSR, MissionPlayers, { "Aircraft" }, "ArmorType")

			if Difficulty == "brutal" then
				InitNavalAttackSquad(Squads.MissileSubs, USSR)
			end
		end
	end

	Actor.Create("ai.unlimited.power", true, { Owner = USSR })

	Trigger.OnAllKilledOrCaptured(NukeSilos, function()
		Nod.MarkCompletedObjective(ObjectiveKillSilos)
	end)

	Trigger.OnAllKilledOrCaptured(TeslaReactors, function()
		local teslaCoils = USSR.GetActorsByType("tsla")
		Utils.Do(teslaCoils, function(a)
			if not a.IsDead then
				a.GrantCondition("disabled")
			end
		end)
		Nod.MarkCompletedObjective(ObjectiveKillReactors)
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Notification("Excellent! The north-west Tesla Reactors have been neutralised; all Soviet Tesla Coils are now offline.")
		end)
	end)

	Trigger.OnAllKilledOrCaptured(AirbaseStructures, function()
		Nod.MarkCompletedObjective(ObjectiveKillAirbase)
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Notification("Good work commander! Their airbase has been neutralised, so you no longer have to worry about being attacked from the air.")
		end)
	end)

	local ussrGroundAttackers = USSRUnits.GetGroundAttackers()

	Utils.Do(ussrGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsUSSRGroundHunterUnit)
	end)

	Trigger.OnEnteredProximityTrigger(MADTank.CenterPosition, WDist.New(7 * 1024), function(a, id)
		if not MADTank.IsDead and not IsMADTankDetonated and IsMissionPlayer(a.Owner) and not a.HasProperty("Land") and a.HasProperty("Health") then
			IsMADTankDetonated = true
			Trigger.RemoveProximityTrigger(id)
			MADTank.MadTankDetonate()
			local madTankCamera = Actor.Create("smallcamera", true, { Owner = Nod, Location = MADTank.Location })
			Trigger.AfterDelay(DateTime.Seconds(4), function()
				madTankCamera.Destroy()
			end)
		end
	end)
end

DoHaloDrop = function()
	local entryPath

	if Nod.IsObjectiveCompleted(ObjectiveKillAirbase) then
		return
	end

	entryPath = { PatrolDropSpawn.Location, PatrolDropLanding.Location }

	local haloDropUnits = { "e1", "e1", "e1", "e2", "e3", "e4" }

	if IsHardOrAbove() and DateTime.GameTime > DateTime.Minutes(15) then
		haloDropUnits = { "e1", "e1", "e1", "e1", "e2", "e2", "e3", "e3", "e4", "shok" }
	end

	DoHelicopterDrop(USSRUnits, entryPath, "halo.paradrop", haloDropUnits,
		function(u)
			if not u.IsDead then
				u.Patrol(PatrolPath)
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

	local delayUntilNext = CalculateInterval(GetTotalCostOfUnits(haloDropUnits), HaloDropAttackValue, HaloDropStart)
	Trigger.AfterDelay(delayUntilNext, DoHaloDrop)
end

PlayerHasICBMSubs = function()
	local icbmSubs = Nod.GetActorsByType("isub")
	return #icbmSubs > 0
end

IdleHunt = function(actor)
	if actor.HasProperty("HuntCA") and not actor.IsDead then
		Trigger.OnIdle(actor, function(a)
			if not a.IsDead and a.IsInWorld and not IsMissionPlayer(a.Owner) then
				a.HuntCA()
			end
		end)
	end
end
