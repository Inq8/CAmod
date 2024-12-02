--[[
   Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
   This file is part of OpenRA Combined Arms, which is free software.
   It is made available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of the License,
   or (at your option) any later version. For more information, see COPYING.
]]

Difficulty = Map.LobbyOptionOrDefault("difficulty", "normal")
GameSpeed = UtilsCA.GameSpeed()

StructureBuildTimeMultipliers = {
	easy = 3,
	normal = 2,
	hard = 1,
}

UnitBuildTimeMultipliers = {
	easy = 1,
	normal = 0.67,
	hard = 0.5,
}

HarvesterDeathDelayTime = {
	easy = DateTime.Seconds(60),
	normal = DateTime.Seconds(40),
	hard = DateTime.Seconds(20),
}

CashAdjustments = {
	easy = 3500,
	normal = 0,
	hard = -1500
}

CapturedCreditsAmount = 1250

EnforceAiBuildRadius = false

ConyardTypes = { "fact", "afac", "sfac" }

HarvesterTypes = { "harv", "harv.td", "harv.scrin", "harv.chrono" }

FactoryTypes = { "weap", "weap.td", "wsph", "airs" }

RefineryTypes = { "proc", "proc.td", "proc.scrin" }

NavalProductionTypes = { "syrd", "spen", "syrd.gdi", "spen.nod" }

CashRewardOnCaptureTypes = { "proc", "proc.td", "proc.scrin", "silo", "silo.td", "silo.scrin" }

WallTypes = { "sbag", "fenc", "brik", "cycl", "barb" }

KeyStructures = { "fact", "afac", "sfac", "proc", "proc.td", "proc.scrin", "weap", "weap.td", "airs", "wsph", "dome", "hq", "nerv", "atek", "stek", "gtek", "tmpl", "scrt" }

-- used to define actors and/or types of actors that the AI should not rebuild
RebuildExcludes = {
	-- USSR = {
	-- 	 Actors = { Actor },
	-- 	 Types = { "proc" }
	-- }
}

-- should be populated with the human players in the mission
MissionPlayers = { }

--
-- begin automatically populated vars (do not assign values to these)
--

-- stores the player base locations (recalculated at intervals)
PlayerBaseLocations = { }

-- queued structures for AI
BuildingQueues = { }

-- stores active squad leaders
SquadLeaders = { }

-- stores actors which have called for help so they don't do so repeatedly
AlertedUnits = { }

-- per production structure, stores which squad to assign produced units to next
SquadAssignmentQueue = { }

-- stores which AI production structures have triggers assigned to prevent them being added multiple times
OnProductionTriggers = { }

-- when player kills an AI harvester it delays the production of the next attack wave
HarvesterDeathStacks = { }

--
-- end automatically populated vars
--

InitObjectives = function(player)
	Trigger.OnObjectiveAdded(player, function(p, id)
		if p.IsLocalPlayer then
			Trigger.AfterDelay(1, function()
				local colour = HSLColor.Yellow
				if p.GetObjectiveType(id) ~= "Primary" then
					colour = HSLColor.Gray
				end
				Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective", colour)
			end)
		end
	end)

	Trigger.OnObjectiveCompleted(player, function(p, id)
		if p.IsLocalPlayer then
			Media.PlaySoundNotification(player, "AlertBleep")
			Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed", HSLColor.LimeGreen)
		end
	end)

	Trigger.OnObjectiveFailed(player, function(p, id)
		if p.IsLocalPlayer then
			Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed", HSLColor.Red)
		end
	end)

	Trigger.OnPlayerLost(player, function()
		if player.IsLocalPlayer then
			Trigger.AfterDelay(DateTime.Seconds(1), function()
				Media.PlaySpeechNotification(player, "MissionFailed")
			end)
		end
	end)

	Trigger.OnPlayerWon(player, function()
		if player.IsLocalPlayer then
			Trigger.AfterDelay(DateTime.Seconds(1), function()
				Media.PlaySpeechNotification(player, "MissionAccomplished")
			end)
		end
	end)
end

Notification = function(text)
	Media.DisplayMessage(text, "Notification", HSLColor.FromHex("1E90FF"))
end

Tip = function(text)
	Media.DisplayMessage(text, "Tip", HSLColor.FromHex("29F3CF"))
end

AttackAircraftTargets = { }
InitAttackAircraft = function(aircraft, targetPlayer, targetTypes)
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

-- make the unit hunt when it becomes idle
IdleHunt = function(actor)
	if actor.HasProperty("Hunt") and not actor.IsDead then
		Trigger.OnIdle(actor, function(a)
			if not a.IsDead and a.IsInWorld and not IsMissionPlayer(a.Owner) then
				a.Hunt()
			end
		end)
	end
end

AssaultPlayerBaseOrHunt = function(actor, targetPlayer, waypoints, fromIdle)

	if not actor.HasProperty("AttackMove") or IsMissionPlayer(actor.Owner) then
		return
	end

	if targetPlayer == nil and #MissionPlayers > 0 then
		targetPlayer = MissionPlayers[1]
	end

	Trigger.AfterDelay(1, function()
		if not actor.IsDead then
			if waypoints ~= nil then
				Utils.Do(waypoints, function(w)
					actor.AttackMove(w, 1)
				end)
			end
			if IsMissionPlayer(targetPlayer) and PlayerBaseLocations[targetPlayer.InternalName] ~= nil then
				local possibleCellsInner = Utils.ExpandFootprint({ PlayerBaseLocations[targetPlayer.InternalName] }, true)
				local possibleCells = Utils.ExpandFootprint(possibleCellsInner, false)
				local cell = Utils.Random(possibleCells)
				actor.AttackMove(cell, 1)
			elseif actor.HasProperty("Hunt") then
				actor.Hunt()
			end
			-- only add the OnIdle trigger if it wasn't triggered from OnIdle (don't need multiple triggers)
			if not fromIdle then
				Trigger.AfterDelay(1, function()
					if not actor.IsDead then
						Trigger.OnIdle(actor, function(a)
							AssaultPlayerBaseOrHunt(a, targetPlayer, nil, true)
						end)
					end
				end)
			end
		end
	end)
end

IsMissionPlayer = function(player)
	if #MissionPlayers == 0 or player == nil then
		return false
	end

	for _, p in pairs(MissionPlayers) do
		if p == player then
			return true
		end
	end

	return false
end

UpdatePlayerBaseLocations = function()
	if #MissionPlayers == 0 then
		return false
	end

	for _, p in pairs(MissionPlayers) do
		local keyBaseBuildings = p.GetActorsByTypes(KeyStructures)
		if #keyBaseBuildings > 0 then
			local keyBaseBuilding = Utils.Random(keyBaseBuildings)
			PlayerBaseLocations[p.InternalName] = keyBaseBuilding.Location
		end
	end
end

AutoRepairBuildings = function(player)
	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == player and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(a)
		AutoRepairBuilding(a, player)
	end)
end

AutoRepairAndRebuildBuildings = function(player, maxAttempts)
	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == player and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(a)
		local excludeFromRebuilding = false
		if a.Type == "fact" or a.Type == "afac" or a.Type == "sfac" then
			excludeFromRebuilding = true
		else
			if RebuildExcludes ~= nil and RebuildExcludes[player.InternalName] ~= nil then
				if RebuildExcludes[player.InternalName].Actors ~= nil then
					for _, aa in pairs(RebuildExcludes[player.InternalName].Actors) do
						if aa == a then
							excludeFromRebuilding = true
							break
						end
					end
				end
				if RebuildExcludes[player.InternalName].Types ~= nil then
					for _, t in pairs(RebuildExcludes[player.InternalName].Types) do
						if a.Type == t then
							excludeFromRebuilding = true
							break
						end
					end
				end
			end
		end
		AutoRepairBuilding(a, player)
		if not excludeFromRebuilding then
			AutoRebuildBuilding(a, player, maxAttempts)
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
	if building.Owner == player and building.Health < (building.MaxHealth * 75 / 100) then
		building.StartBuildingRepairs()
	end
