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

        // TODO: Switch to a system where we use cancellation tokens to exit normal operation and go into "oh shit I got aggroed by something?"
        //private CancellationTokenSource CancellationTokenSource {  get; set; }


        public WoWWorldState WorldState { get; private set; }
        public PlayerState CurrentPlayerState { get; private set; }
        public PathfindingState CurrentPathfindingState { get; private set; }

        public int CurrentWaypointIndex { get; private set; }
        public WoWWaypointDefinition WaypointDefinition { get; private set; }
        public int WaypointTraversalDirection { get; private set; }

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
            WaypointDefinition = WoWWaypoints.LEVEL_21_ZORAMGAR_WAYPOINTS;
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
            //lastFarmingLimitTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

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
                        currentPlayerState = PlayerState.WAIT_UNTIL_BATTLE_READY;
                        break;
                    case PlayerState.WAIT_UNTIL_BATTLE_READY:
                        Console.WriteLine("Waiting until battle ready");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(WoWTasks.RecoverAfterFightTask(),
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
                        await Task.Delay(1000);

                        // skin
                        Mouse.PressButton(Mouse.MouseKeys.Right);
                        await Task.Delay(2500);

                        currentPlayerState = PlayerState.WAIT_UNTIL_BATTLE_READY;
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
                    thrownDynamite = true;
                    continue;
                }

                if (!potionUsed && await WoWTasks.UseHealingPotionTask(worldState))
                {
                    potionUsed = true;
                    continue;
                }

                // Finally, if we've made it this far, do standard combat actions

                if (!worldState.BattleShoutActive && worldState.ResourcePercent >= WoWGameplayConstants.BATTLE_SHOUT_RAGE_COST)
                {
                    Keyboard.KeyPress(WoWInput.BATTLE_SHOUT_KEY);
                }

                if (!worldState.HeroicStrikeQueued && worldState.ResourcePercent >= WoWGameplayConstants.HEROIC_STRIKE_RAGE_COST)
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


                    lastFindTargetTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    targetChecks++;
                }

                // always wait a bit for the UI to update, then grab it?
                await Task.Delay(250);
                previousWorldState = worldState;
                worldState = WoWWorldState.GetWoWWorldState();

                // If we haven't moved in a long time, alert
                if (previousWorldState?.MapX != worldState.MapX || previousWorldState?.MapY != worldState.MapY)
                {
                    lastLocationChangeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                }

                if (!CurrentTimeInsideDuration(lastLocationChangeTime, WoWPathfinding.STATIONARY_MILLIS_BEFORE_ALERT) && !stationaryAlertSent)
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
