MissionDir = "ca|missions/main-campaign/ca27-emancipation"

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

MastermindsLocated = {}

MaxEnslavedUnitsKilled = {
	normal = 20,
	hard = 10,
	vhard = 5,
	brutal = 2
}

Squads = {
	ScrinMain = {
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 54, Max = 54 }),
		ActiveCondition = function()
			return Mastermind3.IsDead or Mastermind4.IsDead or DateTime.GameTime > DateTime.Minutes(6)
		end,
		FollowLeader = true,
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin),
		AttackPaths = ScrinGroundAttackPaths,
	},
	ScrinWater = {
		Delay = AdjustDelayForDifficulty(DateTime.Seconds(120)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 25, RampDuration = DateTime.Minutes(12) }),
		Compositions = ScrinWaterCompositions,
		AttackPaths = ScrinWaterAttackPaths,
	},
	ScrinAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(8)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Scrin
	},
	ScrinBigAir = {
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 24, Max = 24 }),
		ActiveCondition = function()
			return Mastermind4.IsDead
		end,
		Compositions = {
			normal = {
				{ Aircraft = { PacOrDevastator, PacOrDevastator } },
			},
			hard = {
				{ Aircraft = { PacOrDevastator, PacOrDevastator, PacOrDevastator } },
			},
			vhard = {
				{ Aircraft = { PacOrDevastator, PacOrDevastator, PacOrDevastator } },
			},
			brutal = {
				{ Aircraft = { PacOrDevastator, PacOrDevastator, PacOrDevastator, PacOrDevastator } },
			}
		},
		AttackPaths = ScrinWaterAttackPaths,
	},
}

SetupPlayers = function()
	GDI = Player.GetPlayer("GDI")
	Scrin = Player.GetPlayer("Scrin")
	GDISlaves = Player.GetPlayer("GDISlaves")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { GDI }
	MissionEnemies = { Scrin }
end

