RespawnEnabled = Map.LobbyOption("respawn") == "enabled"

PowerGrids = {
	{
		Providers = { NWPower1, NWPower2, NWPower3, NWPower4 },
		Consumers = { NWPowered1, NWPowered2, NWPowered3, NWPowered4, NWPowered5, NWPowered6, NWPowered7, NWPowered8, NWPowered9, NPowered10 },
	},
	{
		Providers = { NPower1, NPower2, NPower3, NPower4 },
		Consumers = { NPowered1, NPowered2, NPowered3, NPowered4, NPowered5, NPowered6, NPowered7, NPowered8, NPowered9 },
	},
	{
		Providers = { NEPower1, NEPower2, NEPower3, NEPower4 },
		Consumers = { NEPowered1, NEPowered2, NEPowered3, NEPowered4, NEPowered5 },
	},
	{
		Providers = { SWPower1, SWPower2, SWPower3, SWPower4 },
		Consumers = { SWPowered1, SWPowered2, SWPowered3, SWPowered4, SWPowered5, SWPowered6, SWPowered7, SWPowered8, SWPowered9, SWPowered10, SWPowered11 },
	},
	{
		Providers = { SPower1, SPower2, SPower3, SPower4, SPower5 },
		Consumers = { SPowered1, SPowered2, SPowered3, SPowered4, SPowered5, SPowered6},
	},
	{
		Providers = { SEPower1, SEPower2, SEPower3, SEPower4 },
		Consumers = { SEPowered1, SEPowered2, SEPowered3, SEPowered4, SEPowered5, SEPowered6, SEPowered7 },
	},
}

PowerPlants = { NWPower1, NWPower2, NWPower3, NWPower4, NPower1, NPower2, NPower3, NPower4, NEPower1, NEPower2, NEPower3, NEPower4, SWPower1, SWPower2, SWPower3, SWPower4, SPower1, SPower2, SPower3, SPower4, SPower5, SEPower1, SEPower2, SEPower3, SEPower4 }
AircraftStructures = { OrcaPad1, OrcaPad2, OrcaPad3, OrcaPad4, WarthogAirfield1, WarthogAirfield2, WarthogAirfield3, WarthogAirfield4, AuroraAirfield1, AuroraAirfield2, LongbowPad1, LongbowPad2, LongbowPad3, LongbowPad4, HarrierPad1, HarrierPad2 }
CoastAAGuns = { CoastAAGun1, CoastAAGun2, CoastAAGun3, CoastAAGun4, CoastAAGun5 }

DisabledAntiAir = { }

GroundedAircraft = {
	{ Orca1, OrcaPad1 },
	{ Orca2, OrcaPad2 },
	{ Orca3, OrcaPad3 },
	{ Orca4, OrcaPad4 },
	{ Warthog1, WarthogAirfield1 },
	{ Warthog2, WarthogAirfield2 },
	{ Warthog3, WarthogAirfield3 },
	{ Warthog4, WarthogAirfield4 },
	{ Aurora1, AuroraAirfield1 },
	{ Aurora2, AuroraAirfield2 },
	{ Longbow1, LongbowPad1 },
	{ Longbow2, LongbowPad2 },
	{ Longbow3, LongbowPad3 },
	{ Longbow4, LongbowPad4 },
	{ Harrier1, HarrierPad1 },
	{ Harrier2, HarrierPad2 },
}

Squads = {
	Allied = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(6)),
		AttackValuePerSecond = {
			vhard = { Min = 2, Max = 7 },
			brutal = { Min = 5, Max = 10 },
		},
		FollowLeader = true,
		ProducerActors = { Infantry = { AlliedSouthBarracks } },
		Compositions = AdjustCompositionsForDifficulty({
			{ Infantry = { "e1", "e1", "e1", "e1", "e3", "e1", "e3", "e1" } },
		}),
	},
	GDI = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(6) + DateTime.Seconds(30)),
		AttackValuePerSecond = {
			vhard = { Min = 2, Max = 7 },
			brutal = { Min = 5, Max = 10 },
		},
		FollowLeader = true,
		ProducerActors = { Infantry = { GDINorthBarracks } },
		Compositions = AdjustCompositionsForDifficulty({
			{ Infantry = { "n2", "n2", "n2", "n2" } },
		}),
	}
}

