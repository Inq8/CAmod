InitialUnits = {
	easy = { "mcv", "2tnk", "jeep" },
	normal = { "mcv", "jeep" },
	hard = { "mcv" }
}

NodSouthAttackPaths = {
	{ LeftAttack1.Location, LeftAttack2.Location, LeftAttack3.Location },
	{ MiddleAttack1.Location, MiddleAttack2.Location, MiddleAttack3.Location, RightAttack3.Location },
	{ MiddleAttack1.Location, MiddleAttack2.Location, MiddleAttack3.Location, LeftAttack3.Location }
}

NodEastAttackPaths = {
	{ RightAttack1.Location, RightAttack2.Location, RightAttack3.Location },
	{ RightAttack1.Location, MiddleAttack2.Location, MiddleAttack3.Location, RightAttack3.Location },
	{ RightAttack1.Location, MiddleAttack2.Location, MiddleAttack3.Location, LeftAttack3.Location }
}

NodNavalAttackPath = {
	{ NodNavalAttack1.Location, NodNavalAttack2.Location, NodNavalAttack3.Location, NodNavalAttack4.Location, NodNavalAttack5.Location, NodNavalAttack6.Location }
}

Patrols = {
	{
		Units = { LTPatroller1, LTPatroller2 },
		Path = { LTPatrol1.Location, LTPatrol2.Location, LTPatrol3.Location, LTPatrol4.Location, LTPatrol5.Location, LTPatrol6.Location, LTPatrol7.Location, LTPatrol8.Location, LTPatrol9.Location, LTPatrol10.Location, LTPatrol11.Location, LTPatrol12.Location, LTPatrol13.Location }
	},
	{
		Units = { EastPatroller1, EastPatroller2, EastPatroller3, EastPatroller4, EastPatroller5, EastPatroller6, EastPatroller7 },
		Path = { EastPatrol1.Location, EastPatrol2.Location, EastPatrol3.Location, EastPatrol2.Location, EastPatrol4.Location, EastPatrol2.Location }
	},
	{
		Units = { SouthPatroller1, SouthPatroller2, SouthPatroller3, SouthPatroller4 },
		Path = { SouthPatrol1.Location, SouthPatrol2.Location }
	}
}

-- Squads

BasicUnits = {
	easy = {
		{ Infantry = {}, Vehicles = { "bike", "bike" } },
		{ Infantry = {}, Vehicles = { "bggy", "bike" } },
		{ Infantry = { "n3", "n1", "n1" }, Vehicles = { "bggy", "bggy" } },
	},
	normal = {
		{ Infantry = {}, Vehicles = { "bggy", "bike", "bike" } },
		{ Infantry = { "n3", "n1", "n1", "n4" }, Vehicles = { "bggy", "bggy", "bike" } },
		{ Infantry = { "n3", "n1", "n1", "n4" }, Vehicles = { "ltnk" } },
	},
	hard = {
		{ Infantry = {}, Vehicles = { "bike", "bike", "bike", "bike" } },
		{ Infantry = { "n3", "n1", "n1", "n1", "n4" }, Vehicles = { "bggy", "bggy", "bike", "bike" } },
		{ Infantry = { "n3", "n1", "n1", "n4" }, Vehicles = { "ltnk", "bggy", "bike" } },
	}
}

AdvancedUnits = {
	easy = {
		{ Infantry = {}, Vehicles = { "bike", "bike", "bike" } },
		{ Infantry = { "n3", "n1", "n1", "n1" }, Vehicles = { "ltnk", "ltnk" } },
		{ Infantry = { "n3", "n1", "n4", "n1" }, Vehicles = { "ftnk", "ftnk" } },
		{ Infantry = { "n1c", "n1c", "n3c" }, Vehicles = { "ltnk" } },
	},
	normal = {
		{ Infantry = {}, Vehicles = { "bggy", "bike", "bike", "stnk.nod" } },
		{ Infantry = { "n3", "n1", "n1", "n4", "n1" }, Vehicles = { "ltnk", "ltnk" } },
		{ Infantry = { "n3", "n1", "n1", "n4", "n1" }, Vehicles = { "ltnk", "arty.nod" } },
		{ Infantry = { "acol.upg", "acol.upg", "n1c", "n3c", "n5" }, Vehicles = { "ftnk", "ltnk" } },
	},
	hard = {
		{ Infantry = {}, Vehicles = { "stnk.nod", "stnk.nod", "stnk.nod", "sapc.ai" } },
		{ Infantry = { "n3", "n1", "n1", "n1", "n1", "n4", "n3" }, Vehicles = { "ltnk", "ltnk", "ftnk", "arty.nod" } },
		{ Infantry = { "n3", "n1", "n1", "n1", "n4", "n1", "n3" }, Vehicles = { "ltnk", "mlrs", "arty.nod", "howi" } },
		{ Infantry = { "acol.upg", "acol.upg", "rmbc", "n1c", "n3c", "n5", "n1c", "n1c" }, Vehicles = { "ftnk", "ltnk" } },
	}
}

