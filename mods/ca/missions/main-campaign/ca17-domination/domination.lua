MissionDir = "ca|missions/main-campaign/ca17-domination"

RespawnEnabled = Map.LobbyOption("respawn") == "enabled"

CommandoRespawnDelay = {
	hard = DateTime.Minutes(3) + DateTime.Seconds(30),
	vhard = DateTime.Minutes(2) + DateTime.Seconds(30),
	brutal = DateTime.Minutes(1) + DateTime.Seconds(30)
}

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
SouthWestPowerPlants = { SouthWestPower1, SouthWestPower2, SouthWestPower3, SouthWestPower4, SouthWestPower5, SouthWestPower6 }
SouthEastPowerPlants = { SouthEastPower1, SouthEastPower2, SouthEastPower3, SouthEastPower4 }
NorthPowerPlants = { NorthPower1, NorthPower2, NorthPower3, NorthPower4 }

VeryHardAndAboveCompositions = {
	Infantry = { "n3", "n1", "n1", "n4", "n3", "n1", "n1", "n1" },
	Infantry = { "n4", "n4", "n4", "n4", "n4" },
}

Squads = {
	Main = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(3)),
		ActiveCondition = function()
			return LaserFencesDown
		end,
		AttackValuePerSecond = {
			vhard = { Min = 10, Max = 15 },
			brutal = { Min = 15, Max = 20 },
		},
		FollowLeader = true,
		RandomProducerActor = true,
		Compositions = AdjustCompositionsForDifficulty(VeryHardAndAboveCompositions),
	},
}

SetupPlayers = function()
	USSR = Player.GetPlayer("USSR")
	Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { USSR }
	MissionEnemies = { Nod }
end