end

AutoRebuildBuilding = function(building, player, maxAttempts)
	if BuildingQueues[player.InternalName] == nil then
		BuildingQueues[player.InternalName] = { }
	end
	if maxAttempts == nil then
		maxAttempts = 15
	end
	if building.IsDead then
		return
	end
	Trigger.OnKilled(building, function(self, killer)
		AddToRebuildQueue(building, player, building.Location, building.CenterPosition, maxAttempts)
	end)
	Trigger.OnSold(building, function(self)
		AddToRebuildQueue(building, player, building.Location, building.CenterPosition, maxAttempts)
	end)
end

AddToRebuildQueue = function(building, player, loc, pos, maxAttempts)
	local queueItem = {
		Actor = building,
		Player = player,
		Location = loc,
		CenterPosition = pos,
		AttemptsRemaining = maxAttempts,
		MaxAttempts = maxAttempts
	}

	BuildingQueues[player.InternalName][#BuildingQueues[player.InternalName] + 1] = queueItem

	-- if the queue was empty, start rebuild immediately
	if #BuildingQueues[player.InternalName] == 1 then
		RebuildNextBuilding(player)
	end
end

RebuildNextBuilding = function(player)
	if BuildingQueues[player.InternalName] == nil or #BuildingQueues[player.InternalName] == 0 then
		return
	end

	local queueItem = BuildingQueues[player.InternalName][1]
	RebuildBuilding(queueItem)
end

RebuildBuilding = function(queueItem)
	local buildTime = math.ceil(Actor.BuildTime(queueItem.Actor.Type) * StructureBuildTimeMultipliers[Difficulty])

	Trigger.AfterDelay(buildTime, function()
		table.remove(BuildingQueues[queueItem.Player.InternalName], 1)

		-- rebuild if no units are nearby (potentially blocking), no enemy buildings are nearby, and friendly buildings are in the area (but nothing friendly in the same cell)
		if CanRebuild(queueItem) then
			local b = Actor.Create(queueItem.Actor.Type, true, { Owner = queueItem.Player, Location = queueItem.Location })
			AutoRepairBuilding(b, queueItem.Player)
			AutoRebuildBuilding(b, queueItem.Player, queueItem.MaxAttempts)
			RestoreSquadProduction(queueItem.Actor, b)

		-- otherwise add to back of queue (if attempts remaining)
		elseif queueItem.AttemptsRemaining > 1 then
			queueItem.AttemptsRemaining = queueItem.AttemptsRemaining - 1
			BuildingQueues[queueItem.Player.InternalName][#BuildingQueues[queueItem.Player.InternalName] + 1] = queueItem
		end

		RebuildNextBuilding(queueItem.Player)
	end)
end

CanRebuild = function(queueItem)
	if not HasConyard(queueItem.Player) then
		return false
	end

	local loc = queueItem.Location
	local pos = queueItem.CenterPosition

	-- require being in conyard build radius
	if EnforceAiBuildRadius then
		local nearbyConyards = Map.ActorsInCircle(queueItem.CenterPosition, WDist.New(20480), function(a)
			return a.Owner == queueItem.Player
		end)

		if #nearbyConyards == 0 then
			return false
		end
	end

	local topLeft = WPos.New(pos.X - 2048, pos.Y - 2048, 0)
	local bottomRight = WPos.New(pos.X + 2048, pos.Y + 2048, 0)

	local nearbyUnits = Map.ActorsInBox(topLeft, bottomRight, function(a)
		return not a.IsDead and a.HasProperty("Move") and not a.HasProperty("Land")
	end)

	-- require no nearby units (stops building on top of them)
	if #nearbyUnits > 0 then
		return false
	end

	topLeft = WPos.New(pos.X - 8192, pos.Y - 8192, 0)
	bottomRight = WPos.New(pos.X + 8192, pos.Y + 8192, 0)
	local nearbyBuildings = Map.ActorsInBox(topLeft, bottomRight, function(a)
		return not a.IsDead and a.Owner == queueItem.Player and a.HasProperty("StartBuildingRepairs") and not a.HasProperty("Attack") and a.Type ~= "silo" and a.Type ~= "silo.td" and a.Type ~= "silo.scrin"
	end)

	-- require an owned building nearby
	if #nearbyBuildings == 0 then
		return false
	end

	local nearbyEnemyBuildings = Map.ActorsInBox(topLeft, bottomRight, function(a)
		return not a.IsDead and IsMissionPlayer(a.Owner) and a.HasProperty("StartBuildingRepairs")
	end)

	-- require no player owned buildings nearby
	if #nearbyEnemyBuildings > 0 then
		return false
	end

	return true
end

RestoreSquadProduction = function(oldBuilding, newBuilding)
	if Squads == nil then
		return
	end

	for squadName,squad in pairs(Squads) do
		if squad.ProducerActors ~= nil then
			for queue,actors in pairs(squad.ProducerActors) do
				if actors ~= nil then
					Utils.Do(actors, function(a)
						if a == oldBuilding then
							table.insert(actors, newBuilding)
						end
					end)
				end
			end
		end
	end
end

-- returns true if player has one of any of the specified actor types
HasOneOf = function(player, types)
	local count = 0

	Utils.Do(types, function(name)
		if #player.GetActorsByType(name) > 0 then
			count = count + 1
		end
	end)

	return count > 0
end

-- make specified units have a chance to swap targets when attacked instead of chasing one target endlessly
TargetSwapChance = function(unit, chance, isMissionPlayerFunc)
	if isMissionPlayerFunc == nil then
		isMissionPlayerFunc = function(p) return IsMissionPlayer(p) end
	end
	Trigger.OnDamaged(unit, function(self, attacker, damage)
		if isMissionPlayerFunc(self.Owner) or not isMissionPlayerFunc(attacker.Owner) then
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

CallForHelpOnDamagedOrKilled = function(actor, range, filter, validAttackingPlayerFunc)
	if validAttackingPlayerFunc == nil then
		validAttackingPlayerFunc = function(p) return IsMissionPlayer(p) end
	end
	Trigger.OnDamaged(actor, function(self, attacker, damage)
		if validAttackingPlayerFunc(attacker.Owner) then
			CallForHelp(self, range, filter, validAttackingPlayerFunc)
		end
	end)
	Trigger.OnKilled(actor, function(self, killer)
		if validAttackingPlayerFunc(killer.Owner) then
			CallForHelp(self, range, filter, validAttackingPlayerFunc)
		end
	end)
end

CallForHelp = function(self, range, filter, validAttackingPlayerFunc)
	if validAttackingPlayerFunc == nil then
		validAttackingPlayerFunc = function(p) return IsMissionPlayer(p) end
	end
	if IsMissionPlayer(self.Owner) then
		return
	end

	local selfId = tostring(self)
	if AlertedUnits[selfId] == nil then
		if not self.IsDead then
			AlertedUnits[selfId] = true
			if filter(self) then
				self.Stop()
				IdleHunt(self)
			end
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

--- attack squad functionality, requires Squads object to be defined properly in the mission script file
InitAttackSquad = function(squad, player, targetPlayer)
	squad.Player = player
	squad.WaveTotalCost = 0
	squad.WaveStartTime = DateTime.GameTime

	if squad.InitTime == nil then
		squad.InitTime = DateTime.GameTime
	end

	if targetPlayer ~= nil then
		squad.TargetPlayer = targetPlayer
	elseif #MissionPlayers > 0 then
		squad.TargetPlayer = MissionPlayers[1]
	else
		return
	end

	if squad.QueueProductionStatuses == nil then
		squad.QueueProductionStatuses = { }
	end

	if squad.IdleUnits == nil then
		squad.IdleUnits = { }
	end

	if IsSquadInProduction(squad) then
		return
	end

	-- make sure ActiveCondition function returns true (if it exists)
	local isActive = squad.ActiveCondition == nil or squad.ActiveCondition()

	if isActive then

		-- filter possible compositions based on game time
		local validCompositions = Utils.Where(squad.Units[Difficulty], function(composition)
			return (composition.MinTime == nil or DateTime.GameTime >= composition.MinTime + squad.InitTime) and (composition.MaxTime == nil or DateTime.GameTime < composition.MaxTime + squad.InitTime)
		end)

		if #validCompositions > 0 then
			-- randomly select a unit composition for next wave
			squad.QueuedUnits = Utils.Random(validCompositions)

			local queuesForComposition = GetQueuesForComposition(squad.QueuedUnits)

			-- go through each queue for the current difficulty and start producing the first unit
			Utils.Do(queuesForComposition, function(queue)
				ProduceNextAttackSquadUnit(squad, queue, 1)
			end)
		else
			Trigger.AfterDelay(DateTime.Seconds(15), function()
				InitAttackSquad(squad, player, targetPlayer)
			end)
		end
	else
		Trigger.AfterDelay(DateTime.Seconds(15), function()
			InitAttackSquad(squad, player, targetPlayer)
		end)
	end
end

GetQueuesForComposition = function(composition)
	local queues = { }

	for k,v in pairs(composition) do
		if k ~= "MinTime" and k ~= "MaxTime" then
			table.insert(queues, k)
		end
	end

	return queues
end

InitAirAttackSquad = function(squad, player, targetPlayer, targetTypes)
	squad.IsAir = true
	squad.AirTargetTypes = targetTypes
	InitAttackSquad(squad, player, targetPlayer)
end

InitNavalAttackSquad = function(squad, player, targetPlayer)
	squad.IsNaval = true
	InitAttackSquad(squad, player, targetPlayer)
end

ProduceNextAttackSquadUnit = function(squad, queue, unitIndex)
	local units = squad.QueuedUnits[queue]

	if units == nil then
		units = { }
	end

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

			-- delay (mostly used for Nod to allow the last unit to arrive at the Airstrip)
			-- if the interval is short enough this may result in units from the subsequent squad being sent with the one that is delayed
			Trigger.AfterDelay(dispatchDelay, function()
				SendAttackSquad(squad)
			end)

			local ticksUntilNext

			if squad.Interval ~= nil and squad.Interval[Difficulty] ~= nil then
				ticksUntilNext = squad.Interval[Difficulty]
			else
				ticksUntilNext = CalculateInterval(squad)
			end

			-- every harvester killed delays the next wave
			if HarvesterDeathStacks[squad.Player.InternalName] ~= nil and HarvesterDeathStacks[squad.Player.InternalName] > 0 then
				HarvesterDeathStacks[squad.Player.InternalName] = HarvesterDeathStacks[squad.Player.InternalName] - 1
				ticksUntilNext = ticksUntilNext + HarvesterDeathDelayTime[Difficulty]
			end

			Trigger.AfterDelay(ticksUntilNext, function()
				InitAttackSquad(squad, squad.Player, squad.TargetPlayer)
			end)
		end
	-- if more units to build, set them to produce after delay equal to their build time (with difficulty multiplier applied)
	else
		squad.QueueProductionStatuses[queue] = true
		local nextUnit = units[unitIndex]

		if type(nextUnit) == "table" then
			nextUnit = Utils.Random(nextUnit)
		end

		local buildTime = math.ceil(Actor.BuildTime(nextUnit) * UnitBuildTimeMultipliers[Difficulty])

		-- after the build time has elapsed
		Trigger.AfterDelay(buildTime, function()
			local producer = nil

			-- find appropriate producer actor (either the first/random specific actor, or if not found, randomly selected of from specified types)
			if squad.ProducerActors ~= nil and squad.ProducerActors[queue] ~= nil then
				local producerActors = squad.ProducerActors[queue]
				if squad.RandomProducerActor then
					producerActors = Utils.Shuffle(producerActors)
				end

				Utils.Do(producerActors, function(a)
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

				-- add production trigger once for the producer (once for every owner, as the producer may be captured)
				if OnProductionTriggers[producerId] == nil or OnProductionTriggers[producerId] ~= producer.Owner.InternalName then
					OnProductionTriggers[producerId] = tostring(producer.Owner.InternalName)

					-- add produced unit to list of idle units for the squad
					Trigger.OnProduction(producer, function(p, produced)
						if produced.Owner == squad.Player then
							HandleProducedSquadUnit(produced, producerId, squad)
						end
					end)
				end

				local engineersNearby = Map.ActorsInCircle(producer.CenterPosition, WDist.New(2730), function(a)
					return not a.IsDead and (a.Type == "e6" or a.Type == "n6" or a.Type == "s6" or a.Type == "mast") and a.Owner ~= producer.Owner
				end)

				if #engineersNearby == 0 then
					producer.Produce(nextUnit)
					squad.WaveTotalCost = squad.WaveTotalCost + Actor.Cost(nextUnit)
				end
			end

			-- start producing the next unit
			ProduceNextAttackSquadUnit(squad, queue, unitIndex + 1)
		end)
	end
end

HandleProducedSquadUnit = function(produced, producerId, squad)
	local isHarvester = false

	-- we don't want to add harvesters to squads, which are produced when replacements are needed
	Utils.Do(HarvesterTypes, function(harvesterType)
		if produced.Type == harvesterType then
			isHarvester = true
		end
	end)

	if not isHarvester then
		InitSquadAssignmentQueueForProducer(producerId, squad)

		-- assign unit to IdleUnits of the next squad in the assignment queue of the producer
		if SquadAssignmentQueue[produced.Owner.InternalName][producerId][1] ~= nil then
			local assignedSquad = SquadAssignmentQueue[produced.Owner.InternalName][producerId][1]
			SquadAssignmentQueue[produced.Owner.InternalName][producerId][1].IdleUnits[#assignedSquad.IdleUnits + 1] = produced
			table.remove(SquadAssignmentQueue[produced.Owner.InternalName][producerId], 1)
		elseif produced.HasProperty("Hunt") then
			produced.Hunt()
		end

		if produced.HasProperty("HasPassengers") and not produced.IsDead then
			Trigger.OnPassengerExited(produced, function(transport, passenger)
				AssaultPlayerBaseOrHunt(passenger, squad.TargetPlayer)
			end)
		end

		if squad.OnProducedAction ~= nil then
			squad.OnProducedAction(produced)
		end

		TargetSwapChance(produced, 10)
	end
end

-- on finishing production of a squad of units for an attack, calculate how long to wait to produce the next squad based on desired value per second
CalculateInterval = function(squad)
	local ticksSpentProducing = DateTime.GameTime - squad.WaveStartTime

	if squad.AttackValuePerSecond ~= nil and squad.AttackValuePerSecond[Difficulty] ~= nil then
		local desiredValue = 0

		Utils.Do(squad.AttackValuePerSecond[Difficulty], function(item)
			if DateTime.GameTime >= item.MinTime and item.Value > desiredValue then
				desiredValue = item.Value
			end
		end)

		local ticks = ((25 * squad.WaveTotalCost) - (desiredValue * ticksSpentProducing)) / desiredValue
		return math.max(math.floor(ticks), 0)
	else
		return ticksSpentProducing
	end
end

-- used to make sure multiple squads being produced from the same structure don't get mixed up
-- also split by player to prevent these getting jumbled if producer owner changes
AddToSquadAssignmentQueue = function(producerId, squad)
	InitSquadAssignmentQueueForProducer(producerId, squad)
	SquadAssignmentQueue[squad.Player.InternalName][producerId][#SquadAssignmentQueue[squad.Player.InternalName][producerId] + 1] = squad
end

InitSquadAssignmentQueueForProducer = function(producerId, squad)
	if SquadAssignmentQueue[squad.Player.InternalName] == nil then
		SquadAssignmentQueue[squad.Player.InternalName] = { }
	end
	if SquadAssignmentQueue[squad.Player.InternalName][producerId] == nil then
		SquadAssignmentQueue[squad.Player.InternalName][producerId] = { }
	end
end

IsSquadInProduction = function(squad)
	for _, isProducing in pairs(squad.QueueProductionStatuses) do
		if isProducing then
			return true
		end
	end

	return false
end

SendAttackSquad = function(squad)
	Utils.Do(squad.IdleUnits, function(a)
		if not a.IsDead then
			a.Stop()
		end
	end)

	if squad.IsAir ~= nil and squad.IsAir then
		Utils.Do(squad.IdleUnits, function(a)
			if not a.IsDead then
				InitAttackAircraft(a, squad.TargetPlayer, squad.AirTargetTypes)
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
						squadLeader = a
					end

					-- if squad leader, queue attack move to each attack path waypoint
					if squadLeader == nil or a == squadLeader then
						Utils.Do(attackPath, function(w)
							a.AttackMove(w, 3)
						end)

						if squad.IsNaval ~= nil and squad.IsNaval then
							IdleHunt(a)
						else
							AssaultPlayerBaseOrHunt(a, squad.TargetPlayer)
						end

						-- on damaged or killed
						Trigger.OnDamaged(a, function(self, attacker, damage)
							ClearSquadLeader(squadLeader)
						end)

						Trigger.OnKilled(a, function(self, attacker, damage)
							ClearSquadLeader(squadLeader)
						end)

					-- if not squad leader, follow the leader
					else
						SquadLeaders[actorId] = squadLeader
						FollowSquadLeader(a, squad)

						-- if damaged (stop guarding, attack move to enemy base)
						Trigger.OnDamaged(a, function(self, attacker, damage)
							ClearSquadLeader(SquadLeaders[actorId])
						end)
					end
				end
			else
				if squad.IsNaval ~= nil and squad.IsNaval then
					IdleHunt(a)
				else
					AssaultPlayerBaseOrHunt(a, squad.TargetPlayer)
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

FollowSquadLeader = function(actor, squad)
	if not actor.IsDead and actor.IsInWorld then
		local actorId = tostring(actor)

		if SquadLeaders[actorId] ~= nil and not SquadLeaders[actorId].IsDead then
			local possibleCells = Utils.ExpandFootprint({ SquadLeaders[actorId].Location }, true)
			local cell = Utils.Random(possibleCells)
			actor.Stop()
			actor.AttackMove(cell, 1)

			Trigger.AfterDelay(Utils.RandomInteger(35,65), function()
				FollowSquadLeader(actor, squad)
			end)
		else
			actor.Stop()
			Trigger.ClearAll(actor)
			AssaultPlayerBaseOrHunt(actor, squad.TargetPlayer)
		end
	end
end

SetupRefAndSilosCaptureCredits = function(player)
	local silosAndRefineries = player.GetActorsByTypes(CashRewardOnCaptureTypes)
	Utils.Do(silosAndRefineries, function(a)
		Trigger.OnCapture(a, function(self, captor, oldOwner, newOwner)
			newOwner.Cash = newOwner.Cash + CapturedCreditsAmount
			Media.FloatingText("+$" .. CapturedCreditsAmount, self.CenterPosition, 30, newOwner.Color)
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
		local buildTime = math.ceil(Actor.BuildTime(harvType) * UnitBuildTimeMultipliers[Difficulty])
		local randomExtraTime = Utils.RandomInteger(DateTime.Seconds(5), DateTime.Seconds(15))

		Trigger.AfterDelay(buildTime + randomExtraTime, function()
			local producers = player.GetActorsByTypes(FactoryTypes)

			if #producers > 0 then
				local producer = Utils.Random(producers)
				producer.Produce(harvType)
			end
		end)

		AddHarvesterDeathStack(player)
	end)
end

AddHarvesterDeathStack = function(player)
	if HarvesterDeathStacks[player.InternalName] == nil then
		HarvesterDeathStacks[player.InternalName] = 0
	end
	HarvesterDeathStacks[player.InternalName] = HarvesterDeathStacks[player.InternalName] + 1
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

SetupReveals = function(revealPoints, cameraType)
	if cameraType == nil then
		cameraType = "smallcamera"
	end
	Utils.Do(revealPoints, function(p)
		Trigger.OnEnteredProximityTrigger(p.CenterPosition, WDist.New(11 * 1024), function(a, id)
			if IsMissionPlayer(a.Owner) and a.Type ~= cameraType then
				Trigger.RemoveProximityTrigger(id)
				local camera = Actor.Create(cameraType, true, { Owner = a.Owner, Location = p.Location })
				Trigger.AfterDelay(DateTime.Seconds(4), function()
					camera.Destroy()
				end)
			end
		end)
	end)
end

AdjustStartingCash = function(player)
	if player == nil then
		for _, p in pairs(MissionPlayers) do
			AdjustStartingCash(p)
		end
	else
		player.Cash = player.Cash + CashAdjustments[Difficulty]
	end
end

AdjustTimeForGameSpeed = function(ticks)
	if (GameSpeed == "default") then
		return ticks
	end

	if (GameSpeed == "fastest") then
		return ticks * 2
	end

	if (GameSpeed == "faster") then
		return ticks * 1.33
	end

	if (GameSpeed == "fast") then
		return ticks * 1.14
	end

	if (GameSpeed == "slower") then
		return ticks * 0.8
	end

	if (GameSpeed == "slowest") then
		return ticks * 0.5
	end

	return ticks
end

PanToPos = function(targetPos, speed)
	local cameraPos = Camera.Position
	local newX = cameraPos.X
	local newY = cameraPos.Y

	if newX < targetPos.X then
		if newX + speed > targetPos.X then
			newX = targetPos.X
		else
			newX = newX + speed
		end
	elseif newX > targetPos.X then
		if newX - speed < targetPos.X then
			newX = targetPos.X
		else
			newX = newX - speed
		end
	end

	if newY < targetPos.Y then
		if newY + speed > targetPos.Y then
			newY = targetPos.Y
		else
			newY = newY + speed
		end
	elseif newY > targetPos.Y then
		if newY - speed < targetPos.Y then
			newY = targetPos.Y
		else
			newY = newY - speed
		end
	end

	Camera.Position = WPos.New(newX, newY, 0)
end

-- Filters

IsGroundHunterUnit = function(actor)
	return not actor.IsDead and actor.HasProperty("Move") and not actor.HasProperty("Land") and actor.HasProperty("Hunt")
end

IsGreeceGroundHunterUnit = function(actor)
	return actor.Owner == Greece and IsGroundHunterUnit(actor) and actor.Type ~= "arty" and actor.Type ~= "cryo" and actor.Type ~= "mgg" and actor.Type ~= "mrj"
end

IsUSSRGroundHunterUnit = function(actor)
	return actor.Owner == USSR and IsGroundHunterUnit(actor) and actor.Type ~= "v2rl" and actor.Type ~= "v3rl" and actor.Type ~= "katy" and actor.Type ~= "grad" and actor.Type ~= "nukc"
end

IsGDIGroundHunterUnit = function(actor)
	return actor.Owner == GDI and (IsGroundHunterUnit(actor) or actor.Type == "jjet") and actor.Type ~= "msam" and actor.Type ~= "memp" and actor.Type ~= "thwk"
end

IsNodGroundHunterUnit = function(actor)
	return actor.Owner == Nod and IsGroundHunterUnit(actor) and actor.Type ~= "mlrs" and actor.Type ~= "arty.nod"
end

IsScrinGroundHunterUnit = function(actor)
	return actor.Owner == Scrin and IsGroundHunterUnit(actor) and actor.Type ~= "mast" and actor.Type ~= "pdgy"
end

-- Units

AlliedT3SupportVehicle = { "mgg", "mrj", "cryo" }
AlliedAdvancedInfantry = { "snip", "enfo", "cryt" }
PrismCannonOrZeus = { "pcan", "zeus" }

SovietMammothVariant = { "4tnk", "4tnk", "4tnk.atomic", "4tnk.erad" }
SovietBasicArty = { "v2rl", "katy" }
SovietAdvancedArty = { "v3rl", "v3rl", "isu" }
TeslaVariant = { "ttnk", "ttra" }
MigOrSukhoi = { "mig", "mig", "suk", "suk.upg" }
HindOrYak = { "hind", "yak" }

HumveeOrGuardianDrone = { "hmmv", "gdrn" }
TOWHumveeOrGuardianDrone = { "hmmv.tow", "gdrn.tow" }
GDIMammothVariant = { "titn.rail", "htnk.ion", "htnk.hover", "htnk.drone" }
ZoneTrooperVariant = { "ztrp", "zrai", "zdef" }
WolverineOrXO = { "wolv", "xo" }

BasicCyborg = { "n1c", "n3c", "n5", "acol" }
AdvancedCyborg = { "rmbc", "enli", "tplr" }
FlameTankHeavyFlameTankOrHowitzer = { "ftnk", "hftk", "howi" }

GunWalkerSeekerOrLacerator = { "gunw", "seek", "lace", "shrw" }
CorrupterDevourerOrDarkener = { "corr", "devo", "dark" }
AtomizerDarkenerOrRuiner = { "atmz", "dark", "ruin" }
TripodVariant = { "tpod", "tpod", "rtpd" }
PacOrDevastator = { "pac", "deva" }

UnitCompositions = {
	Allied = {
		Main = {
			easy = {
				-- 0 to 14 minutes
				{ Infantry = {}, Vehicles = { "jeep", "jeep", "jeep" }, MaxTime = DateTime.Minutes(14) },
				{ Infantry = {}, Vehicles = { "1tnk", "1tnk" }, MaxTime = DateTime.Minutes(14) },
				{ Infantry = { "e3", "e1", "e1", "e1", "e1" }, Vehicles = { "2tnk", "apc" }, MaxTime = DateTime.Minutes(14) },
				{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "2tnk", "ifv.ai" }, MaxTime = DateTime.Minutes(14) },

				-- 14 minutes onwards
				{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "2tnk", "2tnk", "jeep" }, MinTime = DateTime.Minutes(14) },
				{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "2tnk", "ifv.ai", "ptnk" }, MinTime = DateTime.Minutes(14) },
				{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "2tnk", "ifv.ai", AlliedT3SupportVehicle }, MinTime = DateTime.Minutes(14) },
				{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "batf.ai" }, MinTime = DateTime.Minutes(14) },

				{ Infantry = {}, Vehicles = { "rtnk", "rtnk", "rtnk" }, MinTime = DateTime.Minutes(14) },
				{ Infantry = {}, Vehicles = { "ctnk", "ctnk", "ctnk" }, MinTime = DateTime.Minutes(14) },
				{ Infantry = { "e3", "e1", "e1" }, Vehicles = { "2tnk", "ifv.ai", "arty" }, MinTime = DateTime.Minutes(14) },
				{ Infantry = { "seal", "seal", "seal" }, Vehicles = { "ifv.ai", "ifv.ai" }, MinTime = DateTime.Minutes(14) },
			},
			normal = {
				-- 0 to 12 minutes
				{ Infantry = {}, Vehicles = { "jeep", "jeep", "jeep", "jeep"  }, MaxTime = DateTime.Minutes(12) },
				{ Infantry = {}, Vehicles = { "1tnk", "1tnk", "1tnk" }, MaxTime = DateTime.Minutes(12) },
				{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "2tnk", "apc.ai", "rapc" }, MaxTime = DateTime.Minutes(12) },
				{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1" }, Vehicles = { "2tnk", "ifv.ai", "ifv.ai" }, MaxTime = DateTime.Minutes(12) },

				-- 12 minutes onwards
				{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "e3" }, Vehicles = { "2tnk", "ifv.ai", "apc.ai", "rapc.ai" }, MinTime = DateTime.Minutes(12) },
				{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "e3" }, Vehicles = { "2tnk", "2tnk", "ifv.ai", "ptnk" }, MinTime = DateTime.Minutes(12) },
				{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "e3" }, Vehicles = { "2tnk", "2tnk", "ifv.ai", AlliedT3SupportVehicle }, MinTime = DateTime.Minutes(12) },
				{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "batf.ai", "ifv.ai", "ifv.ai" }, MinTime = DateTime.Minutes(12) },

				{ Infantry = {}, Vehicles = { "rtnk", "rtnk", "rtnk", "rtnk" }, MinTime = DateTime.Minutes(12) },
				{ Infantry = {}, Vehicles = { "rapc.ai", "rapc.ai", "jeep", "ifv.ai"  }, MinTime = DateTime.Minutes(12) },
				{ Infantry = {}, Vehicles = { "ctnk", "ctnk", "ctnk", "ctnk" }, MinTime = DateTime.Minutes(12) },
				{ Infantry = { "seal", "seal", "seal", "seal" }, Vehicles = { "ifv.ai", "ifv.ai", "ifv.ai" }, MinTime = DateTime.Minutes(12) },
			},
			hard = {
				-- 0 to 10 minutes
				{ Infantry = {}, Vehicles = { "jeep", "jeep", "jeep", "jeep", "jeep" }, MaxTime = DateTime.Minutes(10) },
				{ Infantry = {}, Vehicles = { "1tnk", "1tnk", "1tnk", "1tnk" }, MaxTime = DateTime.Minutes(10) },
				{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1" }, Vehicles = { "2tnk", "2tnk", "apc.ai", "arty" }, MaxTime = DateTime.Minutes(10) },
				{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "e1", "e1" }, Vehicles = { "2tnk", "2tnk", "ifv.ai", "ifv.ai" }, MaxTime = DateTime.Minutes(10) },

				-- 10 minutes onwards
				{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", AlliedAdvancedInfantry, "e1", "e3" }, Vehicles = { "2tnk", "ifv.ai", "rapc.ai", "rapc.ai", "arty" }, MinTime = DateTime.Minutes(10) },
				{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", AlliedAdvancedInfantry, "e1", "e3" }, Vehicles = { "2tnk", "2tnk", "2tnk", "ifv.ai", "ptnk", "ptnk" }, MinTime = DateTime.Minutes(10) },
				{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", AlliedAdvancedInfantry, "e1", "e3" }, Vehicles = { "2tnk", "2tnk", "2tnk", "ifv.ai", "ptnk", AlliedT3SupportVehicle }, MinTime = DateTime.Minutes(10) },
				{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "e1", "e1", "e3" }, Vehicles = { "batf.ai", "batf.ai", "ifv.ai", "ifv.ai" }, MinTime = DateTime.Minutes(10) },

				{ Infantry = {}, Vehicles = { "rtnk", "rtnk", "rtnk", "rtnk", "rtnk" }, MinTime = DateTime.Minutes(10) },
				{ Infantry = {}, Vehicles = { "rapc.ai", "rapc.ai", "rapc.ai", "jeep", "ifv.ai", "ifv.ai" }, MinTime = DateTime.Minutes(10) },
				{ Infantry = {}, Vehicles = { "ctnk", "ctnk", "ctnk", "ctnk", "ctnk"  }, MinTime = DateTime.Minutes(10) },
				{ Infantry = { "seal", "seal", "seal", "seal", "seal" }, Vehicles = { "ifv.ai", "ifv.ai", "ifv.ai", "ifv.ai" }, MinTime = DateTime.Minutes(10) },

				-- 16 minutes onwards
				{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", AlliedAdvancedInfantry, "e1", "e3", "e1", "e1", "e3" }, Vehicles = { "2tnk", "2tnk", "2tnk", "ifv.ai", AlliedT3SupportVehicle, PrismCannonOrZeus }, MinTime = DateTime.Minutes(16) },
			}
		}
	},
	Soviet = {
		Main = {
			easy = {
				-- 0 to 14 minutes
				{ Infantry = { "e3", "e1", "e1", "e1", "e2", "e4" }, Vehicles = { "3tnk", "btr" }, MaxTime = DateTime.Minutes(14), },

				-- 14 minutes onwards
				{ Infantry = { "e3", "e1", "e1", "shok", "shok", "e1", "e2", "e3", "e4" }, Vehicles = { SovietMammothVariant, "btr.ai", SovietBasicArty }, MinTime = DateTime.Minutes(14), },
				{ Infantry = { "e3", "e1", "e1", "shok", "shok", "e1", "e2", "e3", "e4" }, Vehicles = { SovietMammothVariant, "btr.ai", TeslaVariant }, MinTime = DateTime.Minutes(14), }
			},
			normal = {
				-- 0 to 12 minutes
				{ Infantry = { "e3", "e1", "e1", "e1", "e1", "e2", "e4" }, Vehicles = { "3tnk", "btr.ai", "btr" }, MaxTime = DateTime.Minutes(12), },

				-- 12 minutes onwards
				{ Infantry = { "e3", "e1", "e1", "shok", "shok", "e1", "shok", "e3", "e4" }, Vehicles = { TeslaVariant, "btr.ai", SovietMammothVariant, SovietBasicArty }, MinTime = DateTime.Minutes(12), },
				{ Infantry = { "e3", "e1", "e1", "shok", "shok", "e1", "e2", "e3", "e4" }, Vehicles = { "3tnk", "btr.ai", SovietMammothVariant, SovietBasicArty }, MinTime = DateTime.Minutes(12), },

				-- 15 minutes onwards
				{ Infantry = { "e3", "e1", "e1", "shok", "e8", "e1", "e2", "e3", "e4", "e1", "e1" }, Vehicles = { "3tnk.atomic", "btr.ai", SovietMammothVariant, SovietBasicArty }, MinTime = DateTime.Minutes(15), },
				{ Infantry = { "e3", "e1", "e1", "shok", "shok", "e1", "e2", "e3", "e4", "e1", "e1" }, Vehicles = { "3tnk", "btr.ai", SovietMammothVariant, SovietBasicArty }, MinTime = DateTime.Minutes(15), },
				{ Infantry = { "e3", "e1", "e1", "shok", "shok", "e1", "e2", "e3", "e4", "e1", "e1" }, Vehicles = { "3tnk", "btr.ai", "isu", SovietBasicArty }, MinTime = DateTime.Minutes(15), }
			},
			hard = {
				-- 0 to 10 minutes
				{ Infantry = { "e3", "e1", "e1", "e1", "e1", "e1", "e2", "e3", "e4" }, Vehicles = { "3tnk", "btr.ai", "3tnk" }, MaxTime = DateTime.Minutes(10), },

				-- 10 to 16 minutes
				{ Infantry = { "e3", "e1", "e1", "e3", "shok", "e1", "shok", "e1", "e2", "e3", "e4", "e1" }, Vehicles = { "3tnk", SovietMammothVariant, "btr.ai", TeslaVariant, SovietBasicArty }, MinTime = DateTime.Minutes(10), MaxTime = DateTime.Minutes(16), },
				{ Infantry = { "e3", "e1", "shok", "e3", "shok", "e1", "shok", "e1", "shok", "e3", "e4", "e1" }, Vehicles = { TeslaVariant, SovietMammothVariant, "btr.ai", TeslaVariant, SovietBasicArty }, MinTime = DateTime.Minutes(10), MaxTime = DateTime.Minutes(16), },

				-- 16 minutes onwards
				{ Infantry = { "e3", "e1", "e1", "e3", "shok", "e1", "e1", "cmsr", "e1", "e2", "e3", "e4", "e1", "e1", "e1", "e1", "shok" }, Vehicles = { "3tnk", SovietMammothVariant, "btr.ai", TeslaVariant, SovietBasicArty, SovietAdvancedArty }, MinTime = DateTime.Minutes(16), },
				{ Infantry = { "e3", "e1", "e1", "e3", "ttrp", "e1", "ttrp", "e1", "cmsr", "e2", "e3", "e4", "e1", "e1", "e1", "e1" }, Vehicles = { "3tnk", SovietMammothVariant, "btr.ai", TeslaVariant, SovietBasicArty, SovietAdvancedArty }, MinTime = DateTime.Minutes(16), },
				{ Infantry = { "e3", "e1", "e1", "e3", "e8", "e1", "e8", "e1", "deso", "deso", "e2", "e3", "e4", "e1", "e1", "e1", "e1" }, Vehicles = { "3tnk.atomic", "3tnk.atomic", SovietMammothVariant, "btr.ai", "apoc", "v3rl" }, MinTime = DateTime.Minutes(16), },
			}
		}
	},
	GDI = {
		Main = {
			easy = {
				-- 0 to 14 minutes
				{ Infantry = {}, Vehicles = { HumveeOrGuardianDrone, HumveeOrGuardianDrone, HumveeOrGuardianDrone }, MaxTime = DateTime.Minutes(14) },
				{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n2"}, Vehicles = { "mtnk", "vulc" }, MaxTime = DateTime.Minutes(14) },
				{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n2" }, Vehicles = { "mtnk", HumveeOrGuardianDrone }, MaxTime = DateTime.Minutes(14) },

				-- 14 minutes onwards
				{ Infantry = {}, Vehicles = { HumveeOrGuardianDrone, HumveeOrGuardianDrone, HumveeOrGuardianDrone, HumveeOrGuardianDrone }, MinTime = DateTime.Minutes(14) },
				{ Infantry = { "n3", "n1", "n1" }, Vehicles = { "mtnk", "vulc", "msam" }, MinTime = DateTime.Minutes(14) },
				{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1" }, Vehicles = { "mtnk", "mtnk", "hmmv" }, MinTime = DateTime.Minutes(14) },
				{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1" }, Vehicles = { "mtnk", "vulc", "msam" }, MinTime = DateTime.Minutes(14) },

				{ Infantry = { "jjet", "jjet", "bjet" }, Vehicles = { TOWHumveeOrGuardianDrone, TOWHumveeOrGuardianDrone }, MinTime = DateTime.Minutes(14) },
				{ Infantry = {}, Vehicles = { "hsam", "hsam" }, MinTime = DateTime.Minutes(14) },
				{ Infantry = { "n1", "n1", "n1", "n1", "n3" }, Vehicles = { GDIMammothVariant, WolverineOrXO }, MinTime = DateTime.Minutes(14) },
			},
			normal = {
				-- 0 to 12 minutes
				{ Infantry = {}, Vehicles = { HumveeOrGuardianDrone, HumveeOrGuardianDrone, HumveeOrGuardianDrone, HumveeOrGuardianDrone }, MaxTime = DateTime.Minutes(12) },
				{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n2" }, Vehicles = { "mtnk", "vulc", HumveeOrGuardianDrone }, MaxTime = DateTime.Minutes(12) },
				{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n2" }, Vehicles = { "mtnk", "mtnk", HumveeOrGuardianDrone }, MaxTime = DateTime.Minutes(12) },

				-- 12 minutes onwards
				{ Infantry = {}, Vehicles = { "mtnk", "vulc", HumveeOrGuardianDrone, "mtnk" }, MinTime = DateTime.Minutes(12) },
				{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", "n1", "n1", "n3" }, Vehicles = { "mtnk", "vulc", "apc2.gdiai", "vulc.ai" }, MinTime = DateTime.Minutes(12) },
				{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", "n1", "n1", "n3" }, Vehicles = { "mtnk", "mtnk", "vulc", "apc2.gdiai", "msam" }, MinTime = DateTime.Minutes(12) },

				{ Infantry = { "jjet", "jjet", "jjet", "bjet" }, Vehicles = { TOWHumveeOrGuardianDrone, TOWHumveeOrGuardianDrone, TOWHumveeOrGuardianDrone }, MinTime = DateTime.Minutes(12) },
				{ Infantry = {}, Vehicles = { "hsam", "hsam", "hsam", "hsam" }, MinTime = DateTime.Minutes(12) },
				{ Infantry = { "n1", "n1", "n3", "n1", "n1", "n1" }, Vehicles = { GDIMammothVariant, "vulc", "hsam", WolverineOrXO, WolverineOrXO }, MinTime = DateTime.Minutes(12) },

				-- 18 minutes onwards
				{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", ZoneTrooperVariant, ZoneTrooperVariant }, Vehicles = { "mtnk", GDIMammothVariant, "msam", "vulc" }, MinTime = DateTime.Minutes(16) },
				{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", "n1", "n1" }, Vehicles = { "vulc.ai", "disr", "disr" }, MinTime = DateTime.Minutes(18) },
				{ Infantry = {}, Vehicles = { "memp" }, MinTime = DateTime.Minutes(18) },
				{ Infantry = { ZoneTrooperVariant, ZoneTrooperVariant, ZoneTrooperVariant }, Vehicles = { GDIMammothVariant, GDIMammothVariant, WolverineOrXO }, MinTime = DateTime.Minutes(16) },
			},
			hard = {
				-- 0 to 10 minutes
				{ Infantry = {}, Vehicles = { HumveeOrGuardianDrone, HumveeOrGuardianDrone, HumveeOrGuardianDrone, HumveeOrGuardianDrone, HumveeOrGuardianDrone }, MaxTime = DateTime.Minutes(10) },
				{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", "n2", "n2" }, Vehicles = { "mtnk", "mtnk", "msam", "vulc", "vulc.ai"  }, MaxTime = DateTime.Minutes(10) },
				{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", "n2", "n2" }, Vehicles = { "mtnk", "mtnk", HumveeOrGuardianDrone, HumveeOrGuardianDrone }, MaxTime = DateTime.Minutes(10) },

				-- 10 minutes onwards
				{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", "n1", "n1", "n3", "n1", "n1", "n1" }, Vehicles = { "mtnk", "mtnk", "vulc", "vulc.ai", "jugg"  }, MinTime = DateTime.Minutes(10) },
				{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", "n1", "n1", "jjet", "n1", "n3", "n1", "n1", "n1" }, Vehicles = { "mtnk", "vulc", "vulc.ai", "vulc.ai", "msam" }, MinTime = DateTime.Minutes(10) },
				{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", "n1", "n1", "jjet", "n1", "n3", "n1", "n1", "n1" }, Vehicles = { "titn", GDIMammothVariant, "mtnk", "msam", "vulc" }, MinTime = DateTime.Minutes(10) },

				{ Infantry = { "jjet", "jjet", "jjet", "bjet", "bjet" }, Vehicles = { TOWHumveeOrGuardianDrone, TOWHumveeOrGuardianDrone, TOWHumveeOrGuardianDrone, TOWHumveeOrGuardianDrone }, MinTime = DateTime.Minutes(10) },
				{ Infantry = {}, Vehicles = { "hsam", "hsam", "hsam", "hsam", "hsam" }, MinTime = DateTime.Minutes(10) },
				{ Infantry = { "n1", "n1", "n3", "n1", "n1", "n1", "n1", "n1", "n3", "n1", "n1" }, Vehicles = { "htnk", GDIMammothVariant, "hsam", "vulc", WolverineOrXO, WolverineOrXO, WolverineOrXO }, MinTime = DateTime.Minutes(10) },

				-- 16 minutes onwards
				{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", "n1", ZoneTrooperVariant, ZoneTrooperVariant, ZoneTrooperVariant }, Vehicles = { "mtnk", "mtnk", GDIMammothVariant, "msam", "vulc" }, MinTime = DateTime.Minutes(16) },
				{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", "n1", "n1", "n1", "n1", "n3" }, Vehicles = { "vulc.ai", "disr", "disr", "disr" }, MinTime = DateTime.Minutes(16) },
				{ Infantry = { "n3", "rmbo", "n3", "n1", "n1", "n1", "n1", "n1", "n3", "n1", "n1", "n3" }, Vehicles = { GDIMammothVariant, GDIMammothVariant, "msam", "msam", "vulc" }, MinTime = DateTime.Minutes(16) },
				{ Infantry = {}, Vehicles = { "memp", "memp" }, MinTime = DateTime.Minutes(16) },
				{ Infantry = { ZoneTrooperVariant, ZoneTrooperVariant, ZoneTrooperVariant, ZoneTrooperVariant }, Vehicles = { GDIMammothVariant, GDIMammothVariant, GDIMammothVariant, WolverineOrXO, WolverineOrXO }, MinTime = DateTime.Minutes(16) },
			}
		}
	},
	Nod = {
		Main = {
			easy = {
				-- 0 to 14 minutes
				{ Infantry = {}, Vehicles = { "bike", "bike" }, MaxTime = DateTime.Minutes(14) },
				{ Infantry = {}, Vehicles = { "bggy", "bike" }, MaxTime = DateTime.Minutes(14) },
				{ Infantry = { "n3", "n1", "n1" }, Vehicles = { "bggy", "bggy" }, MaxTime = DateTime.Minutes(14) },

				-- 14 minutes onwards
				{ Infantry = {}, Vehicles = { "bike", "bike", "bike" }, MinTime = DateTime.Minutes(14) },
				{ Infantry = { "n3", "n1", "n1", "n1", "n1", "n1" }, Vehicles = { "ltnk", "ltnk" }, MinTime = DateTime.Minutes(14) },
				{ Infantry = { "n3", "n1", "n4", "n1", "n1", "n1" }, Vehicles = { FlameTankHeavyFlameTankOrHowitzer, FlameTankHeavyFlameTankOrHowitzer }, MinTime = DateTime.Minutes(14) },
				{ Infantry = { BasicCyborg, BasicCyborg, BasicCyborg }, Vehicles = { "ltnk" }, MinTime = DateTime.Minutes(14) },
			},
			normal = {
				-- 0 to 12 minutes
				{ Infantry = {}, Vehicles = { "bggy", "bike", "bike" }, MaxTime = DateTime.Minutes(12) },
				{ Infantry = { "n3", "n1", "n1", "n4" }, Vehicles = { "bggy", "bggy", "bike" }, MaxTime = DateTime.Minutes(12) },
				{ Infantry = { "n3", "n1", "n1", "n4" }, Vehicles = { "ltnk" }, MaxTime = DateTime.Minutes(12) },

				-- 12 minutes onwards
				{ Infantry = {}, Vehicles = { "bggy", "bike", "bike", "stnk.nod" }, MinTime = DateTime.Minutes(12) },
				{ Infantry = { "n3", "n1", "n1", "n4", "n1", "bh", "n1", "n1", "n1", "n1", "n1" }, Vehicles = { "ltnk", "ltnk" }, MinTime = DateTime.Minutes(12) },
				{ Infantry = { "n3", "n1", "n1", "n4", "n1", "n1", "n1", "n1", "n1", "n1" }, Vehicles = { "ltnk", "arty.nod" }, MinTime = DateTime.Minutes(12) },
				{ Infantry = { AdvancedCyborg, BasicCyborg, BasicCyborg, BasicCyborg, BasicCyborg }, Vehicles = { FlameTankHeavyFlameTankOrHowitzer, "ltnk" }, MinTime = DateTime.Minutes(12) },
			},
			hard = {
				-- 0 to 10 minutes
				{ Infantry = {}, Vehicles = { "bike", "bike", "bike", "bike" }, MaxTime = DateTime.Minutes(10) },
				{ Infantry = { "n3", "n1", "n1", "n1", "n4", "n1", "n1" }, Vehicles = { "bggy", "bggy", "bike", "bike" }, MaxTime = DateTime.Minutes(10) },
				{ Infantry = { "n3", "n1", "n1", "n4", "n1", "n1" }, Vehicles = { "ltnk", "bggy", "bike" }, MaxTime = DateTime.Minutes(10) },

				-- 10 minutes onwards
				{ Infantry = {}, Vehicles = { "stnk.nod", "stnk.nod", "stnk.nod", "sapc.ai" }, MinTime = DateTime.Minutes(10) },
				{ Infantry = { "n3", "n1", "n1", "n1", "n1", "n4", "n3", "bh", "n1", "n1", "n1", "n1", "n1" }, Vehicles = { "ltnk", "ltnk", FlameTankHeavyFlameTankOrHowitzer, "arty.nod" }, MinTime = DateTime.Minutes(10) },
				{ Infantry = { "n3", "n1", "n1", "n1", "n4", "n1", "n3", "n1", "n1", "n1", "n1", "n1", "n1" }, Vehicles = { "ltnk", "mlrs", "arty.nod", FlameTankHeavyFlameTankOrHowitzer }, MinTime = DateTime.Minutes(10) },
				{ Infantry = { "tplr", AdvancedCyborg, AdvancedCyborg, BasicCyborg, BasicCyborg, BasicCyborg, BasicCyborg, BasicCyborg }, Vehicles = { "ltnk", FlameTankHeavyFlameTankOrHowitzer, "ltnk" }, MinTime = DateTime.Minutes(10) },

				-- 16 minutes onwards
				{ Infantry = { AdvancedCyborg, AdvancedCyborg, AdvancedCyborg, BasicCyborg, BasicCyborg, BasicCyborg, BasicCyborg, BasicCyborg, BasicCyborg, BasicCyborg }, Vehicles = { "ltnk", "ltnk", FlameTankHeavyFlameTankOrHowitzer }, MinTime = DateTime.Minutes(16) },
			}
		}
	},
	Scrin = {
		Main = {
			easy = {
				-- 0 to 14 minutes
				{ Infantry = { "s3", "s1", "s1", "s1", "s3" }, Vehicles = { "intl.ai2", GunWalkerSeekerOrLacerator }, MaxTime = DateTime.Minutes(14), },

				-- 14 minutes onwards
				{ Infantry = { "s3", "s1", "s1", "s1", "s3" }, Vehicles = { "intl.ai2", "intl.ai2", GunWalkerSeekerOrLacerator, GunWalkerSeekerOrLacerator, CorrupterDevourerOrDarkener }, MinTime = DateTime.Minutes(14), },
			},
			normal = {
				-- 0 to 12 minutes
				{ Infantry = { "s3", "s1", "s1", "s1", "s3", "s1" }, Vehicles = { "intl.ai2", "intl.ai2", GunWalkerSeekerOrLacerator }, MaxTime = DateTime.Minutes(12), },

				-- 12 to 15 minutes
				{ Infantry = { "s3", "s1", "s1", "s1", "s3", "s1", "s4", "s4" }, Vehicles = { "intl.ai2", "intl.ai2", GunWalkerSeekerOrLacerator, CorrupterDevourerOrDarkener, CorrupterDevourerOrDarkener, GunWalkerSeekerOrLacerator, GunWalkerSeekerOrLacerator }, MinTime = DateTime.Minutes(12), MaxTime = DateTime.Minutes(15), },

				-- 15 minutes onwards
				{ Infantry = { "s3", "s1", "s1", "s1", "s1", "s1", "s2", "s2", "s3" }, Vehicles = { "intl.ai2", "tpod", GunWalkerSeekerOrLacerator, CorrupterDevourerOrDarkener, CorrupterDevourerOrDarkener, GunWalkerSeekerOrLacerator, "tpod", GunWalkerSeekerOrLacerator }, MinTime = DateTime.Minutes(15), },
			},
			hard = {
				-- 0 to 10 minutes
				{ Infantry = { "s3", "s1", "s1", "s1", "s3", "s3", "s4" }, Vehicles = { "intl.ai2", "intl.ai2", GunWalkerSeekerOrLacerator, GunWalkerSeekerOrLacerator }, MaxTime = DateTime.Minutes(10), },

				-- 10 to 13 minutes
				{ Infantry = { "s3", "s1", "s1", "s1", "s1", "s1", "s2", "s2", "s3", "s3" }, Vehicles = { "intl.ai2", "intl.ai2", GunWalkerSeekerOrLacerator, CorrupterDevourerOrDarkener, CorrupterDevourerOrDarkener, GunWalkerSeekerOrLacerator, "tpod", GunWalkerSeekerOrLacerator }, MinTime = DateTime.Minutes(10), MaxTime = DateTime.Minutes(13), },

				-- 13 minutes onwards
				{ Infantry = { "s3", "s1", "s1", "s1", "s1", "s1", "s2", "s2", "s3", "s3", "s3", "s4", "s4" }, Vehicles = { "intl.ai2", "intl.ai2", GunWalkerSeekerOrLacerator, CorrupterDevourerOrDarkener, CorrupterDevourerOrDarkener, GunWalkerSeekerOrLacerator, TripodVariant }, Aircraft = { PacOrDevastator }, MinTime = DateTime.Minutes(13), MaxTime = DateTime.Minutes(19), },

				-- 19 minutes onwards
				{ Infantry = { "s3", "s1", "s1", "s1", "s1", "s1", "s2", "s2", "s3", "s3", "s3", "s4", "s4" }, Vehicles = { "intl.ai2", "intl.ai2", GunWalkerSeekerOrLacerator, CorrupterDevourerOrDarkener, AtomizerDarkenerOrRuiner, GunWalkerSeekerOrLacerator, TripodVariant }, Aircraft = { PacOrDevastator, "pac" }, MinTime = DateTime.Minutes(19), },
			}
		}
	}
}
