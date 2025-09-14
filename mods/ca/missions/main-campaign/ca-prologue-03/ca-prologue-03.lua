MissionDir = "ca|missions/main-campaign/ca-prologue-03"

Difficulty = "easy"

-- Setup and Tick

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	USSR = Player.GetPlayer("USSR")
	HiddenGDI = Player.GetPlayer("HiddenGDI")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { GDI }
	MissionEnemies = { USSR }
	TimerTicks = 0
	GroupsFound = {}
	ExitDefendersDead = false
	Rescued = false

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(GDI)
	InitUSSR()

	ObjectiveLocateForces = GDI.AddObjective("Locate all GDI forces.")
	ObjectiveExit = GDI.AddObjective("Find a safe exit route.")

	SetupReveals({ Reveal1, Reveal3, Reveal4 })

	TroopGroups = {
		{ Waypoint = Group1, Id = 1 },
		{ Waypoint = Group2, Id = 2 },
		{ Waypoint = Group3, Id = 3 },
		{ Waypoint = Group4, Id = 4 },
		{ Waypoint = Group5, Id = 5 },
	}

	Trigger.OnEnteredProximityTrigger(Reveal2.CenterPosition, WDist.New(11 * 1024), function(a, id)
		if IsMissionPlayer(a.Owner) and a.Type ~= "smallcamera" and not FirstRevealComplete then
			Trigger.RemoveProximityTrigger(id)
			FirstRevealComplete = true
			local camera = Actor.Create("smallcamera", true, { Owner = GDI, Location = Reveal2.Location })

			if UtilsCA.FogEnabled() then
				Tip("When an enemy structure is destroyed under the fog of war, it won't disappear until its location is revealed again. The explosion sound and screen shake can be used to verify its destruction.")
			end

			Trigger.AfterDelay(DateTime.Seconds(4), function()
				camera.Destroy()
			end)
		end
	end)

	Utils.Do(TroopGroups, function(g)
		Trigger.OnEnteredProximityTrigger(g.Waypoint.CenterPosition, WDist.New(7 * 1024), function(a, id)
			if IsMissionPlayer(a.Owner) and not GroupsFound[g.Id] then
				Trigger.RemoveProximityTrigger(id)
				GroupsFound[g.Id] = true
				Notification("GDI forces found.")
				MediaCA.PlaySound(MissionDir .. "/gdifound.aud", 2)

				if g.Id == 2 then
					Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(2)), function()
						Media.DisplayMessage("Thank god! You found us!.", "GDI Soldier", HSLColor.FromHex("F2CF74"))
						MediaCA.PlaySound(MissionDir .. "/thankgod.aud", 1.5)
					end)
				end

				local groupActors = Map.ActorsInCircle(g.Waypoint.CenterPosition, WDist.New(8 * 1024));
				Utils.Do(groupActors, function(a)
					if a.Owner == HiddenGDI and not a.IsDead then
						a.Owner = GDI
					end
				end)

				local numGroupsFound = 0
				for k,v in pairs(GroupsFound) do
					numGroupsFound = numGroupsFound + 1
				end

				if numGroupsFound == 5 then
					GDI.MarkCompletedObjective(ObjectiveLocateForces)

					Trigger.AfterDelay(DateTime.Seconds(4), function()
						Actor.Create("flare", true, { Owner = GDI, Location = SignalFlare.Location })
						Media.PlaySpeechNotification(GDI, "SignalFlare")
						Notification("Signal flare detected. Press [" .. UtilsCA.Hotkey("ToLastEvent") .. "] to center screen on the most revent event.")
						Beacon.New(GDI, SignalFlare.CenterPosition)
					end)
				end
			end
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(4), function()
		Media.DisplayMessage("Commander what's going on, where the hell are we?!", "GDI Soldier", HSLColor.FromHex("F2CF74"))
		Media.PlaySound(MissionDir .. "/wherearewe.aud")

		Trigger.AfterDelay(DateTime.Seconds(20), function()
			Media.DisplayMessage("Come in, any GDI units, hostile troops have us pinned down.", "Radio", HSLColor.FromHex("F2CF74"))
			MediaCA.PlaySoundAtPos(MissionDir .. "/pinned.aud", 2, Camera.Position + WVec.New(2560, 0, 0))
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

	DistGuns()
	Chatter()
	AfterWorldLoaded()
end

Tick = function()
	OncePerSecondChecks()
	AfterTick()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		if MissionPlayersHaveNoRequiredUnits() then
			if not GDI.IsObjectiveCompleted(ObjectiveLocateForces) then
				GDI.MarkFailedObjective(ObjectiveLocateForces)
			end
			if not GDI.IsObjectiveCompleted(ObjectiveExit) then
				GDI.MarkFailedObjective(ObjectiveExit)
			end
		end

		if GDI.IsObjectiveCompleted(ObjectiveLocateForces) and ExitDefendersDead and not Rescued then
			local exitActors = Map.ActorsInCircle(SignalFlare.CenterPosition, WDist.New(8 * 1024));
			local gdiExitActors = Utils.Where(exitActors, function(a)
				return IsMissionPlayer(a.Owner)
			end)

			if #gdiExitActors > 0 then
				Rescued = true
				Trigger.AfterDelay(DateTime.Seconds(2), function()
					Reinforcements.Reinforce(GDI, { "n1", "n2", "n1", "n2", "n1", "medi", "mtnk", "mtnk" }, { RescueSpawn.Location, RescueRally1.Location, RescueRally2.Location })

					Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(2)), function()
						Media.DisplayMessage("Hold your fire, we're GDI! Damn, we thought we'd lost the whole company! We've got a base not far from here, we'll take you there.", "GDI Soldier", HSLColor.FromHex("F2CF74"))
						MediaCA.PlaySound(MissionDir .. "/holdfire.aud", 2)

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
	AutoRepairBuildings(USSR)

	local ussrGroundAttackers = USSR.GetGroundAttackers()

	Utils.Do(ussrGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsUSSRGroundHunterUnit)
	end)
end

SetupAmbience = function()
	DistGuns()
	Chatter()
end

DistGuns = function()
	local distGunsDelay = AdjustTimeForGameSpeed(Utils.RandomInteger(DateTime.Seconds(10), DateTime.Seconds(25)))
	Trigger.AfterDelay(distGunsDelay, function()
		if not GDI.IsObjectiveCompleted(ObjectiveLocateForces) then
			local distGunSounds = { MissionDir .. "/distguns.aud", MissionDir .. "/distguns2.aud", MissionDir .. "/distguns3.aud" }
			local cameraPos = Camera.Position
			local posModifier = WVec.New(Utils.Random({ -5120, 3072, 5120 }), 0, 0)
			MediaCA.PlaySoundAtPos(Utils.Random(distGunSounds), 1, cameraPos + posModifier)
			DistGuns()
		end
	end)
end

Chatter = function()
	local chatterSounds = { MissionDir .. "/chatter1.aud", MissionDir .. "/chatter2.aud", MissionDir .. "/chatter3.aud" }
	local delay = 0

	Utils.Do(Utils.Shuffle(chatterSounds), function(s)
		delay = delay + AdjustTimeForGameSpeed(Utils.RandomInteger(DateTime.Seconds(60), DateTime.Seconds(120)))
		Trigger.AfterDelay(delay, function()
			if not GDI.IsObjectiveCompleted(ObjectiveLocateForces) then
				local cameraPos = Camera.Position
				local posModifier = WVec.New(Utils.Random({ -2560, 2560 }), 0, 0)
				MediaCA.PlaySoundAtPos(s, 2, cameraPos + posModifier)
			end
		end)
	end)
end
