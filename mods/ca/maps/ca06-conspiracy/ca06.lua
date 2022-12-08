SleeperAwakenTrigger = { LeftBaseTrigger1.Location, LeftBaseTrigger2.Location, LeftBaseTrigger3.Location, LeftBaseTrigger4.Location, LeftBaseTrigger5.Location, LeftBaseTrigger6.Location, LeftBaseTrigger7.Location, RightBaseTrigger1.Location, RightBaseTrigger2.Location, RightBaseTrigger3.Location, RightBaseTrigger4.Location, RightBaseTrigger5.Location, RightBaseTrigger6.Location, RightBaseTrigger7.Location }

Researchers = { Researcher1, Researcher2 }

GDIDefenders = { GDIDefender1, GDIDefender2, GDIDefender3, GDIDefender4, GDIDefender5, GDIDefender6 }

ChinookDropPaths = {
	{ ChinookDrop1Spawn.Location, ChinookDrop1Landing.Location },
	{ ChinookDrop2Spawn.Location, ChinookDrop2Landing.Location },
	{ ChinookDrop3Spawn.Location, ChinookDrop3Landing.Location },
	{ ChinookDrop4Spawn.Location, ChinookDrop4Landing.Location }
}

CarryallDropPaths = {
	{ GDIDropSpawn.Location, GDIDrop1.Location },
	{ GDIDropSpawn.Location, GDIDrop2.Location },
	{ GDIDropSpawn.Location, GDIDrop3.Location },
}

GDIReinforcementPath = { GDIReinforceSpawn.Location, GDIReinforceRally.Location }

GreeceSouthAttackPaths = {
	{ SouthAttack1.Location, SouthAttack2.Location, SouthWestAttack1.Location },
	{ SouthAttack1.Location, SouthAttack2.Location, SouthCentralAttack1.Location },
	{ SouthAttack1.Location, SouthEastAttack1.Location },
	{ EastAttack1.Location, EastAttack2.Location }
}

GreeceNorthAttackPaths = {
	{ NorthAttack1.Location, NorthWestAttack1.Location, NorthWestAttack2.Location },
	{ NorthAttack1.Location, NorthEastAttack1.Location }
}

Patrols = {
	{
		Units = { AlliedPatroller1, AlliedPatroller2 },
		Path = { AlliedPatrol1.Location, AlliedPatrol2.Location }
	},
}

-- Squads

BasicUnits = {
	easy = {
		{ Infantry = {}, Vehicles = { "1tnk", "jeep" } },
		{ Infantry = {}, Vehicles = { "2tnk", "ifv.ai" } },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "2tnk", "jeep" } },
	},
	normal = {
		{ Infantry = {}, Vehicles = { "apc.ai", "1tnk", "jeep"  } },
		{ Infantry = {}, Vehicles = { "2tnk", "ifv.ai", "1tnk" } },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "2tnk", "ifv.ai", "apc.ai" } },
	},
	hard = {
		{ Infantry = {}, Vehicles = { "rapc.ai", "1tnk", "1tnk", "jeep" } },
		{ Infantry = { "e3", "e1", "e1" }, Vehicles = { "2tnk", "2tnk", "arty", "ifv.ai", "rapc.ai"  } },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1" }, Vehicles = { "2tnk", "ifv.ai", "rapc.ai" } },
	}
}

