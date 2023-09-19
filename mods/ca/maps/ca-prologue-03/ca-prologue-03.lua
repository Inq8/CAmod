Difficulty = "normal"

-- Setup and Tick

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	USSR = Player.GetPlayer("USSR")
	HiddenGDI = Player.GetPlayer("HiddenGDI")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayer = GDI
	TimerTicks = 0
	GroupsFound = {}
	ExitDefendersDead = false
	Rescued = false

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(GDI)
	InitUSSR()

	ObjectiveLocateForces = GDI.AddObjective("Locate all GDI forces.")
	ObjectiveExit = GDI.AddObjective("Find an safe exit route.")

    SetupReveals({ Reveal1, Reveal2, Reveal3, Reveal4 })

	TroopGroups = {
		{ Waypoint = Group1, Id = 1 },
		{ Waypoint = Group2, Id = 2 },
		{ Waypoint = Group3, Id = 3 },
		{ Waypoint = Group4, Id = 4 },
		{ Waypoint = Group5, Id = 5 },
	}

	Utils.Do(TroopGroups, function(g)
		Trigger.OnEnteredProximityTrigger(g.Waypoint.CenterPosition, WDist.New(7 * 1024), function(a, id)
			if a.Owner == GDI and not GroupsFound[g.Id] then
				Trigger.RemoveProximityTrigger(id)
				GroupsFound[g.Id] = true
				Notification("GDI forces found.")
				MediaCA.PlaySound("gdifound.aud", "2")

				local groupActors = Map.ActorsInCircle(g.Waypoint.CenterPosition, WDist.New(8 * 1024));
				Utils.Do(groupActors, function(a)
					if a.Owner == HiddenGDI and not a.IsDead then
						a.Owner = GDI
					end
				end)

				if #GroupsFound == 5 then
					GDI.MarkCompletedObjective(ObjectiveLocateForces)

					Trigger.AfterDelay(DateTime.Seconds(4), function()
						Actor.Create("flare", true, { Owner = GDI, Location = SignalFlare.Location })
						Media.PlaySpeechNotification(GDI, "SignalFlare")
						Notification("Signal flare detected.")
						Beacon.New(GDI, SignalFlare.CenterPosition)
					end)
				end
			end
		end)
	end)

	Trigger.AfterDelay(1, function()
		local exitActors = Map.ActorsInCircle(SignalFlare.CenterPosition, WDist.New(8 * 1024));
		local exitDefenders = Utils.Where(exitActors, function(a)
			return a.Owner == USSR and a.HasProperty("Move")
		end)

		Trigger.OnAllKilled(exitDefenders, function()
			ExitDefendersDead = true
		end)
	end)
end

Tick = function()
	OncePerSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		if GDI.HasNoRequiredUnits() then
			if not GDI.IsObjectiveCompleted(ObjectiveLocateForces) then
				GDI.MarkFailedObjective(ObjectiveLocateForces)
			end
			if not GDI.IsObjectiveCompleted(ObjectiveExit) then
				GDI.MarkFailedObjective(ObjectiveExit)
			end
		end

		if ExitDefendersDead and not Rescued then
			local exitActors = Map.ActorsInCircle(SignalFlare.CenterPosition, WDist.New(8 * 1024));
			local gdiExitActors = Utils.Where(exitActors, function(a)
				return a.Owner == GDI
			end)

			if #gdiExitActors > 0 then
				Rescued = true
				Trigger.AfterDelay(DateTime.Seconds(2), function()
					Reinforcements.Reinforce(GDI, { "n1", "n2", "n1", "n2", "n1", "medi", "mtnk", "mtnk" }, { RescueSpawn.Location, RescueRally1.Location, RescueRally2.Location })

					Trigger.AfterDelay(DateTime.Seconds(2), function()
						Media.DisplayMessage("Hold your fire, I'm GDI! Damn, we thought we'd lost the whole company! We've got a base set up not far from here, I'll take you there.", "GDI Soldier", HSLColor.FromHex("F2CF74"))
						MediaCA.PlaySound("holdfire.aud", "2")

						Trigger.AfterDelay(DateTime.Seconds(12), function()
							GDI.MarkCompletedObjective(ObjectiveExit)
						end)
					end)
				end)
			end
		end
	end
end

-- Functions

InitUSSR = function()
	AutoRepairBuildings(USSR, 10)

	local ussrGroundAttackers = USSR.GetGroundAttackers()

	Utils.Do(ussrGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsUSSRGroundHunterUnit)
	end)
end