LabDefenseUnits = {
	easy = {
		{ Infantry = { "n1c", "n1c", "n1c", "n3c" }, Vehicles = { "ltnk" } },
	},
	normal = {
		{ Infantry = { "n1c", "n1c", "n1c", "n3c", "n5", "n1c", "acol.upg" }, Vehicles = { "ltnk", "mlrs", "bike", "bike" } },
	},
	hard = {
		{ Infantry = { "n1c", "n1c", "n1c", "n3c", "n5", "n1c", "acol.upg", "acol.upg", "rmbc" }, Vehicles = { "ltnk", "mlrs", "stnk.nod", "hftk", "ltnk" } },
	}
}

Squads = {
	SouthBasic = {
		Player = nil,
		Delay = {
			easy = DateTime.Minutes(8),
			normal = DateTime.Minutes(6),
			hard = DateTime.Minutes(4)
		},
		Interval = {
			easy = DateTime.Seconds(50),
			normal = DateTime.Seconds(35),
			hard = DateTime.Seconds(20)
		},
		DispatchDelay = DateTime.Seconds(15),
		QueueProductionStatuses = { Infantry = false, Vehicles = false },
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { NodSouthHand }, Vehicles = { NodSouthAirstrip } },
		Units = BasicUnits,
		AttackPaths = NodSouthAttackPaths,
		TransitionTo = {
			SquadType = "SouthAdvanced",
			GameTime = {
				easy = DateTime.Minutes(15),
				normal = DateTime.Minutes(13),
				hard = DateTime.Minutes(11)
			}
		}
	},
	EastBasic = {
		Player = nil,
		Delay = {
			easy = DateTime.Minutes(5),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(3)
		},
		Interval = {
			easy = DateTime.Seconds(70),
			normal = DateTime.Seconds(50),
			hard = DateTime.Seconds(30)
		},
		DispatchDelay = DateTime.Seconds(15),
		QueueProductionStatuses = { Infantry = false, Vehicles = false },
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { NodEastHand1, NodEastHand2 }, Vehicles = { NodEastAirstrip } },
		Units = BasicUnits,
		AttackPaths = NodEastAttackPaths,
		TransitionTo = {
			SquadType = "EastAdvanced",
			GameTime = {
				easy = DateTime.Minutes(14),
				normal = DateTime.Minutes(12),
				hard = DateTime.Minutes(10)
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
		DispatchDelay = DateTime.Seconds(15),
		QueueProductionStatuses = { Infantry = false, Vehicles = false },
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { NodSouthHand }, Vehicles = { NodSouthAirstrip } },
		Units = AdvancedUnits,
		AttackPaths = NodSouthAttackPaths
	},
	EastAdvanced = {
		Player = nil,
		Interval = {
			easy = DateTime.Seconds(60),
			normal = DateTime.Seconds(40),
			hard = DateTime.Seconds(20)
		},
		DispatchDelay = DateTime.Seconds(15),
		QueueProductionStatuses = { Infantry = false, Vehicles = false },
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { NodEastHand1, NodEastHand2 }, Vehicles = { NodEastAirstrip } },
		Units = AdvancedUnits,
		AttackPaths = NodEastAttackPaths,
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
			hard = DateTime.Seconds(150)
		},
		QueueProductionStatuses = {
			Aircraft = false
		},
		IdleUnits = { },
		ProducerActors = nil,
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
	Naval = {
		Player = nil,
		IsNaval = true,
		ActiveCondition = function()
			return PlayerHasNavalProduction(Greece)
		end,
		Interval = {
			easy = DateTime.Seconds(75),
			normal = DateTime.Seconds(60),
			hard = DateTime.Seconds(45)
		},
		QueueProductionStatuses = {
			Ships = false
		},
		IdleUnits = { },
		ProducerTypes = { Ships = { "spen.nod" } },
		Units = {
			easy = {
				{ Ships = { "sb", "ss2" } }
			},
			normal = {
				{ Ships = { "sb", "ss2", "sb" } }
			},
			hard = {
				{ Ships = { "sb", "sb", "ss2", "ss2" } }
			}
		},
		AttackPaths = NodNavalAttackPath
	},
	LabDefense = {
		Player = nil,
		ActiveCondition = function()
			return CountConyards(Nod) < 2
		end,
		Delay = {
			easy = DateTime.Minutes(20),
			normal = DateTime.Minutes(15),
			hard = DateTime.Minutes(10)
		},
		Interval = {
			easy = DateTime.Seconds(30),
			normal = DateTime.Seconds(20),
			hard = DateTime.Seconds(10)
		},
		DispatchDelay = DateTime.Seconds(15),
		QueueProductionStatuses = { Infantry = false, Vehicles = false },
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { NodQuarryHand }, Vehicles = { NodQuarryAirstrip } },
		ProducerTypes = { Infantry = { "hand" }, Vehicles = { "airs" } },
		Units = LabDefenseUnits,
		AttackPaths = NodSouthAttackPaths
	}
}

