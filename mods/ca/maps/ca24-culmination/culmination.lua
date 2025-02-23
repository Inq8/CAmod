AlliedSavedAdvancedBuildings = {}
USSRSavedAdvancedBuildings = {}
NodSavedAdvancedBuildings = {}

AlliesVsNodPaths = { { AlliesVsNodRally1.Location }, { AlliesVsNodRally2.Location }, { AlliesVsNodRally3.Location } }
AlliesVsSovietPaths = { { AlliesVsSovietsRally1.Location }, { AlliesVsSovietsRally2.Location }, { AlliesVsSovietsRally3.Location } }
SovietsVsNodPaths = { { SovietsVsNodRally1.Location }, { SovietsVsNodRally2.Location }, { SovietsVsNodRally3.Location }, { SovietsVsNodRally4.Location } }
SovietsVsAlliesPaths = { { SovietsVsAlliesRally1.Location }, { SovietsVsAlliesRally2.Location }, { SovietsVsAlliesRally3.Location }, { SovietsVsAlliesRally4.Location } }
NodVsAlliesPaths = { { NodVsAlliesRally1.Location }, { NodVsAlliesRally2.Location }, { NodVsAlliesRally3.Location } }
NodVsSovietPaths = { { NodVsSovietsRally1.Location }, { NodVsSovietsRally2.Location }, { NodVsSovietsRally3.Location } }

Squads = {
	Allies = {
		Delay = {
			easy = DateTime.Minutes(3),
			normal = DateTime.Minutes(2),
			hard = DateTime.Seconds(10),
		},
		AttackValuePerSecond = {
			easy = { Min = 12, Max = 25 },
			normal = { Min = 25, Max = 50 },
			hard = { Min = 40, Max = 80 },
		},
		FollowLeader = true,
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = UnitCompositions.Allied.Main,
		AttackPaths = {
			-- set on init
		},
	},
	Nod = {
		Delay = {
			easy = DateTime.Minutes(3),
			normal = DateTime.Minutes(2),
			hard = DateTime.Seconds(10),
		},
		AttackValuePerSecond = {
			easy = { Min = 12, Max = 25 },
			normal = { Min = 25, Max = 50 },
			hard = { Min = 40, Max = 80 },
		},
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = UnitCompositions.Nod.Main,
		AttackPaths = {
			-- set on init
		},
	},
	Soviets = {
		Delay = {
			easy = DateTime.Minutes(3),
			normal = DateTime.Minutes(2),
			hard = DateTime.Seconds(10),
		},
		AttackValuePerSecond = {
			easy = { Min = 12, Max = 25 },
			normal = { Min = 25, Max = 50 },
			hard = { Min = 40, Max = 80 },
		},
		FollowLeader = true,
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = UnitCompositions.Soviet.Main,
		AttackPaths = {
			-- set on init
		},
	},
	AlliesAir = {
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
				{ Aircraft = { "heli", "heli" } },
				{ Aircraft = { "pmak" } },
			},
			normal = {
				{ Aircraft = { "heli", "heli", "heli" } },
				{ Aircraft = { "harr", "harr" } },
				{ Aircraft = { "pmak", "pmak" } }
			},
			hard = {
				{ Aircraft = { "heli", "heli", "heli", "heli" } },
				{ Aircraft = { "harr", "harr", "harr" } },
				{ Aircraft = { "pmak", "pmak", "pmak" } }
			}
		},
	},
	NodAir = {
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
	SovietAir = {
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
		ProducerTypes = { Aircraft = { "afld" } },
		Units = {
			easy = {
				{ Aircraft = { "mig" }, { HindOrYak } }
			},
			normal = {
				{ Aircraft = { MigOrSukhoi, "mig" }, { HindOrYak, HindOrYak } }
			},
			hard = {
				{ Aircraft = { MigOrSukhoi, MigOrSukhoi, MigOrSukhoi }, { HindOrYak, HindOrYak, HindOrYak } }
			}
		},
	},
}

-- Setup and Tick

