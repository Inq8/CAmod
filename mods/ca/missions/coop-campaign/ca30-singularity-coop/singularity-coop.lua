
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	GDI = Player.GetPlayer("GDI")
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	Nod = Player.GetPlayer("Nod")
	Scrin = Player.GetPlayer("Scrin")
	AlliedSlaves = Player.GetPlayer("AlliedSlaves")
	SovietSlaves = Player.GetPlayer("SovietSlaves")
	NodSlaves = Player.GetPlayer("NodSlaves")
	CyborgSlaves = Player.GetPlayer("CyborgSlaves")
	Kane = Player.GetPlayer("Kane")
	NeutralGDI = Player.GetPlayer("NeutralGDI")
	NeutralScrin = Player.GetPlayer("NeutralScrin")
	SignalTransmitterPlayer = Player.GetPlayer("SignalTransmitterPlayer") -- separate player to prevent AI from attacking it
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Scrin, SovietSlaves, AlliedSlaves, NodSlaves }
	SinglePlayerPlayer = GDI
	CoopInit()
end

AfterWorldLoaded = function()
	SplitOwnerBlacklist[#SplitOwnerBlacklist] = "yf23.interceptor"

	local firstActivePlayer = GetFirstActivePlayer()
	TransferBaseToPlayer(SinglePlayerPlayer, firstActivePlayer)
	StartCashSpread(3500)

	local x = 66
	Utils.Do(GetMcvPlayers(), function(p)
		if p ~= firstActivePlayer then
			Reinforcements.Reinforce(p, { "amcv" }, { CPos.New(x, 132), CPos.New(x, 130)})
			x = x + 2
		end
	end)
end

AfterTick = function()

end

FlipSlaveFaction = function(player, killer)
	if player == NodSlaves then
		NodFreed = true
		Squads.NodSlaves.IdleUnits = { }
		attackPath = { EastAttackNode1.Location, WormholeWP.Location }
		InitAttackSquad(Squads.ScrinEast, Scrin)
		if ScrinDefenseBuff1.IsDead and ScrinDefenseBuff2.IsDead then
			InitHackers(HackersDelay[Difficulty])
		end
		Notification("Nod forces have been released from Scrin control.")
		MediaCA.PlaySound(MissionDir .. "/c_nodreleased.aud", 2)
	elseif player == SovietSlaves then
		SovietsFreed = true
		Squads.SovietSlaves.IdleUnits = { }
		attackPath = { WestAttackNode1.Location, WormholeWP.Location }
		InitAttackSquad(Squads.ScrinWest, Scrin)
		InitMADTankAttack()
		Notification("Soviet forces have been released from Scrin control.")
		MediaCA.PlaySound(MissionDir .. "/c_sovietsreleased.aud", 2)
	elseif player == AlliedSlaves then
		AlliesFreed = true
		Squads.AlliedSlaves.IdleUnits = { }
		attackPath = { CenterAttackNode1.Location, WormholeWP.Location }
		InitAttackSquad(Squads.ScrinCenter, Scrin)
		InitChronoTanks()
		Notification("Allied forces have been released from Scrin control.")
		MediaCA.PlaySound(MissionDir .. "/c_alliesreleased.aud", 2)
	end

	local actors = Utils.Where(player.GetActors(), function(a) return not a.IsDead and a.IsInWorld and a.Type ~= "player" end)
	Utils.Do(actors, function(a)
		a.Owner = killer.Owner
		Trigger.ClearAll(a)
	end)
end
