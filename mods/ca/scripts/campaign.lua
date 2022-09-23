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

InitObjectives = function(player)
	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
		Media.PlaySoundNotification(player, "AlertBleep")
	end)

	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
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

AttackAircraftTargets = { }
InitializeAttackAircraft = function(aircraft, targetPlayer, targetTypes)
	Trigger.OnIdle(aircraft, function()
		local actorId = tostring(aircraft)
		local target = AttackAircraftTargets[actorId]

		if not target or not target.IsInWorld then
			if targetTypes ~= nil then
				target = ChooseRandomTargetOfTypes(aircraft, targetPlayer, targetTypes)
			else
				target = ChooseRandomTarget(aircraft, targetPlayer)
			end
		end

		if target then
			AttackAircraftTargets[actorId] = target
			aircraft.Attack(target)
		else
			AttackAircraftTargets[actorId] = nil
			aircraft.ReturnToBase()
		end
	end)
end

ChooseRandomTarget = function(unit, targetPlayer)
	local target = nil
	local enemies = Utils.Where(targetPlayer.GetActors(), function(self)
		return self.HasProperty("Health") and unit.CanTarget(self) and not Utils.Any({ "sbag", "fenc", "brik", "cycl", "barb" }, function(type) return self.Type == type end)
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
		return self.HasProperty("Health") and self.HasProperty("StartBuildingRepairs") and unit.CanTarget(self) and not Utils.Any({ "sbag", "fenc", "brik", "cycl", "barb" }, function(type) return self.Type == type end)
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
		Trigger.OnIdle(actor, actor.Hunt)
	end
end

ClearTriggersStopAndHunt = function(a)
	if not a.IsDead then
		Trigger.ClearAll(a)
		a.Stop()
		if a.HasProperty("Hunt") then
			a.Hunt()
		end
	end
end

AutoRepairBuildings = function(player)
	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == player and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(a)
		AutoRepairBuilding(a, player)
	end)
end

AutoRepairBuilding = function(a, player)
	if a.IsDead then
		return
	end
	Trigger.OnDamaged(a, function(building)
		if building.Owner == player and building.Health < (building.MaxHealth * 75 / 100) then
			building.StartBuildingRepairs()
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
		if attacker.EffectiveOwner == player then
			return
		end
		local rand = Utils.RandomInteger(1,100)
		if rand > 100 - chance then
			if unit.HasProperty("Attack") and not unit.IsDead then
				unit.Stop()
				unit.Attack(attacker)
			end
		end
	end)
end

CallForHelpOnDamaged = function(actor, filter)
	Trigger.OnDamaged(actor, function(self, attacker, damage)

		if not self.HasTag("helpCalled") then
			self.AddTag("helpCalled")
			local nearbyUnits = Map.ActorsInCircle(self.CenterPosition, WDist.New(5120), filter)

			Utils.Do(nearbyUnits, function(nearbyUnit)
				if not actor.IsDead and not actor.HasTag("idleHunt") then
					nearbyUnit.AddTag("idleHunt")
					IdleHunt(nearbyUnit)
				end
			end)
		end
	end)
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

	-- go through each queue for the current difficulty
	Utils.Do(queues, function(queue)
		ProduceNextAttackSquadUnit(squad, queue, 1)
	end)
end

InitAirAttackSquad = function(squad, player, targetPlayer, targetTypes)
	squad.AirTargetPlayer = targetPlayer
	squad.AirTargetTypes = targetTypes
	InitAttackSquad(squad, player)
end

ProduceNextAttackSquadUnit = function(squad, queue, unitIndex)
	local units = squad.Units[Difficulty][queue]

	-- if there are no more units to build for this queue, check if any other queues are producing, if not produce next attack squad after interval
	if unitIndex > #units then
		squad.QueueProductionStatuses[queue] = false
		if not IsSquadInProduction(squad) then
			Trigger.AfterDelay(DateTime.Seconds(2), function()
				if squad.AirTargetTypes ~= nil and squad.AirTargetPlayer ~= nil then
					Utils.Do(squad.IdleUnits, function(a)
						if not a.IsDead and a.IsInWorld then
							InitializeAttackAircraft(a, squad.AirTargetPlayer, squad.AirTargetTypes)
						end
					end)
					squad.IdleUnits = { }
				else
					SendAttackSquad(squad)
				end
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
				-- add produced unit to list of idle units for the squad
				Trigger.OnProduction(producer, function(p, produced)
					squad.IdleUnits[#squad.IdleUnits + 1] = produced

					if produced.HasProperty("HasPassengers") and not produced.IsDead then
						Trigger.OnPassengerExited(produced, function(t, p)
							IdleHunt(p)
						end)
					end

					TargetSwapChance(produced, squad.Player, 10)
				end)

				producer.Produce(nextUnit)

				-- clear the OnProduction trigger as other squads may be produced from the same building
				Trigger.AfterDelay(1, function()
					Trigger.ClearAll(producer)
				end)

				-- restore repair trigger
				if producer.HasProperty("StartBuildingRepairs") then
					Trigger.AfterDelay(2, function()
						AutoRepairBuilding(producer, squad.Player)
					end)
				end
			end

			-- start producing the next unit
			ProduceNextAttackSquadUnit(squad, queue, unitIndex + 1)
		end)
	end
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
	Utils.Do(squad.IdleUnits, function(a)
		if not a.IsDead and a.IsInWorld then
			if squad.AttackPaths ~= nil then
				a.Patrol(Utils.Random(squad.AttackPaths), false)
			end
		end
		IdleHunt(a)
	end)
	squad.IdleUnits = { }
end
