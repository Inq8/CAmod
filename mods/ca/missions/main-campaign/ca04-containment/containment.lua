MissionDir = "ca/missions/main-campaign/ca04-containment"

RespawnEnabled = Map.LobbyOption("respawn") == "enabled"

Patrols = {
	{
		Units = { PatrolBoat, PatrolBoat2 },
		Path = { BoatPatrol1.Location, BoatPatrol2.Location, BoatPatrol3.Location, BoatPatrol2.Location }
	},
	{
		Units = { PatrollerA1, PatrollerA2, PatrollerA3, PatrollerA4, PatrollerA5, PatrollerA6, PatrollerA7 },
		Path = { PatrolA1.Location, PatrolA2.Location, PatrolA3.Location, PatrolA4.Location, PatrolA5.Location, PatrolA6.Location, PatrolA1.Location, PatrolA7.Location, PatrolA8.Location, PatrolA9.Location, PatrolA8.Location, PatrolA7.Location }
	},
	{
		Units = { PatrollerB1, PatrollerB2, PatrollerB3, PatrollerB4, PatrollerB5, PatrollerB6 },
		Path = { PatrolB1.Location, PatrolB2.Location, PatrolB3.Location, PatrolB2.Location, PatrolB4.Location, PatrolB5.Location, PatrolB6.Location, PatrolB7.Location, PatrolB8.Location, PatrolB9.Location, PatrolB8.Location, PatrolB7.Location, PatrolB6.Location, PatrolB5.Location, PatrolB4.Location }
	},
	{
		Units = { PatrollerC1, PatrollerC2, PatrollerC3, PatrollerC4, PatrollerC5, PatrollerC6, PatrollerC7, PatrollerC8, PatrollerC9, PatrollerC10 },
		Path = { PatrolC1.Location, PatrolC2.Location, PatrolC3.Location, PatrolC4.Location, PatrolC5.Location, PatrolC4.Location, PatrolC3.Location, PatrolC2.Location }
	},
	{
		Units = { PatrollerD1, PatrollerD2, PatrollerD3, PatrollerD4, PatrollerD5, PatrollerD6 },
		Path = { PatrolD1.Location, PatrolD2.Location, PatrolD3.Location, PatrolD4.Location, PatrolD3.Location, PatrolD2.Location }
	},
	{
		Units = { PatrollerE1, PatrollerE2, PatrollerE3, PatrollerE4, PatrollerE5, PatrollerE6 },
		Path = { PatrolE1.Location, PatrolE2.Location, PatrolE3.Location, PatrolE4.Location, PatrolE5.Location, PatrolE6.Location, PatrolE5.Location, PatrolE7.Location, PatrolE8.Location, PatrolE9.Location, PatrolE10.Location, PatrolE11.Location, PatrolE10.Location, PatrolE9.Location, PatrolE8.Location, PatrolE7.Location, PatrolE5.Location, PatrolE4.Location, PatrolE3.Location, PatrolE2.Location }
	},
	{
		Units = { PatrollerF1, PatrollerF2, PatrollerF3, PatrollerF4, PatrollerF5, PatrollerF6, PatrollerF7 },
		Path = { PatrolF1.Location, PatrolF2.Location, PatrolF3.Location, PatrolF4.Location, PatrolF3.Location, PatrolF2.Location }
	}
}

Reactors = { EastReactor, SouthReactor, WestReactor }

ShoreSAMs = { ShoreSAM1, ShoreSAM2, ShoreSAM3, ShoreSAM4, ShoreSAM5 }
ShoreSAMFlareLocation = ShoreSAM5.Location
ShoreSAMBeaconPosition = ShoreSAM5.CenterPosition

MissileSilos = { TopSilo, BottomSilo }

NukeTimer = {
	easy = DateTime.Minutes(40),
	normal = DateTime.Minutes(30),
	hard = DateTime.Minutes(20),
	vhard = DateTime.Minutes(20),
	brutal = DateTime.Minutes(20),
}

