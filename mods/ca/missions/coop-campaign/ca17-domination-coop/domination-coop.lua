
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	USSR = Player.GetPlayer("USSR")
	Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	SinglePlayerPlayer = USSR
	StopSpread = true
	CoopInit()
end

AfterWorldLoaded = function()
	if #CoopPlayers >= 2 then
		Ivan.Owner = CoopPlayers[2]
		InitialBrute1.Owner = CoopPlayers[2]
		InitialBrute2.Owner = CoopPlayers[2]
	end

	if #CoopPlayers >= 3 then
		InitialBrute1.Owner = CoopPlayers[3]
		InitialBrute2.Owner = CoopPlayers[3]
	end

	if #CoopPlayers >= 4 then
		InitialBrute2.Owner = CoopPlayers[4]
	end

	if #CoopPlayers >= 5 then
		local extraBrute = Actor.Create(InitialBrute1.Type, true, { Owner = CoopPlayers[5], Location = InitialBrute1.Location })
		extraBrute.Scatter()
	end

	if #CoopPlayers >= 6 then
		local extraBrute = Actor.Create(InitialBrute2.Type, true, { Owner = CoopPlayers[6], Location = InitialBrute2.Location })
		extraBrute.Scatter()
	end

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		StopSpread = false
	end)
end

AfterTick = function()
	if #MissionPlayers >= 2 and not StopSpread then
		Utils.Do(MissionPlayers[1].GetActorsByType("brut"), function(a)
			a.Owner = USSR
		end)
	end
end
