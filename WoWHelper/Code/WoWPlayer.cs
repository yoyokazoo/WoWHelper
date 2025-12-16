using InputManager;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsGameAutomationTools.Slack;
using WoWHelper.Code;
using WoWHelper.Code.WorldState;

namespace WoWHelper
{
    public class WoWPlayer
    {
        private static long TIME_BETWEEN_FIND_TARGET_MILLIS = (long)(2 * 1000); // 2 seconds
        private long lastFindTargetTime = 0;

        public static long FARM_TIME_LIMIT_MILLIS = 8 * 60 * 60 * 1000; // 4 hours

        public long FarmStartTime { get; private set; }
        public long LastDynamiteTime { get; private set; }
        public long LastHealthPotionTime { get; private set; }

        // TODO: Switch to a system where we use cancellation tokens to exit normal operation and go into "oh shit I got aggroed by something?"
        //private CancellationTokenSource CancellationTokenSource {  get; set; }


        public WoWWorldState WorldState { get; private set; }
        public PlayerState CurrentPlayerState { get; private set; }
        public PathfindingState CurrentPathfindingState { get; private set; }

        public int CurrentWaypointIndex { get; private set; }
        public WoWWaypointDefinition WaypointDefinition { get; private set; }
        public int WaypointTraversalDirection { get; private set; }

        public bool LogoutTriggered { get; set; }
        public string LogoutReason { get; set; }

        public enum PlayerState
        {
            WAITING_TO_FOCUS,
            FOCUSED_ON_WINDOW,
            WAIT_UNTIL_BATTLE_READY,
            CHECK_FOR_VALID_TARGET,
            TRY_TO_CHARGE_TARGET,
            IN_CORE_COMBAT_LOOP,
            TARGET_DEFEATED,

            WALK_WAYPOINTS,

            ESC_KEY_SEEN,
            CHECK_FOR_LOGOUT,
            LOGGING_OUT,
            LOGGED_OUT,
            EXITING_CORE_GAMEPLAY_LOOP,
        }

        public enum PathfindingState
        {
            PICKING_NEXT_WAYPOINT,
            MOVING_TOWARDS_WAYPOINT,
            ARRIVED_AT_WAYPOINT
        }

        public WoWPlayer()
        {
            WorldState = new WoWWorldState();
            CurrentPlayerState = PlayerState.WAITING_TO_FOCUS;
            CurrentPathfindingState = PathfindingState.PICKING_NEXT_WAYPOINT;
            CurrentWaypointIndex = -1;
            WaypointDefinition = WoWWaypoints.LEVEL_29_HILLSBRAD_RIVER_WAYPOINTS;
            WaypointTraversalDirection = 1;
        }

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

        public void KickOffCoreLoop()
        {
            KeyPoller.EscPressed += async () => {
                Console.WriteLine("ESC detected!");
                // set cancellation flag or perform cleanup
                Keyboard.KeyPress(WoWInput.MOVE_BACK);
                
                Environment.Exit(0);
            };

            KeyPoller.Start();


            _ = CoreGameplayLoopTask();
            //_ = CorePathfindingLoopTask();
        }