AdvancedUnits = {
	easy = {
		{ Infantry = {}, Vehicles = { "1tnk", "1tnk", "jeep", "jeep" } },
		{ Infantry = { "e3", "e1", "e1" }, Vehicles = { "2tnk", "ifv.ai", "arty" } },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "2tnk", "2tnk", "jeep" } },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "2tnk", "rapc", "ptnk" } },
		{ Infantry = { "seal", "seal", "seal" }, Vehicles = { "ifv.ai", "ifv.ai" } },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "batf.ai" } },
	},
	normal = {
		{ Infantry = {}, Vehicles = { "rapc.ai", "1tnk", "1tnk", "jeep"  } },
		{ Infantry = {}, Vehicles = { "2tnk", "ifv.ai", "1tnk", "2tnk" } },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "e3" }, Vehicles = { "2tnk", "ifv.ai", "apc.ai", "rapc.ai" } },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "e3" }, Vehicles = { "2tnk", "ifv.ai", "apc.ai", "ptnk" } },
		{ Infantry = { "seal", "seal", "seal", "seal" }, Vehicles = { "ifv.ai", "ifv.ai", "ifv.ai" } },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "batf.ai", "ifv.ai", "ifv.ai" } },
	},
	hard = {
		{ Infantry = {}, Vehicles = { "rapc.ai", "1tnk", "1tnk", "jeep", "cryo" } },
		{ Infantry = {}, Vehicles = { "2tnk", "2tnk", "ifv.ai", "rapc.ai", "pcan"  } },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "snip", "e1", "e3" }, Vehicles = { "2tnk", "ifv.ai", "rapc.ai", "rapc.ai", "arty" } },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "snip", "e1", "e3" }, Vehicles = { "ctnk", "ctnk", "ctnk", "ptnk", "ifv.ai" } },
		{ Infantry = { "seal", "seal", "seal", "seal", "seal" }, Vehicles = { "ifv.ai", "ifv.ai", "ifv.ai", "ifv.ai" } },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "batf.ai", "batf.ai", "ifv.ai", "ifv.ai" } },
	}
}

Squads = {
	SouthBasic = {
		Player = nil,
		Delay = {
			easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(2)
		},
		Interval = {
			easy = DateTime.Seconds(50),
			normal = DateTime.Seconds(35),
			hard = DateTime.Seconds(20)
		},
		QueueProductionStatuses = { Infantry = false, Vehicles = false },
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { AlliedSouthBarracks }, Vehicles = { AlliedSouthFactory } },
		Units = BasicUnits,
		AttackPaths = GreeceSouthAttackPaths,
		TransitionTo = {
			SquadType = "SouthAdvanced",
			GameTime = {
				easy = DateTime.Minutes(18),
				normal = DateTime.Minutes(16),
				hard = DateTime.Minutes(14)
			}
		}
	},
	NorthBasic = {
		Player = nil,
		Delay = {
			easy = DateTime.Minutes(3),
			normal = DateTime.Minutes(2),
			hard = DateTime.Minutes(1)
		},
		Interval = {
			easy = DateTime.Seconds(55),
			normal = DateTime.Seconds(45),
			hard = DateTime.Seconds(35)
		},
		QueueProductionStatuses = { Infantry = false, Vehicles = false },
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { AlliedNorthBarracks }, Vehicles = { AlliedNorthFactory } },
		Units = BasicUnits,
		AttackPaths = GreeceNorthAttackPaths,
		TransitionTo = {
			SquadType = "NorthAdvanced",
			GameTime = {
				easy = DateTime.Minutes(17),
				normal = DateTime.Minutes(15),
				hard = DateTime.Minutes(13)
			}
		}
	},
	SouthAdvanced = {
		Player = nil,
		Interval = {
			easy = DateTime.Seconds(60),
			normal = DateTime.Seconds(40),
			hard = DateTime.Seconds(20)
		},
		QueueProductionStatuses = { Infantry = false, Vehicles = false },
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { AlliedSouthBarracks }, Vehicles = { AlliedSouthFactory } },
		Units = AdvancedUnits,
		AttackPaths = GreeceSouthAttackPaths
	},
	NorthAdvanced = {
		Player = nil,
		Interval = {
			easy = DateTime.Seconds(60),
			normal = DateTime.Seconds(40),
			hard = DateTime.Seconds(20)
		},
		QueueProductionStatuses = { Infantry = false, Vehicles = false },
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { AlliedNorthBarracks }, Vehicles = { AlliedNorthFactory } },
		Units = AdvancedUnits,
		AttackPaths = GreeceNorthAttackPaths,
	},
	Air = {
		Player = nil,
		Delay = {
			easy = DateTime.Minutes(13),
			normal = DateTime.Minutes(12),
			hard = DateTime.Minutes(11)
		},
		Interval = {
			easy = DateTime.Minutes(3),
			normal = DateTime.Seconds(150),
			hard = DateTime.Minutes(2)
		},
		QueueProductionStatuses = {
			Aircraft = false
		},
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Aircraft = { "hpad" } },
		Units = {
			easy = {
				{ Aircraft = { "heli" } }
			},
			normal = {
				{ Aircraft = { "heli", "heli" } },
				{ Aircraft = { "harr" } }
			},
			hard = {
				{ Aircraft = { "heli", "heli", "heli" } },
				{ Aircraft = { "harr", "harr" } }
			}
		},
	}
}

