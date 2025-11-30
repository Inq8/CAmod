MissionDir = "ca|missions/main-campaign/ca-prologue-01"

InsertionHelicopterType = "tran.evac"
InsertionPath = { InsertionEntry.Location, InsertionLZ.Location }
ExtractionHelicopterType = "tran.evac"
ExtractionPath = { SouthReinforcementsPoint.Location, ExtractionLZ.Location }
JeepReinforcements = { "jeep", "jeep" }
TanyaReinforcements = { "e7" }
EinsteinType = "einstein"
FlareType = "flare"
CruisersReinforcements = { "ca", "ca", "ca", "ca" }
OpeningAttack = { Patrol1, Patrol2, Patrol3, Patrol4 }
Responders = { Response1, Response2, Response3, Response4, Response5 }
LabGuardsTeam = { LabGuard1, LabGuard2, LabGuard3 }

SetupPlayers = function()
	Greece = Player.GetPlayer("Greece")
	England = Player.GetPlayer("England")
	USSR = Player.GetPlayer("USSR")
	Civilians = Player.GetPlayer("Civilians")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { Greece }
	MissionEnemies = { USSR }
end

WorldLoaded = function()
	SetupPlayers()
	InitObjectives(Greece)

	FindEinsteinObjective = Greece.AddObjective("Find Einstein.")
	TanyaSurviveObjective = Greece.AddObjective("Tanya must survive.")
	EinsteinSurviveObjective = Greece.AddObjective("Einstein must survive.")

	RunInitialActivities()

	Trigger.OnKilled(Lab, LabDestroyed)

	SovietArmy = USSR.GetGroundAttackers()

	Trigger.OnAllKilled(LabGuardsTeam, LabGuardsKilled)

	Trigger.AfterDelay(DateTime.Seconds(5), function() Actor.Create("camera", true, { Owner = Greece, Location = BaseCameraPoint.Location }) end)

	Camera.Position = InsertionLZ.CenterPosition

	Trigger.OnEnteredProximityTrigger(NorthEastTeslaCoil.CenterPosition, WDist.New(8 * 1024), function(a, id)
		if a.Owner == Civilians then
			local autoCamera = Actor.Create("smallcamera", true, { Owner = Greece, Location = a.Location })
			Trigger.AfterDelay(DateTime.Seconds(5), autoCamera.Destroy)
		end
	end)

	Trigger.OnKilled(SubPen, function(self, killer)
		if ObjectiveDestroySubPen == nil then
			ObjectiveDestroySubPen = Greece.AddObjective("Destroy the Soviet Sub Pen.")
		end

		if not Greece.IsObjectiveCompleted(ObjectiveDestroySubPen) then
			Greece.MarkCompletedObjective(ObjectiveDestroySubPen)
			if not Greece.IsObjectiveFailed(TanyaSurviveObjective) then
				Greece.MarkCompletedObjective(TanyaSurviveObjective)
			end
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(30), function()
		Tip("Information is displayed in the bottom right of the screen if any single unit or structure is selected, listing its strengths and weaknesses (as long as Selected Unit Tooltip is enabled in settings).")
	end)

	AfterWorldLoaded()
end

Tick = function()
	PanToCruisers()
	PanToPrisms()
	OncePerSecondChecks()
	AfterTick()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		USSR.Resources = USSR.ResourceCapacity - 500
	end
end

SendInsertionHelicopter = function()
	local passengers = Reinforcements.ReinforceWithTransport(Greece, InsertionHelicopterType,
		TanyaReinforcements, InsertionPath, { InsertionEntry.Location })[2]
	local tanya = passengers[1]
	Trigger.OnKilled(tanya, TanyaKilledInAction)
	Trigger.OnAddedToWorld(tanya, function(a)
		if not a.IsDead then
			a.Move(TanyaDest.Location)
		end
	end)
end

RunInitialActivities = function()
	SendInsertionHelicopter()

	Utils.Do(OpeningAttack, function(a)
		IdleHunt(a)
	end)

	Civilian1.Move(CivMove.Location)

	local powerPlants = USSR.GetActorsByType("powr")
	Trigger.OnAnyKilled(powerPlants, function()
		if not PowerDown then
			PowerDown = true
			local teslaCoils = USSR.GetActorsByType("tsla")
			Utils.Do(teslaCoils, function(self)
				if not self.IsDead then
					self.GrantCondition("disabled")
				end
			end)

			if not Civilian2.IsDead then
				Civilian2.Move(CivMove.Location)
			end
			Utils.Do(Responders, function(r)
				if not r.IsDead then
					IdleHunt(r)
				end
			end)
		end
	end)
end

LabGuardsKilled = function()
	CreateEinstein()

	Trigger.AfterDelay(DateTime.Seconds(4), function()
		Actor.Create(FlareType, true, { Owner = England, Location = ExtractionFlarePoint.Location })
		PlaySpeechNotificationToMissionPlayers("SignalFlare")
		SendExtractionHelicopter()
	end)

	Trigger.AfterDelay(DateTime.Seconds(14), function()
		Utils.Do(SovietArmy, function(a)
			if not a.IsDead and a.HasProperty("Hunt") then
				Trigger.OnIdle(a, a.Hunt)
			end
		end)
	end)
end

SendExtractionHelicopter = function()
	Heli = Reinforcements.ReinforceWithTransport(Greece, ExtractionHelicopterType, nil, ExtractionPath)[1]
	if not Einstein.IsDead then
		Trigger.OnRemovedFromWorld(Einstein, EvacuateHelicopter)
	end
	Trigger.OnKilled(Heli, RescueFailed)
	Trigger.OnRemovedFromWorld(Heli, HelicopterGone)
