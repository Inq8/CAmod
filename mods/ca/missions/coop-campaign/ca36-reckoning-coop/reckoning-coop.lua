
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Nod = Player.GetPlayer("Nod")
	Scrin = Player.GetPlayer("Scrin")
	ScrinRebels = Player.GetPlayer("ScrinRebels")
	GDIHostile = Player.GetPlayer("GDIHostile")
	GDI = Player.GetPlayer("GDI")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Scrin }
	SinglePlayerPlayer = Nod
	StopSpread = true
	CoopInit()
	DistributeUnitsAndBases()
end

AfterWorldLoaded = function()

end

AfterTick = function()

end

DistributeUnitsAndBases = function()
	-- transfer Nod starting units to first Nod player
	if Multi0 ~= nil or Multi3 ~= nil then
		local firstNodPlayer = Multi0 ~= nil and Multi0 or Multi3
		local nodMcv = Nod.GetActorsByType("amcv")[1]
		nodMcv.Owner = firstNodPlayer

		local nodUnits = Utils.Where(Nod.GetActors(), function(a)
			return a.HasProperty("Move") and not IsHarvester(a) and not IsMcv(a)
		end)

		AssignToCoopPlayers(nodUnits, Utils.Where({ Multi0, Multi3 }, function(p) return p ~= nil end))

		-- if there are 2 Nod players, send MCV for second Nod player
		if Multi0 ~= nil and Multi3 ~= nil then
			Reinforcements.Reinforce(Multi3, { "amcv" }, { CPos.New(23, 1), CPos.New(23, 5) })
		end
	end

	-- transfer GDI base to first GDI player
	if Multi1 ~= nil or Multi4 ~= nil then
		GDIActive = true

		local firstGdiPlayer = Multi1 ~= nil and Multi1 or Multi4
		TransferBaseToPlayer(GDI, firstGdiPlayer)

		local gdiUnits = Utils.Where(GDI.GetActors(), function(a)
			return a.HasProperty("Move") and not IsHarvester(a) and not IsMcv(a)
		end)

		AssignToCoopPlayers(gdiUnits, Utils.Where({ Multi1, Multi4 }, function(p) return p ~= nil end))

		-- if there are 2 GDI players,  send MCV for second GDI player
		if Multi1 ~= nil and Multi4 ~= nil then
			Reinforcements.Reinforce(Multi4, { "mcv" }, { CPos.New(113, 1), CPos.New(118, 12) })
		end
	end

	-- transfer top Scrin base to first Scrin player (y = 0 -> 50)
	if Multi2 ~= nil or Multi5 ~= nil then
		ScrinRebelsActive = true

		local firstScrinPlayer = Multi2 ~= nil and Multi2 or Multi5

		local scrinTopActors = Utils.Where(ScrinRebels.GetActors(), function(a)
			return a.HasProperty("Health") and a.Location.Y <= 50
		end)

		Utils.Do(scrinTopActors, function(a)
			a.Owner = firstScrinPlayer
		end)

		local scrinMiddleActors = Utils.Where(ScrinRebels.GetActors(), function(a)
			return a.HasProperty("Health") and a.Location.Y > 50 and a.Location.Y <= 80
		end)

		-- if there are 2 Scrin players, give middle Scrin base to second Scrin player (y = 51 -> 80)
		if Multi2 ~= nil and Multi5 ~= nil then
			Utils.Do(scrinMiddleActors, function(a)
				a.Owner = Multi5
			end)
		-- otherwise give it to first Scrin player
		else
			Utils.Do(scrinMiddleActors, function(a)
				a.Owner = firstScrinPlayer
			end)
		end
	end

	if GDIActive then

	end

	CACoopQueueSyncer()
end