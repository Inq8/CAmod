---@type player[]
CoopPlayers = {}

---@type boolean[]
CoopPlayersSet = {}

---@type player
local MainPlayer

Iterations = 0

---@type string[]
SplitOwnerBlacklist = {}

--- The last player (attempted to be) given a unit in AssignToCoopPlayers.
---@type integer
local SharedBank = 0
local LastAssignedCoopID

local StartNotification = "StartGame"
local Movies = {}
local MovieStyle = Map.LobbyOption("fmvstyle", "fmvfull")
--local MultipliedUnitBehavior = Map.LobbyOption("multibehave", "normal")
--local MovieStyle = "fmvfull"
--local MultipliedUnitBehavior = "normal"

CoopScrinMCVs = function(Mainplayer,MCVPlayers,WarpInLocation)
	if #MCVPlayers > 1 then
		local wormhole = Actor.Create("wormhole", true, { Owner = DummyGuy, Location = WarpInLocation.Location })
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Media.PlaySpeechNotification(All, "ReinforcementsArrived")
			Notification("Reinforcements have arrived.")
			Beacon.New(DummyGuy, WarpInLocation.CenterPosition)

			Utils.Do(MCVPlayers, function(PID)
				if PID ~= Mainplayer then
					if (PID.Cash + PID.Resources) < 3000 then
						PID.Cash = 3000
					end
					local ExtraSMCV = Actor.Create("smcv", true, { Owner = PID, Location = WarpInLocation.Location })
					ScatterIfAble(ExtraSMCV)
				end
			end)
		end)

		Trigger.AfterDelay(DateTime.Seconds(5), function()
			wormhole.Kill()
		end)
	end
end

PrepareBuildingLists = function()
	---Prepare Building Lists for Sharing
	if ORAMod == "ra" then
		SharedBuildingList = { "powr", "apwr", "barr", "tent", "proc", "weap", "fact", "dome", "atek", "stek", "spen", "syrd", "kenn", "tsla", "ftur", "fix", "hpad", "afld"}
		UnitProducers = { "barr", "tent", "weap", "spen", "syrd", "kenn", "hpad", "afld" }
	end

	if ORAMod == "td" then
		SharedBuildingList = { "proc", "weap", "fact", "fact.gdi", "fact.nod", "fix", "hpad", "afld", "pyle", "hand", "nuke", "nuk2", "hq", "eye", "tmpl"  }
		UnitProducers = { "weap", "hpad", "afld", "pyle", "hand" }
	end

	if ORAMod == "ca" then
		SharedBuildingList = { "mslo", "spen", "syrd", "pdox", "tsla", "dome", "atek", --[["alhq",]] "npwr", "stek", "hq", "gtek", "tmpl", --[["cvat", "indp", "munp", "orep", "upgc", "tmpp",]] "ftur", "powr", "apwr", "weap", "fact", "proc", "hpad", "afld", "tpwr", "barr", "kenn", "tent", "fix", "pyle", "hand", "weap.td", "airs", "nuke", "afac", "obli", "eye", "rep", "hpad.td", "proc.td", "afld.gdi", "pris", "spen.nod", "syrd.gdi", "ttur", "mslo.nod", "weat", "sfac", "reac", "rea2", "proc.scrin", "wsph", "nerv", "scrt", "grav", --[["sign",]] "srep", "rfgn" }
		UnitProducers = { "spen", "syrd", "weap", "hpad", "afld", "barr", "kenn", "tent", "pyle", "hand", "weap.td", "airs", "hpad.td", "afld.gdi", "spen.nod", "syrd.gdi", "port", "wsph", "grav" }
	end

	if ORAMod == "tibalt" then
		SharedBuildingList = { "proc", "weap", "fact", "fact.gdi", "fact.nod", "fix", "hpad", "afld", "pyle", "hand", "nuke", "nuk2", "hq", "eye", "tmpl", "powr", "apwr", "pdox"}
		UnitProducers = { "weap", "hpad", "afld", "pyle", "hand" }
	end

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
	if ORAMod == "ca" then
		Utils.Do(CoopPlayers,function(PID)
			local QueueSyncer = Actor.Create("COOPQUEUESYNCER", true, { Owner = PID, Location = CPos.New(3, 3) })
			Trigger.AfterDelay(1, function()
				QueueSyncer.Owner = Neutral
				QueueSyncer.IsInWorld = false
				QueueSyncer.Destroy()
			end)
		end)
	end
end

---@param action fun(player: player)
ForEachPlayer = function(action)
	Utils.Do(CoopPlayers, action)
end

