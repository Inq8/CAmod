
Squads = {
	Planes = {
		Delay = {
			easy = DateTime.Minutes(10),
			normal = DateTime.Minutes(8),
			hard = DateTime.Minutes(6)
		},
		AttackValuePerSecond = {
			easy = { Min = 7, Max = 7 },
			normal = { Min = 14, Max = 14 },
			hard = { Min = 21, Max = 21 },
		},
		ProducerTypes = { Aircraft = { "afld" } },
		Units = {
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
			}
		},
	},
	Helicopters = {
		Delay = {
			easy = DateTime.Minutes(8),
			normal = DateTime.Minutes(6),
			hard = DateTime.Minutes(4)
		},
		AttackValuePerSecond = {
			easy = { Min = 7, Max = 7 },
			normal = { Min = 14, Max = 14 },
			hard = { Min = 21, Max = 21 },
		},
		ProducerTypes = { Aircraft = { "hpad" } },
		Units = {
			easy = {
				{ Aircraft = { "hind" } }
			},
			normal = {
				{ Aircraft = { "hind", "hind" } }
			},
			hard = {
				{ Aircraft = { "hind", "hind", "hind" } }
			}
		},
	},
	Naval = {
		ActiveCondition = function()
			return PlayerHasICBMSubs()
		end,
		Interval = {
			normal = DateTime.Seconds(60),
			hard = DateTime.Seconds(30)
		},
		ProducerTypes = { Ships = { "spen" } },
		Units = {
			normal = {
				{ Ships = { "ss" } }
			},
			hard = {
				{ Ships = { "ss" } }
			}
		},
		AttackPaths = {
			{ SubPatrol1.Location, SubPatrol2.Location, SubPatrol3.Location, SubPatrol4.Location, SubPatrol5.Location, SubPatrol6.Location },
			{ SubPatrol1.Location, SubPatrol6.Location, SubPatrol5.Location, SubPatrol4.Location, SubPatrol3.Location, SubPatrol2.Location },
		},
	}
}

NukeSilos = { NukeSilo1, NukeSilo2, NukeSilo3, NukeSilo4 }

NukeTimer = {
	hard = 45000,
	normal = 60000,
	easy = 75000
}

HaloDropStart = {
	easy = DateTime.Minutes(10),
	normal = DateTime.Minutes(6),
	hard = DateTime.Minutes(4)
}

HaloDropInterval = {
	easy = DateTime.Minutes(4),
	normal = DateTime.Minutes(3),
	hard = DateTime.Minutes(2)
}

TeslaReactors = { TeslaReactor1, TeslaReactor2, TeslaReactor3, TeslaReactor4, TeslaReactor5, TeslaReactor6 }
AirbaseStructures = { Airfield1, Airfield2, Airfield3, Airfield4, Airfield5, Helipad1, Helipad2, Helipad3 }
PatrolPath = { Patrol1.Location, Patrol2.Location, Patrol3.Location, Patrol4.Location, Patrol5.Location, Patrol6.Location, Patrol7.Location, Patrol8.Location, Patrol9.Location }

-- Setup and Tick

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	USSR = Player.GetPlayer("USSR")
	USSRUnits = Player.GetPlayer("USSRUnits")
	MissionPlayers = { Nod }
	TimerTicks = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Nod)
	AdjustStartingCash()
	InitUSSR()

	ObjectiveKillSilos = Nod.AddObjective("Destroy Soviet missile silos before launch.")
	ObjectiveKillReactors = Nod.AddSecondaryObjective("Destroy reactors on north-west of island.")
	ObjectiveKillAirbase = Nod.AddSecondaryObjective("Destroy airbase on north-east of island.")

	if Difficulty == "hard" then
		NukeDummy = Actor.Create("NukeDummyHard", true, { Owner = USSR, Location = NukeSilo1.Location })
	elseif Difficulty == "normal" then
		NukeDummy = Actor.Create("NukeDummyNormal", true, { Owner = USSR, Location = NukeSilo1.Location })
	else
		NukeDummy = Actor.Create("NukeDummyEasy", true, { Owner = USSR, Location = NukeSilo1.Location })
		Actor.Create("difficulty.easy", true, { Owner = Nod, Location = PlayerStart.Location })
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
			Media.PlaySpeechNotification(Nod, "AbombLaunchDetected")
			Notification("A-Bomb launch detected.")

			Trigger.AfterDelay(DateTime.Seconds(3), function()
				if not Nod.IsObjectiveCompleted(ObjectiveKillSilos) then
					Nod.MarkFailedObjective(ObjectiveKillSilos)
				end
			end)
		end
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
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

		if Nod.HasNoRequiredUnits() then
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

	Actor.Create("ai.unlimited.power", true, { Owner = USSR })
	Actor.Create("ai.superweapons.enabled", true, { Owner = USSR })

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
		if not MADTank.IsDead and not IsMADTankDetonated and a.Owner == Nod and not a.HasProperty("Land") and a.HasProperty("Health") then
			IsMADTankDetonated = true
			Trigger.RemoveProximityTrigger(id)
			MADTank.MadTankDetonate()
			local madTankCamera = Actor.Create("smallcamera", true, { Owner = Nod, Location = MADTank.Location })
			Trigger.AfterDelay(DateTime.Seconds(4), function()
				madTankCamera.Destroy()
			end)
		end
	end)

	if Difficulty ~= "easy" then
		InitNavalAttackSquad(Squads.Naval, USSR)
	end

	Trigger.AfterDelay(Squads.Planes.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.Planes, USSR, Nod, { "ltnk", "ftnk", "mlrs", "bggy", "bike", "arty.nod", "nuke", "nuk2" })
	end)

	Trigger.AfterDelay(Squads.Helicopters.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.Helicopters, USSR, Nod, { "ltnk", "ftnk", "mlrs", "bggy", "bike", "arty.nod", "nuke", "nuk2" })
	end)

	Trigger.AfterDelay(HaloDropStart[Difficulty], function()
		DoHaloDrop()
	end)
end

DoHaloDrop = function()
	local entryPath

	if Nod.IsObjectiveCompleted(ObjectiveKillAirbase) then
		return
	end

	entryPath = { PatrolDropSpawn.Location, PatrolDropLanding.Location }

	local haloDropUnits = { "e1", "e1", "e1", "e2", "e3", "e4" }

	if Difficulty == "hard" and DateTime.GameTime > DateTime.Minutes(15) then
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

	Trigger.AfterDelay(HaloDropInterval[Difficulty], DoHaloDrop)
end

PlayerHasICBMSubs = function()
	local icbmSubs = Nod.GetActorsByType("isub")
	return #icbmSubs > 0
end
