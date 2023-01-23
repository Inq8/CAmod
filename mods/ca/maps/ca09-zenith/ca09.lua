
Squads = {
	Planes = {
		Player = nil,
		Delay = {
			easy = DateTime.Minutes(10),
			normal = DateTime.Minutes(8),
			hard = DateTime.Minutes(6)
		},
		Interval = {
			easy = DateTime.Seconds(210),
			normal = DateTime.Seconds(180),
			hard = DateTime.Seconds(150)
		},
		QueueProductionStatuses = {
			Aircraft = false
		},
		IdleUnits = { },
		ProducerActors = nil,
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
		Player = nil,
		Delay = {
			easy = DateTime.Minutes(8),
			normal = DateTime.Minutes(6),
			hard = DateTime.Minutes(4)
		},
		Interval = {
			easy = DateTime.Seconds(210),
			normal = DateTime.Seconds(180),
			hard = DateTime.Seconds(150)
		},
		QueueProductionStatuses = {
			Aircraft = false
		},
		IdleUnits = { },
		ProducerActors = nil,
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
	}
}

NukeSilos = { NukeSilo1, NukeSilo2, NukeSilo3, NukeSilo4 }

NukeTimer = {
	hard = 45000,
	normal = 60000,
	easy = 75000
}

HaloDropStart = {
	easy = DateTime.Minutes(8),
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
	MissionPlayer = Nod
	TimerTicks = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Nod)
	InitUSSR()

	ObjectiveKillSilos = Nod.AddObjective("Destroy Soviet missile silos before launch.")
	ObjectiveKillReactors = Nod.AddSecondaryObjective("Destroy reactors on north west of island.")
	ObjectiveKillAirbase = Nod.AddSecondaryObjective("Destroy airbase on north east of island.")

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
		USSR.Cash = 7500
		USSR.Resources = 7500

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
		-- nothing
	end
end

InitUSSR = function()
	RebuildExcludes.USSR = { Types = { "tsla", "ftur", "tpwr", "afld", "hpad" } }

	if Difficulty == "easy" then
		AutoRepairBuildings(USSR)
	else
		AutoRepairAndRebuildBuildings(USSR, 5)
	end

	SetupRefAndSilosCaptureCredits(USSR)

	Actor.Create("POWERCHEAT", true, { Owner = USSR, Location = UpgradeCreationLocation })
	Actor.Create("hazmatsoviet.upgrade", true, { Owner = USSR, Location = UpgradeCreationLocation })

	if Difficulty == "hard" then
		Trigger.AfterDelay(DateTime.Minutes(20), function()
			Actor.Create("flakarmor.upgrade", true, { Owner = USSR, Location = UpgradeCreationLocation })
			Actor.Create("tarc.upgrade", true, { Owner = USSR, Location = UpgradeCreationLocation })
		end)
	end

	Utils.Do(NukeSilos, function(a)
		Trigger.ClearAll(a)
		Trigger.AfterDelay(1, function()
			AutoRepairBuilding(a, USSR)

			Trigger.OnKilled(a, function(self, killer)
				Trigger.AfterDelay(DateTime.Seconds(1), function()
					local livingNukeSilos = Utils.Where(NukeSilos, function(a)
						return not a.IsDead
					end)
					if #livingNukeSilos == 0 and not Nod.IsObjectiveFailed(ObjectiveKillSilos) then
						Nod.MarkCompletedObjective(ObjectiveKillSilos)
					end
				end)
			end)
		end)
	end)

	Utils.Do(TeslaReactors, function(a)
		Trigger.OnKilled(a, function(self, killer)
			Trigger.AfterDelay(DateTime.Seconds(1), function()
				local livingTeslaReactors = Utils.Where(TeslaReactors, function(a)
					return not a.IsDead
				end)
				if #livingTeslaReactors == 0 then
					local teslaCoils = USSR.GetActorsByType("tsla")
					Utils.Do(teslaCoils, function(a)
						if not a.IsDead then
							a.GrantCondition("powerdown")
						end
					end)
					if not Nod.IsObjectiveCompleted(ObjectiveKillReactors) then
						Nod.MarkCompletedObjective(ObjectiveKillReactors)
					end
				end
			end)
		end)
	end)

	Utils.Do(AirbaseStructures, function(a)
		Trigger.OnKilled(a, function(self, killer)
			Trigger.AfterDelay(DateTime.Seconds(1), function()
				local livingAirbaseStructures = Utils.Where(AirbaseStructures, function(a)
					return not a.IsDead
				end)
				if #livingAirbaseStructures == 0 then
					if not Nod.IsObjectiveCompleted(ObjectiveKillAirbase) then
						Nod.MarkCompletedObjective(ObjectiveKillAirbase)
					end
				end
			end)
		end)
	end)

	local ussrGroundAttackers = USSRUnits.GetGroundAttackers()

	Utils.Do(ussrGroundAttackers, function(a)
		TargetSwapChance(a, USSR, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsUSSRGroundHunterUnit)
	end)

	Trigger.OnEnteredProximityTrigger(MADTank.CenterPosition, WDist.New(7 * 1024), function(a, id)
		if a.Owner == Nod and not IsMADTankDetonated then
			IsMADTankDetonated = true
			Trigger.RemoveProximityTrigger(id)
			MADTank.MadTankDetonate()
			local madTankCamera = Actor.Create("smallcamera", true, { Owner = Nod, Location = MADTank.Location })
			Trigger.AfterDelay(DateTime.Seconds(4), function()
				madTankCamera.Destroy()
			end)
		end
	end)

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

	DoHelicopterDrop(USSRUnits, entryPath, "halo.paradrop", haloDropUnits, function(u) u.Patrol(PatrolPath) end, function(t)
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			if not t.IsDead then
				t.Move(entryPath[1])
				t.Destroy()
			end
		end)
	end)

	Trigger.AfterDelay(HaloDropInterval[Difficulty], DoHaloDrop)
end