WorldLoaded = function()
	Scrin = Player.GetPlayer("Scrin")
	Greece = Player.GetPlayer("Greece")
	GDI = Player.GetPlayer("GDI")
	MissionPlayers = { Scrin }
	MissionEnemies = { Greece, GDI }
	TimerTicks = 0
	StormsEnded = false

	Camera.Position = PlayerStart.CenterPosition

	AntiAir = Utils.Concat(Greece.GetActorsByType("agun"), GDI.GetActorsByType("cram"))

	InitObjectives(Scrin)
	InitGreece()
	InitGDI()
	SetupLightning()
	SetupIonStorm()
	UpdateObjective()

	ObjectiveDestroyAirfields = Scrin.AddObjective("Destroy all airfields and helipads.")
	ObjectiveDestroyAntiAir = Scrin.AddObjective("Destroy or disable all air defense structures.")

	Actor.Create("radar.dummy", true, { Owner = Scrin })
	Actor.Create("blink.upgrade", true, { Owner = Scrin })
	Actor.Create("coalescence.upgrade", true, { Owner = Scrin })

	if Difficulty ~= "easy" then
		EasyOnlyIntruder1.Destroy()
		EasyOnlyIntruder2.Destroy()
	end

	if IsHardOrAbove() then
		EasyNormalOnlyIntruder.Destroy()
	end

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Tip("Intruders can teleport short distances using either the deploy command [" .. UtilsCA.Hotkey("Deploy") .. "] or force move (they can be teleported as a group).")
	end)

	Utils.Do(GroundedAircraft, function(i)
		i[1].ReturnToBase(i[2])
	end)

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		Utils.Do(GroundedAircraft, function(i)
			i[1].GrantCondition("grounded")
		end)
	end)

	Utils.Do(PowerGrids, function(grid)
		Trigger.OnAllKilledOrCaptured(grid.Providers, function()
			Utils.Do(grid.Consumers, function(consumer)
				if not consumer.IsDead then
					consumer.GrantCondition("disabled")
					if consumer.Type == "agun" or consumer.Type == "cram" then
						DisabledAntiAir[tostring(consumer)] = true
					end
				end
			end)
			UpdateObjective()
		end)
	end)

	LeechersRespawning = true

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		SpawnLeechers()

		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Tip("Leechers can be deployed using [" .. UtilsCA.Hotkey("Deploy") .. "] to temporarily transform into balls of bio-matter which heal nearby allies.")
		end)

		Trigger.AfterDelay(DateTime.Seconds(10), function()
			Tip("Leechers also transform in this way to avoid death and attempt to regenerate. In either case they are vulnerable in this state.")
		end)
	end)

	Trigger.OnAllKilled(AircraftStructures, function()
		Scrin.MarkCompletedObjective(ObjectiveDestroyAirfields)
		Trigger.AfterDelay(DateTime.Seconds(3), function()
			StormsEnded = true
		end)
	end)

	Trigger.OnAllKilled(CoastAAGuns, function()
		CoastAAGunsDestroyed = true
		if PowerPlantsDestroyed then
			Scrin.MarkCompletedObjective(ObjectiveDestroyAntiAir)
		end
	end)

	Trigger.OnAllKilled(PowerPlants, function()
		PowerPlantsDestroyed = true
		if CoastAAGunsDestroyed then
			Scrin.MarkCompletedObjective(ObjectiveDestroyAntiAir)
		end
	end)

	Utils.Do(AircraftStructures, function(actor)
		Trigger.OnDamaged(actor, function(self, attacker, damage)
			if IsMissionPlayer(attacker.Owner) then
				local nearbyUnits = Map.ActorsInCircle(self.CenterPosition, WDist.New(3072), function(a) return IsGroundHunterUnit(a) and (a.Owner == GDI or a.Owner == Greece) end)
				Utils.Do(nearbyUnits, function(nearbyUnit)
					nearbyUnit.Attack(attacker)
				end)
			end
		end)
	end)

	Utils.Do(Utils.Concat(AntiAir, AircraftStructures), function(a)
		Trigger.OnKilled(a, function(self, killer)
			UpdateObjective()
		end)
	end)

	SetupReveals({ EntranceReveal1, EntranceReveal2, EntranceReveal3, EntranceReveal4, EntranceReveal5, EntranceReveal6, EntranceReveal7, EntranceReveal8 })
	AfterWorldLoaded()
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()

	if StormsEnded then
		if Lighting.Red > 1 then
			Lighting.Red = Lighting.Red - 0.005
		end

		if Lighting.Blue > 1 then
			Lighting.Blue = Lighting.Blue - 0.005
		end

		if Lighting.Ambient < 1 then
			Lighting.Ambient = Lighting.Ambient + 0.005
		end
	end
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

		local intruders = Scrin.GetActorsByType("s4")
		local leechers = Scrin.GetActorsByTypes({ "lchr", "lchr.orb" })

		if RespawnEnabled then
			if #intruders == 0 and not IntrudersRespawning then
				RespawnIntruders()
			end
			if #leechers == 0 and not LeechersRespawning then
				RespawnLeechers()
			end
		else
			if #intruders + #leechers == 0 then
				if ObjectiveDestroyAirfields ~= nil and not Scrin.IsObjectiveCompleted(ObjectiveDestroyAirfields) then
					Scrin.MarkFailedObjective(ObjectiveDestroyAirfields)
				end
				if ObjectiveDestroyAntiAir ~= nil and not Scrin.IsObjectiveCompleted(ObjectiveDestroyAntiAir) then
					Scrin.MarkFailedObjective(ObjectiveDestroyAntiAir)
				end
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()
	end
