
Squads = {
	NodAir = {
		Delay = {
			easy = DateTime.Minutes(15),
			normal = DateTime.Minutes(11),
			hard = DateTime.Minutes(7)
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

    ObjectiveCaptureComms = GDI.AddObjective("Locate and capture Nod Communications Center.")

    Trigger.OnAllKilled({ Shard1, Shard2 }, function()
        InitMcv()
    end)

    Trigger.OnCapture(NodCommsCenter, function(self, captor, oldOwner, newOwner)
        if newOwner == GDI then
            GDI.MarkCompletedObjective(ObjectiveCaptureComms)
        end
    end)
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
