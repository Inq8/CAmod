RespawnEnabled = Map.LobbyOption("respawn") == "enabled"

PowerGrids = {
	{
		Providers = { NPower1, NPower2, NPower3, NPower4 },
		Consumers = { NPowered1, NPowered2, NPowered3, NPowered4, NPowered5, NPowered6 },
	},
	{
		Providers = { EPower1, EPower2, EPower3 },
		Consumers = { EPowered1, EPowered2, EPowered3, EPowered4, EPowered5 },
	},
	{
		Providers = { SPower1, SPower2, SPower3 },
		Consumers = { SPowered1, SPowered2, SPowered3, SPowered4, SPowered5, SPowered6, SPowered7 },
	},
	{
		Providers = { CPower1, CPower2, CPower3, CPower4 },
		Consumers = { CPowered1, CPowered2, CPowered3, CPowered4, CPowered5, CPowered6, CPowered7, CPowered8, CPowered9, CPowered10, CPowered11, CPowered12, CPowered13, CPowered14, CPowered15, CPowered16 },
	},
}

TibTrucks = {
	First = {
		Actor = ETibTruck,
		Delay = {
			easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(5),
			hard = DateTime.Minutes(4),
			vhard = DateTime.Minutes(4),
			brutal = DateTime.Minutes(4),
		},
		Path = { ETibTruckPath1.Location, ETibTruckPath2.Location, ETibTruckPath3.Location, ETibTruckPath4.Location },
		ObjectiveText = "Prevent first enriched Tiberium delivery\nfrom reaching Yuri.",
		Objective = nil,
	},
	Second = {
		Actor = NTibTruck,
		Delay = {
			easy = DateTime.Minutes(15),
			normal = DateTime.Minutes(12),
			hard = DateTime.Minutes(10),
			vhard = DateTime.Minutes(10),
			brutal = DateTime.Minutes(10),
		},
		Path = { NTibTruckPath1.Location, NTibTruckPath2.Location, NTibTruckPath3.Location, NTibTruckPath4.Location, NTibTruckPath5.Location, NTibTruckPath6.Location, NTibTruckPath7.Location, NTibTruckPath8.Location, NTibTruckPath9.Location, NTibTruckPath10.Location },
		ObjectiveText = "Prevent second enriched Tiberium delivery\nfrom reaching Yuri.",
		Objective = nil,
	},
	Third = {
		Actor = STibTruck,
		Delay = {
			easy = DateTime.Minutes(25),
			normal = DateTime.Minutes(20),
			hard = DateTime.Minutes(16),
			vhard = DateTime.Minutes(16),
			brutal = DateTime.Minutes(16),
		},
		Path = { STibTruckPath1.Location, STibTruckPath2.Location, STibTruckPath3.Location, STibTruckPath4.Location, STibTruckPath5.Location, STibTruckPath6.Location },
		ObjectiveText = "Prevent third enriched Tiberium delivery\nfrom reaching Yuri.",
		Objective = nil,
	},
}

TibFacilities = { NTibFacility, STibFacility, ETibFacility }

HindPatrolPath = { HindPatrol1.Location, HindPatrol2.Location, HindPatrol3.Location, HindPatrol4.Location, HindPatrol5.Location, HindPatrol6.Location, HindPatrol7.Location, HindPatrol8.Location, HindPatrol9.Location }

BombsEnabledTime = {
	vhard = DateTime.Minutes(20),
	brutal = DateTime.Minutes(10),
}

Squads = {
	Main = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(6)),
		AttackValuePerSecond = {
			vhard = { Min = 5, Max = 15 },
			brutal = { Min = 10, Max = 20 },
		},
		FollowLeader = true,
		ProducerActors = { Infantry = { NorthEastBarracks, NorthWestBarracks, SouthEastBarracks, SouthWestBarracks, CentralBarracks } },
		Compositions = AdjustCompositionsForDifficulty({
			{ Infantry = { "e3", "e1", "e1", "e1", "e1", "e2", "brut" } },
			{ Infantry = { "e3", "e2", "tplr", "e1", "e1", "e1", "e1" } },
			{ Infantry = { "e3", "e2", "enli", "e1", "e1", "e1", "e1" }, MinTime = DateTime.Minutes(16) },
			{ Infantry = { "e3", "e2", "rmbc", "e1", "e1", "e1", "e1" }, MinTime = DateTime.Minutes(20) },
		}),
	},
}

