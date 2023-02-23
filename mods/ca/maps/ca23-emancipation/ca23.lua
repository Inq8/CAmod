


ScrinWaterAttackPaths = {
	{ AttackNode1.Location, AttackNode2.Location },
	{ AttackNode1.Location, AttackNode3.Location },
	{ AttackNode1.Location, AttackNode4.Location },
}

ScrinGroundAttackPaths = {
	{ AttackNode5.Location, AttackNode6.Location },
	{ AttackNode5.Location, AttackNode4.Location },
	{ AttackNode5.Location, AttackNode7.Location },
}

Masterminds = { Mastermind1, Mastermind2, Mastermind3, Mastermind4, Mastermind5 }

MaxEnslavedUnitsKilled = {
	normal = 20,
	hard = 10
}

Squads = {
	ScrinMain = {
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 40 } },
			normal = { { MinTime = 0, Value = 68 } },
			hard = { { MinTime = 0, Value = 105 } },
		},
		ActiveCondition = function()
			return Mastermind3.IsDead or Mastermind4.IsDead or DateTime.GameTime > DateTime.Minutes(6)
		end,
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false,
			Aircraft = false,
		},
		FollowSquadLeader = true,
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Infantry = { "port" }, Vehicles = { "wsph" }, Aircraft = { "grav" } },
		Units = UnitCompositions.Scrin.Main,
		AttackPaths = ScrinGroundAttackPaths,
	},
	ScrinWater = {
		Delay = {
			easy = DateTime.Seconds(140),
			normal = DateTime.Seconds(120),
			hard = DateTime.Seconds(100)
		},
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 10 }, { MinTime = DateTime.Minutes(12), Value = 20 } },
			normal = { { MinTime = 0, Value = 16 }, { MinTime = DateTime.Minutes(10), Value = 32 } },
			hard = { { MinTime = 0, Value = 28 }, { MinTime = DateTime.Minutes(8), Value = 55 } },
		},
		QueueProductionStatuses = {
			Vehicles = false
		},
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Vehicles = { "wsph" } },
		Units = {
			easy = {
				{ Vehicles = { "intl", "seek" }, },
				{ Vehicles = { "seek", "seek" }, },
				{ Vehicles = { "lace", "lace" }, }
			},
			normal = {
				{ Vehicles = { "seek", "intl.ai2" }, },
				{ Vehicles = { "seek", "seek", "seek" }, },
				{ Vehicles = { "lace", "lace", "lace" }, },
			},
			hard = {
				{ Vehicles = { "intl", "intl.ai2", "seek" }, },
				{ Vehicles = { "seek", "seek", "seek" }, },
				{ Vehicles = { "lace", "lace", "seek", "seek" }, },
				{ Vehicles = { "devo", "intl.ai2", "ruin" }, MinTime = DateTime.Minutes(7) },
			}
		},
		AttackPaths = ScrinWaterAttackPaths,
	},
	ScrinAir = {
		Delay = {
			easy = DateTime.Minutes(10),
			normal = DateTime.Minutes(8),
			hard = DateTime.Minutes(6)
		},
		Interval = {
			easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(2)
		},
		QueueProductionStatuses = {
			Aircraft = false
		},
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Aircraft = { "grav" } },
		Units = {
			easy = {
				{ Aircraft = { "stmr" } }
			},
			normal = {
				{ Aircraft = { "stmr", "stmr" } },
				{ Aircraft = { "enrv" } },
			},
			hard = {
				{ Aircraft = { "stmr", "stmr", "stmr" } },
				{ Aircraft = { "enrv", "enrv" } },
			}
		}
	},
	ScrinBigAir = {
		Interval = {
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(2)
		},
		ActiveCondition = function()
			return Mastermind4.IsDead
		end,
		QueueProductionStatuses = {
			Aircraft = false
		},
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Aircraft = { "grav" } },
		Units = {
			normal = {
				{ Aircraft = { PacOrDevastator, PacOrDevastator } },
			},
			hard = {
				{ Aircraft = { PacOrDevastator, PacOrDevastator, PacOrDevastator } },
			}
		},
		AttackPaths = ScrinWaterAttackPaths,
	},
}