WorldLoaded = function()
	SetupPlayers()

	TimerTicks = 0
	TempleOfNodLocation = TempleOfNod.Location
	LaserFencesDown = false
	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(USSR)
	InitNod()

	ObjectiveStealCodes = USSR.AddObjective("Steal Nod cyborg encryption codes.")

	if Difficulty == "easy" then
		StartLightTank.Destroy()
		StartTurret3.Destroy()
		SouthWestTurret1.Destroy()
		SouthWestTurret2.Destroy()
		FirstStealthTank.Destroy()
		NodAssassin3.Destroy()
	else
		EasyGren1.Destroy()
		EasyGren2.Destroy()
	end

	if IsHardOrAbove() then
		HealCrate1.Destroy()
		HealCrate2.Destroy()
	end

	if IsVeryHardOrBelow() then
		NodAssassin1.Destroy()
		NodAssassin2.Destroy()
		SouthStealthTank.Destroy()

		if IsNormalOrBelow() then
			HardOnlyAcolyte1.Destroy()
			HardOnlyAcolyte2.Destroy()
			HardOnlyAcolyte3.Destroy()
			HardOnlyChemWarrior1.Destroy()
			HardOnlyChemWarrior2.Destroy()
			HardOnlyTurret1.Destroy()
		end
	end

	if RespawnEnabled then
		ObjectiveKeepYuriAlive = USSR.AddSecondaryObjective("Keep Yuri alive.")
		RespawnTrigger(Yuri)
		RespawnTrigger(Thief)
	else
		ObjectiveKeepYuriAlive = USSR.AddObjective("Yuri must survive.")
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
	end

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Tip("Yuri can mind control up to three enemy units. Mind controlling a fourth will kill the earliest controlled.")
		Trigger.AfterDelay(DateTime.Seconds(3), function()
			Tip("Deploying Yuri releases a mind blast around Yuri and his slaves (the slaves will be unharmed).")
		end)
	end)

	Trigger.OnAllKilledOrCaptured(StartPowerPlants, function()
		local startSAMs = { StartSAM1, StartSAM2 }
		DisableDefenses(startSAMs)
	end)

	Trigger.OnAllKilledOrCaptured(SouthWestPowerPlants, function()
		local centerDefenses = { SouthWestSAM, CenterObelisk1, CenterObelisk2, CenterObelisk3, CenterObelisk4, CenterSAM1, CenterSAM2, CenterSAM3, LeftObelisk }
		DisableDefenses(centerDefenses)
		DisableLaserFences()
		Media.PlaySound("powrdn1.aud")
		Actor.Create("powerproxy.mutabomb", true, { Owner = MissionPlayers[1] })
		Trigger.AfterDelay(DateTime.Seconds(3), function()
			Tip("The Genetic Mutation Bomb support power can turn enemy infantry into Brutes under your command. Avoid enemy SAM sites by holding the mouse button when selecting the target, allowing you to control the approach angle.")
		end)
	end)

	Trigger.OnAllKilledOrCaptured(SouthEastPowerPlants, function()
		local southEastDefenses = { SouthEastSAM1, SouthEastSAM2, SouthEastSAM3, SouthEastSAM4, SouthEastObelisk1, SouthEastObelisk2, SouthEastObelisk3, SouthEastObelisk4 }
		DisableDefenses(southEastDefenses)
	end)

	Trigger.OnAllKilledOrCaptured(NorthPowerPlants, function()
		local northDefenses = { NorthSAM1, NorthSAM2, NorthSAM3, NorthSAM4, NorthObelisk1, NorthObelisk2, NorthObelisk3, NorthObelisk4 }
		DisableDefenses(northDefenses)
	end)

	Trigger.OnKilled(CyberneticsLab, function(self, killer)
		if not USSR.IsObjectiveCompleted(ObjectiveStealCodes) then
			USSR.MarkFailedObjective(ObjectiveStealCodes)
		end
	end)

	Trigger.OnInfiltrated(CyberneticsLab, function(self, infiltrator)
		Actor.Create("cyborgsdecrypted", true, { Owner = Nod })
		ObjectiveDestroyTemple = USSR.AddObjective("Locate and destroy the Temple of Nod.")
		USSR.MarkCompletedObjective(ObjectiveStealCodes)

		if TempleOfNod.IsDead then
			USSR.MarkCompletedObjective(ObjectiveDestroyTemple)
			InitEvacSite()
		end

		Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(2)), function()
			MediaCA.PlaySound(MissionDir .. "/r2_codesacquired.aud", 2)
			Notification("Cyborg encryption codes acquired.")

			if EvacStarted then
				SendEvac()
			end
		end)
	end)

	Trigger.OnKilled(TempleOfNod, function(self, killer)
		TempleDestroyed()
	end)

	Trigger.OnSold(TempleOfNod, function(self)
		TempleDestroyed()
	end)

	Trigger.OnEnteredProximityTrigger(EvacLanding.CenterPosition, WDist.New(2560), function(a, id)
		if ObjectiveEscape ~= nil and not EvacStarted and IsMissionPlayer(a.Owner) and a.Type == "yuri" then
			EvacStarted = true
			Trigger.RemoveProximityTrigger(id)

			if ObjectiveStealCodes == nil or not USSR.IsObjectiveCompleted(ObjectiveStealCodes) then
				Notification("Encryption codes are required for mission completion.")
				MediaCA.PlaySound(MissionDir .. "/r2_codesrequired.aud", 2)
				return
			end

			SendEvac()
		end
	end)

	Trigger.OnEnteredProximityTrigger(TempleOfNod.CenterPosition, WDist.New(10 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) and ObjectiveDestroyTemple ~= nil then
			Trigger.RemoveProximityTrigger(id)
			TempleDiscovered()
		end
	end)

	if IsHardOrAbove() then
		Trigger.OnProduction(NorthHand1, function(p, produced)
			if produced.Type == "rmbo" and not produced.IsDead then
				produced.Hunt()

				Trigger.OnKilled(produced, function(self, killer)
					Trigger.AfterDelay(CommandoRespawnDelay[Difficulty], function()
						SpawnCommando()
					end)
				end)
			end
		end)

		Utils.Do({ CommandoTrigger1, CommandoTrigger2 }, function(t)
			Trigger.OnEnteredProximityTrigger(t.CenterPosition, WDist.New(9 * 1024), function(a, id)
				if IsMissionPlayer(a.Owner) and not a.HasProperty("Land") then
					Trigger.RemoveProximityTrigger(id)
					if not CommandosInitialized then
						CommandosInitialized = true
						SpawnCommando()
					end
				end
			end)
		end)
	end

	SetupReveals({ LaserFenceReveal, EntranceReveal1, EntranceReveal2, EntranceReveal3 })
	AfterWorldLoaded()
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	AfterTick()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
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
		UpdatePlayerBaseLocations()
	end
end

