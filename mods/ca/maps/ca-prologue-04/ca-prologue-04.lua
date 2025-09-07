Difficulty = "easy"

GDIAttackPaths = {
	{ AttackPath1.Location, AttackPath1.Location, AttackPath3.Location, AttackPath4.Location },
}

Squads = {
	Main = {
		Delay = DateTime.Minutes(5),
		AttackValuePerSecond = { Min = 12, Max = 12 },
		FollowLeader = true,
		Compositions = {
			{
				Infantry = { "e1", "e1", "e1", "e2" },
				Vehicles = { { "hmmv", "mtnk", "apc2" } },
			},
		},
		AttackPaths = GDIAttackPaths,
	},
}

-- Setup and Tick

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")
	MissionPlayers = { Nod }
	MissionEnemies = { GDI }

	Camera.Position = PlayerStart.CenterPosition
	WarpInBeaconPos = RocksToRemove1.CenterPosition

	InitObjectives(Nod)
	InitGDI()

	ObjectiveDestroyAA = Nod.AddObjective("Destroy GDI anti-aircraft defenses.")

	local aaGuns = GDI.GetActorsByType("cram")
	Trigger.OnAllKilled(aaGuns, function()
		local frigates = GDI.GetActorsByType("dd2")
		ObjectiveDestroyFrigates = Nod.AddObjective("Destroy GDI naval blockade.")

		Trigger.AfterDelay(DateTime.Seconds(6), function()
			WarpInBanshees()
		end)

		Trigger.OnAllKilled(frigates, function()
			Nod.MarkCompletedObjective(ObjectiveDestroyFrigates)
		end)

		Nod.MarkCompletedObjective(ObjectiveDestroyAA)
	end)

	AfterWorldLoaded()
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	PanToBanshees()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		GDI.Resources = GDI.ResourceCapacity - 500

		if MissionPlayersHaveNoRequiredUnits() then
			if not Nod.IsObjectiveCompleted(ObjectiveDestroyAA) then
				Nod.MarkFailedObjective(ObjectiveDestroyAA)
			end
			if ObjectiveDestroyFrigates ~= nil and not Nod.IsObjectiveCompleted(ObjectiveDestroyFrigates) then
				Nod.MarkFailedObjective(ObjectiveDestroyFrigates)
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

InitGDI = function()
	AutoRepairBuildings(GDI)
	SetupRefAndSilosCaptureCredits(GDI)
	AutoReplaceHarvesters(GDI)
	InitAttackSquad(Squads.Main, GDI)

	local gdiGroundAttackers = GDI.GetGroundAttackers()

	Utils.Do(gdiGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGDIGroundHunterUnit)
	end)
end

WarpInBanshees = function()
	if BansheesWarped then
		return
	end
	BansheesWarped = true
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Lighting.Flash("Chronoshift", 10)
		Media.PlaySound("chrono2.aud")
		Actor.Create("warpin", true, { Owner = Nod, Location = RocksToRemove1.Location })
		Beacon.New(Nod, WarpInBeaconPos)

		RocksToRemove1.Destroy()
		RocksToRemove2.Destroy()

		local hpad1 = Actor.Create("hpad.td", true, { Owner = Nod, Location = HpadSpawn1.Location })
		local hpad2 = Actor.Create("hpad.td", true, { Owner = Nod, Location = HpadSpawn2.Location  })

		Trigger.AfterDelay(10, function()
			Actor.Create("scrn", true, { Owner = Nod, Location = hpad1.Location, CenterPosition = hpad1.CenterPosition, Facing = Angle.NorthEast })
			Actor.Create("scrn", true, { Owner = Nod, Location = hpad1.Location, CenterPosition = hpad2.CenterPosition, Facing = Angle.NorthEast })
		end)

		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Media.PlaySpeechNotification(Nod, "ReinforcementsArrived")
			Notification("Reinforcements have arrived.")

			Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(3)), function()
				Media.DisplayMessage("You have done well commander! Now behold; a taste of things to come. Use them wisely.", "Kane", HSLColor.FromHex("FF0000"))
				MediaCA.PlaySound("thingstocome.aud", 2)
			end)
		end)
	end)
end

PanToBanshees = function()
	if PanToBansheesComplete or not BansheesWarped then
		return
	end

	local targetPos = WarpInBeaconPos
	PanToPos(targetPos, 1024)

	if Camera.Position.X == targetPos.X and Camera.Position.Y == targetPos.Y then
		PanToBansheesComplete = true
	end
end
