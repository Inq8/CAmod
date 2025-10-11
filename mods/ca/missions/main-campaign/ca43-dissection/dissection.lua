MissionDir = "ca|missions/main-campaign/ca43-dissection"

SuperweaponsEnabledTime = {
	easy = DateTime.Minutes(60),
	normal = DateTime.Minutes(40),
	hard = DateTime.Minutes(30),
	vhard = DateTime.Minutes(20),
	brutal = DateTime.Minutes(15)
}

GradReplacementDelay = {
	easy = DateTime.Seconds(360),
	normal = DateTime.Seconds(240),
	hard = DateTime.Seconds(120),
	vhard = DateTime.Seconds(80),
	brutal = DateTime.Seconds(60)
}

MaxBomberTargets = {
	easy = 2,
	normal = 3,
	hard = 4,
	vhard = 5,
	brutal = 6
}

BombingRunInterval = {
	easy = DateTime.Minutes(6),
	normal = DateTime.Minutes(5),
	hard = DateTime.Minutes(4),
	vhard = DateTime.Minutes(3),
	brutal = DateTime.Minutes(2)
}

AllDomesDisabled = false
DomesDisabled = {}

SovietRallyPoints = { EastRally1, EastRally2, EastRally3, EastRally4, EastRally5, WestRally1, WestRally2, WestRally3 }

NextGradReplacementTime = 0

SovietAttackPaths = function(squad)
	local paths = {}
	local conyards = GetMissionPlayersActorsByTypes({ "fact", "mcv" })
	for _, conyard in ipairs(conyards) do
		local rallyPoints = Map.ActorsInCircle(conyard.CenterPosition, WDist.New(36 * 1024), function(a) return a.Type == "waypoint" end)
		for _, rp in ipairs(rallyPoints) do
			for _, srp in ipairs(SovietRallyPoints) do
				if rp == srp then
					table.insert(paths, { srp.Location })
					break
				end
			end
		end
	end
	return paths
end

Squads = {
	Main = {
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Soviet),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 40, Max = 80 }),
		FollowLeader = true,
		AttackPaths = SovietAttackPaths,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(1)),
		ProducerActors = { Infantry = { SouthBarracks, NorthBarracks }, Vehicle = { SouthFactory, NorthFactory } },
	},
	Air = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(12)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Soviet,
	}
}

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	Scrin = Player.GetPlayer("Scrin")
	England = Player.GetPlayer("England")
	Neutral = Player.GetPlayer("Neutral")
	TicksUntilBombingRun = BombingRunInterval[Difficulty]

	MissionPlayers = { Greece }

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Greece)
	AdjustPlayerStartingCashForDifficulty()
	RemoveActorsBasedOnDifficultyTags()
	InitUSSR()
	SetupChurchMoneyCrates(Neutral)

	ObjectiveEliminateSoviets = Greece.AddObjective("Eliminate the Soviet presence.")
	ObjectiveNeutralizeDomes = Greece.AddSecondaryObjective("Neutralize Soviet Radar Domes.")

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
		Notification("Reinforcements have arrived.")
		Reinforcements.Reinforce(Greece, { "lst.mcv" }, { ReinforcementSpawn.Location, ReinforcementDest.Location }, 75)
		Beacon.New(Greece, ReinforcementDest.CenterPosition)
	end)

	InitialRadar = Actor.Create("radar.dummy", true, { Owner = Greece })

	Trigger.OnKilled(EnglandDome, function(self, killer)
		InitialRadar.Destroy()
	end)

	Trigger.OnEnteredProximityTrigger(WestBaseCenter.CenterPosition, WDist.New(24 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveProximityTrigger(id)
			AssumeControl()
		end
	end)

	Trigger.OnEnteredProximityTrigger(EastBaseCenter.CenterPosition, WDist.New(24 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveProximityTrigger(id)
			AssumeControl()
		end
	end)

	SovietDomes = USSR.GetActorsByType("dome")

	Utils.Do(SovietDomes, function(d)
		Trigger.OnKilled(d, function(self, killer)
			DomeDisabled(d)
		end)
		Trigger.OnInfiltrated(d, function(self, infiltrator)
			if IsMissionPlayer(infiltrator.Owner) then
				DomeDisabled(d)
			end
		end)
	end)

	local power = USSR.GetActorsByTypes({"powr", "apwr", "tpwr"})
	Utils.Do(power, function(p)
		Trigger.OnInfiltrated(p, function(self, infiltrator)
			if not p.IsDead and IsMissionPlayer(infiltrator.Owner) then
				p.Sell()
			end
		end)
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
		USSR.Resources = USSR.ResourceCapacity - 500

		if not PlayerHasBuildings(USSR) then
			Greece.MarkCompletedObjective(ObjectiveEliminateSoviets)
		end

		if MissionPlayersHaveNoRequiredUnits() then
			Greece.MarkFailedObjective(ObjectiveEliminateSoviets)
		end

		if AssumedControl and not AllDomesDisabled then
			if TicksUntilBombingRun > 0 then
				if USSR.PowerState == "Normal" then
					TicksUntilBombingRun = TicksUntilBombingRun - 25
				end
			else
				TicksUntilBombingRun = BombingRunInterval[Difficulty]
				InitBombingRun()
			end

			UpdateMissionText()
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()
	end
end

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 750 == 0 then
		CalculatePlayerCharacteristics()
	end
end

InitUSSR = function()
	RebuildExcludes.USSR = { Types = { "dome", "apwr", "powr" } }

	AutoRepairAndRebuildBuildings(USSR, 10)
	SetupRefAndSilosCaptureCredits(USSR)
	AutoReplaceHarvesters(USSR)
	AutoRebuildConyards(USSR)
	InitAiUpgrades(USSR)
	InitAttackSquad(Squads.Main, USSR)
	InitAirAttackSquad(Squads.Air, USSR)

	Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = USSR })
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = USSR })
	end)

	local ussrGroundAttackers = USSR.GetGroundAttackers()
	Utils.Do(ussrGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsUSSRGroundHunterUnit)
	end)

	local grads = USSR.GetActorsByType("grad.defender")
	Utils.Do(grads, function(a)
		GradReplacementTrigger(a, a.Location)
	end)
