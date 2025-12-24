MissionDir = "ca|missions/main-campaign/ca28-duality"

RespawnEnabled = Map.LobbyOption("respawn") == "enabled"

ProdigyPatrolPath = { ProdigyPatrol1.Location, ProdigyPatrol2.Location, ProdigyPatrol3.Location, ProdigyPatrol4.Location, ProdigyPatrol5.Location, ProdigyPatrol6.Location, ProdigyPatrol7.Location, ProdigyPatrol8.Location, ProdigyPatrol9.Location, ProdigyPatrol10.Location, ProdigyPatrol11.Location, ProdigyPatrol12.Location, ProdigyPatrol13.Location, ProdigyPatrol14.Location, ProdigyPatrol15.Location, ProdigyPatrol16.Location, ProdigyPatrol17.Location, ProdigyPatrol18.Location, ProdigyPatrol19.Location, ProdigyPatrol9.Location, ProdigyPatrol8.Location, ProdigyPatrol20.Location }

ScrinReinforcementInterval = {
	easy = DateTime.Seconds(40),
	normal = DateTime.Seconds(30),
	hard = DateTime.Seconds(20),
	vhard = DateTime.Seconds(15),
	brutal = DateTime.Seconds(15)
}

ScrinWaveInterval = {
	vhard = DateTime.Seconds(120),
	brutal = DateTime.Seconds(60)
}

SetupPlayers = function()
	GDI = Player.GetPlayer("GDI")
	Scrin = Player.GetPlayer("Scrin")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { GDI }
	MissionEnemies = { Scrin }
end

WorldLoaded = function()
	SetupPlayers()

	Camera.Position = Commando.CenterPosition

	InitObjectives(GDI)
	InitScrin()

	Utils.Do(MissionPlayers, function(p)
		Actor.Create("radar.dummy", true, { Owner = p })
	end)

	Commando.GrantCondition("difficulty-" .. Difficulty)
	Tanya.GrantCondition("difficulty-" .. Difficulty)

	SetupFindTanyaObjective()
	ObjectiveDestroyTiberiumStores = GDI.AddObjective("Destroy all Scrin Tiberium stores.")
	SetupKeepAliveObjectives()

	CommandoDeathTrigger(Commando)
	TanyaDeathTrigger(Tanya)

	local silos = Scrin.GetActorsByTypes({ "silo.scrin", "silo.scrinblue"})
	NumSilosRemaining = #silos

	Utils.Do(silos, function(a)
		Trigger.OnKilled(a, function(self, killer)
			SiloKilled(killer)
		end)
	end)

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

	UpdateObjectiveText()
	AfterWorldLoaded()
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	AfterTick()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		if NumSilosRemaining == 0 and not GDI.IsObjectiveCompleted(ObjectiveDestroyTiberiumStores) then
			ObjectiveEscape = GDI.AddObjective("Exit the facility.")
			SetEscapeText()
			GDI.MarkCompletedObjective(ObjectiveDestroyTiberiumStores)
			local exitFlare = Actor.Create("flare", true, { Owner = GDI, Location = Exit.Location })
			Beacon.New(GDI, Exit.CenterPosition)
			PlaySpeechNotificationToMissionPlayers("SignalFlare")
			Notification("Signal flare detected.")
			Trigger.OnEnteredProximityTrigger(Exit.CenterPosition, WDist.New(3 * 1024), function(a, id)
				if IsMissionPlayer(a.Owner) and a.Type ~= "flare" then
					Trigger.AfterDelay(DateTime.Seconds(5), function()
						if not exitFlare.IsDead then
							exitFlare.Destroy()
						end
					end)
					if a.Type == "rmbo" then
						if not ObjectiveFindTanya or GDI.IsObjectiveCompleted(ObjectiveFindTanya) then
							CommandoEscaped = true
							if ObjectiveCommandoSurvive ~= nil then
								GDI.MarkCompletedObjective(ObjectiveCommandoSurvive)
							end
							a.Destroy()
						end
					elseif a.Type == "e7" then
						TanyaEscaped = true
						if ObjectiveTanyaSurvive ~= nil then
							GDI.MarkCompletedObjective(ObjectiveTanyaSurvive)
						end
						a.Destroy()
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
	Scrin.Resources = Scrin.ResourceCapacity

	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)
end

