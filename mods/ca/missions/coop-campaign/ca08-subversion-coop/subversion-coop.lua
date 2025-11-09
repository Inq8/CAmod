
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Nod = Player.GetPlayer("Nod")
	Greece = Player.GetPlayer("Greece")
	GDI = Player.GetPlayer("GDI")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { GDI, Greece }
	SinglePlayerPlayer = Nod
	CoopInit()
end

AfterWorldLoaded = function()

end

AfterTick = function()

end

InitIonControl = function()
	IonCannonDamage = {}  -- [player] = totalDamage
	Utils.Do(CoopPlayers, function(p)
		table.insert(IonCannonDamage, {
			Player = p,
			Score = 0
		})
		if p ~= IonControl.Owner then
			Actor.Create(IonControl.Type, true, { Owner = p, Location = IonControl.Location })
		end
	end)
	DamageScoring()
end

DamageScoring = function()
	local GreeceTargets = Greece.GetActors()
	Utils.Do(GreeceTargets,function(UID)
		if UID.Type ~= "player" and UID.Type ~= "waypoint" and UID.Type ~= "brik" then
			Trigger.OnDamaged(UID, function(self, attacker, damage)
				if IsMissionPlayer(attacker.Owner) then
					local p = attacker.Owner
					for _, entry in ipairs(IonCannonDamage) do
						if entry.Player == p then
							entry.Score = entry.Score + damage
							break
						end
					end
					ShowIonScoreboard()
				end
			end)
		end
	end)
end

function ShowIonScoreboard()
	table.sort(IonCannonDamage, function(a, b)
		return a.Score > b.Score
	end)

	local sb = "---Damage Leaderboard---\n"
	local rank = 1
	for _, entry in ipairs(IonCannonDamage) do
		sb = sb .. string.format("%d) %-12s  %8d\n", rank, entry.Player.Name, entry.Score)
		rank = rank + 1
	end

	local msg = "\n\n\n\n\n\n\n\nDestroy bridges then use the Ion Cannon to destroy the Allied base.\n" .. sb
	UserInterface.SetMissionText(msg, HSLColor.Yellow)
end
