-- Locations and Paths

Convoys = {
	{
		Spawn = { FirstConvoyPath1.Location, FirstConvoyPath2.Location },
		Path = { FirstConvoyPath3.Location, FirstConvoyPath4.Location, FirstConvoyPath5.Location, FirstConvoyPath6.Location, FirstConvoyPath7.Location, FirstConvoyPath8.Location, FirstConvoyPath9.Location, FirstConvoyPath10.Location, FirstConvoyPath11.Location, FirstConvoyPath12.Location, FirstConvoyPath13.Location, FirstConvoyPath14.Location, FirstConvoyPath15.Location, FirstConvoyPath16.Location, FirstConvoyPath17.Location, FirstConvoyPath18.Location },
		FlareWaypoint = FirstConvoyPath2
	},
	{
		Spawn = { SecondConvoyPath1.Location, SecondConvoyPath2.Location },
		Path = { SecondConvoyPath3.Location, SecondConvoyPath4.Location, SecondConvoyPath5.Location, SecondConvoyPath6.Location, SecondConvoyPath7.Location, SecondConvoyPath8.Location, SecondConvoyPath9.Location },
		FlareWaypoint = SecondConvoyFlare
	},
	{
		Spawn = { ThirdConvoyPath1.Location, ThirdConvoyPath2.Location },
		Path = { ThirdConvoyPath3.Location, ThirdConvoyPath4.Location, ThirdConvoyPath5.Location, ThirdConvoyPath6.Location, ThirdConvoyPath7.Location, ThirdConvoyPath8.Location, ThirdConvoyPath9.Location, ThirdConvoyPath10.Location, SecondConvoyPath2.Location, SecondConvoyPath3.Location, SecondConvoyPath4.Location, SecondConvoyPath5.Location, SecondConvoyPath6.Location, SecondConvoyPath7.Location, SecondConvoyPath8.Location, SecondConvoyPath9.Location },
		FlareWaypoint = ThirdConvoyPath2
	},
	{
		Spawn = { FourthConvoyPath1.Location, FourthConvoyPath2.Location },
		Path = { FourthConvoyPath3.Location, FourthConvoyPath4.Location, FourthConvoyPath5.Location, FourthConvoyPath6.Location, FourthConvoyPath7.Location, FourthConvoyPath8.Location, FourthConvoyPath9.Location, FourthConvoyPath10.Location, FourthConvoyPath11.Location, FourthConvoyPath12.Location, FourthConvoyPath13.Location, FourthConvoyPath14.Location, FourthConvoyPath15.Location, FourthConvoyPath16.Location, SecondConvoyPath6.Location, SecondConvoyPath7.Location, SecondConvoyPath8.Location, SecondConvoyPath9.Location },
		FlareWaypoint = FourthConvoyPath2
	}
}

ConvoyExits = {
	{ FirstConvoyPath18.Location, CPos.New(FirstConvoyPath18.Location.X - 1, FirstConvoyPath18.Location.Y), CPos.New(FirstConvoyPath18.Location.X + 1, FirstConvoyPath18.Location.Y) },
	{ SecondConvoyPath9.Location, CPos.New(SecondConvoyPath9.Location.X - 1, SecondConvoyPath9.Location.Y), CPos.New(SecondConvoyPath9.Location.X + 1, SecondConvoyPath9.Location.Y) }
}

ScrinAttackPaths = {
	{ ScrinAttackAssembly1.Location, PlayerRefinery.Location },
	{ ScrinAttackAssembly2.Location, PlayerRefinery.Location },
	{ ScrinAttackAssembly3.Location, PlayerRefinery.Location }
}

-- Other Variables

ConvoyUnits = { "truk", "truk", "truk", "truk", "truk" }

MaxLosses = {
	easy = 9,
	normal = 4,
	hard = 0
}

NavalReinforcementsDelay = {
	easy = DateTime.Minutes(2),
	normal = DateTime.Minutes(4),
}