        async Task<bool> CoreGameplayLoopTask()
        {
            FarmStartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            Console.WriteLine("Kicking off core gameplay loop");
            PlayerState currentPlayerState = PlayerState.WAITING_TO_FOCUS;

            while (currentPlayerState != PlayerState.EXITING_CORE_GAMEPLAY_LOOP)
            {
                switch (currentPlayerState)
                {
                    case PlayerState.WAITING_TO_FOCUS:
                        Console.WriteLine("Focusing on window");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(WoWTasks.FocusOnWindowTask(),
                            PlayerState.FOCUSED_ON_WINDOW,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.FOCUSED_ON_WINDOW:
                        Console.WriteLine("Focused on window");
                        currentPlayerState = PlayerState.CHECK_FOR_LOGOUT;
                        break;
                    case PlayerState.CHECK_FOR_LOGOUT:
                        Console.WriteLine("Checking if we should log out");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(WoWTasks.SetLogoutVariablesTask(this),
                            PlayerState.LOGGING_OUT,
                            PlayerState.WAIT_UNTIL_BATTLE_READY);
                        break;
                    case PlayerState.LOGGING_OUT:
                        Console.WriteLine("Logging out");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(WoWTasks.LogoutTask(),
                            PlayerState.LOGGED_OUT,
                            PlayerState.IN_CORE_COMBAT_LOOP);
                        break;
                    case PlayerState.LOGGED_OUT:
                        Console.WriteLine("Logged out");
                        SlackHelper.SendMessageToChannel($"Logged out: {LogoutReason}");
                        currentPlayerState = PlayerState.EXITING_CORE_GAMEPLAY_LOOP;
                        break;
                    case PlayerState.WAIT_UNTIL_BATTLE_READY:
                        Console.WriteLine("Waiting until battle ready");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(WoWTasks.RecoverAfterFightTask(this),
                            PlayerState.CHECK_FOR_VALID_TARGET,
                            PlayerState.IN_CORE_COMBAT_LOOP);
                        break;
                    case PlayerState.CHECK_FOR_VALID_TARGET:
                        Console.WriteLine("Checking for valid target");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(CorePathfindingLoopTask(),
                            PlayerState.TRY_TO_CHARGE_TARGET,
                            PlayerState.IN_CORE_COMBAT_LOOP);
                        break;
                    case PlayerState.TRY_TO_CHARGE_TARGET:
                        Console.WriteLine("Trying to charge target");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(WoWTasks.TryToChargeTask(),
                            PlayerState.IN_CORE_COMBAT_LOOP,
                            PlayerState.CHECK_FOR_VALID_TARGET);
                        break;
                    case PlayerState.IN_CORE_COMBAT_LOOP:
                        Console.WriteLine("In core combat loop");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(CoreCombatLoopTask(),
                            PlayerState.TARGET_DEFEATED,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.TARGET_DEFEATED:
                        Console.WriteLine("Target defeated, trying to loot");

                        // loot
                        Mouse.Move(1720, 720);
                        Mouse.PressButton(Mouse.MouseKeys.Right);
                        await Task.Delay(1200);

                        // skin
                        Mouse.PressButton(Mouse.MouseKeys.Right);
                        await Task.Delay(3000);

                        currentPlayerState = PlayerState.CHECK_FOR_LOGOUT;
                        break;
                }
            }

            Console.WriteLine("Exited Core Gameplay");
            //await EQTask.CampTask();
            return true;
        }

        async Task<bool> CoreCombatLoopTask()
        {
            Console.WriteLine("Kicking off core combat loop");
            WoWWorldState previousWorldState = null;
            WoWWorldState worldState = null;
            bool thrownDynamite = false;
            bool potionUsed = false;
            bool tooManyAttackersActionsTaken = false;

            await WoWTasks.StartOfCombatTask();
            
            do
            {
                await Task.Delay(250);

                previousWorldState = worldState;
                worldState = WoWWorldState.GetWoWWorldState();

                // don't drown
                if (worldState.Underwater)
                {
                    await WoWTasks.GetOutOfWater();
                }

                // First do our "Make sure we're not standing around doing nothing" checks
                if (await WoWTasks.MakeSureWeAreAttackingEnemyTask(worldState, previousWorldState))
                {
                    continue;
                }

                // Next, check if we need to pop any big cooldowns
                if (!tooManyAttackersActionsTaken && await WoWTasks.TooManyAttackersTask(worldState))
                {
                    tooManyAttackersActionsTaken = true;
                    continue;
                }

                if (!thrownDynamite && await WoWTasks.ThrowDynamiteTask(worldState))
                {
                    LastDynamiteTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    thrownDynamite = true;
                    continue;
                }

                if (!potionUsed && await WoWTasks.UseHealingPotionTask(worldState))
                {
                    LastHealthPotionTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    potionUsed = true;
                    continue;
                }

                // Finally, if we've made it this far, do standard combat actions
                if (!worldState.BattleShoutActive && worldState.ResourcePercent >= WoWGameplayConstants.BATTLE_SHOUT_RAGE_COST)
                {
                    Keyboard.KeyPress(WoWInput.BATTLE_SHOUT_KEY);
                }
                else if (worldState.OverpowerUsable && worldState.ResourcePercent >= WoWGameplayConstants.OVERPOWER_RAGE_COST)
                {
                    Keyboard.KeyPress(WoWInput.OVERPOWER_KEY);
                }
                else if (!worldState.TargetHasRend && worldState.ResourcePercent >= WoWGameplayConstants.REND_RAGE_COST)
                {
                    Keyboard.KeyPress(WoWInput.REND_KEY);
                }
                else if (!worldState.HeroicStrikeQueued && worldState.ResourcePercent >= WoWGameplayConstants.HEROIC_STRIKE_RAGE_COST)
                {
                    Keyboard.KeyPress(WoWInput.HEROIC_STRIKE_KEY);
                }
            } while (worldState.IsInCombat);

            return true;
        }

        async Task<bool> CorePathfindingLoopTask()
        {
            Console.WriteLine("Kicking off core pathfinding loop");

            WoWWorldState previousWorldState = null;
            WoWWorldState worldState = null;
            bool stationaryWiggleAttempted = false;
            bool stationaryAlertSent = false;
            long lastLocationChangeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            await WoWTasks.FocusOnWindowTask();

            // Count loops of the waypoints, if we haven't found a target in N loops, error out
            int maxTargetChecks = 1000;
            int targetChecks = 0;
            while (targetChecks < maxTargetChecks)
            {
                if (!CurrentTimeInsideDuration(lastFindTargetTime, TIME_BETWEEN_FIND_TARGET_MILLIS))
                {
                    if (WaypointDefinition.TargetFindMethod == WoWWaypointDefinition.WaypointTargetFindMethod.TAB)
                    {
                        Keyboard.KeyPress(WoWInput.TAB_TARGET);
                    }
                    else if (WaypointDefinition.TargetFindMethod == WoWWaypointDefinition.WaypointTargetFindMethod.MACRO)
                    {
                        Keyboard.KeyPress(WoWInput.FIND_TARGET_MACRO);
                    }
                    else if (WaypointDefinition.TargetFindMethod == WoWWaypointDefinition.WaypointTargetFindMethod.ALTERNATE)
                    {
                        if (targetChecks % 2 == 0)
                        {
                            Keyboard.KeyPress(WoWInput.TAB_TARGET);
                        }
                        else
                        {
                            Keyboard.KeyPress(WoWInput.FIND_TARGET_MACRO);
                        }  
                    }


                    lastFindTargetTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    targetChecks++;
                }

                // always wait a bit for the UI to update, then grab it?
                await Task.Delay(250);
                previousWorldState = worldState;
                worldState = WoWWorldState.GetWoWWorldState();

                // don't drown
                if (worldState.Underwater)
                {
                    await WoWTasks.GetOutOfWater();
                }

                // If we haven't moved in a long time, alert
                if (previousWorldState?.MapX != worldState.MapX || previousWorldState?.MapY != worldState.MapY)
                {
                    lastLocationChangeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                }

                if (!stationaryWiggleAttempted && !CurrentTimeInsideDuration(lastLocationChangeTime, WoWPathfinding.STATIONARY_MILLIS_BEFORE_WIGGLE))
                {
                    // stop walking forward
                    await WoWTasks.EndWalkForwardTask();

                    // back off obstruction
                    Keyboard.KeyDown(Keys.S);
                    await Task.Delay(1000);
                    Keyboard.KeyUp(Keys.S);

                    // strafe left
                    Keyboard.KeyDown(Keys.Q);
                    await Task.Delay(2000);
                    Keyboard.KeyUp(Keys.Q);

                    stationaryWiggleAttempted = true;
                }

                if (!stationaryAlertSent && !CurrentTimeInsideDuration(lastLocationChangeTime, WoWPathfinding.STATIONARY_MILLIS_BEFORE_ALERT))
                {
                    SlackHelper.SendMessageToChannel($"Haven't moved in a long time.  Something wrong?");
                    stationaryAlertSent = true;
                }

                if (worldState.CanChargeTarget || worldState.IsInCombat)
                {
                    await WoWTasks.EndWalkForwardTask();
                    // return true if we can charge, false if we're already in combat
                    return !worldState.IsInCombat;
                }

                switch (CurrentPathfindingState)
                {
                    case PathfindingState.PICKING_NEXT_WAYPOINT:
                        Console.WriteLine($"Picking next waypoint");
                        if (CurrentWaypointIndex == -1)
                        {
                            // we've never picked a waypoint yet, so find the closest one
                            Vector2 playerLocation = new Vector2(worldState.MapX, worldState.MapY);
                            CurrentWaypointIndex = WaypointDefinition.Waypoints
                                .Select((p, i) => (dist: Vector2.Distance(playerLocation, p), index: i))
                                .OrderBy(t => t.dist)
                                .First()
                                .index;

                            // Circular always goes in the same direction, so if you interrupt and restart, you'll still be going the same direction.
                            // For linear let's do our best guess to pick the best direction
                            if (WaypointDefinition.TraversalMethod == WoWWaypointDefinition.WaypointTraversalMethod.LINEAR)
                            {
                                if (CurrentWaypointIndex == 0)
                                {
                                    WaypointTraversalDirection = 1;
                                }
                                else if (CurrentWaypointIndex == WaypointDefinition.Waypoints.Count - 1)
                                {
                                    WaypointTraversalDirection = -1;
                                }
                                else
                                {
                                    var forwardDegrees = WoWPathfinding.GetDesiredDirectionInDegrees(WaypointDefinition.Waypoints[CurrentWaypointIndex], WaypointDefinition.Waypoints[CurrentWaypointIndex + 1]);
                                    var backwardsDegrees = WoWPathfinding.GetDesiredDirectionInDegrees(WaypointDefinition.Waypoints[CurrentWaypointIndex], WaypointDefinition.Waypoints[CurrentWaypointIndex - 1]);
                                    var facingDegrees = worldState.FacingDegrees;
                                    var forwardDiff = WoWPathfinding.GetDegreesToMove(facingDegrees, forwardDegrees);
                                    var backwardsDiff = WoWPathfinding.GetDegreesToMove(facingDegrees, backwardsDegrees);

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

                            if (CurrentWaypointIndex < 0 || CurrentWaypointIndex >= WaypointDefinition.Waypoints.Count)
                            {
                                if (WaypointDefinition.TraversalMethod == WoWWaypointDefinition.WaypointTraversalMethod.CIRCULAR)
                                {
                                    CurrentWaypointIndex = 0;
                                }
                                else if (WaypointDefinition.TraversalMethod == WoWWaypointDefinition.WaypointTraversalMethod.LINEAR)
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
                        CurrentPathfindingState = await ChangeStateBasedOnTaskResult(WoWTasks.MoveTowardsWaypointTask(worldState, WaypointDefinition, CurrentWaypointIndex),
                            PathfindingState.PICKING_NEXT_WAYPOINT,
                            PathfindingState.MOVING_TOWARDS_WAYPOINT);
                        break;
                }
            }

            SlackHelper.SendMessageToChannel($"Haven't found a target in ~4 minutes.  Something wrong?");
            Console.WriteLine("Exited Pathfinding loop.  Too many loops without a successful target find.");
            return false;
        }
    }
}