-- Setup and Tick

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	Scrin = Player.GetPlayer("Scrin")
	MissionPlayers = { Greece }
	MissionEnemies = { USSR }
	TimerTicks = 0

	InitObjectives(Greece)
	InitUSSR()

	Camera.Position = PlayerStart.CenterPosition

	Lighting.Ambient = 0.22
	Lighting.Red = 2.5
	Lighting.Blue = 0.85
	Lighting.Green = 1.6

	Sunrise()

	ObjectiveKillReactors = Greece.AddObjective("Destroy the three Atomic Reactors.")
	ObjectiveKillSAMSites = Greece.AddObjective("Destroy Soviet SAM sites along shoreline.")
	ObjectiveKillSilos = Greece.AddObjective("Destroy Atom Bomb silos before they launch.")
	ObjectiveNeutralizeChronosphere = Greece.AddObjective("Neutralize the Chronosphere.")
	ObjectivePreserveSEALs = Greece.AddSecondaryObjective("Keep both SEALs alive.")

	LandingCraft.Move(LandingCraftExit.Location)
	LandingCraft.Destroy()

	RespawnTrigger(Seal1)
	RespawnTrigger(Seal2)
	RespawnTrigger(Spy)

	Actor.Create("radar.dummy", true, { Owner = Greece })

	local seals = { Seal1, Seal2 }
	Utils.Do(seals, function(a)
		Trigger.OnKilled(a, function(self, killer)
			Greece.MarkFailedObjective(ObjectivePreserveSEALs)

			if Seal1.IsDead and Seal2.IsDead then
				BothSealsDead = true
			end

			if BothSealsDead and not RespawnEnabled then
				if not AllReactorsDead then
					Greece.MarkFailedObjective(ObjectiveKillReactors)
				end

				if not BothNukeSilosDead then
					Greece.MarkFailedObjective(ObjectiveKillSilos)
				end

				if not AllSAMSitesDead then
					Greece.MarkFailedObjective(ObjectiveKillSAMSites)
				end

				if not AllReactorsDead or not BothNukeSilosDead or not AllSAMSitesDead then
					Greece.MarkFailedObjective(ObjectiveNeutralizeChronosphere)
				end
			end
		end)
	end)

	Trigger.OnKilled(WestReactor, function(self, killer)
		DisableWestTeslas()
	end)

	Trigger.OnKilled(EastReactor, function(self, killer)
		DisableEastTeslas()
		if SouthReactor.IsDead then
			DisableNorthTeslas()
		end
	end)

	Trigger.OnKilled(SouthReactor, function(self, killer)
		DisableSouthTeslas()
		DoShoreSAMFlare()
		if EastReactor.IsDead then
			DisableNorthTeslas()
		end
	end)

	Trigger.OnAllKilled(Reactors, function()
		AllReactorsDead = true
		Greece.MarkCompletedObjective(ObjectiveKillReactors)
	end)

	Trigger.OnAllKilled(ShoreSAMs, function()
		AllSAMSitesDead = true
		Greece.MarkCompletedObjective(ObjectiveKillSAMSites)

		if BothNukeSilosDead then
			DropChronoPrison()
		end
	end)

	Trigger.OnAllKilled(MissileSilos, function()
		BothNukeSilosDead = true
		Greece.MarkCompletedObjective(ObjectiveKillSilos)

		if not NukeDummy.IsDead then
			NukeDummy.Destroy()
		end

		if AllSAMSitesDead then
			DropChronoPrison()
		end
	end)

	Trigger.OnKilled(Chronosphere, function(self, killer)
		if not Greece.IsObjectiveFailed(ObjectivePreserveSEALs) then
			Greece.MarkCompletedObjective(ObjectivePreserveSEALs)
		end
		Greece.MarkCompletedObjective(ObjectiveNeutralizeChronosphere)
	end)

	if IsNormalOrAbove() then
		HealCrate1.Destroy()
		HealCrate3.Destroy()
		if IsHardOrAbove() then
			HealCrate2.Destroy()
		end
	end

	if IsNormalOrBelow() then
		V22.Destroy()

		Seal1.GrantCondition("difficulty-" .. Difficulty)
		Seal2.GrantCondition("difficulty-" .. Difficulty)

		Trigger.AfterDelay(DateTime.Seconds(3), function()
			Tip('Disguise your spy by "attacking" enemy infantry. Dogs can see through the disguise.')
			Tip("Navy SEALs can swim.")
		end)

		if Difficulty == "easy" then
			V21.Destroy()
		end
	end

	if IsHardOrBelow() then
		PatrollerA7.Destroy()
		PatrollerC10.Destroy()
		PatrolBoat2.Destroy()
	end

	if IsVeryHardOrBelow() then
		BrutalOnlyV2_1.Destroy()
		BrutalOnlyV2_2.Destroy()
		Utils.Do(Patrols[7].Units, function(a)
			a.Destroy()
		end)
	end

	Trigger.OnEnteredProximityTrigger(Chronosphere.CenterPosition, WDist.New(8192), function(a, id)
		if not IsChronosphereReached and IsMissionPlayer(a.Owner) then
			IsChronosphereReached = true
			Trigger.RemoveProximityTrigger(id)
			SAPC1.GrantCondition("cloak-force-disabled", 25)
			SAPC2.GrantCondition("cloak-force-disabled", 25)
			Trigger.AfterDelay(DateTime.Seconds(3), function()
				SAPC1.Move(SAPCRally1.Location)
				SAPC1.Move(NodExit.Location)
				SAPC1.Destroy()
				SAPC2.Move(SAPCRally2.Location)
				SAPC2.Move(NodExit.Location)
				SAPC2.Destroy()
			end)
		end
	end)

	Trigger.AfterDelay(NukeTimer[Difficulty] + DateTime.Seconds(3), function()
		if not NukeDummy.IsDead then
			if not TopSilo.IsDead then
				TopSilo.ActivateNukePower(Chronosphere.Location)
			elseif not BottomSilo.IsDead then
				BottomSilo.ActivateNukePower(Chronosphere.Location)
			end
			NukeDummy.Destroy()
			Media.PlaySound("nukelaunch.aud")
			Media.PlaySpeechNotification(Greece, "AbombLaunchDetected")
			Notification("A-Bomb launch detected.")

			Trigger.AfterDelay(DateTime.Seconds(15), function()
				WhiteOut = true
				Media.PlaySound("crossrip.aud")
				Trigger.AfterDelay(DateTime.Seconds(2), function()
					if not Greece.IsObjectiveCompleted(ObjectiveKillSilos) then
						Greece.MarkFailedObjective(ObjectiveKillSilos)
					end
					if not Greece.IsObjectiveCompleted(ObjectiveNeutralizeChronosphere) then
						Greece.MarkFailedObjective(ObjectiveNeutralizeChronosphere)
					end
				end)
			end)
		end
	end)

	AfterWorldLoaded()
