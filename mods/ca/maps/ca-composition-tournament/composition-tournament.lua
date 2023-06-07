
NumCycles = Map.LobbyOption("numcycles")
Rounds = { }
SpectatorCams = { }
CurrentRound = 1
CurrentRoundTime = 0
BaseBuilders = { }
Players = { }
PlayerScores = { }
PlayerSurrenderTimes = { }

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
    EmptyPlayer = Player.GetPlayer("Empty")

    Utils.Do(PossiblePlayers, function(p)
        if p ~= nil then
            table.insert(Players, p)
            table.insert(PlayerScores, { Player = p, Captures = 0, Losses = 0 })
            PlayerSurrenderTimes[p.InternalName] = nil
            StartingCash = p.Cash
        end
    end)

    if #Players % 2 ~= 0 then
        table.insert(Players, EmptyPlayer)
    end

    BaseRadius = WDist.New(12288)

    Trigger.AfterDelay(1, function()
        Arenas = {
            {
                HQ = HQ1,
                Base1Pos = Base1.CenterPosition,
                Base2Pos = Base2.CenterPosition,
                Player1Buildings = { },
                Player2Buildings = { },
            },
            {
                HQ = HQ2,
                Base1Pos = Base3.CenterPosition,
                Base2Pos = Base4.CenterPosition,
                Player1Buildings = { },
                Player2Buildings = { },
            },
            {
                HQ = HQ3,
                Base1Pos = Base5.CenterPosition,
                Base2Pos = Base6.CenterPosition,
                Player1Buildings = { },
                Player2Buildings = { },
            },
            {
                HQ = HQ4,
                Base1Pos = Base7.CenterPosition,
                Base2Pos = Base8.CenterPosition,
                Player1Buildings = { },
                Player2Buildings = { },
            },
            {
                HQ = HQ5,
                Base1Pos = Base9.CenterPosition,
                Base2Pos = Base10.CenterPosition,
                Player1Buildings = { },
                Player2Buildings = { },
            },
            {
                HQ = HQ6,
                Base1Pos = Base11.CenterPosition,
                Base2Pos = Base12.CenterPosition,
                Player1Buildings = { },
                Player2Buildings = { },
            },
            {
                HQ = HQ7,
                Base1Pos = Base13.CenterPosition,
                Base2Pos = Base14.CenterPosition,
                Player1Buildings = { },
                Player2Buildings = { },
            },
            {
                HQ = HQ8,
                Base1Pos = Base15.CenterPosition,
                Base2Pos = Base16.CenterPosition,
                Player1Buildings = { },
                Player2Buildings = { },
            }
        }

        -- populate the initial building types and locations so they can be rebuilt each round, then destroy initial buildings
        Utils.Do(Arenas, function(arena)
            local player1Buildings = GetBaseBuildings(arena.Base1Pos)
            local player2Buildings = GetBaseBuildings(arena.Base2Pos)

            Utils.Do(player1Buildings, function(b)
                table.insert(arena.Player1Buildings, { Type = b.Type, Location = b.Location })
                Trigger.AfterDelay(1, function()
                    b.Destroy()
                end)
            end)
            Utils.Do(player2Buildings, function(b)
                table.insert(arena.Player2Buildings, { Type = b.Type, Location = b.Location })
                Trigger.AfterDelay(1, function()
                    b.Destroy()
                end)
            end)
        end)

        CalculateMatchups()
        InitRound()
    end)
end

GetBaseBuildings = function(basePos)
    return Map.ActorsInCircle(basePos, BaseRadius, IsBaseBuilding)
end

Tick = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
        if CurrentRound > #Rounds then
            return
        end

        CurrentRoundTime = CurrentRoundTime + 25

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
            elseif matchup.Winner == nil and matchup.Player1.InternalName ~= "Empty" and matchup.Player2.InternalName ~= "Empty" then
                activeMatchups = activeMatchups + 1

                -- every 5 seconds check if players have active units
                if CurrentRoundTime > DateTime.Seconds(60) and DateTime.GameTime % 125 == 0 then
                    Utils.Do({ { matchup.Player1, matchup.Player2 }, { matchup.Player2, matchup.Player1 } }, function(p)
                        local hqFlipped = false
                        local units = Utils.Where(p[1].GetActors(), IsActiveUnit)
                        if #units == 0 then
                            if PlayerSurrenderTimes[p[1].InternalName] == nil then
                                PlayerSurrenderTimes[p[1].InternalName] = DateTime.GameTime + DateTime.Seconds(15)
                                if p[1].IsLocalPlayer then
                                    Media.DisplayMessage("If you have no units in 15 seconds you will lose this round.", "Notification", HSLColor.FromHex("FF0000"))
                                elseif p[2].IsLocalPlayer then
                                    Media.DisplayMessage("If your opponent has no units in 15 seconds you will win this round.", "Notification", HSLColor.FromHex("00FF00"))
                                end
                            elseif DateTime.GameTime > PlayerSurrenderTimes[p[1].InternalName] and not hqFlipped then
                                hqFlipped = true
                                Arenas[matchup.ArenaIdx].HQ.Owner = p[2]
                                PlayerSurrenderTimes[p[1].InternalName] = nil
                            end
                        else
                            PlayerSurrenderTimes[p[1].InternalName] = nil
                        end
                    end)
                end
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

