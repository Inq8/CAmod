OutpostStructures = { OutpostConyard, OutpostFactory, OutpostBarracks, OutpostRefinery, OutpostPower1, OutpostPower2, OutpostPower3, OutpostSilo1, OutpostSilo2, OutpostGuardTower1, OutpostGuardTower2, OutpostGuardTower3, OutpostGuardTower4 }

SuperweaponsEnabledTime = {
	easy = DateTime.Seconds((60 * 45) + 17),
	normal = DateTime.Seconds((60 * 30) + 17),
	hard = DateTime.Seconds((60 * 15) + 17),
}

Squads = {
	GDIMain1 = {
		Delay = {
			easy = DateTime.Seconds(330),
			normal = DateTime.Seconds(210),
			hard = DateTime.Seconds(90)
		},
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 25 },
			normal = { Min = 25, Max = 50 },
			hard = { Min = 40, Max = 80 },
		},
		FollowLeader = true,
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = UnitCompositions.GDI.Main,
		AttackPaths = {
			{ Path1_1.Location, Path1_2.Location },
			{ Path2_1.Location, Path2_2.Location },
			{ Path3_1.Location, Path3_2.Location },
		},
	},
	GDIMain2 = {
		Delay = {
			easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(2)
		},
		AttackValuePerSecond = {
			easy = { Min = 10, Max = 25 },
			normal = { Min = 25, Max = 50 },
			hard = { Min = 40, Max = 80 },
		},
		FollowLeader = true,
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = UnitCompositions.GDI.Main,
		AttackPaths = {
			{ Path1_1.Location, Path1_2.Location },
			{ Path2_1.Location, Path2_2.Location },
			{ Path3_1.Location, Path3_2.Location },
		},
	},
	GDIAir = {
		Delay = {
			easy = DateTime.Minutes(13),
			normal = DateTime.Minutes(12),
			hard = DateTime.Minutes(11)
		},
		AttackValuePerSecond = {
			easy = { Min = 7, Max = 7 },
			normal = { Min = 14, Max = 14 },
			hard = { Min = 21, Max = 21 },
		},
		ProducerTypes = { Aircraft = { "afld.gdi" } },
		Units = {
			easy = {
				{ Aircraft = { "orca" } },
				{ Aircraft = { "orcb" } },
			},
			normal = {
				{ Aircraft = { "orca", "orca" } },
				{ Aircraft = { "a10" } },
				{ Aircraft = { "a10.gau" } },
				{ Aircraft = { "orcb" } },
				{ Aircraft = { "auro" } },
			},
			hard = {
				{ Aircraft = { "orca", "orca", "orca" } },
				{ Aircraft = { "a10", "a10" } },
				{ Aircraft = { "a10.gau", "a10.gau" } },
				{ Aircraft = { "orcb", "orcb" } },
				{ Aircraft = { "auro", "auro" } },
			}
		},
	}
}

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	GDI = Player.GetPlayer("GDI")
	China = Player.GetPlayer("China")
	MissionPlayers = { USSR }
	TimerTicks = 0

	Camera.Position = McvRally.CenterPosition

	InitObjectives(USSR)
	AdjustStartingCash()
	InitGDI()

	ObjectiveAcquireWeapons = USSR.AddObjective("Acquire Chinese weapons.")
	ObjectiveExpelGDI = USSR.AddObjective("Remove the GDI presence.")

	if Difficulty == "hard" then
		NonHardTroopCrawler.Destroy()
		NonHardOverlord.Destroy()
		NonHardNukeCannon.Destroy()
	end

	Trigger.OnEnteredProximityTrigger(WeaponsCache.CenterPosition, WDist.New(4 * 1024), function(a, id)
		if a.Owner == USSR then
			if not CacheFound then
				Trigger.RemoveProximityTrigger(id)
				CacheFound = true
				local cacheUnits = China.GetActorsByTypes({"ovld", "trpc.empty", "nukc"})
				Utils.Do(cacheUnits, function(u)
					u.Owner = USSR
					USSR.MarkCompletedObjective(ObjectiveAcquireWeapons)
				end)
				Trigger.AfterDelay(DateTime.Seconds(5), function()
					local outpostFlare = Actor.Create("flare", true, { Owner = USSR, Location = GDIOutpostFlare.Location })
					Media.PlaySpeechNotification(USSR, "SignalFlare")
					Notification("Signal flare detected.")
					Beacon.New(USSR, GDIOutpostFlare.CenterPosition)

					Trigger.OnEnteredProximityTrigger(GDIOutpostFlare.CenterPosition, WDist.New(6 * 1024), function(a, id)
						if a.Owner == USSR and a.Type ~= "flare" then
							Trigger.RemoveProximityTrigger(id)
							outpostFlare.Destroy()
						end
					end)
				end)
			end
		end
	end)

	Trigger.OnAllKilledOrCaptured(OutpostStructures, function()
		if not McvRequested then
			McvRequested = true
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
				Notification("Reinforcements have arrived.")
				Reinforcements.Reinforce(USSR, { "mcv" }, { McvSpawn.Location, McvRally.Location })
				Beacon.New(USSR, McvRally.CenterPosition)
				McvArrived = true
			end)

			Trigger.AfterDelay(Squads.GDIMain1.Delay[Difficulty], function()
				InitAttackSquad(Squads.GDIMain1, GDI)
			end)

			Trigger.AfterDelay(Squads.GDIMain2.Delay[Difficulty], function()
				InitAttackSquad(Squads.GDIMain2, GDI)
			end)

			Trigger.AfterDelay(Squads.GDIAir.Delay[Difficulty], function()
				InitAirAttackSquad(Squads.GDIAir, GDI, USSR, { "harv", "4tnk", "4tnk.atomic", "3tnk", "3tnk.atomic", "3tnk.rhino", "3tnk.rhino.atomic",
					"katy", "v3rl", "ttra", "v3rl", "apwr", "tpwr", "npwr", "tsla", "proc", "nukc", "ovld", "apoc", "apoc.atomic", "ovld.atomic" })
			end)
		end
	end)

	Trigger.AfterDelay(DateTime.Minutes(15), function()
		InitCommsCenterObjective()
	end)

	Trigger.OnEnteredProximityTrigger(GDICommsCenter.CenterPosition, WDist.New(20 * 1024), function(a, id)
		if a.Owner == USSR then
			Trigger.RemoveProximityTrigger(id)
			InitCommsCenterObjective()
		end
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		GDI.Resources = GDI.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
		end

		if GDI.HasNoRequiredUnits() then
			USSR.MarkCompletedObjective(ObjectiveExpelGDI)
		end

		if McvArrived and USSR.HasNoRequiredUnits() then
			USSR.MarkFailedObjective(ObjectiveExpelGDI)
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()
	end
end

InitGDI = function()
	RebuildExcludes.GDI = { Actors = OutpostStructures }

	AutoRepairAndRebuildBuildings(GDI, 15)
	SetupRefAndSilosCaptureCredits(GDI)
	AutoReplaceHarvesters(GDI)
	InitAiUpgrades(GDI)

	local gdiGroundAttackers = GDI.GetGroundAttackers()

	Utils.Do(gdiGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGDIGroundHunterUnit)
	end)

	Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = GDI })
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = GDI })
	end)
end

InitCommsCenterObjective = function()
	if ObjectiveCaptureComms ~= nil then
		return
	end

	Media.DisplayMessage("Comrade General, we have reason to believe the GDI Communications Center contains vital information. Capture it at all costs!", "Premier Cherdenko", HSLColor.FromHex("FF0000"))

	ObjectiveCaptureComms = USSR.AddObjective("Capture GDI Communications Center.")

	local camera = Actor.Create("smallcamera", true, { Owner = USSR, Location = GDICommsCenter.Location })
	Beacon.New(USSR, GDICommsCenter.CenterPosition)
	Media.PlaySound("beacon.aud")

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		camera.Destroy()
	end)

	Trigger.OnCapture(GDICommsCenter, function(self, captor, oldOwner, newOwner)
		USSR.MarkCompletedObjective(ObjectiveCaptureComms)
	end)

	Trigger.OnKilled(GDICommsCenter, function(self, killer)
		USSR.MarkFailedObjective(ObjectiveCaptureComms)
	end)
end
