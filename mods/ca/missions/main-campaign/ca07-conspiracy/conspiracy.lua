MissionDir = "ca|missions/main-campaign/ca07-conspiracy"

SleeperAwakenTrigger = { LeftBaseTrigger1.Location, LeftBaseTrigger2.Location, LeftBaseTrigger3.Location, LeftBaseTrigger4.Location, LeftBaseTrigger5.Location, LeftBaseTrigger6.Location, LeftBaseTrigger7.Location, RightBaseTrigger1.Location, RightBaseTrigger2.Location, RightBaseTrigger3.Location, RightBaseTrigger4.Location, RightBaseTrigger5.Location, RightBaseTrigger6.Location, RightBaseTrigger7.Location }

Researchers = { Researcher1, Researcher2 }

GDIDefenders = { GDIDefender1, GDIDefender2, GDIDefender3, GDIDefender4, GDIDefender5, GDIDefender6 }

ChinookDropPaths = {
	{ ChinookDrop1Spawn.Location, ChinookDrop1Landing.Location },
	{ ChinookDrop2Spawn.Location, ChinookDrop2Mid.Location, ChinookDrop2Landing.Location },
	{ ChinookDrop3Spawn.Location, ChinookDrop3Landing.Location },
	{ ChinookDrop4Spawn.Location, ChinookDrop4Landing.Location }
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

ChinookDropStart = AdjustDelayForDifficulty(DateTime.Minutes(12))
ChinookDropAttackValue = AdjustAttackValuesForDifficulty({ Min = 12, Max = 24, RampDuration = DateTime.Minutes(10) })

GDIReinforcementDelay = {
	easy = DateTime.Minutes(17),
	normal = DateTime.Minutes(15),
	hard = DateTime.Minutes(13),
	vhard = DateTime.Minutes(11),
	brutal = DateTime.Minutes(10)
}

-- Squads

if Difficulty == "brutal" then
	table.insert(UnitCompositions.Allied, {
		Infantry = { "hopl", "hopl", "hopl", "hopl", "hopl", "hopl", "hopl", "hopl" }, Vehicles = { "ptnk", "ptnk", "ptnk", "pcan", "pcan", "pcan" }, MinTime = DateTime.Minutes(15), IsSpecial = true
	})
end

AdjustedAlliedCompositions = AdjustCompositionsForDifficulty(UnitCompositions.Allied)

Squads = {
	South = {
		Delay = AdjustDelayForDifficulty(DateTime.Seconds(270)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 36, RampDuration = DateTime.Minutes(14) }),
		FollowLeader = true,
		ProducerActors = { Infantry = { AlliedSouthBarracks }, Vehicles = { AlliedSouthFactory } },
		Compositions = AdjustedAlliedCompositions,
		AttackPaths = GreeceSouthAttackPaths,
	},
	North = {
		Delay = AdjustDelayForDifficulty(DateTime.Seconds(150)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 36, RampDuration = DateTime.Minutes(14) }),
		FollowLeader = true,
		ProducerActors = { Infantry = { AlliedNorthBarracks }, Vehicles = { AlliedNorthFactory } },
		Compositions = AdjustedAlliedCompositions,
		AttackPaths = GreeceNorthAttackPaths,
	},
	Air = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Allied,
	}
}

-- Setup and Tick

SetupPlayers = function()
	Nod = Player.GetPlayer("Nod")
	Greece = Player.GetPlayer("Greece")
	GDI = Player.GetPlayer("GDI")
	Legion = Player.GetPlayer("Legion")
	EvacPlayer = Player.GetPlayer("Evac")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { Nod }
	MissionEnemies = { Greece, GDI }
end