---@param action fun(player: player)
ForEachExtraPlayer = function(action)
	Utils.Do(GetExtraPlayers(), action)
end

---@return integer
GetCoopPlayerCount = function()
	return #Utils.Where(CoopPlayers, function(cp)
		return cp ~= nil
	end)
end

---@return player[]
GetExtraPlayers = function()
	return Utils.Where(CoopPlayers, function(player)
		return player ~= MainPlayer
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

---@return boolean
CoopTeamHasNoRequiredUnits = function()
	return Utils.All(CoopPlayers, function(player)
		return player.HasNoRequiredUnits()
	end)
end

---@param player player
local function MaintainBotMoney(player)
	local RefineryType = "proc"
	local BaseBuilderType = "fact"
	local CanBuildBase = player.HasPrerequisites({ BaseBuilderType })
	local HasNoRefineries = not player.HasPrerequisites({ RefineryType })
	local cost = Actor.Cost(RefineryType)
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
	print("MCV list for " .. tostring(bot) .. " (" .. tostring(#mcvs) .. " entries):")

	if ORAMod ~= "ca" then
		Utils.Do(mcvs, function(mcv)
			print(tostring(mcv))

			Trigger.OnIdle(mcv, function()
				mcv.Deploy()
				mcv.CallFunc(mcv.Scatter)
			end)
		end)
	end

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

---@param aircraft actor
InitializeCoopAttackAircraft = function(aircraft)
	local enemyPlayer = RandomCoopPlayer()
	local target

	Trigger.OnIdle(aircraft, function()
		if not enemyPlayer then
			enemyPlayer = RandomCoopPlayer()
			return
		end

		if not target or not target.IsInWorld then
			target = ChooseRandomTarget(aircraft, enemyPlayer)
		end

		if target then
			aircraft.Attack(target)
		else
			aircraft.ReturnToBase()
		end
	end)
end

---@param unit actor
---@return boolean
local function CanSplitAmongPlayers(unit)
	local matched = Utils.Any(SplitOwnerBlacklist, function(cab)
		return unit.Type == cab
	end)

	return not matched
end

---@param unit actor
---@param newOwner player
local function AssignOnceAddedToWorld(unit, newOwner)
	local done = false

	Trigger.OnAddedToWorld(unit, function()
		if done then
			return
		end

		done = true
		unit.Owner = newOwner
	end)
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
		local valid = specificPlayers == nil or IsPlayerInList(newOwner, specificPlayers)

		if not valid then
			--print("Would-be owner " .. tostring(newOwner) .. " has been skipped.")
			return
		end

		--print("Reassigning unit " .. tostring(unit.Type) .. " to " .. tostring(CoopPlayers[ownerID]))
		if not unit.IsInWorld then
			-- Should cover units created by Reinforce or similar functions.
			AssignOnceAddedToWorld(unit, newOwner)
			return
		end

		unit.Owner = newOwner
		end
	end)
end

--- Split ownership of a transport's passengers among the co-op players.
--- Standard assignment should work for most transport reinforcements,
--- but some cases like paratrooper planes are more fiddly.
---@param transport actor
---@param specificPlayers? player[]
AssignPassengersToCoopPlayers = function(transport, specificPlayers)
	if transport.IsInWorld and transport.HasPassengers then
		print(tostring(transport) .. " already in world. Using standard passenger assignment.")
		AssignToCoopPlayers(transport.Passengers, specificPlayers)
		return
	end

	local assigned = false

	Trigger.OnAddedToWorld(transport, function()
		-- Wait a tick to ensure passengers are created and loaded.
		Trigger.AfterDelay(1, function()
			if assigned or not transport.IsInWorld then
				return
			end

			assigned = true
			AssignToCoopPlayers(transport.Passengers, specificPlayers)
		end)
	end)
end

AssignToExtraPlayers = function(units)
	AssignToCoopPlayers(units, GetExtraPlayers())
end

---@param units actor[]
AssignToEvenPlayers = function(units)
	AssignToCoopPlayers(units, GetEvenPlayers())
end

---@param units actor[]
AssignToOddPlayers = function(units)
	AssignToCoopPlayers(units, GetOddPlayers())
end

---@param types string[]
---@param goal cpos|cpos[]
---@param interval? integer
---@param specificPlayers? player[]
---@return actor[]
ReinforceCoopUnits = function(types, goal, interval, specificPlayers)
	if type(goal) ~= "table" then
		goal = { goal }
	end

	local units = Reinforcements.Reinforce(MainPlayer, types, goal, interval or 0, ScatterIfAble)
	AssignToCoopPlayers(units, specificPlayers or CoopPlayers)
	return units
end

---@param type string
---@param goal cpos|cpos[]
---@param interval? integer
---@param specificPlayers? player[]
---@return actor[]
ReinforceUnitPerPlayer = function(type, goal, interval, specificPlayers)
	local units = { }

	Utils.Do(specificPlayers or CoopPlayers, function()
		units[#units + 1] = type
	end)

	return ReinforceCoopUnits(units, goal, interval, specificPlayers or CoopPlayers)
end

---@param goal cpos|cpos[]
---@param interval? integer
---@param faction? string
---@param facing? wangle
---@return actor[]
ReinforceExtraMCVs = function(goal, interval, faction, facing)
	if BaseShared then
		return { }
	end

	if not faction then
		local mcvs = ReinforceUnitPerPlayer("mcv", goal, interval, GetExtraPlayers())

		Utils.Do(mcvs, function(mcv)
			if not mcv.Owner.IsBot then
				return
			end

			Trigger.OnAddedToWorld(mcv, function()
				UpdateCoopBot(mcv.Owner)
			end)
		end)

		return mcvs
	end

	-- Special case for certain missions (like Tanya's Tale)
	-- where faction-specific MCVs are desired.
	interval = interval or 0
	local delay = 0
	local mcvs = { }

	if type(goal) ~= "table" then
		goal = { goal }
	end

	ForEachExtraPlayer(function(player)
		local ordered = false
		local mcv = Actor.Create("mcv", false, { Owner = player, Location = goal[1], Faction = faction, Facing = facing or Angle.South })
		mcvs[#mcvs + 1] = mcv

		Trigger.AfterDelay(delay, function()
			mcv.IsInWorld = true
		end)

		Trigger.OnAddedToWorld(mcv, function()
			if ordered then
				return
			end

			ordered = true
			Utils.Do(goal, mcv.Move)
			mcv.CallFunc(mcv.Scatter)

			if mcv.Owner.IsBot then
				UpdateCoopBot(mcv.Owner)
			end
		end)

		delay = delay + interval
	end)

	return mcvs
end

GoodSpread = function()
	Trigger.AfterDelay(5, GoodSpread)
	if Stopspread ~= true then
		local attackers = Utils.Where(InitialCoopUnitsOwner.GetActors(), function(a) return a.HasProperty("Move") and not a.HasProperty("Land") end)

		if #attackers >= 1 then
			if WeightedSpread ~= true then
			AssignToCoopPlayers(attackers)
			else
			--WeightedAssignToCoopPlayers(InitialCoopUnitsOwner.GetGroundAttackers())
			end
		end
	end
end

local function SecondaryObjectivesRequired()
	local SecondaryMissionsText = "Complete all other Primary and Secondary objectives."
	if ORAMod ~= "ca" then
		SecondarysRequired = AddPrimaryObjective(MainPlayer, SecondaryMissionsText)
	else
		SecondarysRequired = MainPlayer.AddObjective(SecondaryMissionsText)
	end
	local NumAllObjectives = 0
	local NumAllCompleted = 0
	Trigger.OnObjectiveAdded(MainPlayer, function(_, obid)
		if obid ~= SecondarysRequired then
			NumAllObjectives = NumAllObjectives + 1
			--Media.DisplayMessage("Objectives " .. NumAllCompleted .. "/" .. NumAllObjectives)
		end
	end)
	Trigger.OnObjectiveCompleted(MainPlayer, function(_, obid)
		if obid ~= SecondarysRequired then
			NumAllCompleted = NumAllCompleted + 1
			--Media.DisplayMessage("Objectives " .. NumAllCompleted .. "/" .. NumAllObjectives)
			if NumAllCompleted >= NumAllObjectives and NumAllObjectives > 0 then
				Trigger.AfterDelay(DateTime.Seconds(1), function()
					Utils.Do(CoopPlayers,function(PID)
						PID.MarkCompletedObjective(SecondarysRequired)
					end)
					--MainPlayer.MarkCompletedObjective(SecondarysRequired)
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
				--MainPlayer.MarkFailedObjective(SecondarysRequired)
			end)
		end
	end)
end

local function SyncObjectives()
	-- Translate some reusable text and store it.
	if ORAMod ~= "ca" and ORAMod ~= "tibalt" then
		texts =
		{
			primary = UserInterface.GetFluentMessage("primary"),
			secondary = UserInterface.GetFluentMessage("secondary"),
			newPrimary = UserInterface.GetFluentMessage("new-primary-objective"),
			newSecondary = UserInterface.GetFluentMessage("new-secondary-objective")
		}
	else
		texts =
		{
			primary = "Primary",
			secondary = "Secondary",
			newPrimary = "New primary objective",
			newSecondary = "New secondary objective"
		}
	end

	--Syncing new Objectives
	Trigger.OnObjectiveAdded(MainPlayer, function(_, obid)
		local description = MainPlayer.GetObjectiveDescription(obid)
		local type = MainPlayer.GetObjectiveType(obid)
		local required = type == texts.primary

		ForEachExtraPlayer(function(player)
			-- Using AddObjective directly to avoid duplicate translations
			-- from the usual AddPrimaryObjective or AddSecondaryObjective.
			player.AddObjective(description, type, required)
			if ORAMod ~= "ca" and ORAMod ~= "tibalt" then
				if required then
					Media.DisplayMessageToPlayer(player, description, texts.newPrimary)
				else
					Media.DisplayMessageToPlayer(player, description, texts.newSecondary)
				end
			elseif ORAMod == "tibalt" then
				if player.IsLocalPlayer == true then
					if required then
						Media.DisplayMessage(player, description, texts.newPrimary)
					else
						Media.DisplayMessage(player, description, texts.newSecondary)
					end
				end
			elseif ORAMod == "ca" then
				local OBJcolour = HSLColor.Yellow
				if required then
					Media.DisplayMessageToPlayer(player, description, texts.newPrimary, OBJcolour)
				else
					OBJcolour = HSLColor.Gray
					Media.DisplayMessageToPlayer(player, description, texts.newSecondary, OBJcolour)
				end
			end
		end)
	end)

	ForEachExtraPlayer(function(player)
		Trigger.OnPlayerWon(player, function()
			Trigger.AfterDelay(DateTime.Seconds(1), function()
				Media.PlaySpeechNotification(player, "Win")
			end)
		end)

		Trigger.OnPlayerLost(player, function()
			--Media.DisplayMessage("Does this shit even trigger?", "High Command", player.Color)
			for i, v in ipairs(CoopPlayers) do
				if v == player then
					table.remove(CoopPlayers, i)
					--return  -- Exit after removing the first occurrence
				end
			end
			for i, v in ipairs(MCVPlayers) do
				if v == player then
					table.remove(MCVPlayers, i)
					--return
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
				--Media.DisplayMessage("Failsafe triggered")
				local Deathlist = player.GetActors()
				Utils.Do(Deathlist,function(UID)
					if UID.Type ~= "player" and not UID.IsDead and UID.HasProperty(Health) then
						UID.Kill()
						--Media.DisplayMessage("Killed as Failsafe: " .. UID.Type)
					end
				end)
			end)

		end)
	end)

	--[[
		Unsure why these were commented out, but I've fiddled with them.
		Unless there's something I've missed, they should work in place
		of the per-player marks without issues.
	]]

	if OldObjSync == nil then
		Trigger.OnObjectiveCompleted(MainPlayer, function(_, obid)
			ForEachExtraPlayer(function(player)
				player.MarkCompletedObjective(obid)
				if ORAMod == "ca" and player.IsLocalPlayer then
					Media.PlaySoundNotification(player, "AlertBleep")
					Media.DisplayMessage(MainPlayer.GetObjectiveDescription(obid), "Objective completed", HSLColor.LimeGreen)
				end
			end)
		end)

		Trigger.OnObjectiveFailed(MainPlayer, function(_, obid)
			ForEachExtraPlayer(function(player)
				player.MarkFailedObjective(obid)
				if ORAMod == "ca" and player.IsLocalPlayer then
					Media.PlaySoundNotification(player, "AlertBleep")
					Media.DisplayMessage(MainPlayer.GetObjectiveDescription(obid), "Objective failed", HSLColor.Red)
				end
			end)
		end)
	end
end

local function TibAltSyncObjectives()
	local texts =
		{
			primary = "Primary",
			secondary = "Secondary",
			newPrimary = "New primary objective",
			newSecondary = "New secondary objective"
		}

	--Syncing new Objectives
	Trigger.OnObjectiveAdded(MainPlayer, function(_, obid)
		local description = MainPlayer.GetObjectiveDescription(obid)
		local type = MainPlayer.GetObjectiveType(obid)
		local required = type == texts.primary

		ForEachExtraPlayer(function(player)
			-- Using AddObjective directly to avoid duplicate translations
			-- from the usual AddPrimaryObjective or AddSecondaryObjective.
			player.AddObjective(description, type, required)
		end)
	end)
	Trigger.OnObjectiveCompleted(MainPlayer, function(_, obid)
		ForEachExtraPlayer(function(player)
			player.MarkCompletedObjective(obid)
		end)
	end)

	Trigger.OnObjectiveFailed(MainPlayer, function(_, obid)
		ForEachExtraPlayer(function(player)
			player.MarkFailedObjective(obid)
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
			CAInitAttackAircraft (unit, nil, nil, nil)
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

CAInitAttackAircraft = function(aircraft, targetPlayer, targetList, targetType)
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
						local RandomTargetPlayer = RandomCoopPlayer()
						target = ChooseRandomTarget(self, RandomTargetPlayer)
					end
				end

				if target.IsDead == false and target.IsInWorld == true then
					AttackAircraftTargets[actorId] = target
					self.AttackMove(target.Location)
				else
					AttackAircraftTargets[actorId] = nil
					self.ReturnToBase()
				end
			end
		end)
	end
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
		if ORAMod == "ca" then
			MoveDownIfAble(remoteUnit)
		else
			ScatterIfAble(remoteUnit)
		end
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
	--Media.DisplayMessage("Updating Prequisites")
	if not TechShared then
		--Media.DisplayMessage("Nothing to Update")
		return
	end

	Utils.Do(SharedBuildingList, function(buildingType)
		--Media.DisplayMessage("Checking for Prequs")
		local teamHasPrerequisite = false

		ForEachPlayer(function(player)
			if teamHasPrerequisite or #player.GetActorsByType(buildingType) > 0 then
				teamHasPrerequisite = true
			end
		end)

		ForEachPlayer(function(player)
			local remoteType = "coop" .. buildingType
			local remoteBuildings = player.GetActorsByType(remoteType)

			if not teamHasPrerequisite or #player.GetActorsByType(buildingType) > 0 then
				-- Either no buildings of the original type remain
				-- or this player does not need a remote substitute.
				Utils.Do(remoteBuildings, function(remote)
					remote.Destroy()
					--Media.DisplayMessage("Remotebuilding destroyed")
				end)

				return
			end

			if #remoteBuildings == 0 then
				-- The team has the prerequisite, but this individual does not.
				-- Share the base/tech with a new remote building.
				--Media.DisplayMessage("Trying to create Remotebuilding")
				CreateRemoteBuilding(player, buildingType, remoteType)
			end
		end)
	end)
end

---@param enemyPlayer player
local function MultiplyEnemyStartingUnits(enemyPlayer)
	local multiplier
	local playerCount = GetCoopPlayerCount()

	if Map.LobbyOption("enmp") == "999"  then
		multiplier = playerCount - 1
	else
		if ORAMod ~= "tibalt" then
			multiplier = tonumber(Map.LobbyOptionOrDefault("enmp", "0"))
		else
			multiplier = tonumber(Map.LobbyOption("enmp", "0"))
		end
	end

	if multiplier == 0 then
		return
	end

	local enemyUnits = enemyPlayer.GetGroundAttackers()

	local ValidAircrafts = {}

	if AirCraftMulti == "domulti" then

		local FoundAircrafts = {}
		if ORAMod == "ca" then
			ValidAircrafts = {"b2b","mig","suk","suk.upg","yak","p51","heli","u2","u2.killzone","smig","a10","a10.sw","a10.gau","a10.bomber","apch","orca","orcb","uav","rah","kiro","harr","scrn","venm","auro","pmak","beag","phan","kamv","shde","vert","mcor","disc","jack","stmr","torm","enrv","deva","pac","inva","mshp"}
			FoundAircrafts = enemyPlayer.GetActorsByTypes(ValidAircrafts)
			--Media.DisplayMessage(#enemyUnits .. " Ground Attackers found. " .. #ValidAircrafts .. " Aircraft Types. " .. #FoundAircrafts .. " Aircrafts found.")
		end
		Utils.Do(FoundAircrafts,function(UID)
			table.insert(enemyUnits,UID)
		end)
		--Media.DisplayMessage(#enemyUnits .. " Units overall found.")
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
			if isAircraft == true and ORAMod == "ca" then
				OrderCopiedUnit(copy, false, original, isAircraft)
			else
				OrderCopiedUnit(copy, false, original)
			end
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
		if ORAMod ~= "tibalt" then
			multiplier = 0 + tonumber(Map.LobbyOptionOrDefault("prmp", "0"))
		else
			multiplier = 0 + tonumber(Map.LobbyOption("prmp", "0"))
		end
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
			if ORAMod == "ca" then
				ValidAircrafts = {"b2b","mig","suk","suk.upg","yak","p51","heli","u2","u2.killzone","smig","a10","a10.sw","a10.gau","a10.bomber","apch","orca","orcb","uav","rah","kiro","harr","scrn","venm","auro","pmak","beag","phan","kamv","shde","vert","mcor","disc","jack","stmr","torm","enrv","deva","pac","inva","mshp"}
				FoundAircrafts = enemyPlayer.GetActorsByTypes(ValidAircrafts)
			end
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

					if isAircraft == true and ORAMod == "ca" then
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
		if ORAMod == "ra" then
			local OreMineList = { ExtraMine1, ExtraMine2, ExtraMine3, ExtraMine4, ExtraMine5, ExtraMine6, ExtraMine7, ExtraMine8, ExtraMine9, ExtraMine10 }
			local GemMineList = { ExtraGemMine1, ExtraGemMine2, ExtraGemMine3, ExtraGemMine4, ExtraGemMine5, ExtraGemMine6, ExtraGemMine7, ExtraGemMine8, ExtraGemMine9, ExtraGemMine10, ExtraDiamondMine1, ExtraDiamondMine2, ExtraDiamondMine3, ExtraDiamondMine4, ExtraDiamondMine5, ExtraDiamondMine6, ExtraDiamondMine7, ExtraDiamondMine8, ExtraDiamondMine9, ExtraDiamondMine10 }

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
		end
		if ORAMod == "td" or ORAMod == "tibalt" then
			local GreenBlossomList = { ExtraBlossom1, ExtraBlossom2, ExtraBlossom3, ExtraBlossom4, ExtraBlossom5, ExtraBlossom6, ExtraBlossom7, ExtraBlossom8, ExtraBlossom9, ExtraBlossom10 }
			local BlueBlossomList = { ExtraBlueBlossom1, ExtraBlueBlossom2, ExtraBlueBlossom3, ExtraBlueBlossom4, ExtraBlueBlossom5, ExtraBlueBlossom6, ExtraBlueBlossom7, ExtraBlueBlossom8, ExtraBlueBlossom9, ExtraBlueBlossom10 }

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

		if ORAMod == "ca" then
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
	end
	if Map.LobbyOption("oremines") == "oreupgrade" or Map.LobbyOption("oremines") == "oreonupgrade" then
		Trigger.AfterDelay(2, function()
			local AllT1Spawners = {}
			if ORAMod == "ra" then
				AllT1Spawners = Neutral.GetActorsByTypes({"mine"})
			end

			if ORAMod == "td" or ORAMod == "tibalt" then
				AllT1Spawners = Neutral.GetActorsByTypes({"split2", "split3"})
			end

			if ORAMod == "ca" then
				AllT1Spawners = Neutral.GetActorsByTypes({"split2", "split3", "mine"})
			end
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
			if ORAMod == "ra" then
				AllSpawners = Neutral.GetActorsByTypes({"mine", "gmine"})
			end

			if ORAMod == "td" or ORMod == "tibalt" then
				AllSpawners = Neutral.GetActorsByTypes({"split2", "split3", "splitblue"})
			end

			if ORAMod == "ca" then
				AllSpawners = Neutral.GetActorsByTypes({"split2", "split3", "splitblue", "mine"})
			end
			Utils.Do(AllSpawners,function(SID)
				SID.Destroy()
			end)
			Media.DisplayMessage("All resource spawners are deleted now. Good luck!")
		end)
	end
end

---@param filename string
---@param type string
---@param after fun()
PlayMovie = function(filename, type, after)
	if not filename then
		after()
		return
	end

	if MovieStyle == "fmvfull" then
		Media.PlayMovieFullscreen(filename, after)
		return
	elseif MovieStyle == "fmvradar" then
		Media.PlayMovieInRadar(filename, after)
		return
	end

	-- Mixed rules from here on.
	if type == "briefing" then
		Media.PlayMovieFullscreen(filename, after)
	elseif type == "intro" then
		Media.PlayMovieFullscreen(filename, after)
	elseif type == "opening" then
		Media.PlayMovieInRadar(filename, after)
	else -- Unknown type? Assume we're mid-mission and use the radar.
		Media.PlayMovieInRadar(filename, after)
	end
end

PlayIntro = function()
	Media.StopMusic()
	PlayMovie(Movies.extra, "intro", PlayBriefing)
end

PlayBriefing = function()
	PlayMovie(Movies.briefing, "briefing", PlayOpening)
end

PlayOpening = function()
	PlayMovie(Movies.opening, "opening", PlayStartSounds)
end

PlaySoundtrack = function()
	-- Need to test and be extra certain that onPlayComplete
	-- and PlayMusic are both local only. ~ JF
	Media.PlayMusic(nil, PlaySoundtrack)
end

PlayStartSounds = function()
	-- The normal notification is skipped in the rules to avoid hearing it
	-- at the start of fullscreen movies. Now that those are done, we play the
	-- sound. Some mission rules like those in Production Disruption and
	-- Mousetrap specify a different notification or may even skip it.
	if StartNotification and string.lower(StartNotification) ~= "skip" and ORAMod ~= "ca" then
		Media.PlaySpeechNotification(nil, StartNotification)
	end
	PlaySoundtrack()
end

IncomeSharing = function()
    Utils.Do(CoopPlayers, function(PID)
        -- Handle 100% Shared: Send everything to SharedBank
        if Incomepercentage == 100 then
            SharedBank = SharedBank + PID.Resources
            PID.Resources = 0
		-- Handle 999% Shared: Send everything to everyone
		elseif Incomepercentage == 999 then
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
            local shareThreshold = 1 + (Incomepercentage / 100)  -- Example: If Incomepercentage = 30, this would be 1.3

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

LateEnemyVeterancy = function(mainEnemies,EnLevel)
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
		MinStartCash = 2500 --This should be enough for a Power Plant and a Refinery for each Player
	end
	local StartCash = MainPlayer.Cash / #CoopPlayers
	if StartCash < MinStartCash then
		StartCash = MinStartCash
	end
	Utils.Do(CoopPlayers,function(PID)
		PID.Cash = StartCash
	end)
end

CoopInit = function()
--This is the backwards compatibility Layer for Maps created with the older Version of the Base Script from before the Joviahaul.

	if Mainplayer == nil then
		Mainplayer = Greece
	end
	--Media.DisplayMessage(Mainplayer.InternalName .. " is the Main Player.")
	if Enemyplayer == nil then
		Enemyplayer = USSR
	end
	--Media.DisplayMessage(Enemyplayer.InternalName .. " is the Main Enemy.")
	local Enemylist = { Enemyplayer }
	if Enemyplayer2 ~= nil then
		table.insert(Enemylist, Enemyplayer2)
	end

	if Dummyplayer == nil then
		Dummyplayer = Player.GetPlayer("GoodGuy")
	end
	--Media.DisplayMessage(Dummyplayer.InternalName .. " is the Dummy Player.")
	local coopInfo =
	{
		FMVExtra = FMVExtra,
		FMVBriefing = FMVBriefing,
		FMVOpening = FMVOpening,
		Mainplayer = Mainplayer,
		MainEnemies = Enemylist,
		Dummyplayer = Dummyplayer
	}
	SpreadBlacklist = {}
	SplitOwnerBlacklist = SpreadBlacklist
	CoopCurrent = 1
	--Use the old method of Objective Syncing in Mission Script
	OldObjSync = true
	CoopInit25(coopInfo)
end

CoopInit25 = function(mission)

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

	if ORAMod ~= "tibalt" then
		MovieStyle = Map.LobbyOptionOrDefault("fmvstyle", "fmvfull")
	end

	Movies.extra = mission.FMVExtra
	Movies.opening = mission.FMVOpening
	Movies.briefing = mission.FMVBriefing
	StartNotification = mission.StartNotification or StartNotification
	MainPlayer = mission.Mainplayer
	InitialCoopUnitsOwner = mission.Dummyplayer

	local mainEnemies = mission.MainEnemies or { }

	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Neutral = Player.GetPlayer("Neutral")
	GoodGuy = Player.GetPlayer("GoodGuy")
	BadGuy = Player.GetPlayer("BadGuy")

	if ORAMod == nil then
		ORAMod = "ra"
	end

	PrepareBuildingLists()

	if InitialCoopUnitsOwner == nil then
		InitialCoopUnitsOwner = GoodGuy
	end

	if Map.LobbyOption("basesharing") == "1" or Map.LobbyOption("basesharing") == "2" then
        BaseShared = true
    else
		BaseShared = false
	end
	--BaseShared = Map.LobbyOptionOrDefault("basesharing", "2") == 1
	TechShared = BaseShared --or Map.LobbyOption("basesharing") == 2

	--Media.DisplayMessage(MainPlayer.InternalName .. " is the Main Player after compatibility layer.")
	--Media.DisplayMessage(mainEnemies[1].InternalName .. " is the Main Enemy after compatibility layer.")
	--Media.DisplayMessage(Dummyplayer.InternalName .. " is the Dummy Player after compatibility layer.")

	CoopPlayers = { MainPlayer, Multi1, Multi2, Multi3, Multi4, Multi5 }
	CoopPlayers = Utils.Where(CoopPlayers, function(player)
		return player ~= nil
	end)
	if ORAMod == "ca" then
		MissionPlayers = CoopPlayers
		OverrideAttackTarget()
	end

	if ORAMod == "tibalt" then
		TibAltShuffleMissionPlayer()
	end

	MCVPlayers = {}
	if Map.LobbyOption("basesharing") ~= "1" then
        MCVPlayers = CoopPlayers
    end

	if Map.LobbyOption("basesharing") == "1" then
        table.insert(MCVPlayers, MainPlayer)
    end

	if InitialCoopUnitsOwner and Stopspread ~= true then
		local initialUnits = Utils.Where(MainPlayer.GetActors(), function(a) return a.HasProperty("Move") and not a.HasProperty("Land") end)

		Utils.Do(initialUnits, function(unit)
			unit.Owner = InitialCoopUnitsOwner
		end)
	end
	GoodSpread()

	if ORAMod ~= "tibalt" then
		SyncObjectives()
	elseif ORAMod == "tibalt" then
		TibAltSyncObjectives()
	end


	Trigger.AfterDelay(DateTime.Seconds(1), function()
		if Map.LobbyOption("secondariesrequired") == "required" then
			SecondaryObjectivesRequired()
		end
		PlayIntro()
	end)

	SyncDelay = 5
	if ORAMod == "ca" then
		SyncDelay = 2
	end
	Trigger.AfterDelay(DateTime.Seconds(SyncDelay), function()

		if ORAMod == "ca" then
		OverrideProductionSpeeds()
		end

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
		Incomepercentage = tonumber(IncomeshareLobbyoption)
		if Incomepercentage ~= 0 and #CoopPlayers >= 2 then
			ResourceBuffer = {}

			Utils.Do(CoopPlayers, function(PID)
				ResourceBuffer[PID] = 0
			end)

			SharedBank = 0

			IncomeSharing()
		end

		SetExtraMines()
		originalLocation = CPos.New(3, 3)
		--[[Utils.Do(CoopPlayers,function(PID)
			Utils.Do(PID.GetActorsByTypes(SharedBuildingList),function(BID)
				originalLocation = BID.Location
				--Media.DisplayMessage("Remote Spawn Location changed.")
			end)
		end)]]
		UpdateCoopPrequisites()
		StartCoopBots()
		EnemyVeterancy(mainEnemies)
	end)
end

-- Hooking into the InitAttackSquad function of CA
OverrideAttackTarget = function()
	local originalInitAttackSquad = InitAttackSquad
	InitAttackSquad = function(squad, player, targetPlayer)
		if targetPlayer == nil or IsMissionPlayer(targetPlayer) then
			if targetPlayer ~= nil then
				--Media.DisplayMessage("Attack Squad Called for:" .. targetPlayer.Name)
			else
				--Media.DisplayMessage("Attack Squad Called for undefined Player.")
			end
			local newTarget = Utils.Random(CoopPlayers)
			originalInitAttackSquad(squad, player, newTarget)
			--Media.DisplayMessage("Attacked Player changed to:" .. newTarget.Name)
		else
			originalInitAttackSquad(squad, player, targetPlayer)
			--Media.DisplayMessage("Attacked Player is no Coop Player and stays " .. targetPlayer.Name)
		end
	end

	local originalAssaultPlayerBaseOrHunt = AssaultPlayerBaseOrHunt
	AssaultPlayerBaseOrHunt = function(actor, targetPlayer, waypoints, fromIdle)
		if targetPlayer == nil or IsMissionPlayer(targetPlayer) then
			if targetPlayer ~= nil then
				--Media.DisplayMessage("Assault Called for:" .. targetPlayer.Name)
			else
				--Media.DisplayMessage("Assault Called for undefined Player.")
			end
			local newTarget = Utils.Random(CoopPlayers)
			originalAssaultPlayerBaseOrHunt(actor, newTarget, waypoints, fromIdle)
			--Media.DisplayMessage("Assaulted Player changed to:" .. newTarget.Name)
		else
			originalAssaultPlayerBaseOrHunt(actor, targetPlayer, waypoints, fromIdle)
			--Media.DisplayMessage("Assaulted Player is no Coop Player and stays " .. targetPlayer.Name)
		end
	end
end

TibAltShuffleMissionPlayer = function()
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		MissionPlayer = RandomCoopPlayer()
		TibAltShuffleMissionPlayer()
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