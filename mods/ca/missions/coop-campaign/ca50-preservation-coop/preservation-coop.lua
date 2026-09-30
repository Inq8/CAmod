GatewayChargeTicks = 60000

SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	ScrinRebels = Player.GetPlayer("ScrinRebels")
	USSR = Player.GetPlayer("USSR")
	Scrin = Player.GetPlayer("Scrin")
	Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = GetActiveCoopPlayers({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 })
	MissionEnemies = { USSR, Scrin }
	SinglePlayerPlayer = ScrinRebels
	ScrinRebelPlayers = GetActiveCoopPlayers({ Multi0, Multi2, Multi3, Multi5 })
	NodPlayers = GetActiveCoopPlayers({ Multi1, Multi4 })
	StopSpread = true
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(3500)

	Utils.Do(ScrinRebelPlayers, function(p)
		Actor.Create("rebel.allegiance", true, { Owner = p })
	end)

	local scrinRebelUnits = GetSpreadableUnits(SinglePlayerPlayer)
	AssignToCoopPlayers(scrinRebelUnits, ScrinRebelPlayers)

	local nodUnits = GetSpreadableUnits(Nod)
	if #NodPlayers > 0 then
		AssignToCoopPlayers(nodUnits, NodPlayers)
	end

	if (Multi1 ~= nil and Multi1.IsLocalPlayer) or (Multi4 ~= nil and Multi4.IsLocalPlayer) then
		Camera.Position = EastGateway.CenterPosition
	end

	if BaseSharingEnabled then
		TransferBaseToPlayer(SinglePlayerPlayer, ScrinRebelPlayers[1])

		if #NodPlayers > 0 then
			TransferBaseToPlayer(Nod, NodPlayers[1])
		end
	else
		Trigger.AfterDelay(1, function()
			local centralBaseActors = Utils.Where(SinglePlayerPlayer.GetActors(), function(a)
				return IsBaseTransferActor(a) and a.Location.X > 80
			end)
			Utils.Do(centralBaseActors, function(a)
				a.Owner = ScrinRebelPlayers[1]
			end)

			local westBaseActors = Utils.Where(SinglePlayerPlayer.GetActors(), function(a)
				return IsBaseTransferActor(a) and a.Location.X < 80
			end)

			if #ScrinRebelPlayers > 1 then
				Utils.Do(westBaseActors, function(a)
					a.Owner = ScrinRebelPlayers[2]
				end)

				if #ScrinRebelPlayers > 2 then
					Actor.Create("cspk", true, { Owner = ScrinRebelPlayers[3], Location = CPos.New(126, 88) })

					if #ScrinRebelPlayers > 3 then
						if #NodPlayers > 1 then
							Actor.Create("cspk", true, { Owner = ScrinRebelPlayers[4], Location = CPos.New(37, 79) })
						else
							EastColonySpike.Owner = ScrinRebelPlayers[4]
						end
					end
				end
			else
				Utils.Do(westBaseActors, function(a)
					a.Owner = ScrinRebelPlayers[1]
				end)
			end

			if #NodPlayers > 0 then
				TransferBaseToPlayer(Nod, NodPlayers[1])

				if #NodPlayers > 1 then
					Actor.Create("amcv", true, { Owner = NodPlayers[2], Location = CPos.New(211, 75), Facing = Angle.South })
					EastColonySpike.Destroy()
				end
			end

			CACoopQueueSyncer()
		end)
	end

	Trigger.AfterDelay(2, function()
		StopSpread = false
	end)
end

AfterTick = function()
	GatewayChargeTicks = GatewayChargeTicks - 1
end

SetChargeStatusText = function(chargePerc)
	UserInterface.SetMissionText("Gateway charge progress: " .. chargePerc .. "% - Time remaining: " .. UtilsCA.FormatTimeForGameSpeed(GatewayChargeTicks), HSLColor.Yellow)
end

ShowGatewayChargeTip = function()
	-- do nothing, charging is passive
end