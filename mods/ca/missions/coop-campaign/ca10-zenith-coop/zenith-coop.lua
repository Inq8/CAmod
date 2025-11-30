
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Nod = Player.GetPlayer("Nod")
	USSR = Player.GetPlayer("USSR")
	USSRUnits = Player.GetPlayer("USSRUnits")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { USSR }
	SinglePlayerPlayer = Nod
	CoopInit()
end

AfterWorldLoaded = function()
	local firstActivePlayer = GetFirstActivePlayer()
	TransferBaseToPlayer(SinglePlayerPlayer, firstActivePlayer)
	StartCashSpread()

	local mcvSpawnCell = CPos.New(38, 106)
	Utils.Do(GetMcvPlayers(), function(p)
		if p ~= firstActivePlayer then
			local mcv = Actor.Create("amcv", true, { Owner = p, Location = mcvSpawnCell })
			mcv.Scatter()
		end
	end)

	LandingCraft1.Owner = MissionPlayers[1]

	if #MissionPlayers >= 2 then
		LandingCraft2.Owner = MissionPlayers[2]
	end

	if #MissionPlayers >= 3 then
		LandingCraft3.Owner = MissionPlayers[3]
	end

	if #MissionPlayers >= 4 then
		local lst = Actor.Create(LandingCraft1.Type, true, { Owner = MissionPlayers[4], Location = LandingCraft1.Location })
		lst.Scatter()
	end

	if #MissionPlayers >= 5 then
		local lst = Actor.Create(LandingCraft2.Type, true, { Owner = MissionPlayers[5], Location = LandingCraft2.Location })
		lst.Scatter()
	end

	if #MissionPlayers >= 6 then
		local lst = Actor.Create(LandingCraft3.Type, true, { Owner = MissionPlayers[6], Location = LandingCraft3.Location })
		lst.Scatter()
	end
end

AfterTick = function()

end
