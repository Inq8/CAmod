--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

Difficulty = Map.LobbyOption("difficulty")

BuildTimeMultipliers = {
	easy = 1,
	normal = 0.9,
	hard = 0.8
}

ConyardTypes = { "fact", "afac", "sfac" }

HarvesterTypes = { "harv", "harv.td", "harv.scrin", "harv.chrono" }

FactoryTypes = { "weap", "weap.td", "wsph", "airs" }

RefineryTypes = { "proc", "proc.td", "proc.scrin" }

NavalProductionTypes = { "syrd", "spen", "syrd.gdi", "spen.nod" }

CashRewardOnCaptureTypes = { "proc", "proc.td", "proc.scrin", "silo", "silo.td", "silo.scrin" }

WallTypes = { "sbag", "fenc", "brik", "cycl", "barb" }

KeyStructures = { "fact", "afac", "sfac", "proc", "proc.td", "proc.scrin", "weap", "weap.td", "airs", "wsph", "dome", "hq", "nerv", "atek", "stek", "gtek", "tmpl", "scrt" }

NextRebuildTimes = { }

SquadLeaders = { }

AlertedUnits = { }

-- per production structure, stores which squad to assign produced units to next
SquadAssignmentQueue = { }

-- stores which AI production structures have triggers assigned to prevent them being added multiple times
OnProductionTriggers = { }

InitObjectives = function(player)
	Trigger.OnObjectiveAdded(player, function(p, id)
		Trigger.AfterDelay(1, function()
			local colour = HSLColor.Yellow
			if p.GetObjectiveType(id) ~= "Primary" then
				colour = HSLColor.Gray
			end
			Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective", colour)
		end)
	end)

	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.PlaySoundNotification(player, "AlertBleep")
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed", HSLColor.LimeGreen)
	end)

	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed", HSLColor.Red)
	end)

	Trigger.OnPlayerLost(player, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "MissionFailed")
		end)
	end)

	Trigger.OnPlayerWon(player, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "MissionAccomplished")
		end)
	end)
end

Notification = function(text)
	Media.DisplayMessage(text, "Notification", HSLColor.FromHex("1E90FF"))
end

Tip = function(text)
	Media.DisplayMessage(text, "Tip", HSLColor.FromHex("29F3CF"))
end

AttackAircraftTargets = { }
InitializeAttackAircraft = function(aircraft, targetPlayer, targetTypes)
	if not aircraft.IsDead then
		Trigger.OnIdle(aircraft, function(self)
			if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
				local actorId = tostring(aircraft)
				local target = AttackAircraftTargets[actorId]

				if not target or not target.IsInWorld then
					if targetTypes ~= nil then
						target = ChooseRandomTargetOfTypes(self, targetPlayer, targetTypes)
					else
						target = ChooseRandomTarget(self, targetPlayer)
					end
				end

				if target then
					AttackAircraftTargets[actorId] = target
					self.Attack(target)
				else
					AttackAircraftTargets[actorId] = nil
					self.ReturnToBase()
				end
			end
		end)
	end
end

ChooseRandomTarget = function(unit, targetPlayer)
	local target = nil
	local enemies = Utils.Where(targetPlayer.GetActors(), function(self)
		return self.HasProperty("Health") and unit.CanTarget(self) and not Utils.Any(WallTypes, function(type) return self.Type == type end)
	end)
	if #enemies > 0 then
		target = Utils.Random(enemies)
	end
	return target
end

ChooseRandomTargetOfTypes = function(unit, targetPlayer, types)
	local target = nil
	local enemies = Utils.Where(targetPlayer.GetActors(), function(self)
		return self.HasProperty("Health") and unit.CanTarget(self) and Utils.Any(types, function(type) return self.Type == type end)
	end)
	if #enemies > 0 then
		target = Utils.Random(enemies)
	end
	return target
end