WorldLoaded = function()
	SetupPlayers()

	TimerTicks = 0
	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Nod)
	AdjustPlayerStartingCashForDifficulty()
	InitGDI()
	InitGreece()

	if IsVeryHardOrBelow() then
		WeatherControl.Destroy()

		if IsNormalOrBelow() then
			GDIDefender2.Destroy()
			NorthGapGenerator.Destroy()
			HardOnlyPower.Destroy()
			Cryo1.Destroy()
			Cryo2.Destroy()
			Cryo3.Destroy()
			Cryo4.Destroy()
			Cryo5.Destroy()
			Prism1.Destroy()

			if Difficulty == "easy" then
				GDIDefender1.Destroy()
				SouthGapGenerator1.Destroy()
				SouthGapGenerator2.Destroy()
				SouthGapGenerator3.Destroy()
				HardAndNormalOnlyPower.Destroy()
				AGT1.Owner = Legion
				Prism2.Destroy()
				Prism3.Destroy()
			end
		end
	end

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Tip("Stealth units can be detected by enemy defenses and certain units (see tooltips), as well as all infantry at close range.")
	end)

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

	Trigger.AfterDelay(1, function()
		Utils.Do(Patrols, function(p)
			Utils.Do(p.Units, function(unit)
				if not unit.IsDead then
					unit.Patrol(p.Path, true)
				end
			end)
		end)
	end)

	-- On player reaching GDI base
	Trigger.OnEnteredFootprint(SleeperAwakenTrigger, function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveFootprintTrigger(id)
			AwakenSleeperCell()
		end
	end)

	-- If player tries to go through the Allied base before going to GDI base, begin attacks after a delay
	Trigger.OnEnteredProximityTrigger(BaseTriggerBackup.CenterPosition, WDist.New(10 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveProximityTrigger(id)
			Trigger.AfterDelay(DateTime.Minutes(2), function()
				InitAlliedAttacks()
			end)
		end
	end)

	-- Finding researchers
	Utils.Do(Researchers, function(researcher)
		Trigger.OnEnteredProximityTrigger(researcher.CenterPosition, WDist.New(8 * 1024), function(a, id)
			if IsMissionPlayer(a.Owner) then
				Trigger.RemoveProximityTrigger(id)
				Notification("Researcher located.")
				MediaCA.PlaySound(MissionDir .. "/n_researcherlocated.aud", 2)
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
			if IsMissionPlayer(a.Owner) and a.HasProperty("Move") and not a.HasProperty("Land") then
				Trigger.RemoveProximityTrigger(id)
				researcher.Owner = a.Owner
				Notification("Escort researcher to evacuation point.")
				MediaCA.PlaySound(MissionDir .. "/n_escortresearcher.aud", 2)
				InitEvacSite()
			end
		end)

		Trigger.OnKilled(researcher, function(self, killer)
			if not EvacExiting and ObjectiveRescueResearchers ~= nil and not Nod.IsObjectiveCompleted(ObjectiveRescueResearchers) then
				Nod.MarkFailedObjective(ObjectiveRescueResearchers)
			end
		end)
	end)

	Trigger.OnEnteredProximityTrigger(EvacPoint.CenterPosition, WDist.New(6 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) and (a == Researcher1 or a == Researcher2) then
			a.Owner = EvacPlayer

			if a == Researcher1 then
				a.Move(CPos.New(EvacPoint.Location.X - 1, EvacPoint.Location.Y + 1))
			else
				a.Move(CPos.New(EvacPoint.Location.X + 1, EvacPoint.Location.Y + 1))
			end

			Trigger.AfterDelay(DateTime.Seconds(2), function()
				if Researcher1.Owner == EvacPlayer and Researcher2.Owner == EvacPlayer and not EvacStarted then
					EvacStarted = true

					Notification("Evacuation transport inbound.")
					MediaCA.PlaySound(MissionDir .. "/n_evacinbound.aud", 2)

					Trigger.AfterDelay(DateTime.Seconds(3), function()
						if EvacFlare ~= nil then
							EvacFlare.Destroy()
						end

						Reinforcements.ReinforceWithTransport(EvacPlayer, "tran.evac", nil, { EvacSpawn.Location, EvacPoint.Location }, nil, function(transport, cargo)

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
									EvacExiting = true
									PlaySpeechNotificationToMissionPlayers("TargetRescued")
									t.Move(EvacSpawn.Location)
									t.Destroy()
									Trigger.AfterDelay(DateTime.Seconds(4), function()
										Nod.MarkCompletedObjective(ObjectiveRescueResearchers)
									end)
								end
							end)
						end)
					end)
				else
					Notification("A researcher has reached the evacuation point. Waiting for the second.")
				end
			end)
		end
	end)

	SetupReveals({ EntranceReveal1, EntranceReveal2, EntranceReveal3, EntranceReveal4, EntranceReveal5, EntranceReveal6 })
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
		Greece.Resources = Greece.ResourceCapacity - 500
		GDI.Resources = GDI.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
		end

		if not EvacExiting and MissionPlayersHaveNoRequiredUnits() then
			if ObjectiveRescueResearchers ~= nil and not Nod.IsObjectiveCompleted(ObjectiveRescueResearchers) then
				Nod.MarkFailedObjective(ObjectiveRescueResearchers)
			end
		end

		if not EvacExiting and (Researcher1.IsDead or Researcher2.IsDead) then
			if ObjectiveRescueResearchers ~= nil and not Nod.IsObjectiveCompleted(ObjectiveRescueResearchers) then
				Nod.MarkFailedObjective(ObjectiveRescueResearchers)
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

