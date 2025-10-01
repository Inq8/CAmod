MissionDir = "ca|missions/main-campaign/ca43-dissection"

SuperweaponsEnabledTime = {
	easy = DateTime.Minutes(40),
	normal = DateTime.Minutes(25),
	hard = DateTime.Minutes(15),
	vhard = DateTime.Minutes(10),
	brutal = DateTime.Minutes(10)
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
	easy = DateTime.Minutes(8),
	normal = DateTime.Minutes(6),
	hard = DateTime.Minutes(4),
	vhard = DateTime.Minutes(3),
	brutal = DateTime.Minutes(2)
}

SovietRallyPoints = { EastRally1, EastRally2, EastRally3, EastRally4, EastRally5, WestRally1, WestRally2, WestRally3 }

NextGradReplacementTime = 0

SovietAttackPaths = function(squad)
	local paths = {}
	local conyards = Greece.GetActorsByTypes({ "fact", "mcv" })
	for _, conyard in ipairs(conyards) do
		local rallyPoints = Map.ActorsInCircle(conyard.CenterPosition, WDist.New(30 * 1024), function(a) return a.Type == "waypoint" end)
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

	MissionPlayers = { Greece }

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Greece)
	AdjustPlayerStartingCashForDifficulty()
	RemoveActorsBasedOnDifficultyTags()
	InitUSSR()
	SetupChurchMoneyCrates(Neutral)

	ObjectiveEliminateSoviets = Greece.AddObjective("Eliminate the Soviet presence.")

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

	Trigger.OnEnteredProximityTrigger(WestBaseCenter.CenterPosition, WDist.New(11 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveProximityTrigger(id)
			AssumeControl()
		end
	end)

	Trigger.OnEnteredProximityTrigger(EastBaseCenter.CenterPosition, WDist.New(11 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveProximityTrigger(id)
			AssumeControl()
		end
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	OncePerThirtySecondChecks()
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

	Trigger.AfterDelay(BombingRunInterval[Difficulty], function()
		InitBombingRun()
	end)
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
	local keyBuildings = Greece.GetActorsByTypes({ "proc", "fact" })
	local targets
	local rightTargets = {}
	local leftTargets = {}
	local usedXCoords = {}
	local leftMinX = 2
	local leftMaxX = 50
	local rightMinX = 94
	local rightMaxX = 138
	local targetSide

	Utils.Do(keyBuildings, function(b)
		if b.Location.X > rightMinX then
			table.insert(rightTargets, b)
		elseif b.Location.X < leftMaxX then
			table.insert(leftTargets, b)
		end
	end)

	if math.abs(#leftTargets - #rightTargets) < 4 then
		targets = Utils.Concat(leftTargets, rightTargets)
		targetSide = "both"
	elseif #leftTargets > #rightTargets then
		targets = leftTargets
		targetSide = "left"
	elseif #rightTargets > #leftTargets then
		targets = rightTargets
		targetSide = "right"
	end

	targets = Utils.Take(math.min(MaxBomberTargets[Difficulty], #targets), Utils.Shuffle(targets))

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
