ScrinAttackValues = {
	easy = { Min = 6, Max = 16 },
	normal = { Min = 16, Max = 33 },
	hard = { Min = 25, Max = 55 },
}

NodBuildingsToSell = { NodConyard, NodHand, NodFactory, NodComms }

ScrinReinforcementSquad = { "s3", "s1", "s1", "s1", "s1", "s1", "s2", "s2", "s3", "intl", "rtpd", GunWalkerSeekerOrLacerator, CorrupterDevourerOrDarkener, CorrupterDevourerOrDarkener, GunWalkerSeekerOrLacerator, GunWalkerSeekerOrLacerator }

if Difficulty == "hard" then
	table.insert(UnitCompositions.Scrin, {
		Infantry = { "impl", "impl", "impl", "impl", "impl", "impl", "impl", "impl", "impl" },
		Vehicles = { "null", "null", "null", "null", "null", "null" },
		MinTime = DateTime.Minutes(5)
		IsSpecial = true
	})
end

AdjustedScrinCompositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin)

Squads = {
	ScrinRebels1 = {
		Delay = {
			easy = DateTime.Minutes(4),
			normal = DateTime.Minutes(3),
			hard = DateTime.Minutes(2),
		},
		AttackValuePerSecond = ScrinAttackValues,
		FollowLeader = true,
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = AdjustedScrinCompositions,
		AttackPaths = {
			{ RebelRally5.Location },
			{ RebelRally6.Location },
		},
	},
	ScrinRebels2 = {
		Delay = {
			easy = DateTime.Minutes(9),
			normal = DateTime.Minutes(6),
			hard = DateTime.Minutes(3),
		},
		AttackValuePerSecond = ScrinAttackValues,
		FollowLeader = true,
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = AdjustedScrinCompositions,
		AttackPaths = {
			{ RebelRally1.Location },
			{ RebelRally2.Location },
		},
	},
	ScrinRebels3 = {
		Delay = {
			easy = DateTime.Minutes(10),
			normal = DateTime.Minutes(7),
			hard = DateTime.Minutes(4),
		},
		AttackValuePerSecond = ScrinAttackValues,
		FollowLeader = true,
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = AdjustedScrinCompositions,
		AttackPaths = {
			{ RebelRally2.Location },
			{ RebelRally3.Location },
			{ RebelRally4.Location },
		},
	},
	ScrinRebelsAir = {
		Delay = {
			easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(5),
			hard = DateTime.Minutes(4)
		},
		AttackValuePerSecond = {
			easy = { Min = 7, Max = 7 },
			normal = { Min = 14, Max = 14 },
			hard = { Min = 21, Max = 21 },
		},
		ProducerTypes = { Aircraft = { "grav" } },
		Units = {
			easy = {
				{ Aircraft = { "stmr" } }
			},
			normal = {
				{ Aircraft = { "stmr", "stmr" } },
				{ Aircraft = { "enrv" } },
			},
			hard = {
				{ Aircraft = { "stmr", "stmr", "stmr" } },
				{ Aircraft = { "enrv", "enrv" } },
			}
		}
	},
}

