
ExterminatorsStartTime = {
	easy = DateTime.Minutes(10),
	normal = DateTime.Minutes(8),
	hard = DateTime.Minutes(6),
}

ExterminatorsInterval = {
	easy = DateTime.Minutes(7),
	normal = DateTime.Minutes(5) + DateTime.Seconds(30),
	hard = DateTime.Minutes(4),
}

ExterminatorAttackCount = {
	easy = 4,
	normal = 4,
	hard = 5
}

ExterminatorPatrolPaths = {
	{ Exterminator1Patrol1.Location, Exterminator1Patrol2.Location, Exterminator1Patrol3.Location, Exterminator1Patrol4.Location },
	{ Exterminator2Patrol1.Location, Exterminator2Patrol2.Location, Exterminator2Patrol3.Location, Exterminator2Patrol4.Location },
	{ Exterminator3Patrol1.Location, Exterminator3Patrol2.Location, Exterminator3Patrol3.Location, Exterminator3Patrol4.Location },
	{ Exterminator4Patrol1.Location, Exterminator4Patrol2.Location, Exterminator4Patrol3.Location, Exterminator4Patrol4.Location },
	{ Exterminator5Patrol1.Location, Exterminator5Patrol2.Location, Exterminator5Patrol3.Location, Exterminator5Patrol4.Location },
}

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
			easy = { { MinTime = 0, Value = 20 }, { MinTime = DateTime.Minutes(16), Value = 50 } },
			normal = { { MinTime = 0, Value = 50 }, { MinTime = DateTime.Minutes(14), Value = 100 } },
			hard = { { MinTime = 0, Value = 80 }, { MinTime = DateTime.Minutes(12), Value = 160 } },
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false,
			Aircraft = false,
		},
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { Portal1 }, Vehicles = { WarpSphere1 }, Aircraft = { GravityStabilizer1, GravityStabilizer2 } },
		ProducerTypes = { Infantry = { "port" }, Vehicles = { "wsph" }, Aircraft = { "grav" } },
		Units = UnitCompositions.Scrin.Main,
		AttackPaths = {
			{ L1.Location, L2.Location, L3.Location, L4.Location },
			{ L1.Location, L2.Location, M3.Location, M4.Location },
			{ M1.Location, M2.Location, M3.Location, M4.Location },
			{ M1.Location, M2.Location, M5.Location, M4.Location },
		},
	},
	ScrinRebelKiller = {
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 20 } },
			normal = { { MinTime = 0, Value = 30 } },
			hard = { { MinTime = 0, Value = 40 }, { MinTime = DateTime.Minutes(30), Value = 60 } },
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false,
			Aircraft = false,
		},
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { Portal1 }, Vehicles = { WarpSphere1 }, Aircraft = { GravityStabilizer1, GravityStabilizer2 } },
		ProducerTypes = { Infantry = { "port" }, Vehicles = { "wsph" }, Aircraft = { "grav" } },
		Units = UnitCompositions.Scrin.Main,
		AttackPaths = {
			{ M1.Location, M2.Location, R4.Location, M5.Location },
			{ R1.Location, R2.Location, R3.Location, R4.Location, R5.Location }
		},
	},
	ScrinRebelsMain = {
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 35 } },
			normal = { { MinTime = 0, Value = 25 } },
			hard = { { MinTime = 0, Value = 15 }, { MinTime = DateTime.Minutes(20), Value = 35 } },
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false,
			Aircraft = false,
		},
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { RebelPortal1 }, Vehicles = { RebelWarpSphere1 }, Aircraft = { RebelGravityStabilizer1 } },
		ProducerTypes = { Infantry = { "port" }, Vehicles = { "wsph" }, Aircraft = { "grav" } },
		Units = UnitCompositions.Scrin.Main,
		AttackPaths = {
			{ M5.Location, M2.Location, M1.Location },
			{ M5.Location, R4.Location, M2.Location },
			{ R5.Location, R3.Location, R2.Location, R1.Location }
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
		},
	},
	ScrinRebelsAir = {
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
	NextExterminatorIndex = 1

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Nod)
	AdjustStartingCash()
	InitScrin()
	InitScrinRebels()

	ObjectiveDestroyOverlordForces = Nod.AddObjective("Destroy Scrin forces loyal to the Overlord.")
	ObjectiveDefendRebels = Nod.AddObjective("Protect Scrin rebel forces.")

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("The Overlord's tyranny will die today commander, and a new era will begin. Elsewhere, battles are still raging, but the decisive blow must be dealt here where his most elite forces are gathered. Show no mercy commander. Peace through power.", "Kane", HSLColor.FromHex("FF0000"))
		MediaCA.PlaySound("kane_nomercy.aud", 2)

		Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(14)), function()
			Media.DisplayMessage("Foolish humans! Your armies will be crushed, the rebellion will fall, and you will die here!", "Scrin Overlord", HSLColor.FromHex("7700FF"))
			MediaCA.PlaySound("overlordwarning.aud", 2)
		end)
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Scrin.Resources = Scrin.ResourceCapacity - 500
		ScrinRebels.Resources = ScrinRebels.ResourceCapacity - 500
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocation()

		if Scrin.HasNoRequiredUnits() and not Victory then
			Victory = true
			Media.DisplayMessage("The Overlord's fate is sealed, and the Scrin are liberated. Now we must return to Earth and forge a new beginning for mankind. With purified Tiberium the possibilites are truly limitless, and those who embrace its light will share in its blessings. Those who do not, will be left in the darkness.", "Kane", HSLColor.FromHex("FF0000"))
			MediaCA.PlaySound("kane_newbeginning.aud", 2)
			Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(19)), function()
				Nod.MarkCompletedObjective(ObjectiveDestroyOverlordForces)
				Nod.MarkCompletedObjective(ObjectiveDefendRebels)
			end)
		end

		if ScrinRebels.HasNoRequiredUnits() and not Victory then
			Nod.MarkFailedObjective(ObjectiveDefendRebels)
		end

		if Nod.HasNoRequiredUnits() and not Victory then
			Nod.MarkFailedObjective(ObjectiveDestroyOverlordForces)
		end
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
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit, function(p) return p == Nod or p == ScrinRebels end)
	end)

	if Difficulty == "hard" then
		Actor.Create("ioncon.upgrade", true, { Owner = Scrin })
		Actor.Create("shields.upgrade", true, { Owner = Scrin })

		Trigger.AfterDelay(DateTime.Minutes(15), function()
			Actor.Create("carapace.upgrade", true, { Owner = Scrin })
		end)
	end

	Trigger.AfterDelay(DateTime.Minutes(1), function()
		InitAttackSquad(Squads.ScrinRebelKiller, Scrin, ScrinRebels)
	end)

	Trigger.AfterDelay(Squads.ScrinMain.Delay[Difficulty], function()
		InitAttackSquad(Squads.ScrinMain, Scrin)
	end)

	Trigger.AfterDelay(Squads.ScrinAir.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.ScrinAir, Scrin, Nod, { "harv", "harv.td", "arty.nod", "mlrs", "obli", "wtnk", "gun.nod", "hq", "tmpl", "nuk2", "rmbc", "enli", "tplr" })
	end)

	Trigger.AfterDelay(1, function()
		local initialAttackers = Map.ActorsInBox(InitialAttackersTopLeft.CenterPosition, InitialAttackersBottomRight.CenterPosition, function(a)
			return a.Owner == Scrin and not a.IsDead
		end)

		Utils.Do(initialAttackers, function(a)
			Trigger.ClearAll(a)
			Trigger.AfterDelay(1, function()
				if not a.IsDead then
					a.AttackMove(Exterminator1Patrol3.Location, 2)
					a.Wait(Utils.RandomInteger(25, 75))
					a.Patrol({ R1.Location, L1.Location })
				end
			end)
		end)
	end)

	Trigger.AfterDelay(ExterminatorsStartTime[Difficulty], function()
		SendNextExterminator()
	end)

	Trigger.AfterDelay(RiftEnabledTime[Difficulty], function()
		if not RiftGenerator.IsDead then
			RiftGenerator.GrantCondition("rift-enabled")
		end
	end)

	Utils.Do({ Exterminator1, Exterminator2, Exterminator3, Exterminator4 }, function(a)
		if Difficulty ~= "hard" then
			a.GrantCondition("difficulty-" .. Difficulty)
		end
	end)

	if Difficulty == "easy" then
		Exterminator2.Destroy()
		Exterminator4.Destroy()
	end