ChooseRandomBuildingTarget = function(unit, targetPlayer)
	local target = nil
	local enemies = Utils.Where(targetPlayer.GetActors(), function(self)
		return self.HasProperty("Health") and self.HasProperty("StartBuildingRepairs") and unit.CanTarget(self) and not Utils.Any(WallTypes, function(type) return self.Type == type end)
	end)
	if #enemies > 0 then
		target = Utils.Random(enemies)
	end
	return target
end

OnAnyDamaged = function(actors, func)
	Utils.Do(actors, function(actor)
		Trigger.OnDamaged(actor, func)
	end)
end

-- Make the unit hunt when it becomes idle.
IdleHunt = function(actor)
	if actor.HasProperty("Hunt") and not actor.IsDead then
		Trigger.OnIdle(actor, function(a)
			if not a.IsDead and a.IsInWorld then
				a.Hunt()
			end
		end)
	end
end

AssaultPlayerBaseOrHunt = function(actor, waypoints)
	Trigger.AfterDelay(1, function()
		if not actor.IsDead then
			if waypoints ~= nil then
				Utils.Do(waypoints, function(w)
					actor.AttackMove(w)
				end)
			end
			if PlayerBaseLocation ~= nil then
				local possibleCellsInner = Utils.ExpandFootprint({ PlayerBaseLocation }, true)
				local possibleCells = Utils.ExpandFootprint(possibleCellsInner, false)
				local cell = Utils.Random(possibleCells)
				actor.AttackMove(cell)
			elseif actor.HasProperty("Hunt") then
				actor.Hunt()
			end
			Trigger.AfterDelay(1, function()
				if not actor.IsDead then
					Trigger.OnIdle(actor, function(a)
						AssaultPlayerBaseOrHunt(a)
					end)
				end
			end)
		end
	end)
end

UpdatePlayerBaseLocation = function()
	if MissionPlayer == nil then
		return
	end
	local keyBaseBuildings = MissionPlayer.GetActorsByTypes(KeyStructures)
	if #keyBaseBuildings > 0 then
		local keyBaseBuilding = Utils.Random(keyBaseBuildings)
		PlayerBaseLocation = keyBaseBuilding.Location
	end
end

AutoRepairBuildings = function(player)
	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == player and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(a)
		AutoRepairBuilding(a, player)
	end)
end

AutoRepairAndRebuildBuildings = function(player, maxRebuildAttempts, excludedRebuildTypes)
	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == player and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(a)
		local excludeFromRebuilding = false
		if excludedRebuildTypes ~= nil then
			Utils.Do(excludedRebuildTypes, function(t)
				if t == a.Type then
					excludeFromRebuilding = true
					return;
				end
			end)
		end
		AutoRepairBuilding(a, player)
		if not excludeFromRebuilding then
			AutoRebuildBuilding(a, player)
		end
	end)
end

AutoRepairBuilding = function(building, player)
	if building.IsDead then
		return
	end
	Trigger.OnDamaged(building, function(self, attacker, damage)
		if self.Owner == player and self.Health < (self.MaxHealth * 75 / 100) then
			self.StartBuildingRepairs()
		end
	end)
end

AutoRebuildBuilding = function(building, player, maxAttempts)
	if building.IsDead then
		return
	end
	Trigger.OnKilled(building, function(self, killer)
		local buildingType = self.Type
		local loc = self.Location
		local pos = self.CenterPosition
		RebuildBuilding(buildingType, player, loc, pos, 1, maxAttempts)
	end)
	Trigger.OnSold(building, function(self)
		local buildingType = self.Type
		local loc = self.Location
		local pos = self.CenterPosition
		RebuildBuilding(buildingType, player, loc, pos, 1, maxAttempts)
	end)
end

