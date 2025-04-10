AlliedSavedAdvancedBuildings = {}
USSRSavedAdvancedBuildings = {}
NodSavedAdvancedBuildings = {}

AlliesVsNodPaths = { { AlliesVsNodRally1.Location }, { AlliesVsNodRally2.Location }, { AlliesVsNodRally3.Location } }
AlliesVsSovietPaths = { { AlliesVsSovietsRally1.Location }, { AlliesVsSovietsRally2.Location }, { AlliesVsSovietsRally3.Location } }
SovietsVsNodPaths = { { SovietsVsNodRally1.Location }, { SovietsVsNodRally2.Location }, { SovietsVsNodRally3.Location }, { SovietsVsNodRally4.Location } }
SovietsVsAlliesPaths = { { SovietsVsAlliesRally1.Location }, { SovietsVsAlliesRally2.Location }, { SovietsVsAlliesRally3.Location }, { SovietsVsAlliesRally4.Location } }
NodVsAlliesPaths = { { NodVsAlliesRally1.Location }, { NodVsAlliesRally2.Location }, { NodVsAlliesRally3.Location } }
NodVsSovietPaths = { { NodVsSovietsRally1.Location }, { NodVsSovietsRally2.Location }, { NodVsSovietsRally3.Location } }

NodBaseCameras = { NodBaseCam1, NodBaseCam2, NodBaseCam3 }
AlliedBaseCameras = { AlliedBaseCam1, AlliedBaseCam2 }
SovietBaseCameras = { SovietBaseCam1, SovietBaseCam2, SovietBaseCam3 }

SuperweaponsEnabledTime = {
	easy = DateTime.Seconds((60 * 30) + 17),
	normal = DateTime.Seconds((60 * 20) + 17),
	hard = DateTime.Seconds((60 * 10) + 17),
}

Squads = {
	Allies = {
		Delay = {
			easy = DateTime.Minutes(3),
			normal = DateTime.Minutes(2),
			hard = DateTime.Seconds(10),
		},
		AttackValuePerSecond = {
			easy = { Min = 12, Max = 25, RampDuration = DateTime.Minutes(7) },
			normal = { Min = 25, Max = 50, RampDuration = DateTime.Minutes(5) },
			hard = { Min = 40, Max = 80, RampDuration = DateTime.Minutes(3) },
		},
		FollowLeader = true,
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = AdjustCompositionsForDifficulty(UnitCompositions.Allied),
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
			easy = { Min = 12, Max = 25, RampDuration = DateTime.Minutes(7) },
			normal = { Min = 25, Max = 50, RampDuration = DateTime.Minutes(5) },
			hard = { Min = 40, Max = 80, RampDuration = DateTime.Minutes(3) },
		},
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = AdjustCompositionsForDifficulty(UnitCompositions.Nod),
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
			easy = { Min = 12, Max = 25, RampDuration = DateTime.Minutes(7) },
			normal = { Min = 25, Max = 50, RampDuration = DateTime.Minutes(5) },
			hard = { Min = 40, Max = 80, RampDuration = DateTime.Minutes(3) },
		},
		FollowLeader = true,
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = AdjustCompositionsForDifficulty(UnitCompositions.Soviet),
		AttackPaths = {
			-- set on init
		},
	},
	AlliedAir = {
		Delay = {
			easy = DateTime.Minutes(8),
			normal = DateTime.Minutes(5),
			hard = DateTime.Minutes(2)
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
			normal = DateTime.Minutes(5),
			hard = DateTime.Minutes(2)
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
			normal = DateTime.Minutes(5),
			hard = DateTime.Minutes(2)
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
	AdjustPlayerStartingCashForDifficulty()

	ObjectiveInitialSubjugation = Scrin.AddObjective("Subjugate one of the three human bases.")
	ObjectiveProdigyMustSurvive = Scrin.AddSecondaryObjective("Protect the Prodigy.")

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
				InitUSSR(SovietsVsAlliesPaths, AlliedBaseCameras)
				InitNod(NodVsAlliesPaths, AlliedBaseCameras)
				CreateWormholes(AlliedBase.Location)
				Trigger.AfterDelay(1, function()
					Actor.Create("QueueUpdaterDummy", true, { Owner = Scrin })
				end)
				SetupMainObjectives({ NodConyard, SovietConyard })
				if Difficulty == "hard" then
					Trigger.AfterDelay(DateTime.Minutes(1), function()
						local alliedGroundAttackers = Utils.Where(Greece.GetGroundAttackers(), IsGreeceGroundHunterUnit)
						Utils.Do(alliedGroundAttackers, IdleHunt)
					end)
				end
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
				InitGreece(AlliesVsSovietPaths, SovietBaseCameras)
				InitNod(NodVsSovietPaths, SovietBaseCameras)
				CreateWormholes(SovietBase.Location)
				Trigger.AfterDelay(1, function()
					Actor.Create("QueueUpdaterDummy", true, { Owner = Scrin })
				end)
				SetupMainObjectives({ NodConyard, AlliedConyard })
				if Difficulty == "hard" then
					Trigger.AfterDelay(DateTime.Minutes(1), function()
						local ussrGroundAttackers = Utils.Where(USSR.GetGroundAttackers(), IsUSSRGroundHunterUnit)
						Utils.Do(ussrGroundAttackers, IdleHunt)
					end)
				end
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
				InitUSSR(SovietsVsNodPaths, NodBaseCameras)
				InitGreece(AlliesVsNodPaths, NodBaseCameras)
				CreateWormholes(NodBase.Location)
				Trigger.AfterDelay(1, function()
					Actor.Create("QueueUpdaterDummy", true, { Owner = Scrin })
				end)
				SetupMainObjectives({ SovietConyard, AlliedConyard })
				if Difficulty == "hard" then
					Trigger.AfterDelay(DateTime.Minutes(1), function()
						local nodGroundAttackers = Utils.Where(Nod.GetGroundAttackers(), IsNodGroundHunterUnit)
						Utils.Do(nodGroundAttackers, IdleHunt)
					end)
				end
			end
		end
	end)

	Trigger.OnKilled(Prodigy, function(self, killer)
		Scrin.MarkFailedObjective(ObjectiveProdigyMustSurvive)
		Notification("The Prodigy used its psionic powers to cheat death and has fled the battlefield to recuperate.")
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	OncePerThirtySecondChecks()
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
				Wormhole1.Kill()
				Wormhole2.Kill()
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

				if not Scrin.IsObjectiveCompleted(ObjectiveProdigyMustSurvive) then
					Scrin.MarkCompletedObjective(ObjectiveProdigyMustSurvive)
				end
			end
		end
	end
end

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % DateTime.Seconds(30) == 0 then
		CalculatePlayerCharacteristics()
	end
end

-- Functions

InitUSSR = function(paths, cameras)
	Squads.Soviets.AttackPaths = paths

	local ussrGroundAttackers = USSR.GetGroundAttackers()

	Utils.Do(ussrGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsUSSRGroundHunterUnit)
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Utils.Do(USSRSavedAdvancedBuildings, function(b)
			Actor.Create(b.Type, true, { Location = b.Location, Owner = USSR })
		end)

		Trigger.AfterDelay(1, function()
			AutoRepairAndRebuildBuildings(USSR)
			SetupRefAndSilosCaptureCredits(USSR)
			AutoReplaceHarvesters(USSR)
			InitAiUpgrades(USSR)
		end)
	end)

	Trigger.AfterDelay(Squads.Soviets.Delay[Difficulty], function()
		InitAttackSquad(Squads.Soviets, USSR)
	end)

	Trigger.AfterDelay(Squads.SovietAir.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.SovietAir, USSR)
	end)

	Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = USSR })
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = USSR })
	end)

	Utils.Do(cameras, function(c)
		Actor.Create("largecamera", true, { Owner = USSR, Location = c.Location })
	end)
