Squads = {
	Main = {
		Delay = {
			easy = DateTime.Minutes(8),
			normal = DateTime.Minutes(6),
			hard = DateTime.Minutes(4),
		},
		AttackValuePerSecond = {
			easy = { Min = 25, Max = 50 },
			normal = { Min = 50, Max = 100 },
			hard = { Min = 80, Max = 160 },
		},
		FollowLeader = true,
		ProducerActors = nil,
		ProducerTypes = { Infantry = BarracksTypes, Vehicles = FactoryTypes },
		Units = UnitCompositions.Soviet.Main,
		AttackPaths = {

		},
	},
}

-- Setup and Tick

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")

	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { Greece }
	TimerTicks = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Greece)
	InitUSSR()

	ObjectiveNameGoesHere = Greece.AddObjective("Objective description.")
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		USSR.Resources = USSR.ResourceCapacity - 500

		if Greece.HasNoRequiredUnits() then
			if not Greece.IsObjectiveCompleted(ObjectiveNameGoesHere) then
				Greece.MarkFailedObjective(ObjectiveNameGoesHere)
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

InitUSSR = function()
	AutoRepairAndRebuildBuildings(USSR)
	SetupRefAndSilosCaptureCredits(USSR)
	AutoReplaceHarvesters(USSR)

	local ussrGroundAttackers = USSR.GetGroundAttackers()

	Utils.Do(ussrGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsUSSRGroundHunterUnit)
	end)

	-- Begin main attacks after difficulty based delay
	Trigger.AfterDelay(Squads.Main.Delay[Difficulty], function()
		InitAttackSquad(Squads.Main, USSR)
	end)
end
