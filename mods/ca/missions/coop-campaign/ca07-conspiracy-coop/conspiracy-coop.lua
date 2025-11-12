
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Nod = Player.GetPlayer("Nod")
	Greece = Player.GetPlayer("Greece")
	GDI = Player.GetPlayer("GDI")
	Legion = Player.GetPlayer("Legion")
	EvacPlayer = Player.GetPlayer("Evac")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Greece, GDI }
	SinglePlayerPlayer = Nod
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(3000)
end

AfterTick = function()

end

TransferLegionForces = function()
	local legionForces = Legion.GetActors()
	local firstActivePlayer = GetFirstActivePlayer()

	Utils.Do(legionForces, function(self)
		if self.Type ~= "player" then
			self.Owner = firstActivePlayer
		end
	end)

	local factoryExitCell = CPos.New(19, 47)

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Utils.Do(GetMcvPlayers(), function(p)
			if p ~= firstActivePlayer then
				local mcv = Actor.Create("amcv", true, { Owner = p, Location = factoryExitCell })
				mcv.Scatter()
			end
		end)
	end)
end
