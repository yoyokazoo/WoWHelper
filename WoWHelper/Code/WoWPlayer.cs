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
        public List<Vector2> Waypoints = new List<Vector2> {
            /*
             * Boars level 1
            new Vector2(44.21f, 63.86f),
            new Vector2(44.41f, 59.83f),
            new Vector2(45.78f, 60.20f),
            new Vector2(45.27f, 62.75f)
            */
            /*
             * Imps Level 5
            new Vector2(43.66f, 56.64f),
            new Vector2(46.79f, 57.88f),
            new Vector2(44.52f, 59.48f)
            */
            // Boars level 7
            new Vector2(52.01f, 54.60f),
            new Vector2(52.61f, 57.03f),
            new Vector2(53.12f, 62.92f),
            new Vector2(51.78f, 66.50f),
            new Vector2(54.44f, 66.99f),
            new Vector2(54.48f, 62.45f),
            new Vector2(54.00f, 58.09f),
            new Vector2(53.34f, 53.85f)
        };

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
            KeyPoller.EscPressed += () => {
                Console.WriteLine("ESC detected!");
                // set cancellation flag or perform cleanup
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
                        currentPlayerState = PlayerState.CHECK_FOR_VALID_TARGET;
                        break;
                    case PlayerState.WAIT_UNTIL_BATTLE_READY:
                        Console.WriteLine("Waiting until battle ready");
                        currentPlayerState = PlayerState.CHECK_FOR_VALID_TARGET;
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
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
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

            // combat wiggle in case camera is pointed wrong direction
            Keyboard.KeyDown(Keys.S);
            await Task.Delay(400);
            Keyboard.KeyUp(Keys.S);

            Keyboard.KeyDown(Keys.W);
            await Task.Delay(20);
            Keyboard.KeyUp(Keys.W);

            // always kick things off with heroic strike macro to /startattack
            Keyboard.KeyPress(WoWInput.HEROIC_STRIKE_KEY);

            
            // true if mob killed, false if we need to do emergency stuff? or do emergency stuff in here?
            do
            {
                await Task.Delay(250);

                previousWorldState = worldState;
                worldState = WoWWorldState.GetWoWWorldState();

                if (previousWorldState?.AttackerCount > worldState.AttackerCount)
                {
                    // one of the mobs just died, scoot back to make sure the next mob is in front of you, and heroic strike to startattack
                    await WoWTasks.ScootBackwardsTask();
                    Keyboard.KeyPress(WoWInput.HEROIC_STRIKE_KEY);
                }

                if (worldState.PlayerHpPercent <= WoWGameplayConstants.HEALING_POTION_HP_THRESHOLD)
                {
                    Keyboard.KeyPress(WoWInput.HEALING_POTION_KEY);
                }

                if (!worldState.HeroicStrikeQueued && worldState.ResourcePercent >= WoWGameplayConstants.HEROIC_STRIKE_RAGE_COST)
                {
                    Keyboard.KeyPress(WoWInput.HEROIC_STRIKE_KEY);
                }

                // turning off temporarily to see how the scooting strat works
                /*
                if (worldState.FacingWrongWay)
                {
                    // let's try this first, rather than scooting back
                    await WoWTasks.TurnABitToTheLeftTask();
                }
                */

            } while (worldState.IsInCombat);

            return true;
        }

        async Task<bool> CorePathfindingLoopTask()
        {
            Console.WriteLine("Kicking off core pathfinding loop");

            await WoWTasks.FocusOnWindowTask();

            // Count loops of the waypoints, if we haven't found a target in N loops, error out
            int maxLoops = 1000;
            int loops = 0;
            while (loops < maxLoops)
            {
                if (!CurrentTimeInsideDuration(lastFindTargetTime, TIME_BETWEEN_FIND_TARGET_MILLIS))
                {
                    Keyboard.KeyPress(WoWInput.FIND_TARGET);
                    lastFindTargetTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                }

                // always wait a bit for the UI to update, then grab it?
                await Task.Delay(250);
                WoWWorldState worldState = WoWWorldState.GetWoWWorldState();

                if (worldState.CanChargeTarget || worldState.IsInCombat)
                {
                    await WoWTasks.EndWalkForwardTask();
                    // return true if we can charge, false if we're already in combat
                    return !worldState.IsInCombat;
                }

                loops++;
                switch (CurrentPathfindingState)
                {
                    case PathfindingState.PICKING_NEXT_WAYPOINT:
                        Console.WriteLine($"Picking next waypoint");
                        if (CurrentWaypointIndex == -1)
                        {
                            // we've never picked a waypoint yet, so find the closest one
                            Vector2 playerLocation = new Vector2(worldState.MapX, worldState.MapY);
                            CurrentWaypointIndex = Waypoints
                                .Select((p, i) => (dist: Vector2.Distance(playerLocation, p), index: i))
                                .OrderBy(t => t.dist)
                                .First()
                                .index;
                        }
                        else
                        {
                            // otherwise cycle through them
                            CurrentWaypointIndex += 1;
                            CurrentWaypointIndex %= Waypoints.Count;
                        }
                            
                        CurrentPathfindingState = PathfindingState.MOVING_TOWARDS_WAYPOINT;
                        break;
                    case PathfindingState.MOVING_TOWARDS_WAYPOINT:
                        //WoWWorldState worldState = WoWWorldState.GetWoWWorldState();
                        await WoWTasks.MoveTowardsWaypointTask(worldState, Waypoints[CurrentWaypointIndex]);

                        CurrentPathfindingState = await ChangeStateBasedOnTaskResult(WoWTasks.MoveTowardsWaypointTask(worldState, Waypoints[CurrentWaypointIndex]),
                            PathfindingState.PICKING_NEXT_WAYPOINT,
                            PathfindingState.MOVING_TOWARDS_WAYPOINT);
                        break;
                }
            }

            Console.WriteLine("Exited Pathfinding loop.  Too many loops without a successful target find.");
            return false;
        }
    }
}
