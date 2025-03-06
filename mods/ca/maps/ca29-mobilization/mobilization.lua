UnitBuildTimeMultipliers = {
	easy = 0.05,
	normal = 0.05,
	hard = 0.05,
}

ReinforcementInterval = DateTime.Minutes(4)

ReinforcementLocations = {
	{ Path = { ReinforcementSpawn1.Location, ReinforcementRally1.Location }, Flare = ReinforcementFlare1.Location },
	{ Path = { ReinforcementSpawn2.Location, ReinforcementRally2.Location }, Flare = ReinforcementFlare2.Location },
	{ Path = { ReinforcementSpawn3.Location, ReinforcementRally3.Location }, Flare = ReinforcementFlare3.Location },
	{ Path = { ReinforcementSpawn4.Location, ReinforcementRally4.Location }, Flare = ReinforcementFlare4.Location },
	{ Path = { ReinforcementSpawn5.Location, ReinforcementRally5.Location }, Flare = ReinforcementFlare5.Location },
	{ Path = { ReinforcementSpawn6.Location, ReinforcementRally6.Location }, Flare = ReinforcementFlare6.Location },
	{ Path = { ReinforcementSpawn7.Location, ReinforcementRally7.Location }, Flare = ReinforcementFlare7.Location },
	{ Path = { ReinforcementSpawn8.Location, ReinforcementRally8.Location }, Flare = ReinforcementFlare8.Location },
	{ Path = { ReinforcementSpawn9.Location, ReinforcementRally9.Location }, Flare = ReinforcementFlare9.Location },
	{ Path = { ReinforcementSpawn10.Location, ReinforcementRally10.Location }, Flare = ReinforcementFlare10.Location }
}

ActiveReinforcementLocations = {
	ReinforcementLocations[6],
	ReinforcementLocations[5],
	ReinforcementLocations[2]
}

InitialReinforcementGroup = { "n1", "n1", "n1", "n3", "n1", "n1", "n3", "mtnk", "mtnk", "msam" }

FinalReinforcementGroups = {
	{ "htnk", "n1", "n1", "n1", "n3", "n1", "n1", "n3", "xo", "xo"  },
	{ "wolv", "wolv", "ztrp", "ztrp", "ztrp", "ztrp", "ztrp" },
	{ "titn", "n1", "n1", "n1", "n3", "n1", "n1", "n3", "vulc", "msam", "vulc" },
	{ "htnk", "disr", "n1", "n1", "n1", "n3", "n1", "zrai", "zrai", "vulc" },
	{ "htnk", "zdef", "zdef", "n1", "n1", "n1", "n3", "n1", "medi", "n1", "n1", "n1", "n3", "pbul" }
}

Utils.Do(UnitCompositions.Scrin.Main[Difficulty], function(c)
	if c.Aircraft ~= nil then
		c.Aircraft = {}
	end
end)

