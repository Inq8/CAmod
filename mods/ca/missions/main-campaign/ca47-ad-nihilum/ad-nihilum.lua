MissionDir = "ca|missions/main-campaign/ca47-ad-nihilum"

SuperweaponsEnabledTime = {
	easy = DateTime.Minutes(60),
	normal = DateTime.Minutes(40),
	hard = DateTime.Minutes(25),
	vhard = DateTime.Minutes(18),
	brutal = DateTime.Minutes(15)
}

GatewayLocations = {
	Left = LeftGateway.Location,
	Right = RightGateway.Location,
	Middle = MiddleGateway.Location
}

VoidEnginePaths = {
	Left = { LeftPath1.Location, LeftPath2.Location, LeftPath3.Location, LeftPath4.Location, LeftPath5.Location, LeftPath6.Location, LeftPath7.Location, },
	Right = { RightPath1.Location, RightPath2.Location, RightPath3.Location, RightPath4.Location, RightPath5.Location, RightPath6.Location, RightPath7.Location },
	Middle = { MiddlePath1.Location, MiddlePath2.Location, MiddlePath3.Location, MiddlePath4.Location }
}

VoidEngineStartTime = {
	easy = DateTime.Minutes(10),
	normal = DateTime.Minutes(8),
	hard = DateTime.Minutes(6),
	vhard = DateTime.Minutes(6),
	brutal = DateTime.Minutes(6)
}

VoidEngineInterval = {
	easy = DateTime.Minutes(7),
	normal = DateTime.Minutes(6),
	hard = DateTime.Minutes(5),
	vhard = DateTime.Minutes(4),
	brutal = DateTime.Minutes(3) + DateTime.Seconds(20)
}

VoidEngineAttackCount = {
	easy = 2,
	normal = 4,
	hard = 8,
	vhard = 50,
	brutal = 50
}

LeftAttackPaths = {
	{ LeftPath2.Location, LeftPath3.Location, LeftPath4.Location, LeftPath5.Location },
	{ LeftPath2.Location, LeftFlankPath1.Location, LeftFlankPath2a.Location, LeftFlankPath3.Location },
	{ LeftPath2.Location, LeftFlankPath1.Location, LeftFlankPath2b.Location, LeftFlankPath3.Location },
	{ LeftPath2.Location, LeftPath3.Location, LeftMiddlePath1.Location },
}

RightAttackPaths = {
	{ MiddlePath1.Location, MiddlePath2.Location, LeftMiddlePath1.Location },
	{ MiddlePath1.Location, MiddlePath2.Location, MiddlePath3.Location },
	{ RightPath4.Location, RightPath5.Location, RightPath6.Location },
	{ RightFlankPath1.Location, RightFlankPath2.Location, RightFlankPath3.Location },
}

table.insert(UnitCompositions.Scrin, {
	Infantry = { "s1", "stlk", "s1", "s1", "s1", "stlk", "stlk", "s1", "s1", "stlk", "s1", "s1", "s1", "stlk", "stlk", "stlk", "s1", "s1", "s1" },
	Vehicles = { "dark", "gunw", "dark", "dark", "gunw", "dark" },
	Aircraft = { PacOrDevastator, "pac" },
	MinTime = DateTime.Minutes(17)
})