TimeBetweenConvoys = {
	easy = { DateTime.Minutes(3), DateTime.Minutes(8), DateTime.Minutes(4), DateTime.Minutes(5)  },
	normal = { DateTime.Minutes(2), DateTime.Minutes(7), DateTime.Minutes(3), DateTime.Minutes(4) + DateTime.Seconds(30) },
	hard = { DateTime.Minutes(1), DateTime.Minutes(6), DateTime.Minutes(2), DateTime.Minutes(4) }
}

-- Squads

Squads = {
	Main = {
		Delay = {
			easy = DateTime.Seconds(230),
			normal = DateTime.Seconds(160),
			hard = DateTime.Seconds(90)
		},
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 15 }, { MinTime = DateTime.Minutes(13), Value = 25 } },
			normal = { { MinTime = 0, Value = 35 }, { MinTime = DateTime.Minutes(11), Value = 45 } },
			hard = { { MinTime = 0, Value = 60 }, { MinTime = DateTime.Minutes(9), Value = 70 } },
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false
		},
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Infantry = { "port" }, Vehicles = { "wsph" } },
		Units = {
			easy = {
				{
					Infantry = { "s1", "s1", "s1", "s3", "s3" },
					Vehicles = { "intl.ai2", "intl.ai2", "gunw" },
					MaxTime = DateTime.Minutes(13),
				},
				{
					Infantry = { "s1", "s1", "s1", "s3", "s3" },
					Vehicles = { "intl.ai2", "intl.ai2", "gunw", "gunw", "corr" },
					MinTime = DateTime.Minutes(13),
				}
			},
			normal = {
				{
					Infantry = { "s1", "s1", "s1", "s1", "s3", "s3" },
					Vehicles = { "intl.ai2", "intl.ai2", "gunw" },
					MaxTime = DateTime.Minutes(11),
				},
				{
					Infantry = { "s1", "s1", "s1", "s1", "s3", "s3", "s4", "s4" },
					Vehicles = { "intl.ai2", "intl.ai2", "gunw", "corr", "devo", "seek", "seek" },
					MinTime = DateTime.Minutes(11),
				}
			},
			hard = {
				{
					Infantry = { "s1", "s1", "s1", "s1", "s3", "s3", "s4" },
					Vehicles = { "intl.ai2", "intl.ai2", "gunw", "seek" },
					MaxTime = DateTime.Minutes(9),
				},
				{
					Infantry = { "s1", "s1", "s1", "s1", "s1", "s1", "s2", "s2", "s3", "s3", "s4" },
					Vehicles = { "intl.ai2", "intl.ai2", "gunw", "corr", "devo", "seek", "tpod", "seek" },
					MinTime = DateTime.Minutes(9),
				}
			}
		},
		AttackPaths = ScrinAttackPaths,
	},
	Stormriders = {
		Delay = {
			easy = DateTime.Seconds(330),
			normal = DateTime.Seconds(270),
			hard = DateTime.Seconds(210)
		},
		Interval = {
			easy = DateTime.Seconds(330),
			normal = DateTime.Seconds(240),
			hard = DateTime.Seconds(150)
		},
		QueueProductionStatuses = {
			Aircraft = false
		},
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Aircraft = { "grav" } },
		Units = {
			easy = {
				{ Aircraft = { "stmr", "stmr" } }
			},
			normal = {
				{ Aircraft = { "stmr", "stmr", "stmr" } }
			},
			hard = {
				{ Aircraft = { "stmr", "stmr", "stmr", "stmr" } }
			}
		}
	},
	Devastators = {
		Delay = {
			normal = DateTime.Minutes(15),
			hard = DateTime.Minutes(10)
		},
		Interval = {
			normal = DateTime.Seconds(120),
			hard = DateTime.Seconds(60)
		},
		QueueProductionStatuses = {
			Aircraft = false
		},
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Aircraft = { "grav" } },
		Units = {
			normal = {
				{ Aircraft = { "deva" } }
			},
			hard = {
				{ Aircraft = { "deva", "deva" } }
			}
		}
	}
}