-- overridden in co-op version
UpdateObjectiveText = function()
	UserInterface.SetMissionText("Tiberium stores remaining: " .. NumSilosRemaining , HSLColor.Yellow)
end

ActivateProdigy = function()
	if not Prodigy.IsDead then
		Notification("We're tracking a powerful Scrin unit. Do not engage!")
		MediaCA.PlaySound(MissionDir .. "/c_powerfulscrin.aud", 2)
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
				return not t.IsDead and IsMissionPlayer(t.Owner) and (t.Type == "rmbo" or t.Type == "e7")
			end)
			Utils.Do(closeTargets, function(t)
				if t == ProdigyCurrentTarget then
					maintainCurrentTarget = true
				end
			end)
		end

		-- if current target hasn't been set yet, or it's dead, or the current target isn't close, randomly select a new target
		if not maintainCurrentTarget then
			local possibleTargets = GetMissionPlayersActorsByTypes({ "rmbo", "e7" })
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
		GDI.MarkFailedObjective(ObjectiveCommandoSurvive)
		if RespawnEnabled then
			Notification("Commando respawns in 20 seconds.")
			Trigger.AfterDelay(DateTime.Seconds(20), function()
				local respawnWaypoint = Exit
				if NumSilosRemaining == 0 then
					respawnWaypoint = EscapeRespawn
				end
				Commando = Actor.Create("rmbo", true, { Owner = self.Owner, Location = respawnWaypoint.Location })
				Beacon.New(self.Owner, respawnWaypoint.CenterPosition)
				PlaySpeechNotificationToMissionPlayers("ReinforcementsArrived")
				Commando.GrantCondition("difficulty-" .. Difficulty)
				CommandoDeathTrigger(Commando)
			end)
		end
	end)
end

TanyaDeathTrigger = function(tanya)
	Trigger.OnKilled(tanya, function(self, killer)
		GDI.MarkFailedObjective(ObjectiveTanyaSurvive)
		if RespawnEnabled then
			Notification("Tanya respawns in 20 seconds.")
			Trigger.AfterDelay(DateTime.Seconds(20), function()
				local respawnWaypoint = Exit
				if NumSilosRemaining == 0 then
					respawnWaypoint = EscapeRespawn
				end
				Tanya = Actor.Create("e7", true, { Owner = self.Owner, Location = respawnWaypoint.Location })
				Beacon.New(self.Owner, respawnWaypoint.CenterPosition)
				PlaySpeechNotificationToMissionPlayers("ReinforcementsArrived")
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

	if IsVeryHardOrAbove() and not GDI.IsObjectiveCompleted(ObjectiveDestroyTiberiumStores) then
		timeUntilNext = ScrinWaveInterval[Difficulty]
	end

	Trigger.AfterDelay(timeUntilNext, ScrinReinforcements)
end

-- overridden in co-op version
SetupFindTanyaObjective = function()
	ObjectiveFindTanya = GDI.AddObjective("Find Tanya.")

	Trigger.OnEnteredProximityTrigger(Tanya.CenterPosition, WDist.New(7 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveProximityTrigger(id)
			Tanya.Owner = GDI
			GDI.MarkCompletedObjective(ObjectiveFindTanya)
			MediaCA.PlaySound(MissionDir .. "/c_tanya.aud", 2)
		end
	end)
end

-- overridden in co-op version
SetupKeepAliveObjectives = function()
	if not RespawnEnabled then
		ObjectiveCommandoSurvive = GDI.AddObjective("Commando must survive.")
		ObjectiveTanyaSurvive = GDI.AddObjective("Tanya must survive.")
	else
		ObjectiveCommandoSurvive = GDI.AddSecondaryObjective("Keep Commando alive.")
		ObjectiveTanyaSurvive = GDI.AddSecondaryObjective("Keep Tanya alive.")
	end
end

-- overridden in co-op version
SiloKilled = function(killer)
	NumSilosRemaining = NumSilosRemaining - 1
	UpdateObjectiveText()
end

SetEscapeText = function()
	if GDI.IsObjectiveCompleted(ObjectiveFindTanya) then
		UserInterface.SetMissionText("Exit the facility." , HSLColor.Lime)
	else
		UserInterface.SetMissionText("Find Tanya and exit the facility." , HSLColor.Lime)
	end
end