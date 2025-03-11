
LiquidTibCooldown = DateTime.Minutes(5)

RiftEnabledTime = {
	easy = DateTime.Seconds((60 * 45) + 17),
	normal = DateTime.Seconds((60 * 30) + 17),
	hard = DateTime.Seconds((60 * 15) + 17),
}

ScrinReinforcementSpawns = {
	ScrinReinforcementsSpawn1, ScrinReinforcementsSpawn2, ScrinReinforcementsSpawn3, ScrinReinforcementsSpawn4, ScrinReinforcementsSpawn5, ScrinReinforcementsSpawn6, ScrinReinforcementsSpawn7, ScrinReinforcementsSpawn8
}

AdjustedScrinCompositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin)

Squads = {
	ScrinMain = {
		Delay = {
			easy = DateTime.Seconds(240),
			normal = DateTime.Seconds(150),
			hard = DateTime.Seconds(60)
		},
		AttackValuePerSecond = {
			easy = { Min = 20, Max = 50, RampDuration = DateTime.Minutes(12) },
			normal = { Min = 50, Max = 100, RampDuration = DateTime.Minutes(10) },
			hard = { Min = 80, Max = 160, RampDuration = DateTime.Minutes(8) },
		},
		FollowLeader = true,
		ProducerActors = { Infantry = { Portal1, Portal2 }, Vehicles = { WarpSphere1, WarpSphere2 }, Aircraft = { GravityStabilizer1, GravityStabilizer2 } },
		ProducerTypes = { Infantry = { "port", "wormhole" }, Vehicles = { "wsph", "wormhole" }, Aircraft = { "grav", "hiddenspawner" } },
		Units = AdjustedScrinCompositions,
		AttackPaths = {
			{ ScrinAttack1a.Location, ScrinAttack1b.Location, ScrinAttack1c.Location, ScrinAttack1d.Location },
			{ ScrinAttack1a.Location, ScrinAttack2.Location },
			{ ScrinAttack3a.Location, ScrinAttack3b.Location, ScrinAttack3c.Location },
			{ ScrinAttack3a.Location, ScrinAttack4a.Location, ScrinAttack4b.Location, ScrinAttack3c.Location },
		},
	},
	ScrinAir = {
		Delay = {
			easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(5),
			hard = DateTime.Minutes(4)
		},
		AttackValuePerSecond = {
			easy = { Min = 7, Max = 7 },
			normal = { Min = 14, Max = 14 },
			hard = { Min = 21, Max = 21 },
		},
		ProducerTypes = { Aircraft = { "grav" } },
		Units = {
			easy = {
				{ Aircraft = { "stmr" } }
			},
			normal = {
				{ Aircraft = { "stmr", "stmr" } },
				{ Aircraft = { "enrv" } },
			},
			hard = {
				{ Aircraft = { "stmr", "stmr", "stmr" } },
				{ Aircraft = { "enrv", "enrv" } },
			}
		}
	},
	ScrinRebelsMain = {
		AttackValuePerSecond = {
			easy = { Min = 70, Max = 70 },
			normal = { Min = 70, Max = 70 },
			hard = { Min = 70, Max = 70 },
		},
		FollowLeader = true,
		ProducerTypes = { Infantry = { "wormhole" }, Vehicles = { "wormhole" }, Aircraft = { "grav" } },
		Units = AdjustedScrinCompositions,
		AttackPaths = {
			{ ScrinBaseCenter.Location },
		},
	},
}

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	Scrin = Player.GetPlayer("Scrin")
	ScrinRebels = Player.GetPlayer("ScrinRebels")
	MissionPlayers = { Nod }
	ShipmentsComplete = 0
	TimerTicks = DateTime.Seconds(60)

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Nod)
	AdjustStartingCash()
	InitScrin()

	ObjectiveChargeDevice = Nod.AddObjective("Bring the device to full power.")
	ObjectiveProtectLiquidTib = Nod.AddObjective("Protect liquid Tiberium processing plant.")

	UpdateMissionText()

	if Difficulty == "easy" then
		NormalHardOnlyCarrier1.Destroy()
		NormalHardOnlyCarrier2.Destroy()
	end

	if Difficulty ~= "hard" then
		HardOnlyCarrier1.Destroy()
	end

	Trigger.OnKilled(LiquidTibFacility, function(self, killer)
		if not Nod.IsObjectiveCompleted(ObjectiveProtectLiquidTib) then
			Nod.MarkFailedObjective(ObjectiveProtectLiquidTib)
		end
	end)

	Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(2)), function()
		Media.DisplayMessage("Commander, we must bring the device to full power as quickly as possible. Transporting crystals will take too long, so liquid Tiberium is our only option. We have set up a liquid T production facility. Do not let it be destroyed, and as each shipment becomes available load it into a tanker and bring it to the entrance of the cave system.", "Kane", HSLColor.FromHex("FF0000"))
		MediaCA.PlaySound("kane_liquidt.aud", 2)
		Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(18)), function()
			Tip("Move a tanker next to the processing plant to pick up a prepared shipment, then take it to the cave entrance in the north-east.")
			Utils.Do({ InitAttacker1, InitAttacker2, InitAttacker3, InitAttacker4 }, function(a)
				if not a.IsDead then
					a.AttackMove(PlayerStart.Location)
				end
			end)
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				Utils.Do({ InitAttacker5, InitAttacker6, InitAttacker7 }, function(a)
					if not a.IsDead then
						a.AttackMove(PlayerStart.Location)
					end
				end)
			end)
		end)
	end)

	Trigger.OnEnteredFootprint({ LiquidTibPickup1.Location, LiquidTibPickup2.Location }, function(a)
		if a.Owner == Nod and not a.IsDead and a.Type == "ttrk" then
			if LiquidTibFacility.AmmoCount("primary") == 0 then
				Notification("No liquid Tiberium currently available for pickup.")
			end
		end
	end)

	Trigger.OnEnteredFootprint({ CaveEntrance.Location, LiquidTibDropOff1.Location, LiquidTibDropOff2.Location, LiquidTibDropOff3.Location }, function(a)
		if a.Owner == Nod and not a.IsDead and a.Type == "ttrk" then
			if a.AmmoCount("primary") == 1 then
				a.Reload("primary", -1)
				ShipmentsComplete = ShipmentsComplete + 1
				Notification("Liquid Tiberium shipment delivered.")
				MediaCA.PlaySound("n_liquidtibdelivered.aud", 2)
				UpdateMissionText()
				if ShipmentsComplete == 5 then
					PurificationWave()
					Nod.MarkCompletedObjective(ObjectiveChargeDevice)
					Nod.MarkCompletedObjective(ObjectiveProtectLiquidTib)
				end
			else
				Notification("No liquid Tiberium to drop off.")
			end
		end
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Scrin.Resources = Scrin.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
				LiquidTibProduced()
			end
			UpdateMissionText()
		end

		if LiquidTibFacility.AmmoCount("primary") > 0 then
			local nearbyTrucks = Map.ActorsInBox(LiquidTibPickup1.CenterPosition, LiquidTibPickup2.CenterPosition, function(a)
				return a.Owner == Nod and not a.IsDead and a.Type == "ttrk"
			end)

			Utils.Do(nearbyTrucks, function(t)
				if t.AmmoCount("primary") == 0 and not TibLoaded then
					TibLoaded = true
					t.Reload("primary", 1)
					LiquidTibFacility.Reload("primary", -1)
					Notification("Liquid Tiberium transfer complete.")
					Beacon.New(Nod, t.CenterPosition)
				end
			end)

			TibLoaded = false
		end

		if ObjectiveDestroyRemainingLoyalists ~= nil then
			if Scrin.HasNoRequiredUnits() then
				Nod.MarkCompletedObjective(ObjectiveDestroyRemainingLoyalists)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()

		if not ScrinReinforcementsEnabled then
			local scrinProducerActors = Scrin.GetActorsByTypes({ "sfac", "wsph", "port" })

			if #scrinProducerActors == 0 then
				InitScrinReinforcements()
			end
		end
	end
end

InitScrin = function()
	RebuildExcludes.Scrin = { Types = { "rfgn" } }

	AutoRepairAndRebuildBuildings(Scrin, 15)
	SetupRefAndSilosCaptureCredits(Scrin)
	AutoReplaceHarvesters(Scrin)
	InitAiUpgrades(Scrin)

	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)

	BeginScrinAttacks()

	Trigger.AfterDelay(RiftEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = Scrin })
	end)