RebuildBuilding = function(buildingType, player, loc, pos, attemptNumber, maxAttempts)

	-- If next build time set for player, or it's in the past, set to current game time
	if NextRebuildTimes[player] == nil or NextRebuildTimes[player] < DateTime.GameTime then
		NextRebuildTimes[player] = DateTime.GameTime
	end

	local buildTime = math.ceil(Actor.BuildTime(buildingType) * BuildTimeMultipliers[Difficulty])
	local delayToAdd = NextRebuildTimes[player] - DateTime.GameTime

	-- Add build time of the next building to the next build time for the player
	if attemptNumber == 1 then
		NextRebuildTimes[player] = NextRebuildTimes[player] + buildTime
	else
		if delayToAdd < DateTime.Seconds(20) then
			delayToAdd = DateTime.Seconds(20)
		end
		delayToAdd = delayToAdd + DateTime.Seconds(Utils.RandomInteger(10,30))
	end

	Trigger.AfterDelay(buildTime + delayToAdd, function()
		if HasConyard(player) then
			local topLeft = WPos.New(pos.X - 8192, pos.Y - 8192, 0)
			local bottomRight = WPos.New(pos.X + 8192, pos.Y + 8192, 0)
			local nearbyBuildings = Map.ActorsInBox(topLeft, bottomRight, function(a)
				return not a.IsDead and a.Owner == player and a.HasProperty("StartBuildingRepairs") and not a.HasProperty("Attack")
			end)

			local nearbyEnemyBuildings = Map.ActorsInBox(topLeft, bottomRight, function(a)
				return not a.IsDead and a.Owner ~= player and a.HasProperty("StartBuildingRepairs")
			end)

			topLeft = WPos.New(pos.X - 2048, pos.Y - 2048, 0)
			bottomRight = WPos.New(pos.X + 2048, pos.Y + 2048, 0)
			local nearbyUnits = Map.ActorsInBox(topLeft, bottomRight, function(a)
				return not a.IsDead and a.HasProperty("Move")
			end)

			topLeft = WPos.New(pos.X - 512, pos.Y - 512, 0)
			bottomRight = WPos.New(pos.X + 512, pos.Y + 512, 0)
			local sameCellActors = Map.ActorsInBox(topLeft, bottomRight, function(a)
				return not a.IsDead and a.Owner == player and a.HasProperty("Kill")
			end)

			-- Rebuild if no units are nearby (potentially blocking), no enemy buildings are nearby, and friendly buildings are in the area (but nothing friendly in the same cell)
			if #nearbyBuildings > 0 and #nearbyUnits == 0 and #nearbyEnemyBuildings == 0 and #sameCellActors == 0 then
				local b = Actor.Create(buildingType, true, { Owner = player, Location = loc })
				AutoRepairBuilding(b, player);
				AutoRebuildBuilding(b, player);
			-- Otherwise retry
			elseif maxAttempts == nil or attemptNumber < maxAttempts then
				RebuildBuilding(buildingType, player, loc, pos, attemptNumber + 1, maxAttempts)
			end
		end
	end)
end

-- Returns true if player has one of any of the specified actor types
HasOneOf = function(player, types)
	local count = 0

	Utils.Do(types, function(name)
		if #player.GetActorsByType(name) > 0 then
			count = count + 1
		end
	end)

	return count > 0
end

-- Make specified units have a chance to swap targets when attacked instead of chasing one target endlessly
TargetSwapChance = function(unit, player, chance)
	Trigger.OnDamaged(unit, function(self, attacker, damage)
		if attacker.Owner ~= MissionPlayer then
			return
		end
		local rand = Utils.RandomInteger(1,100)
		if rand > 100 - chance then
			if not unit.IsDead and not attacker.IsDead and unit.HasProperty("Attack") then
				unit.Stop()
				if unit.CanTarget(attacker) then
					unit.Attack(attacker)
				end
			end
		end
	end)
end

CallForHelpOnDamagedOrKilled = function(actor, range, filter)
	Trigger.OnDamaged(actor, function(self, attacker, damage)
		if attacker.Owner == MissionPlayer then
			CallForHelp(self, range, filter)
		end
	end)
	Trigger.OnKilled(actor, function(self, killer)
		if killer.Owner == MissionPlayer then
			CallForHelp(self, range, filter)
		end
	end)
end

