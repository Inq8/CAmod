MissionDir = "ca|missions/main-campaign/ca24-culmination"

AlliedSavedAdvancedBuildings = {}
USSRSavedAdvancedBuildings = {}
NodSavedAdvancedBuildings = {}

AlliesVsNodPaths = { { AlliesVsNodRally1.Location }, { AlliesVsNodRally2.Location }, { AlliesVsNodRally3.Location }, { AlliesVsNodRally4.Location } }
AlliesVsSovietPaths = { { AlliesVsSovietsRally1.Location }, { AlliesVsSovietsRally2.Location }, { AlliesVsSovietsRally3.Location }, { AlliesVsSovietsRally4.Location } }
SovietsVsNodPaths = { { SovietsVsNodRally1.Location }, { SovietsVsNodRally2.Location }, { SovietsVsNodRally3.Location }, { SovietsVsNodRally4.Location } }
SovietsVsAlliesPaths = { { SovietsVsAlliesRally1.Location }, { SovietsVsAlliesRally2.Location }, { SovietsVsAlliesRally3.Location }, { SovietsVsAlliesRally4.Location } }
NodVsAlliesPaths = { { NodVsAlliesRally1.Location }, { NodVsAlliesRally2.Location }, { NodVsAlliesRally3.Location } }
NodVsSovietPaths = { { NodVsSovietsRally1.Location }, { NodVsSovietsRally2.Location }, { NodVsSovietsRally3.Location } }

NodBaseCameras = { NodBaseCam1, NodBaseCam2, NodBaseCam3 }
AlliedBaseCameras = { AlliedBaseCam1, AlliedBaseCam2 }
SovietBaseCameras = { SovietBaseCam1, SovietBaseCam2, SovietBaseCam3 }

SuperweaponsEnabledTime = {
	easy = DateTime.Seconds((60 * 45) + 17),
	normal = DateTime.Seconds((60 * 30) + 17),
	hard = DateTime.Seconds((60 * 15) + 17),
	vhard = DateTime.Seconds((60 * 10) + 17),
	brutal = DateTime.Seconds((60 * 8) + 17)
}

Squads = {
	Allies = {
		InitTimeAdjustment = -DateTime.Minutes(10),
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(1)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40, RampDuration = DateTime.Minutes(12) }),
		FollowLeader = true,
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Allied),
		AttackPaths = {
			-- set on init
		},
	},
	Nod = {
		InitTimeAdjustment = -DateTime.Minutes(10),
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(1)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40, RampDuration = DateTime.Minutes(12) }),
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Nod),
		AttackPaths = {
			-- set on init
		},
	},
	Soviets = {
		InitTimeAdjustment = -DateTime.Minutes(10),
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(1)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40, RampDuration = DateTime.Minutes(12) }),
		FollowLeader = true,
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Soviet),
		AttackPaths = {
			-- set on init
		},
	},
	AlliedAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(4)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 20 }),
		Compositions = AirCompositions.Allied,
	},
	NodAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(4)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 20 }),
		Compositions = AirCompositions.Nod,
	},
	SovietAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(4)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 20 }),
		Compositions = AirCompositions.Soviet,
	}
}

-- Setup and Tick

SetupPlayers = function()
	Scrin = Player.GetPlayer("Scrin")
	ScrinNoControl = Player.GetPlayer("ScrinNoControl")
	USSR = Player.GetPlayer("USSR")
	Greece = Player.GetPlayer("Greece")
	Nod = Player.GetPlayer("Nod")
	MissionPlayers = { Scrin }
	MissionEnemies = { USSR, Greece, Nod }
end

