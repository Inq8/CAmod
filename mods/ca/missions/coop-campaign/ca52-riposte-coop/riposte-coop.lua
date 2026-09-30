Squads.GDIVsNod1.AttackValuesPerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 })
Squads.GDIVsNod2.AttackValuesPerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 })

SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	ScrinRebels = Player.GetPlayer("ScrinRebels")
	HawthorneGDI = Player.GetPlayer("HawthorneGDI")
	Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = GetActiveCoopPlayers({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 })
	MissionEnemies = { HawthorneGDI }
	SinglePlayerPlayer = ScrinRebels
	ScrinRebelPlayers = GetActiveCoopPlayers({ Multi0, Multi2, Multi3, Multi5, Multi4 })
	NodPlayers = GetActiveCoopPlayers({ Multi1 })
	StopSpread = true
	CoopInit()
end

AfterWorldLoaded = function()
	local nodExcessUnits = Nod.GetActorsByTypes({ "avtr", "reap", "bh", "enli", "rmbc", "stnk.nod" })
	Utils.Do(nodExcessUnits, function(u)
		u.Destroy()
	end)

	StartCashSpread(3500)
	TransferMcvsToPlayers(ScrinRebelPlayers)
	AssignToCoopPlayers(GetSpreadableUnits(SinglePlayerPlayer), ScrinRebelPlayers)

	if #NodPlayers > 0 then
		local nodUnits = GetSpreadableUnits(Nod)
		AssignToCoopPlayers(nodUnits, NodPlayers)
		TransferBaseToPlayer(Nod, NodPlayers[1])
	end

	Utils.Do(ScrinRebelPlayers, function(p)
		Actor.Create("rebel.allegiance", true, { Owner = p })
	end)

	if Multi1 ~= nil and Multi1.IsLocalPlayer then
		Camera.Position = TemplePrime.CenterPosition
	end

	Trigger.AfterDelay(1, function()
		StopSpread = false
	end)
end

AfterTick = function()

end
