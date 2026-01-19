
SideAssumedControl = {}

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
	England = Player.GetPlayer("England")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { USSR }
	SinglePlayerPlayer = Greece
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(3500)
end

AfterTick = function()

end

DoMcvArrival = function()
	local entryPoints = {
		CPos.New(37, 160),
		CPos.New(94, 160),
		CPos.New(39, 160),
		CPos.New(92, 160),
		CPos.New(41, 160),
		CPos.New(90, 160),
	}

	local i = 1
	local mcvPlayers = GetMcvPlayers()

	if #mcvPlayers == 1 and #MissionPlayers > 1 then
		table.insert(mcvPlayers, MissionPlayers[2])
	end

	Utils.Do(MissionPlayers, function(p)
		local isMcvPlayer = Utils.Any(mcvPlayers, function(mcvPlayer) return p == mcvPlayer end)
		local entryPoint = entryPoints[i]

		if #mcvPlayers == 1 and isMcvPlayer then
			entryPoint = ReinforcementSpawn.Location
		end

		local transport = Actor.Create("lst", true, { Owner = p, Location = entryPoint })

		if isMcvPlayer then
			local mcv = Actor.Create("mcv", false, { Owner = p })
			transport.LoadPassenger(mcv)
		end

		local tank = Actor.Create("2tnk", false, { Owner = p })
		transport.LoadPassenger(tank)

		if #mcvPlayers < 4 then
			local jeep = Actor.Create("jeep", false, { Owner = p })
			transport.LoadPassenger(jeep)

			if #mcvPlayers < 3 then
				local arty = Actor.Create("arty", false, { Owner = p })
				transport.LoadPassenger(arty)
			end

			if #mcvPlayers < 2 then
				local tank2 = Actor.Create("2tnk", false, { Owner = p })
				transport.LoadPassenger(tank2)
			end
		end

		transport.Move(CPos.New(entryPoint.X, 151))
		i = i + 1

		if p.IsLocalPlayer then
			Camera.Position = transport.CenterPosition
		end
	end)
end

AssumeControl = function(player, side)
	AssumedControl = true

	if SideAssumedControl[side] then
		return
	end

	SideAssumedControl[side] = true

	if player.IsLocalPlayer then
		Notification("Command transfer complete.")
		MediaCA.PlaySound(MissionDir .. "/r_transfer.aud", 2)
	end

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		DestroyFlares()
		local actorsToFlip = Utils.Where(England.GetActors(), function(a) return a.HasProperty("Health") and a.Type ~= "player" end)

		if side == "west" then
			actorsToFlip = Utils.Where(actorsToFlip, function(a) return a.Location.X < 60 end)
		else
			actorsToFlip = Utils.Where(actorsToFlip, function(a) return a.Location.X > 90 end)
		end

		Utils.Do(actorsToFlip, function(a)
			if a.HasProperty("Move") and not IsHarvester(a) then
				a.Owner = Greece
			else
				a.Owner = player
			end
		end)

		CACoopQueueSyncer()
	end)
end