WorldLoaded = function()
	Scrin = Player.GetPlayer("Scrin")
	USSR = Player.GetPlayer("USSR")
	MissionPlayers = { Scrin }
	MissionEnemies = { USSR }
	TimerTicks = 0
	TibFacilitiesCaptured = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Scrin)
	InitUSSR()

	ObjectiveCaptureTibFacilities = Scrin.AddObjective("Capture three Tiberium enrichment facilities.")
	if not RespawnEnabled then
		ObjectiveMastermindSurvives = Scrin.AddObjective("Mastermind must survive.")
	end

	Mastermind.GrantCondition("difficulty-" .. Difficulty)

	if IsHardOrAbove() then
		CapturedCreditsAmount = 1000
	elseif Difficulty == "normal" then
		CapturedCreditsAmount = 1250
	elseif Difficulty == "easy" then
		CapturedCreditsAmount = 1500
	end

	Actor.Create("blink.upgrade", true, { Owner = Scrin })
	Actor.Create("radar.dummy", true, { Owner = Scrin })

	Trigger.AfterDelay(DateTime.Seconds(7), function()
		Media.DisplayMessage("Your powers are no match for me. Flee through your wormholes, while you can.", "Yuri", HSLColor.FromHex("FF00BB"))
		MediaCA.PlaySound("yuri_taunt.aud", 2)
		Trigger.AfterDelay(DateTime.Seconds(7), function()
			Tip("The Mastermind can mind control up to three enemy units. Mind controlling a fourth will kill the earliest controlled.")
			Trigger.AfterDelay(DateTime.Seconds(7), function()
				Tip("The Mastermind has a targeted ability that creates mind sparks from himself and his slaves which damage and slow nearby enemies (the slaves will be unharmed).")
				Trigger.AfterDelay(DateTime.Seconds(7), function()
					Tip("The Mastermind can take control of enemy buildings. Production structures will be able to produce permanently enslaved units.")
					Trigger.AfterDelay(DateTime.Seconds(7), function()
						Tip("Stay out of Yuri's area of influence until your Mastermind becomes powerful enough to protect your units.")
					end)
				end)
			end)
		end)
	end)

	MastermindDeathTrigger(Mastermind)

	Utils.Do(PowerGrids, function(grid)
		Trigger.OnAllKilledOrCaptured(grid.Providers, function()
			Utils.Do(grid.Consumers, function(consumer)
				if not consumer.IsDead then
					consumer.GrantCondition("disabled")
				end
			end)
		end)
	end)

	Utils.Do(TibFacilities, function(a)
		Trigger.OnKilled(a, function(self, killer)
			if not IsMissionPlayer(self.Owner) then
				Scrin.MarkFailedObjective(ObjectiveCaptureTibFacilities)
			end
		end)

		Trigger.OnCapture(a, function(self, captor, oldOwner, newOwner)
			if newOwner == Scrin then
				TibFacilitiesCaptured = TibFacilitiesCaptured + 1
				Mastermind.GrantCondition("rank-veteran")
				Mastermind.Health = Mastermind.MaxHealth
			end

			if TibFacilitiesCaptured == 3 then
				if ObjectiveCaptureYuriHQ == nil then
					ObjectiveCaptureYuriHQ = Scrin.AddObjective("Capture Yuri's command center.")
				end
				Scrin.MarkCompletedObjective(ObjectiveCaptureTibFacilities)
				Trigger.AfterDelay(DateTime.Seconds(2), function()
					Notification("Enriched ichor consumed. Your Mastermind has become a Prodigy and is able to protect nearby units from Yuri's influence.")
					MediaCA.PlaySound("s_prodigy.aud", 2)
				end)
			else
				Trigger.AfterDelay(DateTime.Seconds(2), function()
					Notification("Enriched ichor consumed. Mastermind mind control capacity increased by 1.")
					MediaCA.PlaySound("s_ichorconsumed.aud", 2)
				end)
			end
		end)
	end)

	Trigger.OnKilled(YuriHQ, function(self, killer)
		if ObjectiveCaptureYuriHQ == nil then
			ObjectiveCaptureYuriHQ = Scrin.AddObjective("Capture Yuri's command center.")
		end

		if not IsMissionPlayer(self.Owner) then
			Scrin.MarkFailedObjective(ObjectiveCaptureYuriHQ)
		end
	end)

	Trigger.OnCapture(YuriHQ, function(self, captor, oldOwner, newOwner)
		if ObjectiveCaptureYuriHQ == nil then
			ObjectiveCaptureYuriHQ = Scrin.AddObjective("Capture Yuri's command center.")
		end

		Scrin.MarkCompletedObjective(ObjectiveCaptureYuriHQ)
		if not RespawnEnabled then
			Scrin.MarkCompletedObjective(ObjectiveMastermindSurvives)
		end
	end)

	Utils.Do(TibTrucks, function(t)
		Trigger.AfterDelay(t.Delay[Difficulty], function()
			if not t.Actor.IsDead and t.Actor.Owner == USSR and t.Objective == nil then

				if not FirstShipmentAnnounced then
					FirstShipmentAnnounced = true
					Notification("Enriched ichor shipment detected. Dispatch is imminent. Prevent it from reaching Yuri's command center.")
					MediaCA.PlaySound("s_firstichorshipment.aud", 2)
				else
					Notification("Enriched ichor shipment detected.")
					MediaCA.PlaySound("s_ichorshipment.aud", 2)
				end

				t.Objective = Scrin.AddSecondaryObjective(t.ObjectiveText)
				local camera = Actor.Create("smallcamera", true, { Owner = Scrin, Location = t.Actor.Location })
				Trigger.AfterDelay(DateTime.Seconds(10), function()
					camera.Destroy()
				end)
				Beacon.New(Scrin, t.Actor.CenterPosition)
				Trigger.AfterDelay(DateTime.Seconds(30), function()
					if not t.Actor.IsDead and t.Actor.Owner == USSR and not Scrin.IsObjectiveFailed(t.Objective) then
						Utils.Do(t.Path, function(waypoint)
							t.Actor.Move(waypoint)
						end)
						Trigger.OnEnteredFootprint({ t.Path[#t.Path] }, function(a, id)
							if not YuriHQ.IsDead and YuriHQ.Owner == USSR and a == t.Actor and a.Owner == USSR then
								Trigger.RemoveFootprintTrigger(id)
								YuriHQ.GrantCondition("enriched")
								a.Destroy()
								Notification("A shipment of enriched ichor has reached Yuri's command center and he has grown more powerful.")
								MediaCA.PlaySound("s_yuripower.aud", 2)
								if t.Objective ~= nil and not Scrin.IsObjectiveCompleted(t.Objective) then
									Scrin.MarkFailedObjective(t.Objective)
								end
							end
						end)
					end
				end)
			end
		end)
		Trigger.OnKilled(t.Actor, function(self, killer)
			if t.Objective == nil then
				t.Objective = Scrin.AddSecondaryObjective(t.ObjectiveText)
			end
			Trigger.AfterDelay(2, function()
				if not Scrin.IsObjectiveFailed(t.Objective) then
					Scrin.MarkCompletedObjective(t.Objective)
				end
			end)
		end)
	end)

	Trigger.OnEnteredProximityTrigger(YuriHQ.CenterPosition, WDist.New(18 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) and a.Type ~= "smallcamera" then
			Trigger.RemoveProximityTrigger(id)
			if not YuriDefenderTipShown then
				YuriDefenderTipShown = true
				Tip("Yuri's command center is heavily guarded. Brute force is unlikely to be the best approach.")
			end
		end
	end)

	SetupReveals({ EntranceReveal1, EntranceReveal2, EntranceReveal3, EntranceReveal4, EntranceReveal5, EntranceReveal6, EntranceReveal7, EntranceReveal8, EntranceReveal9, EntranceReveal10, EntranceReveal11 }, "smallcamera")
	AfterWorldLoaded()
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		USSR.Resources = USSR.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()

		Utils.Do(TibTrucks, function(t)
			if not t.Actor.IsDead and IsMissionPlayer(t.Actor.Owner) then
				if t.Objective ~= nil and not Scrin.IsObjectiveFailed(t.Objective) then
					Scrin.MarkCompletedObjective(t.Objective)
				end
			end
		end)
	end
end

InitUSSR = function()
	AutoRepairBuildings(USSR)
	SetupRefAndSilosCaptureCredits(USSR)
	InitAiUpgrades(USSR, 0)

	if IsVeryHardOrAbove() then
		InitAttackSquad(Squads.Main, USSR)
		Trigger.AfterDelay(BombsEnabledTime[Difficulty], function()
			if not CentralDome.IsDead then
				CentralDome.GrantCondition("bombs-enabled")
			end
			if not CentralHelipad.IsDead then
				CentralHelipad.GrantCondition("bombs-enabled")
			end
		end)
	end

	Actor.Create("ai.unlimited.power", true, { Owner = USSR })
	Actor.Create("cyborgspeed.upgrade", true, { Owner = USSR })
	Actor.Create("cyborgarmor.upgrade", true, { Owner = USSR })

	local ussrGroundAttackers = USSR.GetGroundAttackers()

	Utils.Do(ussrGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsUSSRGroundHunterUnit)
	end)

	local hinds = USSR.GetActorsByType("hind")
	Utils.Do(hinds, function(a)
		a.Patrol(HindPatrolPath, true)
		Trigger.OnDamaged(a, function(self, attacker, damage)
			if not self.IsDead and self.AmmoCount() == 0 then
				Trigger.ClearAll(self)
				self.Stop()
				self.ReturnToBase()
				Trigger.AfterDelay(DateTime.Seconds(1), function()
					if not self.IsDead then
						self.Patrol(HindPatrolPath, true)
					end
				end)
			end
		end)
	end)
end

RespawnMastermind = function()
	local mastermindName = "Mastermind"

	if TibFacilitiesCaptured == 3 then
		mastermindName = "Prodigy"
	end

	Notification("The " .. mastermindName .. " used its considerable psionic powers to cheat death. It will return in 20 seconds.")

	Trigger.AfterDelay(DateTime.Seconds(20), function()
		local wormhole = Actor.Create("wormhole", true, { Owner = Scrin, Location = PlayerStart.Location })
		Beacon.New(Scrin, PlayerStart.CenterPosition, DateTime.Seconds(20))

		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Media.PlaySpeechNotification(Scrin, "ReinforcementsArrived")
			Mastermind = Reinforcements.Reinforce(Scrin, { "mast" }, { PlayerStart.Location }, 1)[1]
			Mastermind.Scatter()

			if IsNormalOrBelow() then
				Mastermind.GrantCondition("difficulty-" .. Difficulty)
			end

			for i=1, TibFacilitiesCaptured do
				Mastermind.GrantCondition("rank-veteran")
			end

			Trigger.AfterDelay(DateTime.Seconds(5), function()
				wormhole.Kill()
			end)

			MastermindDeathTrigger(Mastermind)
		end)
	end)
end

MastermindDeathTrigger = function(mastermind)
	Trigger.OnKilled(mastermind, function(self, killer)
		if RespawnEnabled then
			RespawnMastermind()
		else
			Scrin.MarkFailedObjective(ObjectiveMastermindSurvives)
		end
	end)
end
