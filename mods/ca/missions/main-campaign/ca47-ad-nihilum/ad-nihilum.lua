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
	Right = { RightPath1.Location, RightPath2.Location, RightPath3.Location, RightPath4.Location, RightPath5.Location, RightPath6a.Location, RightPath7.Location },
	Middle = { MiddlePath1.Location, HighwayPath1.Location, HighwayExit.Location }
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

VoidEngineRevealDelay = {
	easy = DateTime.Seconds(5),
	normal = DateTime.Minutes(1),
	hard = DateTime.Minutes(2),
	vhard = DateTime.Minutes(3),
	brutal = DateTime.Minutes(3) + DateTime.Seconds(30)
}

LeftAttackPaths = {
	{ LeftPath2.Location, LeftPath3.Location, LeftPath4.Location, LeftPath5.Location },
	{ LeftPath2.Location, LeftPath3.Location, LeftMiddlePath1.Location },
	{ LeftPath2.Location, LeftPath3.Location, LeftMiddlePath1.Location },
}

LeftFlankPaths = {
	{ LeftPath2.Location, LeftFlankPath1.Location, LeftFlankPath2a.Location, LeftFlankPath3.Location },
	{ LeftPath2.Location, LeftFlankPath1.Location, LeftFlankPath2b.Location, LeftFlankPath3.Location },
}

RightAttackPaths = {
	{ MiddlePath1.Location, MiddlePath2.Location, LeftMiddlePath1.Location },
	{ MiddlePath1.Location, MiddlePath2.Location, MiddlePath3.Location },
	{ RightPath4.Location, RightPath5.Location, RightPath6a.Location },
	{ RightPath4.Location, RightPath5.Location, RightPath6b.Location },
}

RightFlankPaths = {
	{ RightFlankPath1.Location, RightFlankPath2.Location, RightFlankPath3.Location },
}

AggrodVoidEngines = {}
BaseAttackVoidEngines = {}
AnathemaVoidEngines = {}

table.insert(UnitCompositions.Scrin, {
	Infantry = { "s1", "stlk", "s1", "s1", "s1", "stlk", "stlk", "s1", "s1", "stlk", "s1", "s1", "s1", "stlk", "stlk", "stlk", "s1", "s1", "s1" },
	Vehicles = { "dark", "gunw", "dark", "dark", "gunw", "dark" },
	Aircraft = { PacOrDevastator, "pac" },
	MinTime = DateTime.Minutes(17)
})

if IsVeryHardOrAbove() then
	table.insert(UnitCompositions.Scrin, {
		Vehicles = { "lace", "lace", "lace", "lace", "lace", "lace", "lace", "lace", "lace", "lace", "lace", "lace", "lace", "lace", "lace" },
		MinTime = DateTime.Minutes(14),
		IsSpecial = true
	})
end

Squads = {
	Left = {
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		FollowLeader = true,
		AttackPaths = function(squad)
			if DateTime.GameTime > DateTime.Minutes(12) then
				return Utils.Concat(LeftAttackPaths, LeftFlankPaths)
			else
				return LeftAttackPaths
			end
		end,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(2)),
		ProducerActors = { Infantry = { LeftPortal }, Vehicles = { LeftWarpSphere }, Aircraft = { LeftGrav } }
	},
	Right = {
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		FollowLeader = true,
		AttackPaths = function(squad)
			if DateTime.GameTime > DateTime.Minutes(14) then
				return Utils.Concat(RightAttackPaths, RightFlankPaths)
			else
				return RightAttackPaths
			end
		end,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(4)),
		ProducerActors = { Infantry = { MiddlePortal, RightPortal }, Vehicles = { MiddleWarpSphere, RightWarpSphere }, Aircraft = { MiddleGrav1, MiddleGrav2, MiddleGrav3, RightGrav } }
	},
	Air = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(12)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Scrin,
	},
	AirToAir = AirToAirSquad(
		{ "stmr", "enrv", "torm" },
		AdjustAirDelayForDifficulty(DateTime.Minutes(10)),
		function(a)
			a.Patrol({ GatewayLocations.Left, GatewayLocations.Middle, GatewayLocations.Right, GatewayLocations.Middle })
		end
	),
}

SetupPlayers = function()
	Greece = Player.GetPlayer("Greece")
	MaleficScrin = Player.GetPlayer("MaleficScrin")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { Greece }
	MissionEnemies = { MaleficScrin }
end

