MissionDir = "ca|missions/main-campaign/ca-prologue-02"

Difficulty = "easy"

AlliedAttackPaths = {
	{ WestAttackRally.Location, SovietBase.Location },
	{ EastAttackRally.Location, SovietBase.Location }
}

TeslaTrigger = { TeslaTrigger1.Location, TeslaTrigger2.Location, TeslaTrigger3.Location, TeslaTrigger4.Location, TeslaTrigger5.Location, TeslaTrigger6.Location, TeslaTrigger7.Location }
TeslaTriggerWest = { TeslaTriggerWest1.Location, TeslaTriggerWest2.Location, TeslaTriggerWest3.Location, TeslaTriggerWest4.Location, TeslaTriggerWest5.Location, TeslaTriggerWest6.Location, TeslaTriggerWest7.Location,
	TeslaTriggerWest8.Location, TeslaTriggerWest9.Location, TeslaTriggerWest10.Location, TeslaTriggerWest11.Location, TeslaTriggerWest12.Location, TeslaTriggerWest13.Location, TeslaTriggerWest14.Location,
	TeslaTriggerWest15.Location, TeslaTriggerWest16.Location }

Squads = {
	Main = {
		Delay = DateTime.Minutes(6),
		AttackValuePerSecond = { Min = 10, Max = 10 },
		FollowLeader = true,
		Compositions = {
			{
				Infantry = { "e3", "e1", "e1", "e1" },
				Vehicles = { "jeep" },
			},
		},
		AttackPaths = AlliedAttackPaths,
	},
}

-- Setup and Tick

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	Civilians = Player.GetPlayer("Civilians")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { USSR }
	MissionEnemies = { Greece }
	TimerTicks = 0

	Camera.Position = McvRally.CenterPosition

	InitObjectives(USSR)
	InitGreece()

	ObjectiveWipeOutVillage = USSR.AddObjective("Wipe out the village.")
	ObjectiveDestroyBase = USSR.AddObjective("Destroy the Allied base.")

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		Notification("Reinforcements have arrived.")
		Reinforcements.Reinforce(USSR, { "mcv" }, { McvSpawn.Location, McvRally.Location })
	end)

	Trigger.AfterDelay(DateTime.Minutes(1), function()
		local villageFlare = Actor.Create("flare", true, { Owner = USSR, Location = VillageCenter.Location })
		Media.PlaySpeechNotification(USSR, "SignalFlare")
		Notification("Signal flare detected. Press [" .. UtilsCA.Hotkey("ToLastEvent") .. "] to view location.")
		Beacon.New(USSR, VillageCenter.CenterPosition)
		Trigger.AfterDelay(DateTime.Minutes(5), villageFlare.Destroy)
	end)

	local civilianActors = Civilians.GetActors()
	local civilianTargets = Utils.Where(civilianActors, function(a)
		return a.HasProperty("Kill") and a.Type ~= "wood"
	end)
	Trigger.OnAllKilledOrCaptured(civilianTargets, function()
		USSR.MarkCompletedObjective(ObjectiveWipeOutVillage)
	end)

	local alliedBase = Greece.GetActorsByTypes({ "fact", "powr", "proc", "tent", "weap" })
	Trigger.OnAllKilledOrCaptured(alliedBase, function()
		USSR.MarkCompletedObjective(ObjectiveDestroyBase)
	end)

	Trigger.OnEnteredFootprint(TeslaTrigger, function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveFootprintTrigger(id)
			WarpInTeslaTanks(TeslaSpawn1.Location, TeslaSpawn2.Location, TeslaBeacon.Location)
		end
	end)

	Trigger.OnEnteredFootprint(TeslaTriggerWest, function(a, id)
		if IsMissionPlayer(a.Owner) then
			Trigger.RemoveFootprintTrigger(id)
			WarpInTeslaTanks(TeslaSpawnWest1.Location, TeslaSpawnWest2.Location, TeslaBeaconWest.Location)
		end
	end)

	Trigger.OnKilled(Church, function(self, killer)
		Actor.Create("moneycrate", true, { Owner = USSR, Location = Church.Location })
	end)

	AfterWorldLoaded()
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	AfterTick()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Greece.Resources = Greece.ResourceCapacity - 500

		if MissionPlayersHaveNoRequiredUnits() then
			if not USSR.IsObjectiveCompleted(ObjectiveWipeOutVillage) then
				USSR.MarkFailedObjective(ObjectiveWipeOutVillage)
			end
			if not USSR.IsObjectiveCompleted(ObjectiveDestroyBase) then
				USSR.MarkFailedObjective(ObjectiveDestroyBase)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()
	end
end

-- Functions

InitGreece = function()
	AutoRepairBuildings(Greece)
	SetupRefAndSilosCaptureCredits(Greece)
	AutoReplaceHarvesters(Greece)
	InitAttackSquad(Squads.Main, Greece)

	local greeceGroundAttackers = Greece.GetGroundAttackers()

	Utils.Do(greeceGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGreeceGroundHunterUnit)
	end)
end

WarpInTeslaTanks = function(TankLocation1, TankLocation2, EffectLocation)
	if TeslaTanksWarped then
		return
	end
	TeslaTanksWarped = true
	Lighting.Flash("Chronoshift", 10)
	Media.PlaySound("chrono2.aud")
	Actor.Create("warpin", true, { Owner = USSR, Location = EffectLocation })
	Actor.Create("ttnk", true, { Owner = USSR, Location = TankLocation1, Facing = Angle.South })
	Actor.Create("ttnk", true, { Owner = USSR, Location = TankLocation2, Facing = Angle.South })
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Media.DisplayMessage("Greetings Comrades! The Soviet Empire truly knows no boundaries!", "Tesla Tank", HSLColor.FromHex("FF0000"))
		MediaCA.PlaySound(MissionDir .. "/greetings.aud", 2)

		Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(6)), function()
			Media.DisplayMessage("We understand that Comrade Stalin has his doubts about our agreement. We hope these gifts will put his mind at ease.", "Unknown", HSLColor.FromHex("999999"))
			MediaCA.PlaySound(MissionDir .. "/doubts.aud", 2)
		end)
	end)
end
