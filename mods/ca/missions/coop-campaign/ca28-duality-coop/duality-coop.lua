
TimeLimit = {
	easy = DateTime.Minutes(30),
	normal = DateTime.Minutes(25),
	hard = DateTime.Minutes(20),
	vhard = DateTime.Minutes(15),
	brutal = DateTime.Minutes(10),
}

RushTimeBonus = {
	easy = DateTime.Seconds(15),
	normal = DateTime.Seconds(10),
	hard = DateTime.Seconds(8),
	vhard = DateTime.Seconds(5),
	brutal = DateTime.Seconds(3),
}

DefaultText = "\n\n\n\nTiberium stores remaining: "
TimerText = ""
ScoreboardText = "\n---Leaderboard---\n"

SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	GDI = Player.GetPlayer("GDI")
	Scrin = Player.GetPlayer("Scrin")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Scrin }
	SinglePlayerPlayer = GDI
	StopSpread = true
	CoopInit()
end

AfterWorldLoaded = function()
	CamlockEnabled = Map.LobbyOption("camlock") == "enabled"
	Commando.Owner = MissionPlayers[1]
	Tanya.Owner = MissionPlayers[2]
	HealCrate1.Destroy()
	Actor.Create("healcrate", true, { Owner = Neutral, Location = CPos.New(70, 25) })

	if #MissionPlayers > 2 then
		local y = Commando.Location.Y + 1

		local cells = {
			CPos.New(Commando.Location.X, Commando.Location.Y - 1),
			CPos.New(Commando.Location.X, Commando.Location.Y - 2),
			CPos.New(Commando.Location.X, Commando.Location.Y + 1),
			CPos.New(Commando.Location.X, Commando.Location.Y + 2),
		}

		for i = 3, #MissionPlayers do
			local extraCommando = Actor.Create("rmbo", true, { Owner = MissionPlayers[i], Location = cells[i - 2], Facing = Angle.West })
			extraCommando.GrantCondition("difficulty-" .. Difficulty)
			CommandoDeathTrigger(extraCommando)
		end
	end

	TimeLimitMode = Map.LobbyOption("missiontimer")

	if TimeLimitMode == "enabled" then
		TimerTicks = TimeLimit[Difficulty]
	elseif TimeLimitMode == "rush" then
		TimerTicks = DateTime.Minutes(1)
	else
		TimerTicks = 0
	end

	SiloScore = {} -- [player] = totalDamage
	Utils.Do(CoopPlayers, function(p)
		table.insert(SiloScore, {
			Player = p,
			Score = 0
		})
	end)

	UpdateScoreboard()
end

AfterTick = function()
	if CamlockEnabled then
		Utils.Do(MissionPlayers, function(p)
			if not p.IsLocalPlayer then
				return
			end
			local validCamTargets = p.GetActorsByTypes({"rmbo", "e7"})
			if #validCamTargets == 1 then
				PanToPos(validCamTargets[1].CenterPosition, 100)
			end
		end)
	end

	if DateTime.GameTime > 1 and DateTime.GameTime % DateTime.Seconds(1) == 0 then
		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0

				if NumSilosRemaining > 0 then
					if not GDI.IsObjectiveCompleted(ObjectiveDestroyTiberiumStores) then
						GDI.MarkFailedObjective(ObjectiveDestroyTiberiumStores)
					end
				elseif GDI.IsObjectiveCompleted(ObjectiveDestroyTiberiumStores) then
					if not GDI.IsObjectiveCompleted(ObjectiveEscape) then
						GDI.MarkFailedObjective(ObjectiveEscape)
					end
				end
			end
			TimerText = " | Time remaining: " .. UtilsCA.FormatTimeForGameSpeed(TimerTicks)
			UpdateObjectiveText()
		end
	end
end

SetupFindTanyaObjective = function()
	-- not used in co-op
end

SetupKeepAliveObjectives = function()
	if not RespawnEnabled then
		ObjectiveCommandoSurvive = GDI.AddObjective("All Commandos must survive.")
		ObjectiveTanyaSurvive = GDI.AddObjective("Tanya must survive.")
	else
		ObjectiveCommandoSurvive = GDI.AddSecondaryObjective("Keep all Commandos alive.")
		ObjectiveTanyaSurvive = GDI.AddSecondaryObjective("Keep Tanya alive.")
	end
end

UpdateScoreboard = function()
	table.sort(SiloScore, function(a, b)
		return a.Score > b.Score
	end)

	ScoreboardText = "\n---Leaderboard---\n"
	local rank = 1
	Utils.Do(SiloScore, function(entry)
		ScoreboardText = ScoreboardText .. string.format("%d) %-12s  %8d\n", rank, entry.Player.Name, entry.Score)
		rank = rank + 1
	end)
end

UpdateObjectiveText = function()
	local msg = ""
	if NumSilosRemaining > 0 then
		msg = DefaultText .. NumSilosRemaining .. TimerText .. ScoreboardText
	else
		msg = DefaultText .. TimerText .. ScoreboardText
	end
	UserInterface.SetMissionText(msg, HSLColor.Yellow)
end

SiloKilled = function(killer)
	if TimeLimitMode == "rush" then
		TimerTicks = TimerTicks + RushTimeBonus[Difficulty]
	end
	NumSilosRemaining = NumSilosRemaining - 1
	SiloScoring(killer)
	UpdateObjectiveText()
end

SiloScoring = function(killer)
	if IsMissionPlayer(killer.Owner) then
		for _, entry in ipairs(SiloScore) do
			if entry.Player == killer.Owner then
				entry.Score = entry.Score + 1
				UpdateScoreboard()
				break
			end
		end
	end
end

SetEscapeText = function()
	DefaultText = "\n\n\n\nExit the facility."
end