-- Setup and Tick

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	England = Player.GetPlayer("England")
	Scrin = Player.GetPlayer("Scrin")
	USSR = Player.GetPlayer("USSR")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { Greece }
	TimerTicks = 0
	TrucksLost = 0
	TrucksLostCurrentConvoy = 0
	NextConvoyIdx = 1
	CurrentConvoyArrivalComplete = false
	PathsClear = false

	Camera.Position = PlayerBarracks.CenterPosition

	InitObjectives(Greece)
	AdjustStartingCash()
	InitScrin()

	ObjectiveClearPath = Greece.AddObjective("Clear a path for inbound convoys.")

	if Difficulty == "hard" then
		ObjectiveProtectConvoys = Greece.AddObjective("Do not lose any convoy trucks.")
	else
		ObjectiveProtectConvoys = Greece.AddObjective("Do not lose more than " .. MaxLosses[Difficulty] .. " convoy trucks.")
	end

	if Difficulty ~= "hard" then
		HardOnlyTripod1.Destroy()
		HardOnlyShardLauncher1.Destroy()
		HardOnlyShardLauncher2.Destroy()
		HardOnlyStormColumn1.Destroy()
		HardOnlyStormColumn2.Destroy()

		if Difficulty == "easy" then
			NonEasyStormColumn1.Destroy()
			NonEasyStormColumn2.Destroy()
			NonEasyStormColumn3.Destroy()
			NonEasyCorrupter1.Destroy()
		end

		Trigger.AfterDelay(NavalReinforcementsDelay[Difficulty], function()
			NavalReinforcements()
		end)

		Trigger.AfterDelay(DateTime.Minutes(1), function()
			Tip("Resources in the vicinity are limited. Explore to find additional sources of income.")
		end)
	end

	Trigger.AfterDelay(DateTime.Seconds(15), function()
		InitConvoy()
	end)

	-- When convoy units reach destination, remove them
	Utils.Do(ConvoyExits, function(exitCells)
		Trigger.OnEnteredFootprint(exitCells, function(a, id)
			if a.Owner == England then
				a.Destroy()
			end
		end)
	end)

	-- Easter egg
	Trigger.OnKilled(Church, function(a)
		Media.PlaySound("screams.aud")
		local congregation1 = Reinforcements.Reinforce(Neutral, { "c1" }, { Church.Location, CPos.New(Church.Location.X - 2, Church.Location.Y - 1) })[1]
		local congregation2 = Reinforcements.Reinforce(Neutral, { "c3" }, { Church.Location, CPos.New(Church.Location.X - 2, Church.Location.Y) })[1]
		local congregation3 = Reinforcements.Reinforce(Neutral, { "c4" }, { Church.Location, CPos.New(Church.Location.X - 2, Church.Location.Y + 1) })[1]
		local congregation4 = Reinforcements.Reinforce(Neutral, { "c8" }, { Church.Location, CPos.New(Church.Location.X - 2, Church.Location.Y + 2) })[1]
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.FloatingText("WHY??", congregation2.CenterPosition, 30, HSLColor.Red)
		end)
		congregation1.Scatter()
		congregation3.Scatter()
	end)

	SetupReveals({ EntranceReveal1, EntranceReveal2, EntranceReveal3 })
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Scrin.Resources = Scrin.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
			UpdateConvoyCountdown()
		end

		if Greece.HasNoRequiredUnits() then
			if ObjectiveClearPath ~= nil and not Greece.IsObjectiveCompleted(ObjectiveClearPath) then
				Greece.MarkFailedObjective(ObjectiveClearPath)
			end
			if ObjectiveProtectConvoys ~= nil and not Greece.IsObjectiveCompleted(ObjectiveProtectConvoys) then
				Greece.MarkFailedObjective(ObjectiveProtectConvoys)
			end
			if ObjectiveDestroyScrinBase ~= nil and not Greece.IsObjectiveCompleted(ObjectiveDestroyScrinBase) then
				Greece.MarkFailedObjective(ObjectiveDestroyScrinBase)
			end
		end

		if DateTime.GameTime > DateTime.Minutes(15) and not HasOneOf(Scrin, { "reac", "rea2", "sfac", "proc.scrin", "port", "wsph", "nerv", "grav", "scrt", "srep" }) then
			if ObjectiveDestroyScrinBase ~= nil and not Greece.IsObjectiveCompleted(ObjectiveDestroyScrinBase) then
				Greece.MarkCompletedObjective(ObjectiveDestroyScrinBase)
			end
			if not PathsClear and not HasOneOf(Scrin, { "scol", "ptur" }) then
				PathsClear = true
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