WorldLoaded = function()
    GDI = Player.GetPlayer("GDI")
    Scrin = Player.GetPlayer("Scrin")
    GDISlaves = Player.GetPlayer("GDISlaves")
	MissionPlayer = GDI
	EnslavedUnitsKilled = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(GDI)
	InitScrin()

	ObjectiveLiberateBases = GDI.AddObjective("Kill Masterminds to liberate GDI bases.")

	if Difficulty == "easy" then
		NormalHardOnlyTripod.Destroy()
	else
		ObjectiveMinimiseCasualties = GDI.AddObjective("Avoid killing mind controlled GDI units.")
	end

	Actor.Create("bdrone.upgrade", true, { Owner = GDI })
	Actor.Create("mdrone.upgrade", true, { Owner = GDI })

	Trigger.AfterDelay(DateTime.Seconds(7), function()
		Tip("Drones (e.g. Guardian Drones, Mini Drones, Battle Drones, Mammoth Drones and Mobile EMP) are immune to mind control.")
		Trigger.AfterDelay(DateTime.Seconds(7), function()
			Tip("Masterminds are also unable to mind control aircraft.")
			Trigger.AfterDelay(DateTime.Seconds(7), function()
				Tip("Larger drones (Battle Drones, Mammoth Drones and Mobile EMP) require an active radar to function.")
			end)
		end)
	end)

	Trigger.OnAllKilled(Masterminds, function()
		ObjectiveEliminateScrin = GDI.AddObjective("Eliminate the Scrin presence.")
		GDI.MarkCompletedObjective(ObjectiveLiberateBases)
		if ObjectiveMinimiseCasualties ~= nil and EnslavedUnitsKilled <= MaxEnslavedUnitsKilled[Difficulty] then
			GDI.MarkCompletedObjective(ObjectiveMinimiseCasualties)
		end
	end)

	Trigger.AfterDelay(1, function()
		Utils.Do(Masterminds, function(m)
			local slaves = Map.ActorsInCircle(m.CenterPosition, WDist.New(18 * 1024), function(s)
				return not s.IsDead and s.Owner == GDISlaves
			end)

			Trigger.OnKilled(m, function(self, killer)
				UpdateObjectiveText()
				Utils.Do(slaves, function(s)
					if not s.IsDead then
						s.Owner = GDI
						s.Stop()
						if s.HasProperty("FindResources") then
							s.FindResources()
						end
					end
				end)

				Trigger.AfterDelay(1, function()
					Actor.Create("QueueUpdaterDummy", true, { Owner = GDI })
				end)

				if m == Mastermind4 then
					Actor.Create("amcv.enabled", true, { Owner = GDI })
				end
			end)

			Trigger.OnEnteredProximityTrigger(m.CenterPosition, WDist.New(11 * 1024), function(a, id)
				if a.Owner == GDI and a.Type ~= "smallcamera" and not m.IsDead then
					Trigger.RemoveProximityTrigger(id)
					local camera = Actor.Create("smallcamera", true, { Owner = GDI, Location = m.Location })
					Notification("A Mastermind has been located.")
					Beacon.New(GDI, m.CenterPosition)
					Trigger.AfterDelay(DateTime.Seconds(4), function()
						camera.Destroy()
					end)
				end
			end)
		end)
	end)

	local enslavedUnits = GDISlaves.GetActors()

	Utils.Do(enslavedUnits, function(a)
		if a.HasProperty("Move") then
			Trigger.OnKilled(a, function(self, killer)
				if killer.Owner == GDI and (self.Owner == GDISlaves or self.Owner == Scrin) then
					EnslavedUnitKilled()
				end
			end)
		end
	end)

	Trigger.OnAnyProduction(function(producer, produced, productionType)

		if produced.Owner == GDI and produced.HasProperty("Move") then
			Trigger.OnKilled(produced, function(self, killer)
				if killer.Owner == GDI and self.Owner == Scrin then
					EnslavedUnitKilled()
				end
			end)
		end
	end)

	local revealPoints = { EntranceReveal1, EntranceReveal2, EntranceReveal3, EntranceReveal4, EntranceReveal5, EntranceReveal6 }
	Utils.Do(revealPoints, function(p)
		Trigger.OnEnteredProximityTrigger(p.CenterPosition, WDist.New(11 * 1024), function(a, id)
			if a.Owner == GDI and a.Type ~= "smallcamera" then
				Trigger.RemoveProximityTrigger(id)
				local camera = Actor.Create("smallcamera", true, { Owner = GDI, Location = p.Location })
				Trigger.AfterDelay(DateTime.Seconds(4), function()
					camera.Destroy()
				end)
			end
		end)
	end)

	UpdateObjectiveText()
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Scrin.Resources = Scrin.ResourceCapacity - 500

		if Scrin.HasNoRequiredUnits() and ObjectiveEliminateScrin ~= nil then
			GDI.MarkCompletedObjective(ObjectiveEliminateScrin)
		end

		if GDI.HasNoRequiredUnits() then
			if ObjectiveLiberateBases ~= nil and not GDI.IsObjectiveCompleted(ObjectiveLiberateBases) then
				GDI.MarkFailedObjective(ObjectiveLiberateBases)
			end
			if ObjectiveEliminateScrin ~= nil and not GDI.IsObjectiveCompleted(ObjectiveEliminateScrin) then
				GDI.MarkFailedObjective(ObjectiveEliminateScrin)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocation()
	end
