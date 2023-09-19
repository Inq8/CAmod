Difficulty = "normal"

AlliedAttackPaths = {
	{ WestAttackRally.Location, SovietBase.Location },
	{ EastAttackRally.Location, SovietBase.Location }
}

TeslaTrigger = { TeslaTrigger1.Location, TeslaTrigger2.Location, TeslaTrigger3.Location, TeslaTrigger4.Location, TeslaTrigger5.Location, TeslaTrigger6.Location, TeslaTrigger7.Location }

Squads = {
	Main = {
		Player = nil,
		Delay = {
			normal = DateTime.Minutes(6),
		},
		AttackValuePerSecond = {
			normal = { { MinTime = 0, Value = 10 } },
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false
		},
		FollowLeader = true,
		IdleUnits = { },
		ProducerTypes = { Infantry = { "tent" }, Vehicles = { "weap" } },
		Units = {
			normal = {
				{
					Infantry = { "e3", "e1", "e1", "e1" },
					Vehicles = { "jeep" },
				},
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
	MissionPlayer = USSR
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
		Notification("Signal flare detected.")
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
		if a.Owner == USSR then
			Trigger.RemoveFootprintTrigger(id)
			WarpInTeslaTanks()
		end
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Greece.Resources = Greece.ResourceCapacity - 500

		if USSR.HasNoRequiredUnits() then
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
		UpdatePlayerBaseLocation()
	end
end

-- Functions

InitGreece = function()
	AutoRepairBuildings(Greece, 10)
	SetupRefAndSilosCaptureCredits(Greece)
	AutoReplaceHarvesters(Greece)

	-- Begin main attacks after difficulty based delay
	Trigger.AfterDelay(Squads.Main.Delay[Difficulty], function()
		InitAttackSquad(Squads.Main, Greece)
	end)

	local greeceGroundAttackers = Greece.GetGroundAttackers()

	Utils.Do(greeceGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGreeceGroundHunterUnit)
	end)
end

WarpInTeslaTanks = function()
	if TelsaTanksWarped then
		return
	end
	TelsaTanksWarped = true
	Lighting.Flash("Chronoshift", 10)
	Media.PlaySound("chrono2.aud")
	Actor.Create("warpin", true, { Owner = USSR, Location = TeslaBeacon.Location })
	Actor.Create("ttnk", true, { Owner = USSR, Location = TeslaSpawn1.Location, Facing = Angle.South })
	Actor.Create("ttnk", true, { Owner = USSR, Location = TeslaSpawn2.Location, Facing = Angle.South })
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		Notification("Reinforcements have arrived.")

		Trigger.AfterDelay(DateTime.Seconds(3), function()
			Media.DisplayMessage("Greetings Comrade! The Soviet Empire truly knows no boundaries!", "Unknown", HSLColor.FromHex("FF0000"))
			MediaCA.PlaySound("greetings.aud", "2")
		end)
	end)
end
