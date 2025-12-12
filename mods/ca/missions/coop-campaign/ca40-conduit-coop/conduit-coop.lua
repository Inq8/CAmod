
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	USSR = Player.GetPlayer("USSR")
	Nod1 = Player.GetPlayer("Nod1")
	Nod2 = Player.GetPlayer("Nod2")
	Nod3 = Player.GetPlayer("Nod3")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Nod1, Nod2, Nod3 }
	SinglePlayerPlayer = USSR
	CoopInit()
end

AfterWorldLoaded = function()
	local firstActivePlayer = GetFirstActivePlayer()
	TransferBaseToPlayer(SinglePlayerPlayer, firstActivePlayer)
	StartCashSpread(3500)

	local delay = 25
	local path = { CPos.New(18,1), CPos.New(18,8) }
	Utils.Do(GetMcvPlayers(), function(p)
		if p ~= firstActivePlayer then
			Trigger.AfterDelay(delay, function()
				Reinforcements.Reinforce(p, { "mcv" }, path)
			end)
			delay = delay + DateTime.Seconds(1)
		end
	end)
end

AfterTick = function()

end