end

BeginScrinAttacks = function()
	Trigger.AfterDelay(Squads.ScrinMain.Delay[Difficulty], function()
		InitAttackSquad(Squads.ScrinMain, Scrin)
	end)

	Trigger.AfterDelay(Squads.ScrinAir.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.ScrinAir, Scrin, Nod, { "harv", "harv.td", "arty.nod", "mlrs", "obli", "gun.nod", "wtnk", "hq", "tmpl", "nuk2", "rmbc", "enli", "tplr" })
	end)
end

UpdateMissionText = function()
	if Nod.IsObjectiveCompleted(ObjectiveChargeDevice) then
		UserInterface.SetMissionText("")
		return
	end

	local shipmentsText = "Shipments complete: " .. ShipmentsComplete .. "/5"
	local cooldownText = " -- Next shipment ready in " .. UtilsCA.FormatTimeForGameSpeed(TimerTicks)
	UserInterface.SetMissionText(shipmentsText .. cooldownText, HSLColor.Yellow)
end

LiquidTibProduced = function()
	if Nod.IsObjectiveCompleted(ObjectiveChargeDevice) then
		return
	end

	TimerTicks = LiquidTibCooldown
	Notification("Liquid Tiberium shipment ready.")
	MediaCA.PlaySound("n_liquidtibready.aud", 2)

	if not LiquidTibFacility.IsDead then
		LiquidTibFacility.Reload("primary", 1)
		Beacon.New(Nod, LiquidTibFacility.CenterPosition)
	end
