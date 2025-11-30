MissionDir = "ca|missions/main-campaign/ca06-machinations"

InitialUnits = {
	easy = { "jeep", "mcv", "2tnk", "e1", "e1", "e1", "e3" },
	normal = { "jeep", "mcv", "e1", "e1", "e1", "e3"  },
	hard = { "jeep", "mcv", "e1", "e1", "e3" },
	vhard = { "jeep", "mcv", "e1", "e1" },
	brutal = { "jeep", "mcv" }
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
	easy = DateTime.Minutes(40),
	normal = DateTime.Minutes(25),
	hard = DateTime.Minutes(15),
	vhard = DateTime.Minutes(10),
	brutal = DateTime.Minutes(10)
}

PreparationTime = {
	easy = DateTime.Minutes(60),
	normal = DateTime.Minutes(20),
	hard = DateTime.Minutes(16),
	vhard = DateTime.Minutes(14),
	brutal = DateTime.Minutes(10)
}

-- Squads

LabDefenseCompositions = {
	easy = {
		{ Infantry = { "n1c", "n1c", "n1c", "n3c" }, Vehicles = { "ltnk" } },
	},
	normal = {
		{ Infantry = { "n1c", "n1c", "n1c", "n3c", "n5", "n1c", "acol" }, Vehicles = { "ltnk", "bggy", "bike", "bike" } },
	},
	hard = {
		{ Infantry = { "n1c", "n1c", "n1c", "n3c", "n5", "n1c", "tplr", "tplr", "rmbc" }, Vehicles = { "ltnk", "mlrs", "stnk.nod", "hftk", "ltnk" } },
	},
	vhard = {
		{ Infantry = { "n1c", "n1c", "n1c", "n3c", "n5", "n1c", "tplr", "tplr", "rmbc", "enli", "n1c", "n3c" }, Vehicles = { "ltnk", "mlrs", "stnk.nod", "hftk", "ltnk" } },
	},
	brutal = {
		{ Infantry = { "n1c", "n1c", "n1c", "n3c", "n5", "n1c", "tplr", "rmbc", "n1c", "n3c", "tplr", "rmbc", "enli", "n1c", "n1c", "n3c" }, Vehicles = { "ltnk", "mlrs", "stnk.nod", "hftk", "avtr" } },
	}
}

AdjustedNodCompositions = AdjustCompositionsForDifficulty(UnitCompositions.Nod)

Squads = {
	South = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(4)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		ProducerActors = { Infantry = { NodSouthHand }, Vehicles = { NodSouthAirstrip } },
		Compositions = AdjustedNodCompositions,
		AttackPaths = NodSouthAttackPaths,
	},
	East = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(2)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		ProducerActors = { Infantry = { NodEastHand1, NodEastHand2 }, Vehicles = { NodEastAirstrip } },
		Compositions = AdjustedNodCompositions,
		AttackPaths = NodEastAttackPaths,
	},
	Air = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Nod,
	},
	Naval = {
		ActiveCondition = function()
			return PlayerHasNavalProduction(Greece)
		end,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 14, Max = 14 }),
		Compositions = {
			easy = {
				{ Ships = { "sb", "ss2" } }
			},
			normal = {
				{ Ships = { "sb", "ss2", "sb" } }
			},
			hard = {
				{ Ships = { "sb", "sb", "ss2", "ss2" } }
			},
			vhard = {
				{ Ships = { "sb", "sb", "ss2", "ss2" } }
			},
			brutal = {
				{ Ships = { "sb", "sb", "ss2", "ss2" } }
			}
		},
		AttackPaths = NodNavalAttackPath
	},
	ICBMSubs = {
		Delay = DateTime.Minutes(15),
		AttackValuePerSecond = { Min = 8, Max = 16 },
		Compositions = {
			brutal = {
				{ Ships = { "isub" } }
			},
		},
		AttackPaths = NodNavalAttackPath
	},
	LabDefense = {
		ActiveCondition = function()
			return CountConyards(Nod) < 2 and DateTime.GameTime >= DateTime.Minutes(15)
		end,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 20 }),
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		ProducerActors = { Infantry = { NodQuarryHand }, Vehicles = { NodQuarryAirstrip } },
		Compositions = LabDefenseCompositions,
		AttackPaths = NodSouthAttackPaths
	}
}

-- Setup and Tick

SetupPlayers = function()
	Greece = Player.GetPlayer("Greece")
	Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { Greece }
	MissionEnemies = { Nod }
end

WorldLoaded = function()
	SetupPlayers()

	TimerTicks = 0
	Camera.Position = McvLanding.CenterPosition

	InitObjectives(Greece)
	AdjustPlayerStartingCashForDifficulty()
	InitNod()
	SetupChurchMoneyCrates()
	DoMcvArrival()

	if IsNormalOrBelow() then
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

	Trigger.AfterDelay(PreparationTime[Difficulty], function()
		InitNodAttacks()
	end)

	ObjectiveFindLab = Greece.AddObjective("Locate the Nod research lab.")

	-- On proximity to lab, reveal it and update objectives.
	Trigger.OnEnteredProximityTrigger(ResearchLab.CenterPosition, WDist.New(10 * 1024), function(a, id)
		if a.EffectiveOwner == Greece then
			Trigger.RemoveProximityTrigger(id)
			RevealLab()
		end
	end)

	Trigger.OnCapture(ResearchLab, function(self, captor, oldOwner, newOwner)
		if IsMissionPlayer(newOwner) then
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
			if IsMissionPlayer(a.Owner) then
				Trigger.RemoveProximityTrigger(id)
				InitNodAttacks()
			end
		end)
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
		Nod.Resources = Nod.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
		end

		if DateTime.GameTime > DateTime.Seconds(10) then
			if MissionPlayersHaveNoRequiredUnits() then
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
		MediaCA.PlaySound(MissionDir .. "/r_nodalerted.aud", 2)

		InitAttackSquad(Squads.South, Nod)
		InitAttackSquad(Squads.East, Nod)
		InitAirAttackSquad(Squads.Air, Nod)

		if Difficulty == "brutal" then
			InitNavalAttackSquad(Squads.ICBMSubs, Nod)
		end

		Utils.Do(Patrols, function(p)
			Utils.Do(p.Units, function(unit)
				if not unit.IsDead then
					unit.Patrol(p.Path, true)
				end
			end)
		end)
	end
end

-- overridden in co-op version
DoMcvArrival = function()
	local mcvArrivalPath = { McvEntry.Location, McvLanding.Location }
	local mcvExitPath = { McvEntry.Location }
	DoNavalTransportDrop(Greece, mcvArrivalPath, mcvExitPath, "lst.reinforce", InitialUnits[Difficulty], function(a)
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