WorldLoaded = function()
	SetupPlayers()

	TimerTicks = 0
	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Scrin)
	AdjustPlayerStartingCashForDifficulty()

	ObjectiveInitialSubjugation = Scrin.AddObjective("Subjugate one of the three human bases.")

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
		if IsMissionPlayer(a.Owner) and a.Type == "subjugation.dummy" then
			Trigger.RemoveProximityTrigger(id)

			if not Scrin.IsObjectiveCompleted(ObjectiveInitialSubjugation) then
				ObjectiveSubjugateRemaining = Scrin.AddObjective("Capture Nod and Soviet Construction Yards.")
				Scrin.MarkCompletedObjective(ObjectiveInitialSubjugation)
				DestroyCameras()
				WarpOutProdigy()
				InitUSSR(SovietsVsAlliesPaths, AlliedBaseCameras)
				InitNod(NodVsAlliesPaths, AlliedBaseCameras)
				CreateWormholes(AlliedBase.Location)
				Trigger.AfterDelay(1, function()
					Actor.Create("QueueUpdaterDummy", true, { Owner = Scrin })
				end)
				SetupMainObjectives({ NodConyard, SovietConyard })
				if IsHardOrAbove() then
					Trigger.AfterDelay(DateTime.Minutes(1), function()
						local alliedGroundAttackers = Utils.Where(Greece.GetGroundAttackers(), IsGreeceGroundHunterUnit)
						Utils.Do(alliedGroundAttackers, IdleHunt)
					end)
				end
			end
		end
	end)

	Trigger.OnEnteredProximityTrigger(SovietBase.CenterPosition, WDist.New(14 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) and a.Type == "subjugation.dummy" then
			Trigger.RemoveProximityTrigger(id)
			if not Scrin.IsObjectiveCompleted(ObjectiveInitialSubjugation) then
				ObjectiveSubjugateRemaining = Scrin.AddObjective("Capture Allied and Nod Construction Yards.")
				Scrin.MarkCompletedObjective(ObjectiveInitialSubjugation)
				DestroyCameras()
				WarpOutProdigy()
				InitGreece(AlliesVsSovietPaths, SovietBaseCameras)
				InitNod(NodVsSovietPaths, SovietBaseCameras)
				CreateWormholes(SovietBase.Location)
				Trigger.AfterDelay(1, function()
					Actor.Create("QueueUpdaterDummy", true, { Owner = Scrin })
				end)
				SetupMainObjectives({ NodConyard, AlliedConyard })
				if IsHardOrAbove() then
					Trigger.AfterDelay(DateTime.Minutes(1), function()
						local ussrGroundAttackers = Utils.Where(USSR.GetGroundAttackers(), IsUSSRGroundHunterUnit)
						Utils.Do(ussrGroundAttackers, IdleHunt)
					end)
				end
			end
		end
	end)

	Trigger.OnEnteredProximityTrigger(NodBase.CenterPosition, WDist.New(14 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) and a.Type == "subjugation.dummy" then
			Trigger.RemoveProximityTrigger(id)
			if not Scrin.IsObjectiveCompleted(ObjectiveInitialSubjugation) then
				ObjectiveSubjugateRemaining = Scrin.AddObjective("Capture Allied and Soviet Construction Yards.")
				Scrin.MarkCompletedObjective(ObjectiveInitialSubjugation)
				DestroyCameras()
				WarpOutProdigy()
				InitUSSR(SovietsVsNodPaths, NodBaseCameras)
				InitGreece(AlliesVsNodPaths, NodBaseCameras)
				CreateWormholes(NodBase.Location)
				Trigger.AfterDelay(1, function()
					Actor.Create("QueueUpdaterDummy", true, { Owner = Scrin })
				end)
				SetupMainObjectives({ SovietConyard, AlliedConyard })
				if IsHardOrAbove() then
					Trigger.AfterDelay(DateTime.Minutes(1), function()
						local nodGroundAttackers = Utils.Where(Nod.GetGroundAttackers(), IsNodGroundHunterUnit)
						Utils.Do(nodGroundAttackers, IdleHunt)
					end)
				end
			end
		end
	end)

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

		if MissionPlayersHaveNoRequiredUnits() then
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

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % DateTime.Seconds(30) == 0 then
		CalculatePlayerCharacteristics()
	end
end

-- Functions

InitUSSR = function(paths, cameras)
	Squads.Soviets.AttackPaths = paths

	InitAttackSquad(Squads.Soviets, USSR)
	InitAirAttackSquad(Squads.SovietAir, USSR)

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
			AutoRebuildConyards(USSR)
			InitAiUpgrades(USSR)
		end)
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

	InitAttackSquad(Squads.Allies, Greece)
	InitAirAttackSquad(Squads.AlliedAir, Greece)

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
			AutoRebuildConyards(Greece)
			InitAiUpgrades(Greece)
		end)
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

	InitAttackSquad(Squads.Nod, Nod)
	InitAirAttackSquad(Squads.NodAir, Nod)

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
			AutoRebuildConyards(Nod)
			InitAiUpgrades(Nod)
		end)
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
			if not IsMissionPlayer(self.Owner) then
				Scrin.MarkFailedObjective(ObjectiveSubjugateRemaining)
			end
		end)
	end)
end

WarpOutProdigy = function()
	Prodigy.Owner = ScrinNoControl
	Prodigy.Stop()
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Notification("The Prodigy will now leave the battlefield to recuperate.")
		local prodigyExitWormhole = Actor.Create("wormhole.exit", true, { Owner = ScrinNoControl, Location = ProdigyExit.Location })
		Prodigy.Move(prodigyExitWormhole.Location)
		Trigger.OnEnteredFootprint({ ProdigyExit.Location }, function(a, id)
			if a == Prodigy then
				Trigger.RemoveFootprintTrigger(id)
				Prodigy.Destroy()
				Trigger.AfterDelay(DateTime.Seconds(2), function()
					if not prodigyExitWormhole.IsDead then
						prodigyExitWormhole.Kill()
					end
				end)
			end
		end)
		Trigger.AfterDelay(DateTime.Seconds(20), function()
			if not Prodigy.IsDead then
				Prodigy.Destroy()
			end
			if not prodigyExitWormhole.IsDead then
				prodigyExitWormhole.Kill()
			end
		end)
	end)
end