end

PurificationWave = function()
	ObjectivePurify = Nod.AddObjective("Await the purification wave.")

	Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(2)), function()
		Media.DisplayMessage("Well done commander! The device is at full power, and will soon release its purifying energy. The question is, will the Scrin fight for their freedom against the Overlord, or cower in servitude even after such heinous treachery is revealed?", "Kane", HSLColor.FromHex("FF0000"))
		MediaCA.PlaySound("kane_purification.aud", 2)

		Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(14)), function()
			MediaCA.PlaySound("purification.aud", 2)
			Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(9)), function()
				PurificationComplete = true
				Lighting.Flash("Purification", AdjustTimeForGameSpeed(10))
				ObjectiveDestroyRemainingLoyalists = Nod.AddObjective("Eliminate any hostile Scrin remaining.")
				Nod.MarkCompletedObjective(ObjectivePurify)
				PurifyScrin()
				InitScrinReinforcements()
				InitRebelReinforcements()
				if not IslandGrav1.IsDead then
					IslandGrav1.Kill()
				end
				if not IslandGrav2.IsDead then
					IslandGrav2.Kill()
				end
				Trigger.AfterDelay(1, function()
					local wormholes = Scrin.GetActorsByType("wormhole")
					Utils.Do(wormholes, function(w)
						if not w.IsDead then
							w.GrantCondition("regen-disabled")
						end
					end)
				end)
				Trigger.AfterDelay(AdjustTimeForGameSpeed(4), function()
					Lighting.Flash("Purification", AdjustTimeForGameSpeed(10))
					Trigger.AfterDelay(AdjustTimeForGameSpeed(4), function()
						Lighting.Flash("Purification", AdjustTimeForGameSpeed(10))
						Trigger.AfterDelay(AdjustTimeForGameSpeed(4), function()
							Lighting.Flash("Purification", AdjustTimeForGameSpeed(10))
						end)
					end)
				end)
			end)
		end)
	end)
end

PurifyScrin = function()
	local scrinGroundUnits = Utils.Shuffle(Utils.Where(Scrin.GetGroundAttackers(), IsScrinGroundHunterUnit))
	local count = 1

	Utils.Do(scrinGroundUnits, function(a)
		Purify(a, count)
		count = count + 1
		if count > 10 then
			count = 1
		end
	end)

	local scrinAirUnits = Scrin.GetActorsByTypes({ "pac", "deva" })
	count = 1

	Utils.Do(scrinAirUnits, function(a)
		Purify(a, count)
		count = count + 1
		if count > 10 then
			count = 1
		end
	end)
end

Purify = function(a, count)
	Trigger.ClearAll(a)
	local shouldConvert = false

	if Difficulty == "easy" and count % 3 > 0 then
		shouldConvert = true
	elseif count % 2 == 0 then
		shouldConvert = true
	end

	if shouldConvert then
		a.Owner = ScrinRebels
	end
	Trigger.AfterDelay(1, function()
		IdleHunt(a)
	end)
end

InitScrinReinforcements = function()
	if not ScrinReinforcementsEnabled then
		ScrinReinforcementsEnabled = true
		Utils.Do(ScrinReinforcementSpawns, function(s)
			SpawnWormhole(s.Location)
		end)
		Utils.Do({ ScrinHiddenSpawn1.Location, ScrinHiddenSpawn2.Location, ScrinHiddenSpawn3.Location, ScrinHiddenSpawn4.Location, ScrinHiddenSpawn5.Location, ScrinHiddenSpawn6.Location }, function(loc)
			Actor.Create("hiddenspawner", true, { Owner = Scrin, Location = loc })
		end)
	end
end

SpawnWormhole = function(loc)
	local wormhole = Actor.Create("wormhole", true, { Owner = Scrin, Location = loc })
	Trigger.OnKilled(wormhole, function(self, killer)
		Trigger.AfterDelay(DateTime.Minutes(1), function()
			if not Nod.IsObjectiveCompleted(ObjectiveChargeDevice) then
				SpawnWormhole(loc)
			end
		end)
	end)
end

InitRebelReinforcements = function()
	local rebelSpawns = { RebelSpawnPoint1, RebelSpawnPoint2 }

	Utils.Do(rebelSpawns, function(s)
		local wormhole = Actor.Create("wormhole", true, { Owner = ScrinRebels, Location = s.Location })
		local units = Reinforcements.Reinforce(ScrinRebels, { "s1", "s3", "intl.ai2", "devo", "s4", "s1", "tpod", "gscr", "intl", "s1", "s1" }, { wormhole.Location }, 10)

		Utils.Do(units, function(a)
			a.AttackMove(ScrinBaseCenter.Location)
			IdleHunt(a)
		end)
	end)

	InitAttackSquad(Squads.ScrinRebelsMain, ScrinRebels, Scrin)
end