InitRound = function()
    Media.DisplayMessage("Round " .. CurrentRound .. " starting...", "Notification", HSLColor.FromHex("1E90FF"))
    UserInterface.SetMissionText("Round " .. CurrentRound .. " of " .. #Rounds, HSLColor.Yellow)
    ResetAll()
    CurrentRoundTime = 0

    if #Rounds == 0 then
        return
    end

    Trigger.AfterDelay(DateTime.Seconds(2), function()
        local roundMatchups = Rounds[CurrentRound]
        Media.DisplayMessage("Round started. Objectives capturable in 60 seconds.", "Notification", HSLColor.FromHex("1E90FF"))

        local objectives = Neutral.GetActorsByType("miss")
        Utils.Do(objectives, function(o)
            o.GrantCondition("locked", 1500)
        end)

        Trigger.AfterDelay(1500, function()
            Media.DisplayMessage("Objectives are now capturable.", "Notification", HSLColor.FromHex("1E90FF"))
        end)

        for i=1, #roundMatchups do
            local matchup = roundMatchups[i]

            if matchup.Player1.InternalName == "Empty" then
                if matchup.Player2.IsLocalPlayer then
                    Media.DisplayMessage("You have no opponent this round, please wait for the next one.", "Notification", HSLColor.FromHex("1E90FF"))
                end
                table.insert(SpectatorCams, Actor.Create("spectatorcam", true, { Owner = matchup.Player2 }))
            elseif matchup.Player2.InternalName == "Empty" then
                if matchup.Player1.IsLocalPlayer then
                    Media.DisplayMessage("You have no opponent this round, please wait for the next one.", "Notification", HSLColor.FromHex("1E90FF"))
                end
                table.insert(SpectatorCams, Actor.Create("spectatorcam", true, { Owner = matchup.Player1 }))
            else
                Utils.Do(Arenas[matchup.ArenaIdx].Player1Buildings, function(b)
                    Actor.Create(b.Type, true, { Owner = matchup.Player1, Location = b.Location })
                end)
                Utils.Do(Arenas[matchup.ArenaIdx].Player2Buildings, function(b)
                    Actor.Create(b.Type, true, { Owner = matchup.Player2, Location = b.Location })
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
    local rankingText = "\n\n"

    local playerRank = 1

    Utils.Do(PlayerScores, function(s)
        if playerRank == 1 then
            Winner = s.Player
        end
        rankingText = rankingText .. tostring(playerRank) .. ". " .. s.Player.Name .. " (" .. s.Captures .. " victories)\n"
        playerRank = playerRank + 1
    end)

    Utils.Do(Players, function(p)
        if p ~= Winner and p.InternalName ~= "Empty" then
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
        if p.InternalName ~= "Empty" then
            p.Cash = 0
            p.Resources = StartingCash
            local playerBuildings = p.GetActorsByTypes({ "miss", "weap", "tent", "afld", "fix" })
            Utils.Do(playerBuildings, function(b)
                if b.Type == "miss" then
                    b.Owner = Neutral
                else
                    b.Kill()
                end
            end)
            local actors = p.GetActors()
            Utils.Do(actors, function(a)
                KillUnit(a)
            end)
            Utils.Do(SpectatorCams, function(c)
                if not c.IsDead then
                    c.Destroy()
                end
            end)
            Utils.Do(BaseBuilders, function(b)
                if not b.IsDead then
                    b.Destroy()
                end
            end)
            SpectatorCams = { }
            BaseBuilders = { }
        end
    end)
end

KillUnit = function(a)
    if IsUpgrade(a) then
        Trigger.AfterDelay(5, function()
            a.Destroy()
        end)
    elseif IsActiveUnit(a) or IsDefense(a) then
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

IsActiveUnit = function(a)
    return not IsBaseBuilding(a) and a.Type ~= "player" and a.Type ~= "playerutils" and a.HasProperty("Kill") and not a.IsDead
end

IsDefense = function(a)
    return a.HasProperty("StartBuildingRepairs") and a.HasProperty("Attack")
end