-- Setup and Tick

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	Nod = Player.GetPlayer("Nod")
	MissionPlayer = Greece
	TimerTicks = 0

	InitObjectives(Greece)
	InitNod()
	Camera.Position = McvLanding.CenterPosition

	DoMcvArrival()

	Utils.Do(Patrols, function(p)
		Utils.Do(p.Units, function(unit)
			if not unit.IsDead then
				unit.Patrol(p.Path, true)
			end
		end)
	end)

	Trigger.OnKilled(Church1, function(self, killer)
		Actor.Create("moneycrate", true, { Owner = Greece, Location = Church1.Location })
	end)

	Trigger.OnKilled(Church2, function(self, killer)
		Actor.Create("moneycrate", true, { Owner = Greece, Location = Church2.Location })
	end)

	ObjectiveFindLab = Greece.AddObjective("Locate the Nod research lab.")

	-- On proximity to lab, reveal it and update objectives.
	Trigger.OnEnteredProximityTrigger(ResearchLab.CenterPosition, WDist.New(8 * 1024), function(a, id)
		if a.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)
			Beacon.New(Greece, ResearchLab.CenterPosition)
			ObjectiveCaptureLab = Greece.AddObjective("Capture the Nod research lab.")

			if ObjectiveFindLab ~= nil and not Greece.IsObjectiveCompleted(ObjectiveFindLab) then
				Greece.MarkCompletedObjective(ObjectiveFindLab)
			end

			UserInterface.SetMissionText("Capture the Nod research lab.", HSLColor.Yellow)
			LabCamera = Actor.Create("camera.paradrop", true, { Owner = Greece, Location = ResearchLab.Location })

			Trigger.AfterDelay(DateTime.Seconds(5), function()
				LabCamera.Destroy()
			end)
		end
	end)

	Trigger.OnCapture(ResearchLab, function(self, captor, oldOwner, newOwner)
		if newOwner == Greece then
			Greece.MarkCompletedObjective(ObjectiveCaptureLab)
		end
	end)

	Trigger.OnKilled(ResearchLab, function(self, killer)
		Greece.MarkFailedObjective(ObjectiveCaptureLab)
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Nod.Cash = 7500
		Nod.Resources = 7500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
		end

		if DateTime.GameTime > DateTime.Seconds(10) then
			if Greece.HasNoRequiredUnits() then
				if ObjectiveFindLab ~= nil and not Greece.IsObjectiveCompleted(ObjectiveFindLab) then
					Greece.MarkFailedObjective(ObjectiveFindLab)
				end
				if ObjectiveCaptureLab ~= nil and not Greece.IsObjectiveCompleted(ObjectiveCaptureLab) then
					Greece.MarkFailedObjective(ObjectiveCaptureLab)
				end
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocation()
	end
end

InitNod = function()
	AutoRepairAndRebuildBuildings(Nod, 20)
	SetupRefAndSilosCaptureCredits(Nod)
	AutoReplaceHarvesters(Nod)

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, Nod, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)

	Trigger.AfterDelay(Squads.SouthBasic.Delay[Difficulty], function()
		InitAttackSquad(Squads.SouthBasic, Nod)
	end)

	Trigger.AfterDelay(Squads.EastBasic.Delay[Difficulty], function()
		InitAttackSquad(Squads.EastBasic, Nod)
	end)

	Trigger.AfterDelay(Squads.Air.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.Air, Nod, Greece, { "harv", "harv.td", "pris", "ifv", "cryo", "ptnk", "pcan", "ca", "dome", "apwr", "mech", "medi" })
	end)

	InitAttackSquad(Squads.Naval, Nod)
	InitAttackSquad(Squads.LabDefense, Nod)

	local upgradeCreationLocation = CPos.New(0, 0)

	Actor.Create("hazmat.upgrade", true, { Owner = Nod, Location = upgradeCreationLocation })

	if Difficulty == "hard" then
		Actor.Create("cyborgspeed.upgrade", true, { Owner = Nod, Location = upgradeCreationLocation })
		Actor.Create("cyborgarmor.upgrade", true, { Owner = Nod, Location = upgradeCreationLocation })

		Trigger.AfterDelay(DateTime.Minutes(15), function()
			Actor.Create("tibcore.upgrade", true, { Owner = Nod, Location = upgradeCreationLocation })
		end)
	end
end

DoMcvArrival = function()
	local mcvArrivalPath = { McvEntry.Location, McvLanding.Location }
	local mcvExitPath = { McvEntry.Location }
	DoNavalTransportDrop(Greece, mcvArrivalPath, mcvExitPath, "lst", InitialUnits[Difficulty], function(a)
		a.Move(McvRally.Location)
	end)
end

IsNodGroundHunterUnit = function(actor)
	return actor.Owner == Nod and actor.HasProperty("Move") and not actor.HasProperty("Land") and actor.HasProperty("Hunt") and actor.Type ~= "mlrs" and actor.Type ~= "arty.nod"
end
