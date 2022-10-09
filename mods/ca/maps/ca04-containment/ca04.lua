Patrols = {
	{
		Units = { PatrollerA1, PatrollerA2, PatrollerA3, PatrollerA4, PatrollerA5, PatrollerA6 },
		Path = { PatrolA1.Location, PatrolA2.Location, PatrolA3.Location, PatrolA4.Location, PatrolA5.Location, PatrolA6.Location, PatrolA1.Location, PatrolA7.Location, PatrolA8.Location, PatrolA9.Location, PatrolA8.Location, PatrolA7.Location }
	},
	{
		Units = { PatrollerB1, PatrollerB2, PatrollerB3, PatrollerB4, PatrollerB5, PatrollerB6, PatrollerB7 },
		Path = { PatrolB1.Location, PatrolB2.Location, PatrolB3.Location, PatrolB2.Location, PatrolB4.Location, PatrolB5.Location, PatrolB6.Location, PatrolB7.Location, PatrolB8.Location, PatrolB9.Location, PatrolB8.Location, PatrolB7.Location, PatrolB6.Location, PatrolB5.Location, PatrolB4.Location }
	},
	{
		Units = { PatrollerC1, PatrollerC2, PatrollerC3, PatrollerC4, PatrollerC5, PatrollerC6, PatrollerC7, PatrollerC8 },
		Path = { PatrolC1.Location, PatrolC2.Location, PatrolC3.Location, PatrolC2.Location }
	},
	{
		Units = { PatrollerD1, PatrollerD2, PatrollerD3, PatrollerD4, PatrollerD5, PatrollerD6 },
		Path = { PatrolD1.Location, PatrolD2.Location }
	},
	{
		Units = { PatrollerE1, PatrollerE2, PatrollerE3, PatrollerE4, PatrollerE5, PatrollerE6 },
		Path = { PatrolE1.Location, PatrolE2.Location, PatrolE3.Location, PatrolE4.Location, PatrolE5.Location, PatrolE6.Location, PatrolE5.Location, PatrolE7.Location, PatrolE8.Location, PatrolE9.Location, PatrolE10.Location, PatrolE11.Location, PatrolE10.Location, PatrolE9.Location, PatrolE8.Location, PatrolE7.Location, PatrolE5.Location, PatrolE4.Location, PatrolE3.Location, PatrolE2.Location }
	},
	{
		Units = { PatrolBoat },
		Path = { BoatPatrol1.Location, BoatPatrol2.Location, BoatPatrol3.Location, BoatPatrol2.Location }
	}
}

Reactors = { EastReactor, SouthReactor, WestReactor }

ShoreSAMs = { ShoreSAM1, ShoreSAM2, ShoreSAM3, ShoreSAM4, ShoreSAM5 }

MissileSilos = { TopSilo, BottomSilo }

NukeTimer = {
	hard = DateTime.Minutes(15),
	normal = DateTime.Minutes(25),
	easy = DateTime.Minutes(35)
}

-- Setup and Tick

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	Scrin = Player.GetPlayer("Scrin")
	MissionPlayer = Greece
	TimerTicks = 0

	InitObjectives(Greece)
	InitUSSR()
	Camera.Position = PlayerStart.CenterPosition

	Lighting.Ambient = 0.95
	Lighting.Red = 1
	Lighting.Blue = 1
	Lighting.Green = 1.1

	ObjectiveKillReactors = Greece.AddObjective("Destroy the three Atomic Reactors.")
	ObjectiveKillSAMSites = Greece.AddObjective("Destroy Soviet SAM sites along shoreline.")
	ObjectiveKillSilos = Greece.AddObjective("Destroy Atom Bomb silos before they launch.")
	ObjectiveNeutralizeChronosphere = Greece.AddObjective("Neutralize the Chronosphere.")
	ObjectivePreserveSEALs = Greece.AddSecondaryObjective("Keep both SEALs alive.")

	LandingCraft.Move(LandingCraftExit.Location)
	LandingCraft.Destroy()

	local squadMembers = { Spy, Seal1, Seal2 }
	Utils.Do(squadMembers, function(a)
		Trigger.OnKilled(a, function(self, killer)

			if self.Type == "seal" then
				Greece.MarkFailedObjective(ObjectivePreserveSEALs)
			end

			CheckSquad()
		end)
	end)

	Trigger.OnKilled(WestReactor, function(self, killer)
		CheckReactors()
		DisableWestTeslas()
	end)

	Trigger.OnKilled(EastReactor, function(self, killer)
		CheckReactors()
		DisableEastTeslas()
		if SouthReactor.IsDead then
			DisableNorthTeslas()
		end
	end)

	Trigger.OnKilled(SouthReactor, function(self, killer)
		CheckReactors()
		DisableSouthTeslas()
		if EastReactor.IsDead then
			DisableNorthTeslas()
		end
	end)

	Utils.Do(ShoreSAMs, function(a)
		Trigger.OnKilled(a, function(self, killer)
			CheckShoreSAMs()
		end)
	end)

	Utils.Do(MissileSilos, function(a)
		Trigger.OnKilled(a, function(self, killer)
			CheckMissileSilos()
		end)
	end)

	Trigger.OnKilled(Chronosphere, function(self, killer)
		Greece.MarkCompletedObjective(ObjectiveNeutralizeChronosphere)
	end)

	if Difficulty ~= "hard" then
		Trigger.AfterDelay(DateTime.Seconds(3), function()
			Tip('Disguise your spy by "attacking" enemy infantry.')
		end)
	end

	Trigger.OnEnteredProximityTrigger(Chronosphere.CenterPosition, WDist.New(8192), function(a, id)
		if a.Owner == Greece then
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
		end

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
	end)