CallForHelp = function(self, range, filter)
	local selfId = tostring(self);
	if AlertedUnits[selfId] == nil then
		if not self.IsDead then
			AlertedUnits[selfId] = true
			self.Stop()
			IdleHunt(self)
		end

		local nearbyUnits = Map.ActorsInCircle(self.CenterPosition, range, filter)

		Utils.Do(nearbyUnits, function(nearbyUnit)
			local nearbyUnitId = tostring(nearbyUnit)
			if not nearbyUnit.IsDead and AlertedUnits[nearbyUnitId] == nil then
				AlertedUnits[nearbyUnitId] = true
				nearbyUnit.Stop()
				IdleHunt(nearbyUnit)
			end
		end)
	end
end

--- Attack squad functionality, requires Squads object to be defined properly in the mission script file
InitAttackSquad = function(squad, player)
	squad.Player = player

	if IsSquadInProduction(squad) then
		return
	end

	local queues = { }
	for k,v in pairs(squad.QueueProductionStatuses) do
		table.insert(queues, k)
	end

	-- make sure ActiveCondition function returns true (if it exists)
	local isActive = squad.ActiveCondition == nil or squad.ActiveCondition()

	if isActive then

		-- randomly select a unit composition for next wave
		squad.QueuedUnits = Utils.Random(squad.Units[Difficulty])

		-- go through each queue for the current difficulty and start producing the first unit
		Utils.Do(queues, function(queue)
			ProduceNextAttackSquadUnit(squad, queue, 1)
		end)
	else
		Trigger.AfterDelay(squad.Interval[Difficulty], function()
			InitAttackSquad(squad, player)
		end)
	end
end

InitAirAttackSquad = function(squad, player, targetPlayer, targetTypes)
	squad.AirTargetPlayer = targetPlayer
	squad.AirTargetTypes = targetTypes
	InitAttackSquad(squad, player)
end