WorldLoaded = function()
	SetupPlayers()

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Greece)
	AdjustPlayerStartingCashForDifficulty()
	RemoveActorsBasedOnDifficultyTags()
	InitMaleficScrin()

	ObjectiveDestroyScrinBases = Greece.AddObjective("Destroy all Scrin bases.")
	ObjectiveStopVoidEngines = Greece.AddObjective("Prevent Void Engines from breaking through.")

	Trigger.AfterDelay(DateTime.Seconds(4), function()
		Media.PlaySpeechNotification(nil, "ReinforcementsArrived")
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

	local productionBuildings = MaleficScrin.GetActorsByTypes({ "port", "wsph", "airs", "sfac" })
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
			Media.PlaySoundNotification(nil, "AlertBuzzer")
			Greece.MarkFailedObjective(ObjectiveStopVoidEngines)
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
		MaleficScrin.Resources = MaleficScrin.ResourceCapacity - 500

		if not PlayerHasBuildings(MaleficScrin) then
			Greece.MarkCompletedObjective(ObjectiveDestroyScrinBases)
			Utils.Do({ LeftGateway, RightGateway, MiddleGateway }, function(g)
				if not g.IsDead then
					g.Kill()
				end
			end)
		end

		if Greece.IsObjectiveCompleted(ObjectiveDestroyScrinBases) and #MaleficScrin.GetActorsByTypes({ "veng" }) == 0 then
			Greece.MarkCompletedObjective(ObjectiveStopVoidEngines)
		end

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
	if NextVoidEngineIndex <= VoidEngineAttackCount[Difficulty] and not Greece.IsObjectiveCompleted(ObjectiveDestroyScrinBases) then
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

		local voidEngine = Reinforcements.Reinforce(MaleficScrin, { "veng" }, { spawnLoc }, 10, function(a)
			local actorId = tostring(a)

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
				if IsMissionPlayer(attacker.Owner) and not self.IsDead then
					-- below 66% health, start attacking units on path to exit
					if not AggrodVoidEngines[actorId] and self.Health < self.MaxHealth / 3 * 2 then
						AggrodVoidEngines[actorId] = true
						self.Stop()
						self.AttackMove(path[#path])
					else
						-- below 33% health, attack player base
						if self.Health < self.MaxHealth / 3 then
							if not BaseAttackVoidEngines[actorId] then
								BaseAttackVoidEngines[actorId] = true
								self.Stop()
								AssaultPlayerBaseOrHunt(self)
							-- 5% chance every time damaged to clear any current target and continue attacking player base
							else
								local rand = Utils.RandomInteger(1,100)
								if rand > 100 - 5 then
									self.Stop()
									AssaultPlayerBaseOrHunt(self)
								end
							end
						-- between 66% and 33% health, 10% chance every time damaged to clear target and attack move to exit
						elseif AggrodVoidEngines[actorId] then
							local rand = Utils.RandomInteger(1,100)
							if rand > 100 - 10 then
								self.Stop()
								self.AttackMove(path[#path])
							end
						end
					end

					if IsVeryHardOrAbove() and not AnathemaVoidEngines[actorId] and self.Health < 200000 then
						AnathemaVoidEngines[actorId] = true
						self.GrantCondition("anathema")
						Media.PlaySound("anathema.aud")
					end
				end
			end)

			Trigger.AfterDelay(VoidEngineRevealDelay[Difficulty], function()
				if not a.IsDead then
					Beacon.New(Greece, a.CenterPosition)
					Media.PlaySound("beacon.aud")
					a.GrantCondition("veng-reveal")
				end
			end)
		end)[1]

		if Difficulty == "brutal" and DateTime.GameTime > DateTime.Minutes(30) then
			local guardsList = {}

			-- starting with 4, add a guard for every 10 minutes past 30 minutes, up to a max of 10 guards
			local numGuards = math.min(10, 4 + math.floor((DateTime.GameTime - DateTime.Minutes(30)) / DateTime.Minutes(10)))
			for i = 1, numGuards do
				table.insert(guardsList, "gunw")
			end

			local guards = Reinforcements.Reinforce(MaleficScrin, guardsList, { spawnLoc }, 250, function(g)
				g.Guard(voidEngine)
				IdleHunt(g)
			end)
		end

		NextVoidEngineIndex = NextVoidEngineIndex + 1

		Trigger.AfterDelay(VoidEngineInterval[Difficulty], function()
			SendNextVoidEngine()
		end)
	end
end
