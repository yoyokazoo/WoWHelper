using InputManager;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsGameAutomationTools.Images;
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
        // TODO: add task to zoom out and point camera down

        // TODO: write custom getters/setters for these so we can keep checking the time until they're off cooldown,
        // then use the cached value until they get dirtied again?
        public long FarmStartTime { get; private set; }
        public long LastFindTargetTime { get; private set; }
        public long LastJumpTime { get; private set; }
        public long DynamiteTime { get; private set; }
        public long HealthPotionTime { get; private set; }
        public long HealingTrinketTime { get; private set; } // and Diamond Flask
        public long BerserkerRageTime { get; private set; }
        public long NextUpdateTime { get; private set; }

        public bool FullBagsAlertSent { get; private set; }

        public int EngageAttempts { get; private set; }

        public int LootX { get; private set; }
        public int LootY { get; private set; }

        public WowWorldState PreviousWorldState { get; private set; }
        public WowWorldState WorldState { get; private set; }

        // Class-specific counterpart to WorldState -- see WowClassState.
        // Concrete type is picked once (based on FarmingConfig.CombatConfiguration)
        // and never changes for the lifetime of this WowPlayer; the class-specific
        // Wow*Tasks.cs methods receive it pre-cast to their own class's type
        // (see WowPlayerCombatConfig.cs), not read directly off this property.
        public WowClassState ClassState { get; private set; }

        public PlayerState CurrentPlayerState { get; private set; }
        public PathfindingState CurrentPathfindingState { get; private set; }

        public int CurrentWaypointIndex { get; private set; }
        public int WaypointTraversalDirection { get; private set; }

        public bool LogoutTriggered { get; private set; }
        public string LogoutReason { get; private set; }
        public Bitmap LogoutBitmap { get; private set; }

        public WowFarmingConfiguration FarmingConfig { get; private set; }

        public WowPlayer() : this(WowFarmingConfigs.CURRENT_CONFIG.ScreenConfiguration)
        {
        }

        public WowPlayer(WowScreenConfiguration screenConfiguration)
        {
            CurrentPlayerState = PlayerState.WAITING_TO_FOCUS_ON_WINDOW;
            CurrentPathfindingState = PathfindingState.PICKING_NEXT_WAYPOINT;
            CurrentWaypointIndex = -1;
            WaypointTraversalDirection = 1;

            FarmingConfig = WowFarmingConfigs.CURRENT_CONFIG;
            FarmingConfig.ScreenConfiguration = screenConfiguration;

            PreviousWorldState = new WowWorldState(screenConfiguration);
            WorldState = new WowWorldState(screenConfiguration);
            ClassState = CreateClassState(FarmingConfig.CombatConfiguration);

            NextUpdateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        private static WowClassState CreateClassState(WowCombatConfiguration combatConfiguration)
        {
            switch (combatConfiguration)
            {
                case WowCombatConfiguration.Warrior: return new WowWarriorClassState();
                case WowCombatConfiguration.Mage: return new WowMageClassState();
                case WowCombatConfiguration.Shaman: return new WowShamanClassState();
                default: throw new System.NotImplementedException();
            }
        }

        public async Task UpdateWorldStateAsync()
        {
            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            var timeToWait = NextUpdateTime - now;
            int timeToWaitClamped = (int)Math.Max(0, timeToWait);
            await Task.Delay(timeToWaitClamped);

            PreviousWorldState.Bmp?.Dispose();
            PreviousWorldState = WorldState;
            WorldState = WowWorldState.GetWoWWorldState(FarmingConfig.ScreenConfiguration);
            ClassState.UpdateFromBitmap(WorldState.Bmp, FarmingConfig.ScreenConfiguration);

            NextUpdateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() + WowPlayerConstants.TIME_BETWEEN_WORLDSTATE_UPDATES;
        }

        public void UpdateWorldState()
        {
            PreviousWorldState.Bmp?.Dispose();
            PreviousWorldState = WorldState;
            WorldState = WowWorldState.GetWoWWorldState(FarmingConfig.ScreenConfiguration);
            ClassState.UpdateFromBitmap(WorldState.Bmp, FarmingConfig.ScreenConfiguration);

            NextUpdateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() + WowPlayerConstants.TIME_BETWEEN_WORLDSTATE_UPDATES;
        }

        // For Testing only, otherwise use UpdateWorldState
        public void UpdateFromBitmap(Bitmap bmp)
        {
            WorldState.UpdateFromBitmap(bmp);
            ClassState.UpdateFromBitmap(bmp, FarmingConfig.ScreenConfiguration);
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
                Console.WriteLine("ESC detected! Performing cleanup then quitting");
                await Task.Delay(0);

                // Make sure we don't have any lingering keys pressed down
                Keyboard.KeyUp(WowInput.MOVE_FORWARD);
                Keyboard.KeyUp(WowInput.MOVE_BACK);
                Keyboard.KeyUp(WowInput.TURN_LEFT);
                Keyboard.KeyUp(WowInput.TURN_RIGHT);
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

        public void AdHocTest()
        {
            _ = AdHocTestTask();
            /*
            //_ = AdHocTestTask();
            KeyPoller.EscPressed += async () => {
                Console.WriteLine("ESC detected! Performing cleanup then quitting");
                await Task.Delay(0);

                // Make sure we don't have any lingering keys pressed down
                Keyboard.KeyUp(WowInput.MOVE_FORWARD);
                Keyboard.KeyUp(WowInput.MOVE_BACK);
                Keyboard.KeyUp(WowInput.TURN_LEFT);
                Keyboard.KeyUp(WowInput.TURN_RIGHT);
                Keyboard.KeyUp(WowInput.JUMP);
                Keyboard.KeyUp(WowInput.STRAFE_LEFT);
                Keyboard.KeyUp(WowInput.STRAFE_RIGHT);
                Keyboard.KeyUp(WowInput.LatestShiftKey);
                Keyboard.KeyUp(Keys.LShiftKey);

                Environment.Exit(0);
            };
            KeyPoller.Start();

            _ = CupidTradeLoopTask();
            */
        }

        public async Task<bool> AdHocTestTask()
        {
            //SlackHelper.SendMessageToChannel($"Slack Test");
            await FocusOnWindowTask();
            await FocusOnWindowTask();
            await UpdateWorldStateAsync();
            await CreateHeatmapForLooting(saveBitmaps: true);
            return true;
            /*
            await FocusOnWindowTask();
            await PetriAltF4Task();
            SlackHelper.SendMessageToChannel($"Petri Alt+F4ed!  Consider using Unstuck instead of logging back in");
            Environment.Exit(0);
            
            */

            /*
            await FocusOnWindowTask();
            await ThrowTargetDummyTask();

            await Task.Delay(0);
            return true;
            */
            /*
            await FocusOnWindowTask();
            await Task.Delay(10000);
            await PutMoneyInTradeTask();
            await AcceptTradeTask();
            // wait for button to not be greyed out, and for other player to accept trade
            await Task.Delay(7000);
            await AcceptTradeConfirmationTask();

            //SlackHelper.SendMessageToChannel($"Testing notification!");
            await Task.Delay(0);
            return true;
            */
        }

        public async Task<bool> CreateHeatmapForLooting(bool saveBitmaps = false)
        {
            List<Bitmap> screenChunks = new List<Bitmap>();

            var lootHeatmapRectangle = new Rectangle(
                        FarmingConfig.ScreenConfiguration.LootHeatmapX,
                        FarmingConfig.ScreenConfiguration.LootHeatmapY,
                        FarmingConfig.ScreenConfiguration.LootHeatmapWidth,
                        FarmingConfig.ScreenConfiguration.LootHeatmapHeight);

            for (int i = 0; i < 20; i++)
            {
                Bitmap bmp = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(lootHeatmapRectangle);
                screenChunks.Add(bmp);
                await Task.Delay(100);
            }

            // convert from absolute coords to relative to the snippet we took
            int ignoreXMin = FarmingConfig.ScreenConfiguration.LootHeatmapIgnoreX - FarmingConfig.ScreenConfiguration.LootHeatmapX;
            int ignoreXMax = ignoreXMin + FarmingConfig.ScreenConfiguration.LootHeatmapIgnoreWidth;
            int ignoreYMin = FarmingConfig.ScreenConfiguration.LootHeatmapIgnoreY - FarmingConfig.ScreenConfiguration.LootHeatmapY;
            int ignoreYMax = ignoreYMin + FarmingConfig.ScreenConfiguration.LootHeatmapIgnoreHeight;

            int squareSize = 40;
            int halfSquareSize = squareSize / 2;

            var points = BitmapDifferenceVisualizer.FindHotspots(screenChunks, ignoreXMin, ignoreXMax, ignoreYMin, ignoreYMax);
            var bestSquareOffset = BitmapDifferenceVisualizer.FindBestSquareOffset(points, FarmingConfig.ScreenConfiguration.LootHeatmapWidth, FarmingConfig.ScreenConfiguration.LootHeatmapHeight, squareSize);
            var asdf = BitmapDifferenceVisualizer.BuildDifferenceHeatmap(points, FarmingConfig.ScreenConfiguration.LootHeatmapWidth, FarmingConfig.ScreenConfiguration.LootHeatmapHeight, ignoreXMin, ignoreXMax, ignoreYMin, ignoreYMax);

            Console.WriteLine($"Best Offset = {bestSquareOffset}, click at {new Point(bestSquareOffset.offsetX + halfSquareSize, bestSquareOffset.offsetY + halfSquareSize)}");
            LootX = FarmingConfig.ScreenConfiguration.LootHeatmapX + bestSquareOffset.offsetX + halfSquareSize;
            LootY = FarmingConfig.ScreenConfiguration.LootHeatmapY + bestSquareOffset.offsetY + halfSquareSize;

            Bitmap example = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(lootHeatmapRectangle);

            if (saveBitmaps)
            {
                ScreenCapture.SaveBitmapToFile(asdf, "Heatmap.bmp");
                ScreenCapture.SaveBitmapToFile(example, "Example.bmp");

                using (Bitmap exampleWithIgnore = new Bitmap(example))
                using (Graphics graphics = Graphics.FromImage(exampleWithIgnore))
                {
                    graphics.FillRectangle(Brushes.Black, ignoreXMin, ignoreYMin, ignoreXMax - ignoreXMin, ignoreYMax - ignoreYMin);
                    ScreenCapture.SaveBitmapToFile(exampleWithIgnore, "ExampleWithIgnore.bmp");
                }
            }

            foreach (Bitmap bmp in screenChunks)
            {
                bmp.Dispose();
            }
            asdf.Dispose();
            example.Dispose();

            return true;
        }

        public async Task<bool> CoreGameplayLoopTask()
        {
            FarmStartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            Console.WriteLine("Kicking off core gameplay loop");

            while (CurrentPlayerState != PlayerState.EXITING_CORE_GAMEPLAY_LOOP)
            {
                await UpdateWorldStateAsync();

                // TODO: short circuit into combat/getting out of water/etc.
                // TODO: if on login screen all other values will be messed up
                if (!WorldState.OnLoginScreen && WorldState.IsInCombat)
                {
                    if (CurrentPlayerState == PlayerState.CONTINUE_TO_TRY_TO_ENGAGE && 
                        FarmingConfig.EngageMethod == WowLocationConfiguration.EngagementMethod.Spellcast && 
                        WorldState.ResourcePercent < 100)
                    {
                        // We likely just cast a spell that hasn't yet hit the target.  Wait a little bit so it does,
                        // so we correctly read that our current target is in combat with us, otherwise we get confused
                        Console.WriteLine($"Waiting for spellcast");
                        await Task.Delay(1200);
                    }
                    Console.WriteLine($"In combat unexpectedly ({CurrentPlayerState}), switching to PlayerState.IN_CORE_COMBAT_LOOP");
                    CurrentPlayerState = PlayerState.IN_CORE_COMBAT_LOOP;
                    //Keyboard.KeyPress(WowInput.CLEAR_TARGET_MACRO); // we may have an errant target that's not attacking us
                }

                await EveryWorldStateUpdateTasks();

                switch (CurrentPlayerState)
                {
                    case PlayerState.WAITING_TO_FOCUS_ON_WINDOW:
                        Console.WriteLine("Focusing on window");
                        CurrentPlayerState = await ChangeStateBasedOnTaskResult(FocusOnWindowTask(),
                            PlayerState.CHECK_FOR_LOGOUT,
                            PlayerState.WAITING_TO_FOCUS_ON_WINDOW);
                        break;
                    case PlayerState.CHECK_FOR_LOGOUT:
                        Console.WriteLine("Checking if we should log out");
                        CurrentPlayerState = await ChangeStateBasedOnTaskResult(SetLogoutVariablesTask(),
                            PlayerState.START_LOGGING_OUT,
                            PlayerState.START_BATTLE_READY_RECOVERY);
                        break;
                    case PlayerState.START_LOGGING_OUT:
                        Console.WriteLine($"Started logging out ({LogoutReason})");
                        SlackHelper.SendMessageToChannel($"Logging out: {LogoutReason}");
                        CurrentPlayerState = await ChangeStateBasedOnTaskResult(StartLogoutTask(),
                            PlayerState.WAITING_TO_LOG_OUT,
                            PlayerState.IN_CORE_COMBAT_LOOP);
                        break;
                    case PlayerState.WAITING_TO_LOG_OUT:
                        Console.WriteLine("Waiting to log out");
                        CurrentPlayerState = await ChangeStateBasedOnTaskResult(CheckIfLoggedOutTask(),
                            PlayerState.LOGGED_OUT,
                            PlayerState.WAITING_TO_LOG_OUT);
                        break;
                    case PlayerState.LOGGED_OUT:
                        Console.WriteLine("Logged out");
                        CurrentPlayerState = PlayerState.EXITING_CORE_GAMEPLAY_LOOP;
                        break;
                    case PlayerState.START_BATTLE_READY_RECOVERY:
                        Console.WriteLine("Starting battle ready recovery");
                        CurrentPlayerState = await ChangeStateBasedOnTaskResult(StartBattleReadyTask(),
                            PlayerState.WAIT_UNTIL_BATTLE_READY,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.WAIT_UNTIL_BATTLE_READY:
                        Console.WriteLine("Waiting until battle ready");
                        CurrentPlayerState = await ChangeStateBasedOnTaskResult(WaitUntilBattleReadyTask(),
                            PlayerState.CHECK_FOR_VALID_TARGET,
                            PlayerState.WAIT_UNTIL_BATTLE_READY);
                        break;
                    case PlayerState.CHECK_FOR_VALID_TARGET:
                        Console.WriteLine("Checking for valid target");
                        CurrentPlayerState = await ChangeStateBasedOnTaskResult(PathfindingLoopTask(),
                            PlayerState.INITIATE_ENGAGE_TARGET,
                            PlayerState.IN_CORE_COMBAT_LOOP);
                        break;
                    case PlayerState.INITIATE_ENGAGE_TARGET:
                        Console.WriteLine("Trying to engage target");
                        CurrentPlayerState = await ChangeStateBasedOnTaskResult(StartEngageTask(),
                            PlayerState.CONTINUE_TO_TRY_TO_ENGAGE,
                            PlayerState.CHECK_FOR_LOGOUT);
                        break;
                    case PlayerState.CONTINUE_TO_TRY_TO_ENGAGE:
                        Console.WriteLine("Continuing to engage target");
                        CurrentPlayerState = await ChangeStateBasedOnTaskResult(WaitUntilEngageTask(),
                            PlayerState.CONTINUE_TO_TRY_TO_ENGAGE,
                            PlayerState.CHECK_FOR_LOGOUT);
                        break;
                    case PlayerState.IN_CORE_COMBAT_LOOP:
                        Console.WriteLine("In core combat loop");
                        CurrentPlayerState = await ChangeStateBasedOnTaskResult(CombatLoopTask(),
                            PlayerState.TARGET_DEFEATED,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.TARGET_DEFEATED:
                        Console.WriteLine("Target defeated, trying to loot");
                        // TODO: /canceltarget and /stopcasting and /stopattack here so we don't accidentally attack something
                        await WaitUnlessInCombatTask(6000);
                        await CreateHeatmapForLooting();
                        CurrentPlayerState = await ChangeStateBasedOnTaskResult(LootTask(),
                            PlayerState.LOOT_ATTEMPT_TWO,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.LOOT_ATTEMPT_TWO:
                        Console.WriteLine("Trying to loot a second time, in case the dying anim is slow");
                        CurrentPlayerState = await ChangeStateBasedOnTaskResult(SkinTask(),
                            PlayerState.SKIN_ATTEMPT,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.SKIN_ATTEMPT:
                        Console.WriteLine("Trying to skin");
                        CurrentPlayerState = await ChangeStateBasedOnTaskResult(SkinTask(),
                            PlayerState.CHECK_FOR_LOGOUT,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        await ScootForwardsTask();
                        break;
                }
            }

            Console.WriteLine("Exited Core Gameplay");
            Environment.Exit(0);

            return true;
        }
    }
}