WorldLoaded = function()
	SetupPlayers()

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

	if Difficulty == "brutal" then
		AdvComms.Destroy()
	end

	Utils.Do(MissionPlayers, function(p)
		Actor.Create("bdrone.upgrade", true, { Owner = p })
		Actor.Create("mdrone.upgrade", true, { Owner = p })
	end)

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
		if ObjectiveEliminateScrin == nil then
			ObjectiveEliminateScrin = GDI.AddObjective("Destroy the remaining Scrin presence in the area.")
		end
		GDI.MarkCompletedObjective(ObjectiveLiberateBases)
		if ObjectiveMinimiseCasualties ~= nil and EnslavedUnitsKilled <= MaxEnslavedUnitsKilled[Difficulty] then
			GDI.MarkCompletedObjective(ObjectiveMinimiseCasualties)
		end
		UpdateObjectiveText()
		MediaCA.PlaySound(MissionDir .. "/c_destroyscrin.aud", 2)
	end)

	Trigger.OnKilled(Mastermind1, function(self, killer)
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Notification("The first GDI base has been released from Scrin control.")
			MediaCA.PlaySound(MissionDir .. "/c_firstbasereleased.aud", 2)
			if not Mastermind2.IsDead then
				Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(4)), function()
					Notification("The next GDI base is located to the north-east.")
					MediaCA.PlaySound(MissionDir .. "/c_secondbaselocated.aud", 2)
				end)
			end
		end)
	end)

	Trigger.OnKilled(Mastermind2, function(self, killer)
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Notification("The second GDI base has been released from Scrin control.")
			MediaCA.PlaySound(MissionDir .. "/c_secondbasereleased.aud", 2)
			if not Mastermind3.IsDead then
				Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(4)), function()
					Notification("GDI airbase located to the south-east.")
					MediaCA.PlaySound(MissionDir .. "/c_airbaselocated.aud", 2)
				end)
			end
		end)
	end)

	Trigger.OnKilled(Mastermind3, function(self, killer)
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Notification("GDI airbase secured.")
			MediaCA.PlaySound(MissionDir .. "/c_airbasereleased.aud", 2)

			if not Mastermind4.IsDead then
				Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(4)), function()
					Notification("The primary GDI base is located to the south.")
					MediaCA.PlaySound(MissionDir .. "/c_primarybaselocated.aud", 2)
					if not Mastermind5.IsDead then
						Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(4)), function()
							Notification("We have also lost contact with our outpost on the island to the north.")
							MediaCA.PlaySound(MissionDir .. "/c_island.aud", 2)
						end)
					end
				end)
			elseif not Mastermind5.IsDead then
				Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(4)), function()
					Notification("We have also lost contact with our outpost on the island to the north.")
					MediaCA.PlaySound(MissionDir .. "/c_island.aud", 2)
				end)
			end
		end)
	end)

	Trigger.OnKilled(Mastermind4, function(self, killer)
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Notification("The primary GDI base has been released from Scrin control.")
			MediaCA.PlaySound(MissionDir .. "/c_primarybasereleased.aud", 2)
			if not Mastermind1.IsDead or not Mastermind2.IsDead or not Mastermind3.IsDead or not Mastermind5.IsDead then
				Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(4)), function()
					Notification("Eliminate the remaining Masterminds before assaulting the Scrin base.")
					MediaCA.PlaySound(MissionDir .. "/c_remainingmasterminds.aud", 2)
				end)
			end
		end)
	end)

	Trigger.OnKilled(Mastermind5, function(self, killer)
		Notification("Good job getting our EMP Missile launcher back. This should come in very handy.")
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
						Trigger.AfterDelay(1, function()
							if not s.IsDead then
								if s.HasProperty("Move") then
									s.Stop()
								end
								if s.HasProperty("FindResources") then
									s.FindResources()
								end
							end
						end)
					end
				end)

				Trigger.AfterDelay(1, function()
					Actor.Create("QueueUpdaterDummy", true, { Owner = GDI })
				end)

				if m == Mastermind4 then
					Utils.Do(MissionPlayers, function(p)
						Actor.Create("amcv.enabled", true, { Owner = p })
					end)
				end
			end)

			Trigger.OnEnteredProximityTrigger(m.CenterPosition, WDist.New(11 * 1024), function(a, id)
				if IsMissionPlayer(a.Owner) and a.Type ~= "smallcamera" and not m.IsDead and not MastermindsLocated[tostring(m)] then
					MastermindsLocated[tostring(m)] = true
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
				if IsMissionPlayer(killer.Owner) and (self.Owner == GDISlaves or self.Owner == Scrin) then
					EnslavedUnitKilled()
				end
			end)
		end
	end)

	Trigger.OnAnyProduction(function(producer, produced, productionType)
		if produced.Owner == GDI and produced.HasProperty("Move") then
			Trigger.OnKilled(produced, function(self, killer)
				if IsMissionPlayer(killer.Owner) and self.Owner == Scrin then
					EnslavedUnitKilled()
				end
			end)
		end
	end)

	Trigger.AfterDelay(1, function()
		local enslavedHarvesters = GDISlaves.GetActorsByType("harv.td")
		Utils.Do(enslavedHarvesters, function(a)
			if not a.IsDead then
				a.Stop()
			end
		end)
	end)

	SetupReveals({ EntranceReveal1, EntranceReveal2, EntranceReveal3, EntranceReveal4, EntranceReveal5, EntranceReveal6 })
	UpdateObjectiveText()
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
		Scrin.Resources = Scrin.ResourceCapacity - 500

		if not PlayerHasBuildings(Scrin) then
			if ObjectiveEliminateScrin == nil then
				ObjectiveEliminateScrin = GDI.AddObjective("Eliminate the Scrin presence.")
			end
			GDI.MarkCompletedObjective(ObjectiveEliminateScrin)
		end

		if MissionPlayersHaveNoRequiredUnits() then
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
		UpdatePlayerBaseLocations()
	end
end

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % DateTime.Seconds(30) == 0 then
		CalculatePlayerCharacteristics()
	end
end

InitScrin = function()
	Actor.Create("ai.unlimited.power", true, { Owner = GDISlaves })

	AutoRepairAndRebuildBuildings(Scrin, 15)
	SetupRefAndSilosCaptureCredits(Scrin)
	AutoReplaceHarvesters(Scrin)
	AutoRebuildConyards(Scrin)
	InitAiUpgrades(Scrin)
	InitAttackSquad(Squads.ScrinMain, Scrin)
	InitAttackSquad(Squads.ScrinWater, Scrin)
	InitAirAttackSquad(Squads.ScrinAir, Scrin)

	if IsNormalOrAbove() then
		InitAttackSquad(Squads.ScrinBigAir, Scrin)
	end

	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)
end

UpdateObjectiveText = function()
	if not GDI.IsObjectiveCompleted(ObjectiveLiberateBases) then
		local activeMasterminds = Scrin.GetActorsByType("mast")
		local objectiveText = "      Masterminds remaining: " .. #activeMasterminds
		local objectiveTextColor = HSLColor.Yellow

		if IsNormalOrAbove() then
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
