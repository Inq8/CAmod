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

CompositionValueMultiplier = {
	easy = 0.5,
	normal = 0.67,
	hard = 1,
}

HarvesterDeathDelayTime = {
	easy = DateTime.Seconds(60),
	normal = DateTime.Seconds(40),
	hard = DateTime.Seconds(20),
}

CashAdjustments = {
	easy = 4000,
	normal = 0,
	hard = -1000
}

CapturedCreditsAmount = 1250

EnforceAiBuildRadius = false

ConyardTypes = { "fact", "afac", "sfac" }

HarvesterTypes = { "harv", "harv.td", "harv.td.upg", "harv.scrin", "harv.chrono", "harv.td.upg" }

McvTypes = { "mcv", "amcv", "smcv" }

BarracksTypes = { "tent", "barr", "pyle", "hand", "port" }

FactoryTypes = { "weap", "weap.td", "wsph", "airs" }

RefineryTypes = { "proc", "proc.td", "proc.scrin" }

AirProductionTypes = { "hpad", "afld", "afld.gdi", "hpad.td", "grav" }

NavalProductionTypes = { "syrd", "spen", "syrd.gdi", "spen.nod" }

CashRewardOnCaptureTypes = { "proc", "proc.td", "proc.scrin", "silo", "silo.td", "silo.scrin" }

WallTypes = { "sbag", "fenc", "brik", "cycl", "barb" }

KeyStructures = { "fact", "afac", "sfac", "proc", "proc.td", "proc.scrin", "weap", "weap.td", "airs", "wsph", "dome", "hq", "nerv", "atek", "stek", "gtek", "tmpl", "scrt", "mcv", "amcv", "smcv" }

-- used to define actors and/or types of actors that the AI should not rebuild
RebuildExcludes = {
	-- USSR = {
	-- 	 Actors = { Actor },
	-- 	 Types = { "proc" }
	-- }
}

-- used to define functions to be called when a building is rebuilt
RebuildFunctions = {
	-- USSR = function(building)
	-- 	-- do something
	-- end
}

-- should be populated with the human players in the mission
MissionPlayers = { }

-- should be populated with squads definitions needed by the mission
Squads = { }

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

-- minimum time until next special composition for each AI player
SpecialCompositionMinTimes = { }

-- caches unit costs for adjusting composition difficulty
UnitCosts = { }

-- player characteristics used to enable AI behaviours
PlayerCharacteristics = { }

-- stores original AI conyards for rebuilding purposes
AiConyards = { }

