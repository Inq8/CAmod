---@type player[]
CoopPlayers = {}

---@type player
local MainPlayer

---@type string[]
SplitOwnerBlacklist = {}

---@type integer
local SharedBank = 0

--- The last player (attempted to be) given a unit in AssignToCoopPlayers.
---@type integer
local LastAssignedCoopID

PrepareBuildingLists = function()
	UnitProducers = Utils.Concat(BarracksTypes, Utils.Concat(FactoryTypes, Utils.Concat(AirProductionTypes, NavalProductionTypes)))

	SharedBuildingLists = {
		allies = { "fact", "powr", "apwr", "tent", "weap", "hpad", "proc", "dome", "atek", "pdox", "weat", "fix", "syrd" },
		soviet = { "fact", "powr", "apwr", "barr", "weap", "afld", "proc", "dome", "stek", "mslo", "fix", "kenn", "spen", "npwr", "tpwr", "ftur", "ttur", "tsla" },
		gdi = { "afac", "nuke", "nuk2", "pyle", "weap.td", "afld.gdi", "proc.td", "hq", "gtek", "eye", "rep", "syrd.gdi" },
		nod = { "afac", "nuke", "nuk2", "hand", "weap.td", "airs", "hpad.td", "proc.td", "hq", "tmpl", "mslo.nod", "rep", "spen.nod", "obli" },
		scrin = { "sfac", "reac", "rea2", "port", "wsph", "grav", "proc.scrin", "nerv", "scrt", "rfgn", "srep" }
	}

	RemoteBuildingLists = {}

	-- copy SharedBuildingLists to RemoteBuildingLists with "coop" prefix
	for faction, buildingTypes in pairs(SharedBuildingLists) do
		RemoteBuildingLists[faction] = {}
		Utils.Do(buildingTypes, function(buildingType)
			RemoteBuildingLists[faction][#RemoteBuildingLists[faction] + 1] = "coop" .. buildingType
		end)
	end

	-- non-shared buildings (build limit 1)
	-- "alhq", "cvat", "indp", "munp", "orep", "upgc", "tmpp", "sign"

	RemoteExits =
	{
		barr = CVec.New(0, 1),
		tent = CVec.New(0, 1),
		weap = CVec.New(1, 1),
		spen = CVec.New(2, 2),
		syrd = CVec.New(2, 2),
		kenn = CVec.New(0, 0),
		afld = CVec.New(1, 1),
		hpad = CVec.New(1, 0),
		hand = CVec.New(1, 1),
		pyle = CVec.New(0, 1),
		["weap.td"] = CVec.New(1, 1),
		airs = CVec.New(1, 1),
		["hpad.td"] = CVec.New(1, 0),
		["afld.gdi"] = CVec.New(1, 1),
		["spen.nod"] = CVec.New(2, 2),
		["syrd.gdi"] = CVec.New(2, 2),
		port = CVec.New(0, 1),
		grav = CVec.New(1, 1),
		wsph = CVec.New(1, 1)
	}
end

CACoopQueueSyncer = function()
	Trigger.AfterDelay(1, function()
		Utils.Do(CoopPlayers,function(p)
			Actor.Create("QueueUpdaterDummy", true, { Owner = p })
		end)
	end)
end

---@param action fun(player: player)
ForEachPlayer = function(action)
	Utils.Do(CoopPlayers, action)
end

---@return integer
GetCoopPlayerCount = function()
	return #Utils.Where(CoopPlayers, function(cp)
		return cp ~= nil
	end)
end

