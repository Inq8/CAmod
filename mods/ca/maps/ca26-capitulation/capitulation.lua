AttackPaths = {
	{ WestDelivery3.Location, AttackRally1.Location },
	{ WestDelivery3.Location, AttackRally2.Location },
	{ SouthDelivery3.Location, AttackRally2.Location },
	{ SouthDelivery3.Location, AttackRally3.Location },
}

Deliveries = {
	{
		Spawn = SouthDeliverySpawn.Location,
		Path = { SouthDelivery1.Location, SouthDelivery2.Location, SouthDelivery3.Location, SouthDelivery4.Location, ReactorDeliveryPoint2.Location },
	},
	{
		Spawn = WestDeliverySpawn.Location,
		Path = { WestDelivery1.Location, WestDelivery2.Location, WestDelivery3.Location, WestDelivery4.Location, ReactorDeliveryPoint2.Location },
	},
	{
		Spawn = EastDeliverySpawn.Location,
		Path = { EastDelivery1.Location, EastDelivery2.Location, EastDelivery3.Location, ReactorDeliveryPoint2.Location },
	},
}

OuterSAMs = { OuterSAM2, OuterSAM4, OuterSAM6, OuterSAM8, OuterSAM10, OuterSAM12, OuterSAM14, OuterSAM16, OuterSAM18, OuterSAM20, OuterSAM22, OuterSAM24, OuterSAM26, OuterSAM28, OuterSAM30, OuterSAM31, OuterSAM32, OuterSAM33, OuterSAM34, OuterSAM35, OuterSAM36, OuterSAM37  }

TeslaReactors = { TPower1, TPower2, TPower3, TPower4, TPower5, TPower6, TPower7, TPower8, TPower9 }

InnerTeslas = { InnerTesla1, InnerTesla2, InnerTesla3, InnerTesla4 }

ParabombsEnabledDelay = {
	easy = DateTime.Minutes(9),
	normal = DateTime.Minutes(7),
	hard = DateTime.Minutes(5)
}

ParatroopersEnabledDelay = {
	easy = DateTime.Minutes(8),
	normal = DateTime.Minutes(6),
	hard = DateTime.Minutes(4)
}

MaxReactorFuelTime = DateTime.Minutes(10)

