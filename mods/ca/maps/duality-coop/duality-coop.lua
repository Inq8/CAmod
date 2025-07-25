
RespawnEnabled = Map.LobbyOption("respawn") == "enabled"

ProdigyPatrolPath = { ProdigyPatrol1.Location, ProdigyPatrol2.Location, ProdigyPatrol3.Location, ProdigyPatrol4.Location, ProdigyPatrol5.Location, ProdigyPatrol6.Location, ProdigyPatrol7.Location, ProdigyPatrol8.Location, ProdigyPatrol9.Location, ProdigyPatrol10.Location, ProdigyPatrol11.Location, ProdigyPatrol12.Location, ProdigyPatrol13.Location, ProdigyPatrol14.Location, ProdigyPatrol15.Location, ProdigyPatrol16.Location, ProdigyPatrol17.Location, ProdigyPatrol18.Location, ProdigyPatrol19.Location, ProdigyPatrol9.Location, ProdigyPatrol8.Location, ProdigyPatrol20.Location }

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

ScrinReinforcementInterval = {
	easy = DateTime.Seconds(45),
	normal = DateTime.Seconds(30),
	hard = DateTime.Seconds(20),
	vhard = DateTime.Seconds(20),
	brutal = DateTime.Seconds(20)
}

ScrinWaveInterval = {
	vhard = DateTime.Seconds(120),
	brutal = DateTime.Seconds(60)
}

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Greece = Player.GetPlayer("Greece")
	Scrin = Player.GetPlayer("Scrin")
	MissionPlayers = { GDI, Greece }
	TimerTicks = 0

	Camera.Position = Commando.CenterPosition

	InitObjectives(GDI)
	InitObjectives(Greece)
	InitScrin()

	Utils.Do(MissionPlayers, function(p)
		Objectives.DestroyTiberiumStores[p.InternalName] = p.AddObjective(Objectives.DestroyTiberiumStores.Text)
	end)

	if not RespawnEnabled then
		Utils.Do(MissionPlayers, function(p)
			Objectives.CommandoSurvives[p.InternalName] = p.AddObjective(Objectives.CommandoSurvives.Text)
			Objectives.TanyaSurvives[p.InternalName] = p.AddObjective(Objectives.TanyaSurvives.Text)
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
		Trigger.OnKilled(a, function(self, killer)
			NumSilosRemaining = NumSilosRemaining - 1
			UpdateObjectiveText()
		end)
	end)

	UpdateObjectiveText()

	if IsHardOrAbove() then
		HealCrate2.Destroy()
		HealCrate3.Destroy()

		if IsVeryHardOrAbove() then
			Trigger.AfterDelay(DateTime.Seconds(30), function()
				InitWormholes()
			end)
		end
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

			UpdateObjectiveText()

			if RespawnEnabled then
				Utils.Do(MissionPlayers, function(p)
					Objectives.Escape[p.InternalName] = p.AddObjective(Objectives.Escape.Text)
				end)
			end

			Utils.Do(MissionPlayers, function(p)
				if not p.IsObjectiveFailed(Objectives.DestroyTiberiumStores[p.InternalName]) then
					p.MarkCompletedObjective(Objectives.DestroyTiberiumStores[p.InternalName])
				end
			end)

			InitWormholes()

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
				Utils.Do(MissionPlayers, function(p)
					p.MarkCompletedObjective(Objectives.CommandoSurvives[p.InternalName])
				end)
			elseif not IsCommandoExitNotified then
				IsCommandoExitNotified = true
				Notification("Commando has exited the facility.")
			end
		end

		if TanyaEscaped then
			if not RespawnEnabled then
				Utils.Do(MissionPlayers, function(p)
					p.MarkCompletedObjective(Objectives.TanyaSurvives[p.InternalName])
				end)
			elseif not IsTanyaExitNotified then
				IsTanyaExitNotified = true
				Notification("Tanya has exited the facility.")
			end
		end

		if RespawnEnabled and CommandoEscaped and TanyaEscaped then
			Utils.Do(MissionPlayers, function(p)
				p.MarkCompletedObjective(Objectives.Escape[p.InternalName])
			end)
		end

		UpdateObjectiveText()
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
	if IsExitActive then
		UserInterface.SetMissionText("Exit the facility. Time remaining: " .. UtilsCA.FormatTimeForGameSpeed(TimerTicks), HSLColor.Lime)
	else
		UserInterface.SetMissionText("Tiberium stores remaining: " .. NumSilosRemaining .. "\n      Time remaining: " .. UtilsCA.FormatTimeForGameSpeed(TimerTicks), HSLColor.Yellow)
	end
