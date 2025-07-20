SAMs = {
	{ SAMSite = SAM1, Pos = SAM1Squad.CenterPosition, Rally = SAM1SquadRally.Location },
	{ SAMSite = SAM2, Pos = SAM2Squad.CenterPosition, Rally = SAM2SquadRally.Location },
	{ SAMSite = SAM3, Pos = SAM3Squad.CenterPosition, Rally = SAM3SquadRally.Location },
	{ SAMSite = SAM4, Pos = SAM4Squad.CenterPosition, Rally = SAM4SquadRally.Location },
	{ SAMSite = SAM5, Pos = SAM5Squad.CenterPosition, Rally = SAM5SquadRally.Location },
	{ SAMSite = SAM6, Pos = SAM6Squad.CenterPosition, Rally = SAM6SquadRally.Location },
	{ SAMSite = SAM7, Pos = SAM7Squad.CenterPosition, Rally = SAM7SquadRally.Location },
}

Patrols = {
	{ NodPatrol1_1.Location, NodPatrol1_2.Location, NodPatrol1_3.Location, NodPatrol1_4.Location, NodPatrol1_5.Location },
	{ NodPatrol2_1.Location, NodPatrol2_2.Location, NodPatrol2_3.Location },
}

-- Setup and Tick

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")
	England = Player.GetPlayer("England")
	MissionPlayers = { Greece }
	TimerTicks = 0

	local samsRemaining = Nod.GetActorsByType("nsam")
	SAMCount = #samsRemaining

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Greece)
	InitNod()

    Actor.Create("optics.upgrade", true, { Owner = Greece })
	Actor.Create("radar.dummy", true, { Owner = Greece })

    ObjectiveDestroySAMSites = Greece.AddObjective("Destroy Nod SAM Sites.")
	ObjectiveClearBase = Greece.AddObjective("Clear the Nod naval base.")
	ObjectiveApprehendTransports = Greece.AddObjective("Secure Nod transports.")

	UpdateMissionText()

	NodBaseCamera.Destroy()

	if IsNormalOrAbove() then
		Medic2.Destroy()
		Mechanic.Destroy()

		if IsHardOrAbove() then
			Medic1.Destroy()
			Ranger2.Destroy()
			NonHardSniper1.Destroy()
			NonHardMirage1.Destroy()

			if IsVeryHardOrAbove() then
				NonHardSniper2.Destroy()
			end
		end
	end

	if IsVeryHardOrBelow() then
		local brutalOnlyUnits = Map.GetActorsByTypes({ "shad", "hftk" })
		Utils.Do(brutalOnlyUnits, function(a) a.Destroy() end)

		if IsHardOrBelow() then
			local blackHand = Map.GetActorsByType("bh")
			Utils.Do(blackHand, function(a) a.Destroy() end)

			VeryHardOnlyFlameTank1.Destroy()
			VeryHardOnlyFlameTank2.Destroy()

			if IsNormalOrBelow() then
				HardOnlySSM.Destroy()
				HardOnlyFlameTank1.Destroy()
				HardOnlyFlameTank2.Destroy()
				HardOnlyFlameTank3.Destroy()
				HardOnlyFlameTank4.Destroy()
				HardOnlyFlameTank5.Destroy()
			end
		end
	end

	Utils.Do(SAMs, function(s)
		Trigger.OnKilled(s.SAMSite, function(self, killer)
			local defenders = Map.ActorsInCircle(s.Pos, WDist.New(4 * 1024));

			Utils.Do(defenders, function(a)
				if a.Owner == Nod and a.HasProperty("AttackMove") then
					a.AttackMove(s.Rally, 4)
					a.AttackMove(s.SAMSite.Location, 4)
					if IsHardOrAbove() then
						IdleHunt(a)
					end
				end
			end)

			SAMCount = #Nod.GetActorsByType("nsam")
			UpdateMissionText()
		end)
	end)

	Trigger.OnAllKilled({ SAM1, SAM2, SAM3, SAM4, SAM5, SAM6, SAM7 }, function()
		Greece.MarkCompletedObjective(ObjectiveDestroySAMSites)
		InitLongbows()
	end)

	Trigger.OnAnyKilled({ Transport1, Transport2 }, function()
		if not Greece.IsObjectiveCompleted(ObjectiveApprehendTransports) then
			Greece.MarkFailedObjective(ObjectiveApprehendTransports)
		end
	end)

	Trigger.AfterDelay(1, function()
		Utils.Do(Patrols, function(p)
			local patrollers = Map.ActorsInCircle(Map.CenterOfCell(p[1]), WDist.New(4 * 1024));

			Utils.Do(patrollers, function(a)
				if a.Owner == Nod and a.HasProperty("AttackMove") then
					a.Patrol(p)
				end
			end)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		local rangersDesc
		if IsHardOrAbove() then
			rangersDesc = "Rangers are"
		else
			rangersDesc = "Ranger is"
		end
		Tip("Your " .. rangersDesc .. " equipped with the Advanced Optics upgrade. Press [" .. UtilsCA.Hotkey("Deploy") .. "] (deploy) to activate for increased vision for a limited time.")
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Nod.Resources = Nod.ResourceCapacity - 500

		if Greece.HasNoRequiredUnits() then
			if not Greece.IsObjectiveCompleted(ObjectiveDestroySAMSites) then
				Greece.MarkFailedObjective(ObjectiveDestroySAMSites)
			end
			if not Greece.IsObjectiveCompleted(ObjectiveClearBase) then
				Greece.MarkFailedObjective(ObjectiveClearBase)
			end
			if not Greece.IsObjectiveCompleted(ObjectiveApprehendTransports) then
				Greece.MarkFailedObjective(ObjectiveApprehendTransports)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()

		if not Greece.IsObjectiveCompleted(ObjectiveClearBase) then
			local nodForces = Map.ActorsInBox(NodBaseTopLeft.CenterPosition, NodBaseBottomRight.CenterPosition, function(a)
				return a.Owner == Nod and not a.IsDead and a.HasProperty("Attack")
			end)

			if #nodForces == 0 then
				Greece.MarkCompletedObjective(ObjectiveClearBase)

				Trigger.AfterDelay(DateTime.Seconds(4), function()
					Transport1.GrantCondition("cloak-force-disabled")
					Transport2.GrantCondition("cloak-force-disabled")
					Trigger.AfterDelay(DateTime.Seconds(2), function()
						if not Greece.IsObjectiveCompleted(ObjectiveApprehendTransports) then
							if Transport1.IsDead or Transport2.IsDead then
								Greece.MarkFailedObjective(ObjectiveApprehendTransports)
							else
								Greece.MarkCompletedObjective(ObjectiveApprehendTransports)
							end
						end
					end)
				end)
			end
		end
	end
end

UpdateMissionText = function()
	if SAMCount > 0 then
		UserInterface.SetMissionText(SAMCount .. " SAM sites remaining.", HSLColor.Yellow)
	else
		UserInterface.SetMissionText("")
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

InitLongbows = function()
	Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
	Notification("Air support inbound.")
	local targets = { Obelisk3, Obelisk1, Obelisk2, Turret1, Turret2 }
	local delay = DateTime.Seconds(2)

	Utils.Do(targets, function(t)
		if t.IsDead then
			return
		end

		local unitList = { "heli", "heli" }
		Utils.Do(unitList, function(u)
			Trigger.AfterDelay(delay, function()
				local spawn = CPos.New(Utils.RandomInteger(35,85), 0)
				local rally = CPos.New(t.Location.X + Utils.RandomInteger(-8, 8), t.Location.Y - 8)
				local lb = Reinforcements.Reinforce(England, { u }, {spawn})[1]
				Trigger.AfterDelay(DateTime.Seconds(5), function()
					Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(spawn), WDist.New(2 * 1024), function(a, id)
						if lb == a then
							Trigger.RemoveProximityTrigger(id)
							lb.Stop()
							lb.Destroy()
						end
					end)
				end)
				lb.Move(rally)
				lb.Attack(t)
				lb.Move(spawn)
			end)
			delay = delay + 15
		end)
	end)
end