ChinookDropStart = {
	easy = DateTime.Minutes(12),
	normal = DateTime.Minutes(10),
	hard = DateTime.Minutes(8)
}

ChinookDropInterval = {
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(2),
	hard = DateTime.Minutes(1)
}

GDIReinforcementDelay = {
	easy = DateTime.Minutes(17),
	normal = DateTime.Minutes(14),
	hard = DateTime.Minutes(11)
}

-- Setup and Tick

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	Greece = Player.GetPlayer("Greece")
	GDI = Player.GetPlayer("GDI")
	Legion = Player.GetPlayer("Legion")
	EvacPlayer = Player.GetPlayer("Evac")
	MissionPlayer = Nod
	TimerTicks = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Nod)
	InitGDI()
	InitGreece()

	ObjectiveTakeOverBase = Nod.AddObjective("Take control of the GDI base due south.")
	UserInterface.SetMissionText("Take control of the GDI base due south.", HSLColor.Yellow)

	local startingUnits = Nod.GetActors()
	Utils.Do(startingUnits, function(a)
		if a.HasProperty("Kill") then
			Trigger.OnKilled(a, function(self, killer)
				local livingStartingUnits = Utils.Where(startingUnits, function(u)
					return u.HasProperty("Kill") and not u.IsDead
				end)
				if #livingStartingUnits == 0 and not Nod.IsObjectiveCompleted(ObjectiveTakeOverBase) then
					Nod.MarkFailedObjective(ObjectiveTakeOverBase)
				end
			end)
		end
	end)

	if Difficulty ~= "hard" then
		GDIDefender2.Destroy()
		if Difficulty == "easy" then
			GDIDefender1.Destroy()
		end

		Trigger.AfterDelay(DateTime.Seconds(3), function()
			Tip("Stealth units can be detected by enemy defenses, as well as infantry at close range.")
		end)
	end

	Utils.Do(Patrols, function(p)
		Utils.Do(p.Units, function(unit)
			if not unit.IsDead then
				unit.Patrol(p.Path, true)
			end
		end)
	end)

	-- On player reaching GDI base
	Trigger.OnEnteredFootprint(SleeperAwakenTrigger, function(a, id)
		if a.Owner == Nod then
			Trigger.RemoveFootprintTrigger(id)
			AwakenSleeperCell()
		end
	end)

	-- If player tries to go through the Allied base before going to GDI base, begin attacks after a delay
	Trigger.OnEnteredProximityTrigger(BaseTriggerBackup.CenterPosition, WDist.New(10 * 1024), function(a, id)
		if a.Owner == Nod then
			Trigger.RemoveProximityTrigger(id)
			Trigger.AfterDelay(DateTime.Minutes(2), function()
				InitAlliedAttacks()
			end)
		end
	end)

	-- If player enters vicinity of south east base with a ground unit after GDI base has been taken over, add free power
	Trigger.OnEnteredProximityTrigger(SouthEastBaseCenter.CenterPosition, WDist.New(12 * 1024), function(a, id)
		if a.Owner == Nod and not a.HasProperty("Land") and ObjectiveTakeOverBase ~= nil and Nod.IsObjectiveCompleted(ObjectiveTakeOverBase) then
			Trigger.RemoveProximityTrigger(id)
			Actor.Create("powercheat.minor", true, { Owner = Greece, Location = SouthEastBaseCenter.Location })
		end
	end)

	-- Finding researchers
	Utils.Do(Researchers, function(researcher)
		Trigger.OnEnteredProximityTrigger(researcher.CenterPosition, WDist.New(8 * 1024), function(a, id)
			if a.Owner == Nod then
				Trigger.RemoveProximityTrigger(id)
				Notification("Researcher located.")
				if ObjectiveRescueResearchers == nil then
					ObjectiveRescueResearchers = Nod.AddObjective("Locate and rescue Nod researchers.")
				end
				local researcherCamera = Actor.Create("camera.paradrop", true, { Owner = Nod, Location = researcher.Location })
				Trigger.AfterDelay(DateTime.Seconds(10), function()
					researcherCamera.Destroy()
				end)
			end
		end)

		Trigger.OnEnteredProximityTrigger(researcher.CenterPosition, WDist.New(2 * 1024), function(a, id)
			if a.Owner == Nod and a.HasProperty("Move") and not a.HasProperty("Land") then
				Trigger.RemoveProximityTrigger(id)
				researcher.Owner = Nod
				Notification("Escort researcher to evacuation point.")
				InitEvacSite()
			end
		end)

		Trigger.OnKilled(researcher, function(self, killer)
			if ObjectiveRescueResearchers ~= nil and not Nod.IsObjectiveCompleted(ObjectiveRescueResearchers) then
				Nod.MarkFailedObjective(ObjectiveRescueResearchers)
			end
		end)
	end)

	Trigger.OnEnteredProximityTrigger(EvacPoint.CenterPosition, WDist.New(6 * 1024), function(a, id)
		if a.Owner == Nod and (a == Researcher1 or a == Researcher2) then
			a.Owner = EvacPlayer

			if a == Researcher1 then
				a.Move(CPos.New(EvacPoint.Location.X - 1, EvacPoint.Location.Y + 1))
			else
				a.Move(CPos.New(EvacPoint.Location.X + 1, EvacPoint.Location.Y + 1))
			end

			Trigger.AfterDelay(DateTime.Seconds(2), function()
				if Researcher1.Owner == EvacPlayer and Researcher2.Owner == EvacPlayer and not EvacStarted then
					EvacStarted = true

					if EvacFlare ~= nil then
						EvacFlare.Destroy()
					end

					Reinforcements.ReinforceWithTransport(EvacPlayer, "tran", nil, { EvacSpawn.Location, EvacPoint.Location }, nil, function(transport, cargo)
						Notification("Evacuation transport inbound.")
						Trigger.AfterDelay(DateTime.Seconds(1), function()
							if not Researcher1.IsDead then
								Researcher1.EnterTransport(transport)
							end
							if not Researcher1.IsDead then
								Researcher2.EnterTransport(transport)
							end
						end)
						Trigger.OnPassengerEntered(transport, function(t, passenger)
							if t.PassengerCount == 2 then
								Media.PlaySpeechNotification(Nod, "TargetRescued")
								t.Move(EvacSpawn.Location)
								t.Destroy()
								Trigger.AfterDelay(DateTime.Seconds(4), function()
									Nod.MarkCompletedObjective(ObjectiveRescueResearchers)
								end)
							end
						end)
					end)
				else
					Notification("A researcher has reached the evacuation point. Waiting for the second.")
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
		Greece.Cash = 7500
		Greece.Resources = 7500
		GDI.Cash = 7500
		GDI.Resources = 7500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
		end

		if Nod.HasNoRequiredUnits() then
			if ObjectiveRescueResearchers ~= nil and not Nod.IsObjectiveCompleted(ObjectiveRescueResearchers) then
				Nod.MarkFailedObjective(ObjectiveRescueResearchers)
			end
		end

		if Researcher1.IsDead or Researcher2.IsDead then
			if ObjectiveRescueResearchers ~= nil and not Nod.IsObjectiveCompleted(ObjectiveRescueResearchers) then
				Nod.MarkFailedObjective(ObjectiveRescueResearchers)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocation()
	end
end

AwakenSleeperCell = function()
	if not SleeperCellAwakened then
		SleeperCellAwakened = true
		UserInterface.SetMissionText("")
		local legionForces = Legion.GetActors()

		Utils.Do(legionForces, function(self)
			if self.Type ~= "player" then
				self.Owner = Nod
			end
		end)

		if not GDIBarracks.IsDead then
			GDIBarracks.Kill()
		end

		if not GDICommsCenter.IsDead then
			GDICommsCenter.Kill()
		end

		Trigger.AfterDelay(DateTime.Seconds(60), function()
			Utils.Do(GDIDefenders, function(self)
				if not self.IsDead then
					self.AttackMove(GDIBaseCenter.Location)
					CallForHelp(self, WDist.New(10240), IsGDIGroundHunterUnit)
				end
			end)
		end)

		if not GDIHarvester.IsDead then
			GDIHarvester.Owner = Nod
		end

		Trigger.AfterDelay(1, function()
			Actor.Create("QueueUpdaterDummy", true, { Owner = Nod, Location = GDIBaseCenter.Location })
		end)

		if ObjectiveRescueResearchers == nil then
			ObjectiveRescueResearchers = Nod.AddObjective("Locate and rescue Nod researchers.")
		end

		if ObjectiveTakeOverBase ~= nil and not Nod.IsObjectiveCompleted(ObjectiveTakeOverBase) then
			Nod.MarkCompletedObjective(ObjectiveTakeOverBase)
		end

		-- Initialise Allied attacks
		InitAlliedAttacks()
	end
end

InitEvacSite = function()
	if not EvacSiteInitialized then
		EvacSiteInitialized = true
		Beacon.New(Nod, EvacPoint.CenterPosition)
		EvacFlare = Actor.Create("flare", true, { Owner = Nod, Location = EvacPoint.Location })
	end
end

InitGDI = function()
	AutoRepairAndRebuildBuildings(GDI, 15)
	local gdiGroundAttackers = GDI.GetGroundAttackers()

	Utils.Do(gdiGroundAttackers, function(a)
		TargetSwapChance(a, GDI, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGDIGroundHunterUnit)
	end)
end

InitGreece = function()
	AutoRepairAndRebuildBuildings(Greece, 15)
	SetupRefAndSilosCaptureCredits(Greece)
	AutoReplaceHarvesters(Greece)

	local greeceGroundAttackers = Greece.GetGroundAttackers()

	Utils.Do(greeceGroundAttackers, function(a)
		TargetSwapChance(a, Greece, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGreeceGroundHunterUnit)
	end)

	local upgradeCreationLocation = CPos.New(0, 0)

	Actor.Create("hazmat.upgrade", true, { Owner = Greece, Location = upgradeCreationLocation })
	Actor.Create("apb.upgrade", true, { Owner = Greece, Location = upgradeCreationLocation })

	if Difficulty == "hard" then
		Actor.Create("cryr.upgrade", true, { Owner = Greece, Location = upgradeCreationLocation })
	end
end

InitAlliedAttacks = function()
	if not AlliedAttacksInitialized then
		AlliedAttacksInitialized = true

		Trigger.AfterDelay(Squads.SouthBasic.Delay[Difficulty], function()
			InitAttackSquad(Squads.SouthBasic, Greece)
		end)

		Trigger.AfterDelay(Squads.NorthBasic.Delay[Difficulty], function()
			InitAttackSquad(Squads.NorthBasic, Greece)
		end)

		Trigger.AfterDelay(Squads.Air.Delay[Difficulty], function()
			InitAirAttackSquad(Squads.Air, Greece, Nod, { "harv", "harv.td", "arty.nod", "mlrs", "obli", "atwr", "gtwr", "gun.nod", "hq", "nuk2" })
		end)

		Trigger.AfterDelay(ChinookDropStart[Difficulty], function()
			DoChinookDrop()
		end)

		Trigger.AfterDelay(GDIReinforcementDelay[Difficulty], function()
			local gdiReinforcements = { "mtnk", "mtnk" }
			if Difficulty == "hard" then
				gdiReinforcements = { "htnk" , "htnk" }
			end

			Reinforcements.Reinforce(GDI, gdiReinforcements, GDIReinforcementPath, 75, function(a)
				AssaultPlayerBaseOrHunt(a)
			end)
		end)
	end
end

IsGDIGroundHunterUnit = function(actor)
	return actor.Owner == GDI and actor.HasProperty("Move") and not actor.HasProperty("Land") and actor.HasProperty("Hunt")
end

IsGreeceGroundHunterUnit = function(actor)
	return actor.Owner == Greece and actor.HasProperty("Move") and not actor.HasProperty("Land") and actor.HasProperty("Hunt") and actor.Type ~= "arty" and actor.Type ~= "cryo" and actor.Type ~= "mgg" and actor.Type ~= "mrj"
end

DoChinookDrop = function()
	local entryPath
	entryPath = Utils.Random(ChinookDropPaths)
	local chinookDropUnits = { "e1", "e1", "e1", "e3", "e3" }

	if Difficulty == "hard" and DateTime.GameTime > DateTime.Minutes(16) then
		chinookDropUnits = { "e1", "e1", "e1", "e1", "e1", "e3", "seal", "seal", "seal" }
	end

	DoHelicopterDrop(Greece, entryPath, "tran.paradrop", chinookDropUnits, AssaultPlayerBaseOrHunt, function(t)
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			if not t.IsDead then
				t.Move(entryPath[1])
				t.Destroy()
			end
		end)
	end)

	Trigger.AfterDelay(ChinookDropInterval[Difficulty], DoChinookDrop)
end
