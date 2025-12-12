
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	GDI = Player.GetPlayer("GDI")
	Scrin = Player.GetPlayer("Scrin")
	TibLifeforms = Player.GetPlayer("TibLifeforms")
	GatewayOwner = Player.GetPlayer("GatewayOwner")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Scrin }
	SinglePlayerPlayer = GDI
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(3500)
end

AfterTick = function()

end

DoMcvArrival = function()
	local interval = 30
	local defenders = { "hmmv", "mtnk", "mtnk", "n1", "n1", "n1", "n1", "n3" }
	Reinforcements.Reinforce(GDI, defenders, { GatewayStable.Location, PlayerStart.Location }, interval)

	local delay = interval * #defenders
	Utils.Do(GetMcvPlayers(), function(p)
		Trigger.AfterDelay(delay, function()
			Reinforcements.Reinforce(p, { "amcv" }, { GatewayStable.Location, PlayerStart.Location })
		end)
		delay = delay + interval
	end)
end
