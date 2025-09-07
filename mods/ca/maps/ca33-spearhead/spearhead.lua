
ShardLaunchers = { Shard1, Shard2, Shard3, Shard4, Shard5, Shard6 }

AdjustedNodCompositions = AdjustCompositionsForDifficulty(UnitCompositions.Nod)

Squads = {
	Main = {
		AttackValuePerSecond = {
			normal = { Min = 14, Max = 14 },
			hard = { Min = 20, Max = 20 },
			vhard = { Min = 26, Max = 26 },
			brutal = { Min = 32, Max = 32 }
		},
		ActiveCondition = function()
			return HasConyardAcrossRiver() or (Difficulty == "brutal" and HasUnitsAcrossRiver())
		end,
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		Compositions = AdjustedNodCompositions,
	},
	NodAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(9)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Nod,
	}
}

WorldLoaded = function()
    GDI = Player.GetPlayer("GDI")
	Scrin = Player.GetPlayer("Scrin")
	Nod = Player.GetPlayer("Nod")
    MissionPlayers = { GDI }

    Camera.Position = PlayerStart.CenterPosition

	InitObjectives(GDI)
	AdjustPlayerStartingCashForDifficulty()
	InitNod()
	UpdateMissionText()

	ObjectiveDestroyShardLaunchers = GDI.AddObjective("Destroy Scrin Shard Launchers.")
    ObjectiveCaptureComms = GDI.AddObjective("Locate and capture Nod Communications Center.")

	if IsHardOrBelow() then
		HardOnlyTripod.Destroy()
		HardOnlyAvatar1.Destroy()
		HardOnlyAvatar2.Destroy()
		HardOnlyCyborg1.Destroy()
		HardOnlyCyborg5.Destroy()

		if IsNormalOrBelow() then
			HardOnlyCyborg2.Destroy()
			HardOnlyCyborg3.Destroy()
			HardOnlyCyborg4.Destroy()
			HardOnlyCyborg6.Destroy()
			HardOnlyCyborg7.Destroy()
			HardOnlyCyborg8.Destroy()
		end
	end

    Trigger.OnAllKilled(ShardLaunchers, function()
        InitMcv()
		GDI.MarkCompletedObjective(ObjectiveDestroyShardLaunchers)
    end)

	Utils.Do(ShardLaunchers, function(s)
		Trigger.OnKilled(s, function(self, killer)
			UpdateMissionText()
		end)
	end)

    Trigger.OnCapture(NodCommsCenter, function(self, captor, oldOwner, newOwner)
        if newOwner == GDI then
            GDI.MarkCompletedObjective(ObjectiveCaptureComms)
        end
    end)

	Trigger.OnKilled(NodCommsCenter, function(self, killer)
		if not GDI.IsObjectiveCompleted(ObjectiveCaptureComms) then
			GDI.MarkFailedObjective(ObjectiveCaptureComms)
		end
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Nod.Resources = Nod.ResourceCapacity - 500

		if MissionPlayersHaveNoRequiredUnits() then
			if not GDI.IsObjectiveCompleted(ObjectiveCaptureComms) then
				GDI.MarkFailedObjective(ObjectiveCaptureComms)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()

		if Difficulty ~= "easy" and not NodGroundAttacksStarted and HasConyardAcrossRiver() then
			NodGroundAttacksStarted = true
			InitAttackSquad(Squads.Main, Nod)
		end
	end
end

UpdateMissionText = function()
	ShardLaunchersRemaining = #Utils.Where(ShardLaunchers, function(s) return not s.IsDead end)

	if ShardLaunchersRemaining > 0 then
		UserInterface.SetMissionText("Shard Launchers remaining: " .. ShardLaunchersRemaining, HSLColor.Yellow)
	else
		UserInterface.SetMissionText("")
	end
end

InitNod = function()
	AutoRepairBuildings(Nod)
	AutoRepairAndRebuildBuildings(Nod)
	SetupRefAndSilosCaptureCredits(Nod)
	AutoReplaceHarvesters(Nod)
	InitAiUpgrades(Nod)
	InitAirAttackSquad(Squads.NodAir, Nod)

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)
end

InitMcv = function()
	Media.PlaySpeechNotification(GDI, "ReinforcementsArrived")
	Notification("Reinforcements have arrived.")
    local entryPath = { CarryallSpawn.Location, CarryallDest.Location }
    local exitPath =  { CarryallSpawn.Location }
    ReinforcementsCA.ReinforceWithTransport(GDI, "ocar.amcv", nil, entryPath, exitPath)
end

HasConyardAcrossRiver = function()
	local conyards = GDI.GetActorsByType("afac")

	local conyardsAcrossRiver = Utils.Where(conyards, function(c)
		return IsMissionPlayer(c.Owner) and c.Location.X > 34 and c.Location.Y > 72
	end)

	return #conyardsAcrossRiver > 0
end

HasUnitsAcrossRiver = function()
	local unitsAcrossRiver = Utils.Where(GDI.GetActors(), function(a)
		return a.HasProperty("Attack")
	end)

	return #unitsAcrossRiver > 0
end
