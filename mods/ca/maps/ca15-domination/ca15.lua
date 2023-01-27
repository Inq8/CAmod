
Patrols = {
	{
		Units = { NodPatroller1a, NodPatroller1b, NodPatroller1c },
		Path = { NodPatrol1a.Location, NodPatrol1b.Location, NodPatrol1c.Location, NodPatrol1d.Location, NodPatrol1c.Location, NodPatrol1b.Location }
	},
	{
		Units = { NodPatroller2a, NodPatroller2b, NodPatroller2c },
		Path = { NodPatrol2a.Location, NodPatrol2b.Location }
	},
	{
		Units = { NodPatroller3a, NodPatroller3b, NodPatroller3c, NodPatroller3d },
		Path = { NodPatrol3a.Location, NodPatrol3b.Location, NodPatrol3c.Location, NodPatrol3b.Location }
	},
	{
		Units = { NodPatroller4a, NodPatroller4b, NodPatroller4c },
		Path = { NodPatrol4a.Location, NodPatrol4b.Location, NodPatrol4c.Location, NodPatrol4d.Location, NodPatrol4e.Location }
	},
}

StartPowerPlants = { StartPower1, StartPower2 }
SouthWestPowerPlants = { SouthWestPower1, SouthWestPower2, SouthWestPower3, SouthWestPower4, SouthWestPower5, SouthWestPower6, SouthWestPower7, SouthWestPower8, SouthWestPower9 }
SouthEastPowerPlants = { SouthEastPower1, SouthEastPower2, SouthEastPower3, SouthEastPower4 }
NorthPowerPlants = { NorthPower1, NorthPower2, NorthPower3, NorthPower4 }

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Nod = Player.GetPlayer("Nod")
	MissionPlayer = USSR
	TimerTicks = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(USSR)
	InitNod()

	ObjectiveStealCodes = USSR.AddObjective("Steal Nod cyborg encryption codes.")
	ObjectiveKeepYuriAlive = USSR.AddObjective("Yuri must survive.")

	if Difficulty == "easy" then
		StartLightTank.Destroy()
		StartTurret3.Destroy()
		SouthWestTurret1.Destroy()
		SouthWestTurret2.Destroy()
		FirstStealthTank.Destroy()
	else
		EasyGren1.Destroy()
		EasyGren2.Destroy()
	end

	if Difficulty == "hard" then
		HealCrate1.Destroy()
		HealCrate2.Destroy()
	end

	if Difficulty ~= "hard" then
		HardOnlyAcolyte1.Destroy()
		HardOnlyAcolyte2.Destroy()
		HardOnlyAcolyte3.Destroy()
		HardOnlyChemWarrior1.Destroy()
		HardOnlyChemWarrior2.Destroy()
		NodCommando.Destroy()
		SouthStealthTank.Destroy()
		HardOnlyTurret1.Destroy()
	end

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Tip("Yuri can mind control up to three enemy units. Mind controlling a fourth will make him lose control of the earliest controlled.")
		Trigger.AfterDelay(DateTime.Seconds(3), function()
			Tip("Deploying Yuri releases a mind blast around Yuri and his slaves, releasing the slaves in the process.")
		end)
	end)

	Trigger.OnAllKilled(StartPowerPlants, function()
		local startSAMs = { StartSAM1, StartSAM2 }
		DisableDefenses(startSAMs)
	end)

	Trigger.OnAllKilled(SouthWestPowerPlants, function()
		local centerDefenses = { SouthWestSAM, CenterObelisk1, CenterObelisk2, CenterObelisk3, CenterObelisk4, CenterSAM1, CenterSAM2, CenterSAM3 }
		DisableDefenses(centerDefenses)
		DisableLaserFences()
		Media.PlaySound("powrdn1.aud")
		Yuri.GrantCondition("mutabomb-enabled")
		Trigger.AfterDelay(DateTime.Seconds(3), function()
			Tip("The Genetic Mutation Bomb support power can turn enemy infantry into Brutes under your command. Avoid enemy SAM sites by holding the mouse button when selecting the target, allowing you to control the approach angle.")
		end)
	end)

	Trigger.OnAllKilled(SouthEastPowerPlants, function()
		local southEastDefenses = { SouthEastSAM1, SouthEastSAM2, SouthEastSAM3, SouthEastSAM4, SouthEastObelisk1, SouthEastObelisk2, SouthEastObelisk3, SouthEastObelisk4 }
		DisableDefenses(southEastDefenses)
	end)

	Trigger.OnAllKilled(NorthPowerPlants, function()
		local northDefenses = { NorthSAM1, NorthSAM2, NorthObelisk1, NorthObelisk2, NorthObelisk3, NorthObelisk4 }
		DisableDefenses(northDefenses)
	end)

	Trigger.OnKilled(CyberneticsLab, function(self, killer)
		if not USSR.IsObjectiveCompleted(ObjectiveStealCodes) then
			USSR.MarkFailedObjective(ObjectiveStealCodes)
		end
	end)

	Trigger.OnKilled(Thief, function(self, killer)
		if not USSR.IsObjectiveCompleted(ObjectiveStealCodes) then
			USSR.MarkFailedObjective(ObjectiveStealCodes)
		end
	end)

	Trigger.OnKilled(Yuri, function(self, killer)
		USSR.MarkFailedObjective(ObjectiveKeepYuriAlive)

		if ObjectiveEscape ~= nil and not USSR.IsObjectiveCompleted(ObjectiveEscape) then
			USSR.MarkFailedObjective(ObjectiveEscape)
		end
	end)

	Trigger.OnInfiltrated(CyberneticsLab, function(self, infiltrator)
		Actor.Create("cyborgsdecrypted", true, { Owner = Nod, Location = UpgradeCreationLocation })
		ObjectiveDestroyTemple = USSR.AddObjective("Locate and destroy the Temple of Nod.")
		USSR.MarkCompletedObjective(ObjectiveStealCodes)

		if TempleOfNod.IsDead then
			USSR.MarkCompletedObjective(ObjectiveDestroyTemple)
			InitEvacSite()
		end
	end)

	Trigger.OnKilled(TempleOfNod, function(self, killer)
		ObjectiveEscape = USSR.AddObjective("Bring Yuri to the extraction point.")

		if ObjectiveDestroyTemple ~= nil then
			USSR.MarkCompletedObjective(ObjectiveDestroyTemple)
		end

		if ObjectiveStealCodes ~= nil and USSR.IsObjectiveCompleted(ObjectiveStealCodes) then
			InitEvacSite()
		end
	end)

	Trigger.OnEnteredProximityTrigger(EvacLanding.CenterPosition, WDist.New(2560), function(a, id)
		if ObjectiveEscape ~= nil and not EvacStarted and a.Owner == USSR and a == Yuri then

			if ObjectiveStealCodes == nil or not USSR.IsObjectiveCompleted(ObjectiveStealCodes) then
				Notification("Cybernetics Lab has not been infiltrated!")
				return
			end

			EvacStarted = true
			Trigger.RemoveProximityTrigger(id)

			if EvacFlare ~= nil then
				EvacFlare.Destroy()
			end

			Notification("Extraction transport inbound.")

			Reinforcements.ReinforceWithTransport(USSR, "halo.paradrop", nil, { EvacSpawn.Location, EvacLanding.Location }, nil, function(transport, cargo)
				transport.Land(EvacLanding)
				Trigger.OnPassengerEntered(transport, function(t, passenger)
					if passenger == Yuri then
						EvacExiting = true
						t.Move(EvacSpawn.Location)
						Trigger.AfterDelay(DateTime.Seconds(2), function()
							USSR.MarkCompletedObjective(ObjectiveEscape)
							USSR.MarkCompletedObjective(ObjectiveKeepYuriAlive)
						end)
					end
				end)
			end)
		end
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Nod.Cash = Nod.ResourceCapacity - 500
		Nod.Resources = Nod.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocation()
	end
end

InitNod = function()
	RebuildExcludes.Nod = { Types = { "nuke", "nuk2", "tmpl", "afac", "obli", "gun.nod" } }

	AutoRepairAndRebuildBuildings(Nod, 10)
	SetupRefAndSilosCaptureCredits(Nod)
	AutoReplaceHarvesters(Nod)

	Actor.Create("POWERCHEAT", true, { Owner = Nod, Location = UpgradeCreationLocation })

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, Nod, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)

	Utils.Do(Patrols, function(p)
		Utils.Do(p.Units, function(unit)
			if not unit.IsDead then
				unit.Patrol(p.Path, true, 10)
			end
		end)
	end)
end

DisableLaserFences = function()
	local fences = Nod.GetActorsByType("lasw")

	Utils.Do(fences, function(a)
		if not a.IsDead then
			a.Destroy()
		end
	end)

	DisableDefenses(Nod.GetActorsByType("lasp"))
end

DisableDefenses = function(actors)
	Utils.Do(actors, function(a)
		if not a.IsDead then
			a.GrantCondition("disabled")
		end
	end)
end

InitEvacSite = function()
	if not EvacSiteInitialized then
		EvacSiteInitialized = true
		Beacon.New(USSR, EvacLanding.CenterPosition)
		EvacFlare = Actor.Create("flare", true, { Owner = USSR, Location = EvacLanding.Location })
	end
end
