BattleTankPatrolPath = { BattleTankPatrol1.Location, BattleTankPatrol2.Location, BattleTankPatrol3.Location, BattleTankPatrol4.Location, BattleTankPatrol5.Location, BattleTankPatrol6.Location, BattleTankPatrol5.Location, BattleTankPatrol4.Location, BattleTankPatrol3.Location, BattleTankPatrol2.Location }

GuardianPatrolPath = { GuardianPatrol1.Location, GuardianPatrol2.Location, GuardianPatrol3.Location, GuardianPatrol4.Location,  GuardianPatrol3.Location, GuardianPatrol2.Location }

AlliedKeyBuildings = { AlliedTechCenter, AlliedWarFactory, AlliedConyard, AlliedRefinery, AlliedRadar }

DroneTipLocations = { DroneTip1.Location, DroneTip2.Location, DroneTip3.Location, DroneTip4.Location, DroneTip5.Location }

EmpTipLocations = { EmpTip1.Location, EmpTip2.Location, EmpTip3.Location, EmpTip4.Location, EmpTip5.Location, EmpTip6.Location }

-- Setup and Tick

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	Greece = Player.GetPlayer("Greece")
	GDI = Player.GetPlayer("GDI")
	MissionPlayer = Nod
	TimerTicks = 0

	Camera.Position = Commando.CenterPosition

	InitObjectives(Nod)
	InitGDI()
	InitGreece()

	if Difficulty == "hard" then
		Hospital.Destroy()
		Actor.Create("nuke", true, { Owner = GDI, Location = Power1Spawn.Location })
		Actor.Create("nuke", true, { Owner = GDI, Location = Power2Spawn.Location })
	else
		Actor.Create("sathack.dummy", true, { Owner = Nod, Location = Commando.Location })

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

	Trigger.OnKilled(IonControl, function(self, killer)
		if ObjectiveHackIonControl ~= nil and not Nod.IsObjectiveCompleted(ObjectiveHackIonControl) then
			Nod.MarkFailedObjective(ObjectiveHackIonControl)
		end
		if ObjectiveDestroyAlliedBase ~= nil and not Nod.IsObjectiveCompleted(ObjectiveDestroyAlliedBase) then
			Nod.MarkFailedObjective(ObjectiveDestroyAlliedBase)
		end
	end)

	Utils.Do(AlliedKeyBuildings, function(b)
		Trigger.OnKilled(b, function(self, killer)
			local intactKeyBuildings = Utils.Where(AlliedKeyBuildings, function(b)
				return not b.IsDead
			end)
			if #intactKeyBuildings == 0 then
				Nod.MarkCompletedObjective(ObjectiveDestroyAlliedBase)
			end
		end)
	end)

	Utils.Do({ Commando, Hacker1, Hacker2 }, function(h)
		Trigger.OnKilled(h, function(self, killer)
			if (Commando.IsDead or (Hacker1.IsDead and Hacker2.IsDead)) and not Nod.IsObjectiveCompleted(ObjectiveHackIonControl) then
				Nod.MarkFailedObjective(ObjectiveHackIonControl)
			end
		end)
	end)

	if Difficulty ~= "hard" then
		Trigger.OnEnteredFootprint(DroneTipLocations, function(a)
			if a.Owner == Nod then
				Trigger.RemoveFootprintTrigger(id)
				Tip("Mammoth Drone detected. Hackers can take control of this vehicle.")
			end
		end)
		Trigger.OnEnteredFootprint(EmpTipLocations, function(a)
			if a.Owner == Nod then
				Trigger.RemoveFootprintTrigger(id)
				Tip("Too many guards up ahead. Find a way to neutralise them.")
			end
		end)
	end
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Greece.Cash = 7500
		Greece.Resources = 7500
		GDI.Cash = 7500
		GDI.Resources = 7500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
		end

		if not IonControlHacked and IonControl.Owner == Nod then
			IonControlHacked = true
			ObjectiveDestroyAlliedBase = Nod.AddObjective("Use the Ion Cannon to destroy the Allied base.")
			Nod.MarkCompletedObjective(ObjectiveHackIonControl)
			UserInterface.SetMissionText("Use the Ion Cannon to destroy the Allied base.", HSLColor.Yellow)
			BaseCamera1 = Actor.Create("camera", true, { Owner = Nod, Location = AlliedBase1.Location })
			BaseCamera2 = Actor.Create("camera", true, { Owner = Nod, Location = AlliedBase2.Location })
			BaseCamera3 = Actor.Create("camera", true, { Owner = Nod, Location = AlliedBase3.Location })
			BaseCamera4 = Actor.Create("camera", true, { Owner = Nod, Location = AlliedBase4.Location })
			BaseCamera5 = Actor.Create("camera", true, { Owner = Nod, Location = AlliedBase5.Location })
			Beacon.New(Nod, AlliedBase2.CenterPosition)
			Trigger.AfterDelay(DateTime.Seconds(6), function()
				BaseCamera1.Destroy()
				BaseCamera2.Destroy()
				BaseCamera3.Destroy()
				BaseCamera4.Destroy()
				BaseCamera5.Destroy()
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
	AutoRepairAndRebuildBuildings(GDI, 15)
	SetupRefAndSilosCaptureCredits(GDI)
	AutoReplaceHarvesters(GDI)
	InitGDIPatrols()
	local gdiGroundAttackers = GDI.GetGroundAttackers()

	Utils.Do(gdiGroundAttackers, function(a)
		TargetSwapChance(a, GDI, 10)
		if a.Type ~= "memp" and a.Type ~= "gdrn" and a.Type ~= "htnk.drone" and a.Type ~= "mtnk.drone" then
			CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGDIGroundHunterUnit)
		end
	end)
end

InitGreece = function()
	AutoRepairAndRebuildBuildings(Greece, 15)
	SetupRefAndSilosCaptureCredits(Greece)
	AutoReplaceHarvesters(Greece)

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

	Trigger.OnEnteredFootprint({ AlliedBoundary1.Location, AlliedBoundary2.Location, AlliedBoundary3.Location, AlliedBoundary4.Location, AlliedBoundary5.Location, AlliedBoundary6.Location }, function(a)
		if a.Owner == Greece and not a.IsDead and a.HasProperty("Move") then
			a.Stop()
			local randomDest = Utils.Random({ AlliedBase1.Location, AlliedBase2.Location, AlliedBase4.Location, AlliedBase5.Location })
			a.Move(randomDest)
		end
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

IsGDIGroundHunterUnit = function(actor)
	return actor.Owner == GDI and actor.HasProperty("Move") and (not actor.HasProperty("Land") or actor.Type == "jjet")and actor.HasProperty("Hunt") and actor.Type ~= "msam" and actor.Type ~= "memp"
end

IsGreeceGroundHunterUnit = function(actor)
	return actor.Owner == Greece and actor.HasProperty("Move") and not actor.HasProperty("Land") and actor.HasProperty("Hunt") and actor.Type ~= "arty" and actor.Type ~= "cryo" and actor.Type ~= "mgg" and actor.Type ~= "mrj"
end