Squads = {
	ScrinMain = {
		Delay = {
			easy = DateTime.Minutes(3),
			normal = DateTime.Minutes(2),
			hard = DateTime.Minutes(1)
		},
		AttackValuePerSecond = {
			easy = { Min = 20, Max = 50 },
			normal = { Min = 50, Max = 100 },
			hard = { Min = 80, Max = 160 },
		},
		FollowLeader = true,
		ProducerActors = nil,
		ProducerTypes = { Infantry = { "wormhole" }, Vehicles = { "wormhole" }, Aircraft = { "wormhole" } },
		Units = UnitCompositions.Scrin.Main,
		AttackPaths = {
			{ HQ.Location },
		},
	},
	ScrinAir = {
		Delay = {
			easy = DateTime.Minutes(14),
			normal = DateTime.Minutes(10),
			hard = DateTime.Minutes(6)
		},
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 10 },
			normal = { Min = 35, Max = 35 },
			hard = { Min = 60, Max = 60 },
		},
		ProducerTypes = { Aircraft = { "hiddenspawner" } },
		Units = {
			easy = {
				{ Aircraft = { PacOrDevastator } }
			},
			normal = {
				{ Aircraft = { PacOrDevastator } }
			},
			hard = {
				{ Aircraft = { PacOrDevastator } }
			}
		},
		AttackPaths = {
			{ HQ.Location },
		},
	}
}

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Scrin = Player.GetPlayer("Scrin")
	Nod = Player.GetPlayer("Nod")
	USSR = Player.GetPlayer("USSR")
	Greece = Player.GetPlayer("Greece")
	MissionPlayers = { GDI }
	LastScrinProduction = 0
	ReinforcementGroupIndex = 1
	ReinforcementLocationIndex = 1
	ReinforcementWave = 1
	TimerTicks = ReinforcementInterval

	NextReinforcementsFlare()

	Camera.Position = HQ.CenterPosition

	InitObjectives(GDI)
	AdjustStartingCash()
	InitScrin()
	SetupLightning()

	ObjectiveDestroyWormholes = GDI.AddObjective("Destroy all Scrin wormholes.")
	ObjectiveDefendHQ = GDI.AddObjective("Protect the Command Center.")

	Utils.Do({ Nod, USSR, Greece }, function(p)
		local groundAttackers = p.GetGroundAttackers()

		Utils.Do(groundAttackers, function(a)
			TargetSwapChance(a, 10)
			CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGroundHunterUnit)
		end)
	end)

	Trigger.OnKilled(HQ, function()
		GDI.MarkFailedObjective(ObjectiveDefendHQ)
	end)

	local wormholes = Scrin.GetActorsByType("wormhole")
	WormholeCount = #wormholes
	Utils.Do(wormholes, function(w)
		Trigger.OnProduction(w, function(producer, produced)
			LastScrinProduction = DateTime.GameTime
		end)

		Trigger.OnKilled(w, function(self, killer)
			local wormholesRemaining = Scrin.GetActorsByType("wormhole")
			WormholeCount = #wormholesRemaining
			UpdateMissionText()
			if WormholeCount == 0 and not GDI.IsObjectiveCompleted(ObjectiveDestroyWormholes) then
				GDI.MarkCompletedObjective(ObjectiveDestroyWormholes)
				GDI.MarkCompletedObjective(ObjectiveDefendHQ)
			end
		end)
	end)

	Trigger.AfterDelay(1, function()
		SetActiveWormhole()
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				SendReinforcements()
				TimerTicks = ReinforcementInterval
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

UpdateMissionText = function()
	if TimerTicks > 0 then
		UserInterface.SetMissionText(WormholeCount .. " wormholes remaining. Reinforcements in " .. UtilsCA.FormatTimeForGameSpeed(TimerTicks), HSLColor.Yellow)
	else
		UserInterface.SetMissionText("")
	end
end

InitScrin = function()
	InitAiUpgrades(Scrin)

	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)

	Trigger.AfterDelay(Squads.ScrinMain.Delay[Difficulty], function()
		InitAttackSquad(Squads.ScrinMain, Scrin)
	end)

	Trigger.AfterDelay(Squads.ScrinAir.Delay[Difficulty], function()
		InitAttackSquad(Squads.ScrinAir, Scrin)
	end)
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

SetActiveWormhole = function()
	-- If last unit produced was less than 5 seconds ago, wait 5 seconds to allow squad to finish producing
	if LastScrinProduction < DateTime.GameTime - DateTime.Seconds(5) then
		Trigger.AfterDelay(DateTime.Seconds(5), SetActiveWormhole)
		return
	end

	local wormholes = Scrin.GetActorsByType("wormhole")
	if #wormholes > 0 then
		local wormhole = Utils.Random(wormholes)
		Squads.ScrinMain.ProducerActors = { Infantry = { wormhole }, Vehicles = { wormhole }, Aircraft = { wormhole } }
		Trigger.AfterDelay(DateTime.Seconds(30), SetActiveWormhole)
	end
end

NextReinforcementsFlare = function()
	Trigger.AfterDelay(DateTime.Seconds(15), function()
		local flareLoc = ActiveReinforcementLocations[ReinforcementLocationIndex].Flare
		ReinforcementFlare = Actor.Create("flare", true, { Location = flareLoc, Owner = GDI })
		Beacon.New(GDI, Map.CenterOfCell(flareLoc))
	end)
end

SendReinforcements = function()
	local locations = ActiveReinforcementLocations[ReinforcementLocationIndex]
	local path = locations.Path
	local flareLoc = locations.Flare
	local units

	if ReinforcementWave > 4 then
		units = Utils.Random(FinalReinforcementGroups)
	else
		units = InitialReinforcementGroup
	end

	if ReinforcementWave == 3 then
		local unitsWithMcv = {}
		Utils.Do(units, function(u)
			table.insert(unitsWithMcv, u)
		end)
		table.insert(unitsWithMcv, "amcv")
		units = unitsWithMcv
		ActiveReinforcementLocations = Utils.Shuffle(ReinforcementLocations)
	else
		ReinforcementLocationIndex = ReinforcementLocationIndex + 1
	end

	TimerTicks = ReinforcementInterval
	ReinforcementWave = ReinforcementWave + 1

	if ReinforcementLocationIndex > #ReinforcementLocations then
		ReinforcementLocations = Utils.Shuffle(ReinforcementLocations)
		ReinforcementLocationIndex = 1
	end

	local reinforcements = Reinforcements.Reinforce(GDI, units, path, 50)

	ReinforcementFlare.Destroy()
	Media.PlaySpeechNotification(GDI, "ReinforcementsArrived")
	Notification("Reinforcements have arrived.")
	Beacon.New(GDI, Map.CenterOfCell(flareLoc))
	NextReinforcementsFlare()
end
