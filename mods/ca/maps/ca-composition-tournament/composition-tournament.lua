
NumCycles = Map.LobbyOption("numcycles")
Rounds = { }
SpectatorCams = { }
CurrentRound = 1
BaseBuilders = { }

WorldLoaded = function()
    PossiblePlayers = {
        Multi0 = Player.GetPlayer("Multi0"),
        Multi1 = Player.GetPlayer("Multi1"),
        Multi2 = Player.GetPlayer("Multi2"),
        Multi3 = Player.GetPlayer("Multi3"),
        Multi4 = Player.GetPlayer("Multi4"),
        Multi5 = Player.GetPlayer("Multi5"),
        Multi6 = Player.GetPlayer("Multi6"),
        Multi7 = Player.GetPlayer("Multi7"),
        Multi8 = Player.GetPlayer("Multi8"),
        Multi9 = Player.GetPlayer("Multi9"),
        Multi10 = Player.GetPlayer("Multi10"),
        Multi11 = Player.GetPlayer("Multi11"),
        Multi12 = Player.GetPlayer("Multi12"),
        Multi13 = Player.GetPlayer("Multi13"),
        Multi14 = Player.GetPlayer("Multi14"),
        Multi15 = Player.GetPlayer("Multi15")
    }

    Neutral = Player.GetPlayer("Neutral")

    Players = { }
    PlayerScores = { }

    Utils.Do(PossiblePlayers, function(p)
        if p ~= nil then
            table.insert(Players, p)
            table.insert(PlayerScores, { Player = p, Captures = 0, Losses = 0 })
            StartingCash = p.Cash
        end
    end)

    if #Players % 2 ~= 0 then
        table.insert(Players, { InternalName = "Empty", IsBot = true, IsLocalPlayer = false })
    end

    HQs = Neutral.GetActorsByType("miss")

    BaseRadius = WDist.New(12288)

    Trigger.AfterDelay(1, function()
        Arenas = {
            {
                Player1Buildings = Map.ActorsInCircle(Base1.CenterPosition, BaseRadius, IsBaseBuilding),
                Player2Buildings = Map.ActorsInCircle(Base2.CenterPosition, BaseRadius, IsBaseBuilding),
                HQ = HQ1,
                Base1Pos = Base1.CenterPosition,
                Base2Pos = Base2.CenterPosition,
                TopLeft = Arena1TopLeft,
                BottomRight = Arena1BottomRight,
            },
            {
                Player1Buildings = Map.ActorsInCircle(Base3.CenterPosition, BaseRadius, IsBaseBuilding),
                Player2Buildings = Map.ActorsInCircle(Base4.CenterPosition, BaseRadius, IsBaseBuilding),
                HQ = HQ2,
                Base1Pos = Base3.CenterPosition,
                Base2Pos = Base4.CenterPosition,
                TopLeft = Arena2TopLeft,
                BottomRight = Arena2BottomRight,
            },
            {
                Player1Buildings = Map.ActorsInCircle(Base5.CenterPosition, BaseRadius, IsBaseBuilding),
                Player2Buildings = Map.ActorsInCircle(Base6.CenterPosition, BaseRadius, IsBaseBuilding),
                HQ = HQ3,
                Base1Pos = Base5.CenterPosition,
                Base2Pos = Base6.CenterPosition,
                TopLeft = Arena3TopLeft,
                BottomRight = Arena3BottomRight,
            },
            {
                Player1Buildings = Map.ActorsInCircle(Base7.CenterPosition, BaseRadius, IsBaseBuilding),
                Player2Buildings = Map.ActorsInCircle(Base8.CenterPosition, BaseRadius, IsBaseBuilding),
                HQ = HQ4,
                Base1Pos = Base7.CenterPosition,
                Base2Pos = Base8.CenterPosition,
                TopLeft = Arena4TopLeft,
                BottomRight = Arena4BottomRight,
            },
            {
                Player1Buildings = Map.ActorsInCircle(Base9.CenterPosition, BaseRadius, IsBaseBuilding),
                Player2Buildings = Map.ActorsInCircle(Base10.CenterPosition, BaseRadius, IsBaseBuilding),
                HQ = HQ5,
                Base1Pos = Base9.CenterPosition,
                Base2Pos = Base10.CenterPosition,
                TopLeft = Arena5TopLeft,
                BottomRight = Arena5BottomRight,
            },
            {
                Player1Buildings = Map.ActorsInCircle(Base11.CenterPosition, BaseRadius, IsBaseBuilding),
                Player2Buildings = Map.ActorsInCircle(Base12.CenterPosition, BaseRadius, IsBaseBuilding),
                HQ = HQ6,
                Base1Pos = Base11.CenterPosition,
                Base2Pos = Base12.CenterPosition,
                TopLeft = Arena6TopLeft,
                BottomRight = Arena6BottomRight,
            },
            {
                Player1Buildings = Map.ActorsInCircle(Base13.CenterPosition, BaseRadius, IsBaseBuilding),
                Player2Buildings = Map.ActorsInCircle(Base14.CenterPosition, BaseRadius, IsBaseBuilding),
                HQ = HQ7,
                Base1Pos = Base13.CenterPosition,
                Base2Pos = Base14.CenterPosition,
                TopLeft = Arena7TopLeft,
                BottomRight = Arena7BottomRight,
            },
            {
                Player1Buildings = Map.ActorsInCircle(Base15.CenterPosition, BaseRadius, IsBaseBuilding),
                Player2Buildings = Map.ActorsInCircle(Base16.CenterPosition, BaseRadius, IsBaseBuilding),
                HQ = HQ8,
                Base1Pos = Base15.CenterPosition,
                Base2Pos = Base16.CenterPosition,
                TopLeft = Arena8TopLeft,
                BottomRight = Arena8BottomRight,
            }
        }

        CalculateMatchups()
        InitRound()
    end)
