-- Setup and Tick

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	Nod = Player.GetPlayer("Nod")

	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { Greece }
	TimerTicks = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Greece)
	InitNod()

    Actor.Create("optics.upgrade", true, { Owner = Greece })

    ObjectiveCaptureComms = Greece.AddObjective("Capture Nod Communications Center.")
	ObjectiveAmbushTransports = Greece.AddObjective("Ambush Nod transports.")
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Nod.Resources = Nod.ResourceCapacity - 500

		if Greece.HasNoRequiredUnits() then
			if not Greece.IsObjectiveCompleted(ObjectiveCaptureComms) then
				Greece.MarkFailedObjective(ObjectiveCaptureComms)
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

InitNod = function()
	AutoRepairBuildings(Nod)
	SetupRefAndSilosCaptureCredits(Nod)
	AutoReplaceHarvesters(Nod)

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)
end