-- Setup and Tick

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Nod = Player.GetPlayer("Nod")
	Scrin = Player.GetPlayer("Scrin")
	ScrinRebels1 = Player.GetPlayer("ScrinRebels1")
	ScrinRebels2 = Player.GetPlayer("ScrinRebels2")
	ScrinRebels3 = Player.GetPlayer("ScrinRebels3")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { USSR }
	TimerTicks = DateTime.Minutes(3)
	NumTransmittersCaptured = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(USSR)
	AdjustPlayerStartingCashForDifficulty()
	InitScrinRebels()
	InitNod()

	Actor.Create("hazmatsoviet.upgrade", true, { Owner = USSR })

	ObjectiveCaptureNerveCenter = USSR.AddObjective("Capture rebel Nerve Center.")
	ObjectiveEliminateRebels = USSR.AddObjective("Eliminate all rebel forces.")

	if Difficulty ~= "hard" then
		HardOnlyElite.Destroy()
		HardOnlyReaper.Destroy()
	end

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("Assist us to annihilate Kane and the rebels, and you will be rewarded.", "Scrin Overlord", HSLColor.FromHex("7700FF"))
		MediaCA.PlaySound("ovld_assist.aud", 2)
	end)

	local transmitters = { SignalTransmitter1, SignalTransmitter2, SignalTransmitter3 }
	Utils.Do(transmitters, function(t)
		Trigger.OnEnteredProximityTrigger(t.CenterPosition, WDist.New(12 * 1024), function(a, id)
			if a.Owner == USSR then
				Trigger.RemoveProximityTrigger(id)
				InitSignalTransmittersObjective()
			end
		end)
	end)

	Trigger.OnCapture(GatewayNerveCenter, function(self, captor, oldOwner, newOwner)
		if newOwner == USSR and not USSR.IsObjectiveCompleted(ObjectiveCaptureNerveCenter) then
			USSR.MarkCompletedObjective(ObjectiveCaptureNerveCenter)
			ObjectiveHoldNerveCenter = USSR.AddObjective("Protect the captured Nerve Center.")
			TimerTicks = 0

			Trigger.OnRemovedFromWorld(GatewayNerveCenter, function(a)
				if not USSR.IsObjectiveCompleted(ObjectiveHoldNerveCenter) then
					USSR.MarkFailedObjective(ObjectiveHoldNerveCenter)
					Gateway.Destroy()
				end
			end)
		end
	end)

	Trigger.OnKilled(GatewayNerveCenter, function(self, killer)
		if not USSR.IsObjectiveCompleted(ObjectiveCaptureNerveCenter) then
			USSR.MarkFailedObjective(ObjectiveCaptureNerveCenter)
			Gateway.Destroy()
		end
	end)

	Utils.Do(NodBuildingsToSell, function(b)
		Trigger.OnEnteredProximityTrigger(b.CenterPosition, WDist.New(5 * 1024), function(a, id)
			if a.Owner == USSR and a.Type == "e6" then
				Trigger.RemoveProximityTrigger(id)
				NodSellOff()
			end
		end)
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		ScrinRebels1.Resources = ScrinRebels1.ResourceCapacity - 500

		if USSR.HasNoRequiredUnits() then
			if not USSR.IsObjectiveCompleted(ObjectiveEliminateRebels) then
				USSR.MarkFailedObjective(ObjectiveEliminateRebels)
			end
		end

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0

				if not Gateway.IsDead then
					Gateway.Destroy()
					if not USSR.IsObjectiveCompleted(ObjectiveCaptureNerveCenter) then
						USSR.MarkFailedObjective(ObjectiveCaptureNerveCenter)
					end
				end
			end

			UpdateMissionText()
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()
	end
end

UpdateMissionText = function()
	if TimerTicks > 0 then
		UserInterface.SetMissionText("Gateway collapses in " .. UtilsCA.FormatTimeForGameSpeed(TimerTicks), HSLColor.Yellow)
	else
		UserInterface.SetMissionText("")
	end
end

-- Functions

InitScrinRebels = function()
	local scrinRebelPlayers = { ScrinRebels1, ScrinRebels2, ScrinRebels3 }

	Utils.Do(scrinRebelPlayers, function(p)
		AutoRepairAndRebuildBuildings(p)
		SetupRefAndSilosCaptureCredits(p)
		AutoReplaceHarvesters(p)
		InitAiUpgrades(p)

		local scrinRebelsGroundAttackers = p.GetGroundAttackers()

		Utils.Do(scrinRebelsGroundAttackers, function(a)
			TargetSwapChance(a, 10)
			CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
		end)
	end)

	Trigger.AfterDelay(Squads.ScrinRebels1.Delay[Difficulty], function()
		InitAttackSquad(Squads.ScrinRebels1, ScrinRebels1)
	end)
	Trigger.AfterDelay(Squads.ScrinRebels2.Delay[Difficulty], function()
		InitAttackSquad(Squads.ScrinRebels2, ScrinRebels2)
	end)
	Trigger.AfterDelay(Squads.ScrinRebels3.Delay[Difficulty], function()
		InitAttackSquad(Squads.ScrinRebels3, ScrinRebels3)
	end)
	Trigger.AfterDelay(Squads.ScrinRebelsAir.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.ScrinRebelsAir, ScrinRebels1, USSR, { "harv", "4tnk", "4tnk.atomic", "3tnk", "3tnk.atomic", "3tnk.rhino", "3tnk.rhino.atomic",
			"katy", "v3rl", "ttra", "v3rl", "apwr", "tpwr", "npwr", "tsla", "proc", "nukc", "ovld", "apoc", "apoc.atomic", "ovld.atomic" })
	end)
end