ProduceNextAttackSquadUnit = function(squad, queue, unitIndex)
	local units = squad.QueuedUnits[queue]

	-- if there are no more units to build for this queue, check if any other queues are producing -- if none are, send the attack and produce next attack squad after interval
	if unitIndex > #units then
		squad.QueueProductionStatuses[queue] = false

		-- only send if units have actually been produced for this queue, and no other queues are producing
		if #units > 0 and not IsSquadInProduction(squad) then
			local dispatchDelay

			if squad.DispatchDelay ~= nil then
				dispatchDelay = squad.DispatchDelay
			else
				dispatchDelay = DateTime.Seconds(2)
			end

			Trigger.AfterDelay(dispatchDelay, function()
				SendAttackSquad(squad)
			end)
			Trigger.AfterDelay(squad.Interval[Difficulty], function()
				local transitionPlayer = squad.Player
				if squad.TransitionTo ~= nil and DateTime.GameTime >= squad.TransitionTo.GameTime[Difficulty] then
					squad = Squads[squad.TransitionTo.SquadType]
				end
				if squad ~= nil then
					InitAttackSquad(squad, transitionPlayer)
				end
			end)
		end
	-- if more units to build, set them to produce after delay equal to their build time (with difficulty multiplier applied)
	else
		squad.QueueProductionStatuses[queue] = true
		local nextUnit = units[unitIndex]
		local buildTime = math.ceil(Actor.BuildTime(nextUnit) * BuildTimeMultipliers[Difficulty])

		-- after the build time has elapsed
		Trigger.AfterDelay(buildTime, function()
			local producer = nil

			-- find appropriate producer actor (either the first specific actor, or if not found, randomly selected of from specified types)
			if squad.ProducerActors ~= nil and squad.ProducerActors[queue] ~= nil then
				Utils.Do(squad.ProducerActors[queue], function(a)
					if producer == nil and not a.IsDead and a.Owner == squad.Player then
						producer = a
					end
				end)
			end

			if producer == nil and squad.ProducerTypes ~= nil and squad.ProducerTypes[queue] ~= nil then
				local producers = squad.Player.GetActorsByTypes(squad.ProducerTypes[queue])
				if #producers > 0 then
					producer = Utils.Random(producers)
				end
			end

			-- create the unit
			if producer ~= nil then
				local producerId = tostring(producer)
				AddToSquadAssignmentQueue(producerId, squad)

				if OnProductionTriggers[producerId] == nil then
					OnProductionTriggers[producerId] = true

					-- add produced unit to list of idle units for the squad
					Trigger.OnProduction(producer, function(p, produced)
						local isHarvester = false

						-- we don't want to add harvesters to squads, which are produced when replacements are needed
						Utils.Do(HarvesterTypes, function(harvesterType)
							if produced.Type == harvesterType then
								isHarvester = true
							end
						end)

						if not isHarvester then
							if SquadAssignmentQueue[producerId][1] ~= nil then
								local assignedSquad = SquadAssignmentQueue[producerId][1]
								SquadAssignmentQueue[producerId][1].IdleUnits[#assignedSquad.IdleUnits + 1] = produced
								table.remove(SquadAssignmentQueue[producerId], 1)
							elseif produced.HasProperty("Hunt") then
								produced.Hunt()
							end

							if produced.HasProperty("HasPassengers") and not produced.IsDead then
								Trigger.OnPassengerExited(produced, function(t, p)
									AssaultPlayerBaseOrHunt(p)
								end)
							end

							TargetSwapChance(produced, squad.Player, 10)
						end
					end)
				end

				producer.Produce(nextUnit)
			end

			-- start producing the next unit
			ProduceNextAttackSquadUnit(squad, queue, unitIndex + 1)
		end)
	end
end

-- used to make sure multiple squads being produced from the same structure don't get mixed up
AddToSquadAssignmentQueue = function(producerId, squad)
	if SquadAssignmentQueue[producerId] == nil then
		SquadAssignmentQueue[producerId] = { }
	end
	SquadAssignmentQueue[producerId][#SquadAssignmentQueue[producerId] + 1] = squad
end

IsSquadInProduction = function(squad)
	local producing = false
	Utils.Do(squad.QueueProductionStatuses, function(p)
		if p then
			producing = true
		end
	end)
	return producing
end

SendAttackSquad = function(squad)

	if squad.AirTargetTypes ~= nil and squad.AirTargetPlayer ~= nil then
		Utils.Do(squad.IdleUnits, function(a)
			if not a.IsDead then
				InitializeAttackAircraft(a, squad.AirTargetPlayer, squad.AirTargetTypes)
			end
		end)
	else
		local squadLeader = nil
		local attackPath = nil

		if squad.AttackPaths ~= nil then
			attackPath = Utils.Random(squad.AttackPaths)
		end

		Utils.Do(squad.IdleUnits, function(a)
			local actorId = tostring(a)

			if attackPath ~= nil then
				if not a.IsDead and a.IsInWorld then
					if squad.FollowLeader ~= nil and squad.FollowLeader == true and squadLeader == nil then
						squadLeader = a;
					end

					-- If squad leader, queue attack move to each attack path waypoint
					if squadLeader == nil or a == squadLeader then
						Utils.Do(attackPath, function(w)
							a.AttackMove(w, 3)
							if squad.IsNaval ~= nil and squad.IsNaval then
								IdleHunt(a)
							else
								AssaultPlayerBaseOrHunt(a);
							end
						end)

						-- On damaged or killed
						Trigger.OnDamaged(a, function(self, attacker, damage)
							ClearSquadLeader(squadLeader)
						end)

						Trigger.OnKilled(a, function(self, attacker, damage)
							ClearSquadLeader(squadLeader)
						end)

					-- If not squad leader, follow the leader
					else
						SquadLeaders[actorId] = squadLeader
						FollowSquadLeader(a)

						-- If damaged (stop guarding, attack move to enemy base)
						Trigger.OnDamaged(a, function(self, attacker, damage)
							ClearSquadLeader(SquadLeaders[actorId])
						end)
					end
				end
			else
				if squad.IsNaval ~= nil and squad.IsNaval then
					IdleHunt(a)
				else
					AssaultPlayerBaseOrHunt(a);
				end
			end
		end)
	end
	squad.IdleUnits = { }
end

ClearSquadLeader = function(squadLeader)
	for k,v in pairs(SquadLeaders) do
		if v == squadLeader then
			SquadLeaders[k] = nil
		end
	end
end

FollowSquadLeader = function(actor)
	if not actor.IsDead and actor.IsInWorld then
		local actorId = tostring(actor)

		if SquadLeaders[actorId] ~= nil and not SquadLeaders[actorId].IsDead then
			local possibleCells = Utils.ExpandFootprint({ SquadLeaders[actorId].Location }, true)
			local cell = Utils.Random(possibleCells)
			actor.Stop()
			actor.AttackMove(cell, 1)

			Trigger.AfterDelay(Utils.RandomInteger(35,65), function()
				FollowSquadLeader(actor)
			end)
		else
			actor.Stop()
			Trigger.ClearAll(actor)
			AssaultPlayerBaseOrHunt(actor);
		end
	end
end

SetupRefAndSilosCaptureCredits = function(player)
	local silosAndRefineries = player.GetActorsByTypes(CashRewardOnCaptureTypes)
	Utils.Do(silosAndRefineries, function(a)
		Trigger.OnCapture(a, function(self, captor, oldOwner, newOwner)
			newOwner.Cash = newOwner.Cash + 500
			Media.FloatingText("+$500", self.CenterPosition, 30, newOwner.Color)
		end)
	end)
end

HasConyard = function(player)
	return CountConyards(player) >= 1
end

CountConyards = function(player)
	local Conyards = player.GetActorsByTypes(ConyardTypes)
	return #Conyards
end

AutoReplaceHarvesters = function(player)
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		local harvesters = player.GetActorsByTypes(HarvesterTypes)
		Utils.Do(harvesters, function(a)
			AutoReplaceHarvester(player, a)
		end)
	end)

	Trigger.OnAnyProduction(function(producer, produced, productionType)
		if produced.Owner == player then
			Utils.Do(HarvesterTypes, function(t)
				if not produced.IsDead and produced.Type == t then
					local refineries = player.GetActorsByTypes(RefineryTypes)
					if #refineries > 0 then
						local refinery = Utils.Random(refineries)
						produced.Move(refinery.Location, 2)
						produced.FindResources()
						AutoReplaceHarvester(player, produced)
					end
				end
			end)
		end
	end)
end

AutoReplaceHarvester = function(player, harvester)
	Trigger.OnKilled(harvester, function(self, killer)
		local harvType = self.Type
		local buildTime = math.ceil(Actor.BuildTime(harvType) * BuildTimeMultipliers[Difficulty])
		local randomExtraTime = Utils.RandomInteger(DateTime.Seconds(5), DateTime.Seconds(15))

		Trigger.AfterDelay(buildTime + randomExtraTime, function()
			local producers = player.GetActorsByTypes(FactoryTypes)

			if #producers > 0 then
				local producer = Utils.Random(producers)
				producer.Produce(harvType)
			end
		end)
	end)
end

DoHelicopterDrop = function(player, entryPath, transportType, units, unitFunc, transportExitFunc)
	Reinforcements.ReinforceWithTransport(player, transportType, units, entryPath, nil, function(transport, cargo)
		if not transport.IsDead then
			transport.UnloadPassengers()
			if unitFunc ~= nil then
				Trigger.AfterDelay(DateTime.Seconds(5), function()
					Utils.Do(cargo, function(a)
						unitFunc(a)
					end)
				end)
			end
			if transportExitFunc ~= nil then
				transportExitFunc(transport)
			end
		end
	end)
end

DoNavalTransportDrop = function(player, entryPath, exitPath, transportType, units, unitFunc)
	local cargo = Reinforcements.ReinforceWithTransport(player, transportType, units, entryPath, exitPath)[2]
	if unitFunc ~= nil then
		Utils.Do(cargo, function(a)
			Trigger.OnAddedToWorld(a, function(self)
				unitFunc(self)
			end)
		end)
	end
end

PlayerHasNavalProduction = function(player)
	local navalProductionBuildings = player.GetActorsByTypes(NavalProductionTypes)
	return #navalProductionBuildings > 0
end