end

UpdateMissionText = function()
	local color = HSLColor.Yellow
	if TimerPushedBack then
		color = HSLColor.Red
		TimerPushedBack = false
	end

	if not AllDomesDisabled then
		UserInterface.SetMissionText("Soviet bombing run ETA " .. UtilsCA.FormatTimeForGameSpeed(TicksUntilBombingRun), color)
	else
		UserInterface.SetMissionText("")
	end
end

GradReplacementTrigger = function(grad, loc)
	Trigger.OnKilled(grad, function(self, killer)
		if not SouthFactory.IsDead and SouthFactory.Owner == USSR then
			local nextReplacementDelay = GradReplacementDelay[Difficulty]

			if NextGradReplacementTime > DateTime.GameTime then
				nextReplacementDelay = nextReplacementDelay + NextGradReplacementTime - DateTime.GameTime
			end

			NextGradReplacementTime = DateTime.GameTime + nextReplacementDelay

			Trigger.AfterDelay(nextReplacementDelay, function()
				local newGrad = Reinforcements.Reinforce(USSR, { "grad.defender" }, { GradSpawn.Location, loc })[1]
				GradReplacementTrigger(newGrad, loc)
			end)
		end
	end)
end

DestroyFlares = function()
	if not WestFlare.IsDead then
		WestFlare.Destroy()
	end
	if not EastFlare.IsDead then
		EastFlare.Destroy()
	end
end

AssumeControl = function()
	if AssumedControl then
		return
	end

	AssumedControl = true

	Notification("Command transfer complete.")
	MediaCA.PlaySound(MissionDir .. "/r_transfer.aud", 2)

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		DestroyFlares()
		local actorsToFlip = Utils.Where(England.GetActors(), function(a) return a.Type ~= "player" end)

		Utils.Do(actorsToFlip, function(a) a.Owner = Greece end)

		Trigger.AfterDelay(1, function()
			Actor.Create("QueueUpdaterDummy", true, { Owner = Greece })
		end)
	end)
end