Squads = {
	Main = {
		Delay = {
			easy = DateTime.Minutes(5),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(3)
		},
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 25 },
			normal = { Min = 25, Max = 50 },
			hard = { Min = 40, Max = 80 },
		},
		FollowLeader = true,
		ProducerActors = { Infantry = { MainBarracks1, MainBarracks2 }, Vehicles = { MainFactory } },
		ProducerTypes = { Infantry = { "barr" }, Vehicles = { "weap" } },
		Units = AdjustCompositionsForDifficulty(UnitCompositions.Soviet),
		AttackPaths = AttackPaths,
	},
	AirAntiLight = {
		Delay = {
			easy = DateTime.Minutes(7),
			normal = DateTime.Minutes(6),
			hard = DateTime.Minutes(5)
		},
		AttackValuePerSecond = {
			easy = { Min = 5, Max = 10 },
			normal = { Min = 9, Max = 15 },
			hard = { Min = 13, Max = 40 },
		},
		ProducerTypes = { Aircraft = { "afld" } },
		Units = {
			easy = {
				{ Aircraft = { HindOrYak } },
			},
			normal = {
				{ Aircraft = { HindOrYak, HindOrYak } },
			},
			hard = {
				{ Aircraft = { HindOrYak, HindOrYak, HindOrYak } },
			}
		},
	},
	AirAntiHeavy = {
		Delay = {
			easy = DateTime.Minutes(13),
			normal = DateTime.Minutes(10),
			hard = DateTime.Minutes(7)
		},
		AttackValuePerSecond = {
			easy = { Min = 5, Max = 10 },
			normal = { Min = 9, Max = 15 },
			hard = { Min = 13, Max = 40 },
		},
		ProducerTypes = { Aircraft = { "afld" } },
		Units = {
			easy = {
				{ Aircraft = { MigOrSukhoi } },
			},
			normal = {
				{ Aircraft = { MigOrSukhoi, MigOrSukhoi } },
			},
			hard = {
				{ Aircraft = { MigOrSukhoi, MigOrSukhoi, MigOrSukhoi } },
			}
		},
	},
	AirAntiAir = {
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 10 },
			normal = { Min = 15, Max = 15 },
			hard = { Min = 25, Max = 25 },
		},
		ActiveCondition = function()
			local gdiAircraft = GDI.GetActorsByTypes({ "orca", "a10", "orcb", "auro" })
			return #gdiAircraft > 3
		end,
		ProducerTypes = { Aircraft = { "afld" } },
		Units = {
			easy = {
				{ Aircraft = { "mig" } },
			},
			normal = {
				{ Aircraft = { "mig", "yak" } },
			},
			hard = {
				{ Aircraft = { "mig", "mig", "yak" } },
			}
		},
	},
	Kirovs = {
		Delay = {
			easy = DateTime.Minutes(15),
			normal = DateTime.Minutes(13),
			hard = DateTime.Minutes(11)
		},
		AttackValuePerSecond = {
			easy = { Min = 5, Max = 10 },
			normal = { Min = 9, Max = 25 },
			hard = { Min = 13, Max = 40 },
		},
		ProducerTypes = { Aircraft = { "afld" } },
		Units = {
			easy = {
				{ Aircraft = { "kiro" } },
			},
			normal = {
				{ Aircraft = { "kiro" }, MaxTime = DateTime.Minutes(20) },
				{ Aircraft = { "kiro", "kiro" }, MinTime = DateTime.Minutes(20) },
			},
			hard = {
				{ Aircraft = { "kiro" }, MaxTime = DateTime.Minutes(10) },
				{ Aircraft = { "kiro", "kiro" }, MinTime = DateTime.Minutes(10), MaxTime = DateTime.Minutes(20) },
				{ Aircraft = { "kiro", "kiro", "kiro" }, MinTime = DateTime.Minutes(20), MaxTime = DateTime.Minutes(30) },
				{ Aircraft = { "kiro", "kiro", "kiro", "kiro" }, MinTime = DateTime.Minutes(30) }
			}
		},
		AttackPaths = {
			{ KirovPath1_1.Location, KirovPath1_2.Location, KirovPath1_3.Location, KirovPath1_4.Location },
			{ KirovPath2_1.Location, KirovPath2_2.Location, KirovPath2_3.Location },
			{ AttackRally2.Location },
		},
	},
}

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	USSR = Player.GetPlayer("USSR")
	MissionPlayers = { GDI }
	TimerTicks = MaxReactorFuelTime
	CurrentDelivery = 1
	McvArrived = false

	Camera.Position = Spy.CenterPosition

	InitObjectives(GDI)
	AdjustPlayerStartingCashForDifficulty()
	InitUSSR()

	ObjectiveCaptureOrDestroyBunker = GDI.AddObjective("Capture or destroy Stalin's bunker.")
	ObjectiveStarveAtomicReactor = GDI.AddSecondaryObjective("Cut supply lines to starve atomic reactor of fuel.")
	ObjectiveDestroyTeslaReactors = GDI.AddSecondaryObjective("Destroy Tesla reactors on southeastern island.")

	Trigger.OnKilledOrCaptured(StalinHQ, function()
		GDI.MarkCompletedObjective(ObjectiveCaptureOrDestroyBunker)
	end)

	Trigger.OnKilledOrCaptured(AtomicReactor, function()
		ReactorStarved()
	end)

	Trigger.OnAllKilledOrCaptured(TeslaReactors, function()
		TeslaReactorsOffline()
	end)

	Trigger.AfterDelay(DateTime.Minutes(2), function()
		DoDelivery()
	end)

	Trigger.AfterDelay(DateTime.Seconds(13), function()
		Media.PlaySpeechNotification(GDI, "ReinforcementsArrived")
		Notification("Reinforcements have arrived.")
		Reinforcements.Reinforce(GDI, { "amcv" }, { McvSpawn.Location, PlayerStart.Location }, 75)
		McvArrived = true
	end)

	local revealPoints = { SupplyReveal1, SupplyReveal2, SupplyReveal3 }
	Utils.Do(revealPoints, function(p)
		Trigger.OnEnteredProximityTrigger(p.CenterPosition, WDist.New(12 * 1024), function(a, id)
			if a.Owner == GDI and a.Type ~= "camera" then
				Trigger.RemoveProximityTrigger(id)
				local camera = Actor.Create("camera", true, { Owner = GDI, Location = p.Location })
				Notification("Fuel supply route identified.")
				Beacon.New(GDI, p.CenterPosition)
				Trigger.AfterDelay(DateTime.Seconds(4), function()
					camera.Destroy()
				end)
			end
		end)
	end)

	Spy.DisguiseAs(SpyDisguiseTarget)
	Trigger.AfterDelay(DateTime.Seconds(4), function()
		Beacon.New(GDI, Spy.CenterPosition)
		Media.DisplayMessage("It feels like they're getting suspicious, I'm getting out of here...", "Allied Spy", HSLColor.FromHex("1E90FF"))
		MediaCA.PlaySound("suspicious.aud", 2)
		Spy.Move(SouthDelivery3.Location)
		SpyKiller.Attack(Spy)
		Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(12)), function()
			Media.DisplayMessage("Attention you capitalist dogs! My defenses are impenetrable. Leave at once, or prepare to be crushed!", "Stalin", HSLColor.FromHex("DD0000"))
			MediaCA.PlaySound("stalin_warning.aud", 2)
		end)
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	OncePerThirtySecondChecks()
	PanToStart()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		USSR.Resources = USSR.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
				ReactorStarved()
			end
		end

		UpdateObjectiveText()

		if GDI.HasNoRequiredUnits() then
			GDI.MarkFailedObjective(ObjectiveCaptureOrDestroyBunker)
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()
	end