InitNod = function()
	RebuildExcludes.Nod = { Types = { "nuke", "nuk2", "tmpl", "obli", "gun.nod", "nsam" }, Actors = { StartHand, StartComms, MidHand, MidComms } }

	AutoRepairAndRebuildBuildings(Nod, 15)
	SetupRefAndSilosCaptureCredits(Nod)
	AutoReplaceHarvesters(Nod)
	InitAiUpgrades(Nod)
	InitAttackSquad(Squads.Main, Nod)

	Actor.Create("ai.unlimited.power", true, { Owner = Nod })

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)

	Trigger.AfterDelay(1, function()
		Utils.Do(Patrols, function(p)
			Utils.Do(p.Units, function(unit)
				if not unit.IsDead then
					unit.Patrol(p.Path, true, 10)
				end
			end)
		end)
	end)
end

SpawnCommando = function()
	if not NorthHand1.IsDead then
		NorthHand1.Produce("rmbo")
	end
end

DisableLaserFences = function()
	LaserFencesDown = true
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

TempleDestroyed = function()
	ObjectiveEscape = USSR.AddObjective("Bring Yuri to the extraction point.")

	if ObjectiveDestroyTemple ~= nil then
		USSR.MarkCompletedObjective(ObjectiveDestroyTemple)
	end

	if ObjectiveStealCodes ~= nil and USSR.IsObjectiveCompleted(ObjectiveStealCodes) then
		InitEvacSite()
	end
end

TempleDiscovered = function()
	if not IsTempleDiscovered then
		IsTempleDiscovered = true
		Beacon.New(USSR, TempleOfNod.CenterPosition)
		Notification("Temple of Nod located.")
		MediaCA.PlaySound(MissionDir .. "/r2_templelocated.aud", 2)
		local autoCamera = Actor.Create("smallcamera", true, { Owner = USSR, Location = TempleOfNodLocation })
		Trigger.AfterDelay(DateTime.Seconds(5), autoCamera.Destroy)
	end
end

InitEvacSite = function()
	if not EvacSiteInitialized then
		EvacSiteInitialized = true
		Beacon.New(USSR, EvacLanding.CenterPosition)
		EvacFlare = Actor.Create("flare", true, { Owner = USSR, Location = EvacLanding.Location })
	end
end

SendEvac = function()
	if EvacFlare ~= nil then
		EvacFlare.Destroy()
	end

	Notification("Extraction transport inbound.")
	MediaCA.PlaySound(MissionDir .. "/r2_extraction.aud", 2)

	Reinforcements.ReinforceWithTransport(USSR, "halo.paradrop", nil, { EvacSpawn.Location, EvacLanding.Location }, nil, function(transport, cargo)
		Trigger.OnPassengerEntered(transport, function(t, passenger)
			t.Stop()
			if passenger.Type == "yuri" then
				EvacExiting = true
				t.Move(EvacSpawn.Location)
				Trigger.AfterDelay(DateTime.Seconds(2), function()
					USSR.MarkCompletedObjective(ObjectiveEscape)
					if ObjectiveKeepYuriAlive ~= nil then
						USSR.MarkCompletedObjective(ObjectiveKeepYuriAlive)
					end
				end)
			end
		end)

		transport.Land(EvacLanding)
	end)
end

RespawnTrigger = function(a)
	Trigger.OnKilled(a, function(self, killer)
		if a.Type == "yuri" then
			message = "Yuri has used his tremendous psionic power to cheat death. He will return in 20 seconds."
			USSR.MarkFailedObjective(ObjectiveKeepYuriAlive)
		else
			message = "Yuri has used his tremendous psionic power to save the Thief from death. He will return in 20 seconds."
		end

		Notification(message)

		local respawnLocation = PlayerStart.Location

		if USSR.IsObjectiveCompleted(ObjectiveStealCodes) then
			if a.Type == "thf" then
				return
			end
			respawnLocation = MidRespawn.Location
		end

		if not RespawnFlare or RespawnFlare.IsDead then
			RespawnFlare = Actor.Create("flare", true, { Owner = a.Owner, Location = respawnLocation })
		end

		Beacon.New(a.Owner, Map.CenterOfCell(respawnLocation), DateTime.Seconds(20))

		Trigger.AfterDelay(DateTime.Seconds(20), function()
			local respawnedActor = Actor.Create(a.Type, true, { Owner = a.Owner, Location = respawnLocation })
			Media.PlaySpeechNotification(a.Owner, "ReinforcementsArrived")
			Beacon.New(a.Owner, Map.CenterOfCell(respawnLocation))
			RespawnTrigger(respawnedActor)
			if not RespawnFlare or RespawnFlare.IsDead then
				RespawnFlare.Destroy()
			end
		end)
	end)
end
