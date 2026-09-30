SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	ScrinRebels = Player.GetPlayer("ScrinRebels")
	Scrin = Player.GetPlayer("Scrin")
	Nod1 = Player.GetPlayer("Nod1")
	Nod2 = Player.GetPlayer("Nod2")
	Nod3 = Player.GetPlayer("Nod3")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = GetActiveCoopPlayers({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 })
	MissionEnemies = { Scrin }
	SinglePlayerPlayer = ScrinRebels
	ScrinRebelPlayers = GetActiveCoopPlayers({ Multi0, Multi4 })
	NodPlayers = GetActiveCoopPlayers({ Multi1, Multi5 })
	StopSpread = true
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(3500)
	TransferMcvsToPlayers(ScrinRebelPlayers)
	AssignToCoopPlayers(GetSpreadableUnits(SinglePlayerPlayer), ScrinRebelPlayers)

	if #NodPlayers > 0 then
		NodTemplePrime.Destroy()
		NodTemple.Destroy()
		NodComms.Destroy()
		Nod2Comms.Destroy()
		local nodUnitsToDestroy = Nod1.GetActorsByTypes({ "rmbc", "reap" })
		Utils.Do(nodUnitsToDestroy, function(u)
			u.Destroy()
		end)
	end

	-- west nod
	if Multi1 ~= nil then
		TransferBaseToPlayer(Nod1, Multi1)
		AssignToCoopPlayers(GetSpreadableUnits(Nod1), { Multi1 })

		if Multi1.IsLocalPlayer then
			Camera.Position = ScrinWaypoint8.CenterPosition
		end

		if Multi5 == nil then
			TransferBaseToPlayer(Nod2, Multi1)
			AssignToCoopPlayers(GetSpreadableUnits(Nod2), { Multi1 })
		end
	end

	--gdi
	if Multi2 ~= nil then
		if Multi2.IsLocalPlayer then
			Media.PlaySpeechNotification(Multi2, "ReinforcementsArrived")
			Notification("Reinforcements have arrived.")
			Camera.Position = ExtraMcvDest.CenterPosition
		end
		Reinforcements.Reinforce(Multi2, { "amcv" }, { ExtraMcvSpawn.Location, ExtraMcvDest.Location })
	end

	-- allies
	if Multi3 ~= nil then
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			if Multi3.IsLocalPlayer then
				Media.PlaySpeechNotification(Multi3, "ReinforcementsArrived")
				Notification("Reinforcements have arrived.")
				Camera.Position = ExtraMcvDest.CenterPosition
			end
			Reinforcements.Reinforce(Multi3, { "mcv" }, { ExtraMcvSpawn.Location, ExtraMcvDest.Location })
		end)
	end

	-- east nod
	if Multi5 ~= nil then
		TransferBaseToPlayer(Nod2, Multi5)
		AssignToCoopPlayers(GetSpreadableUnits(Nod2), { Multi5 })
		if Multi5.IsLocalPlayer then
			Camera.Position = NodEastBase.CenterPosition
		end
	end

	Utils.Do(ScrinRebelPlayers, function(p)
		Actor.Create("rebel.allegiance", true, { Owner = p })
	end)

	Trigger.AfterDelay(1, function()
		StopSpread = false
	end)
end

AfterTick = function()

end