end

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % DateTime.Seconds(30) == 0 then
		CalculatePlayerCharacteristics()
	end
end

InitUSSR = function()
	RebuildExcludes.USSR = { Types = { "tpwr", "tsla", "sam", "npwr" } }

	AutoRepairAndRebuildBuildings(USSR, 15)
	SetupRefAndSilosCaptureCredits(USSR)
	AutoReplaceHarvesters(USSR)
	AutoRebuildConyards(USSR)
	InitAiUpgrades(USSR)

	Actor.Create("ai.unlimited.power", true, { Owner = USSR })

	local ussrGroundAttackers = USSR.GetGroundAttackers()

	Utils.Do(ussrGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsUSSRGroundHunterUnit)
	end)

	Utils.Do(InnerTeslas, function(a)
		if not a.IsDead then
			a.GrantCondition("invulnerability")
		end
	end)

	Trigger.AfterDelay(Squads.Main.Delay[Difficulty], function()
		InitAttackSquad(Squads.Main, USSR)
	end)

	Trigger.AfterDelay(Squads.AirAntiLight.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.AirAntiLight, USSR, GDI, { "Light" }, "ArmorType")
	end)

	Trigger.AfterDelay(Squads.AirAntiHeavy.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.AirAntiHeavy, USSR, GDI, { "Heavy" }, "ArmorType")
	end)

	InitAirAttackSquad(Squads.AirAntiAir, USSR, GDI, { "Aircraft" }, "ArmorType")

	Trigger.AfterDelay(Squads.Kirovs.Delay[Difficulty], function()
		InitAttackSquad(Squads.Kirovs, USSR)
	end)

	Trigger.AfterDelay(ParabombsEnabledDelay[Difficulty], function()
		if not MainAirfield.IsDead then
			MainAirfield.GrantCondition("parabombs-enabled")
		end
	end)

	Trigger.AfterDelay(ParatroopersEnabledDelay[Difficulty], function()
		if not SovietRadar.IsDead then
			SovietRadar.GrantCondition("paratroopers-enabled")
		end
	end)

	Trigger.OnEnteredFootprint({ReactorDeliveryPoint1.Location, ReactorDeliveryPoint2.Location, ReactorDeliveryPoint3.Location, ReactorDeliveryPoint4.Location }, function(a, id)
		if a.Owner == USSR and a.Type == "utrk" then
			a.Destroy()
			if not GDI.IsObjectiveCompleted(ObjectiveStarveAtomicReactor) then
				TimerTicks = TimerTicks + DateTime.Minutes(5)
				if TimerTicks > MaxReactorFuelTime then
					TimerTicks = MaxReactorFuelTime
				end
				Notification("A fuel shipment has reached the Soviet reactor.")
				MediaCA.PlaySound("c_fuelshipment.aud", 2)
			end
		end
	end)
end

