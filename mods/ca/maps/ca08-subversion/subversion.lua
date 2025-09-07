RespawnEnabled = Map.LobbyOption("respawn") == "enabled"

BattleTankPatrolPath = { BattleTankPatrol1.Location, BattleTankPatrol2.Location, BattleTankPatrol3.Location, BattleTankPatrol4.Location, BattleTankPatrol5.Location, BattleTankPatrol6.Location, BattleTankPatrol5.Location, BattleTankPatrol4.Location, BattleTankPatrol3.Location, BattleTankPatrol2.Location }

GuardianPatrolPath = { GuardianPatrol1.Location, GuardianPatrol2.Location, GuardianPatrol3.Location, GuardianPatrol4.Location,  GuardianPatrol3.Location, GuardianPatrol2.Location }

AlliedKeyBuildings = { AlliedTechCenter, AlliedWarFactory, AlliedConyard, AlliedRefinery, AlliedRadar }

DroneTipLocations = { DroneTip1.Location, DroneTip2.Location, DroneTip3.Location, DroneTip4.Location, DroneTip5.Location }

EmpTipLocations = { EmpTip1.Location, EmpTip2.Location, EmpTip3.Location, EmpTip4.Location, EmpTip5.Location, EmpTip6.Location }

VeryHardAndAboveCompositions = {
	{ Infantry = { "n1", "n1", "n2", "n1", "n1", "n2", "n1" }, MaxTime = DateTime.Minutes(16) },
	{ Infantry = { "n1", "n1", "n2", "n1", "n1", "n2", "ztrp", "n1" }, MinTime = DateTime.Minutes(16) },
}

if Difficulty == "brutal" then
	table.insert(VeryHardAndAboveCompositions, {
		{ Infantry = { "zrai", "zrai", "zrai" }, MinTime = DateTime.Minutes(26) },
	})
end

Squads = {
	Main = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(6)),
		AttackValuePerSecond = {
			vhard = { Min = 5, Max = 15 },
			brutal = { Min = 10, Max = 20 },
		},
		FollowLeader = true,
		ProducerActors = { Infantry = { WestBarracks, SouthBarracks, NorthBarracks } },
		Compositions = AdjustCompositionsForDifficulty(VeryHardAndAboveCompositions),
	},
}

