
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Nod }
	SinglePlayerPlayer = GDI
	CoopInit()
end

AfterWorldLoaded = function()
	if #MissionPlayers > 1 then
		local player2Buildings = { Actor109, Actor96, Actor100, Actor294 }
		Utils.Do(player2Buildings, function(b)
			b.Owner = MissionPlayers[2]
		end)
	end
end

AfterTick = function()

end

DoReinforcements = function()
	Reinforcements.Reinforce(GDI, { "hmmv", "mtnk", "mtnk" }, { McvSpawn.Location, McvRally.Location }, 75)
	GDI.Cash = 6000 + CashAdjustments[Difficulty]
	StartCashSpread(3500)

	local delay = 0
	Utils.Do(GetMcvPlayers(), function(p)
		Trigger.AfterDelay(delay, function()
			Reinforcements.Reinforce(p, { "amcv" }, { McvSpawn.Location, McvRally.Location })
		end)
		delay = delay + DateTime.Seconds(1)
	end)
end
