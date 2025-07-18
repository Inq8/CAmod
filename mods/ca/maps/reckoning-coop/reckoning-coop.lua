
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
	hard = 50
}

Exterminators = {
	{ SpawnLocation = ExterminatorFirstSpawn.Location, Path = { Exterminator1Patrol1.Location, Exterminator1Patrol2.Location, Exterminator1Patrol3.Location, Exterminator1Patrol4.Location } },
	{ SpawnLocation = ExterminatorSpawnWest.Location, Path = { Exterminator2Patrol1.Location, Exterminator2Patrol2.Location, Exterminator2Patrol3.Location, Exterminator2Patrol4.Location } },
	{ SpawnLocation = ExterminatorSpawnWest.Location, Path = { Exterminator3Patrol1.Location, Exterminator3Patrol2.Location, Exterminator3Patrol3.Location, Exterminator3Patrol4.Location } },
}

SuperweaponsEnabledTime = {
	easy = DateTime.Seconds((60 * 50) + 17),
	normal = DateTime.Seconds((60 * 35) + 17),
	hard = DateTime.Seconds((60 * 20) + 17),
}

if Difficulty == "hard" then
	table.insert(UnitCompositions.Scrin, {
		Infantry = { "s3", "s4", "evis", "evis", "evis", "evis", "s1", "s1", "s4", "s1", "s4", "s1", "s4", "s1", "mast" },
		Vehicles = { "shrw", TripodVariant, TripodVariant, "shrw", CorrupterOrDevourer, "oblt", "shrw" },
		Aircraft = { PacOrDevastator, "pac" },
		MinTime = DateTime.Minutes(22)
	})
end

AdjustedScrinCompositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin)

