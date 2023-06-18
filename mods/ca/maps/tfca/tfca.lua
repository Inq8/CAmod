UnitsPerPlayer = Map.LobbyOption("unitsperplayer")

WorldLoaded = function()
    Blue = Player.GetPlayer("Blue")
    Red = Player.GetPlayer("Red")
    BlueScore = 0
    RedScore = 0

    Players = Player.GetPlayers(function(p) return p.Team == 1 or p.Team == 2 and not p.IsNonCombatant end)
    BluePlayers = Player.GetPlayers(function(p) return p.Team == 1 and not p.IsNonCombatant end)
    RedPlayers = Player.GetPlayers(function(p) return p.Team == 2 and not p.IsNonCombatant end)
    Objectives = { Dome, Comms, Ref, Power, Tech }

    Utils.Do(Objectives, function(o)
        Trigger.OnCapture(o, function(self, captor, oldOwner, newOwner)
            if newOwner.Team == 1 then
                self.Owner = Blue
            elseif newOwner.Team == 2 then
                self.Owner = Red
            end
        end)
    end)

    Utils.Do(Players, function(p)
        p.Cash = tonumber(UnitsPerPlayer)
        local spawnId = p.Spawn
        local spawnPoint = Map.NamedActor("Spawn" .. spawnId)

        if spawnPoint ~= nil then
            local spawner = Actor.Create("spawn", true, { Owner = p, Location = spawnPoint.Location })
            Trigger.OnProduction(spawner, function(producer, produced)
                Trigger.OnKilled(produced, function(self, killer)
                    Trigger.AfterDelay(DateTime.Seconds(10), function()
                        self.Owner.Cash = self.Owner.Cash + 1
                    end)
                end)
            end)
        end
    end)

    if #BluePlayers > #RedPlayers and #RedPlayers > 0 then
        local redExtra = (#BluePlayers - #RedPlayers) * UnitsPerPlayer
        local redPlayerIdx = 1

        while(redExtra > 0)
        do
            RedPlayers[redPlayerIdx].Cash = RedPlayers[redPlayerIdx].Cash + 1

            if #RedPlayers > redPlayerIdx then
                redPlayerIdx = redPlayerIdx + 1
            else
                redPlayerIdx = 1
            end

            redExtra = redExtra - 1
        end

    elseif #RedPlayers > #BluePlayers and #BluePlayers > 0 then
        local blueExtra = (#RedPlayers - #BluePlayers) * UnitsPerPlayer
        local bluePlayerIdx = 1

        while(blueExtra > 0)
        do
            BluePlayers[bluePlayerIdx].Cash = BluePlayers[bluePlayerIdx].Cash + 1

            if #BluePlayers > bluePlayerIdx then
                bluePlayerIdx = bluePlayerIdx + 1
            else
                bluePlayerIdx = 1
            end

            blueExtra = blueExtra - 1
        end
    end
end

Tick = function()
    if DateTime.GameTime > 1 and DateTime.GameTime % 50 == 0 then
        local blueObjectives = Blue.GetActorsByTypes({ "atek", "dome", "hq", "apwr", "proc" })
        local redObjectives = Red.GetActorsByTypes({ "atek", "dome", "hq", "apwr", "proc" })
        local scoreAmounts = { 2, 3, 4, 7, 60 }

        if #blueObjectives > 0 then
            BlueScore = BlueScore + scoreAmounts[#blueObjectives]
        end

        if #redObjectives > 0 then
            RedScore = RedScore + scoreAmounts[#redObjectives]
        end

        if BlueScore >= 1500 then
            BlueScore = 1500
            BlueWins()
        elseif RedScore >= 1500 then
            RedScore = 1500
            RedWins()
        end

        UpdateScoresText()
    end
end

UpdateScoresText = function()
    local color = HSLColor.White

    if BlueScore > RedScore then
        color = HSLColor.FromHex("0080FF")
    elseif RedScore > BlueScore then
        color = HSLColor.Red
    end

    UserInterface.SetMissionText("Blue = " .. BlueScore .. " / 1500 -- vs -- Red = " .. RedScore .. " / 1500", color)
end

BlueWins = function()
    Utils.Do(RedPlayers, function(p)
        local spawns = p.GetActorsByType("spawn")
        Utils.Do(spawns, function(s)
            s.Destroy()
        end)
    end)
end

RedWins = function()
    Utils.Do(BluePlayers, function(p)
        local spawns = p.GetActorsByType("spawn")
        Utils.Do(spawns, function(s)
            s.Destroy()
        end)
    end)
end
