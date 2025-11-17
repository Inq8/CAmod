MissionDir = "ca|missions/main-campaign/ca46-intervention"

SuperweaponsEnabledTime = {
	easy = DateTime.Minutes(60),
	normal = DateTime.Minutes(40),
	hard = DateTime.Minutes(25),
	vhard = DateTime.Minutes(18),
	brutal = DateTime.Minutes(15)
}

NodNavalAttackPaths = {
	{ NavalWaypoint1.Location, NavalWaypoint2.Location, NavalWaypoint3.Location, NavalWaypoint4.Location },
	{ NavalWaypoint4.Location, NavalWaypoint3.Location, NavalWaypoint2.Location, NavalWaypoint1.Location },
	{ NavalWaypoint1.Location, NavalWaypoint5.Location, NavalWaypoint4.Location },
	{ NavalWaypoint4.Location, NavalWaypoint5.Location, NavalWaypoint1.Location },
	{ NavalWaypoint5.Location, NavalWaypoint2.Location, NavalWaypoint3.Location },
	{ NavalWaypoint5.Location, NavalWaypoint3.Location, NavalWaypoint2.Location }
}

Squads = {
	Naval = {
		ActiveCondition = function()
			return MissionPlayersHaveNavalPresence()
		end,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 8, Max = 12 }),
		Compositions = {
			easy = {
				{ Ships = { "sb" } }
			},
			normal = {
				{ Ships = { "sb" } }
			},
			hard = {
				{ Ships = { "sb", "ss2" } }
			},
			vhard = {
				{ Ships = { "sb", "sb", "ss2" } }
			},
			brutal = {
				{ Ships = { "sb", "sb", "sb", "ss2" } }
			}
		},
		AttackPaths = NodNavalAttackPaths
	},
	ICBMSubs = {
		Delay = DateTime.Minutes(15),
		AttackValuePerSecond = { Min = 8, Max = 16 },
		Compositions = {
			brutal = {
				{ Ships = { "isub" } }
			},
		},
		AttackPaths = NodNavalAttackPaths
	},
	Air = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(12)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 20 }),
		Compositions = AirCompositions.Nod,
	},
	AntiCruiserAir = {
		ActiveCondition = function(squad)
			return #GetMissionPlayersActorsByType("ca") > 0
		end,
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 20 }),
		Compositions = { { Aircraft = { "scrn", "scrn" } } }
	},
	AirToAir = AirToAirSquad({ "scrn", "apch", "venm" }, AdjustAirDelayForDifficulty(DateTime.Minutes(12)))
}

SetupPlayers = function()
	Greece = Player.GetPlayer("Greece")
	Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { Greece }
	MissionEnemies = { Nod }
end

WorldLoaded = function()
	SetupPlayers()

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Greece)
	AdjustPlayerStartingCashForDifficulty()
	RemoveActorsBasedOnDifficultyTags()
	InitNod()
	SetupLightning()

	ObjectiveDestroySilos = Greece.AddObjective("Capture or destroy all Nod Tiberium Silos.")

	if IsHardOrAbove() then
		InitialTree.Destroy()
	end

	if Difficulty == "brutal" then
		SecondTree.Destroy()
	end

	Trigger.AfterDelay(1, function()
		local silos = Nod.GetActorsByType("silo.td")

		Trigger.OnAllKilledOrCaptured(silos, function()
			Greece.MarkCompletedObjective(ObjectiveDestroySilos)
		end)

		Utils.Do(silos, function(s)
			Trigger.OnKilledOrCaptured(s, function()
				UpdateMissionText()
			end)
		end)
	end)

	Trigger.OnKilledOrCaptured(StealthGen, function()
		local mobileStealthGens = Nod.GetActorsByType("msg")
		Utils.Do(mobileStealthGens, function(m)
			m.Kill()
		end)
	end)

	UpdateMissionText()
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
		Nod.Resources = Nod.ResourceCapacity - 500

		if MissionPlayersHaveNoRequiredUnits() then
			Greece.MarkFailedObjective(ObjectiveDestroySilos)
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

UpdateMissionText = function()
	local siloCount = #Nod.GetActorsByType("silo.td")

	if siloCount > 0 then
		UserInterface.SetMissionText(siloCount .. " silos remaining.", HSLColor.Yellow)
	else
		UserInterface.SetMissionText("")
	end
end

InitNod = function()
	AutoRepairAndRebuildBuildings(Nod, 10)
	SetupRefAndSilosCaptureCredits(Nod)
	AutoReplaceHarvesters(Nod)
	AutoRebuildConyards(Nod)
	InitAiUpgrades(Nod)
	InitNavalAttackSquad(Squads.Naval, Nod)
	InitAirAttackSquad(Squads.Air, Nod)

	if IsHardOrAbove() then
		InitAirAttackSquad(Squads.AntiCruiserAir, Nod, MissionPlayers, { "ca" })
		InitAirAttackSquad(Squads.AirToAir, Nod, MissionPlayers, { "Aircraft" }, "ArmorType")

		if Difficulty == "brutal" then
			InitNavalAttackSquad(Squads.ICBMSubs, Nod)
		end
	end

	Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = Nod })
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = Nod })
	end)

	local NodGroundAttackers = Nod.GetGroundAttackers()
	Utils.Do(NodGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)

	local productionBuildings = Nod.GetActorsByTypes({ "hand", "hpad.td", "airs", "afac" })
	for _, b in pairs(productionBuildings) do
		SellOnCaptureAttempt(b)
	end
end

SetupLightning = function()
	local nextStrikeDelay = Utils.RandomInteger(DateTime.Seconds(4), DateTime.Seconds(30))
	Trigger.AfterDelay(nextStrikeDelay, function()
		LightningStrike()
		SetupLightning()
	end)
end

LightningStrike = function()
	local duration = Utils.RandomInteger(5, 8)
	local thunderDelay = Utils.RandomInteger(5, 65)
	local soundNumber
	Lighting.Flash("LightningStrike", duration)

	repeat
		soundNumber = Utils.RandomInteger(1, 7)
	until(soundNumber ~= LastSoundNumber)
	LastSoundNumber = soundNumber

	Trigger.AfterDelay(thunderDelay, function()
		Media.PlaySound("thunder" .. soundNumber .. ".aud")
	end)
end

IdleHunt = function(actor)
	if actor.HasProperty("HuntCA") and not actor.IsDead then
		Trigger.OnIdle(actor, function(a)
			if not a.IsDead and a.IsInWorld and not IsMissionPlayer(a.Owner) then
				a.HuntCA()
			end
		end)
	end
end