UpdateConvoyCountdown = function()
	if TimerTicks == 0 then
		if Difficulty == "hard" then
			UserInterface.SetMissionText("Protect the convoy. All trucks must survive." , HSLColor.Yellow)
		else
			if TrucksLost == MaxLosses[Difficulty] then
				UserInterface.SetMissionText("Protect the convoy. No more trucks can be lost.", HSLColor.Yellow)
			else
				UserInterface.SetMissionText("Protect the convoy. Acceptable losses: " .. TrucksLost .. " / " ..  MaxLosses[Difficulty] , HSLColor.Yellow)
			end
		end
	else
		UserInterface.SetMissionText("Next convoy arrives in " .. UtilsCA.FormatTimeForGameSpeed(TimerTicks), HSLColor.Yellow)
	end
end

InitConvoy = function()
	local nextConvoy = Convoys[NextConvoyIdx]

	-- Spawn and announce flare
	ConvoyFlare = Actor.Create("flare", true, { Owner = Greece, Location = nextConvoy.FlareWaypoint.Location })
	Media.PlaySpeechNotification(Greece, "SignalFlare")
	Beacon.New(Greece, nextConvoy.FlareWaypoint.CenterPosition)

	if not FirstConvoyAnnounced then
		FirstConvoyAnnounced = true
		Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(3)), function()
			MediaCA.PlaySound("r_firstconvoy.aud", 1.5)
		end)
	end

	-- Set the timer
	if PathsClear then
		TimerTicks = DateTime.Seconds(20)
	else
		TimerTicks = TimeBetweenConvoys[Difficulty][NextConvoyIdx]
	end

	UpdateConvoyCountdown()

	-- Schedule convoy to arrive after timer expires
	Trigger.AfterDelay(TimerTicks, function()
		ConvoyFlare.Destroy()
		UpdateConvoyCountdown()
		Media.PlaySpeechNotification(Greece, "ConvoyApproaching")
		Notification("Convoy approaching.")
		CurrentConvoyArrivalComplete = false

		local trucks = Reinforcements.Reinforce(England, ConvoyUnits, nextConvoy.Spawn, 50, function(truck)
			Utils.Do(nextConvoy.Path, function(waypoint)
				truck.Move(waypoint)
			end)
		end)

		Trigger.AfterDelay(DateTime.Seconds(15), function()
			CurrentConvoyArrivalComplete = true
		end)

		Utils.Do(trucks, function(truck)
			Trigger.OnKilled(truck, function(self, killer)
				TrucksLost = TrucksLost + 1
				TrucksLostCurrentConvoy = TrucksLostCurrentConvoy + 1
				Media.PlaySpeechNotification(Greece, "ConvoyUnitLost")
				Media.PlaySoundNotification(Greece, "AlertBuzzer")

				if TimerTicks == 0 then
					UpdateConvoyCountdown()
				end

				if TrucksLost > MaxLosses[Difficulty] then
					Greece.MarkFailedObjective(ObjectiveClearPath)
					Greece.MarkFailedObjective(ObjectiveProtectConvoys)
				elseif TrucksLostCurrentConvoy == 5 then
					QueueNextConvoy(DateTime.Seconds(45))
				end
			end)

			Trigger.OnRemovedFromWorld(truck, function(a)
				if truck.IsDead then
					return
				end
				if CurrentConvoyArrivalComplete then
					local numTrucks = #England.GetActorsByType("truk")
					if numTrucks == 0 then
						QueueNextConvoy(DateTime.Seconds(15))
					end
				end
			end)

			Trigger.AfterDelay(DateTime.Seconds(180), function()
				if not truck.IsDead and truck.IsInWorld then
					truck.Destroy()
				end
			end)
		end)
	end)