-- Setup and Tick

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	Greece = Player.GetPlayer("Greece")
	GDI = Player.GetPlayer("GDI")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { Nod }
	TimerTicks = 0

	Camera.Position = Commando.CenterPosition

	InitObjectives(Nod)
	InitGDI()
	InitGreece()

	if IsVeryHardOrBelow() then
		VeryHardOnlyTank1.Destroy()
	end

	if IsHardOrAbove() then
		local hospitalLocation = Hospital.Location
		Hospital.Destroy()
		Actor.Create("hosp.husk", true, { Owner = Neutral, Location = hospitalLocation })
	else

		if Difficulty == "normal" then
			Commando.GrantCondition("difficulty-normal")
			Hacker1.GrantCondition("difficulty-normal")
			Hacker2.GrantCondition("difficulty-normal")
			StealthTank1.GrantCondition("difficulty-normal")
			StealthTank2.GrantCondition("difficulty-normal")
		else
			Commando.GrantCondition("difficulty-easy")
			Hacker1.GrantCondition("difficulty-easy")
			Hacker2.GrantCondition("difficulty-easy")
			StealthTank1.GrantCondition("difficulty-easy")
			StealthTank2.GrantCondition("difficulty-easy")
			Actor.Create("sathack.dummy", true, { Owner = Nod, Location = Commando.Location })
		end

		JumpJet1.Destroy()
		JumpJet2.Destroy()
		HardOnlyBattleTank1.Destroy()
		HardOnlyBattleTank2.Destroy()
		HardOnlyBattleTank3.Destroy()
		HardOnlyBattleTank4.Destroy()
		HardOnlyHumvee.Destroy()
		HardOnlyDisruptor1.Destroy()
		HardOnlyDisruptor2.Destroy()
		HardOnlyMammoth.Destroy()

		Trigger.AfterDelay(DateTime.Seconds(3), function()
			Tip("Hackers can remotely take control of enemy structures, defenses and drone vehicles.")
			Tip("Stealth units can be detected by enemy defenses, as well as infantry at close range.")
		end)
	end

	ObjectiveHackIonControl = Nod.AddObjective("Hack into GDI Advanced Comms Center.")

	CommandoDeathTrigger(Commando)
	HackerDeathTrigger(Hacker1)
	HackerDeathTrigger(Hacker2)
	StealthTankDeathTrigger(StealthTank1)
	StealthTankDeathTrigger(StealthTank2)

	Actor.Create("radar.dummy", true, { Owner = Nod })

	Trigger.OnKilled(IonControl, function(self, killer)
		if ObjectiveHackIonControl ~= nil and not Nod.IsObjectiveCompleted(ObjectiveHackIonControl) then
			Nod.MarkFailedObjective(ObjectiveHackIonControl)
		end
		if ObjectiveDestroyAlliedBase ~= nil and not Nod.IsObjectiveCompleted(ObjectiveDestroyAlliedBase) then
			Nod.MarkFailedObjective(ObjectiveDestroyAlliedBase)
		end
	end)

	Trigger.OnAllKilled(AlliedKeyBuildings, function()
		if not Nod.IsObjectiveCompleted(ObjectiveHackIonControl) then
			Nod.MarkCompletedObjective(ObjectiveHackIonControl)
		end
		if ObjectiveDestroyAlliedBase ~= nil then
			Nod.MarkCompletedObjective(ObjectiveDestroyAlliedBase)
		end
	end)

	if IsNormalOrBelow() then
		Trigger.OnEnteredFootprint(DroneTipLocations, function(a, id)
			if IsMissionPlayer(a.Owner) and not DroneTipShown then
				DroneTipShown = true
				Trigger.RemoveFootprintTrigger(id)
				if not MammothDrone.IsDead and MammothDrone.Owner ~= Nod then
					Tip("Mammoth Drone detected. Hackers can take control of this vehicle.")
				end
			end
		end)
	end

	local revealPoints = { EntranceReveal1, EntranceReveal2, EntranceReveal3, EntranceReveal4, BridgeDefendersReveal1, BridgeDefendersReveal2, EmpDroneReveal }
	Utils.Do(revealPoints, function(p)
		Trigger.OnEnteredProximityTrigger(p.CenterPosition, WDist.New(11 * 1024), function(a, id)
			if IsMissionPlayer(a.Owner) and a.Type ~= "smallcamera" then
				Trigger.RemoveProximityTrigger(id)
				if p == BridgeDefendersReveal1 and not BridgeTipShown then
					BridgeTipShown = true
					Tip("Too many guards up ahead. Find a way to neutralise them.")
				end
				if IsNormalOrBelow() and p == EmpDroneReveal and not EmpDroneTipShown then
					EmpDroneTipShown = true
					Media.DisplayMessage("That E.M.P Drone could come in handy.", "Hacker", HSLColor.FromHex("00FF00"))
				end
				local camera = Actor.Create("smallcamera", true, { Owner = Nod, Location = p.Location })
				Trigger.AfterDelay(DateTime.Seconds(4), function()
					camera.Destroy()
				end)
			end
		end)
	end)

	local bridgeParts = Utils.Where(Map.ActorsInWorld, function(b) return b.Type =="brh1" or b.Type == "brh2" or b.Type == "brh3" end)
	local northBridgeParts = Utils.Where(bridgeParts, function(b) return b.Location.Y < 50 end)
	local southBridgeParts = Utils.Where(bridgeParts, function(b) return b.Location.Y > 50 end)

	Trigger.OnAnyKilled(northBridgeParts, function()
		NorthBridgeDestroyed = true
		if SouthBridgeDestroyed then
			InitBridgesObjective()
			Nod.MarkCompletedObjective(ObjectiveDestroyBridges)
		end
	end)

	Trigger.OnAnyKilled(southBridgeParts, function()
		SouthBridgeDestroyed = true
		if NorthBridgeDestroyed then
			InitBridgesObjective()
			Nod.MarkCompletedObjective(ObjectiveDestroyBridges)
		end
	end)

	AfterWorldLoaded()
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
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

		if not IonControlHacked and IsMissionPlayer(IonControl.Owner) then
			IonControlHacked = true
			InitBridgesObjective()
			ObjectiveDestroyAlliedBase = Nod.AddObjective("Use the Ion Cannon to destroy the Allied base.")
			Nod.MarkCompletedObjective(ObjectiveHackIonControl)
			UserInterface.SetMissionText("Destroy bridges then use the Ion Cannon to destroy the Allied base.", HSLColor.Yellow)
			MediaCA.PlaySound("n_useioncannon.aud", 2)
			BaseCamera1 = Actor.Create("camera", true, { Owner = Nod, Location = AlliedBase1.Location })
			BaseCamera2 = Actor.Create("camera", true, { Owner = Nod, Location = AlliedBase2.Location })
			BaseCamera3 = Actor.Create("camera", true, { Owner = Nod, Location = AlliedBase3.Location })
			BaseCamera4 = Actor.Create("camera", true, { Owner = Nod, Location = AlliedBase4.Location })
			BaseCamera5 = Actor.Create("camera", true, { Owner = Nod, Location = AlliedBase5.Location })
			BridgeCamera1 = Actor.Create("camera", true, { Owner = Nod, Location = AlliedBoundary2.Location })
			BridgeCamera2 = Actor.Create("camera", true, { Owner = Nod, Location = AlliedBoundary5.Location })
			Beacon.New(Nod, AlliedBase2.CenterPosition)
			Trigger.AfterDelay(DateTime.Seconds(6), function()
				BaseCamera1.Destroy()
				BaseCamera2.Destroy()
				BaseCamera3.Destroy()
				BaseCamera4.Destroy()
				BaseCamera5.Destroy()
				BridgeCamera1.Destroy()
				BridgeCamera2.Destroy()
			end)
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		-- nothing
	end
