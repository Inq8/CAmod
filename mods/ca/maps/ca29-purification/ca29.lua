
LiquidTibCooldown = DateTime.Minutes(5)

RiftEnabledTime = {
	easy = DateTime.Seconds((60 * 45) + 17),
	normal = DateTime.Seconds((60 * 30) + 17),
	hard = DateTime.Seconds((60 * 15) + 17),
}

Squads = {
	ScrinMain = {
		Delay = {
			easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(2)
		},
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 20 }, { MinTime = DateTime.Minutes(14), Value = 50 } },
			normal = { { MinTime = 0, Value = 50 }, { MinTime = DateTime.Minutes(12), Value = 100 } },
			hard = { { MinTime = 0, Value = 80 }, { MinTime = DateTime.Minutes(10), Value = 160 } },
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false,
			Aircraft = false,
		},
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { Portal1, Portal2 }, Vehicles = { WarpSphere1, WarpSphere2 }, Aircraft = { GravityStabilizer1, GravityStabilizer2 } },
		ProducerTypes = { Infantry = { "port" }, Vehicles = { "wsph" }, Aircraft = { "grav" } },
		Units = UnitCompositions.Scrin.Main,
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
		Interval = {
			easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(2)
		},
		QueueProductionStatuses = {
			Aircraft = false
		},
		IdleUnits = { },
		ProducerActors = nil,
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
}

WorldLoaded = function()
    Nod = Player.GetPlayer("Nod")
    Scrin = Player.GetPlayer("Scrin")
    ScrinRebels = Player.GetPlayer("ScrinRebels")
	MissionPlayer = Nod
    ShipmentsComplete = 0
	TimerTicks = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Nod)
	AdjustStartingCash()
	InitScrin()
    UpdateMissionText()

    ObjectiveChargeDevice = Nod.AddObjective("Bring the device to full power.")
	ObjectiveProtectLiquidTib = Nod.AddObjective("Protect liquid Tiberium processing plant.")

    Trigger.OnKilled(LiquidTibFacility, function(self, killer)
        if not Nod.IsObjectiveCompleted(ObjectiveProtectLiquidTib) then
            Nod.MarkFailedObjective(ObjectiveProtectLiquidTib)
        end
    end)

    Trigger.OnAnyProduction(function(producer, produced, productionType)
        if produced.Owner == Nod and produced.Type == "liquidtib" then
            LiquidTibProduced()
        end
    end)

    Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(2)), function()
        Media.DisplayMessage("Commander, we must bring the device to full power as quickly as possible. Transporting crystals will take too long, so liquid Tiberium is our only option. Use your refined reserves to fill tankers with liquid T, then bring them to the entrance to the cave system", "Kane", HSLColor.FromHex("FF0000"))
        MediaCA.PlaySound("kane_liquidt.aud", 2)
        Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(14)), function()
            Tip("Liquid Tiberium can be processed via the Upgrades tab. Move a tanker next to the processing plant to pick up a shipment, then take it to the cave entrance in the north-east.")
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
                Notification("Liquid Tiberium delivered.")
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
			end
			UpdateMissionText()
		end

        if LiquidTibFacility.AmmoCount("primary") > 0 then
            local nearbyTrucks = Map.ActorsInBox(LiquidTibPickup1.CenterPosition, LiquidTibPickup2.CenterPosition, function(a)
                return a.Owner == Nod and not a.IsDead and a.Type == "ttrk"
            end)

            Utils.Do(nearbyTrucks, function(t)
                if t.AmmoCount("primary") == 0 then
                    t.Reload("primary", 1)
                    LiquidTibFacility.Reload("primary", -1)
                    Notification("Liquid Tiberium transfer complete.")
                    Beacon.New(Nod, t.CenterPosition)
                    return
                end
            end)
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
		UpdatePlayerBaseLocation()
	end
end

InitScrin = function()
	RebuildExcludes.Scrin = { Types = { "rfgn" } }

	AutoRepairAndRebuildBuildings(Scrin, 15)
	SetupRefAndSilosCaptureCredits(Scrin)
	AutoReplaceHarvesters(Scrin)

	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)

	if Difficulty == "hard" then
        IonConduits = Actor.Create("ioncon.upgrade", true, { Owner = Scrin })

		Trigger.AfterDelay(DateTime.Minutes(15), function()
			Actor.Create("carapace.upgrade", true, { Owner = Scrin })
		end)
	end

    BeginScrinAttacks()

    Trigger.AfterDelay(RiftEnabledTime[Difficulty], function()
		if not RiftGenerator.IsDead then
			RiftGenerator.GrantCondition("rift-enabled")
		end
	end)
end

BeginScrinAttacks = function()
	Trigger.AfterDelay(Squads.ScrinMain.Delay[Difficulty], function()
		InitAttackSquad(Squads.ScrinMain, Scrin)
	end)

	Trigger.AfterDelay(Squads.ScrinAir.Delay[Difficulty], function()
        InitAirAttackSquad(Squads.ScrinAir, Scrin, Nod, { "harv", "harv.td", "arty.nod", "mlrs", "obli", "atwr", "gtwr", "gun.nod", "hq", "nuk2", "rmbc", "enli", "tplr" })
	end)
end

UpdateMissionText = function()
    local shipmentsText = "Shipments complete: " .. ShipmentsComplete .. "/5"
    local cooldownText

    if TimerTicks > 0 then
        cooldownText = " -- Plant status: Ready in " .. Utils.FormatTime(TimerTicks)
    else
        cooldownText = " -- Plant status: Ready"
    end

    UserInterface.SetMissionText(shipmentsText .. cooldownText, HSLColor.Yellow)
end

LiquidTibProduced = function()
    if not CooldownTip then
        CooldownTip = true
        Tip("The plant requires time to prepare before each batch of liquid Tiberium is produced.")
    end

    TimerTicks = LiquidTibCooldown
    LiquidTibFacility.GrantCondition("prepping", LiquidTibCooldown)
    LiquidTibFacility.Reload("primary", 1)
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
    local scrinToPurify = Utils.Where(Scrin.GetGroundAttackers(), IsScrinGroundHunterUnit)

    Utils.Do(scrinToPurify, function(a)
        Purify(a)
	end)

    local scrinAirToPurify = Scrin.GetActorsByTypes({ "pac", "deva" })

    Utils.Do(scrinAirToPurify, function(a)
        Purify(a)
    end)
end

Purify = function(a)
    Trigger.ClearAll(a)
    local random = Utils.RandomInteger(1,100)
    if random > 30 then
        a.Owner = ScrinRebels
    end
    Trigger.AfterDelay(1, function()
        IdleHunt(a)
    end)
end
