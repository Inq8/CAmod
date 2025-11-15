
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	Scrin = Player.GetPlayer("Scrin")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { USSR }
	SinglePlayerPlayer = Greece
	StopSpread = true
	CoopInit()
end

AfterWorldLoaded = function()
	CamlockEnabled = Map.LobbyOption("camlock") == "enabled"
end

AfterTick = function()
	if CamlockEnabled then
		Utils.Do(MissionPlayers, function(p)
			if not p.IsLocalPlayer then
				return
			end
			local validCamTargets = p.GetActorsByTypes({"spy", "seal", "chpr"})
			if #validCamTargets == 1 then
				PanToPos(validCamTargets[1].CenterPosition, 100)
			end
		end)
	end
end

SetupUnits = function()
	Seals = { Seal1, Seal2 }

	local extraSealLocations = {
		CPos.New(93, 8),
		CPos.New(95, 9),
		CPos.New(101, 10),
		CPos.New(103, 10),
	}

	for i = 3, #MissionPlayers do
		local loc = extraSealLocations[i - 2]
		local seal = Actor.Create("seal", true, { Owner = Greece, Location = loc })
		table.insert(Seals, seal)
	end

	AssignToCoopPlayers(Utils.Concat(Seals, { Spy }))

	Utils.Do(Seals, function(a)
		RespawnTrigger(a, a.Location)
	end)

	RespawnTrigger(Spy, Spy.Location)
end

TransferChronoPrisonOwnership = function(chronoPrison)
	chronoPrison.Owner = GetFirstActivePlayer()
end