Squads = {
	ScrinVsNod = {
		ActiveCondition = function()
			return not IsNodDead
		end,
		Delay = {
			easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(2)
		},
		AttackValuePerSecond = {
			easy = { Min = 20, Max = 50 },
			normal = { Min = 50, Max = 100 },
			hard = { Min = 80, Max = 160 },
		},
		FollowLeader = true,
		ProducerActors = { Infantry = { Portal1 }, Vehicles = { WarpSphere1 }, Aircraft = { GravityStabilizer1, GravityStabilizer2 } },
		ProducerTypes = { Infantry = { "port" }, Vehicles = { "wsph" }, Aircraft = { "grav" } },
		Units = AdjustedScrinCompositions,
		AttackPaths = {
			{ L1.Location, L2.Location, L3.Location, L4.Location },
			{ L1.Location, L2.Location, M3.Location, M4.Location },
			{ M1.Location, M2.Location, M3.Location, M4.Location },
			{ M1.Location, M2.Location, M5.Location, M4.Location },
		},
	},
	ScrinVsGDI = {
		ActiveCondition = function()
			return not IsGDIDead
		end,
		Delay = {
			easy = DateTime.Minutes(6),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(2)
		},
		AttackValuePerSecond = {
			easy = { Min = 20, Max = 50 },
			normal = { Min = 50, Max = 100 },
			hard = { Min = 80, Max = 160 },
		},
		FollowLeader = true,
		ProducerActors = { Infantry = { Portal3 }, Vehicles = { WarpSphere4 }, Aircraft = { GravityStabilizer3 } },
		ProducerTypes = { Infantry = { "port" }, Vehicles = { "wsph" }, Aircraft = { "grav" } },
		Units = AdjustedScrinCompositions,
		AttackPaths = {
			{ R7.Location, R6.Location, GDIBase.Location },
			{ R10.Location, R6.Location, GDIBase.Location },
		},
	},
	ScrinRebelKiller = {
		AttackValuePerSecond = {
			easy = { Min = 20, Max = 20 },
			normal = { Min = 40, Max = 40 },
			hard = { Min = 60, Max = 80 },
		},
		FollowLeader = true,
		ProducerActors = { Infantry = { Portal2 }, Vehicles = { WarpSphere2 }, Aircraft = { GravityStabilizer1, GravityStabilizer2, GravityStabilizer3 } },
		ProducerTypes = { Infantry = { "port" }, Vehicles = { "wsph" }, Aircraft = { "grav" } },
		Units = AdjustedScrinCompositions,
		AttackPaths = {
			{ M1.Location, M2.Location, R4.Location, M5.Location },
			{ R1.Location, R2.Location, R3.Location, R4.Location, R5.Location }
		},
	},
	ScrinRebelsMain = {
		AttackValuePerSecond = {
			easy = { Min = 35, Max = 35 },
			normal = { Min = 35, Max = 35 },
			hard = { Min = 35, Max = 55, RampDuration = DateTime.Minutes(22) },
		},
		FollowLeader = true,
		ProducerActors = { Infantry = { RebelPortal1 }, Vehicles = { RebelWarpSphere1 }, Aircraft = { RebelGravityStabilizer1 } },
		ProducerTypes = { Infantry = { "port" }, Vehicles = { "wsph" }, Aircraft = { "grav" } },
		Units = AdjustedScrinCompositions,
		AttackPaths = {
			{ M5.Location, M2.Location, M1.Location },
			{ M5.Location, R4.Location, M2.Location },
			{ R5.Location, R3.Location, R2.Location, R1.Location }
		},
	},
	ScrinAirVsNod = {
		ActiveCondition = function()
			return not IsNodDead
		end,
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
		ProducerActors = { Aircraft = { GravityStabilizer1, GravityStabilizer2 } },
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
	ScrinAirVsGDI = {
		ActiveCondition = function()
			return not IsGDIDead
		end,
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
		ProducerActors = { Aircraft = { GravityStabilizer3, GravityStabilizer4 } },
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
	ScrinAirToAirVsNod = {
		Interval = {
			hard = DateTime.Seconds(90)
		},
		ActiveCondition = function()
			return NodHasMassAir()
		end,
		OnProducedAction = function(a)
			a.Patrol({ A2APatrol1.Location, A2APatrol2.Location, A2APatrol3.Location, A2APatrol4.Location, A2APatrol5.Location, A2APatrol6.Location, A2APatrol7.Location, A2APatrol8.Location })
		end,
		ProducerActors = { Aircraft = { GravityStabilizer1, GravityStabilizer2 } },
		ProducerTypes = { Aircraft = { "grav" } },
		Units = {
			hard = {
				{ Aircraft = { { "stmr" , "enrv" }, { "stmr" , "enrv" }, { "stmr" , "enrv" }, { "stmr" , "enrv" }, { "stmr" , "enrv" }, { "stmr" , "enrv" }, { "stmr" , "enrv" }, { "stmr" , "enrv" } } },
			}
		},
	},
	ScrinAirToAirVsGDI = {
		Interval = {
			hard = DateTime.Seconds(90)
		},
		ActiveCondition = function()
			return GDIHasMassAir()
		end,
		OnProducedAction = function(a)
			a.Patrol({ A2ABPatrol1.Location, A2ABPatrol2.Location, A2ABPatrol3.Location, A2ABPatrol4.Location, A2ABPatrol5.Location })
		end,
		ProducerActors = { Aircraft = { GravityStabilizer3, GravityStabilizer4 } },
		ProducerTypes = { Aircraft = { "grav" } },
		Units = {
			hard = {
				{ Aircraft = { { "stmr" , "enrv" }, { "stmr" , "enrv" }, { "stmr" , "enrv" }, { "stmr" , "enrv" }, { "stmr" , "enrv" }, { "stmr" , "enrv" }, { "stmr" , "enrv" }, { "stmr" , "enrv" } } },
			}
		},
	},
	ScrinRebelsAir = {
		Delay = {
			easy = DateTime.Minutes(5),
			normal = DateTime.Minutes(5),
			hard = DateTime.Minutes(5)
		},
		AttackValuePerSecond = {
			easy = { Min = 14, Max = 14 },
			normal = { Min = 14, Max = 14 },
			hard = { Min = 14, Max = 14 },
		},
		ProducerTypes = { Aircraft = { "grav" } },
		Units = {
			easy = {
				{ Aircraft = { "stmr", "stmr" } },
				{ Aircraft = { "enrv" } },
			},
			normal = {
				{ Aircraft = { "stmr", "stmr" } },
				{ Aircraft = { "enrv" } },
			},
			hard = {
				{ Aircraft = { "stmr", "stmr" } },
				{ Aircraft = { "enrv" } },
			}
		}
	}
}

Objectives = {
	DestroyOverlordForces = {
		Text = "Destroy Scrin forces loyal to the Overlord.",
	},
	DefendRebels = {
		Text = "Protect Scrin rebel forces.",
	},
}

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	Scrin = Player.GetPlayer("Scrin")
	ScrinRebels = Player.GetPlayer("ScrinRebels")
	GDI = Player.GetPlayer("GDI")
	MissionPlayers = { Nod, GDI }
	NextExterminatorIndex = 1

	if Nod.IsLocalPlayer then
		Camera.Position = PlayerStartNod.CenterPosition
	else
		Camera.Position = PlayerStartGDI.CenterPosition
	end

	InitObjectives(Nod)
	InitObjectives(GDI)
	AdjustPlayerStartingCashForDifficulty()
	InitScrin()
	InitScrinRebels()

	Utils.Do(Objectives, function(o)
		Utils.Do(MissionPlayers, function(p)
			o[p.InternalName] = p.AddObjective(o.Text)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("The Overlord's tyranny will die today commander, and a new era will begin. Elsewhere, battles are still raging, but the decisive blow must be dealt here where his most elite forces are gathered. Show no mercy commander. Peace through power.", "Kane", HSLColor.FromHex("FF0000"))
		MediaCA.PlaySound("../ca36-reckoning/kane_nomercy.aud", 2)

		Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(14)), function()
			Media.DisplayMessage("Foolish humans! Your armies will be crushed, the rebellion will fall, and you will die here!", "Scrin Overlord", HSLColor.FromHex("7700FF"))
			MediaCA.PlaySound("../ca36-reckoning/overlordwarning.aud", 2)
		end)
	end)

	NodRadarProvider = Actor.Create("radar.dummy", true, { Owner = Nod })
	GDIRadarProvider = Actor.Create("radar.dummy", true, { Owner = GDI })

	Trigger.OnKilled(RebelMainNerveCenter, function(self, killer)
		NodRadarProvider.Destroy()
		GDIRadarProvider.Destroy()
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	OncePerThirtySecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Scrin.Resources = Scrin.ResourceCapacity - 500
		ScrinRebels.Resources = ScrinRebels.ResourceCapacity - 500
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()

		if not PlayerHasBuildings(Scrin) and #Scrin.GetActorsByType("etpd") == 0 and not Victory then
			Victory = true
			Media.DisplayMessage("The Overlord's fate is sealed, and the Scrin are liberated. Now we must return to Earth and forge a new beginning for mankind. With purified Tiberium the possibilites are truly limitless, and those who embrace its light will share in its blessings. Those who do not, will be left in the darkness.", "Kane", HSLColor.FromHex("FF0000"))
			MediaCA.PlaySound("../ca36-reckoning/kane_newbeginning.aud", 2)

			Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(19)), function()
				Utils.Do(MissionPlayers, function(p)
					p.MarkCompletedObjective(Objectives.DestroyOverlordForces[p.InternalName])
					p.MarkCompletedObjective(Objectives.DefendRebels[p.InternalName])
				end)
			end)
		end

		if not PlayerHasBuildings(ScrinRebels) and not Victory then
			Utils.Do(MissionPlayers, function(p)
				p.MarkFailedObjective(Objectives.DefendRebels[p.InternalName])
			end)
		end

		if Nod.HasNoRequiredUnits() and not Victory then
			IsNodDead = true
			Nod.MarkFailedObjective(Objectives.DestroyOverlordForces[Nod.InternalName])
		end

		if GDI.HasNoRequiredUnits() and not Victory then
			IsGDIDead = true
			GDI.MarkFailedObjective(Objectives.DestroyOverlordForces[GDI.InternalName])
		end
	end
