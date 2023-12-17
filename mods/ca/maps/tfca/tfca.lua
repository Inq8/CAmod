UnitsPerPlayer = tonumber(Map.LobbyOption("unitsperplayer"))
WinScore = tonumber(Map.LobbyOption("winscore"))

WorldLoaded = function()
	Blue = Player.GetPlayer("Blue")
	Red = Player.GetPlayer("Red")
	Neutral = Player.GetPlayer("Neutral")
	BlueScore = 0
	RedScore = 0

	Media.DisplayMessage("Loading...", "Notification", HSLColor.Lime)

	Players = Player.GetPlayers(function(p) return (p.Team == 1 or p.Team == 2) and not p.IsNonCombatant and p.InternalName ~= "Blue" and p.InternalName ~= "Red" end)
	BluePlayers = Player.GetPlayers(function(p) return p.Team == 1 and not p.IsNonCombatant and p.InternalName ~= "Blue" and p.InternalName ~= "Red" end)
	RedPlayers = Player.GetPlayers(function(p) return p.Team == 2 and not p.IsNonCombatant and p.InternalName ~= "Blue" and p.InternalName ~= "Red" end)
	BotPlayers = Player.GetPlayers(function(p) return p.IsBot and p.InternalName ~= "Blue" and p.InternalName ~= "Red" end)
	BuildableUnitTypes = { "seal", "e3", "e4", "ivan", "snip", "medi", "xo", "e6", "sab" }
	BotInfo = { }
	BotEngiTurrets = {
		-- engistring = turret
	}

	Objectives = {
		Tech = { Actor = Tech, Waypoints = { Tech1, Tech2, Tech3, Tech4 }, Name = "Tech Center" },
		Power = { Actor = Power, Waypoints = { Power1, Power2, Power3 }, Name = "Power Plant" },
		Ref = { Actor = Ref, Waypoints = { Ref1, Ref2, Ref3, Ref4, Ref5 }, Name = "Refinery" },
		Comms = { Actor = Comms, Waypoints = { Comms1, Comms2, Comms3, Comms4 }, Name = "Comms Center" },
		Dome = { Actor = Dome, Waypoints = { Dome1, Dome2, Dome3, Dome4 }, Name = "Radar Dome" }
	}

	Utils.Do(Objectives, function(o)
		Trigger.OnCapture(o.Actor, function(self, captor, oldOwner, newOwner)
			if newOwner.Team == 1 then
				Media.DisplayMessage("The blue team have captured the " .. o.Name .. "!", "Notification", HSLColor.FromHex("0080FF"))
				self.Owner = Blue
			elseif newOwner.Team == 2 then
				Media.DisplayMessage("The red team have captured the " .. o.Name .. "!", "Notification", HSLColor.Red)
				self.Owner = Red
			end
		end)
	end)

	Trigger.AfterDelay(1, function()
		BotSetup()

		Utils.Do(Players, function(p)
			p.Cash = UnitsPerPlayer
			local spawnPoints = p.GetActorsByType("spawn")

			if #spawnPoints > 0 then
				spawnPoint = spawnPoints[1]
				local spawner = Actor.Create("spawn", true, { Owner = p, Location = spawnPoint.Location })
				Trigger.OnProduction(spawner, function(producer, produced)
					Trigger.OnKilled(produced, function(self, killer)
						Trigger.AfterDelay(DateTime.Seconds(10), function()
							self.Owner.Cash = self.Owner.Cash + 1
						end)
					end)
				end)
			end
		end)

		BalanceUnits = Blue.HasPrerequisites({ "global.balanceunits" })

		if BalanceUnits then
			if #BluePlayers > #RedPlayers and #RedPlayers > 0 then
				Media.DisplayMessage("Blue team has more players. Allocating extra credits.", "Notification", HSLColor.Yellow)
				local redExtra = (#BluePlayers - #RedPlayers) * UnitsPerPlayer
				local redPlayerIdx = 1

				while(redExtra > 0)
				do
					RedPlayers[redPlayerIdx].Cash = RedPlayers[redPlayerIdx].Cash + 1

					if #RedPlayers > redPlayerIdx then
						redPlayerIdx = redPlayerIdx + 1
					else
						redPlayerIdx = 1
					end

					redExtra = redExtra - 1
				end

			elseif #RedPlayers > #BluePlayers and #BluePlayers > 0 then
				Media.DisplayMessage("Red team has more players. Allocating extra credits.", "Notification", HSLColor.Yellow)
				local blueExtra = (#RedPlayers - #BluePlayers) * UnitsPerPlayer
				local bluePlayerIdx = 1

				while(blueExtra > 0)
				do
					BluePlayers[bluePlayerIdx].Cash = BluePlayers[bluePlayerIdx].Cash + 1

					if #BluePlayers > bluePlayerIdx then
						bluePlayerIdx = bluePlayerIdx + 1
					else
						bluePlayerIdx = 1
					end

					blueExtra = blueExtra - 1
				end
			end
		end
	end)
end

Tick = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 50 == 0 then
		local blueObjectives = Blue.GetActorsByTypes({ "atek", "dome", "hq", "apwr", "proc" })
		local redObjectives = Red.GetActorsByTypes({ "atek", "dome", "hq", "apwr", "proc" })
		local scoreAmounts = { 2, 3, 4, 7, 60 }

		if #blueObjectives > 0 then
			BlueScore = BlueScore + scoreAmounts[#blueObjectives]
		end

		if #redObjectives > 0 then
			RedScore = RedScore + scoreAmounts[#redObjectives]
		end

		if BlueScore >= WinScore then
			BlueScore = WinScore
			BlueWins()
		elseif RedScore >= WinScore then
			RedScore = WinScore
			RedWins()
		end

		UpdateScoresText()
		BotTick()
	end
end

UpdateScoresText = function()
	local color = HSLColor.White

	if BlueScore > RedScore then
		color = HSLColor.FromHex("0080FF")
	elseif RedScore > BlueScore then
		color = HSLColor.Red
	end

	UserInterface.SetMissionText("Blue = " .. BlueScore .. " / " .. WinScore .. " -- vs -- Red = " .. RedScore .. " / " .. WinScore, color)
end

BlueWins = function()
	Utils.Do(RedPlayers, function(p)
		local spawns = p.GetActorsByType("spawn")
		Utils.Do(spawns, function(s)
			s.Destroy()
		end)
	end)
	local spawnguns = Red.GetActorsByType("spawngun")
	Utils.Do(spawnguns, function(g)
		g.Kill()
	end)
	Utils.Do(Objectives, function(o)
		o.Actor.Owner = Blue
	end)
end

RedWins = function()
	Utils.Do(BluePlayers, function(p)
		local spawns = p.GetActorsByType("spawn")
		Utils.Do(spawns, function(s)
			s.Destroy()
		end)
	end)
	local spawnguns = Blue.GetActorsByType("spawngun")
	Utils.Do(spawnguns, function(g)
		g.Kill()
	end)
	Utils.Do(Objectives, function(o)
		o.Actor.Owner = Red
	end)
end

BotSetup = function()
	Utils.Do(BotPlayers, function(p)
		local initialObjective

		if p.Team == 1 then
			initialObjective = Objectives.Comms
		else
			initialObjective = Objectives.Dome
		end

		BotInfo[p.InternalName] = {
			FirstObjective = true,
			CurrentObjective = initialObjective
		}
	end)
end

BotTick = function()
	Utils.Do(BotPlayers, function(p)
		local botUnits = p.GetActorsByTypes(BuildableUnitTypes)
		local botInfo = BotInfo[p.InternalName]

		local alliedPlayers
		if p.Team == 1 then
			alliedPlayers = BluePlayers
		else
			alliedPlayers = RedPlayers
		end

		-- if current objective is not set, or is already owned by an ally, set a new one
		if botInfo.CurrentObjective == nil or p.IsAlliedWith(botInfo.CurrentObjective.Actor.Owner) then
			SelectNewObjective(p, botUnits, botInfo)

		-- random chance to swap objectives every 25 seconds
		elseif DateTime.GameTime > 1 and DateTime.GameTime % 625 == 0 and Utils.RandomInteger(0, 100) > 50 then
			SelectNewObjective(p, botUnits, botInfo)
		end

		-- if bot has money, make a unit
		if p.Cash > 0 then
			local chosenType = Utils.Random(BuildableUnitTypes)

			p.Build({ chosenType }, function(actors)
				Utils.Do(actors, function(a)

					Trigger.OnIdle(a, function(self)

						if botInfo.CurrentObjective ~= nil then

							if self.Type == "medi" then
								local randomTarget = RandomTargetToHeal(self, alliedPlayers)
								if randomTarget ~= nil then
									self.Guard(randomTarget)
								end
							end

							-- attack move to random location near current objective
							local randomWaypoint = Utils.Random(botInfo.CurrentObjective.Waypoints)
							self.AttackMove(randomWaypoint.Location)

							-- 75% chance to try capping if engi/xo/infil, otherwise 25%
							local capChance = Utils.RandomInteger(0, 100)
							if (self.Type == "e6" or self.Type == "sab" or self.Type == "xo" and capChance > 25) or (capChance > 75) then
								if self.CanCapture(botInfo.CurrentObjective.Actor) then
									self.Capture(botInfo.CurrentObjective.Actor)
								end
							end

							if self.Type == "e6" then
								if Utils.RandomInteger(0, 100) > 66 then
									local selfString = tostring(self)

									if BotEngiTurrets[selfString] == nil or BotEngiTurrets[selfString].IsDead then
										local randomTurretLocation = FindTurretLocation(self)

										if randomTurretLocation ~= nil then
											BotEngiTurrets[selfString] = Actor.Create("gun", true, { Owner = self.Owner, Location = randomTurretLocation })
										end
									end

									if BotEngiTurrets[selfString] ~= nil and not BotEngiTurrets[selfString].IsDead then
										local guardChance = Utils.RandomInteger(0, 100)
										if guardChance > 15 then
											self.Guard(BotEngiTurrets[selfString])
										end
									end
								end
							end
						end
					end)
				end)
			end)
		end
	end)
end

RandomTargetToHeal = function(self, alliedPlayers)
	local target = nil

	Utils.Do(alliedPlayers, function(ap)
		local units = ap.GetActorsByTypes(BuildableUnitTypes)
		if #units > 0 then
			possibleTargets = Utils.Where(units, function(a)
				return a ~= self and a.Type ~= "medi" and a.Type ~= "sab"
			end)
			if #possibleTargets > 0 then
				target = Utils.Random(possibleTargets)
			end
			return
		end
	end)

	return target
end

FindTurretLocation = function(self)
	local waypoints = Map.ActorsInCircle(self.CenterPosition, WDist.New(5120), function(a)
		return a.Type == "waypoint"
	end)

	if #waypoints > 0 then
		local randomWaypoint = Utils.Random(waypoints)
		local footprint = Utils.ExpandFootprint({ randomWaypoint.Location }, true)
		randomTargetCell = Utils.Random(footprint)
		return randomTargetCell
	end

	return nil
end

SelectNewObjective = function(p, botUnits, botInfo)
	local possibleObjectives = Utils.Where(Objectives, function(o)
		return not p.IsAlliedWith(o.Actor.Owner)
	end)

	if #possibleObjectives > 0 then
		local newObjective = Utils.Random(possibleObjectives)
		botInfo.CurrentObjective = newObjective
		botInfo.FirstObjective = false

		if #botUnits > 0 then
			Utils.Do(botUnits, function(a)
				a.Stop()
			end)
		end
	end
end