---@return player[]
GetEvenPlayers = function()
	local players = { }

	--[[
		I've argued with myself if the current player count should be used
		instead of the initial head count. Without more tweaks, I think the
		first has fewer problems in the event of a disconnect. ~ JF
	]]
	for i = 2, GetCoopPlayerCount(), 2 do
		if CoopPlayers[i] then
			players[#players + 1] = CoopPlayers[i]
		end
	end

	return players
end

---@return player[]
GetOddPlayers = function()
	local players = { }

	for i = 1, GetCoopPlayerCount(), 2 do
		if CoopPlayers[i] then
			players[#players + 1] = CoopPlayers[i]
		end
	end

	return players
end

---@return player
RandomCoopPlayer = function()
	return Utils.Random(CoopPlayers)
end

---@param unit actor
---@return boolean
IsOwnedByCoopPlayer = function(unit)
	return Utils.Any(CoopPlayers, function(player)
		return unit.Owner == player
	end)
end

---@param player player
---@param players player[]
---@return boolean
IsPlayerInList = function(player, players)
	return Utils.Any(players, function(p)
		return player == p
	end)
end

GetCoopGroundAttackers = function()
	local attackers = { }

	ForEachPlayer(function(player)
		Utils.Concat(attackers, player.GetGroundAttackers())
	end)

	return attackers
end

---@param player player
local function MaintainBotMoney(player)
	local RefineryType = "anyrefinery"
	local BaseBuilderType = "anyconyard"
	local CanBuildBase = player.HasPrerequisites({ BaseBuilderType })
	local HasNoRefineries = not player.HasPrerequisites({ RefineryType })
	local cost = Actor.Cost("proc")
	local bank = player.Cash + player.Resources

	if bank < cost and CanBuildBase and HasNoRefineries then
		player.Cash = cost
	end
end

---@param bot player
---@param interval? integer
local function UpdateCoopBot(bot, interval)
	MaintainBotMoney(bot)
	local mcvs = bot.GetActorsByType("mcv")

	if not interval then
		return
	end

	Trigger.AfterDelay(interval, function()
		UpdateCoopBot(bot, interval)
	end)
end

local function StartCoopBots()
	local interval = DateTime.Seconds(5)
	local stagger = 3

	local bots = Utils.Where(CoopPlayers, function(player)
		return player.IsBot
	end)

	Utils.Do(bots, function(bot)
		UpdateCoopBot(bot, interval + stagger)
		stagger = stagger + stagger
	end)
end

---@param unit actor
ScatterIfAble = function(unit)
	if unit.HasProperty("Scatter") then
		unit.Scatter()
	end
end

MoveDownIfAble = function(unit)
	if unit.HasProperty("Move") then
		unit.Move(unit.Location + CVec.New(0, 1))
	end
end

---@param unit actor
---@return boolean
local function CanSplitAmongPlayers(unit)
	local matched = Utils.Any(SplitOwnerBlacklist, function(cab)
		return unit.Type == cab
	end)

	return not matched
end

--- Split the ownership of a group among the different co-op players.
---@param units actor[]
---@param specificPlayers? player[]
AssignToCoopPlayers = function(units, specificPlayers, Ignoreblacklist)
	if not Ignoreblacklist then
		units = Utils.Where(units, CanSplitAmongPlayers)
	end

	local playerCount = GetCoopPlayerCount()
	local ownerID = LastAssignedCoopID or 0

	-- Rotate through the player list and assign a
	-- unit to each until no more units remain.
	Utils.Do(units, function(unit)
		if unit.Type ~= "player" then
			ownerID = ownerID + 1

			if ownerID > playerCount then
				ownerID = 1
			end

			LastAssignedCoopID = ownerID
			local newOwner = CoopPlayers[ownerID]
			local valid = newOwner and (specificPlayers == nil or IsPlayerInList(newOwner, specificPlayers))

			if not valid then
				return
			end

			unit.Owner = newOwner

			if unit.HasProperty("HasPassengers") then
				Trigger.AfterDelay(1, function()
					AssignToCoopPlayers(Utils.Where(unit.Passengers, function(a) return not a.IsDead end))
				end)
			end
		end
	end)
end

---@param units actor[]
AssignToEvenPlayers = function(units)
	AssignToCoopPlayers(units, GetEvenPlayers())
end

---@param units actor[]
AssignToOddPlayers = function(units)
	AssignToCoopPlayers(units, GetOddPlayers())
end

GoodSpread = function()
	Trigger.AfterDelay(5, GoodSpread)

	if StopSpread ~= true then
		local actors = Utils.Where(SinglePlayerPlayer.GetActors(), function(a) return a.HasProperty("Move") and not IsHarvester(a) and not IsMcv(a) end)

		if #actors >= 1 then
			AssignToCoopPlayers(actors)
		end
	end
end

local function SecondaryObjectivesRequired()
	local SecondaryMissionsText = "Complete all other Primary and Secondary objectives."
	SecondarysRequired = MainPlayer.AddObjective(SecondaryMissionsText)
	local NumAllObjectives = 0
	local NumAllCompleted = 0
	Trigger.OnObjectiveAdded(MainPlayer, function(_, obid)
		if obid ~= SecondarysRequired then
			NumAllObjectives = NumAllObjectives + 1
		end
	end)
	Trigger.OnObjectiveCompleted(MainPlayer, function(_, obid)
		if obid ~= SecondarysRequired then
			NumAllCompleted = NumAllCompleted + 1
			if NumAllCompleted >= NumAllObjectives and NumAllObjectives > 0 then
				Trigger.AfterDelay(DateTime.Seconds(1), function()
					Utils.Do(CoopPlayers,function(PID)
						PID.MarkCompletedObjective(SecondarysRequired)
					end)
				end)
			end
		end
	end)
	Trigger.OnObjectiveFailed(MainPlayer, function(_, obid)
		if obid ~= SecondarysRequired then
			Trigger.AfterDelay(DateTime.Seconds(1), function()
				Utils.Do(CoopPlayers,function(PID)
					PID.MarkFailedObjective(SecondarysRequired)
				end)
			end)
		end
	end)
end

local function SyncObjectives()
	local texts = {
		primary = "Primary",
		secondary = "Secondary",
		newPrimary = "New primary objective",
		newSecondary = "New secondary objective"
	}

	Trigger.OnObjectiveAdded(MainPlayer, function(_, obid)
		local description = MainPlayer.GetObjectiveDescription(obid)
		local type = MainPlayer.GetObjectiveType(obid)
		local required = type == texts.primary

		ForEachPlayer(function(player)
			player.AddObjective(description, type, required)
			local OBJcolour = HSLColor.Yellow
			if required then
				Media.DisplayMessageToPlayer(player, description, texts.newPrimary, OBJcolour)
			else
				OBJcolour = HSLColor.Gray
				Media.DisplayMessageToPlayer(player, description, texts.newSecondary, OBJcolour)
			end
		end)
	end)

	ForEachPlayer(function(player)
		Trigger.OnPlayerWon(player, function()
			Trigger.AfterDelay(DateTime.Seconds(1), function()
				Media.PlaySpeechNotification(player, "Win")
			end)
		end)

		Trigger.OnPlayerLost(player, function()
			for i, v in ipairs(CoopPlayers) do
				if v == player then
					table.remove(CoopPlayers, i)
					break
				end
			end

			Trigger.AfterDelay(DateTime.Seconds(1), function()
				Media.PlaySpeechNotification(player, "Lose")
			end)

			local surrenderMessages = {
				"PID's not surrenderin'! PID's passed on! This Commander is no more! They have ceased to be! PID's expired and gone to meet their maker! PID's a stiff! Bereft of life, PID rests in peace! If you hadn't nailed them to the playerlist, PID'd be pushing up the daisies! Their metabolic processes are now history! PID's off the twig! PID's kicked the bucket, PID's shuffled off their mortal coil, run down the curtain and joined the bleedin' choir invisible!! THIS IS AN EX-COMMANDER!!",
				"We noticed that PID went AWOL. All troops under their command will be reassigned.",
				"We are sad to announce that PID is lost in the Combat Zone. We can't afford a search party. All units, regroup.",
				"PID has abandoned the operation. Their assets are now under joint command.",
				"Commander PID has failed to report in. All units will be redistributed.",
				"PID is MIA. Remaining forces are now reassigned to active commanders.",
				"Reports confirm that PID is no longer in the fight. Reallocating resources.",
				"High Command suspects PID was compromised. Their troops are now yours.",
				"PID pulled out. Their units remain. Use them wisely.",
				"We've lost contact with PID. Taking control of their remaining forces.",
				"PID has been deemed unfit for command. Reassigning assets.",
				"No further transmissions from PID. Their troops now fall under unified command.",
				"High Command regrets to inform that PID has been silenced. Units are being reassigned.",
				"Another Commander down: PID. Their legacy continues through their troops.",
				"PID's command channel went dark. Redirecting all forces to surviving operatives.",
				"Surrender confirmed from PID. Their units will continue the fight without them.",
				"PID's resignation has been accepted... by force. Reassigning units.",
				"Satellite link to PID severed. Their war assets are now at your disposal.",
				"Combat stress got the better of PID. Picking up the slack.",
				"Casualty of war: PID. All operable units reassigned to remaining players.",
				"Command vacancy filled. PID's units will continue under new leadership.",
				"PID has paid the ultimate price. Their forces are yours to command.",
				"War spares no one. PID has fallen. Their troops remain.",
				"PID has been promoted to civilian. By force. Units reassigned.",
				"PID tripped on a landmine and career-ending shame. Units reassigned.",
				"PID forgot to pay their command subscription. Reallocating troops.",
				"PID's command authority revoked. Initiating redistribution of forces.",
				"Command integrity of PID compromised. Units transferring to secure channels.",
				"PID's signal is gone. Let their sacrifice not be in vain.",
				"Command silence from PID. Reallocation of units underway.",
				"PID fell to the chaos of war. Their forces continue the mission.",
				"Confirmed KIA: PID. Taking operational control of remaining assets.",
				"PID's command integrity shattered. Their war effort continues through us.",
				"Another ghost in the fog: PID. Let their units be our resolve.",
				"Transmission lost. PID is no more. We fight on.",
				"PID has ragequit real life. You get their toys.",
				"PID left the oven on. They've gone home. You're in charge now.",
				"PID suffered from sudden strategic incompetence. Assets reallocated.",
				"PID experienced spontaneous desk flipping. Their troops are free real estate.",
				"Last known words of PID: 'Watch this!' Reassigning units.",
				"PID achieved a higher state of 'not our problem'. You take it from here.",
				"Command code: FAIL-STATE. PID's forces now under community management.",
				"We've promoted PID to field observer. Very, very far from the field.",
				"PID has been debriefed from active duty. Units reassigned.",
				"Command slot vacated: PID. Assets redistributed.",
				"PID no longer reports to HQ. Taking direct control of their forces.",
				"Operational handover complete for PID. Troops reassigned.",
				"Command continuity protocol activated. PID's assets now reassigned.",
				"PID has disengaged. Their units now fall under surviving command.",
				"Control signal lost from PID. Integrating their forces.",
				"PID has relinquished control. Remaining assets transferred."
			}

			--Media.DisplayMessage("Number of Bullshit Messages: " .. #surrenderMessages)
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				if Messagecooldown ~= true then
					Messagecooldown = true
					local MessageIndex = Utils.RandomInteger(1, #surrenderMessages)
					local selectedMessage = surrenderMessages[MessageIndex]
					local playerName = player.Name
					if selectedMessage ~= nil then
						local formattedMessage = string.gsub(selectedMessage, "PID", playerName)
						Media.DisplayMessage(formattedMessage, "High Command", player.Color)
					end
				end
			end)


			Trigger.AfterDelay(DateTime.Seconds(8), function()
				AssignToCoopPlayers(player.GetActors(), CoopPlayers)
				local EstateCash = (player.Cash + player.Resources)
				EstateCash = (EstateCash / #CoopPlayers)
				Utils.Do(CoopPlayers, function(PID)
					PID.Cash = PID.Cash + EstateCash
				end)
				Messagecooldown = false
			end)

			Trigger.AfterDelay(DateTime.Seconds(10), function()
				--Failsafe if reassignment isnt possible
				local Deathlist = player.GetActors()
				Utils.Do(Deathlist,function(UID)
					if UID.Type ~= "player" and not UID.IsDead and UID.HasProperty(Health) then
						UID.Kill()
					end
				end)
			end)

		end)
	end)

	Trigger.OnObjectiveCompleted(MainPlayer, function(_, obid)
		ForEachPlayer(function(player)
			player.MarkCompletedObjective(obid)
			if player.IsLocalPlayer then
				Media.PlaySoundNotification(player, "AlertBleep")
				Media.DisplayMessage(MainPlayer.GetObjectiveDescription(obid), "Objective completed", HSLColor.LimeGreen)
			end
		end)
	end)

	Trigger.OnObjectiveFailed(MainPlayer, function(_, obid)
		ForEachPlayer(function(player)
			player.MarkFailedObjective(obid)
			if player.IsLocalPlayer then
				Media.PlaySoundNotification(player, "AlertBleep")
				Media.DisplayMessage(MainPlayer.GetObjectiveDescription(obid), "Objective failed", HSLColor.Red)
			end
		end)
	end)
end

---@param unit actor
---@param produced boolean
---@param original? actor
local function OrderCopiedUnit(unit, produced, original, isAircraft)
	ScatterIfAble(unit)
	Trigger.AfterDelay(60, function()
		if unit.IsDead == true or unit.IsInWorld == false then
			return
		end
		local behavior
		if not produced then
			behavior = Utils.Random(SBehaviours)
		elseif produced then
			behavior = Utils.Random(PBehaviours)
		else
			return
		end
		local IsAggro = false
		if isAircraft == true then
			InitAttackAircraft(unit)
			return
		end
		Trigger.OnDamaged(unit,function(self, attacker, damage)
			if IsAggro == false then
				IsAggro = true
				unit.Stop()
				IdleHunt(unit)
			end
		end)
		Trigger.OnKilled(unit,function()
			Trigger.ClearAll(unit)
			return
		end)
		--Media.DisplayMessage("Random Behaviour: " .. behavior)
		if behavior == "normal" then
			if original and unit.HasProperty("Guard") then
				Trigger.OnIdle(unit, function()
					if original.IsDead then
						unit.Hunt()
						return
					end
					unit.Guard(original)
				end)
			end
			--Send the Unit to hunt if Guarding takes too long to prevent clogging the Map
			if produced then
				Trigger.AfterDelay(DateTime.Seconds(60), function()
					if unit.IsDead == false and unit.IsInWorld == true then
						unit.Stop()
						unit.Hunt()
						return
					end
				end)
			end
		end

		if behavior == "hunt" then
			IdleHunt(unit)
			return
		end

		if behavior == "wander" then
			StartWandering(unit)
		end

		if behavior == "idle" then
			StartIdling(unit)
		end
	end)
end

StartIdling = function(unit)
	local idletime = Utils.RandomInteger(20, 120)
	Trigger.AfterDelay(DateTime.Seconds(idletime), function()
		if unit.IsDead == false and unit.IsInWorld == true then
			ScatterIfAble(unit)
			StartIdling(unit)
		end
	end)
end

StartWandering = function(unit)
	local wandertarget = Map.RandomCell()
	local wandertime = Utils.RandomInteger(5, 30)
	Trigger.AfterDelay(DateTime.Seconds(wandertime), function()
		if unit.IsDead == false and unit.IsInWorld == true then
			unit.AttackMove(wandertarget, 5)
			StartWandering(unit)
		end
	end)
end

--- Create a remote building to mimic players sharing base buildings and tech.
--- If this can produce units, those units will be produced at the map edge,
--- and possibly moved to the last created building of the desired type.
---@param owner player Owner of the new remote building.
---@param originalType string The "real" building type, where units can appear.
---@param remoteType string The remote building type.
local function CreateRemoteBuilding(owner, originalType, remoteType)
	--Media.DisplayMessage("Remotebuilding created, Type " .. originalType)
	local remote = Actor.Create(remoteType, true, { Owner = owner, Location = originalLocation })
	local offset = RemoteExits[originalType]
	CACoopQueueSyncer()

	--[[if not offset then
		-- One of three things is true: this is not a factory, map edge
		-- production is desired, or somebody forgot to add a remote exit CVec.
		return
	end]]
	local IsProducer = false
	Utils.Do(UnitProducers, function(UPID)
		if UPID == originalType then
			IsProducer = true
		end
	end)

	if IsProducer == true then
		Trigger.OnProduction(remote, function(producer, produced)
			--Media.DisplayMessage(tostring(originalType) .. " is the Remote Building Type.")
			local primary

			-- Use the newest player-created producer as the "primary building".
			ForEachPlayer(function(player)
				local realProducers = player.GetActorsByType(originalType)
				if #realProducers > 0 then
					primary = realProducers[#realProducers]
				end
			end)

			if not primary then
				-- It seems all factories of this type have been wiped
				-- out before the shared prerequisites were updated.
				--print(produced.Type .. " produced by " .. tostring(produced.Owner) .. " lacks a spawn building. Refunded.")
				producer.Owner.Cash = producer.Owner.Cash + Actor.Cost(produced.Type)
				produced.Destroy()
				--Media.DisplayMessage(produced.Type .. " produced by " .. tostring(produced.Owner) .. " lacks a spawn building. Refunded.")
				return
			end

			local exit = primary.Location + offset
			local remoteUnit = Actor.Create(produced.Type, true, { Owner = producer.Owner, Location = exit })
			MoveDownIfAble(remoteUnit)
			produced.Owner = Neutral
			produced.IsInWorld = false
			produced.Destroy()
			--Media.DisplayMessage(produced.Type .. " produced by " .. tostring(produced.Owner) .. " has a spawn building. Produced.")
		end)
	end
end

--- Periodically update the co-op team's prerequisites, which can
--- be shared through the creation/destruction of remote buildings.
local function UpdateCoopPrequisites()
	Trigger.AfterDelay(5, UpdateCoopPrequisites)

	if not TechShared then
		return
	end

	for faction, sharedBuildingTypesForFaction in pairs(SharedBuildingLists) do
		local factionPlayers = Utils.Where(CoopPlayers, function(player)
			return player.Faction == faction or (ExtraPrerequisiteFactions and Utils.Any(ExtraPrerequisiteFactions, function(ef) return ef == faction end))
		end)

		if #factionPlayers > 0 then
			local teamBuildings = {}
			local teamRemoteBuildings = {}
			local playerBuildingTypes = {}

			for _, player in ipairs(factionPlayers) do
				playerBuildingTypes[player.InternalName] = {}

				local playerBuildings = player.GetActorsByTypes(sharedBuildingTypesForFaction)
				Utils.Do(playerBuildings, function(b)
					teamBuildings[b.Type] = true
					playerBuildingTypes[player.InternalName][b.Type] = true
				end)

				local playerRemoteBuildings = player.GetActorsByTypes(RemoteBuildingLists[faction])
				Utils.Do(playerRemoteBuildings, function(rb)
					teamRemoteBuildings[rb.Type] = true
				end)
			end

			-- if the team has a building, ensure all players have it or its remote equivalent
			for _, buildingType in ipairs(sharedBuildingTypesForFaction) do
				local teamHasBuilding = teamBuildings[buildingType] == true

				Utils.Do(factionPlayers, function(player)
					local remoteType = "coop" .. buildingType
					local teamHasRemoteBuilding = teamRemoteBuildings[remoteType] == true

					if playerBuildingTypes[player.InternalName][buildingType] or (not teamHasBuilding and teamHasRemoteBuilding) then
						local remoteBuildings = player.GetActorsByType(remoteType)
						Utils.Do(remoteBuildings, function(remote)
							remote.Destroy()
						end)
					elseif not playerBuildingTypes[player.InternalName][buildingType] and teamHasBuilding and not teamHasRemoteBuilding then
						CreateRemoteBuilding(player, buildingType, remoteType)
					end
				end)
			end
		end
	end
end

---@param enemyPlayer player
local function MultiplyEnemyStartingUnits(enemyPlayer)
	local multiplier
	local playerCount = GetCoopPlayerCount()

	if Map.LobbyOption("enmp") == "999"  then
		multiplier = playerCount - 1
	else
		multiplier = tonumber(Map.LobbyOptionOrDefault("enmp", "0"))
	end

	if multiplier == 0 then
		return
	end

	local enemyUnits = enemyPlayer.GetGroundAttackers()

	local ValidAircrafts = {}

	if AirCraftMulti == "domulti" then
		local FoundAircrafts = {}
		ValidAircrafts = {"b2b","mig","suk","suk.upg","yak","p51","heli","u2","u2.killzone","smig","a10","a10.sw","a10.gau","a10.bomber","apch","orca","orcb","uav","rah","kiro","harr","scrn","venm","auro","pmak","beag","phan","kamv","shde","vert","mcor","disc","jack","stmr","torm","enrv","deva","pac","inva","mshp"}
		FoundAircrafts = enemyPlayer.GetActorsByTypes(ValidAircrafts)
		Utils.Do(FoundAircrafts,function(UID)
			table.insert(enemyUnits,UID)
		end)
	end

	Utils.Do(enemyUnits, function(original)
		local types = { }

		for _ = 1, multiplier do
			types[#types + 1] = original.Type
		end

		Reinforcements.Reinforce(enemyPlayer, types, { original.Location }, 0, function(copy)
			local isAircraft = Utils.Any(ValidAircrafts,function(CID)
				if copy.Type == CID then
					return true
				else
					return false
				end
			end)
			OrderCopiedUnit(copy, false, original, isAircraft)
		end)
	end)
end

---@param enemyPlayer player
local function MultiplyEnemyProduction(enemyPlayer)
	local multiplier
	local playerCount = GetCoopPlayerCount()

	if Map.LobbyOption("prmp") == "999" then
		multiplier = playerCount - 1
	else
		multiplier = 0 + tonumber(Map.LobbyOptionOrDefault("prmp", "0"))
	end

	if multiplier == 0 then
		return
	end

	Trigger.OnAnyProduction(function(_, unit)
		if unit.Owner ~= enemyPlayer then
			return
		end
		local AttackerFilter = unit.Owner.GetGroundAttackers()
		local ValidAircrafts = {}
		if AirCraftMulti == "domulti" then
			local FoundAircrafts = {}

			ValidAircrafts = {"b2b","mig","suk","suk.upg","yak","p51","heli","u2","u2.killzone","smig","a10","a10.sw","a10.gau","a10.bomber","apch","orca","orcb","uav","rah","kiro","harr","scrn","venm","auro","pmak","beag","phan","kamv","shde","vert","mcor","disc","jack","stmr","torm","enrv","deva","pac","inva","mshp"}
			FoundAircrafts = enemyPlayer.GetActorsByTypes(ValidAircrafts)

			Utils.Do(FoundAircrafts,function(UID)
				table.insert(AttackerFilter,UID)
			end)
		end

		Utils.Do(AttackerFilter, function(UID)
			if unit == UID then
				ScatterIfAble(unit)
				local types = { }

				for _ = 1, multiplier do
					types[#types + 1] = unit.Type
				end

				Reinforcements.Reinforce(unit.Owner, types, { unit.Location }, 0, function(copy)
					local original = unit

					local isAircraft = Utils.Any(ValidAircrafts,function(CID)
						if copy.Type == CID then
							return true
						else
							return false
						end
					end)

					if isAircraft == true then
						OrderCopiedUnit(copy, true, original, isAircraft)
					else
						OrderCopiedUnit(copy, true, original)
					end
				end)
				return
			end
		end)
	end)
end

local function SetExtraMines()
	if Map.LobbyOption("oremines") == "oreoff" then
		--Media.DisplayMessage("Extra Mines are turned off.")
		return
	end
	if Map.LobbyOption("oremines") == "oreon" or Map.LobbyOption("oremines") == "oreonupgrade" then
		local OreMineList = { ExtraMine1, ExtraMine2, ExtraMine3, ExtraMine4, ExtraMine5, ExtraMine6, ExtraMine7, ExtraMine8, ExtraMine9, ExtraMine10 }
		local GemMineList = { ExtraGemMine1, ExtraGemMine2, ExtraGemMine3, ExtraGemMine4, ExtraGemMine5, ExtraGemMine6, ExtraGemMine7, ExtraGemMine8, ExtraGemMine9, ExtraGemMine10, ExtraDiamondMine1, ExtraDiamondMine2, ExtraDiamondMine3, ExtraDiamondMine4, ExtraDiamondMine5, ExtraDiamondMine6, ExtraDiamondMine7, ExtraDiamondMine8, ExtraDiamondMine9, ExtraDiamondMine10 }
		local GreenBlossomList = { ExtraBlossom1, ExtraBlossom2, ExtraBlossom3, ExtraBlossom4, ExtraBlossom5, ExtraBlossom6, ExtraBlossom7, ExtraBlossom8, ExtraBlossom9, ExtraBlossom10 }
		local BlueBlossomList = { ExtraBlueBlossom1, ExtraBlueBlossom2, ExtraBlueBlossom3, ExtraBlueBlossom4, ExtraBlueBlossom5, ExtraBlueBlossom6, ExtraBlueBlossom7, ExtraBlueBlossom8, ExtraBlueBlossom9, ExtraBlueBlossom10 }

		Utils.Do(OreMineList, function(actor)
			if actor then
				Actor.Create("mine", true, { Location = actor.Location, Owner = Neutral })
				--Media.DisplayMessage("Extra Ore Mine created.")
			end
		end)
		Utils.Do(GemMineList, function(actor)
			if actor then
				Actor.Create("gmine", true, { Location = actor.Location, Owner = Neutral })
				--Media.DisplayMessage("Extra Gem Mine created.")
			end
		end)
		Utils.Do(GreenBlossomList, function(actor)
			if actor then
				Actor.Create("split2", true, { Location = actor.Location, Owner = Neutral })
			end
		end)
		Utils.Do(BlueBlossomList, function(actor)
			if actor then
				Actor.Create("splitblue", true, { Location = actor.Location, Owner = Neutral })
			end
		end)
	end
	if Map.LobbyOption("oremines") == "oreupgrade" or Map.LobbyOption("oremines") == "oreonupgrade" then
		Trigger.AfterDelay(2, function()
			local AllT1Spawners = {}
			AllT1Spawners = Neutral.GetActorsByTypes({"split2", "split3", "mine"})
			Utils.Do(AllT1Spawners,function(SID)
				local Spawnertype = SID.Type
				local Spawnerlocation = SID.Location
				if Spawnertype == "mine" then
					Spawnertype = "gmine"
				elseif Spawnertype == "split2" or Spawnertype == "split3" then
					Spawnertype = "splitblue"
				end
				SID.Destroy()
				Actor.Create(Spawnertype, true, { Location = Spawnerlocation, Owner = Neutral })
			end)
		end)
	end
	if Map.LobbyOption("oremines") == "orefinite" then
		Trigger.AfterDelay(2, function()
			local AllSpawners = {}
			AllSpawners = Neutral.GetActorsByTypes({"split2", "split3", "splitblue", "mine"})
			Utils.Do(AllSpawners,function(SID)
				SID.Destroy()
			end)
			Media.DisplayMessage("All resource spawners are deleted now. Good luck!")
		end)
	end
end

IncomeSharing = function()
	Utils.Do(CoopPlayers, function(PID)
		-- Handle 100% Shared: Send everything to SharedBank
		if IncomePercentage == 100 then
			SharedBank = SharedBank + PID.Resources
			PID.Resources = 0
		-- Handle 999% Shared: Send everything to everyone
		elseif IncomePercentage == 999 then
			if PID.Resources > 0 then
				Utils.Do(CoopPlayers,function(PID2)
					PID2.Cash = PID2.Cash + PID.Resources
				end)
				--Media.DisplayMessage(PID.Resources .. "$ distributed to all players.")
				PID.Resources = 0
			end
		else
			-- Store resources in the buffer for non-100% sharing
			if PID.Resources > 0 then
				ResourceBuffer[PID] = ResourceBuffer[PID] + PID.Resources
				PID.Resources = 0  -- Ensure resources are fully moved to buffer
			end

			-- Convert Buffered Resources into Cash & SharedBank when threshold is met
			local shareThreshold = 1 + (IncomePercentage / 100)  -- Example: If IncomePercentage = 30, this would be 1.3

			while ResourceBuffer[PID] >= shareThreshold do
				-- Pay the player 1$ in cash
				PID.Cash = PID.Cash + 1

				-- Transfer the shared portion to SharedBank
				local shareAmount = shareThreshold - 1
				SharedBank = SharedBank + shareAmount

				-- Reduce buffer accordingly
				ResourceBuffer[PID] = ResourceBuffer[PID] - shareThreshold
			end
		end
	end)

	-- Distribute Shared Account Money when there's enough in SharedBank
	if SharedBank >= #CoopPlayers then
		local fullDollars = math.floor(SharedBank / #CoopPlayers)  -- Calculate the full dollars to distribute
		local remainder = SharedBank - (fullDollars * #CoopPlayers)  -- Calculate the remaining SharedBank value

		-- Distribute the full dollars to each player
		Utils.Do(CoopPlayers, function(PID)
			PID.Cash = PID.Cash + fullDollars
		end)

		-- Remaining SharedBank goes back to the SharedBank after distribution
		SharedBank = remainder
		--Media.DisplayMessage((fullDollars * #CoopPlayers) .. "$ distributed " .. remainder .. "$ left in Shared Account.")
	end

	-- Loop with a delay to keep running
	Trigger.AfterDelay(1, IncomeSharing)
end

EnemyVeterancy = function(mainEnemies)
	--Small delay for the Multiplicators
	Trigger.AfterDelay(5, function()
		local EnLevel = tonumber(Map.LobbyOption("enemyranks"))
		if EnLevel ~= 0 then
			--Level up all Starting Units
			Utils.Do(mainEnemies, function(EID)
				Utils.Do(EID.GetActors(), function(UID)
					if UID.HasProperty("CanGainLevel") == true then
						UID.GiveLevels(EnLevel, true)
					end
				end)
			end)
			--Level up all Produced Units
			Trigger.OnAnyProduction(function(producer, UID)
				Utils.Do(mainEnemies, function(EID)
					if UID.Owner == EID and UID.HasProperty("CanGainLevel") == true then
						if UID.Level == 0 then
							UID.GiveLevels(EnLevel, true)
						end
					end
				end)
			end)
			--Level up eventual Reinforcements with periodic Checks
			LateEnemyVeterancy(mainEnemies,EnLevel)
		end
	end)
end

LateEnemyVeterancy = function(mainEnemies, EnLevel)
	Utils.Do(mainEnemies, function(EID)
		Utils.Do(EID.GetActors(), function(UID)
			if UID.HasProperty("CanGainLevel") == true then
				if UID.Level == 0 then
					UID.GiveLevels(EnLevel, true)
				end
			end
		end)
	end)
	Trigger.AfterDelay(60, function()
		LateEnemyVeterancy(mainEnemies,EnLevel)
	end)
end

StartCashSpread = function(MinStartCash)
	if MinStartCash == nil then
		MinStartCash = 2500 --This should be enough for a power plant and a refinery for each player
	end
	local StartCash = MainPlayer.Cash / #CoopPlayers
	if StartCash < MinStartCash then
		StartCash = MinStartCash
	end
	Utils.Do(CoopPlayers, function(p)
		p.Cash = StartCash
	end)
end

CoopInit = function()
	SBehaviours = {}
	PBehaviours = {}

	AirCraftMulti = Map.LobbyOption("multiplyaircraft")

	local Pfollow = tonumber(Map.LobbyOption("pfollow"))
	local Phunt = tonumber(Map.LobbyOption("phunt"))
	local Pwander = tonumber(Map.LobbyOption("pwander"))
	local Pidle = tonumber(Map.LobbyOption("pidle"))

	local Sfollow = tonumber(Map.LobbyOption("sfollow"))
	local Shunt = tonumber(Map.LobbyOption("shunt"))
	local Swander = tonumber(Map.LobbyOption("swander"))
	local Sidle = tonumber(Map.LobbyOption("sidle"))

	for i = 1, Pfollow do
		table.insert(PBehaviours,"normal")
	end

	for i = 1, Phunt do
		table.insert(PBehaviours,"hunt")
	end

	for i = 1, Pwander do
		table.insert(PBehaviours,"wander")
	end

	for i = 1, Pidle do
		table.insert(PBehaviours,"idle")
	end

	for i = 1, Sfollow do
		table.insert(SBehaviours,"normal")
	end

	for i = 1, Shunt do
		table.insert(SBehaviours,"hunt")
	end

	for i = 1, Swander do
		table.insert(SBehaviours,"wander")
	end

	for i = 1, Sidle do
		table.insert(SBehaviours,"idle")
	end

	if #SBehaviours == 0 then
		SBehaviours = {"idle"}
	end

	if #PBehaviours == 0 then
		PBehaviours = {"idle"}
	end

	MainPlayer = SinglePlayerPlayer
	CoopPlayers = MissionPlayers

	local mainEnemies = MissionEnemies

	PrepareBuildingLists()

	local baseSharingValue = Map.LobbyOption("basesharing")

	if baseSharingValue == "1" then
		McvPerPlayer = false
		TechShared = true
	elseif baseSharingValue == "2" then
		McvPerPlayer = true
		TechShared = true
	else
		McvPerPlayer = true
		TechShared = false
	end

	GoodSpread()
	SyncObjectives()

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		if Map.LobbyOption("secondariesrequired") == "required" then
			SecondaryObjectivesRequired()
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		OverrideProductionSpeeds()

		Utils.Do(mainEnemies, function(player)
			MultiplyEnemyProduction(player)
			MultiplyEnemyStartingUnits(player)
		end)

		IncomeshareLobbyoption = Map.LobbyOption("incomeshare")

		if MoneyShareOverride ~= nil then
			if IncomeshareLobbyoption == "999" and MoneyShareOverride == 100 then
				MoneyShareOverride = "999"
			end
			IncomeshareLobbyoption = MoneyShareOverride
		end

		IncomePercentage = tonumber(IncomeshareLobbyoption)

		if IncomePercentage ~= 0 and #CoopPlayers >= 2 then
			ResourceBuffer = {}

			Utils.Do(CoopPlayers, function(PID)
				ResourceBuffer[PID] = 0
			end)

			SharedBank = 0

			IncomeSharing()
		end

		SetExtraMines()
		originalLocation = CPos.New(3, 3)
		UpdateCoopPrequisites()
		StartCoopBots()
		EnemyVeterancy(mainEnemies)
	end)
end

OverrideProductionSpeeds = function()
	local ProSpeedDeductor = 1
	if Map.LobbyOption("enprotime") == "999"  then
		ProSpeedDeductor = 1 - (#CoopPlayers * 0.1)
	elseif Map.LobbyOption("enprotime") == "998"  then
		ProSpeedDeductor = 1 - (#CoopPlayers * 0.15)
	else
		ProSpeedDeductor = tonumber(Map.LobbyOption("enprotime"))
	end
	UnitBuildTimeMultipliers[Difficulty] = UnitBuildTimeMultipliers[Difficulty] * ProSpeedDeductor
end

-------------------

TransferBaseToPlayer = function(fromPlayer, toPlayer)
	Trigger.AfterDelay(1, function()
		local baseActors = Utils.Where(fromPlayer.GetActors(), function(a)
			return a.HasProperty("StartBuildingRepairs") or IsHarvester(a) or Utils.Any(WallTypes, function(t) return a.Type == t end)
		end)
		Utils.Do(baseActors, function(a)
			a.Owner = toPlayer
		end)
	end)
end

TransferMcvsToPlayers = function()
	local mcvs = SinglePlayerPlayer.GetActorsByTypes(McvTypes)
	local toPlayers = GetMcvPlayers()
	Utils.Do(mcvs, function(mcv)
		mcv.Owner = toPlayers[1]
		if McvPerPlayer then
			Utils.Do(toPlayers, function(p)
				if p ~= toPlayers[1] then
					local copy = Actor.Create(mcv.Type, true, { Owner = p, Location = mcv.Location })
					ScatterIfAble(copy)
				end
			end)
		end
	end)
end

GetFirstActivePlayer = function()
	for _, p in ipairs(MissionPlayers) do
		if p.PlayerIsActive then
			return p
		end
	end
	return nil
end

GetMcvPlayers = function()
	if McvPerPlayer then
		return CoopPlayers
	else
		local firstActive = GetFirstActivePlayer()
		if firstActive then
			return { firstActive }
		else
			return {}
		end
	end
end