end

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % DateTime.Seconds(30) == 0 then
		CalculatePlayerCharacteristics()
	end
end

InitScrin = function()
	RebuildExcludes.Scrin = { Types = { "rfgn" } }

	AutoRepairAndRebuildBuildings(Scrin, 15)
	SetupRefAndSilosCaptureCredits(Scrin)
	AutoReplaceHarvesters(Scrin)
	AutoRebuildConyards(Scrin)
	InitAiUpgrades(Scrin)

	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnitExcludingExterminators, function(p) return p == Nod or p == ScrinRebels or p == GDI end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1), function()
		InitAttackSquad(Squads.ScrinRebelKiller, Scrin, ScrinRebels)
	end)

	Trigger.AfterDelay(Squads.ScrinVsNod.Delay[Difficulty], function()
		InitAttackSquad(Squads.ScrinVsNod, Scrin, Nod)
	end)

	Trigger.AfterDelay(Squads.ScrinVsGDI.Delay[Difficulty], function()
		InitAttackSquad(Squads.ScrinVsGDI, Scrin, GDI)
	end)

	Trigger.AfterDelay(Squads.ScrinAirVsNod.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.ScrinAirVsNod, Scrin, Nod)
	end)

	Trigger.AfterDelay(Squads.ScrinAirVsGDI.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.ScrinAirVsGDI, Scrin, GDI)
	end)

	if Difficulty == "hard" then
		InitAirAttackSquad(Squads.ScrinAirToAirVsNod, Scrin, Nod, { "scrn", "apch", "venm" })
		InitAirAttackSquad(Squads.ScrinAirToAirVsGDI, Scrin, GDI, { "orca", "orcb", "a10", "a10.gau", "a10.sw", "auro" })
	end

	Trigger.AfterDelay(ExterminatorsStartTime[Difficulty], function()
		SendNextExterminator()
	end)

	Trigger.OnEnteredProximityTrigger(FirstExterminatorDetector.CenterPosition, WDist.New(5 * 1024), function(a, id)
		if a.Owner == Scrin and a.Type == "etpd" then
			Trigger.RemoveProximityTrigger(id)
			local camera = Actor.Create("camera", true, { Owner = Nod, Location = a.Location })
			Beacon.New(Nod, a.CenterPosition)
			Media.PlaySound("beacon.aud")
			Trigger.AfterDelay(DateTime.Seconds(6), function()
				camera.Destroy()
			end)
			local rebelDefenders = Utils.Where(Map.ActorsInCircle(Exterminator1Patrol1.CenterPosition, WDist.New(13 * 1024)), function(a)
				return a.Owner == ScrinRebels and not a.IsDead and a.HasProperty("Hunt")
			end)
			Utils.Do(rebelDefenders, function(a)
				a.AttackMove(Exterminator1Patrol1.Location)
				a.Hunt()
			end)
		end
	end)

	Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = Scrin })
		Actor.Create("ai.superweapons.enabled", true, { Owner = Scrin })
	end)

	Utils.Do({ Exterminator1, Exterminator2, Exterminator3, Exterminator4, Exterminator5, Exterminator6, Exterminator7, Exterminator8 }, function(a)
		if Difficulty ~= "hard" then
			a.GrantCondition("difficulty-" .. Difficulty)
		end
		Trigger.OnDamaged(a, function(self, attacker, damage)
			if damage > 500 then
				AggroExterminator(self, Utils.Random(MissionPlayers))
			end
		end)
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
	AutoRebuildConyards(ScrinRebels, true)

	local scrinRebelGroundAttackers = ScrinRebels.GetGroundAttackers()

	Utils.Do(scrinRebelGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinRebelGroundHunterUnit, function(p) return p == Scrin end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1), function()
		InitAttackSquad(Squads.ScrinRebelsMain, ScrinRebels, Scrin)
	end)

	Trigger.AfterDelay(Squads.ScrinRebelsAir.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.ScrinRebelsAir, ScrinRebels, Scrin, { "harv.scrin", "scol", "tpod", "rtpd", "ptur", "devo", "corr", "ruin" })
	end)
