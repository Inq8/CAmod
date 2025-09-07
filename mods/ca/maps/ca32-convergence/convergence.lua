MaxBreakthroughs = {
	easy = 6,
	normal = 3,
	hard = 0,
	vhard = 0,
	brutal = 0
}

FleetWaveCompositions = {
	easy = {
		{ "pac", "pac", "deva" },
		{ "pac", "deva", "deva" }
	},
	normal = {
		{ "pac", "pac", "deva", "pac" },
		{ "pac", "deva", "deva", "pac" },
	},
	hard = {
		{ "pac", "pac", "deva", "pac", "deva" },
		{ "pac", "deva", "pac", "deva", "pac" },
	},
	vhard = {
		{ "pac", "pac", "deva", "pac", "deva" },
		{ "pac", "deva", "pac", "deva", "pac" },
	},
	brutal = {
		{ "pac", "pac", "deva", "pac", "deva", "pac" },
		{ "pac", "deva", "pac", "deva", "pac", "deva" },
	}
}

TimeBetweenWaves = {
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(3),
	hard = DateTime.Minutes(3),
	vhard = DateTime.Minutes(3),
	brutal = DateTime.Minutes(3)
}

FleetSpawns = {
	Left = { LSpawn1, LSpawn2, LSpawn3 },
	Middle = { MSpawn1, MSpawn2 },
	Right = { RSpawn1, RSpawn2 },
	LeftAndRight = { LSpawn1, LSpawn2, LSpawn3, RSpawn1, RSpawn2 },
	MiddleAndRight = { MSpawn1, MSpawn2, RSpawn1, RSpawn2 },
	MiddleAndLeft = { MSpawn1, MSpawn2, LSpawn1, LSpawn2, LSpawn3 },
	Any = { LSpawn1, LSpawn2, LSpawn3, MSpawn1, MSpawn2, RSpawn1, RSpawn2 },
}

WaveSpawns = {
	FleetSpawns.Left, FleetSpawns.Left, FleetSpawns.Left, FleetSpawns.Middle, FleetSpawns.Right, FleetSpawns.MiddleAndLeft, FleetSpawns.MiddleAndRight, FleetSpawns.LeftAndRight, FleetSpawns.Any, FleetSpawns.Any
}

UnitBuildTimeMultipliers = {
	easy = 0.3,
	normal = 0.2,
	hard = 0.1,
	vhard = 0.1,
	brutal = 0.1
}

LeftScrinSpawners = { ScrinSpawnerL1, ScrinSpawnerL2, ScrinSpawnerL3, ScrinSpawnerL4 }
MiddleScrinSpawners = { ScrinSpawnerM1, ScrinSpawnerM2 }

Squads = {
	ScrinMain = {
		Delay = AdjustDelayForDifficulty(DateTime.Seconds(150)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 27, Max = 64 }),
		FollowLeader = true,
		RandomProducerActor = true,
		ProducerActors = { Infantry = LeftScrinSpawners, Vehicles = LeftScrinSpawners, Aircraft = LeftScrinSpawners },
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin),
		AttackPaths = {
			{ LAttackRally1a.Location, LAttackRally1b.Location },
			{ LAttackRally2a.Location, LAttackRally2b.Location },
			{ LAttackRally3a.Location, LAttackRally3b.Location }
		},
	},
	ScrinWater = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(4)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 36 }),
		FollowLeader = true,
		RandomProducerActor = true,
		ProducerActors = { Infantry = MiddleScrinSpawners, Vehicles = MiddleScrinSpawners, Aircraft = MiddleScrinSpawners },
		Compositions = ScrinWaterCompositions,
		AttackPaths = {
			{ MAttackRally1.Location },
			{ MAttackRally1.Location },
			{ MAttackRally2a.Location, MAttackRally2b.Location },
			{ MAttackRally2a.Location, MAttackRally2b.Location },
			{ RAttackRally1.Location, RAttackRally2.Location, RAttackRally3.Location, MAttackRally2b.Location }
		},
	},
}