end

InitGreece = function()
	RebuildExcludes.Greece = { Types = { "powr", "apwr", "hpad", "agun", "pbox", "pris" } }

	AutoRepairAndRebuildBuildings(Greece, 15)
	SetupRefAndSilosCaptureCredits(Greece)
	AutoReplaceHarvesters(Greece)
	InitAiUpgrades(Greece)

	if IsVeryHardOrAbove() then
		InitAttackSquad(Squads.Allied, Greece)
	end

	Actor.Create("ai.unlimited.power", true, { Owner = Greece })

	local alliedGroundAttackers = Greece.GetGroundAttackers()

	Utils.Do(alliedGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGreeceGroundHunterUnit)
	end)
end

InitGDI = function()
	RebuildExcludes.GDI = { Types = { "nuke", "nuk2", "hpad.td", "afld.gdi", "cram", "gtwr", "atwr" } }

	AutoRepairAndRebuildBuildings(GDI, 15)
	SetupRefAndSilosCaptureCredits(GDI)
	AutoReplaceHarvesters(GDI)
	InitAiUpgrades(GDI)

	if IsVeryHardOrAbove() then
		InitAttackSquad(Squads.GDI, GDI)
	end

	Actor.Create("ai.unlimited.power", true, { Owner = GDI })

	local gdiGroundAttackers = GDI.GetGroundAttackers()

	Utils.Do(gdiGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGDIGroundHunterUnit)
	end)

	local titanTriggerFootprint = { TitanTrigger1.Location, TitanTrigger2.Location, TitanTrigger3.Location, TitanTrigger4.Location, TitanTrigger5.Location }
	Trigger.OnEnteredFootprint(titanTriggerFootprint, function(a, id)
		if IsMissionPlayer(a.Owner) and not TitanPatroller.IsDead and not IsTitanSpotted then
			IsTitanSpotted = true
			Trigger.RemoveProximityTrigger(id)
			local camera = Actor.Create("smallcamera", true, { Owner = Scrin, Location = TitanPatroller.Location })
			Trigger.AfterDelay(DateTime.Seconds(4), function()
				camera.Destroy()
			end)
			local titanPatrolPath = { TitanPatrol1.Location, TitanPatrol2.Location, TitanPatrol3.Location, TitanPatrol4.Location, TitanPatrol5.Location, TitanPatrol6.Location }
			TitanPatroller.Patrol(titanPatrolPath, true)
		end
	end)

	local miniDronePatrolPath = { MiniDronePatrol1.Location, MiniDronePatrol2.Location, MiniDronePatrol3.Location, MiniDronePatrol4.Location, MiniDronePatrol5.Location, MiniDronePatrol6.Location, MiniDronePatrol5.Location, MiniDronePatrol4.Location, MiniDronePatrol3.Location, MiniDronePatrol2.Location }
	MiniDronePatroller1.Patrol(miniDronePatrolPath, true)
	MiniDronePatroller2.Patrol(miniDronePatrolPath, true)