end

Sunrise = function()
	local active = false

	if Lighting.Ambient < 0.85 then
		Lighting.Ambient = Lighting.Ambient + 0.0002
		active = true
	end

	if Lighting.Red > 0.9 then
		Lighting.Red = Lighting.Red - 0.0005
		active = true
	end

	if Lighting.Green > 1.25 then
		Lighting.Green = Lighting.Green - 0.0002
		active = true
	end

	if active then
		Trigger.AfterDelay(5, function()
			Sunrise()
		end)
	end
end

DisableEastTeslas = function()
	local teslaCoils = { EastTesla1, EastTesla2, EastTesla3, EastTesla4, EastTesla5 }
	Utils.Do(teslaCoils, function(self)
		if not self.IsDead then
			self.GrantCondition("disabled")
		end
	end)
end

DisableSouthTeslas = function()
	local teslaCoils = { SouthTesla1, SouthTesla2 }
	Utils.Do(teslaCoils, function(self)
		if not self.IsDead then
			self.GrantCondition("disabled")
		end
	end)
end

DisableWestTeslas = function()
	local teslaCoils = { WestTesla1, WestTesla2, WestTesla3, WestTesla4, WestTesla5 }
	Utils.Do(teslaCoils, function(self)
		if not self.IsDead then
			self.GrantCondition("disabled")
		end
	end)
end

DisableNorthTeslas = function()
	local teslaCoils = { NorthTesla1, NorthTesla2, NorthTesla3, NorthTesla4, NorthTesla5, NorthTesla6 }
	Utils.Do(teslaCoils, function(self)
		if not self.IsDead then
			self.GrantCondition("disabled")
		end
	end)
end

DoShoreSAMFlare = function()
	Trigger.AfterDelay(DateTime.Seconds(4), function()
		ShoreSAMFlare = Actor.Create("flare", true, { Owner = Greece, Location = ShoreSAMFlareLocation })
		Notification("Signal flare detected. A shore SAM Site has been located.")
		MediaCA.PlaySound(MissionDir .. "/r_samlocated.aud", 2)
		Beacon.New(Greece, ShoreSAMBeaconPosition)
		Trigger.AfterDelay(DateTime.Seconds(10), function()
			ShoreSAMFlare.Destroy()
		end)
	end)
end

Tick = function()
	if WhiteOut ~= nil and WhiteOut then
		Lighting.Ambient = Lighting.Ambient + 0.015
		Lighting.Blue = Lighting.Blue + 0.01
		Lighting.Green = Lighting.Green + 0.005
	end

	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	AfterTick()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		USSR.Resources = USSR.ResourceCapacity - 500

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

	end