end

InitScrinRebels = function()
	RebuildExcludes.ScrinRebels = { Actors = { FallenRebel1, FallenRebel2, FallenRebel3, FallenRebel4, FallenRebel5, FallenRebel6, FallenRebel7, FallenRebel8, FallenRebel9, FallenRebel10 } }

	AutoRepairAndRebuildBuildings(ScrinRebels, 15)
	AutoReplaceHarvesters(ScrinRebels)

	local scrinRebelGroundAttackers = ScrinRebels.GetGroundAttackers()

	Utils.Do(scrinRebelGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinRebelGroundHunterUnit, function(p) return p == Scrin end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1), function()
		InitAttackSquad(Squads.ScrinRebelsMain, ScrinRebels, Scrin)
	end)

	Trigger.AfterDelay(Squads.ScrinRebelsAir.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.ScrinRebelsAir, ScrinRebels, Scrin, { "harv.scrin", "scol", "tpod", "rptp", "ptur", "devo", "corr", "ruin" })
	end)
end

SendNextExterminator = function()
	if NextExterminatorIndex <= ExterminatorAttackCount[Difficulty] and not Victory then
		local wormhole = Actor.Create("wormhole", true, { Owner = Scrin, Location = ExterminatorSpawn.Location })

		Trigger.AfterDelay(DateTime.Seconds(2), function()
			MediaCA.PlaySound("exterminator-aggro.aud", 2)

			if NextExterminatorIndex == 1 then
				Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(2)), function()
					Media.DisplayMessage("Commander, the Overlord's most powerful weapons are being deployed. Use everything at your disposal to destroy them.", "Kane", HSLColor.FromHex("FF0000"))
					MediaCA.PlaySound("kane_exterminators.aud", 2)
				end)
			else
				Notification("Exterminator Tripod detected.")
			end

			local reinforcements = Reinforcements.Reinforce(Scrin, { "otpd" }, { ExterminatorSpawn.Location }, 10, function(a)
				a.Patrol(ExterminatorPatrolPaths[NextExterminatorIndex])
				IdleHunt(a)

				if Difficulty ~= "hard" then
					a.GrantCondition("difficulty-" .. Difficulty)
				end

				Trigger.AfterDelay(DateTime.Minutes(25), function()
					if not a.IsDead then
						a.Stop()
					end
				end)

				Trigger.OnDamaged(a, function(self, attacker, damage)
					if attacker == MissionPlayer then
						Trigger.ClearAll(a)
						a.Stop()
						Trigger.AfterDelay(1, function()
							IdleHunt(a)
						end)
					end
				end)

				Trigger.AfterDelay(DateTime.Seconds(10), function()
					wormhole.Kill()
				end)

				NextExterminatorIndex = NextExterminatorIndex + 1

				Trigger.AfterDelay(ExterminatorsInterval[Difficulty], function()
					SendNextExterminator()
				end)
			end)
		end)
	end
end

IsScrinRebelGroundHunterUnit = function(actor)
	return actor.Owner == ScrinRebels and IsGroundHunterUnit(actor) and actor.Type ~= "mast"
end