end

InitGDI = function()
	RebuildExcludes.GDI = { Types = { "gtwr", "atwr" } }

	AutoRepairAndRebuildBuildings(GDI, 15)
	SetupRefAndSilosCaptureCredits(GDI)
	AutoReplaceHarvesters(GDI)
	InitAiUpgrades(GDI)
	InitGDIPatrols()
	if IsVeryHardOrAbove() then
		InitAttackSquad(Squads.Main, GDI)
	end
	local gdiGroundAttackers = GDI.GetGroundAttackers()

	Utils.Do(gdiGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		if a.Type ~= "memp" and a.Type ~= "gdrn" and a.Type ~= "htnk.drone" and a.Type ~= "mtnk.drone" then
			CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGDIGroundHunterUnit)
		end
	end)
end

InitGreece = function()
	AutoRepairAndRebuildBuildings(Greece, 15)
	SetupRefAndSilosCaptureCredits(Greece)
	AutoReplaceHarvesters(Greece)
	InitAiUpgrades(Greece)

	local greeceGroundAttackers = Greece.GetGroundAttackers()

	Utils.Do(greeceGroundAttackers, function(a)
		Trigger.OnDamaged(a, function(self, attacker, damage)
			if not self.IsDead and not attacker.IsDead and self.HasProperty("Attack") and self.CanTarget(attacker) then
				self.Attack(attacker)
			elseif not self.IsDead and self.HasProperty("Scatter") then
				self.Scatter()
			end
		end)
	end)
end

InitGDIPatrols = function()
	if not GuardianPatroller1.IsDead then
		GuardianPatroller1.Patrol(GuardianPatrolPath, true)
	end

	if not BattleTankPatroller1.IsDead then
		BattleTankPatroller1.Patrol(BattleTankPatrolPath, true)
	end
end

CommandoDeathTrigger = function(commando)
	Trigger.OnKilled(commando, function(self, killer)
		if RespawnEnabled then
			Notification("Commando arriving in 20 seconds.")
			Trigger.AfterDelay(DateTime.Seconds(20), function()
				Commando = Reinforcements.Reinforce(Nod, { "rmbo" }, { Respawn.Location, RespawnRally.Location })[1]
				Beacon.New(Nod, RespawnRally.CenterPosition)
				Media.PlaySpeechNotification(Nod, "ReinforcementsArrived")
				if IsNormalOrBelow() then
					Commando.GrantCondition("difficulty-" .. Difficulty)
				end
				CommandoDeathTrigger(Commando)
			end)
		end
	end)
end

HackerDeathTrigger = function(hacker)
	Trigger.OnKilled(hacker, function(self, killer)
		if #Nod.GetActorsByType("hack") == 0 and not Nod.IsObjectiveCompleted(ObjectiveHackIonControl) then
			if RespawnEnabled then
				Notification("Hacker arriving in 20 seconds.")
				Trigger.AfterDelay(DateTime.Seconds(20), function()
					Hacker = Reinforcements.Reinforce(Nod, { "hack" }, { Respawn.Location, RespawnRally.Location })[1]
					Beacon.New(Nod, RespawnRally.CenterPosition)
					Media.PlaySpeechNotification(Nod, "ReinforcementsArrived")
					if IsNormalOrBelow() then
						Hacker.GrantCondition("difficulty-" .. Difficulty)
					end
					HackerDeathTrigger(Hacker)
				end)
			else
				Nod.MarkFailedObjective(ObjectiveHackIonControl)
			end
		end
	end)
end

StealthTankDeathTrigger = function(stealthTank)
	Trigger.OnKilled(stealthTank, function(self, killer)
		if #Nod.GetActorsByType("stnk.nod") == 0 then
			if RespawnEnabled then
				Notification("Stealth Tank arriving in 20 seconds.")
				Trigger.AfterDelay(DateTime.Seconds(20), function()
					StealthTank = Reinforcements.Reinforce(Nod, { "stnk.nod" }, { Respawn.Location, RespawnRally.Location })[1]
					Beacon.New(Nod, RespawnRally.CenterPosition)
					Media.PlaySpeechNotification(Nod, "ReinforcementsArrived")
					if IsNormalOrBelow() then
						StealthTank.GrantCondition("difficulty-" .. Difficulty)
					end
					StealthTankDeathTrigger(StealthTank)
				end)
			end
		end
	end)
end

InitBridgesObjective = function()
	if ObjectiveDestroyBridges == nil then
		ObjectiveDestroyBridges = Nod.AddObjective("Destroy bridges leading to Allied base.")
	end
end