end

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % DateTime.Seconds(30) == 0 then
		ResetBoats()
	end
end

-- Functions

InitUSSR = function()
	AutoRepairBuildings(USSR)

	Actor.Create("ai.unlimited.power", true, { Owner = USSR })

	local ussrGroundAttackers = USSR.GetGroundAttackers()

	Utils.Do(ussrGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(4096), IsUSSRGroundHunterUnit)
	end)

	if IsHardOrAbove() then
		NukeDummy = Actor.Create("NukeDummyHard", true, { Owner = USSR, Location = Chronosphere.Location })
	elseif Difficulty == "easy" then
		NukeDummy = Actor.Create("NukeDummyEasy", true, { Owner = USSR, Location = Chronosphere.Location })
	else
		NukeDummy = Actor.Create("NukeDummyNormal", true, { Owner = USSR, Location = Chronosphere.Location })
	end

	if IsNormalOrBelow() then
		local sovietInf = USSR.GetActorsByTypes({ "e1", "e2", "e3", "e4", "shok", "dog" })
		Utils.Do(sovietInf, function(a)
			a.GrantCondition("difficulty-" .. Difficulty)
		end)
	end

	Trigger.AfterDelay(1, function()
		Utils.Do(Patrols, function(p)
			Utils.Do(p.Units, function(unit)
				if not unit.IsDead then
					unit.Patrol(p.Path, true)
				end
			end)
		end)
	end)
end

ResetBoats = function()
	Utils.Do(Patrols[1].Units, function(boat)
		if not boat.IsDead then
			local enemies = Utils.Where(Map.ActorsInCircle(boat.CenterPosition, WDist.New(5120)), function(a)
				return IsMissionPlayer(a.Owner)
			end)
			if #enemies == 0 then
				boat.Stop()
				boat.Patrol(Patrols[1].Path, true)
			end
		end
	end)
end

DropChronoPrison = function()
	ChronoPrisonFlare = Actor.Create("flare", true, { Owner = Greece, Location = CarryallDropPoint.Location })
	Media.PlaySpeechNotification(Greece, "SignalFlare")

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Notification("Signal flare detected. Reinforcements inbound.")
		Beacon.New(Greece, CarryallDropPoint.CenterPosition)
	end)

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		local entryPath = { CarryallEntryPoint.Location, CarryallDropPoint.Location }
		local exitPath =  { CarryallEntryPoint.Location }
		ReinforcementsCA.ReinforceWithTransport(Greece, "ocar.chpr", nil, entryPath, exitPath)
		Notification("Rendezvous with the Chrono Prison and proceed to the Chronosphere.")
		MediaCA.PlaySound(MissionDir .. "/r_cprendezvous.aud", 2)

		Trigger.OnEnteredProximityTrigger(CarryallDropPoint.CenterPosition, WDist.New(2048), function(a, id)
			if IsMissionPlayer(a.Owner) and a.Type == "chpr" then
				Trigger.RemoveProximityTrigger(id)
				ChronoPrisonFlare.Destroy()

				local chronoPrisons = Greece.GetActorsByType("chpr")
				local chronoPrison = chronoPrisons[1]
				chronoPrison.GrantCondition("difficulty-" .. Difficulty)

				Trigger.OnKilled(chronoPrison, function(self, killer)
					if RespawnEnabled then
						DropChronoPrison()
					else
						Greece.MarkFailedObjective(ObjectiveNeutralizeChronosphere)
					end
				end)
			end
		end)
	end)
end

RespawnTrigger = function(a)
	if RespawnEnabled then
		Trigger.OnKilled(a, function(self, killer)
			local name
			if a.Type == "spy" then
				name = "Spy"
			else
				name = "SEAL"
			end
			Notification(name .. " respawns in 20 seconds.")
			Trigger.AfterDelay(DateTime.Seconds(20), function()
				local respawnedActor = Actor.Create(a.Type, true, { Owner = a.Owner, Location = PlayerStart.Location })
				Beacon.New(a.Owner, PlayerStart.CenterPosition)
				Media.PlaySpeechNotification(a.Owner, "ReinforcementsArrived")
				if a.Type == "seal" then
					respawnedActor.GrantCondition("difficulty-" .. Difficulty)
				end
				RespawnTrigger(respawnedActor)
			end)
		end)
	end
end
