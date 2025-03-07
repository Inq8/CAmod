
ShardLaunchers = { Shard1, Shard2, Shard3, Shard4, Shard5, Shard6 }

Squads = {
	NodAir = {
		Delay = {
			easy = DateTime.Minutes(11),
			normal = DateTime.Minutes(8),
			hard = DateTime.Minutes(5)
		},
		AttackValuePerSecond = {
			easy = { Min = 3, Max = 3 },
			normal = { Min = 7, Max = 7 },
			hard = { Min = 12, Max = 12 },
		},
		ProducerTypes = { Aircraft = { "hpad.td" } },
		Units = {
			easy = {
				{ Aircraft = { "apch" } }
			},
			normal = {
				{ Aircraft = { "apch", "apch" } },
				{ Aircraft = { "venm", "venm" } },
				{ Aircraft = { "scrn" } },
				{ Aircraft = { "rah" } }
			},
			hard = {
				{ Aircraft = { "apch", "apch", "apch" } },
				{ Aircraft = { "venm", "venm", "venm" } },
				{ Aircraft = { "scrn", "scrn" } },
				{ Aircraft = { "rah", "rah" } }
			}
		},
	}
}

WorldLoaded = function()
    GDI = Player.GetPlayer("GDI")
	Scrin = Player.GetPlayer("Scrin")
	Nod = Player.GetPlayer("Nod")
    MissionPlayers = { GDI }

    Camera.Position = PlayerStart.CenterPosition

	InitObjectives(GDI)
	AdjustStartingCash()
	InitNod()
	UpdateMissionText()

	ObjectiveDestroyShardLaunchers = GDI.AddObjective("Destroy Scrin Shard Launchers.")
    ObjectiveCaptureComms = GDI.AddObjective("Locate and capture Nod Communications Center.")

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
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Nod.Resources = Nod.ResourceCapacity - 500

		if GDI.HasNoRequiredUnits() then
			if not GDI.IsObjectiveCompleted(ObjectiveCaptureComms) then
				GDI.MarkFailedObjective(ObjectiveCaptureComms)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()
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

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)

	Trigger.AfterDelay(Squads.NodAir.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.NodAir, Nod, GDI)
	end)
end

InitMcv = function()
	Media.PlaySpeechNotification(GDI, "ReinforcementsArrived")
	Notification("Reinforcements have arrived.")
    local entryPath = { CarryallSpawn.Location, CarryallDest.Location }
    local exitPath =  { CarryallSpawn.Location }
    ReinforcementsCA.ReinforceWithTransport(GDI, "ocar.amcv", nil, entryPath, exitPath)
end