WorldLoaded = function()
	Scrin = Player.GetPlayer("Scrin")
	GDI = Player.GetPlayer("GDI")
	TibLifeforms = Player.GetPlayer("TibLifeforms")
	MissionPlayers = { GDI }
	TimerTicks = 0
	WavesRemaining = #WaveSpawns
	NumBreakthroughs = 0
	NextWave = 1
	FinalWaveArrived = false

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(GDI)
	AdjustPlayerStartingCashForDifficulty()
	InitScrin()
	InitAiUpgrades(Scrin, 0)
	SetupLightning()
	SetupIonStorm()
	UpdateMissionText()

	if IsNormalOrAbove() then
		Sensor1.Destroy()
		Sensor2.Destroy()

		if IsHardOrAbove() then
			Sensor3.Destroy()
		end
	end

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		if IsHardOrAbove() then
			Tip("Scrin fleet vessels will be pinged on the minimap when entering the area.")
		else
			Tip("Scrin fleet vessels will be pinged on the minimap when entering the area and their paths will be visible as long as you have an active radar.")
		end
	end)

	Trigger.AfterDelay(TimeBetweenWaves[Difficulty] + DateTime.Minutes(1), function()
		SendFleetWave()

		Trigger.AfterDelay(DateTime.Seconds(120), function()
			Notification("The area across the river is infested with Tiberium lifeforms. You will need to use aicraft to intercept Scrin fleet vessels attempting to break through there.")
			MediaCA.PlaySound("c_acrossriver.aud", 2)
			Beacon.New(GDI, AcrossRiver.CenterPosition)
			local acrossRiverCamera = Actor.Create("camera", true, { Owner = GDI, Location = AcrossRiver.Location })
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				acrossRiverCamera.Destroy()
			end)
		end)
	end)

	if IsHardOrAbove() then
		ObjectiveStopFleet = GDI.AddObjective("Prevent any Scrin fleet vessels breaking through.")
	else
		ObjectiveStopFleet = GDI.AddObjective("Allow no more than " .. MaxBreakthroughs[Difficulty] .. " fleet vessels through.")
	end

	BottomOfMap = { }
	for i=1, 128 do
		table.insert(BottomOfMap, CPos.New(i,96))
	end

	AfterWorldLoaded()
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	OncePerThirtySecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Scrin.Resources = Scrin.ResourceCapacity - 500

		if NumBreakthroughs > MaxBreakthroughs[Difficulty] then
			GDI.MarkFailedObjective(ObjectiveStopFleet)
		end

		if FinalWaveArrived and #Scrin.GetActorsByTypes({ "pac", "deva" }) == 0 then
			GDI.MarkCompletedObjective(ObjectiveStopFleet)
		end

		if MissionPlayersHaveNoRequiredUnits() and not GDI.IsObjectiveCompleted(ObjectiveStopFleet) then
			GDI.MarkFailedObjective(ObjectiveStopFleet)
		end

		UpdateMissionText()
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
	AutoRepairBuildings(Scrin)
	InitAttackSquad(Squads.ScrinMain, Scrin)
	InitAttackSquad(Squads.ScrinWater, Scrin)

	Actor.Create("ioncon.upgrade", true, { Owner = Scrin })

	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)
end

SetupLightning = function()
	local nextStrikeDelay = Utils.RandomInteger(DateTime.Seconds(8), DateTime.Seconds(25))
	Trigger.AfterDelay(nextStrikeDelay, function()
		LightningStrike()
		SetupLightning()
	end)
end