end

QueueNextConvoy = function(timeUntilNext)
	NextConvoyIdx = NextConvoyIdx + 1
	TrucksLostCurrentConvoy = 0
	if NextConvoyIdx <= #Convoys then
		UserInterface.SetMissionText("Awaiting next convoy.")
		Trigger.AfterDelay(timeUntilNext, function()
			InitConvoy()
		end)
	else
		ObjectiveDestroyScrinBase = Greece.AddObjective("Destroy the alien stronghold.")
		Greece.MarkCompletedObjective(ObjectiveClearPath)
		Greece.MarkCompletedObjective(ObjectiveProtectConvoys)
		UserInterface.SetMissionText("Destroy the alien stronghold.", HSLColor.Yellow)
	end
end

InitScrin = function()
	if Difficulty == "easy" then
		RebuildExcludes.Scrin = { Types = { "scol", "ptur" } }
	end

	AutoRepairAndRebuildBuildings(Scrin, 15)
	SetupRefAndSilosCaptureCredits(Scrin)
	AutoReplaceHarvesters(Scrin)

	StormriderAttacker1.Attack(PlayerRefinery)
	StormriderAttacker2.Attack(PlayerRefinery)

	StormriderPatroller1.Patrol({ ScrinAirPatrol1a.Location, ScrinAirPatrol1b.Location, ScrinAirPatrol1c.Location, ScrinAirPatrol1b.Location })
	StormriderPatroller2.Patrol({ ScrinAirPatrol1a.Location, ScrinAirPatrol1b.Location, ScrinAirPatrol1c.Location, ScrinAirPatrol1b.Location })

	StormriderPatroller3.Patrol({ ScrinAirPatrol2a.Location, ScrinAirPatrol2b.Location })
	StormriderPatroller4.Patrol({ ScrinAirPatrol2a.Location, ScrinAirPatrol2b.Location })

	SeekerPatroller1.Patrol({ SeekerPatrol1a.Location, SeekerPatrol1b.Location })
	SeekerPatroller2.Patrol({ SeekerPatrol1a.Location, SeekerPatrol1b.Location })
	SeekerPatroller3.Patrol({ SeekerPatrol1a.Location, SeekerPatrol1b.Location })

	Trigger.AfterDelay(Squads.Main.Delay[Difficulty], function()
		InitAttackSquad(Squads.Main, Scrin)
	end)

	Trigger.AfterDelay(Squads.Stormriders.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.Stormriders, Scrin, Greece, { "dome", "atek", "apwr", "apwr", "apwr", "ptnk", "cryo", "heli", "harr" })
	end)

	if Difficulty ~= "easy" then
		Trigger.AfterDelay(Squads.Devastators.Delay[Difficulty], function()
			InitAirAttackSquad(Squads.Devastators, Scrin, Greece, { "dome", "atek", "apwr", "pris", "fix" })
		end)
	end

	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)

	local stormriders = Scrin.GetActorsByType("stmr")
	Utils.Do(stormriders, function(a)
		Trigger.OnDamaged(a, function(self, attacker, damage)
			if not self.IsDead and self.AmmoCount() == 0 then
				Trigger.ClearAll(self)
				self.Stop()
				self.ReturnToBase()
				Trigger.AfterDelay(DateTime.Seconds(1), function()
					InitAttackAircraft(self, Greece, { "dome", "atek", "apwr", "apwr", "apwr", "ptnk", "cryo", "heli", "harr" })
				end)
			end
		end)
	end)
end

NavalReinforcements = function()
	if not NavalReinforcementsArrived then
		NavalReinforcementsArrived = true
		Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
		Beacon.New(Greece, DestroyerSpawn1.CenterPosition)
		Reinforcements.Reinforce(Greece, { "dd" }, { DestroyerSpawn1.Location, DestroyerRally1.Location })
		Reinforcements.Reinforce(Greece, { "dd" }, { DestroyerSpawn2.Location, DestroyerRally2.Location })
	end
end
