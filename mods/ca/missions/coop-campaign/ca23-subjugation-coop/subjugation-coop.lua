
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Scrin = Player.GetPlayer("Scrin")
	USSR = Player.GetPlayer("USSR")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { USSR }
	SinglePlayerPlayer = Scrin
	CoopInit()
end

AfterWorldLoaded = function()

end

AfterTick = function()

end

-- overrides the base campaign version
SetupRefAndSilosCaptureCredits = function(player)
	local silosAndRefineries = player.GetActorsByTypes(CashRewardOnCaptureTypes)
	Utils.Do(silosAndRefineries, function(a)
		Trigger.OnCapture(a, function(self, captor, oldOwner, newOwner)
			if IsMissionPlayer(newOwner) then
				Utils.Do(MissionPlayers, function(PID)
					PID.Cash = PID.Cash + (CapturedCreditsAmount / #MissionPlayers)
				end)
			else
				newOwner.Cash = newOwner.Cash + CapturedCreditsAmount
			end
			Media.FloatingText("+$" .. CapturedCreditsAmount, self.CenterPosition, 30, newOwner.Color)
		end)
	end)
end
