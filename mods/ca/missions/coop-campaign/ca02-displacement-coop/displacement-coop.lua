
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Greece = Player.GetPlayer("Greece")
	England = Player.GetPlayer("England")
	Scrin = Player.GetPlayer("Scrin")
	USSR = Player.GetPlayer("USSR")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Scrin }
	SinglePlayerPlayer = Greece
	CoopInit()
end

AfterWorldLoaded = function()
	TransferBaseToPlayer(SinglePlayerPlayer, GetFirstActivePlayer())
	StartCashSpread(0)

	Trigger.OnAnyProduction(function(producer, produced, productionType)
		local firstActivePlayer = GetFirstActivePlayer()
		if IsHarvester(produced) and IsMissionPlayer(produced.Owner) and produced.Owner ~= firstActivePlayer then
			produced.Owner = firstActivePlayer
		end
	end)

	Utils.Do({ SovietRefinery, SovietSilo1, SovietSilo2, SovietPower1, SovietPower2, SovietPower3 }, function(a)
		Trigger.OnCapture(a, function(self, captor, oldOwner, newOwner)
			local firstActivePlayer = GetFirstActivePlayer()
			if newOwner ~= firstActivePlayer then
				self.Owner = firstActivePlayer
			end
		end)
	end)
end

AfterTick = function()

end
