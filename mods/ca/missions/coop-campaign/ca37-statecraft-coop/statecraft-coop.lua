
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	USSR = Player.GetPlayer("USSR")
	Marinesko = Player.GetPlayer("Marinesko")
	Romanov = Player.GetPlayer("Romanov")
	Krukov = Player.GetPlayer("Krukov")
	MarineskoUnited = Player.GetPlayer("MarineskoUnited")
	RomanovUnited = Player.GetPlayer("RomanovUnited")
	KrukovUnited = Player.GetPlayer("KrukovUnited")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Marinesko, Romanov, Krukov, MarineskoUnited, RomanovUnited, KrukovUnited }
	SinglePlayerPlayer = USSR
	CoopInit()
end

AfterWorldLoaded = function()
	TransferMcvsToPlayers()
	StartCashSpread(3500)
end

AfterTick = function()

end