Squads = {
	Left = {
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		FollowLeader = true,
		AttackPaths = LeftAttackPaths,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(2)),
		ProducerActors = { Infantry = { LeftPortal }, Vehicles = { LeftWarpSphere }, Aircraft = { LeftGrav } }
	},
	Right = {
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		FollowLeader = true,
		AttackPaths = RightAttackPaths,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(4)),
		ProducerActors = { Infantry = { MiddlePortal, RightPortal }, Vehicles = { MiddleWarpSphere, RightWarpSphere }, Aircraft = { MiddleGrav1, MiddleGrav2, MiddleGrav3, RightGrav } }
	},
	Air = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(12)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Scrin,
	},
	AirToAir = AirToAirSquad({ "stmr", "enrv", "torm" }, AdjustAirDelayForDifficulty(DateTime.Minutes(10))),
}

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	MaleficScrin = Player.GetPlayer("MaleficScrin")
	Neutral = Player.GetPlayer("Neutral")

	MissionPlayers = { Greece }

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Greece)
	AdjustPlayerStartingCashForDifficulty()
	RemoveActorsBasedOnDifficultyTags()
	InitMaleficScrin()

	ObjectiveDestroyScrinBases = Greece.AddObjective("Destroy all Scrin bases.")
	ObjectiveStopVoidEngines = Greece.AddObjective("Prevent Void Engines from breaking through.")

	Trigger.AfterDelay(DateTime.Seconds(4), function()
		Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
		Notification("Reinforcements have arrived.")
		Reinforcements.Reinforce(Greece, { "mcv" }, { McvSpawn1.Location, McvDest1.Location }, 75)
		Reinforcements.Reinforce(Greece, { "mcv" }, { McvSpawn2.Location, McvDest2.Location }, 75)
	end)

	Trigger.AfterDelay(VoidEngineStartTime[Difficulty], function()
		NextVoidEngineIndex = 1
		VoidEngineInitialPaths = Utils.Random({
			{ "Left", "Right", "Left", "Middle" },
			{ "Right", "Left", "Right", "Middle" }
		})

		SendNextVoidEngine()
	end)

	local productionBuildings = MaleficScrin.GetActorsByTypes({ "port", "wsph", "airs", "afac" })
	for _, b in pairs(productionBuildings) do
		SellOnCaptureAttempt(b)
	end

	local voidEngineExit = {}
	for x = 84, 132 do
		table.insert(voidEngineExit, CPos.New(x, 1))
	end

	Trigger.OnEnteredFootprint(voidEngineExit, function(a, id)
		if a.Type == "veng" then
			a.Destroy()
			Notification("A Void Engine has broken through.")
			Media.PlaySoundNotification(Greece, "AlertBuzzer")
			Greece.MarkFailedObjective(ObjectiveStopVoidEngines)
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
		MaleficScrin.Resources = MaleficScrin.ResourceCapacity - 500

		if MissionPlayersHaveNoRequiredUnits() then
			Greece.MarkFailedObjective(ObjectiveDestroyScrinBases)
			Greece.MarkFailedObjective(ObjectiveStopVoidEngines)
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

InitMaleficScrin = function()
	AutoRepairAndRebuildBuildings(MaleficScrin, 10)
	SetupRefAndSilosCaptureCredits(MaleficScrin)
	AutoReplaceHarvesters(MaleficScrin)
	AutoRebuildConyards(MaleficScrin)
	InitAiUpgrades(MaleficScrin)
	InitAttackSquad(Squads.Left, MaleficScrin)
	InitAttackSquad(Squads.Right, MaleficScrin)
	InitAirAttackSquad(Squads.Air, MaleficScrin)

	if IsHardOrAbove() then
		InitAirAttackSquad(Squads.AirToAir, MaleficScrin, MissionPlayers, { "Aircraft" }, "ArmorType")
	end

	Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = MaleficScrin })
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = MaleficScrin })
	end)

	local maleficScrinGroundAttackers = MaleficScrin.GetGroundAttackers()
	Utils.Do(maleficScrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)

	local productionBuildings = MaleficScrin.GetActorsByTypes({ "port", "wsph", "sfac", "grav" })
	for _, b in pairs(productionBuildings) do
		SellOnCaptureAttempt(b)
	end
end

SendNextVoidEngine = function()
	if NextVoidEngineIndex <= VoidEngineAttackCount[Difficulty] then
		MediaCA.PlaySound("veng-spawn.aud", 2)

		Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(2)), function()
			Notification("Alert. Void Engine detected.")
			MediaCA.PlaySound(MissionDir .. "/r_vengdet.aud", 2)
		end)

		local path
		local spawnLoc

		if NextVoidEngineIndex <= #VoidEngineInitialPaths then
			path = VoidEnginePaths[VoidEngineInitialPaths[NextVoidEngineIndex]]
			spawnLoc = GatewayLocations[VoidEngineInitialPaths[NextVoidEngineIndex]]
		else
			pathName = Utils.Random({ "Left", "Right", "Middle" })
			path = VoidEnginePaths[pathName]
			spawnLoc = GatewayLocations[pathName]
		end

		local reinforcements = Reinforcements.Reinforce(MaleficScrin, { "veng" }, { spawnLoc }, 10, function(a)
			Utils.Do(path, function(loc)
				a.Move(loc)
			end)

			a.GrantCondition("difficulty-" .. Difficulty)

			Trigger.OnIdle(a, function(self)
				if not self.IsDead then
					self.AttackMove(path[#path])
				end
			end)

			Trigger.OnDamaged(a, function(self, attacker, damage)
				if IsMissionPlayer(attacker.Owner) and self.Health < self.MaxHealth / 3 * 2 then
					self.Stop()
					self.AttackMove(path[#path])
				end
			end)

			if Difficulty ~= "brutal" then
				Trigger.AfterDelay(DateTime.Seconds(30), function()
					if not a.IsDead then
						Beacon.New(Greece, a.CenterPosition)
						Media.PlaySound("beacon.aud")
					end
				end)
			end
		end)

		NextVoidEngineIndex = NextVoidEngineIndex + 1

		Trigger.AfterDelay(VoidEngineInterval[Difficulty], function()
			SendNextVoidEngine()
		end)
	end
end