end

InitScrin = function()
	Actor.Create("POWERCHEAT", true, { Owner = GDISlaves })

	AutoRepairAndRebuildBuildings(Scrin, 15)
	SetupRefAndSilosCaptureCredits(Scrin)
	AutoReplaceHarvesters(Scrin)

	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)

	if Difficulty == "hard" then
		Actor.Create("ioncon.upgrade", true, { Owner = Scrin })
		Actor.Create("shields.upgrade", true, { Owner = Scrin })

		Trigger.AfterDelay(DateTime.Minutes(20), function()
			Actor.Create("carapace.upgrade", true, { Owner = Scrin })
		end)
	end

	InitAttackSquad(Squads.ScrinMain, Scrin)
	InitAttackSquad(Squads.ScrinBigAir, Scrin)

	Trigger.AfterDelay(Squads.ScrinWater.Delay[Difficulty], function()
		InitAttackSquad(Squads.ScrinWater, Scrin)
	end)

	Trigger.AfterDelay(Squads.ScrinAir.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.ScrinAir, Scrin, GDI, { "harv.td", "msam", "nuke", "nuk2", "orca", "a10", "a10.upg", "htnk", "htnk.drone", "mtnk.drone" })
	end)
end

UpdateObjectiveText = function()
	if not GDI.IsObjectiveCompleted(ObjectiveLiberateBases) then
		local activeMasterminds = Scrin.GetActorsByType("mast")
		local objectiveText = "      Masterminds remaining: " .. #activeMasterminds
		local objectiveTextColor = HSLColor.Yellow

		if Difficulty ~= "easy" then
			if MaxEnslavedUnitsKilled[Difficulty] - EnslavedUnitsKilled < 2 then
				objectiveTextColor = HSLColor.Red
			end

			objectiveText = objectiveText .. "\nEnslaved GDI units killed: " .. EnslavedUnitsKilled .. " (max " .. MaxEnslavedUnitsKilled[Difficulty] .. ")"
		end

		UserInterface.SetMissionText(objectiveText, objectiveTextColor)
	else
		UserInterface.SetMissionText("Eliminate the Scrin presence.", HSLColor.Yellow)
	end
end

EnslavedUnitKilled = function()
	EnslavedUnitsKilled = EnslavedUnitsKilled + 1
	UpdateObjectiveText()
	if ObjectiveMinimiseCasualties ~= nil and EnslavedUnitsKilled > MaxEnslavedUnitsKilled[Difficulty] then
		GDI.MarkFailedObjective(ObjectiveMinimiseCasualties)
	end
end
