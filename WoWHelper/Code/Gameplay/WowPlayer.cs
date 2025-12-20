using InputManager;
using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsGameAutomationTools.Slack;
using WoWHelper.Code;
using WoWHelper.Code.Constants;
using WoWHelper.Code.Gameplay;
using WoWHelper.Code.WorldState;
using static WoWHelper.Code.Gameplay.WowFarmingConfiguration;
using static WoWHelper.Code.WowPlayerStates;

namespace WoWHelper
{
    public partial class WowPlayer
    {
        // TODO: write custom getters/setters for these so we can keep checking the time until they're off cooldown,
        // then use the cached value until they get dirtied again?
        public long FarmStartTime { get; private set; }
        public long LastFindTargetTime { get; private set; }
        public long DynamiteTime { get; private set; }
        public long HealthPotionTime { get; private set; }
        public long NextUpdateTime { get; private set; }

        public WowWorldState PreviousWorldState { get; private set; }
        public WowWorldState WorldState { get; private set; }
        public PlayerState CurrentPlayerState { get; private set; }
        public PathfindingState CurrentPathfindingState { get; private set; }

        public int CurrentWaypointIndex { get; private set; }
        public int WaypointTraversalDirection { get; private set; }

        public bool LogoutTriggered { get; private set; }
        public string LogoutReason { get; private set; }
        public Bitmap LogoutBitmap { get; private set; }

        public WowFarmingConfiguration FarmingConfig { get; private set; }

        public WowPlayer()
        {
            PreviousWorldState = new WowWorldState();
            WorldState = new WowWorldState();

            CurrentPlayerState = PlayerState.WAITING_TO_FOCUS_ON_WINDOW;
            CurrentPathfindingState = PathfindingState.PICKING_NEXT_WAYPOINT;
            CurrentWaypointIndex = -1;
            WaypointTraversalDirection = 1;

            FarmingConfig = WowFarmingConfigConstants.LEVEL_42_TANARIS_TURTLES;
        }

        public async Task UpdateWorldStateAsync()
        {
            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var timeToWait = NextUpdateTime - now;
            int timeToWaitClamped = (int)Math.Max(0, timeToWait);

            await Task.Delay(timeToWaitClamped);

            PreviousWorldState = WorldState;
            WorldState = WowWorldState.GetWoWWorldState();

            NextUpdateTime = now + WowPlayerConstants.TIME_BETWEEN_WORLDSTATE_UPDATES;
        }

        public void UpdateWorldState()
        {
            PreviousWorldState = WorldState;
            WorldState = WowWorldState.GetWoWWorldState();
        }

        // For Testing only, otherwise use UpdateWorldState
        public void UpdateFromBitmap(Bitmap bmp)
        {
            WorldState.UpdateFromBitmap(bmp);
        }

        async Task<TState> ChangeStateBasedOnTaskResult<TState>(Task<bool> task, TState successState, TState failureState) where TState : Enum
        {
            bool taskResult = await task;
            return taskResult ? successState : failureState;
        }

        PlayerState ChangeStateBasedOnBool(bool boolToCheck, PlayerState successState, PlayerState failureState)
        {
            return boolToCheck ? successState : failureState;
        }

        public static bool CurrentTimeInsideDuration(long startTime, long duration)
        {
            return (DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTime) < duration;
        }

        public bool CanEngageTarget()
        {
            if (FarmingConfig.EngageMethod == EngagementMethod.Charge)
            {
                return WorldState.CanChargeTarget;
            }
            else if (FarmingConfig.EngageMethod == EngagementMethod.Shoot)
            {
                return WorldState.CanShootTarget;
            }

            return false;
        }

        public void KickOffCoreLoop()
        {
            KeyPoller.EscPressed += async () => {
                Console.WriteLine("ESC detected! Performing cleanup then quitting");

                // Make sure we don't have any lingering keys pressed down
                Keyboard.KeyUp(WowInput.MOVE_FORWARD);
                Keyboard.KeyUp(WowInput.MOVE_BACK);
                Keyboard.KeyUp(WowInput.TURN_LEFT);
                Keyboard.KeyUp(WowInput.JUMP);
                Keyboard.KeyUp(WowInput.STRAFE_LEFT);
                Keyboard.KeyUp(WowInput.STRAFE_RIGHT);
                Keyboard.KeyUp(WowInput.LatestShiftKey);
                Keyboard.KeyUp(Keys.LShiftKey);

                Environment.Exit(0);
            };
            KeyPoller.Start();


            _ = CoreGameplayLoopTask();
        }