end

SetupLightning = function()
	local nextStrikeDelay = Utils.RandomInteger(DateTime.Seconds(8), DateTime.Seconds(25))
	Trigger.AfterDelay(nextStrikeDelay, function()
		if not StormsEnded then
			LightningStrike()
			SetupLightning()
		end
	end)
end

SetupIonStorm = function()
	local nextStrikeDelay = Utils.RandomInteger(DateTime.Seconds(8), DateTime.Seconds(25))
	Trigger.AfterDelay(nextStrikeDelay, function()
		if not StormsEnded then
			IonStorm()
			SetupIonStorm()
		end
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

RespawnLeechers = function()
	if not LeechersRespawning then
		LeechersRespawning = true
		Notification("Reinforcements will arrive in 30 seconds.")
		Trigger.AfterDelay(DateTime.Seconds(30), function()
			SpawnLeechers()
		end)
	end
end

RespawnIntruders = function()
	if not IntrudersRespawning then
		IntrudersRespawning = true
		Notification("Reinforcements will arrive in 20 seconds.")
		Trigger.AfterDelay(DateTime.Seconds(20), function()
			SpawnIntruders()
		end)
	end
end

SpawnLeechers = function()
	local wormhole = Actor.Create("wormhole", true, { Owner = Scrin, Location = LeecherSpawn.Location })
	Beacon.New(Scrin, LeecherSpawn.CenterPosition, DateTime.Seconds(20))

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Media.PlaySpeechNotification(Scrin, "ReinforcementsArrived")

		local leecherSquad = { "lchr", "lchr" }
		if IsHardOrAbove() then
			leecherSquad = { "lchr" }
		end

		local leechers = Reinforcements.Reinforce(Scrin, leecherSquad, { LeecherSpawn.Location }, 1)
		Utils.Do(leechers, function(leecher)
			leecher.Scatter()
		end)

		LeechersRespawning = false

		Trigger.AfterDelay(DateTime.Seconds(5), function()
			wormhole.Kill()
		end)
	end)
end

SpawnIntruders = function()
	local wormhole = Actor.Create("wormhole", true, { Owner = Scrin, Location = IntruderSpawn.Location })
	Beacon.New(Scrin, IntruderSpawn.CenterPosition, DateTime.Seconds(20))

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Media.PlaySpeechNotification(Scrin, "ReinforcementsArrived")
		local intruderSquad = { "s4", "s4", "s4", "s4", "s4", "s4" }

		if IsHardOrAbove() then
			intruderSquad = { "s4", "s4", "s4" }
		elseif Difficulty == "normal" then
			intruderSquad = { "s4", "s4", "s4", "s4" }
		end

		local intruders = Reinforcements.Reinforce(Scrin, intruderSquad, { IntruderSpawn.Location }, 1)
		Utils.Do(intruders, function(intruder)
			intruder.Scatter()
		end)

		IntrudersRespawning = false

		Trigger.AfterDelay(DateTime.Seconds(5), function()
			wormhole.Kill()
		end)
	end)
end

UpdateObjective = function()
	local activeAA = Utils.Where(AntiAir, function(a) return not a.IsDead and not DisabledAntiAir[tostring(a)] end)
	local aircraftStructuresRemaining = Utils.Where(AircraftStructures, function(a) return not a.IsDead end)
	UserInterface.SetMissionText(#activeAA .. " active anti-aircraft defenses remaining. " .. #aircraftStructuresRemaining .. " aircraft structures remaining.", HSLColor.Yellow)
end