-- stores actors that have a trigger assigned for rebuilding AI conyards
McvProductionTriggers = { }

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
InitAttackAircraft = function(aircraft, targetPlayer, targetList, targetType)
	if not aircraft.IsDead then
		Trigger.OnIdle(aircraft, function(self)
			if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
				local actorId = tostring(aircraft)
				local target = AttackAircraftTargets[actorId]

				if not target or not target.IsInWorld then
					if targetList ~= nil and #targetList > 0 and targetType ~= nil then
						target = ChooseRandomTargetOfTypes(self, targetPlayer, targetList, targetType)
					end
					if target == nil then
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

ChooseRandomTargetOfTypes = function(unit, targetPlayer, targetList, targetType)
	local target = nil
	local enemies = {}

	if targetList ~= nil and #targetList > 0 then
		if targetType == "ArmorType" then
			enemies = targetPlayer.GetActorsByArmorTypes(targetList)
		elseif targetType == "TargetType" then
			enemies = targetPlayer.GetActorsByTargetTypes(targetList)
		else
			enemies = targetPlayer.GetActorsByTypes(targetList)
		end

		local enemies = Utils.Where(enemies, function(e)
			return e.HasProperty("Health") and (e.HasProperty("Move") or e.HasProperty("StartBuildingRepairs")) and unit.CanTarget(e)
		end)

		if #enemies > 0 then
			target = Utils.Random(enemies)
		end
	end

	return target
end

ChooseRandomBuildingTarget = function(unit, targetPlayer)
	local target = nil
	local enemies = Utils.Where(targetPlayer.GetActors(), function(e)
		return e.HasProperty("Health") and e.HasProperty("StartBuildingRepairs") and unit.CanTarget(e) and not Utils.Any(WallTypes, function(t) return e.Type == t end)
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

PlayerHasBuildings = function(player)
	return PlayerBuildingsCount(player) > 0
end

PlayerBuildingsCount = function(player)
	local buildings = Utils.Where(player.GetActors(), function(a)
		return a.HasProperty("StartBuildingRepairs") and not a.HasProperty("Attack")
	end)
	return #buildings
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
		else
			PlayerBaseLocations[p.InternalName] = nil
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

		-- never rebuild silos or conyards
		if a.Type == "fact" or a.Type == "afac" or a.Type == "sfac" or a.Type == "silo" or a.Type == "silo.td" or a.Type == "silo.scrin" then
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

			if RebuildFunctions ~= nil and RebuildFunctions[queueItem.Player.InternalName] ~= nil then
				RebuildFunctions[queueItem.Player.InternalName](b)
			end

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

	local pos = queueItem.CenterPosition

	-- require being in conyard build radius
	if EnforceAiBuildRadius then
		local nearbyConyards = Map.ActorsInCircle(pos, WDist.New(20480), function(a)
			return a.Owner == queueItem.Player
		end)

		if #nearbyConyards == 0 then
			return false
		end
	end

	-- require no nearby units (stops building on top of them)
	if not UtilsCA.CanPlaceBuilding(queueItem.Actor.Type, queueItem.Location) then
		return false
	end

	-- require an owned building nearby, and no enemy buildings nearby
	if not HasOwnedBuildingsNearby(queueItem.Player, pos, true) then
		return false
	end

	return true
end

HasOwnedBuildingsNearby = function(player, pos, noEnemyBuildings)
	local topLeft = WPos.New(pos.X - 8192, pos.Y - 8192, 0)
	local bottomRight = WPos.New(pos.X + 8192, pos.Y + 8192, 0)

	local nearbyBuildings = Map.ActorsInBox(topLeft, bottomRight, function(a)
		return not a.IsDead and a.Owner == player and a.HasProperty("StartBuildingRepairs") and not a.HasProperty("Attack") and a.Type ~= "silo" and a.Type ~= "silo.td" and a.Type ~= "silo.scrin"
	end)

	-- require an owned building nearby
	if #nearbyBuildings == 0 then
		return false
	end

	-- require no enemy buildings nearby
	if noEnemyBuildings then
		local nearbyEnemyBuildings = Map.ActorsInBox(topLeft, bottomRight, function(a)
			return not a.IsDead and not a.Owner.IsAlliedWith(player) and a.HasProperty("StartBuildingRepairs")
		end)

		if #nearbyEnemyBuildings > 0 then
			return false
		end
	end

	return true
end

AutoRebuildConyards = function(player, allDifficulties)
	if Difficulty ~= "hard" and not allDifficulties then
		return
	end
	local conyards = player.GetActorsByTypes(ConyardTypes)
	Utils.Do(conyards, function(a)
		local conyard = ConyardEntry(player, a)
		table.insert(AiConyards, conyard)
		McvRequestTrigger(a, conyard)
	end)
end

ConyardEntry = function(player, a)
	local conyard = {
		Player = player,
		Actor = a,
		AssignedMcv = nil
	}

	if a.Type == "fact" then
		conyard.McvType = "mcv"
		conyard.DeployLocation = a.Location + CVec.New(1, 1)
	elseif a.Type == "afac" then
		conyard.McvType = "amcv"
		conyard.DeployLocation = a.Location + CVec.New(1, 1)
	elseif a.Type == "sfac" then
		conyard.McvType = "smcv"
		conyard.DeployLocation = a.Location + CVec.New(1, 1)
	end

	return conyard
end

McvRequestTrigger = function(a, conyard)
	local isMcv = IsMcv(a)

	if not isMcv then
		Trigger.OnCapture(a, function(self, captor, oldOwner, newOwner)
			for idx, c in pairs(AiConyards) do
				if c.Actor == self then
					table.remove(AiConyards, idx)
				end
			end
		end)
	end

	Trigger.OnKilled(a, function(self, killer)
		local mcvType
		local owner

		-- if it was an mcv that was killed, unassign it from the conyard
		if isMcv then
			mcvType = self.Type
			owner = self.Owner
			for idx, c in pairs(AiConyards) do
				if c.AssignedMcv == self then
					c.AssignedMcv = nil
					break
				end
			end
		else
			mcvType = conyard.McvType
			owner = conyard.Player
		end

		if isMcv or conyard.Player == self.Owner then
			QueueMcv(owner, mcvType)
		end
	end)
end

QueueMcv = function(player, mcvType)
	Trigger.AfterDelay(DateTime.Minutes(8), function()
		local producers = player.GetActorsByTypes(FactoryTypes)

		if #producers > 0 then
			local producer = Utils.Random(producers)

			-- get a conyard without an assigned mcv that has friendly buildings nearby
			local possibleConyards = Utils.Where(AiConyards, function(c)
				return c.Actor.IsDead and c.AssignedMcv == nil and c.Player == player and HasOwnedBuildingsNearby(player, Map.CenterOfCell(c.DeployLocation), true)
			end)

			if #possibleConyards == 0 then
				return
			end

			if not McvProductionTriggers[tostring(producer)] then
				McvProductionTriggers[tostring(producer)] = player.InternalName
				Trigger.OnProduction(producer, function(p, produced)
					if IsMcv(produced) and produced.Owner == player then

						-- get a conyard without an assigned mcv that has friendly buildings nearby (repeated inside the production trigger)
						local possibleConyards = Utils.Where(AiConyards, function(c)
							return c.Actor.IsDead and c.AssignedMcv == nil and c.Player == player and HasOwnedBuildingsNearby(player, Map.CenterOfCell(c.DeployLocation), true)
						end)

						local targetConyard
						if #possibleConyards > 0 then
							targetConyard = Utils.Random(possibleConyards)
							targetConyard.AssignedMcv = produced

							Trigger.OnIdle(produced, function(self)
								produced.Move(targetConyard.DeployLocation)
								produced.Deploy()

								-- move to a random location within 5 cells to prevent excessive idle calls
								local randomCell = CPos.New(targetConyard.DeployLocation.X + Utils.RandomInteger(-5, 5), targetConyard.DeployLocation.Y + Utils.RandomInteger(-5, 5))
								produced.Move(randomCell)
							end)

							-- if deployed
							Trigger.OnRemovedFromWorld(produced, function(self)
								if not self.IsDead then
									Trigger.AfterDelay(DateTime.Seconds(1), function()
										local nearbyConyards = Map.ActorsInCircle(Map.CenterOfCell(targetConyard.DeployLocation), WDist.FromCells(2), function(a)
											return a.Owner == player and IsConyard(a)
										end)
										if #nearbyConyards > 0 then
											local deployedConyard = Utils.Random(nearbyConyards)
											targetConyard.Actor = deployedConyard
											targetConyard.AssignedMcv = nil
											McvRequestTrigger(deployedConyard, targetConyard)
											AutoRepairBuilding(deployedConyard, player)
										end
									end)
								end
							end)

							-- if mcv is killed, re-request
							McvRequestTrigger(produced)
						else
							produced.Destroy()
						end
					end
				end)
			end

			producer.Produce(mcvType)
		end
	end)
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
			CallForHelp(self, range, filter)
		end
	end)
	Trigger.OnKilled(actor, function(self, killer)
		if validAttackingPlayerFunc(killer.Owner) then
			CallForHelp(self, range, filter)
		end
	end)
end

CallForHelp = function(self, range, filter)
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

		local nearbyUnits = Map.ActorsInCircle(self.CenterPosition, range, function(a)
			return a.Owner.IsAlliedWith(self.Owner) and not IsMissionPlayer(a.Owner) and filter(a)
		end)

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

	-- set the squad's name based on the key if it hasn't been set manually
	if squad.Name == nil then
		for key, value in pairs(Squads) do
			if value == squad then
				squad.Name = key
				break
			end
		end
	end

	-- by default the init time is whenever the squad is initialized,
	-- this can be overridden so that the AI will use later game compositions and attack values
	if squad.InitTime == nil then
		squad.InitTime = DateTime.GameTime
	end

	if squad.InitTimeAdjustment ~= nil then
		squad.InitTime = squad.InitTime + squad.InitTimeAdjustment
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

		local allCompositions
		if squad.Units[Difficulty] ~= nil then
			allCompositions = squad.Units[Difficulty]
		else
			allCompositions = squad.Units
		end

		-- filter possible compositions based on game time and other requirements
		local validCompositions = Utils.Where(allCompositions, function(composition)
			return (composition.MinTime == nil or DateTime.GameTime >= composition.MinTime + squad.InitTime) -- after min time
				and (composition.MaxTime == nil or DateTime.GameTime < composition.MaxTime + squad.InitTime) -- before max time
				and (composition.RequiredTargetCharacteristics == nil or Utils.All(composition.RequiredTargetCharacteristics, function(characteristic)
					return PlayerCharacteristics[squad.TargetPlayer.InternalName] ~= nil and PlayerCharacteristics[squad.TargetPlayer.InternalName][characteristic] ~= nil and PlayerCharacteristics[squad.TargetPlayer.InternalName][characteristic]
				end)) -- target player has all required characteristics
				and (composition.Prerequisites == nil or squad.Player.HasPrerequisites(composition.Prerequisites)) -- player has prerequisites
		end)

		-- determine whether to choose a special composition (33% chance if enough time has elapsed since last used)
		local useSpecialComposition = false

		if SpecialCompositionMinTimes[squad.Player.InternalName] == nil or DateTime.GameTime >= SpecialCompositionMinTimes[squad.Player.InternalName] then
			useSpecialComposition = Utils.RandomInteger(1, 100) > 66
		end

		local validStandardCompositions = Utils.Where(validCompositions, function(composition)
			return not composition.IsSpecial
		end)

		if useSpecialComposition then
			local validSpecialCompositions = Utils.Where(validCompositions, function(composition)
				return composition.IsSpecial
			end)

			if #validSpecialCompositions > 0 then
				validCompositions = validSpecialCompositions
			else
				validCompositions = validStandardCompositions
			end
		else
			validCompositions = validStandardCompositions
		end

		if #validCompositions > 0 then
			-- randomly select a unit composition for next wave
			local chosenComposition = Utils.Random(validCompositions)

			-- if this is a special composition, another special composition can't be chosen for 10 minutes
			if chosenComposition.IsSpecial then
				SpecialCompositionMinTimes[squad.Player.InternalName] = DateTime.GameTime + DateTime.Minutes(10)
			end

			squad.QueuedUnits = chosenComposition
			local queuesForComposition = GetQueuesForComposition(chosenComposition)

			-- go through each queue for the current difficulty and start producing the first unit
			Utils.Do(queuesForComposition, function(queue)
				ProduceNextAttackSquadUnit(squad, queue, 1)
			end)
		else
			Trigger.AfterDelay(DateTime.Seconds(15), function()
				InitAttackSquad(squad, squad.Player, squad.TargetPlayer)
			end)
		end
	else
		Trigger.AfterDelay(DateTime.Seconds(15), function()
			InitAttackSquad(squad, squad.Player, squad.TargetPlayer)
		end)
	end
end

GetQueuesForComposition = function(composition)
	local queues = { }

	for k,v in pairs(composition) do
		if not IsCompositionSetting(k) then
			table.insert(queues, k)
		end
	end

	return queues
end

IsCompositionSetting = function(key)
	local settings = {
		MinTime = true,
		MaxTime = true,
		IsSpecial = true,
		RequiredTargetCharacteristics = true,
		Prerequisites = true,
		TargetList = true,
		TargetType = true
	}
	return settings[key] or false
end

InitAirAttackSquad = function(squad, player, targetPlayer, targetList, targetType)
	squad.IsAirSquad = true
	squad.AirTargetList = targetList
	squad.AirTargetType = targetType
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

				for _, producerActor in pairs(producerActors) do
					if not producerActor.IsDead and producerActor.Owner == squad.Player then
						producer = producerActor
						break
					end
				end
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

				-- set that the next unit produced from this producer should be assigned to the specified squad
				AddToSquadAssignmentQueue(producerId, squad)

				-- add production trigger once for the producer (once for every owner, as the producer may be captured)
				if OnProductionTriggers[producerId] == nil or OnProductionTriggers[producerId] ~= producer.Owner.InternalName then
					OnProductionTriggers[producerId] = tostring(producer.Owner.InternalName)

					-- add produced unit to list of idle units for the squad
					Trigger.OnProduction(producer, function(p, produced)
						HandleProducedSquadUnit(produced, producerId, squad)
					end)
				end

				local engineersNearby = Map.ActorsInCircle(producer.CenterPosition, WDist.New(2730), function(a)
					return not a.IsDead and (a.Type == "e6" or a.Type == "n6" or a.Type == "s6" or a.Type == "mast") and a.Owner ~= producer.Owner
				end)

				-- prevents capturing player getting a free unit
				-- the capture blocks the production until the capture completes
				if #engineersNearby == 0 then
					producer.Produce(nextUnit)

					if UnitCosts[nextUnit] == nil then
						UnitCosts[nextUnit] = ActorCA.CostOrDefault(nextUnit)
					end

					squad.WaveTotalCost = squad.WaveTotalCost + UnitCosts[nextUnit]
				end
			end

			-- start producing the next unit
			ProduceNextAttackSquadUnit(squad, queue, unitIndex + 1)
		end)
	end