end

EvacuateHelicopter = function()
	if Heli.HasPassengers then
		Heli.Move(ExtractionExitPoint.Location)
		Heli.Destroy()
	end
end

SendCruisers = function()
	CruisersArrived = true

	Notification("Allied cruisers have arrived.")
	MediaCA.PlaySound(MissionDir .. "/r_alliedcruisers.aud", 2);
	Actor.Create("camera", true, { Owner = Greece, Location = CruiserCameraPoint.Location })
	Beacon.New(Greece, CruiserBeacon.CenterPosition)

	local i = 1

	Utils.Do(CruisersReinforcements, function(cruiser)
		local ca = Actor.Create(cruiser, true, { Owner = England, Location = Map.NamedActor("CruiserSpawn" .. i).Location })
		ca.Move(Map.NamedActor("CruiserPoint" .. i).Location)
		i = i + 1
	end)

	Trigger.AfterDelay(DateTime.Seconds(4), function()
		Media.DisplayMessage("Encountering Soviet naval presence! We're under heavy fire!", "Cruiser Captain", HSLColor.FromHex("99ACF2"))
		MediaCA.PlaySound(MissionDir .. "/encountering.aud", 2)
		Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(4)), function()
			Media.DisplayMessage("This is impossible! These waters were cleared!", "Cruiser Captain", HSLColor.FromHex("99ACF2"))
			MediaCA.PlaySound(MissionDir .. "/impossible.aud", 2)
			Trigger.AfterDelay(DateTime.Seconds(2), function()
				if not SubPen.IsDead and ObjectiveDestroySubPen == nil then
					ObjectiveDestroySubPen = Greece.AddObjective("Destroy the Soviet Sub Pen.")
					Beacon.New(Greece, SubPen.CenterPosition)
					Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(4)), function()
						PrismsArrived = true
						Trigger.AfterDelay(DateTime.Seconds(1), function()
							Lighting.Flash("Chronoshift", 10)
							Media.PlaySound(MissionDir .. "/chrono2.aud")
							Beacon.New(Greece, PrismBeacon.CenterPosition)
							Actor.Create("warpin", true, { Owner = Greece, Location = PrismBeacon.Location })
							Actor.Create("ptnk", true, { Owner = England, Location = PrismSpawn1.Location, Facing = Angle.East })
							Actor.Create("ptnk", true, { Owner = England, Location = PrismSpawn2.Location, Facing = Angle.East })
							Actor.Create("ptnk", true, { Owner = England, Location = PrismSpawn3.Location, Facing = Angle.East })
							Trigger.AfterDelay(DateTime.Seconds(2), function()
								Notification("Unidentified Allied units detected.")
								MediaCA.PlaySound(MissionDir .. "/r_unidentified.aud", 2)

								Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(3)), function()
									Media.DisplayMessage("Another temporal disturbance.. Well, we can work this out later. For now, we are at your disposal commander.", "Unknown", HSLColor.FromHex("99ACF2"))
									MediaCA.PlaySound(MissionDir .. "/disturbance.aud", 2)
									Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(5)), function()
										local prismTanks = England.GetActorsByType("ptnk")
										Utils.Do(prismTanks, function(a)
											if not a.IsDead then
												a.Owner = Greece
											end
										end)
									end)
								end)
							end)
						end)
					end)
				end
			end)
		end)
	end)
end

LabDestroyed = function()
	if not Einstein then
		RescueFailed()
	end
end

RescueFailed = function()
	PlaySpeechNotificationToMissionPlayers("ObjectiveNotMet")
	Greece.MarkFailedObjective(EinsteinSurviveObjective)
end

TanyaKilledInAction = function()
	PlaySpeechNotificationToMissionPlayers("ObjectiveNotMet")
	Greece.MarkFailedObjective(TanyaSurviveObjective)
end

CreateEinstein = function()
	Greece.MarkCompletedObjective(FindEinsteinObjective)
	Einstein = Actor.Create(EinsteinType, true, { Location = EinsteinSpawnPoint.Location, Owner = Greece })
	Einstein.Scatter()
	Trigger.OnKilled(Einstein, RescueFailed)
	ExtractObjective = Greece.AddObjective("Bring Einstein to the extraction point and board\nthe transport helicopter.")
	Trigger.AfterDelay(DateTime.Seconds(1), function() PlaySpeechNotificationToMissionPlayers("TargetFreed") end)
end

HelicopterGone = function()
	if not Heli.IsDead and #Heli.Passengers > 0 then
		PlaySpeechNotificationToMissionPlayers("TargetRescued")
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Greece.MarkCompletedObjective(ExtractObjective)
			Greece.MarkCompletedObjective(EinsteinSurviveObjective)
		end)

		Trigger.AfterDelay(DateTime.Seconds(3), function()
			SendCruisers()
		end)
	end
end

PanToCruisers = function()
	if PanToCruisersComplete or not CruisersArrived then
		return
	end

	local targetPos = CruiserBeacon.CenterPosition
	PanToPos(targetPos, 1024)

	if Camera.Position.X == targetPos.X and Camera.Position.Y == targetPos.Y then
		PanToCruisersComplete = true
	end
end

PanToPrisms = function()
	if PanToPrismsComplete or not PrismsArrived then
		return
	end

	local targetPos = PrismBeacon.CenterPosition
	PanToPos(targetPos, 1024)

	if Camera.Position.X == targetPos.X and Camera.Position.Y == targetPos.Y then
		PanToPrismsComplete = true
	end
end