InitBombingRun = function()
	local delay = 1
	local primaryTargets = GetMissionPlayersActorsByTypes({ "proc", "fact", "pdox", "weat" })
	local rightPrimaryTargets = {}
	local leftPrimaryTargets = {}
	local targets = {}
	local usedXCoords = {}
	local leftMinX = 2
	local leftMaxX = 50
	local rightMinX = 94
	local rightMaxX = 138
	local targetSide

	Utils.Do(primaryTargets, function(b)
		if b.Location.X > rightMinX then
			table.insert(rightPrimaryTargets, b)
		elseif b.Location.X < leftMaxX then
			table.insert(leftPrimaryTargets, b)
		end
	end)

	if math.abs(#leftPrimaryTargets - #rightPrimaryTargets) < 4 then
		targets = Utils.Concat(leftPrimaryTargets, rightPrimaryTargets)
		targetSide = "both"
	elseif #leftPrimaryTargets > #rightPrimaryTargets then
		targets = leftPrimaryTargets
		targetSide = "left"
	elseif #rightPrimaryTargets > #leftPrimaryTargets then
		targets = rightPrimaryTargets
		targetSide = "right"
	end

	local maxTargets = MaxBomberTargets[Difficulty]

	if IsVeryHardOrAbove() and DateTime.GameTime > DateTime.Minutes(20) then
		maxTargets = maxTargets + 1
	end

	if IsVeryHardOrAbove() and DateTime.GameTime > DateTime.Minutes(30) then
		maxTargets = maxTargets + 1
	end

	if IsVeryHardOrAbove() and DateTime.GameTime > DateTime.Minutes(40) then
		maxTargets = maxTargets + 1
	end

	targets = Utils.Take(math.min(maxTargets, #primaryTargets), Utils.Shuffle(primaryTargets))

	if #targets < maxTargets and DateTime.GameTime > DateTime.Minutes(20) then
		local secondaryTargets = GetMissionPlayersActorsByTypes({ "pbox", "gun", "pris", "htur" })
		secondaryTargets = Utils.Shuffle(secondaryTargets)

		for _, t in ipairs(secondaryTargets) do
			table.insert(targets, t)
			if #targets >= maxTargets then
				break
			end
		end
	end

	if #targets > 0 then
		Notification("Warning, bombing run incoming.")
		MediaCA.PlaySound(MissionDir .. "/r_bombingrun.aud", 2)

		Utils.Do(targets, function(t)
			Trigger.AfterDelay(delay, function()
				if not t.IsDead then
					local x = t.Location.X + 1

					if usedXCoords[x] then
						if targetSide == "both" then
							targetSide = Utils.Random({ "left", "right" })
						end
						if targetSide == "left" then
							x = Utils.RandomInteger(leftMinX, leftMaxX)
						else
							x = Utils.RandomInteger(rightMinX, rightMaxX)
						end
					end

					usedXCoords[x] = true
					usedXCoords[x - 1] = true
					usedXCoords[x + 1] = true

					local entry = CPos.New(x, 0)
					local exit = CPos.New(x, 160)

					Reinforcements.Reinforce(USSR, { "badr.carpet" }, { entry, exit }, 25, function(self)
						self.Destroy()
					end)
				end
			end)
			delay = delay + DateTime.Seconds(2)
		end)
	end
	Trigger.AfterDelay(BombingRunInterval[Difficulty], InitBombingRun)
end

DomeDisabled = function(d)
	local actorId = tostring(d)

	if DomesDisabled[actorId] then
		return
	end

	DomesDisabled[actorId] = true

	if not d.IsDead then
		d.GrantCondition("powerdown")
	end

	local numDomesDisabled = 0
	for _ in pairs(DomesDisabled) do
		numDomesDisabled = numDomesDisabled + 1
	end

	if numDomesDisabled >= #SovietDomes then
		AllDomesDisabled = true
		Greece.MarkCompletedObjective(ObjectiveNeutralizeDomes)
	end

	TicksUntilBombingRun = TicksUntilBombingRun + DateTime.Minutes(1)
	BombingRunInterval[Difficulty] = BombingRunInterval[Difficulty] + DateTime.Seconds(20)

	if AssumedControl then
		TimerPushedBack = true
		UpdateMissionText()
	end
end
