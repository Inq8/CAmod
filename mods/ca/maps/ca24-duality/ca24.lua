
ProdigyPatrolPath = { ProdigyPatrol1.Location, ProdigyPatrol2.Location, ProdigyPatrol3.Location, ProdigyPatrol4.Location, ProdigyPatrol5.Location, ProdigyPatrol6.Location, ProdigyPatrol7.Location, ProdigyPatrol8.Location, ProdigyPatrol9.Location, ProdigyPatrol10.Location, ProdigyPatrol11.Location, ProdigyPatrol12.Location, ProdigyPatrol13.Location, ProdigyPatrol14.Location, ProdigyPatrol15.Location, ProdigyPatrol16.Location, ProdigyPatrol17.Location, ProdigyPatrol18.Location, ProdigyPatrol19.Location, ProdigyPatrol9.Location, ProdigyPatrol8.Location, ProdigyPatrol20.Location }

ScrinReinforcementInterval = {
	easy = DateTime.Seconds(45),
	normal = DateTime.Seconds(30),
	hard = DateTime.Seconds(15),
}

WorldLoaded = function()
    GDI = Player.GetPlayer("GDI")
    Scrin = Player.GetPlayer("Scrin")
	MissionPlayer = GDI
	TimerTicks = 0

	Camera.Position = Commando.CenterPosition

	InitObjectives(GDI)
	InitScrin()

	ObjectiveFindTanya = GDI.AddObjective("Find Tanya.")
	ObjectiveDestroyTiberiumStores = GDI.AddObjective("Destroy all Scrin Tiberium stores.")
	ObjectiveCommandoSurvive = GDI.AddObjective("Commando must survive.")
	ObjectiveTanyaSurvive = GDI.AddObjective("Tanya must survive.")

	Scrin.Resources = Scrin.ResourceCapacity

	Actor.Create("radar.dummy", true, { Owner = GDI })
	Commando.GrantCondition("difficulty-" .. Difficulty)
	Tanya.GrantCondition("difficulty-" .. Difficulty)

	Trigger.OnEnteredProximityTrigger(Tanya.CenterPosition, WDist.New(7 * 1024), function(a, id)
		if a.Owner == GDI then
			Trigger.RemoveProximityTrigger(id)
			Tanya.Owner = GDI
			GDI.MarkCompletedObjective(ObjectiveFindTanya)
		end
	end)

	Trigger.OnKilled(Commando, function()
		if not CommandoEscaped then
			GDI.MarkFailedObjective(ObjectiveCommandoSurvive)
		end
	end)

	Trigger.OnKilled(Tanya, function()
		if not TanyaEscaped then
			GDI.MarkFailedObjective(ObjectiveTanyaSurvive)
		end
	end)

	local silos = Scrin.GetActorsByTypes({ "silo.scrin", "silo.scrinblue"})
	Utils.Do(silos, function(a)
		NumSilosRemaining = #silos
		Trigger.OnKilled(a, function()
			NumSilosRemaining = NumSilosRemaining - 1
			UpdateObjectiveText()
		end)
	end)

	UpdateObjectiveText()

	if Difficulty == "hard" then
		HealCrate2.Destroy()
		HealCrate3.Destroy()
	end

	if Difficulty == "easy" then
		Prodigy.Destroy()
	end

	Trigger.AfterDelay(DateTime.Minutes(1), function()
		ActivateProdigy()
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
				TimerTicks = 0
			end
		end

		if NumSilosRemaining == 0 and not GDI.IsObjectiveCompleted(ObjectiveDestroyTiberiumStores) then
			ObjectiveEscape = GDI.AddObjective("Exit the facility.")

			if GDI.IsObjectiveCompleted(ObjectiveFindTanya) then
				UserInterface.SetMissionText("Exit the facility." , HSLColor.Lime)
			else
				UserInterface.SetMissionText("Find Tanya and exit the facility." , HSLColor.Lime)
			end

			GDI.MarkCompletedObjective(ObjectiveDestroyTiberiumStores)
			local exitFlare = Actor.Create("flare", true, { Owner = GDI, Location = Exit.Location })
			Beacon.New(GDI, Exit.CenterPosition)
			Media.PlaySpeechNotification(GDI, "SignalFlare")
			Notification("Signal flare detected.")
			Trigger.OnEnteredProximityTrigger(Exit.CenterPosition, WDist.New(3 * 1024), function(a, id)
				if a.Owner == GDI and a.Type ~= "flare" then
					Trigger.AfterDelay(DateTime.Seconds(5), function()
						if not exitFlare.IsDead then
							exitFlare.Destroy()
						end
					end)
					if a.Type == "rmbo" then
						if GDI.IsObjectiveCompleted(ObjectiveFindTanya) then
							CommandoEscaped = true
							GDI.MarkCompletedObjective(ObjectiveCommandoSurvive)
							Commando.Destroy()
						end
					elseif a.Type == "e7" then
						TanyaEscaped = true
						GDI.MarkCompletedObjective(ObjectiveTanyaSurvive)
						Tanya.Destroy()
					end
				end
			end)

			InitWormholes()
		end

		if CommandoEscaped and TanyaEscaped then
			GDI.MarkCompletedObjective(ObjectiveEscape)
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		local silos = Scrin.GetActorsByTypes({ "silo.scrin", "silo.scrinblue"})
		if #silos == 0 and NumSilosRemaining > 0 then
			NumSilosRemaining = 0
		end
	end
end

InitScrin = function()
	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)
end

UpdateObjectiveText = function()
	UserInterface.SetMissionText("Tiberium stores remaining: " .. NumSilosRemaining , HSLColor.Yellow)
end

ActivateProdigy = function()
	if not Prodigy.IsDead then
		Notification("We're tracking a powerful Scrin unit. Do not engage!")
		Prodigy.Patrol(ProdigyPatrolPath)
		Prodigy.GrantCondition("difficulty-" .. Difficulty)
		Prodigy.GrantCondition("activated")
		Beacon.New(GDI, Prodigy.CenterPosition)
	end
end

InitWormholes = function()
	Actor.Create("wormhole", true, { Owner = Scrin, Location = WormholeSpawn1.Location })
	Actor.Create("wormhole", true, { Owner = Scrin, Location = WormholeSpawn2.Location })
	Actor.Create("wormhole", true, { Owner = Scrin, Location = WormholeSpawn3.Location })
	Trigger.AfterDelay(DateTime.Seconds(8), function()
		ScrinReinforcements()
	end)
end

ScrinReinforcements = function()
	local wormholes = Scrin.GetActorsByType("wormhole")

	Utils.Do(wormholes, function(wormhole)
		local units = { }
		local possibleUnits = { "s1", "s1", "s3", "gscr", "feed", "s2", "s4" }
		for i=1, 7 do
			table.insert(units, Utils.Random(possibleUnits))
		end

		local units = Reinforcements.Reinforce(Scrin, units, { wormhole.Location }, 5, function(a)
			a.Scatter()
			Trigger.AfterDelay(5, function()
				if not a.IsDead then
					a.AttackMove(Exit.Location)
					IdleHunt(a)
				end
			end)
		end)
	end)

	Trigger.AfterDelay(ScrinReinforcementInterval[Difficulty], ScrinReinforcements)
end