SetupIonStorm = function()
	local nextStrikeDelay = Utils.RandomInteger(DateTime.Seconds(8), DateTime.Seconds(25))
	Trigger.AfterDelay(nextStrikeDelay, function()
		IonStorm()
		SetupIonStorm()
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

IonStorm = function()
	local duration = Utils.RandomInteger(5, 8)
	local soundNumber
	Lighting.Flash("IonStrike", duration)
	repeat
		soundNumber = Utils.RandomInteger(1, 4)
	until(soundNumber ~= LastIonSoundNumber)
	LastIonSoundNumber = soundNumber
	Media.PlaySound("ionstorm" .. soundNumber .. ".aud")
end

SendFleetWave = function()
	Notification("Scrin fleet vessels approaching.")
	MediaCA.PlaySound("c_scrinfleetvessels.aud", 2)
	local currentWave = NextWave
	local interval = 1

	if currentWave == 5 then
		Utils.Do(FleetWaveCompositions[Difficulty], function(c)
			table.insert(c, "pac")
			if IsHardOrAbove() then
				table.insert(c, "deva")
			end
		end)
	end
	if currentWave == 8 then
		Utils.Do(FleetWaveCompositions[Difficulty], function(c)
			table.insert(c, "deva")
			if IsHardOrAbove() then
				table.insert(c, "pac")
			end
		end)
	end
	if currentWave == 10 and Difficulty ~= "easy" then
		Utils.Do(FleetWaveCompositions[Difficulty], function(c)
			table.insert(c, "deva")
			if IsHardOrAbove() then
				table.insert(c, "pac")
			end
		end)
	end

	local composition = Utils.Random(FleetWaveCompositions[Difficulty])

	local xUsed = { }

	-- for each unit in the wave, get the possible base spawn points, pick one and generate offsetted entry/exit
	Utils.Do(composition, function(shipType)
		Trigger.AfterDelay(interval, function()
			local spawn = nil
			local xOffset = nil
			local entry = nil
			while entry == nil or xUsed[entry.X] ~= nil do
				spawn = Utils.Random(WaveSpawns[currentWave])
				xOffset = Utils.RandomInteger(-7, 7)
				entry = spawn.Location + CVec.New(xOffset, 0)
			end
			xUsed[entry.X] = true
			local exit = CPos.New(entry.X, 96)
			Beacon.New(GDI, spawn.CenterPosition + WVec.New(xOffset * 1024, 0, 0))
			local ships = Reinforcements.Reinforce(Scrin, { shipType }, { entry, exit }, 25, function(self)
				self.Destroy()
				NumBreakthroughs = NumBreakthroughs + 1
				Media.PlaySoundNotification(GDI, "AlertBuzzer")
				Notification("A Scrin fleet vessel has broken through.")
			end)
			if IsNormalOrBelow() then
				local pathRenderer = Actor.Create("pathRenderer", true, { Owner = GDI, Location = entry })
				Trigger.OnRemovedFromWorld(ships[1], function(self)
					pathRenderer.Destroy()
				end)
			end
			Media.PlaySound("beepslct.aud")
		end)

		if currentWave >= 8 and IsHardOrAbove() then
			interval = interval + DateTime.Seconds(3)
		elseif currentWave >= 5 then
			interval = interval + DateTime.Seconds(4)
		else
			interval = interval + DateTime.Seconds(5)
		end
	end)

	if currentWave == #WaveSpawns then
		Trigger.AfterDelay(#composition * DateTime.Seconds(5), function()
			FinalWaveArrived = true
		end)
	end

	NextWave = NextWave + 1
	WavesRemaining = WavesRemaining - 1

	if NextWave <= #WaveSpawns then
		Trigger.AfterDelay(TimeBetweenWaves[Difficulty], function()
			SendFleetWave()
		end)
	end
end

UpdateMissionText = function()
	local missionText = "Waves remaining: " .. WavesRemaining

	if IsNormalOrBelow() then
		missionText = missionText .. " -- Fleet vessels escaped: " .. NumBreakthroughs .. "/" .. MaxBreakthroughs[Difficulty]
	end

	local color = HSLColor.Yellow
	if IsNormalOrBelow() and NumBreakthroughs >= MaxBreakthroughs[Difficulty] then
		color = HSLColor.Red
	end

	UserInterface.SetMissionText(missionText, color)
end