end

SendNextExterminator = function()
	if NextExterminatorIndex <= ExterminatorAttackCount[Difficulty] and not Victory then
		local exterminators = {}

		if Exterminators[NextExterminatorIndex] ~= nil then
			exterminators = { Exterminators[NextExterminatorIndex] }
		else
			if not IsNodDead then
				table.insert(exterminators, { SpawnLocation = ExterminatorSpawnWest.Location, TargetPlayer = Nod })
			end
			if not IsGDIDead then
				table.insert(exterminators, { SpawnLocation = ExterminatorSpawnEast.Location, TargetPlayer = GDI })
			end
		end

		Utils.Do(exterminators, function(exterminator)
			local wormhole = Actor.Create("wormhole", true, { Owner = Scrin, Location = exterminator.SpawnLocation })

			Trigger.AfterDelay(DateTime.Seconds(12), function()
				wormhole.Kill()
			end)
		end)

		Trigger.AfterDelay(DateTime.Seconds(2), function()
			MediaCA.PlaySound("etpd-aggro.aud", 2)

			if NextExterminatorIndex == 1 then
				Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(2)), function()
					Media.DisplayMessage("Commander, the Overlord's most powerful weapons are being deployed. Use everything at your disposal to destroy them.", "Kane", HSLColor.FromHex("FF0000"))
					MediaCA.PlaySound("../ca36-reckoning/kane_exterminators.aud", 2)
				end)
			elseif #exterminators > 1 then
				Notification("Exterminator Tripods detected.")
			else
				Notification("Exterminator Tripod detected.")
			end

			Utils.Do(exterminators, function(exterminator)
				local reinforcements = Reinforcements.Reinforce(Scrin, { "etpd" }, { exterminator.SpawnLocation }, 10, function(a)
					if exterminator.Path ~= nil then
						local path = exterminator.Path
						a.Patrol(path)

						Trigger.OnIdle(a, function(self)
							self.Patrol(path)
						end)
					else
						if exterminator.TargetPlayer == nil then
							exterminator.TargetPlayer = MissionPlayers[1]
						end
						AssaultPlayerBaseOrHunt(a, exterminator.TargetPlayer)
					end

					if Difficulty ~= "hard" then
						a.GrantCondition("difficulty-" .. Difficulty)
					end

					Trigger.AfterDelay(ExterminatorsInterval[Difficulty] * 5, function()
						AggroExterminator(a, exterminator.TargetPlayer)
					end)

					Trigger.OnDamaged(a, function(self, attacker, damage)
						if damage > 500 then
							AggroExterminator(self, Utils.Random(MissionPlayers))
						end
					end)
				end)
			end)

			NextExterminatorIndex = NextExterminatorIndex + 1

			Trigger.AfterDelay(ExterminatorsInterval[Difficulty], function()
				SendNextExterminator()
			end)
		end)
	end
end

AggroExterminator = function(a, targetPlayer)
	if not a.IsDead then
		Trigger.ClearAll(a)
		a.Stop()
		Trigger.AfterDelay(1, function()
			if not a.IsDead then
				AssaultPlayerBaseOrHunt(a, targetPlayer)
			end
		end)
	end
end

IsScrinRebelGroundHunterUnit = function(actor)
	return actor.Owner == ScrinRebels and IsGroundHunterUnit(actor) and actor.Type ~= "mast"
end

IsScrinGroundHunterUnitExcludingExterminators = function(actor)
	return IsScrinGroundHunterUnit(actor) and actor.Type ~= "etpd"
end

NodHasMassAir = function()
	local nodAir = Nod.GetActorsByTypes({ "scrn", "apch", "venm" })
	return #nodAir > 7
end

GDIHasMassAir = function()
	local gdiAir = GDI.GetActorsByTypes({ "orca", "orcb", "a10", "a10.gau", "a10.sw", "auro" })
	return #gdiAir > 7
end