WorldLoaded = function()
	Scrin = Player.GetPlayer("Scrin")
	USSR = Player.GetPlayer("USSR")
	Greece = Player.GetPlayer("Greece")
	Nod = Player.GetPlayer("Nod")
	MissionPlayers = { Scrin }
	TimerTicks = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Scrin)
	AdjustStartingCash()

	ObjectiveInitialSubjugation = Scrin.AddObjective("Subjugate one of the three human bases.")
	ObjectiveProdigyMustSurvive = Scrin.AddObjective("The Prodigy must survive.")

	local greeceAdvancedBuildings = Greece.GetActorsByTypes({ "atek", "alhq", "weat", "pdox", "dome", "hpad" })
	local ussrAdvancedBuildings = USSR.GetActorsByTypes({ "stek", "npwr", "mslo", "iron", "dome", "afld" })
	local nodAdvancedBuildings = Nod.GetActorsByTypes({ "tmpl", "tmpp", "sgen", "mslo.nod", "hq", "hpad.td" })

	table.insert(greeceAdvancedBuildings, AlliedExtraPower1)
	table.insert(greeceAdvancedBuildings, AlliedExtraPower2)
	table.insert(nodAdvancedBuildings, NodExtraPower1)
	table.insert(nodAdvancedBuildings, NodExtraPower2)

	Utils.Do(greeceAdvancedBuildings, function(a)
		local building = { Type = a.Type, Location = a.Location }
		table.insert(AlliedSavedAdvancedBuildings, building)
		a.Destroy()
	end)

	Utils.Do(ussrAdvancedBuildings, function(a)
		local building = { Type = a.Type, Location = a.Location }
		table.insert(USSRSavedAdvancedBuildings, building)
		a.Destroy()
	end)

	Utils.Do(nodAdvancedBuildings, function(a)
		local building = { Type = a.Type, Location = a.Location }
		table.insert(NodSavedAdvancedBuildings, building)
		a.Destroy()
	end)

	local nodCamera = Actor.Create("largecamera", true, { Owner = Scrin, Location = NodBase.Location })
	local greeceCamera = Actor.Create("largecamera", true, { Owner = Scrin, Location = AlliedBase.Location })
	local ussrCamera = Actor.Create("largecamera", true, { Owner = Scrin, Location = SovietBase.Location })

	Trigger.OnEnteredProximityTrigger(AlliedBase.CenterPosition, WDist.New(14 * 1024), function(a, id)
		if a.Owner == Scrin and a.Type == "subjugation.dummy" then
			Trigger.RemoveProximityTrigger(id)

			if not Scrin.IsObjectiveCompleted(ObjectiveInitialSubjugation) then
				ObjectiveSubjugateRemaining = Scrin.AddObjective("Capture Nod and Soviet Construction Yards.")
				Scrin.MarkCompletedObjective(ObjectiveInitialSubjugation)
				DestroyCameras()
				InitUSSR(SovietsVsAlliesPaths)
				InitNod(NodVsAlliesPaths)
				CreateWormholes(AlliedBase.Location)
				Trigger.AfterDelay(1, function()
					Actor.Create("QueueUpdaterDummy", true, { Owner = Scrin })
				end)
				SetupMainObjectives({ NodConyard, SovietConyard })
			end
		end
	end)

	Trigger.OnEnteredProximityTrigger(SovietBase.CenterPosition, WDist.New(14 * 1024), function(a, id)
		if a.Owner == Scrin and a.Type == "subjugation.dummy" then
			Trigger.RemoveProximityTrigger(id)
			if not Scrin.IsObjectiveCompleted(ObjectiveInitialSubjugation) then
				ObjectiveSubjugateRemaining = Scrin.AddObjective("Capture Allied and Nod Construction Yards.")
				Scrin.MarkCompletedObjective(ObjectiveInitialSubjugation)
				DestroyCameras()
				InitGreece(AlliesVsSovietPaths)
				InitNod(NodVsSovietPaths)
				CreateWormholes(SovietBase.Location)
				Trigger.AfterDelay(1, function()
					Actor.Create("QueueUpdaterDummy", true, { Owner = Scrin })
				end)
				SetupMainObjectives({ NodConyard, AlliedConyard })
			end
		end
	end)

	Trigger.OnEnteredProximityTrigger(NodBase.CenterPosition, WDist.New(14 * 1024), function(a, id)
		if a.Owner == Scrin and a.Type == "subjugation.dummy" then
			Trigger.RemoveProximityTrigger(id)
			if not Scrin.IsObjectiveCompleted(ObjectiveInitialSubjugation) then
				ObjectiveSubjugateRemaining = Scrin.AddObjective("Capture Allied and Soviet Construction Yards.")
				Scrin.MarkCompletedObjective(ObjectiveInitialSubjugation)
				DestroyCameras()
				InitUSSR(SovietsVsNodPaths)
				InitGreece(AlliesVsNodPaths)
				CreateWormholes(NodBase.Location)
				Trigger.AfterDelay(1, function()
					Actor.Create("QueueUpdaterDummy", true, { Owner = Scrin })
				end)
				SetupMainObjectives({ SovietConyard, AlliedConyard })
			end
		end
	end)

	Trigger.OnKilled(Prodigy, function(self, killer)
		Scrin.MarkFailedObjective(ObjectiveProdigyMustSurvive)
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Greece.Resources = Greece.ResourceCapacity - 500
		Nod.Resources = Nod.ResourceCapacity - 500
		USSR.Resources = USSR.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
				UserInterface.SetMissionText("Wormhole closes in " .. UtilsCA.FormatTimeForGameSpeed(TimerTicks), HSLColor.Yellow)
			else
				TimerTicks = 0
				UserInterface.SetMissionText("")
				Wormhole1.Destroy()
				Wormhole2.Destroy()
			end
		end

		if Scrin.HasNoRequiredUnits() then
			if ObjectiveInitialSubjugation ~= nil and not Scrin.IsObjectiveCompleted(ObjectiveInitialSubjugation) then
				Scrin.MarkFailedObjective(ObjectiveInitialSubjugation)
			end

			if ObjectiveSubjugateRemaining ~= nil and not Scrin.IsObjectiveCompleted(ObjectiveSubjugateRemaining) then
				Scrin.MarkFailedObjective(ObjectiveSubjugateRemaining)
			end

			if ObjectiveDefeatRemaining ~= nil and not Scrin.IsObjectiveCompleted(ObjectiveDefeatRemaining) then
				Scrin.MarkFailedObjective(ObjectiveDefeatRemaining)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()

		if ObjectiveDefeatRemaining ~= nil and not Scrin.IsObjectiveCompleted(ObjectiveDefeatRemaining) then
			if not PlayerHasBuildings(Greece) and not PlayerHasBuildings(USSR) and not PlayerHasBuildings(Nod) then
				Scrin.MarkCompletedObjective(ObjectiveDefeatRemaining)
			end
		end
	end
end

-- Functions

InitUSSR = function(paths)
	Squads.Soviets.AttackPaths = paths

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Utils.Do(USSRSavedAdvancedBuildings, function(b)
			Actor.Create(b.Type, true, { Location = b.Location, Owner = USSR })
		end)
	end)

	Trigger.AfterDelay(Squads.SovietAir.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.SovietAir, USSR, Scrin)
	end)
