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

namespace WoWHelper
{
    public class WoWPlayer
    {
        private static long FARMING_LIMIT_TIME_MILLIS = (long)(8.5 * 60 * 60 * 1000);
        private long lastFarmingLimitTime = 0;

        // TODO: Switch to a system where we use cancellation tokens to exit normal operation and go into "oh shit I got aggroed by something?"
        //private CancellationTokenSource CancellationTokenSource {  get; set; }


        public WoWWorldState WorldState { get; private set; }
        public PlayerState CurrentPlayerState { get; private set; }
        public PathfindingState CurrentPathfindingState { get; private set; }

        public int CurrentWaypointIndex { get; private set; }
        public List<Vector2> Waypoints = new List<Vector2> {
            new Vector2(44.21f, 63.86f),
            new Vector2(44.41f, 59.83f),
            new Vector2(45.78f, 60.20f),
            new Vector2(45.27f, 62.75f)
        };

        public enum PlayerState
        {
            WAITING_TO_FOCUS,
            FOCUSED_ON_WINDOW,
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
            CurrentPathfindingState = PathfindingState.MOVING_TOWARDS_WAYPOINT; // first WP auto picked at index 0
            CurrentWaypointIndex = 0;
        }

        public void UpdateFromBitmap(Bitmap bmp)
        {
            WorldState.UpdateFromBitmap(bmp);
        }

        async Task<PlayerState> ChangeStateBasedOnTaskResult(Task<bool> task, PlayerState successState, PlayerState failureState)
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
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            KeyPoller.EscPressed += () => {
                Console.WriteLine("ESC detected!");
                // set cancellation flag or perform cleanup
            };

            KeyPoller.Start();


            //_ = CoreGameplayLoopTask();
            _ = CorePathfindingLoopTask();
        }

        async Task<bool> CoreGameplayLoopTask()
        {
            lastFarmingLimitTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

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
                    case PlayerState.CHECK_FOR_VALID_TARGET:
                        Console.WriteLine("Checking for valid target");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(WoWTasks.CheckForValidTargetTask(),
                            PlayerState.TRY_TO_CHARGE_TARGET,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
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
                        Console.WriteLine("Target defeated");
                        currentPlayerState = PlayerState.EXITING_CORE_GAMEPLAY_LOOP;
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
            WoWWorldState worldState;

            // true if mob killed, false if we need to do emergency stuff? or do emergency stuff in here?
            do
            {
                await Task.Delay(250);

                worldState = WoWWorldState.GetWoWWorldState();
            } while (worldState.IsInCombat);

            return true;
        }

        async Task<bool> CorePathfindingLoopTask()
        {
            Console.WriteLine("Kicking off core pathfinding loop");

            // Count loops of the waypoints, if we haven't found a target in N loops, error out
            int maxLoops = 1000;
            int loops = 0;
            while (loops < maxLoops)
            {
                // always wait a bit for the UI to update, then grab it?
                //await Task.Delay(250);
                //WoWWorldState worldState = WoWWorldState.GetWoWWorldState();

                loops++;
                switch (CurrentPathfindingState)
                {
                    case PathfindingState.PICKING_NEXT_WAYPOINT:
                        Console.WriteLine($"Picking next waypoint.  Current Waypoint = {Waypoints[CurrentWaypointIndex]}");
                        CurrentWaypointIndex += 1;
                        CurrentWaypointIndex %= Waypoints.Count;
                        CurrentPathfindingState = PathfindingState.MOVING_TOWARDS_WAYPOINT;
                        break;
                    case PathfindingState.MOVING_TOWARDS_WAYPOINT:
                        WoWWorldState worldState = WoWWorldState.GetWoWWorldState();
                        await WoWTasks.MoveTowardsWaypointTask(worldState, Waypoints[CurrentWaypointIndex]);
                        break;
                }
            }

            Console.WriteLine("Exited Core Gameplay");
            //await EQTask.CampTask();
            return true;
        }
    }
}