end

Tick = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
        if CurrentRound > #Rounds then
            return
        end

        local activeMatchups = 0

        -- for each matchup in current round, check if the arena HQ is not neutral
        Utils.Do(Rounds[CurrentRound], function(matchup)
            if matchup.Winner == nil and Arenas[matchup.ArenaIdx].HQ.Owner ~= Neutral then
                matchup.Winner = Arenas[matchup.ArenaIdx].HQ.Owner

                Utils.Do(PlayerScores, function(ps)
                    if ps.Player == matchup.Winner then
                        ps.Captures = ps.Captures + 1
                    end
                end)

                if matchup.Winner == matchup.Player1 then
                    matchup.Loser = matchup.Player2
                else
                    matchup.Loser = matchup.Player1
                end

                if matchup.Winner.IsLocalPlayer then
                    Media.DisplayMessage("You are victorious! Please wait while remaining matchups are completed.", "Notification", HSLColor.FromHex("00FF00"))
                elseif matchup.Loser.IsLocalPlayer then
                    Media.DisplayMessage("You have been defeated. Please wait while remaining matchups are completed.", "Notification", HSLColor.FromHex("FF0000"))
                end

                EndMatchup(matchup)
            elseif not matchup.Player1.IsBot and not matchup.Player2.IsBot then
                activeMatchups = activeMatchups + 1
            end
        end)

        if activeMatchups == 0 then
            EndRound()
        end
    end
end

