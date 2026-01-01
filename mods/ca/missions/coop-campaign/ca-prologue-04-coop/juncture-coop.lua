
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { GDI }
	SinglePlayerPlayer = Nod
	CoopInit()
end

AfterWorldLoaded = function()
	TransferMcvsToPlayers()
	StartCashSpread(3500)
end

AfterTick = function()

end

WarpInBanshees = function()
	local firstActivePlayer = GetFirstActivePlayer()
	if firstActivePlayer == nil then
		return
	end

	local hpad1 = Actor.Create("hpad.td", true, { Owner = firstActivePlayer, Location = HpadSpawn1.Location })
	local hpad2 = Actor.Create("hpad.td", true, { Owner = firstActivePlayer, Location = HpadSpawn2.Location  })

	Trigger.AfterDelay(10, function()
		local useFirstHpad = true
		for _, player in ipairs(MissionPlayers) do
			local hpad = useFirstHpad and hpad1 or hpad2
			local banshee = Actor.Create("scrn", true, { Owner = player, Location = hpad.Location, CenterPosition = hpad.CenterPosition, Facing = Angle.NorthEast })
			banshee.Move(hpad.Location)
			useFirstHpad = not useFirstHpad
		end
	end)
end
