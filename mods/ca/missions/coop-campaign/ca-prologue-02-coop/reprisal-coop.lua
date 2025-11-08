
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	Civilians = Player.GetPlayer("Civilians")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { Greece }
	SinglePlayerPlayer = USSR
	CoopInit()
end

AfterWorldLoaded = function()

end

AfterTick = function()

end

DoMcvArrival = function()
	PlaySpeechNotificationToMissionPlayers("ReinforcementsArrived")
	Notification("Reinforcements have arrived.")
	local delay = 0
	Utils.Do(GetMcvPlayers(), function(p)
		Trigger.AfterDelay(DateTime.Seconds(delay), function()
			Reinforcements.Reinforce(p, { "mcv" }, { McvSpawn.Location, McvRally.Location })
		end)
		delay = delay + DateTime.Seconds(1)
	end)
end
