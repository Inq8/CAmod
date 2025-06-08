## Player ranks
label-player-level = Current rank: { $level }
label-player-level-current-xp = Current XP: { $currentXp }
label-player-level-required-xp = Next rank XP: { $nextLevelXp }

label-player-influence-level = Influence level: { $level }
label-player-influence-level-time = Next level in { $time }
label-player-influence-coalition = Coalition: { $coalition }
label-player-influence-policy = Policy: { $policy }

## ObserverStatsLogic
options-observer-stats =
    .none = Information: None
    .basic = Basic
    .economy = Economy
    .production = Production
    .support-powers = Support Powers
    .combat = Combat
    .army = Army
    .upgrades = Upgrades
    .build-order = Build Order
    .units-produced = Units Produced
    .earnings-graph = Earnings (graph)
    .army-graph = Army Value (graph)
    .team-army-graph = Team Value (graph)

## chrome/gamesave-loading.yaml
label-gamesave-loading-screen-loadtime-line1 = Sorry for the long load times, this is due to how the OpenRA engine handles saved games.
label-gamesave-loading-screen-loadtime-line2 = It replays the game from the beginning as fast as possible (so a longer game = longer time to load).

## chrome/ingame-player.yaml
button-command-bar-attack-move =
    .tooltip = Attack Move
    .tooltipdesc =
    Selected units will move to the desired location
    and attack any enemies they encounter en route.

    Hold <(Ctrl)> while targeting to order an Assault Move
    that attacks any units or structures encountered en route.

    Left-click icon then right-click on target location.

button-command-bar-force-move =
    .tooltip = Force Move
    .tooltipdesc =
    Selected units will move to the desired location
     - Default activity for the target is suppressed
     - Vehicles will attempt to crush enemies at the target location
     - Helicopters will land at the target location
     - Chrono Tanks will teleport towards the target location

    Left-click icon then right-click on target.
    Hold <(Alt)> to activate temporarily while commanding units.

button-command-bar-force-attack =
    .tooltip = Force Attack
    .tooltipdesc =
    Selected units will attack the targeted unit or location
     - Default activity for the target is suppressed
     - Allows targeting of own or ally forces
     - Long-range artillery units will always target the
       location, ignoring units and buildings

    Left-click icon then right-click on target.
    Hold <(Ctrl)> to activate temporarily while commanding units.

button-command-bar-guard =
    .tooltip = Guard
    .tooltipdesc =
    Selected units will follow the targeted unit.

    Left-click icon then right-click on target unit.

button-command-bar-deploy =
    .tooltip = Deploy
    .tooltipdesc =
    Selected units will perform their default deploy activity
     - MCVs will unpack into a Construction Yard
     - Construction Yards will re-pack into a MCV
     - Transports will unload their passengers
     - Demolition Trucks and MAD Tanks will self-destruct
     - Minelayers will deploy a mine
     - Aircraft will return to base

    Acts immediately on selected units.

button-command-bar-scatter =
    .tooltip = Scatter
    .tooltipdesc =
    Selected units will stop their current activity
    and move to a nearby location.

    Acts immediately on selected units.

button-command-bar-stop =
    .tooltip = Stop
    .tooltipdesc =
    Selected units will stop their current activity.
    Selected buildings will reset their rally point.

    Acts immediately on selected targets.

button-command-bar-queue-orders =
    .tooltip = Waypoint Mode
    .tooltipdesc =
    Use Waypoint Mode to give multiple linking commands
    to the selected units. Units will execute the commands
    immediately upon receiving them.

    Left-click icon then give commands in the game world.
    Hold <(Shift)> to activate temporarily while commanding units.

button-stance-bar-attackanything =
    .tooltip = Attack Anything Stance
    .tooltipdesc =
    Set the selected units to Attack Anything stance:
     - Units will attack enemy units and structures on sight
     - Units will pursue attackers across the battlefield

button-stance-bar-defend =
    .tooltip = Defend Stance
    .tooltipdesc =
    Set the selected units to Defend stance:
     - Units will attack enemy units on sight
     - Units will not move or pursue enemies

button-stance-bar-returnfire =
    .tooltip = Return Fire Stance
    .tooltipdesc =
    Set the selected units to Return Fire stance:
     - Units will retaliate against enemies that attack them
     - Units will not move or pursue enemies

button-stance-bar-holdfire =
    .tooltip = Hold Fire Stance
    .tooltipdesc =
    Set the selected units to Hold Fire stance:
     - Units will not fire upon enemies
     - Units will not move or pursue enemies

button-top-buttons-beacon-tooltip = Place Beacon
button-top-buttons-sell-tooltip = Sell
button-top-buttons-power-tooltip = Power Down
button-top-buttons-repair-tooltip = Repair

supportpowers-support-powers-palette =
    .ready = READY
    .hold = ON HOLD