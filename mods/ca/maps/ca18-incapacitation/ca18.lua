
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

WorldLoaded = function()
    Scrin = Player.GetPlayer("Scrin")
    Greece = Player.GetPlayer("Greece")
    GDI = Player.GetPlayer("GDI")
	MissionPlayer = Scrin
	TimerTicks = 0
	StormsEnded = false

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Scrin)
	InitGreece()
	InitGDI()
	SetupLightning()
	SetupIonStorm()

	ObjectiveDestroyAirfields = Scrin.AddObjective("Destroy all airfields and helipads.")
	ObjectiveDestroyAntiAir = Scrin.AddObjective("Destroy or disable all air defense structures.")

	Actor.Create("blink.upgrade", true, { Owner = Scrin })
	Actor.Create("coalescence.upgrade", true, { Owner = Scrin })

	if Difficulty ~= "easy" then
		EasyOnlyIntruder1.Destroy()
		EasyOnlyIntruder2.Destroy()
	end

	if Difficulty == "hard" then
		EasyNormalOnlyIntruder.Destroy()
	end

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Tip("Intruders can teleport short distances (using either the deploy command or force move).")
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
				end
			end)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		local wormhole = Actor.Create("wormhole", true, { Owner = Scrin, Location = LeecherSpawn.Location })

		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Media.PlaySpeechNotification(Scrin, "ReinforcementsArrived")

			local leecherSquad = { "lchr", "lchr" }
			if Difficulty == "hard" then
				leecherSquad = { "lchr" }
			end

			local leechers = Reinforcements.Reinforce(Scrin, leecherSquad, { LeecherSpawn.Location }, 1)
			Utils.Do(leechers, function(leecher)
				leecher.Scatter()
			end)
		end)

		Trigger.AfterDelay(DateTime.Seconds(5), function()
			wormhole.Kill()
			Tip("Leechers can be deployed to temporarily transform into balls of bio-matter which heal nearby allies.")
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
			if attacker.Owner == MissionPlayer then
				local nearbyUnits = Map.ActorsInCircle(self.CenterPosition, WDist.New(3072), function(a) return IsGroundHunterUnit(a) and (a.Owner == GDI or a.Owner == Greece) end)
				Utils.Do(nearbyUnits, function(nearbyUnit)
					nearbyUnit.Attack(attacker)
				end)
			end
		end)
	end)

	SetupReveals({ EntranceReveal1, EntranceReveal2, EntranceReveal3, EntranceReveal4, EntranceReveal5, EntranceReveal6, EntranceReveal7, EntranceReveal8 })
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

		scrinUnits = Scrin.GetActorsByTypes({ "lchr", "lchr.orb", "s4" })
		if #scrinUnits == 0 then
			if ObjectiveDestroyAirfields ~= nil and not Scrin.IsObjectiveCompleted(ObjectiveDestroyAirfields) then
				Scrin.MarkFailedObjective(ObjectiveDestroyAirfields)
			end
			if ObjectiveDestroyAntiAir ~= nil and not Scrin.IsObjectiveCompleted(ObjectiveDestroyAntiAir) then
				Scrin.MarkFailedObjective(ObjectiveDestroyAntiAir)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocation()
	end
end

InitGreece = function()
	Actor.Create("POWERCHEAT", true, { Owner = Greece })
	Actor.Create("hazmat.upgrade", true, { Owner = Greece })
	Actor.Create("cryr.upgrade", true, { Owner = Greece })

	RebuildExcludes.Greece = { Types = { "powr", "apwr", "hpad", "agun" } }

	AutoRepairAndRebuildBuildings(Greece, 15)
	SetupRefAndSilosCaptureCredits(Greece)
	AutoReplaceHarvesters(Greece)

	local alliedGroundAttackers = Greece.GetGroundAttackers()

	Utils.Do(alliedGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGreeceGroundHunterUnit)
	end)
end

InitGDI = function()
	Actor.Create("POWERCHEAT", true, { Owner = GDI })
	Actor.Create("hazmat.upgrade", true, { Owner = GDI })
	Actor.Create("hold.strat", true, { Owner = GDI })
	Actor.Create("hold2.strat", true, { Owner = GDI })

	if Difficulty == "hard" then
		Actor.Create("hold3.strat", true, { Owner = GDI })
	end

	RebuildExcludes.GDI = { Types = { "nuke", "nuk2", "hpad.td", "afld.gdi", "cram" } }

	local titanPatrolPath = { TitanPatrol1.Location, TitanPatrol2.Location, TitanPatrol3.Location, TitanPatrol4.Location, TitanPatrol5.Location, TitanPatrol6.Location }
	TitanPatroller.Patrol(titanPatrolPath, true)

	local miniDronePatrolPath = { MiniDronePatrol1.Location, MiniDronePatrol2.Location, MiniDronePatrol3.Location, MiniDronePatrol4.Location, MiniDronePatrol5.Location, MiniDronePatrol6.Location, MiniDronePatrol5.Location, MiniDronePatrol4.Location, MiniDronePatrol3.Location, MiniDronePatrol2.Location }
	MiniDronePatroller1.Patrol(miniDronePatrolPath, true)
	MiniDronePatroller2.Patrol(miniDronePatrolPath, true)

	AutoRepairAndRebuildBuildings(GDI, 15)
	SetupRefAndSilosCaptureCredits(GDI)
	AutoReplaceHarvesters(GDI)

	local gdiGroundAttackers = GDI.GetGroundAttackers()

	Utils.Do(gdiGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGDIGroundHunterUnit)
	end)
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
		soundNumber = Utils.RandomInteger(1, 6)
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
		soundNumber = Utils.RandomInteger(1, 3)
	until(soundNumber ~= LastIonSoundNumber)
	LastIonSoundNumber = soundNumber
	Media.PlaySound("ionstorm" .. soundNumber .. ".aud")
end