CalculateMatchups = function()
    local numPlayers = #Players
    local matchupsPerRound = numPlayers / 2
    local roundsPerCycle = numPlayers * (numPlayers - 1) / 2 / matchupsPerRound

    local opponentPool = { }
    Utils.Do(Players, function(p)
        table.insert(opponentPool, p)
    end)

    PossibleMatchups = { }

    -- for each player, assign all possible opponents (removing them from pool of possible opponents to prevent duplicates)
    Utils.Do(Players, function(p)
        Utils.Do(opponentPool, function(o)
            if o ~= p then
                local matchupPlayers = { p, o }
                matchupPlayers = Utils.Shuffle(matchupPlayers)
                table.insert(PossibleMatchups, { Player1 = matchupPlayers[1], Player2 = matchupPlayers[2], Winner = nil, Loser = nil, ArenaIdx = nil })
            end
        end)
        table.remove(opponentPool, 1)
    end)

    local round = 1

    for i=1, NumCycles do
        local matchupPool = { }
        Utils.Do(PossibleMatchups, function(m)
            table.insert(matchupPool, { Player1 = m.Player1, Player2 = m.Player2, Winner = nil, Loser = nil, ArenaIdx = nil })
        end)

        -- build each round of matchups
        for j=1, roundsPerCycle do
            Rounds[round] = { }
            local playersUsedInRound = { }

            -- while the number of matchups assigned to the round is less than required, add more
            while #Rounds[round] < matchupsPerRound do
                local matchupIdx = Utils.RandomInteger(1, #matchupPool + 1)
                local matchup = matchupPool[matchupIdx]
                matchup.ArenaIdx = #Rounds[round] + 1

                if not playersUsedInRound[matchup.Player1.InternalName] and not playersUsedInRound[matchup.Player2.InternalName] then
                    table.insert(Rounds[round], matchup)
                    playersUsedInRound[matchup.Player1.InternalName] = true
                    playersUsedInRound[matchup.Player2.InternalName] = true
                    table.remove(matchupPool, matchupIdx)
                    -- Media.Debug("Matchup " .. tostring(#Rounds[j]) .. ": " .. matchup.Player1.InternalName .. " vs " .. matchup.Player2.InternalName .. " on arena " .. matchup.ArenaIdx)
                end
            end

            round = round + 1
        end
    end
end

EndMatchup = function(matchup)
    local loserUnits = matchup.Loser.GetActors()
    Utils.Do(loserUnits, function(u)
        KillUnit(u)
    end)

    table.insert(SpectatorCams, Actor.Create("spectatorcam", true, { Owner = matchup.Winner }))
    table.insert(SpectatorCams, Actor.Create("spectatorcam", true, { Owner = matchup.Loser }))
end

EndRound = function()
    CurrentRound = CurrentRound + 1

    if CurrentRound > #Rounds then
        EndGame()
        return
    end

    InitRound()
end

EndGame = function()
    ResetAll()

    table.sort(PlayerScores, function(a, b) return a.Captures * 100000000 - a.Player.DeathsCost > b.Captures * 100000000 - b.Player.DeathsCost end)
    local rankingText = ""

    local playerRank = 1

    Utils.Do(PlayerScores, function(s)
        if playerRank == 1 then
            Winner = s.Player
        end
        rankingText = rankingText .. tostring(playerRank) .. ". " .. s.Player.Name .. " (" .. s.Captures .. " victories)\n"
        playerRank = playerRank + 1
    end)

    Utils.Do(Players, function(p)
        if p ~= Winner then
            local utils = p.GetActorsByType("playerutils")
            Utils.Do(utils, function(u)
                u.Destroy()
            end)
        end
    end)

    Media.DisplayMessage("Congratulations to the winner, " .. Winner.Name .. "!", "Notification", HSLColor.FromHex("00FF00"))
    UserInterface.SetMissionText(rankingText, HSLColor.Yellow)
end

ResetAll = function()
    Utils.Do(Players, function(p)
        if not p.IsBot then
            p.Cash = 0
            p.Resources = StartingCash
            local playerBuildings = p.GetActorsByTypes({ "miss", "weap", "tent", "afld", "fix" })
            Utils.Do(playerBuildings, function(c) c.Owner = Neutral end)
            local actors = p.GetActors()
            Utils.Do(actors, function(a)
                KillUnit(a)
            end)
            Utils.Do(SpectatorCams, function(c)
                c.Destroy()
            end)
            Utils.Do(BaseBuilders, function(b)
                b.Destroy()
            end)
        end
    end)
end

InitRound = function()
    Media.DisplayMessage("Round " .. CurrentRound .. " starting...", "Notification", HSLColor.FromHex("1E90FF"))
    UserInterface.SetMissionText("Round " .. CurrentRound .. " of " .. #Rounds, HSLColor.Yellow)
    ResetAll()

    if #Rounds == 0 then
        return
    end

    Trigger.AfterDelay(DateTime.Seconds(2), function()
        local roundMatchups = Rounds[CurrentRound]
        Media.DisplayMessage("Round started. Objectives capturable in 30 seconds.", "Notification", HSLColor.FromHex("1E90FF"))

        local objectives = Neutral.GetActorsByType("miss")
        Utils.Do(objectives, function(o)
            o.GrantCondition("locked", 750)
        end)

        Trigger.AfterDelay(750, function()
            Media.DisplayMessage("Objectives are now capturable.", "Notification", HSLColor.FromHex("1E90FF"))
        end)

        for i=1, #roundMatchups do
            local matchup = roundMatchups[i]

            if matchup.Player1.IsBot then
                table.insert(SpectatorCams, Actor.Create("spectatorcam", true, { Owner = matchup.Player2 }))
            elseif matchup.Player2.IsBot then
                table.insert(SpectatorCams, Actor.Create("spectatorcam", true, { Owner = matchup.Player1 }))
            else
                Utils.Do(Arenas[matchup.ArenaIdx].Player1Buildings, function(b)
                    b.Owner = matchup.Player1
                end)
                Utils.Do(Arenas[matchup.ArenaIdx].Player2Buildings, function(b)
                    b.Owner = matchup.Player2
                end)

                Beacon.New(matchup.Player1, Arenas[matchup.ArenaIdx].Base1Pos)
                Beacon.New(matchup.Player2, Arenas[matchup.ArenaIdx].Base2Pos)

                if matchup.Player1.IsLocalPlayer then
                    Camera.Position = Arenas[matchup.ArenaIdx].Base1Pos
                elseif matchup.Player2.IsLocalPlayer then
                    Camera.Position = Arenas[matchup.ArenaIdx].Base2Pos
                end

                table.insert(BaseBuilders, Actor.Create("basebuilder", true, { Owner = matchup.Player1, Location = Arenas[matchup.ArenaIdx].HQ.Location }))
                table.insert(BaseBuilders, Actor.Create("basebuilder", true, { Owner = matchup.Player2, Location = Arenas[matchup.ArenaIdx].HQ.Location }))

                Trigger.AfterDelay(1, function()
                    Actor.Create("QueueUpdaterDummy", true, { Owner = matchup.Player1 })
                    Actor.Create("QueueUpdaterDummy", true, { Owner = matchup.Player2 })
                end)
            end
        end
    end)
end

KillUnit = function(a)
    if IsUpgrade(a) then
        Trigger.AfterDelay(5, function()
            a.Destroy()
        end)
    elseif not IsBaseBuilding(a) and a.Type ~= "player" and a.HasProperty("Kill") and not a.IsDead then
        a.Stop()
        a.Destroy()
        Trigger.AfterDelay(5, function()
            KillUnit(a)
        end)
    end
end

IsUpgrade = function(a)
    return string.find(a.Type, ".upgrade") or string.find(a.Type, ".strat")
end

IsBaseBuilding = function(a)
    return a.HasProperty("StartBuildingRepairs") or a.Type == "miss"
end