InitNod = function()
	AutoRepairBuildings(Nod)
	SetupRefAndSilosCaptureCredits(Nod)

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)

	Trigger.AfterDelay(DateTime.Minutes(2), function()
		Utils.Do(nodGroundAttackers, function(a)
			AssaultPlayerBaseOrHunt(a, USSR)
		end)
	end)
end

InitSignalTransmittersObjective = function()
	if ObjectiveSignalTransmitters == nil then
		ObjectiveSignalTransmitters = USSR.AddObjective("Capture the three signal transmitters.")
		Media.DisplayMessage("Capture the rebel Signal Transmiters, and I will unleash my forces to assist you.", "Scrin Overlord", HSLColor.FromHex("7700FF"))
		MediaCA.PlaySound("ovld_capture.aud", 2)

		local transmitters = Utils.Where({ SignalTransmitter1, SignalTransmitter2, SignalTransmitter3 }, function(a)
			return not a.IsDead and a.Owner == ScrinRebels1
		end)

		Utils.Do(transmitters, function(t)
			local transmitterFlare = Actor.Create("flare", true, { Owner = USSR, Location = t.Location })
			Beacon.New(USSR, t.CenterPosition)
			Trigger.AfterDelay(DateTime.Seconds(20), function()
				transmitterFlare.Destroy()
			end)

			Trigger.OnCapture(t, function(self, captor, oldOwner, newOwner)
				NumTransmittersCaptured = NumTransmittersCaptured + 1

				if NumTransmittersCaptured == #transmitters and not USSR.IsObjectiveCompleted(ObjectiveSignalTransmitters) then
					USSR.MarkCompletedObjective(ObjectiveSignalTransmitters)
				end

				local wormholeLoc
				if self == ScrinGatewayWp1 then
					wormholeLoc = ScrinWormholeWp1.Location
				elseif self == ScrinGatewayWp2 then
					wormholeLoc = ScrinWormholeWp2.Location
				elseif self == ScrinGatewayWp3 then
					wormholeLoc = ScrinWormholeWp3.Location
				end

				local wormhole = SpawnWormhole(wormholeLoc)
				InitScrinReinforcements(wormhole)
				FleetRecall(self)
			end)

			Trigger.OnKilled(t, function(self, killer)
				if self.Owner == ScrinRebels1 then
					USSR.MarkFailedObjective(ObjectiveSignalTransmitters)
				end
			end)
		end)
	end
end

NodSellOff = function()
	if NodSold then
		return
	end
	NodSold = true
	Utils.Do(NodBuildingsToSell, function(b)
		if not b.IsDead then
			b.Sell()
		end
	end)
end

SpawnWormhole = function(loc)
	return Actor.Create("wormhole", true, { Owner = Scrin, Location = loc })
end

InitScrinReinforcements = function(wormhole)
	DeployScrinReinforcements(wormhole)
end

DeployScrinReinforcements = function(wormhole)
	if not wormhole.IsDead then
		local unitsList = {}
		Utils.Do(ScrinReinforcementSquad, function(u)
			if type(u) == "table" then
				table.insert(unitsList, Utils.Random(u))
			else
				table.insert(unitsList, u)
			end
		end)

		local units = Reinforcements.Reinforce(Scrin, unitsList, { wormhole.Location }, 5)
		Utils.Do(units, function(unit)
			unit.Scatter()
			Trigger.AfterDelay(5, function()
				AssaultPlayerBaseOrHunt(unit, ScrinRebels1)
			end)
		end)

		Trigger.AfterDelay(DateTime.Minutes(2), function()
			DeployScrinReinforcements(wormhole)
		end)
	end
end

FleetRecall = function(transmitter)
	local effect = Actor.Create("recall.effect", true, { Owner = Scrin, Location = transmitter.Location })
	Trigger.AfterDelay(DateTime.Seconds(5), effect.Destroy)

	local spawnLocations = {
		CPos.New(transmitter.Location.X + 2, transmitter.Location.Y + 1),
		CPos.New(transmitter.Location.X - 2, transmitter.Location.Y - 1),
		CPos.New(transmitter.Location.X - 1, transmitter.Location.Y + 2),
		CPos.New(transmitter.Location.X + 1, transmitter.Location.Y - 2)
	}

	Utils.Do(spawnLocations, function(loc)
		Actor.Create("pac", true, { Owner = Scrin, Location = loc, Facing = Angle.SouthWest, CenterPosition = Map.CenterOfCell(loc) + WVec.New(0, 0, Actor.CruiseAltitude("pac"))})
	end)
end
