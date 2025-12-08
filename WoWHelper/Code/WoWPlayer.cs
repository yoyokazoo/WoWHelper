using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
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

        public WoWWorldState WorldState { get; private set; }

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

        public WoWPlayer()
        {
            WorldState = new WoWWorldState();
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
            _ = CoreGameplayLoopTask();
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
    }
}