end

ActivateProdigy = function()
	if not Prodigy.IsDead then
		Notification("We're tracking a powerful Scrin unit. Do not engage!")
		Prodigy.GrantCondition("activated")
		Beacon.New(GDI, Prodigy.CenterPosition)

		if IsHardOrAbove() then
			UpdateProdigyTarget()
		else
			Prodigy.Patrol(ProdigyPatrolPath)
		end
	end
end

UpdateProdigyTarget = function()
	if not Prodigy.IsDead then
		local maintainCurrentTarget = false

		-- if current target has been set and is not dead, determine whether it's close enough to prevent looking for a new target
		if ProdigyCurrentTarget ~= nil and not ProdigyCurrentTarget.IsDead then
			local closeTargets = Map.ActorsInCircle(Prodigy.CenterPosition, WDist.New(30 * 1024), function(t)
				return not t.IsDead and (t.Type == "rmbo" or t.Type == "e7")
			end)
			Utils.Do(closeTargets, function(t)
				if t == ProdigyCurrentTarget then
					maintainCurrentTarget = true
				end
			end)
		end

		-- if current target hasn't been set yet, or it's dead, or the current target isn't close, randomly select a new target
		if not maintainCurrentTarget then
			local possibleTargets = GDI.GetActorsByTypes({ "rmbo", "e7" })
			if #possibleTargets > 0 then
				ProdigyCurrentTarget = Utils.Random(possibleTargets)
				Prodigy.Stop()
				Prodigy.AttackMove(ProdigyCurrentTarget.Location)
			end
		end
	end

	Trigger.AfterDelay(DateTime.Seconds(15), function()
		UpdateProdigyTarget()
	end)
end

CommandoDeathTrigger = function(commando)
	Trigger.OnKilled(commando, function(self, killer)
		if not RespawnEnabled and not CommandoEscaped then
			Utils.Do(MissionPlayers, function(p)
				p.MarkFailedObjective(Objectives.CommandoSurvives[p.InternalName])
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
	Trigger.OnKilled(tanya, function(self, killer)
		if not RespawnEnabled and not TanyaEscaped then
			Utils.Do(MissionPlayers, function(p)
				p.MarkFailedObjective(Objectives.TanyaSurvives[p.InternalName])
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

InitWormholes = function()
	if WormholesActive then
		return
	end
	WormholesActive = true
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
		local possibleUnits = { "s1", "s1", "s3", "gscr", "brst2", "s2", "s4" }
		for i=1, 4 do
			table.insert(units, Utils.Random(possibleUnits))
		end

		Reinforcements.Reinforce(Scrin, units, { wormhole.Location }, 5, function(a)
			a.Scatter()
			Trigger.AfterDelay(5, function()
				if not a.IsDead then
					a.AttackMove(Exit.Location)
					IdleHunt(a)
				end
			end)
		end)
	end)

	local timeUntilNext = ScrinReinforcementInterval[Difficulty]

	if IsVeryHardOrAbove() and not GDI.IsObjectiveCompleted(Objectives.DestroyTiberiumStores.GDI) then
		timeUntilNext = ScrinWaveInterval[Difficulty]
	end

	Trigger.AfterDelay(timeUntilNext, ScrinReinforcements)
end