end

DisableEastTeslas = function()
	EastTesla1.GrantCondition("disabled")
	EastTesla2.GrantCondition("disabled")
	EastTesla3.GrantCondition("disabled")
	EastTesla4.GrantCondition("disabled")
	EastTesla5.GrantCondition("disabled")
end

DisableSouthTeslas = function()
	SouthTesla1.GrantCondition("disabled")
	SouthTesla2.GrantCondition("disabled")
end

DisableWestTeslas = function()
	WestTesla1.GrantCondition("disabled")
	WestTesla2.GrantCondition("disabled")
	WestTesla3.GrantCondition("disabled")
	WestTesla4.GrantCondition("disabled")
	WestTesla5.GrantCondition("disabled")
end

DisableNorthTeslas = function()
	NorthTesla1.GrantCondition("disabled")
	NorthTesla2.GrantCondition("disabled")
	NorthTesla3.GrantCondition("disabled")
	NorthTesla4.GrantCondition("disabled")
	NorthTesla5.GrantCondition("disabled")
	NorthTesla6.GrantCondition("disabled")
end

CheckReactors = function()
	if EastReactor.IsDead and SouthReactor.IsDead and WestReactor.IsDead then
		Greece.MarkCompletedObjective(ObjectiveKillReactors)
	end
end

CheckShoreSAMs = function()
	if ShoreSAM1.IsDead and ShoreSAM2.IsDead and ShoreSAM3.IsDead and ShoreSAM4.IsDead and ShoreSAM5.IsDead then
		Greece.MarkCompletedObjective(ObjectiveKillSAMSites)
	end
end

CheckMissileSilos = function()
	if TopSilo.IsDead and BottomSilo.IsDead then
		if not NukeDummy.IsDead then
			NukeDummy.Destroy()
		end

		Greece.MarkCompletedObjective(ObjectiveKillSilos)
		DropChronoPrison()
	end
end

CheckSquad = function()
	if not Greece.IsObjectiveCompleted(ObjectiveKillReactors) and ((Seal1.IsDead and Seal2.IsDead) or Spy.IsDead) then
		Greece.MarkFailedObjective(ObjectiveKillReactors)
	end
end

Tick = function()
	if WhiteOut ~= nil and WhiteOut then
		Lighting.Ambient = Lighting.Ambient + 0.015
		Lighting.Blue = Lighting.Blue + 0.01
		Lighting.Green = Lighting.Green + 0.005
	end

	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		USSR.Cash = 5000
		USSR.Resources = 5000

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

-- Functions

InitUSSR = function()
	AutoRepairBuildings(USSR)
	local ussrGroundAttackers = USSR.GetGroundAttackers()

	if Difficulty == "hard" then
		NukeDummy = Actor.Create("NukeDummyHard", true, { Owner = USSR, Location = Chronosphere.Location })
	elseif Difficulty == "easy" then
		NukeDummy = Actor.Create("NukeDummyEasy", true, { Owner = USSR, Location = Chronosphere.Location })
	else
		NukeDummy = Actor.Create("NukeDummyNormal", true, { Owner = USSR, Location = Chronosphere.Location })
	end

	Utils.Do(ussrGroundAttackers, function(a)
		TargetSwapChance(a, USSR, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(4096), IsUSSRGroundHunterUnit)
	end)

	Utils.Do(Patrols, function(p)
		Utils.Do(p.Units, function(unit)
			if not unit.IsDead then
				unit.Patrol(p.Path, true)
			end
		end)
	end)
end

IsUSSRGroundHunterUnit = function(actor)
	return actor.Owner == USSR and actor.HasProperty("Move") and not actor.HasProperty("Land") and actor.HasProperty("Hunt") and actor.Type ~= "v2rl" and actor.Type ~= "katy"
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

		Trigger.OnEnteredProximityTrigger(CarryallDropPoint.CenterPosition, WDist.New(2048), function(a, id)
			if a.Owner == Greece and a.Type == "chpr" then
				Trigger.RemoveProximityTrigger(id)
				ChronoPrisonFlare.Destroy()

				local chronoPrisons = Greece.GetActorsByType("chpr")
				Trigger.OnKilled(chronoPrisons[1], function(self, killer)
					Greece.MarkFailedObjective(ObjectiveNeutralizeChronosphere)
				end)
			end
		end)
	end)
end
