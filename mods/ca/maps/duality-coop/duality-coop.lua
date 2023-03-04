
ProdigyPatrolPath = { ProdigyPatrol1.Location, ProdigyPatrol2.Location, ProdigyPatrol3.Location, ProdigyPatrol4.Location, ProdigyPatrol5.Location, ProdigyPatrol6.Location, ProdigyPatrol7.Location, ProdigyPatrol8.Location, ProdigyPatrol9.Location, ProdigyPatrol10.Location, ProdigyPatrol11.Location, ProdigyPatrol12.Location, ProdigyPatrol13.Location, ProdigyPatrol14.Location, ProdigyPatrol15.Location, ProdigyPatrol16.Location, ProdigyPatrol17.Location, ProdigyPatrol18.Location, ProdigyPatrol19.Location, ProdigyPatrol9.Location, ProdigyPatrol8.Location, ProdigyPatrol20.Location }

RespawnEnabled = Map.LobbyOption("respawn") == "enabled"

Objectives = {
	DestroyTiberiumStores = {
		Text = "Destroy all Scrin Tiberium stores.",
	},
	Escape = {
		Text = "Exit the facility.",
	},
	CommandoSurvives = {
		Text = "Commando must survive and escape.",
	},
	TanyaSurvives = {
		Text = "Tanya must survive and escape.",
	},
}

WorldLoaded = function()
    GDI = Player.GetPlayer("GDI")
	Greece = Player.GetPlayer("Greece")
    Scrin = Player.GetPlayer("Scrin")
	MissionPlayer = GDI
	TimerTicks = 0
	Players = { GDI, Greece }

	Camera.Position = Commando.CenterPosition

	InitObjectives(GDI)
	InitObjectives(Greece)
	InitScrin()

	Utils.Do(Players, function(p)
		Objectives.DestroyTiberiumStores[p.Name] = p.AddObjective(Objectives.DestroyTiberiumStores.Text)
	end)

	if not RespawnEnabled then
		Utils.Do(Players, function(p)
			Objectives.CommandoSurvives[p.Name] = p.AddObjective(Objectives.CommandoSurvives.Text)
			Objectives.TanyaSurvives[p.Name] = p.AddObjective(Objectives.TanyaSurvives.Text)
		end)
	end

	Commando.GrantCondition("difficulty-" .. Difficulty)
	Tanya.GrantCondition("difficulty-" .. Difficulty)
	Actor.Create("radar.dummy", true, { Owner = GDI })
	Actor.Create("radar.dummy", true, { Owner = Greece })

	Scrin.Resources = Scrin.ResourceCapacity

	CommandoDeathTrigger(Commando)
	TanyaDeathTrigger(Tanya)

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

			if RespawnEnabled then
				Utils.Do(Players, function(p)
					Objectives.Escape[p.Name] = p.AddObjective(Objectives.Escape.Text)
				end)
			end

			Utils.Do(Players, function(p)
				p.MarkCompletedObjective(Objectives.DestroyTiberiumStores[p.Name])
			end)

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
			if not RespawnEnabled then
				Utils.Do(Players, function(p)
					p.MarkCompletedObjective(Objectives.CommandoSurvives[p.Name])
				end)
			else
				Notification("Commando has exited the facility.")
			end
		end

		if TanyaEscaped then
			if not RespawnEnabled then
				Utils.Do(Players, function(p)
					p.MarkCompletedObjective(Objectives.TanyaSurvives[p.Name])
				end)
			else
				Notification("Tanya has exited the facility.")
			end
		end

		if RespawnEnabled and CommandoEscaped and TanyaEscaped then
			Utils.Do(Players, function(p)
				p.MarkCompletedObjective(Objectives.Escape[p.Name])
			end)
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
		TargetSwapChance(a, 10, function(p) return p == GDI or p == Greece end)
		CallForHelpOnDamagedOrKilled(a, WDist.New(4096), IsScrinGroundHunterUnit, function(p) return p == GDI or p == Greece end)
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

CommandoDeathTrigger = function(commando)
	Trigger.OnKilled(commando, function()
		if not RespawnEnabled and not CommandoEscaped then
			Utils.Do(Players, function(p)
				p.MarkFailedObjective(Objectives.CommandoSurvives[p.Name])
			end)
		elseif RespawnEnabled then
			Notification("Commando respawns in 30 seconds.")
			Trigger.AfterDelay(DateTime.Seconds(30), function()
				local respawnWaypoint = CommandoSpawn
				if NumSilosRemaining == 0 then
					respawnWaypoint = EscapeRespawn
				end
				Commando = Actor.Create("rmbo", true, { Owner = GDI, Location = respawnWaypoint.Location })
				Beacon.New(GDI, respawnWaypoint.CenterPosition)
				Media.PlaySpeechNotification(GDI, "ReinforcementsArrived")
				Commando.GrantCondition("difficulty-" .. Difficulty)
				CommandoDeathTrigger(Commando)
			end)
		end
	end)
end

TanyaDeathTrigger = function(tanya)
	Trigger.OnKilled(tanya, function()
		if not RespawnEnabled and not TanyaEscaped then
			Utils.Do(Players, function(p)
				p.MarkFailedObjective(Objectives.TanyaSurvives[p.Name])
			end)
		elseif RespawnEnabled then
			Notification("Tanya respawns in 30 seconds.")
			Trigger.AfterDelay(DateTime.Seconds(30), function()
				local respawnWaypoint = CommandoSpawn
				if NumSilosRemaining == 0 then
					respawnWaypoint = EscapeRespawn
				end
				Tanya = Actor.Create("e7", true, { Owner = Greece, Location = respawnWaypoint.Location })
				Beacon.New(Greece, respawnWaypoint.CenterPosition)
				Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
				Tanya.GrantCondition("difficulty-" .. Difficulty)
				TanyaDeathTrigger(Tanya)
			end)
		end
	end)
end