AwakenSleeperCell = function()
	if SleeperCellAwakened then
		return
	end
	SleeperCellAwakened = true
	Media.DisplayMessage("The time has come, warriors of Nod.", "Nod Soldier", HSLColor.FromHex("FF0000"))
	MediaCA.PlaySound(MissionDir .. "/timehascome.aud", 2)
	Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(4)), function()
		Media.DisplayMessage("Down with GDI!", "Nod Soldier", HSLColor.FromHex("FF0000"))
		MediaCA.PlaySound(MissionDir .. "/downwithgdi.aud", 2)
		Trigger.AfterDelay(15, function()
			UserInterface.SetMissionText("")
			TransferLegionForces()

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
				Actor.Create("QueueUpdaterDummy", true, { Owner = Nod })
			end)

			if ObjectiveRescueResearchers == nil then
				ObjectiveRescueResearchers = Nod.AddObjective("Locate and rescue Nod researchers.")
			end

			if ObjectiveTakeOverBase ~= nil and not Nod.IsObjectiveCompleted(ObjectiveTakeOverBase) then
				Nod.MarkCompletedObjective(ObjectiveTakeOverBase)
			end

			local extraPower1 = Actor.Create("apwr", true, { Owner = Greece, Location = ExtraPower1.Location })
			local extraPower2 = Actor.Create("apwr", true, { Owner = Greece, Location = ExtraPower2.Location })
			AutoRepairBuilding(extraPower1, Greece)
			AutoRebuildBuilding(extraPower1, Greece, 15)
			AutoRepairBuilding(extraPower2, Greece)
			AutoRebuildBuilding(extraPower2, Greece, 15)

			-- Initialise Allied attacks
			InitAlliedAttacks()
		end)
	end)
end

-- overridden in co-op version
TransferLegionForces = function()
	local legionForces = Legion.GetActors()

	Utils.Do(legionForces, function(self)
		if self.Type ~= "player" then
			self.Owner = Nod
		end
	end)
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
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGDIGroundHunterUnit)
	end)
end

InitGreece = function()
	if Difficulty == "easy" then
		RebuildExcludes.Greece = { Types = { "htur", "pris", "gun", "pbox" } }
	end

	AutoRepairAndRebuildBuildings(Greece, 15)
	SetupRefAndSilosCaptureCredits(Greece)
	AutoReplaceHarvesters(Greece)
	AutoRebuildConyards(Greece)
	InitAiUpgrades(Greece)

	local greeceGroundAttackers = Greece.GetGroundAttackers()

	Utils.Do(greeceGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGreeceGroundHunterUnit)
	end)
end

InitAlliedAttacks = function()
	if not AlliedAttacksInitialized then
		AlliedAttacksInitialized = true
		InitAttackSquad(Squads.South, Greece)
		InitAttackSquad(Squads.North, Greece)
		InitAirAttackSquad(Squads.Air, Greece)

		Trigger.AfterDelay(ChinookDropStart, DoChinookDrop)

		Trigger.AfterDelay(GDIReinforcementDelay[Difficulty], function()
			local gdiReinforcements = { "mtnk", "mtnk" }
			if IsHardOrAbove() then
				gdiReinforcements = { "htnk" , "htnk" }
				if Difficulty == "brutal" then
					gdiReinforcements = { "titn" , "titn", "thwk", "thwk" }
				end
			end

			Reinforcements.Reinforce(GDI, gdiReinforcements, GDIReinforcementPath, 75, function(a)
				AssaultPlayerBaseOrHunt(a)
			end)
		end)

		if Difficulty == "brutal" then
			Trigger.AfterDelay(DateTime.Minutes(10), function()
				Actor.Create("ai.superweapons.enabled", true, { Owner = Greece })
			end)
		end
	end
end

DoChinookDrop = function()
	local entryPath
	entryPath = Utils.Random(ChinookDropPaths)
	local chinookDropUnits = { "e1", "e1", "e1", "e3", "e3" }

	if IsHardOrAbove() and DateTime.GameTime > DateTime.Minutes(16) then
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

	local delayUntilNext = CalculateInterval(GetTotalCostOfUnits(chinookDropUnits), ChinookDropAttackValue, ChinookDropStart)
	Trigger.AfterDelay(delayUntilNext, DoChinookDrop)
end