        public async Task<bool> CoreGameplayLoopTask()
        {
            FarmStartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            Console.WriteLine("Kicking off core gameplay loop");
            PlayerState currentPlayerState = PlayerState.WAITING_TO_FOCUS_ON_WINDOW;

            while (currentPlayerState != PlayerState.EXITING_CORE_GAMEPLAY_LOOP)
            {
                await UpdateWorldStateAsync();

                // TODO: short circuit into combat/getting out of water/etc.

                switch (currentPlayerState)
                {
                    case PlayerState.WAITING_TO_FOCUS_ON_WINDOW:
                        Console.WriteLine("Focusing on window");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(FocusOnWindowTask(),
                            PlayerState.CHECK_FOR_LOGOUT,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.CHECK_FOR_LOGOUT:
                        Console.WriteLine("Checking if we should log out");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(SetLogoutVariablesTask(),
                            PlayerState.START_LOGGING_OUT,
                            PlayerState.START_BATTLE_READY_RECOVERY);
                        break;
                    case PlayerState.START_LOGGING_OUT:
                        Console.WriteLine("Started logging out");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(StartLogoutTask(),
                            PlayerState.WAITING_TO_LOG_OUT,
                            PlayerState.IN_CORE_COMBAT_LOOP);
                        break;
                    case PlayerState.WAITING_TO_LOG_OUT:
                        Console.WriteLine("Waiting to log out");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(CheckIfLoggedOutTask(),
                            PlayerState.LOGGED_OUT,
                            PlayerState.IN_CORE_COMBAT_LOOP);
                        break;
                    case PlayerState.LOGGED_OUT:
                        Console.WriteLine("Logged out");
                        SlackHelper.SendMessageToChannel($"Logged out: {LogoutReason}");
                        currentPlayerState = PlayerState.EXITING_CORE_GAMEPLAY_LOOP;
                        break;
                    case PlayerState.START_BATTLE_READY_RECOVERY:
                        Console.WriteLine("Waiting until battle ready");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(StartBattleReadyRecoverTask(),
                            PlayerState.WAIT_UNTIL_BATTLE_READY,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.WAIT_UNTIL_BATTLE_READY:
                        Console.WriteLine("Waiting until battle ready");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(WaitUntilBattleReadyTask(),
                            PlayerState.CHECK_FOR_VALID_TARGET,
                            PlayerState.WAIT_UNTIL_BATTLE_READY);
                        break;
                    case PlayerState.CHECK_FOR_VALID_TARGET:
                        Console.WriteLine("Checking for valid target");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(CorePathfindingLoopTask(), // TODO
                            PlayerState.TRY_TO_CHARGE_TARGET,
                            PlayerState.IN_CORE_COMBAT_LOOP);
                        break;
                    case PlayerState.TRY_TO_CHARGE_TARGET:
                        Console.WriteLine("Trying to charge target");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(TryToEngageTask(), // TODO
                            PlayerState.IN_CORE_COMBAT_LOOP,
                            PlayerState.CHECK_FOR_LOGOUT);
                        break;
                    case PlayerState.IN_CORE_COMBAT_LOOP:
                        Console.WriteLine("In core combat loop");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(CoreCombatLoopTask(), // TODO
                            PlayerState.TARGET_DEFEATED,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.TARGET_DEFEATED:
                        Console.WriteLine("Target defeated, trying to loot");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(LootTask(),
                            PlayerState.LOOT_ATTEMPT_TWO,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.LOOT_ATTEMPT_TWO:
                        Console.WriteLine("Trying to loot a second time, in case the dying anim is slow");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(LootTask(),
                            PlayerState.SKIN_ATTEMPT,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.SKIN_ATTEMPT:
                        Console.WriteLine("Trying to skin");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(SkinTask(),
                            PlayerState.CHECK_FOR_LOGOUT,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                }
            }

            Console.WriteLine("Exited Core Gameplay");
            //await EQTask.CampTask();
            return true;
        }

        public async Task<bool> CoreCombatLoopTask()
        {
            Console.WriteLine("Kicking off core combat loop");
            bool thrownDynamite = false;
            bool potionUsed = false;
            bool tooManyAttackersActionsTaken = false;

            await StartOfCombatTask();
            
            do
            {
                await Task.Delay(250);

                UpdateWorldState();

                // don't drown
                if (WorldState.Underwater)
                {
                    await GetOutOfWater();
                }

                // First do our "Make sure we're not standing around doing nothing" checks
                if (await MakeSureWeAreAttackingEnemyTask())
                {
                    continue;
                }

                // Next, check if we need to pop any big cooldowns
                if (!tooManyAttackersActionsTaken && await TooManyAttackersTask())
                {
                    tooManyAttackersActionsTaken = true;
                    continue;
                }

                // Just in case, if for some reason things are going really poorly, try to pop retal regardless
                if (!tooManyAttackersActionsTaken && WorldState.PlayerHpPercent <= WowPlayerConstants.OH_SHIT_RETAL_HP_THRESHOLD)
                {
                    SlackHelper.SendMessageToChannel($"{WowPlayerConstants.OH_SHIT_RETAL_HP_THRESHOLD}% Retal popped, not sure what went wrong!");
                    // cast retaliation
                    Keyboard.KeyPress(WowInput.RETALIATION_KEY);
                    // until I get GCD tracking working, just wait a bit and click it again to make sure
                    await Task.Delay(1500);
                    Keyboard.KeyPress(WowInput.RETALIATION_KEY);
                    tooManyAttackersActionsTaken = true;

                    LogoutReason = $"Got down to {WowPlayerConstants.OH_SHIT_RETAL_HP_THRESHOLD}% somehow";
                    LogoutTriggered = true;

                    continue;
                }

                if (!thrownDynamite && await ThrowDynamiteTask())
                {
                    DynamiteTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    thrownDynamite = true;
                    continue;
                }

                if (!potionUsed && await UseHealingPotionTask())
                {
                    HealthPotionTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    potionUsed = true;
                    continue;
                }

                // Finally, if we've made it this far, do standard combat actions
                if (!WorldState.BattleShoutActive && WorldState.ResourcePercent >= WowGameplayConstants.BATTLE_SHOUT_RAGE_COST)
                {
                    Keyboard.KeyPress(WowInput.BATTLE_SHOUT_KEY);
                }
                else if (WorldState.OverpowerUsable && WorldState.ResourcePercent >= WowGameplayConstants.OVERPOWER_RAGE_COST)
                {
                    Keyboard.KeyPress(WowInput.OVERPOWER_KEY);
                }
                else if (!WorldState.TargetHasRend && WorldState.ResourcePercent >= WowGameplayConstants.REND_RAGE_COST)
                {
                    Keyboard.KeyPress(WowInput.REND_KEY);
                }
                else if(WorldState.AttackerCount > 1)
                {
                    if (WorldState.SweepingStrikesCooledDown && WorldState.ResourcePercent < WowGameplayConstants.SWEEPING_STRIKES_RAGE_COST)
                    {
                        // Popping SS ASAP is priority
                        continue;
                    }

                    if (WorldState.SweepingStrikesCooledDown && WorldState.ResourcePercent >= WowGameplayConstants.SWEEPING_STRIKES_RAGE_COST)
                    {
                        await WowInput.PressKeyWithShift(WowInput.SHIFT_SWEEPING_STRIKES_MACRO);
                    }
                    else if (WorldState.WhirlwindCooledDown && WorldState.ResourcePercent >= WowGameplayConstants.WHIRLWIND_RAGE_COST)
                    {
                        await WowInput.PressKeyWithShift(WowInput.SHIFT_WHIRLWIND_MACRO);
                        await Task.Delay(150);
                        Keyboard.KeyPress(WowInput.HEROIC_STRIKE_KEY);
                        await Task.Delay(150);
                        Keyboard.KeyPress(WowInput.HEROIC_STRIKE_KEY);
                        await Task.Delay(150);
                        Keyboard.KeyPress(WowInput.HEROIC_STRIKE_KEY);
                        await Task.Delay(150);
                        Keyboard.KeyPress(WowInput.HEROIC_STRIKE_KEY);
                    }
                    else if (!WorldState.WhirlwindCooledDown && !WorldState.HeroicStrikeQueued && WorldState.ResourcePercent >= WowGameplayConstants.CLEAVE_RAGE_COST)
                    {
                        // if WW is cooled down, prefer waiting for rage for that over cleaving
                        await WowInput.PressKeyWithShift(WowInput.SHIFT_CLEAVE_MACRO);
                    }
                }
                else if (WorldState.AttackerCount == 1)
                {
                    if (WorldState.ResourcePercent >= WowGameplayConstants.MORTAL_STRIKE_RAGE_COST)
                    {
                        Keyboard.KeyPress(WowInput.HEROIC_STRIKE_KEY);
                    }
                }
            } while (WorldState.IsInCombat);

            return true;
        }

        public async Task<bool> CorePathfindingLoopTask()
        {
            Console.WriteLine("Kicking off core pathfinding loop");

            bool stationaryJumpAttemptedOnce = false;
            bool stationaryWiggleAttemptedOnce = false;
            bool stationaryWiggleAttemptedTwice = false;
            bool stationaryAlertSent = false;
            long lastLocationChangeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            await FocusOnWindowTask();

            // Count loops of the waypoints, if we haven't found a target in N loops, error out
            int maxTargetChecks = 1000;
            int targetChecks = 0;
            //bool lookingForDangerousTarget = false;
            while (targetChecks < maxTargetChecks)
            {
                if (!CurrentTimeInsideDuration(LastFindTargetTime, WowPlayerConstants.TIME_BETWEEN_FIND_TARGET_MILLIS))
                {
                    LastFindTargetTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    if (FarmingConfig.WaypointDefinition.TargetFindMethod == WowWaypointDefinition.WaypointTargetFindMethod.TAB)
                    {
                        Keyboard.KeyPress(WowInput.TAB_TARGET);
                    }
                    else if (FarmingConfig.WaypointDefinition.TargetFindMethod == WowWaypointDefinition.WaypointTargetFindMethod.MACRO)
                    {
                        Keyboard.KeyPress(WowInput.FIND_TARGET_MACRO);
                    }
                    else if (FarmingConfig.WaypointDefinition.TargetFindMethod == WowWaypointDefinition.WaypointTargetFindMethod.ALTERNATE)
                    {
                        if (targetChecks % 2 == 0)
                        {
                            Keyboard.KeyPress(WowInput.TAB_TARGET);
                        }
                        else
                        {
                            Keyboard.KeyPress(WowInput.FIND_TARGET_MACRO);
                        }  
                    }

                    targetChecks++;
                }

                // always wait a bit for the UI to update, then grab it?
                await Task.Delay(250);
                UpdateWorldState();

                // don't drown
                if (WorldState.Underwater)
                {
                    await GetOutOfWater();
                }

                if (WorldState.IsInCombat)
                {
                    await EndWalkForwardTask();
                    // return true if we can charge, false if we're already in combat
                    return false;
                }

                if (CanEngageTarget())
                {
                    await EndWalkForwardTask();
                    return true;
                }

                // If we haven't moved in a long time, alert
                if (PreviousWorldState?.MapX != WorldState.MapX || PreviousWorldState?.MapY != WorldState.MapY)
                {
                    lastLocationChangeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                }

                

                if (!stationaryJumpAttemptedOnce && !CurrentTimeInsideDuration(lastLocationChangeTime, WowPathfinding.STATIONARY_MILLIS_BEFORE_JUMP))
                {
                    await AvoidObstacleByJumping();
                    stationaryJumpAttemptedOnce = true;
                }

                if (!stationaryWiggleAttemptedOnce && !CurrentTimeInsideDuration(lastLocationChangeTime, WowPathfinding.STATIONARY_MILLIS_BEFORE_WIGGLE))
                {
                    // first wiggle try left
                    await AvoidObstacle(left: true);
                    stationaryWiggleAttemptedOnce = true;
                }

                if (!stationaryWiggleAttemptedTwice && !CurrentTimeInsideDuration(lastLocationChangeTime, WowPathfinding.STATIONARY_MILLIS_BEFORE_SECOND_WIGGLE))
                {
                    // second wiggle try right
                    await AvoidObstacle(left: true);
                    stationaryWiggleAttemptedTwice = true;
                }

                if (!stationaryAlertSent && !CurrentTimeInsideDuration(lastLocationChangeTime, WowPathfinding.STATIONARY_MILLIS_BEFORE_ALERT))
                {
                    //SlackHelper.SendMessageToChannel($"Haven't moved in a long time.  Something wrong?");
                    //stationaryAlertSent = true;
                    LogoutTriggered = true;
                    LogoutReason = "Stuck for a long time, couldn't wiggle out";
                    return true;
                }

                if (CanEngageTarget() || WorldState.IsInCombat)
                {
                    await EndWalkForwardTask();
                    // return true if we can charge/shoot, false if we're already in combat
                    return !WorldState.IsInCombat;
                }

                switch (CurrentPathfindingState)
                {
                    case PathfindingState.PICKING_NEXT_WAYPOINT:
                        Console.WriteLine($"Picking next waypoint");
                        if (CurrentWaypointIndex == -1)
                        {
                            // we've never picked a waypoint yet, so find the closest one
                            Vector2 playerLocation = new Vector2(WorldState.MapX, WorldState.MapY);
                            CurrentWaypointIndex = FarmingConfig.WaypointDefinition.Waypoints
                                .Select((p, i) => (dist: Vector2.Distance(playerLocation, p), index: i))
                                .OrderBy(t => t.dist)
                                .First()
                                .index;

                            // Circular always goes in the same direction, so if you interrupt and restart, you'll still be going the same direction.
                            // For linear let's do our best guess to pick the best direction
                            if (FarmingConfig.WaypointDefinition.TraversalMethod == WowWaypointDefinition.WaypointTraversalMethod.LINEAR)
                            {
                                if (CurrentWaypointIndex == 0)
                                {
                                    WaypointTraversalDirection = 1;
                                }
                                else if (CurrentWaypointIndex == FarmingConfig.WaypointDefinition.Waypoints.Count - 1)
                                {
                                    WaypointTraversalDirection = -1;
                                }
                                else
                                {
                                    var forwardDegrees = WowPathfinding.GetDesiredDirectionInDegrees(FarmingConfig.WaypointDefinition.Waypoints[CurrentWaypointIndex], FarmingConfig.WaypointDefinition.Waypoints[CurrentWaypointIndex + 1]);
                                    var backwardsDegrees = WowPathfinding.GetDesiredDirectionInDegrees(FarmingConfig.WaypointDefinition.Waypoints[CurrentWaypointIndex], FarmingConfig.WaypointDefinition.Waypoints[CurrentWaypointIndex - 1]);
                                    var facingDegrees = WorldState.FacingDegrees;
                                    var forwardDiff = WowPathfinding.GetDegreesToMove(facingDegrees, forwardDegrees);
                                    var backwardsDiff = WowPathfinding.GetDegreesToMove(facingDegrees, backwardsDegrees);

                                    if (backwardsDiff < forwardDiff)
                                    {
                                        WaypointTraversalDirection = -1;
                                    }
                                    
                                    Console.WriteLine("Forward/Backwards/Facing/AbsFor/AbsBack");
                                    Console.WriteLine(forwardDegrees);
                                    Console.WriteLine(backwardsDegrees);
                                    Console.WriteLine(facingDegrees);
                                    Console.WriteLine(forwardDiff);
                                    Console.WriteLine(backwardsDiff);
                                    
                                }
                            }
                        }
                        else
                        {
                            // otherwise cycle through them
                            CurrentWaypointIndex += WaypointTraversalDirection;

                            if (CurrentWaypointIndex < 0 || CurrentWaypointIndex >= FarmingConfig.WaypointDefinition.Waypoints.Count)
                            {
                                if (FarmingConfig.WaypointDefinition.TraversalMethod == WowWaypointDefinition.WaypointTraversalMethod.CIRCULAR)
                                {
                                    CurrentWaypointIndex = 0;
                                }
                                else if (FarmingConfig.WaypointDefinition.TraversalMethod == WowWaypointDefinition.WaypointTraversalMethod.LINEAR)
                                {
                                    // since we detect this when we've gone out of bounds, switch direction.
                                    // first addition puts us back in bounds, but we know we're already there, so do a second addition
                                    WaypointTraversalDirection *= -1;
                                    CurrentWaypointIndex += WaypointTraversalDirection;
                                    CurrentWaypointIndex += WaypointTraversalDirection;
                                }
                            }
                        }
                            
                        CurrentPathfindingState = PathfindingState.MOVING_TOWARDS_WAYPOINT;
                        break;
                    case PathfindingState.MOVING_TOWARDS_WAYPOINT:
                        CurrentPathfindingState = await ChangeStateBasedOnTaskResult(MoveTowardsWaypointTask(),
                            PathfindingState.PICKING_NEXT_WAYPOINT,
                            PathfindingState.MOVING_TOWARDS_WAYPOINT);
                        break;
                }
            }

            SlackHelper.SendMessageToChannel($"Haven't found a target in ~4 minutes.  Something wrong?");
            Console.WriteLine("Exited Pathfinding loop.  Too many loops without a successful target find.");
            LogoutTriggered = true;
            LogoutReason = "4 minutes without finding a target";

            return true;
        }
    }
}