DoDelivery = function()
	local delivery = Deliveries[CurrentDelivery]

	local truck = Reinforcements.Reinforce(USSR, { "utrk" }, { delivery.Spawn }, 50, function(truck)
		Utils.Do(delivery.Path, function(waypoint)
			truck.Move(waypoint)
		end)

		Trigger.OnIdle(truck, function(self)
			truck.Move(delivery.Path[#delivery.Path - 1])
			truck.Move(delivery.Path[#delivery.Path])
		end)
	end)

	if CurrentDelivery < #Deliveries then
		CurrentDelivery = CurrentDelivery + 1
	else
		CurrentDelivery = 1
	end

	Trigger.AfterDelay(DateTime.Minutes(2), function()
		if not GDI.IsObjectiveCompleted(ObjectiveStarveAtomicReactor) then
			DoDelivery()
		end
	end)
end

ReactorStarved = function()
	if not IsReactorStarved then
		IsReactorStarved = true
		GDI.MarkCompletedObjective(ObjectiveStarveAtomicReactor)
		DisableMainPower()

		if not StalinHQ.IsDead then
			StalinHQ.GrantCondition("ic-offline")
		end

		if not AtomicReactor.IsDead then
			AtomicReactor.GrantCondition("ic-offline")
			AtomicReactor.GrantCondition("disabled")
		end

		Utils.Do(InnerTeslas, function(a)
			if not a.IsDead then
				a.GrantCondition("ic-offline")
			end
		end)

		local notificationText = "Atomic Reactor shutting down."
		if AreTeslaReactorsOffline then
			notificationText = notificationText .. ". The Soviet base is now without power."
		else
			notificationText = notificationText .. ". The Telsa Reactors in the south-east continue to provide the base with power."
		end
		Notification(notificationText)
		MediaCA.PlaySound("c_atomicshutdown.aud", 2)
		Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(3)), function()
			if AreTeslaReactorsOffline then
				MediaCA.PlaySound("c_sovietbasenopower.aud", 2)
			else
				MediaCA.PlaySound("c_teslareactorsremain.aud", 2)
			end
		end)
	end
end

TeslaReactorsOffline = function()
	if not AreTeslaReactorsOffline then
		AreTeslaReactorsOffline = true
		GDI.MarkCompletedObjective(ObjectiveDestroyTeslaReactors)
		DisableMainPower()

		Utils.Do(OuterSAMs, function(a)
			if not a.IsDead then
				a.GrantCondition("disabled")
			end
		end)

		local defenses = USSR.GetActorsByTypes({ "sam", "tsla" })
		Utils.Do(defenses, function(a)
			if not a.IsDead then
				a.GrantCondition("buff-removed")
			end
		end)

		local notificationText = "Soviet secondary power is offline."
		if IsReactorStarved then
			notificationText = notificationText .. " The Soviet base is now without power."
		else
			notificationText = notificationText .. " Tesla Coils are no longer supercharged and some perimeter air defenses are down, however the Atomic Reactor continues to provide the base with power."
		end
		Notification(notificationText)
		MediaCA.PlaySound("c_sovietsecondarypoweroffline.aud", 2)
		Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(3)), function()
			if IsReactorStarved then
				MediaCA.PlaySound("c_sovietbasenopower.aud", 2)
			else
				MediaCA.PlaySound("c_atomicup.aud", 2)
			end
		end)
	end
end

UpdateObjectiveText = function()
	if not GDI.IsObjectiveCompleted(ObjectiveStarveAtomicReactor) then
		local percentage = math.floor(TimerTicks / MaxReactorFuelTime * 100)
		UserInterface.SetMissionText("Atomic Reactor fuel level: " .. percentage .. "%", HSLColor.Yellow)
	else
		UserInterface.SetMissionText("Capture or destroy Stalin's bunker.", HSLColor.Yellow)
	end
end

DisableMainPower = function()
	if IsReactorStarved and AreTeslaReactorsOffline then
		local defenses = USSR.GetActorsByTypes({ "sam", "tsla" })
		Utils.Do(defenses, function(a)
			if not a.IsDead then
				a.GrantCondition("disabled")
			end
		end)
	end
end

PanToStart = function()
	if PanToStartComplete or not McvArrived then
		return
	end

	local targetPos = PlayerStart.CenterPosition
	PanToPos(targetPos, 1536)

	if Camera.Position.X == targetPos.X and Camera.Position.Y == targetPos.Y then
		PanToStartComplete = true
	end
end