end

HandleProducedSquadUnit = function(produced, producerId, squad)

	-- if the produced unit is not owned by the squad player, ignore it
	if produced.Owner ~= squad.Player then
		return
	end

	-- we don't want to add harvesters or mcvs to squads, which are produced when replacements are needed
	for _, t in pairs(Utils.Concat(HarvesterTypes, McvTypes)) do
		if produced.Type == t then
			return
		end
	end

	InitSquadAssignmentQueueForProducer(producerId, squad.Player)

	-- assign unit to IdleUnits of the next squad in the assignment queue of the producer
	if SquadAssignmentQueue[squad.Player.InternalName][producerId][1] ~= nil then
		local assignedSquad = SquadAssignmentQueue[squad.Player.InternalName][producerId][1]
		assignedSquad.IdleUnits[#assignedSquad.IdleUnits + 1] = produced
		table.remove(SquadAssignmentQueue[squad.Player.InternalName][producerId], 1)

		if produced.HasProperty("HasPassengers") and not produced.IsDead then
			Trigger.OnPassengerExited(produced, function(transport, passenger)
				AssaultPlayerBaseOrHunt(passenger, assignedSquad.TargetPlayer)
			end)
		end

		if assignedSquad.OnProducedAction ~= nil then
			assignedSquad.OnProducedAction(produced)
		end

	elseif produced.HasProperty("Hunt") then
		produced.Hunt()
	end

	TargetSwapChance(produced, 10)
end

-- used to make sure multiple squads being produced from the same structure don't get mixed up
-- also split by player to prevent these getting jumbled if producer owner changes
AddToSquadAssignmentQueue = function(producerId, squad)
	InitSquadAssignmentQueueForProducer(producerId, squad.Player)
	SquadAssignmentQueue[squad.Player.InternalName][producerId][#SquadAssignmentQueue[squad.Player.InternalName][producerId] + 1] = squad
end

InitSquadAssignmentQueueForProducer = function(producerId, player)
	if SquadAssignmentQueue[player.InternalName] == nil then
		SquadAssignmentQueue[player.InternalName] = { }
	end
	if SquadAssignmentQueue[player.InternalName][producerId] == nil then
		SquadAssignmentQueue[player.InternalName][producerId] = { }
	end
end

-- on finishing production of a squad of units for an attack, calculate how long to wait to produce the next squad based on desired value per second
CalculateInterval = function(squad)
	local ticksSpentProducing = DateTime.GameTime - squad.WaveStartTime

	if squad.AttackValuePerSecond ~= nil and squad.AttackValuePerSecond[Difficulty] ~= nil then
		local desiredValue = 0
		local attackValues = squad.AttackValuePerSecond[Difficulty]
		local ticksSinceInit = DateTime.GameTime - squad.InitTime
		desiredValue = CalculateValuePerSecond(ticksSinceInit, attackValues)
		local ticks = ((25 * squad.WaveTotalCost) - (desiredValue * ticksSpentProducing)) / desiredValue
		return math.max(math.floor(ticks), 0)
	else
		return ticksSpentProducing
	end
end

-- calculate the value per second based on the current tick and the min/max value of the squad
function CalculateValuePerSecond(currentTick, attackValues)
	local minValue = attackValues.Min
	local maxValue = attackValues.Max
	local rampDuration
	local growthFactor
	if attackValues.RampDuration ~= nil then
		rampDuration = attackValues.RampDuration
	else
		if Difficulty == "hard" then
			rampDuration = DateTime.Minutes(15)
		elseif Difficulty == "normal" then
			rampDuration = DateTime.Minutes(17)
		else
			rampDuration = DateTime.Minutes(19)
		end
	end
	if attackValues.GrowthFactor ~= nil then
		growthFactor = attackValues.GrowthFactor
	else
		growthFactor = 2.06
	end
    local progress = currentTick / rampDuration
    local scaledProgress = progress ^ growthFactor
    local value = minValue + (maxValue - minValue) * scaledProgress
    return math.min(math.floor(value), maxValue)
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

	if squad.IsAirSquad ~= nil and squad.IsAirSquad then
		Utils.Do(squad.IdleUnits, function(a)
			if not a.IsDead then
				local targetList = squad.AirTargetList
				local targetType = squad.AirTargetType
				local typeKey = string.gsub(a.Type, "%.", "_")

				if targetList == nil and AircraftTargets[typeKey] ~= nil then
					targetList = AircraftTargets[typeKey].TargetList
				end

				if targetType == nil and AircraftTargets[typeKey] ~= nil then
					targetType = AircraftTargets[typeKey].TargetType
				end

				InitAttackAircraft(a, squad.TargetPlayer, targetList, targetType)
			end
		end)
	else
		local squadLeader = nil
		local attackPath = nil

		if squad.AttackPaths ~= nil and #squad.AttackPaths > 0 then
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
	if not actor.IsDead and actor.IsInWorld and squad.Player == actor.Owner then
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

			-- reapply unload trigger
			if actor.HasProperty("HasPassengers") then
				Trigger.AfterDelay(1, function()
					if not actor.IsDead then
						Trigger.OnPassengerExited(actor, function(transport, passenger)
							AssaultPlayerBaseOrHunt(passenger, squad.TargetPlayer)
						end)
					end
				end)
			end
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

AdjustPlayerStartingCashForDifficulty = function(player)
	if player == nil then
		for _, p in pairs(MissionPlayers) do
			AdjustPlayerStartingCashForDifficulty(p)
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

IsMcv = function(actor)
	for _, t in pairs(McvTypes) do
		if actor.Type == t then
			return true
		end
	end
end

IsConyard = function(actor)
	for _, t in pairs(ConyardTypes) do
		if actor.Type == t then
			return true
		end
	end
end

IsGroundHunterUnit = function(actor)
	return not actor.IsDead and actor.HasProperty("Move") and not actor.HasProperty("Land") and actor.HasProperty("Hunt")
end

IsGreeceGroundHunterUnit = function(actor)
	return IsGroundHunterUnit(actor) and actor.Type ~= "arty" and actor.Type ~= "cryo" and actor.Type ~= "mgg" and actor.Type ~= "mrj"
end

IsUSSRGroundHunterUnit = function(actor)
	return IsGroundHunterUnit(actor) and actor.Type ~= "v2rl" and actor.Type ~= "v3rl" and actor.Type ~= "katy" and actor.Type ~= "grad" and actor.Type ~= "nukc"
end

IsGDIGroundHunterUnit = function(actor)
	return (IsGroundHunterUnit(actor) or actor.Type == "jjet") and actor.Type ~= "msam" and actor.Type ~= "memp" and actor.Type ~= "thwk"
end

IsNodGroundHunterUnit = function(actor)
	return IsGroundHunterUnit(actor) and actor.Type ~= "mlrs" and actor.Type ~= "arty.nod"
end

IsScrinGroundHunterUnit = function(actor)
	return IsGroundHunterUnit(actor) and actor.Type ~= "mast" and actor.Type ~= "pdgy"
end

-- Upgrades

InitAiUpgrades = function(player, advancedDelay)
	if advancedDelay == nil then
		advancedDelay = DateTime.Minutes(15)
	end

	if player.Faction == "soviet" then
		Actor.Create("hazmatsoviet.upgrade", true, { Owner = player })
	elseif player.Faction ~= "scrin" then
		Actor.Create("hazmat.upgrade", true, { Owner = player })
	end

	if Difficulty == "hard" then
		Trigger.AfterDelay(advancedDelay, function()
			if player.Faction == "scrin" then
				Actor.Create("carapace.upgrade", true, { Owner = player })
			else
				Actor.Create("flakarmor.upgrade", true, { Owner = player })
			end
		end)
	end

	if (player.Faction == "allies") then

		if Difficulty == "hard" then
			Actor.Create("cryw.upgrade", true, { Owner = Greece })
		end

	elseif (player.Faction == "soviet") then

		if Difficulty == "hard" then
			Trigger.AfterDelay(advancedDelay, function()
				Actor.Create("tarc.upgrade", true, { Owner = player })

				local doctrineUpgrades = { "rocketpods.upgrade", "reactive.upgrade", "imppara.upgrade" }
				local selectedDoctrineUpgrade = Utils.Random(doctrineUpgrades)
				Actor.Create(selectedDoctrineUpgrade, true, { Owner = player })
			end)
		end

	elseif (player.Faction == "nod") then

		if Difficulty == "hard" then
			Trigger.AfterDelay(advancedDelay, function()
				Actor.Create("blacknapalm.upgrade", true, { Owner = player })
				Actor.Create("tibcore.upgrade", true, { Owner = player })
				Actor.Create("quantum.upgrade", true, { Owner = player })
				Actor.Create("cyborgspeed.upgrade", true, { Owner = player })
				Actor.Create("cyborgarmor.upgrade", true, { Owner = player })
			end)
		end

	elseif (player.Faction == "gdi") then

		if Difficulty == "hard" then
			Actor.Create("sonic.upgrade", true, { Owner = player, })
			Actor.Create("empgren.upgrade", true, { Owner = player, })

			Trigger.AfterDelay(advancedDelay, function()
				local strategyUpgrades = {
					{ "bombard.strat", "bombard2.strat", "hailstorm.upgrade" },
					{ "seek.strat", "seek2.strat", "hypersonic.upgrade" },
					{ "hold.strat", "hold2.strat", "hammerhead.upgrade" },
				}

				local selectedStrategyUpgrades = Utils.Random(strategyUpgrades)
				Utils.Do(selectedStrategyUpgrades, function(u)
					Actor.Create(u, true, { Owner = player })
				end)
			end)
		end

	elseif (player.Faction == "scrin") then

		if Difficulty == "hard" then
			Actor.Create("ioncon.upgrade", true, { Owner = player })

			Trigger.AfterDelay(advancedDelay, function()
				Actor.Create("resconv.upgrade", true, { Owner = player })
				Actor.Create("shields.upgrade", true, { Owner = player })
			end)
		end

	end
end

-- Units & compositions

AdjustCompositionsForDifficulty = function(compositions, difficulty)

	if difficulty == nil then
		difficulty = Difficulty
	end

	if Difficulty == "hard" then
		return compositions
	end

	local updatedCompositions = { }

	Utils.Do(compositions, function(comp)
		local updatedComposition = AdjustCompositionForDifficulty(comp, difficulty)
		table.insert(updatedCompositions, updatedComposition)
	end)

	return updatedCompositions
end

AdjustCompositionForDifficulty = function(composition, difficulty)

	if difficulty == nil then
		difficulty = Difficulty
	end

	if difficulty == "hard" then
		return composition
	end

	-- total unadjusted cost for all units in each queue
	local queueTotalUnitCost = { }

	-- total adjusted cost for each queue
	local queueAllocatedTotalUnitCost = { }

	-- units added to the adjusted composition
	local updatedComposition = { }

	for k,v in pairs(composition) do

		if not IsCompositionSetting(k) then
			local queueName = k
			local queueUnits = v
			queueTotalUnitCost[queueName] = 0
			queueAllocatedTotalUnitCost[queueName] = 0

			-- for each unit in the queue
			for i,unit in pairs(queueUnits) do
				local chosenUnit

				-- if the unit is a table of possible units, select one randomly
				if type(unit) == "table" then
					chosenUnit = Utils.Random(unit)
				else
					chosenUnit = unit
				end

				if UnitCosts[chosenUnit] == nil then
					UnitCosts[chosenUnit] = ActorCA.CostOrDefault(chosenUnit)
				end

				-- add the cost to the total cost for the queue
				queueTotalUnitCost[queueName] = queueTotalUnitCost[queueName] + UnitCosts[chosenUnit]
			end

			local adjustedDesiredTotalUnitCostForQueue = queueTotalUnitCost[queueName] * CompositionValueMultiplier[difficulty]

			-- allocate units until the adjusted cost is reached
			for i,unit in pairs(queueUnits) do

				if queueAllocatedTotalUnitCost[queueName] >= adjustedDesiredTotalUnitCostForQueue then
					break
				end

				local chosenUnit

				if type(unit) == "table" then
					chosenUnit = unit[1]
				else
					chosenUnit = unit
				end

				if updatedComposition[queueName] == nil then
					updatedComposition[queueName] = { }
				end

				table.insert(updatedComposition[queueName], unit)

				if UnitCosts[chosenUnit] == nil then
					UnitCosts[chosenUnit] = ActorCA.CostOrDefault(chosenUnit)
				end

				queueAllocatedTotalUnitCost[queueName] = queueAllocatedTotalUnitCost[queueName] + UnitCosts[chosenUnit]
			end
		else
			if k == "MinTime" or k == "MaxTime" then

				if difficulty == "easy" then
					updatedComposition[k] = v * 1.4
				elseif difficulty == "normal" then
					updatedComposition[k] = v * 1.2
				end

			else
				updatedComposition[k] = v
			end
		end
	end

	return updatedComposition
end

CalculatePlayerCharacteristics = function()
	Utils.Do(MissionPlayers, function(p)
		PlayerCharacteristics[p.InternalName] = {
			MassInfantry = false,
			MassHeavy = false,
		}

		local infantryUnits = p.GetActorsByArmorTypes({ "None" })
		local heavyUnits = p.GetActorsByArmorTypes({ "Heavy" })
		local infantryValue = 0
		local heavyValue = 0

		Utils.Do(infantryUnits, function(u)
			if UnitCosts[u.Type] == nil then
				UnitCosts[u.Type] = ActorCA.CostOrDefault(u.Type)
			end
			infantryValue = infantryValue + UnitCosts[u.Type]
		end)

		Utils.Do(heavyUnits, function(u)
			if UnitCosts[u.Type] == nil then
				UnitCosts[u.Type] = ActorCA.CostOrDefault(u.Type)
			end
			heavyValue = heavyValue + UnitCosts[u.Type]
		end)

		if infantryValue > heavyValue * 3 then
			PlayerCharacteristics[p.InternalName].MassInfantry = true
		elseif heavyValue > infantryValue * 3 then
			PlayerCharacteristics[p.InternalName].MassHeavy = true
		end

		if infantryValue > 15000 then
			PlayerCharacteristics[p.InternalName].MassInfantry = true
		end

		if heavyValue > 15000 then
			PlayerCharacteristics[p.InternalName].MassHeavy = true
		end
	end)
end

AircraftTargets = {
	heli = { TargetList = { "Heavy", "Concrete", "Aircraft" }, TargetType = "ArmorType" },
	harr = { TargetList = { "None", "Light", "Wood", "Aircraft" }, TargetType = "ArmorType" },
	pmak = { TargetList = { "Heavy", "Light", "Concrete" }, TargetType = "ArmorType" },
	nhaw = { TargetList = { "None", "Light" }, TargetType = "ArmorType" },
	beag = { TargetList = { "Heavy", "Light", "Aircraft" }, TargetType = "ArmorType" },
	hind = { TargetList = { "Vehicle", "Infantry" }, TargetType = "TargetType" },
	yak = { TargetList = { "None", "Light", "Wood" }, TargetType = "ArmorType" },
	mig = { TargetList = { "Heavy", "Concrete", "Aircraft" }, TargetType = "ArmorType" },
	suk = { TargetList = { "Heavy", "Light", "Concrete" }, TargetType = "ArmorType" },
	suk_upg = { TargetList = { "Heavy", "Light", "Concrete" }, TargetType = "ArmorType" },
	kiro = { TargetList = { "Wood", "Concrete" }, TargetType = "ArmorType" },
	disc = { TargetList = { "Heavy", "Light", "None" }, TargetType = "ArmorType" },
	orca = { TargetList = { "Heavy", "Concrete", "Aircraft" }, TargetType = "ArmorType" },
	a10 = { TargetList = { "None", "Wood" }, TargetType = "ArmorType" },
	a10_gau = { TargetList = { "None", "Light", "Wood" }, TargetType = "ArmorType" },
	a10_sw = { TargetList = { "None", "Wood", "Aircraft" }, TargetType = "ArmorType" },
	orcb = { TargetList = { "Heavy", "Light", "Concrete" }, TargetType = "ArmorType" },
	auro = { TargetList = { "Concrete", "Heavy" }, TargetType = "ArmorType" },
	jack = { TargetList = { "Heavy", "Light" }, TargetType = "ArmorType" },
	apch = { TargetList = { "None", "Light", "Aircraft" }, TargetType = "ArmorType" },
	venm = { TargetList = { "None", "Light", "Aircraft" }, TargetType = "ArmorType" },
	rah = { TargetList = { "None", "Light", "Wood" }, TargetType = "ArmorType" },
	scrn = { TargetList = { "Heavy", "Concrete", "Aircraft" }, TargetType = "ArmorType" },
	stmr = { TargetList = { "None", "Light", "Aircraft" }, TargetType = "ArmorType" },
	torm = { TargetList = { "Heavy", "Concrete", "Aircraft" }, TargetType = "ArmorType" },
	enrv = { TargetList = { "Heavy", "Concrete", "Aircraft" }, TargetType = "ArmorType" },
	deva = { TargetList = { "None", "Concrete", "Wood" }, TargetType = "ArmorType" },
	pac = { TargetList = { "Heavy", "Light" }, TargetType = "ArmorType" },
}

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
CorrupterOrDevourer = { "corr", "devo" }
AtomizerObliteratorOrRuiner = { "atmz", "oblt", "ruin" }
TripodVariant = { "tpod", "tpod", "rtpd" }
PacOrDevastator = { "pac", "deva" }

UnitCompositions = {
	Allied = {
		-- 0 to 10 minutes
		{ Infantry = {}, Vehicles = { "jeep", "jeep", "jeep", "jeep", "jeep" }, MaxTime = DateTime.Minutes(10) },
		{ Infantry = {}, Vehicles = { "1tnk", "1tnk", "1tnk", "1tnk" }, MaxTime = DateTime.Minutes(10) },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1" }, Vehicles = { "2tnk", "apc.ai", "2tnk", "arty" }, MaxTime = DateTime.Minutes(10) },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "e1", "e1" }, Vehicles = { "2tnk", "ifv.ai", "2tnk", "ifv.ai" }, MaxTime = DateTime.Minutes(10) },

		-- 10 minutes onwards
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", AlliedAdvancedInfantry, "e1", "e3", "e1", "e1" }, Vehicles = { "2tnk", "ifv.ai", "rapc.ai", "rapc.ai", "arty" }, MinTime = DateTime.Minutes(10) },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", AlliedAdvancedInfantry, "e1", "e3", "e1", "e1" }, Vehicles = { "2tnk", "2tnk", "2tnk", "ifv.ai", "ptnk", "ptnk" }, MinTime = DateTime.Minutes(10) },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", AlliedAdvancedInfantry, "e1", "e3", "e1", "e1" }, Vehicles = { "2tnk", "ifv.ai", "ptnk", "2tnk", "2tnk", AlliedT3SupportVehicle }, MinTime = DateTime.Minutes(10) },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "e3", "e1", "e1" }, Vehicles = { "batf.ai", "ifv.ai", "batf.ai", "ifv.ai" }, MinTime = DateTime.Minutes(10) },
		{ Infantry = {}, Vehicles = { "rapc.ai", "jeep", "ifv.ai", "rapc.ai", "ifv.ai", "rapc.ai" }, MinTime = DateTime.Minutes(10) },

		-- 16 minutes onwards
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", AlliedAdvancedInfantry, "e1", "e3", "e1", "e1", "e3", "e1", "e1" }, Vehicles = { "2tnk", "2tnk", "ifv.ai", AlliedT3SupportVehicle, "2tnk", PrismCannonOrZeus }, MinTime = DateTime.Minutes(16) },
		{ Infantry = { "e3", "enfo", "enfo", "enfo", "enfo", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "e3", "e1", "e1", "e1", "e3", "enfo", "enfo" }, Vehicles = { "2tnk", "ptnk", "2tnk", "2tnk", "2tnk", "ptnk" }, MinTime = DateTime.Minutes(18), IsSpecial = true },

		------ Anti-tank
		{ Infantry = { "e3", "e1", "e3", "e3", "e1", "e3", "e1", "e3", "e1", "e3", "e3", "e1", "e3", "e3", "e3", "e3" }, Vehicles = { "tnkd", "tnkd", "tnkd", "tnkd", "tnkd", "tnkd" }, MinTime = DateTime.Minutes(16), RequiredTargetCharacteristics = { "MassHeavy" } },

		------ Anti-infantry
		{ Infantry = { "e3", "enfo", "e1", "e1", "e1", "enfo", "e1", "e1", "enfo", "e3", "e1", "e1", "enfo", "e1", "e1", "e1", "e1", "e1", "e1", "enfo", "enfo" }, Vehicles = { "ptnk", "ptnk", "ptnk", "cryo", "ptnk", "ptnk" }, MinTime = DateTime.Minutes(16), RequiredTargetCharacteristics = { "MassInfantry" } },

		-- Specials
		{ Infantry = {}, Vehicles = { "ctnk", "ctnk", "ctnk", "ctnk", "ctnk", "ctnk"  }, MinTime = DateTime.Minutes(18), IsSpecial = true },
		{ Infantry = { "seal", "seal", "seal", "seal", "seal", "seal", "e7" }, Vehicles = { }, MinTime = DateTime.Minutes(18), IsSpecial = true },
		{ Infantry = { "snip", "snip", "snip", "snip", "snip", "snip", "snip", "snip" }, Vehicles = { "rtnk", "rtnk", "rtnk", "rtnk", "rtnk", "rtnk", "rtnk" }, MinTime = DateTime.Minutes(18), IsSpecial = true },
		{ Infantry = { "e3", "cryt", "cryt", "cryt", "e3", "e1", "e1", "e1", "e1", "e1", "e1", "cryt", "cryt", "cryt",  }, Vehicles = { "cryo", "2tnk", "2tnk", "cryo", "ifv", "cryo", "2tnk" }, MinTime = DateTime.Minutes(18), IsSpecial = true }
	},
	Soviet = {
		-- 0 to 10 minutes
		{ Infantry = { "e3", "e1", "e1", "e1", "e1", "e1", "e2", "e3", "e4" }, Vehicles = { "3tnk", "btr.ai", "3tnk" }, MaxTime = DateTime.Minutes(10), },

		-- 10 to 16 minutes
		{ Infantry = { "e3", "e1", "e1", "e3", "shok", "e1", "shok", "e1", "e2", "e3", "e4", "e1" }, Vehicles = { "3tnk", SovietMammothVariant, "btr.ai", TeslaVariant, SovietBasicArty }, MinTime = DateTime.Minutes(10), MaxTime = DateTime.Minutes(16), },
		{ Infantry = { "e3", "e1", "shok", "e3", "shok", "e1", "shok", "e1", "shok", "e3", "e4", "e1" }, Vehicles = { TeslaVariant, SovietMammothVariant, "btr.ai", TeslaVariant, SovietBasicArty }, MinTime = DateTime.Minutes(10), MaxTime = DateTime.Minutes(16), },

		-- 16 minutes onwards
		{ Infantry = { "e3", "e1", "e1", "e3", "shok", "e1", "e1", "cmsr", "e1", "e2", "e3", "e4", "e1", "e1", "e1", "e1", "shok" }, Vehicles = { "3tnk", SovietMammothVariant, "btr.ai", TeslaVariant, SovietBasicArty, SovietAdvancedArty }, MinTime = DateTime.Minutes(16), },
		{ Infantry = { "e3", "e1", "e1", "e3", "ttrp", "e1", "ttrp", "e1", "cmsr", "e2", "e3", "e4", "e1", "e1", "e1", "e1" }, Vehicles = { "3tnk", SovietMammothVariant, "btr.ai", TeslaVariant, SovietBasicArty, SovietAdvancedArty }, MinTime = DateTime.Minutes(16), },
		{ Infantry = { "e3", "e1", "e1", "e3", "e8", "e1", "e8", "e1", "deso", "deso", "e2", "e3", "e4", "e1", "e1", "e1", "e1" }, Vehicles = { SovietMammothVariant, "3tnk.atomic", "btr.ai", "3tnk.atomic", "apoc", "v3rl" }, MinTime = DateTime.Minutes(16), },

		------ Anti-tank
		{ Infantry = { "e3", "e1", "e3", "e3", "e1", "e3", "e1", "e3", "e1", "e3", "e3", "e1", "e3", "e3", "e3", "e3" }, Vehicles = { "ttra", "ttra", "ttra", "ttra", "ttra", "ttra" }, MinTime = DateTime.Minutes(16), RequiredTargetCharacteristics = { "MassHeavy" } },

		------ Anti-infantry
		{ Infantry = { "e3", "e1", "e1", "e1", "e1", "shok", "shok", "ttrp", "e1", "e1", "e1", "e1", "e1", "e1", "e1", "e1", "e1" }, Vehicles = { "btr", "btr", "ttnk", "v2rl", "ttnk", "btr", "v2rl", "v2rl" }, MinTime = DateTime.Minutes(16), RequiredTargetCharacteristics = { "MassInfantry" } },

		-- Specials
		{ Infantry = { "ttrp", "ttrp", "ttrp", "ttrp", "ttrp", "ttrp", "ttrp", "ttrp" }, Vehicles = { "ttnk", "ttra", "ttnk", "ttra", "ttnk", "ttnk", "ttra" }, MinTime = DateTime.Minutes(18), IsSpecial = true },
		{ Infantry = { "deso", "deso", "deso", "deso", "deso", "deso", "deso", "deso" }, Vehicles = { "4tnk.erad", "4tnk.erad", "4tnk.erad", "4tnk.erad", "4tnk.erad" }, MinTime = DateTime.Minutes(18), IsSpecial = true }
	},
	GDI = {
		-- 0 to 10 minutes
		{ Infantry = {}, Vehicles = { HumveeOrGuardianDrone, HumveeOrGuardianDrone, HumveeOrGuardianDrone, HumveeOrGuardianDrone, HumveeOrGuardianDrone }, MaxTime = DateTime.Minutes(10) },
		{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", "n2", "n2" }, Vehicles = { "mtnk", "mtnk", "msam", "vulc", "vulc.ai"  }, MaxTime = DateTime.Minutes(10) },
		{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", "n2", "n2", "n1", "n1", "n1", "n3" }, Vehicles = { "mtnk", "mtnk", HumveeOrGuardianDrone, HumveeOrGuardianDrone }, MaxTime = DateTime.Minutes(10) },

		-- 10 minutes onwards
		{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", "n1", "n1", "n3", "n1", "n1", "n1" }, Vehicles = { "mtnk", "mtnk", "vulc", "vulc.ai", "jugg"  }, MinTime = DateTime.Minutes(10) },
		{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", "n1", "n1", "jjet", "n1", "n3", "n1", "n1", "n1" }, Vehicles = { "mtnk", "vulc", "vulc.ai", "msam", "vulc.ai" }, MinTime = DateTime.Minutes(10) },
		{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", "n1", "n1", "jjet", "n1", "n3", "n1", "n1", "n1" }, Vehicles = { "titn", "mtnk", "msam", "vulc", GDIMammothVariant }, MinTime = DateTime.Minutes(10) },
		{ Infantry = { "jjet", "bjet", "jjet", "jjet", "bjet", "jjet" }, Vehicles = { TOWHumveeOrGuardianDrone, TOWHumveeOrGuardianDrone, TOWHumveeOrGuardianDrone, TOWHumveeOrGuardianDrone, TOWHumveeOrGuardianDrone }, MinTime = DateTime.Minutes(10) },
		{ Infantry = {}, Vehicles = { "hsam", "hsam", "hsam", "hsam", "hsam", "hsam", "hsam" }, MinTime = DateTime.Minutes(10) },

		-- 10 to 16 minutes
		{ Infantry = { "n1", "n1", "n3", "n1", "n1", "n1", "n1", "n1", "n3", "n1", "n1" }, Vehicles = { "htnk", WolverineOrXO, WolverineOrXO, "hsam", "vulc", GDIMammothVariant, WolverineOrXO }, MinTime = DateTime.Minutes(10), MaxTime = DateTime.Minutes(16) },

		-- 16 minutes onwards
		{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", "n1", ZoneTrooperVariant, ZoneTrooperVariant, ZoneTrooperVariant }, Vehicles = { "mtnk", GDIMammothVariant, "vulc", "mtnk", "msam" }, MinTime = DateTime.Minutes(16) },
		{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", "n1", "n1", "n1", "n1", "n3", "n1", "n1" }, Vehicles = { "vulc.ai", "disr", "disr", "disr" }, MinTime = DateTime.Minutes(16) },
		{ Infantry = { "n3", "rmbo", "n3", "n1", "n1", "n1", "n1", "n1", "n3", "n1", "n1", "n3", "n1", "n1" }, Vehicles = { GDIMammothVariant, "msam", "vulc", "msam", GDIMammothVariant }, MinTime = DateTime.Minutes(16) },
		{ Infantry = { "n3", "n1", "n1", ZoneTrooperVariant, ZoneTrooperVariant, ZoneTrooperVariant, ZoneTrooperVariant, "n1", "n1" }, Vehicles = { GDIMammothVariant, "mtnk", WolverineOrXO, WolverineOrXO, GDIMammothVariant, GDIMammothVariant }, MinTime = DateTime.Minutes(16) },

		------ Anti-tank
		{ Infantry = { "n3", "n3", "n3", "ztrp", "n3", "n3", "ztrp", "ztrp", "ztrp", "ztrp" }, Vehicles = { GDIMammothVariant, "xo", GDIMammothVariant, "xo", GDIMammothVariant, "xo" }, MinTime = DateTime.Minutes(16), RequiredTargetCharacteristics = { "MassHeavy" } },

		------ Anti-infantry
		{ Infantry = { "n3", "n1", "n1", "n1", "n3", "n1", "n1", "n1", "n3", "n1", "n1", "n1", "n1", "n1", "n1", "n1", "n1" }, Vehicles = { "wolv", "wolv", "vulc", "vulc.ai", "wolv", "disr", "jugg", "wolv" }, MinTime = DateTime.Minutes(16), RequiredTargetCharacteristics = { "MassInfantry" } },

		-- Specials
		{ Infantry = { "n2", "n2", "n2", "n2", "n2", "n2", "n2", "n2", "n2", "n2", "n2", "n2", "n2", "n2", "n2", "n2" }, Vehicles = { "htnk.ion", "htnk.ion", "htnk.ion" }, MinTime = DateTime.Minutes(18), IsSpecial = true },
		{ Infantry = {}, Vehicles = { "memp", "memp", "memp", "memp", "memp" }, MinTime = DateTime.Minutes(18), IsSpecial = true },
		{ Infantry = {}, Vehicles = { "mtnk", "vulc", "thwk", "mtnk", "vulc", "thwk", "thwk", "thwk" }, MinTime = DateTime.Minutes(18), IsSpecial = true },
		{ Infantry = { "ztrp", "ztrp", "ztrp", "ztrp", "ztrp", "ztrp", "ztrp", "ztrp" }, Vehicles = { "titn.rail", "titn.rail", "titn.rail", "titn.rail", "titn.rail" }, MinTime = DateTime.Minutes(18), IsSpecial = true },
	},
	Nod = {
		-- 0 to 10 minutes
		{ Infantry = {}, Vehicles = { "bike", "bike", "bike", "bike" }, MaxTime = DateTime.Minutes(10) },
		{ Infantry = { "n3", "n1", "n1", "n1", "n4", "n1", "n1", "n1" }, Vehicles = { "bggy", "bggy", "bike", "bike" }, MaxTime = DateTime.Minutes(10) },
		{ Infantry = { "n3", "n1", "n1", "n4", "n1", "n1", "n1" }, Vehicles = { "ltnk", "bggy", "bike" }, MaxTime = DateTime.Minutes(10) },

		-- 10 minutes onwards
		{ Infantry = { "n3", "n1", "n1", "n1", "n1", "n4", "n3", "bh", "n1", "n1", "n1", "n1", "n1", "n3", "n1", "n1" }, Vehicles = { "ltnk", "ltnk", FlameTankHeavyFlameTankOrHowitzer, "arty.nod" }, MinTime = DateTime.Minutes(10) },
		{ Infantry = { "n3", "n1", "n1", "n1", "n4", "n1", "n3", "n1", "n1", "n1", "n1", "n1", "n1", "n3", "n1", "n1" }, Vehicles = { "ltnk", "arty.nod", FlameTankHeavyFlameTankOrHowitzer, "mlrs" }, MinTime = DateTime.Minutes(10) },
		{ Infantry = { BasicCyborg, BasicCyborg, BasicCyborg, BasicCyborg, "tplr", AdvancedCyborg, "n1c", "n1c", BasicCyborg, AdvancedCyborg }, Vehicles = { "ltnk", FlameTankHeavyFlameTankOrHowitzer, "ltnk" }, MinTime = DateTime.Minutes(10) },

		-- 10 to 16 minutes
		{ Infantry = {}, Vehicles = { "stnk.nod", "sapc.ai", "stnk.nod", "stnk.nod", "sapc.ai" }, MinTime = DateTime.Minutes(10), MaxTime = DateTime.Minutes(16) },

		-- 16 minutes onwards
		{ Infantry = {}, Vehicles = { "stnk.nod", "stnk.nod", "sapc.ai", "stnk.nod", "stnk.nod", "sapc.ai", "stnk.nod" }, MinTime = DateTime.Minutes(16) },
		{ Infantry = { BasicCyborg, BasicCyborg, BasicCyborg, BasicCyborg, BasicCyborg, AdvancedCyborg, "n1c", "n1c", BasicCyborg, BasicCyborg, "rmbc", AdvancedCyborg, AdvancedCyborg }, Vehicles = { "ltnk", "ltnk", FlameTankHeavyFlameTankOrHowitzer, "mlrs" }, MinTime = DateTime.Minutes(16) },

		------ Anti-tank
		{ Infantry = { "n3", "n3", "n1", "n1", "n4", "n1", "n3", "n1", "n1", "n1", "n1", "n1" }, Vehicles = { "ltnk", "ltnk" }, MinTime = DateTime.Minutes(16), RequiredTargetCharacteristics = { "MassHeavy" } },

		------ Anti-infantry
		{ Infantry = { "n4", "n4", "n1", "n1", "n1", "n1", "n1", "n1", "n4", "n4", "n4", "n4", "n1", "n1", "n1", "n1", "n4", "n4" }, Vehicles = { FlameTankHeavyFlameTankOrHowitzer, FlameTankHeavyFlameTankOrHowitzer, "mlrs", FlameTankHeavyFlameTankOrHowitzer, FlameTankHeavyFlameTankOrHowitzer, "mlrs" }, MinTime = DateTime.Minutes(16), RequiredTargetCharacteristics = { "MassInfantry" } },

		-- Specials
		{ Infantry = { "bh", "bh", "bh", "bh", "bh", "bh", "bh", "bh", "bh" }, Vehicles = { "hftk", "hftk", "hftk", "hftk", "hftk", "hftk" }, MinTime = DateTime.Minutes(18), IsSpecial = true },
		{ Infantry = { "n3", "n1", "n1", "n1", "n4", "n1", "n3", "n1", "n1", "n1", "n1", "n1", "n1", "n3", "n1", "n1" }, Vehicles = { "wtnk", "wtnk", "wtnk", "wtnk", "wtnk" }, MinTime = DateTime.Minutes(18), IsSpecial = true },
		{ Infantry = { }, Vehicles = { "bike", "bike", "bike", "bike", "bike", "bike", "bike", "bike", "bike", "bike", "bike", "bike", "bike", "bike" }, MinTime = DateTime.Minutes(18), IsSpecial = true },
		{ Infantry = { "rmbc", "rmbc", "rmbc", "rmbc", "rmbc", "enli", "rmbc", "rmbc", "enli" }, MinTime = DateTime.Minutes(18), IsSpecial = true },
	},
	Scrin = {
		-- 0 to 10 minutes
		{ Infantry = { "s3", "s1", "s1", "s1", "s3", "s3", "s4" }, Vehicles = { "intl.ai2", GunWalkerSeekerOrLacerator, "intl.ai2", GunWalkerSeekerOrLacerator }, MaxTime = DateTime.Minutes(10), },

		-- 10 to 13 minutes
		{ Infantry = { "s3", "s1", "s1", "s1", "s1", "s1", "s2", "s2", "s3", "s3" }, Vehicles = { "intl.ai2", GunWalkerSeekerOrLacerator, "intl.ai2", CorrupterOrDevourer, GunWalkerSeekerOrLacerator, "tpod", GunWalkerSeekerOrLacerator, CorrupterOrDevourer }, MinTime = DateTime.Minutes(10), MaxTime = DateTime.Minutes(13), },

		-- 13 to 19 minutes
		{ Infantry = { "s3", "s1", "s1", "s1", "s1", "s1", "s2", "s2", "s3", "s3", "s3", "s4", "s4" }, Vehicles = { "intl.ai2", GunWalkerSeekerOrLacerator, "intl.ai2", CorrupterOrDevourer, GunWalkerSeekerOrLacerator, TripodVariant, CorrupterOrDevourer }, Aircraft = { PacOrDevastator }, MinTime = DateTime.Minutes(13), MaxTime = DateTime.Minutes(19), },

		------ Anti-infantry
		{ Infantry = { "s3", "s1", "s1", "s1", "s1", "s1", "s2", "s2", "s3", "s3", "s1", "s1", "s1", "s2" }, Vehicles = { "shrw", "corr", "corr", "shrw", "corr", "shrw", "corr", "corr" }, MinTime = DateTime.Minutes(16), RequiredTargetCharacteristics = { "MassInfantry" } },

		------ Anti-tank
		{ Infantry = { "s3", "s4", "s1", "s4", "s4", "s1", "s4", "s4", "s3", "s1", "s4", "s1", "s4", "s4", "s4" }, Vehicles = { "gunw", "devo", "devo", "gunw", "devo", "atmz", "devo", "tpod", "atmz", "devo" }, MinTime = DateTime.Minutes(16), RequiredTargetCharacteristics = { "MassHeavy" } },

		-- 19 minutes onwards
		{ Infantry = { "s3", "s1", "s1", "s1", "s1", "s1", "s2", "s2", "s3", "s3", "s3", "s4", "s4" }, Vehicles = { "intl.ai2", GunWalkerSeekerOrLacerator, "intl.ai2", CorrupterOrDevourer, GunWalkerSeekerOrLacerator, TripodVariant, AtomizerObliteratorOrRuiner }, Aircraft = { PacOrDevastator, "pac" }, MinTime = DateTime.Minutes(19), },

		-- Specials
		{ Infantry = { "brst", "brst", "brst", "brst", "brst", "brst", "brst" }, MinTime = DateTime.Minutes(18), IsSpecial = true },
		{ Infantry = { "s3", "s3", "s2", "s2", "s2", "s2", "s2", "s2", "s2", "s2" }, Vehicles = { "tpod", "tpod", "tpod", "tpod", "tpod", "tpod" }, MinTime = DateTime.Minutes(18), IsSpecial = true },
		{ Infantry = { "s3", "s1", "s1", "s1", "s1", "s3", "s1", "s1", "s1", "s1", "s3", "s1", "s1", "s1", "s1", "s3", "s1", "s1", "s1", "s1", "s3", "s1", "s1", "s1" }, Vehicles = { "stcr", "gunw", "stcr", "gunw", "stcr", "gunw", "stcr" }, MinTime = DateTime.Minutes(18), IsSpecial = true }
	}
}