end

InitGreece = function(paths)
	Squads.Allies.AttackPaths = paths

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Utils.Do(AlliedSavedAdvancedBuildings, function(b)
			Actor.Create(b.Type, true, { Location = b.Location, Owner = Greece })
		end)
	end)

    Trigger.AfterDelay(Squads.AlliedAir.Delay[Difficulty], function()
        InitAirAttackSquad(Squads.AlliedAir, Nod, Scrin)
    end)
end

InitNod = function(paths)
	Squads.Nod.AttackPaths = paths

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Utils.Do(NodSavedAdvancedBuildings, function(b)
			Actor.Create(b.Type, true, { Location = b.Location, Owner = Nod })
		end)
	end)

    Trigger.AfterDelay(Squads.NodAir.Delay[Difficulty], function()
        InitAirAttackSquad(Squads.NodAir, Nod, Scrin)
    end)
end

DestroyCameras = function()
	Utils.Do(Scrin.GetActorsByType("largecamera"), function(a)
		a.Destroy()
	end)
end

CreateWormholes = function(dest)
	Wormhole1 = Actor.Create("wormhole", true, { Owner = Scrin, Location = PlayerStart.Location })
	Wormhole2 = Actor.Create("wormhole", true, { Owner = Scrin, Location = dest })
	TimerTicks = DateTime.Minutes(1)
end

SetupMainObjectives = function(conyards)
	ObjectiveDefeatRemaining = Scrin.AddObjective("Defeat the remaining human forces.")
	ConyardsCaptured = 0

	Utils.Do(conyards, function(c)
		Trigger.OnCapture(c, function(self, captor, oldOwner, newOwner)
			if newOwner == Scrin then
				ConyardsCaptured = ConyardsCaptured + 1
				if ConyardsCaptured == 2 then
					Scrin.MarkCompletedObjective(ObjectiveSubjugateRemaining)
				end
			end
		end)

		Trigger.OnKilled(c, function(self, killer)
			if self.Owner ~= Scrin then
				Scrin.MarkFailedObjective(ObjectiveSubjugateRemaining)
			end
		end)
	end)
end
