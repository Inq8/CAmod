InitialUnits = {
	easy = { "jeep", "mcv", "2tnk", "e1", "e1", "e1", "e3" },
	normal = { "jeep", "mcv", "e1", "e1", "e1", "e3"  },
	hard = { "jeep", "mcv", "e1", "e1", "e3" }
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

SuperweaponsEnabledTime = {
	easy = DateTime.Minutes(30),
	normal = DateTime.Minutes(20),
	hard = DateTime.Minutes(10)
}

-- Squads

LabDefenseUnits = {
	easy = {
		{ Infantry = { "n1c", "n1c", "n1c", "n3c" }, Vehicles = { "ltnk" } },
	},
	normal = {
		{ Infantry = { "n1c", "n1c", "n1c", "n3c", "n5", "n1c", "acol" }, Vehicles = { "ltnk", "bggy", "bike", "bike" } },
	},
	hard = {
		{ Infantry = { "n1c", "n1c", "n1c", "n3c", "n5", "n1c", "tplr", "tplr", "rmbc" }, Vehicles = { "ltnk", "mlrs", "stnk.nod", "hftk", "ltnk" } },
	}
}

AdjustedNodCompositions = AdjustCompositionsForDifficulty(UnitCompositions.Nod)

Squads = {
	South = {
		Delay = {
			easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(2)
		},
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 25 },
			normal = { Min = 25, Max = 50 },
			hard = { Min = 40, Max = 80 },
		},
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		ProducerActors = { Infantry = { NodSouthHand }, Vehicles = { NodSouthAirstrip } },
		Units = AdjustedNodCompositions,
		AttackPaths = NodSouthAttackPaths,
	},
	East = {
		Delay = {
			easy = DateTime.Minutes(3),
			normal = DateTime.Minutes(2),
			hard = DateTime.Minutes(1)
		},
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 25 },
			normal = { Min = 25, Max = 50 },
			hard = { Min = 40, Max = 80 },
		},
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		ProducerActors = { Infantry = { NodEastHand1, NodEastHand2 }, Vehicles = { NodEastAirstrip } },
		Units = AdjustedNodCompositions,
		AttackPaths = NodEastAttackPaths,
	},
	Air = {
		Delay = {
			easy = DateTime.Minutes(13),
			normal = DateTime.Minutes(12),
			hard = DateTime.Minutes(11)
		},
		AttackValuePerSecond = {
			easy = { Min = 7, Max = 7 },
			normal = { Min = 14, Max = 14 },
			hard = { Min = 21, Max = 21 },
		},
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
		ActiveCondition = function()
			return PlayerHasNavalProduction(Greece)
		end,
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 10 },
			normal = { Min = 18, Max = 18 },
			hard = { Min = 32, Max = 32 },
		},
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
		ActiveCondition = function()
			return CountConyards(Nod) < 2 and DateTime.GameTime >= DateTime.Minutes(15)
		end,
		AttackValuePerSecond = {
			easy = { Min = 15, Max = 15 },
			normal = { Min = 25, Max = 25 },
			hard = { Min = 40, Max = 40 },
		},
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
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
	MissionPlayers = { Greece }
	TimerTicks = 0

	Camera.Position = McvLanding.CenterPosition

	InitObjectives(Greece)
	AdjustPlayerStartingCashForDifficulty()
	InitNod()
	DoMcvArrival()

	if Difficulty ~= "hard" then
		EastObelisk2.Destroy()
		EastObelisk4.Destroy()
		SouthWestObelisk2.Destroy()
		SouthWestObelisk3.Destroy()
		RiverTurret1.Destroy()

		if Difficulty == "easy" then
			SouthWestObelisk1.Destroy()
			EastObelisk1.Destroy()
			EastObelisk3.Destroy()
			LabObelisk1.Destroy()
			LabObelisk2.Destroy()
		end
	end

	if Difficulty ~= "easy" then
		Trigger.AfterDelay(DateTime.Minutes(14), function()
			InitNodAttacks()
		end)
	end

	Trigger.OnKilled(Church1, function(self, killer)
		Actor.Create("moneycrate", true, { Owner = Greece, Location = Church1.Location })
	end)

	Trigger.OnKilled(Church2, function(self, killer)
		Actor.Create("moneycrate", true, { Owner = Greece, Location = Church2.Location })
	end)

	ObjectiveFindLab = Greece.AddObjective("Locate the Nod research lab.")

	-- On proximity to lab, reveal it and update objectives.
	Trigger.OnEnteredProximityTrigger(ResearchLab.CenterPosition, WDist.New(10 * 1024), function(a, id)
		if a.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)
			RevealLab()
		end
	end)

	Trigger.OnCapture(ResearchLab, function(self, captor, oldOwner, newOwner)
		if newOwner == Greece then
			Greece.MarkCompletedObjective(ObjectiveCaptureLab)
		end
	end)

	Trigger.OnDamaged(ResearchLab, function(self, attacker, damage)
		RevealLab()
	end)

	Trigger.OnKilled(ResearchLab, function(self, killer)
		RevealLab()
		Greece.MarkFailedObjective(ObjectiveCaptureLab)
	end)

	Utils.Do({ Turret1, Turret2, Turret3, Turret4, LeftAttack2, MiddleAttack3, EastBoundary }, function (t)
		Trigger.OnEnteredProximityTrigger(t.CenterPosition, WDist.New(7 * 1024), function(a, id)
			if a.Owner == Greece then
				Trigger.RemoveProximityTrigger(id)
				InitNodAttacks()
			end
		end)
	end)

	SetupReveals({ EntranceReveal1, EntranceReveal2, EntranceReveal3, EntranceReveal4 })
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	OncePerThirtySecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Nod.Resources = Nod.ResourceCapacity - 500

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
		RebuildExcludes.Nod = { Types = { "obli", "gun.nod" } }
	end

	AutoRepairAndRebuildBuildings(Nod, 15)
	SetupRefAndSilosCaptureCredits(Nod)
	AutoReplaceHarvesters(Nod)
	AutoRebuildConyards(Nod)
	InitAiUpgrades(Nod)

	Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = Nod })
	end)

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)

	InitNavalAttackSquad(Squads.Naval, Nod)
	InitAttackSquad(Squads.LabDefense, Nod)
end

InitNodAttacks = function()
	if not NodAttacksInitialized then
		NodAttacksInitialized = true
		Notification("Nod forces have been alerted to your presence, prepare your defenses!")
		MediaCA.PlaySound("r_nodalerted.aud", 2)

		Utils.Do(Patrols, function(p)
			Utils.Do(p.Units, function(unit)
				if not unit.IsDead then
					unit.Patrol(p.Path, true)
				end
			end)
		end)

		Trigger.AfterDelay(Squads.South.Delay[Difficulty], function()
			InitAttackSquad(Squads.South, Nod)
		end)

		Trigger.AfterDelay(Squads.East.Delay[Difficulty], function()
			InitAttackSquad(Squads.East, Nod)
		end)

		Trigger.AfterDelay(Squads.Air.Delay[Difficulty], function()
			InitAirAttackSquad(Squads.Air, Nod)
		end)
	end
end

DoMcvArrival = function()
	local mcvArrivalPath = { McvEntry.Location, McvLanding.Location }
	local mcvExitPath = { McvEntry.Location }
	DoNavalTransportDrop(Greece, mcvArrivalPath, mcvExitPath, "lst.init", InitialUnits[Difficulty], function(a)
		a.Move(McvRally.Location)
	end)
end

RevealLab = function()
	if not IsLabRevealed then
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

		IsLabRevealed = true
	end
end
