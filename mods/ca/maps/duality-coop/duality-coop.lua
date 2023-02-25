
ProdigyPatrolPath = { ProdigyPatrol1.Location, ProdigyPatrol2.Location, ProdigyPatrol3.Location, ProdigyPatrol4.Location, ProdigyPatrol5.Location, ProdigyPatrol6.Location, ProdigyPatrol7.Location, ProdigyPatrol8.Location, ProdigyPatrol9.Location, ProdigyPatrol10.Location, ProdigyPatrol11.Location, ProdigyPatrol12.Location, ProdigyPatrol13.Location, ProdigyPatrol14.Location, ProdigyPatrol15.Location, ProdigyPatrol16.Location, ProdigyPatrol17.Location, ProdigyPatrol18.Location, ProdigyPatrol19.Location, ProdigyPatrol9.Location, ProdigyPatrol8.Location, ProdigyPatrol20.Location }

WorldLoaded = function()
    GDI = Player.GetPlayer("GDI")
	Greece = Player.GetPlayer("Greece")
    Scrin = Player.GetPlayer("Scrin")
	MissionPlayer = GDI
	TimerTicks = 0

	Camera.Position = Commando.CenterPosition

	InitObjectives(GDI)
	InitObjectives(Greece)
	InitScrin()

	ObjectiveDestroyTiberiumStoresGDI = GDI.AddObjective("Destroy all Tiberium stores.")
	ObjectiveCommandoSurvivesGDI = GDI.AddObjective("Commando must survive and escape.")
	ObjectiveTanyaSurvivesGDI = GDI.AddObjective("Tanya must survive and escape.")
	Actor.Create("radar.dummy", true, { Owner = GDI })
	Commando.GrantCondition("difficulty-" .. Difficulty)

	ObjectiveDestroyTiberiumStoresGreece = Greece.AddObjective("Destroy all Tiberium stores.")
	ObjectiveTanyaSurvivesGreece = Greece.AddObjective("Tanya must survive and escape.")
	ObjectiveCommandoSurvivesGreece = Greece.AddObjective("Commando must survive and escape.")
	Actor.Create("radar.dummy", true, { Owner = Greece })
	Tanya.GrantCondition("difficulty-" .. Difficulty)

	Scrin.Resources = Scrin.ResourceCapacity

	Trigger.OnKilled(Commando, function()
		if not CommandoEscaped then
			GDI.MarkFailedObjective(ObjectiveCommandoSurvivesGDI)
			Greece.MarkFailedObjective(ObjectiveCommandoSurvivesGreece)
		end
	end)

	Trigger.OnKilled(Tanya, function()
		if not TanyaEscaped then
			Greece.MarkFailedObjective(ObjectiveTanyaSurvivesGreece)
			GDI.MarkFailedObjective(ObjectiveTanyaSurvivesGDI)
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

		if NumSilosRemaining == 0 and not IsExitActive then
			IsExitActive = true

			UserInterface.SetMissionText("Exit the facility." , HSLColor.Lime)

			GDI.MarkCompletedObjective(ObjectiveDestroyTiberiumStoresGDI)
			Greece.MarkCompletedObjective(ObjectiveDestroyTiberiumStoresGreece)

			local exitFlare = Actor.Create("flare", true, { Owner = GDI, Location = Exit.Location })
			Beacon.New(GDI, Exit.CenterPosition)
			Media.PlaySpeechNotification(GDI, "SignalFlare")
			Notification("Signal flare detected.")
			Trigger.OnEnteredProximityTrigger(Exit.CenterPosition, WDist.New(3 * 1024), function(a, id)
				if (a.Owner == GDI or a.Owner == Greece) and a.Type ~= "flare" then
					Trigger.AfterDelay(DateTime.Seconds(5), function()
						if not exitFlare.IsDead then
							exitFlare.Destroy()
						end
					end)
					if a.Type == "rmbo" then
						CommandoEscaped = true
						Commando.Destroy()
					elseif a.Type == "e7" then
						TanyaEscaped = true
						Tanya.Destroy()
					end
				end
			end)
		end

		if CommandoEscaped then
			GDI.MarkCompletedObjective(ObjectiveCommandoSurvivesGDI)
			Greece.MarkCompletedObjective(ObjectiveCommandoSurvivesGreece)
		end

		if TanyaEscaped then
			Greece.MarkCompletedObjective(ObjectiveTanyaSurvivesGreece)
			GDI.MarkCompletedObjective(ObjectiveTanyaSurvivesGDI)
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