end

InitGreece = function(paths, cameras)
	Squads.Allies.AttackPaths = paths

	local greeceGroundAttackers = Greece.GetGroundAttackers()

	Utils.Do(greeceGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGreeceGroundHunterUnit)
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Utils.Do(AlliedSavedAdvancedBuildings, function(b)
			Actor.Create(b.Type, true, { Location = b.Location, Owner = Greece })
		end)

		Trigger.AfterDelay(1, function()
			AutoRepairAndRebuildBuildings(Greece)
			SetupRefAndSilosCaptureCredits(Greece)
			AutoReplaceHarvesters(Greece)
			InitAiUpgrades(Greece)
		end)
	end)

	Trigger.AfterDelay(Squads.Allies.Delay[Difficulty], function()
		InitAttackSquad(Squads.Allies, Greece)
	end)

    Trigger.AfterDelay(Squads.AlliedAir.Delay[Difficulty], function()
        InitAirAttackSquad(Squads.AlliedAir, Nod)
    end)

	Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = Greece })
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = Greece })
	end)

	Utils.Do(cameras, function(c)
		Actor.Create("largecamera", true, { Owner = Greece, Location = c.Location })
	end)
end

InitNod = function(paths, cameras)
	Squads.Nod.AttackPaths = paths

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Utils.Do(NodSavedAdvancedBuildings, function(b)
			Actor.Create(b.Type, true, { Location = b.Location, Owner = Nod })
		end)

		Trigger.AfterDelay(1, function()
			AutoRepairAndRebuildBuildings(Nod)
			SetupRefAndSilosCaptureCredits(Nod)
			AutoReplaceHarvesters(Nod)
			InitAiUpgrades(Nod)
		end)
	end)

	Trigger.AfterDelay(Squads.Nod.Delay[Difficulty], function()
		InitAttackSquad(Squads.Nod, Nod)
	end)

    Trigger.AfterDelay(Squads.NodAir.Delay[Difficulty], function()
        InitAirAttackSquad(Squads.NodAir, Nod)
    end)

	Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = Nod })
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = Nod })
	end)

	Utils.Do(cameras, function(c)
		Actor.Create("largecamera", true, { Owner = Nod, Location = c.Location })
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
