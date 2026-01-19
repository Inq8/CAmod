
local voidFrequencyOptionValue = tonumber(Map.LobbyOption("voidfreq"))

if voidFrequencyOptionValue ~= nil then
	local VoidIntervalMultiplier = 1 + (voidFrequencyOptionValue / 100)
	VoidEngineInterval[Difficulty] = math.max(VoidEngineInterval[Difficulty] / VoidIntervalMultiplier, 25)
end

SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Greece = Player.GetPlayer("Greece")
	MaleficScrin = Player.GetPlayer("MaleficScrin")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { MaleficScrin }
	SinglePlayerPlayer = Greece
	StopSpread = true
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(3500)

	local leftSidePlayers = {}
	local rightSidePlayers = {}
	local isLeft = true

	for _, p in ipairs(MissionPlayers) do
		if isLeft then
			table.insert(leftSidePlayers, p)
		else
			table.insert(rightSidePlayers, p)
		end
		isLeft = not isLeft
	end

	if #leftSidePlayers == 0 then
		leftSidePlayers = rightSidePlayers
	end

	if #rightSidePlayers == 0 then
		rightSidePlayers = leftSidePlayers
	end

	local units = Utils.Where(Greece.GetActors(), function(a) return a.HasProperty("Move") end)
	local leftSideUnits = Utils.Where(units, function(a) return a.Location.X < 112 end)
	local rightSideUnits = Utils.Where(units, function(a) return a.Location.X >= 112 end)

	AssignToCoopPlayers(leftSideUnits, leftSidePlayers)
	AssignToCoopPlayers(rightSideUnits, rightSidePlayers)
end

AfterTick = function()

end

DoMcvArrival = function()
	local mcvPlayers = GetMcvPlayers()

	if #mcvPlayers == 1 and #MissionPlayers > 1 then
		table.insert(mcvPlayers, MissionPlayers[2])
	end

	local mcvPaths = {
		{ McvSpawn1.Location, McvDest1.Location },
		{ McvSpawn2.Location, McvDest2.Location },
		{ CPos.New(McvSpawn1.Location.X - 2, McvSpawn1.Location.Y), CPos.New(McvDest1.Location.X - 2, McvDest1.Location.Y) },
		{ CPos.New(McvSpawn2.Location.X - 2, McvSpawn2.Location.Y), CPos.New(McvDest2.Location.X - 2, McvDest2.Location.Y) },
		{ CPos.New(McvSpawn1.Location.X + 2, McvSpawn1.Location.Y), CPos.New(McvDest1.Location.X + 2, McvDest1.Location.Y) },
		{ CPos.New(McvSpawn2.Location.X + 2, McvSpawn2.Location.Y), CPos.New(McvDest2.Location.X + 2, McvDest2.Location.Y) },
	}

	local i = 1
	Utils.Do(mcvPlayers, function(p)
		Reinforcements.Reinforce(p, { "mcv" }, mcvPaths[i], 75)
		i = i + 1
	end)

	StopSpread = false
end
