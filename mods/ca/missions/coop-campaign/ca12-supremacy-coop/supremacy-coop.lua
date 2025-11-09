
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Nod = Player.GetPlayer("Nod")
	Nod2 = Player.GetPlayer("Nod2")
	Nod3 = Player.GetPlayer("Nod3")
	GDI = Player.GetPlayer("GDI")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { GDI }
	SinglePlayerPlayer = Nod
	CoopInit()
end

AfterWorldLoaded = function()
	TransferMcvsToPlayers()
	if Nod.Cash > 2500 then
		StartCashSpread(2500)
	end
end

AfterTick = function()

end

TransferEastNod = function()
	local nod2Assets = Nod2.GetActors()
	Utils.Do(nod2Assets, function(a)
		if not a.IsDead and a.Type ~= "player" then
			a.Owner = Nod
		end
	end)
	Trigger.AfterDelay(1, function()
		local firstActivePlayer = GetFirstActivePlayer()
		TransferBaseToPlayer(SinglePlayerPlayer, firstActivePlayer)
	end)
end

TransferWestNod = function()
	local nod3Assets = Nod3.GetActors()
	Utils.Do(nod3Assets, function(a)
		if not a.IsDead and a.Type ~= "player" then
			a.Owner = Nod
		end
	end)
	Trigger.AfterDelay(1, function()
		local firstActivePlayer = GetFirstActivePlayer()
		TransferBaseToPlayer(SinglePlayerPlayer, firstActivePlayer)
	end)
end
