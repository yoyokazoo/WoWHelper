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
        // Set whenever an engage attempt bails because WorldState.NotInLineOfSight was true
        // (see AbandonUnreachableEngageTarget in WowCommonCombatTasks.cs). Defaults to 0, so
        // CurrentTimeInsideDuration is false and nothing is suppressed until the first
        // bailout. PathfindingLoopTask checks this to avoid immediately re-acquiring the
        // same unreachable target -- see LINE_OF_SIGHT_RETARGET_SUPPRESS_MILLIS.
        public long LastLineOfSightBailoutTime { get; private set; }
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

        // Class-specific counterpart to WorldState -- see WowClassState. Null until
        // ResolveCombatConfiguration (WowConfigResolutionTasks.cs, called every tick from
        // EveryWorldStateUpdateTasks) picks FarmingConfig.CombatConfiguration from the
        // player's live-detected class (WowWorldState.PlayerClass) and builds the matching
        // concrete type; never changes again afterward. The class-specific Wow*Tasks.cs
        // methods receive it pre-cast to their own class's type (see
        // WowPlayerCombatConfig.cs), not read directly off this property.
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
            // ClassState stays null until ResolveCombatConfiguration can detect the
            // player's actual class -- see the property comment above.

            NextUpdateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        private static WowClassState CreateClassState(WowCombatConfiguration combatConfiguration)
        {
            switch (combatConfiguration)
            {
                case WowCombatConfiguration.Warrior: return new WowWarriorClassState();
                case WowCombatConfiguration.Mage: return new WowMageClassState();
                case WowCombatConfiguration.Shaman: return new WowShamanClassState();
                default: throw new System.NotImplementedException(
                    $"{nameof(CreateClassState)}: no ClassState implemented for CombatConfiguration \"{combatConfiguration}\" -- " +
                    $"this should only be called with a resolved (non-Unknown) CombatConfiguration.");
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
            // ClassState is still null before ResolveCombatConfiguration has run (its
            // concrete type isn't known yet -- nothing reads it before then).
            ClassState?.UpdateFromBitmap(WorldState.Bmp, FarmingConfig.ScreenConfiguration);

            NextUpdateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() + WowPlayerConstants.TIME_BETWEEN_WORLDSTATE_UPDATES;
        }

        public void UpdateWorldState()
        {
            PreviousWorldState.Bmp?.Dispose();
            PreviousWorldState = WorldState;
            WorldState = WowWorldState.GetWoWWorldState(FarmingConfig.ScreenConfiguration);
            ClassState?.UpdateFromBitmap(WorldState.Bmp, FarmingConfig.ScreenConfiguration);

            NextUpdateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() + WowPlayerConstants.TIME_BETWEEN_WORLDSTATE_UPDATES;
        }

        // For Testing only, otherwise use UpdateWorldState
        public void UpdateFromBitmap(Bitmap bmp)
        {
            WorldState.UpdateFromBitmap(bmp);
            ClassState?.UpdateFromBitmap(bmp, FarmingConfig.ScreenConfiguration);
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

            // Fire-and-forget -- nothing else awaits this Task, so without observing its
            // exception here, any unhandled exception anywhere in the state machine (a bug
            // in this codebase, not just a deliberate NotImplementedException) would raise
            // a first-chance exception notification with no further trace, then silently
            // kill the whole gameplay loop -- the character just stops being piloted, with
            // nothing logged and no Slack alert to say why. Logging the full exception
            // (with stack trace) and alerting on Slack turns that into something
            // diagnosable and noticeable instead.
            _ = CoreGameplayLoopTask().ContinueWith(t =>
            {
                Console.WriteLine($"CoreGameplayLoopTask crashed: {t.Exception}");
                SlackHelper.SendMessageToChannel($"WoWHelper crashed: {t.Exception?.GetBaseException().Message}");
            }, TaskContinuationOptions.OnlyOnFaulted);
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
            //await PetriAltF4Task();
            //await CreateHeatmapForLooting(saveBitmaps: true);
            //await TargetMarkerDebugTask();

            // Testing ShamanFaceCorrectDirectionToEngageTask/TurnToFaceTargetMarkerTask (see
            // the "Approach ranged/caster mobs" plan) in isolation, without the full engage
            // state machine around it. ClassState needs resolving once before the loop so the
            // WowShamanClassState cast below has something real to work with.
            //
            // ESC ends the loop early (KeyPoller is the same global ESC-detection mechanism
            // used elsewhere in this codebase) -- useful since this loop otherwise only exits
            // once ShamanFaceCorrectDirectionToEngageTask succeeds, which might never happen
            // mid-test. Scoped to just this task: subscribed/started right before the loop,
            // unsubscribed/stopped right after, so repeat AdHocTest runs don't stack handlers
            // on KeyPoller's static event.
            bool escPressed = false;
            Action onEsc = () => escPressed = true;
            KeyPoller.EscPressed += onEsc;
            KeyPoller.Start();

            try
            {
                ResolveCombatConfiguration();
                while (true)
                {
                    if (escPressed)
                    {
                        Console.WriteLine("ESC pressed, ending ad hoc test loop");
                        break;
                    }

                    await UpdateWorldStateAsync();
                    bool canEngage = await ShamanFaceCorrectDirectionToEngageTask((WowShamanClassState)ClassState);
                    break;
                }
            }
            finally
            {
                KeyPoller.EscPressed -= onEsc;
                KeyPoller.Stop();
            }

            await AvoidObstacleByJumping();
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

        // Captures a full-screen screenshot and searches it for the sentinel-colored target
        // marker UIFunctions.lua paints onto the current target's nameplate (see
        // WowScreenConfiguration.TARGET_MARKER_COLOR) -- a full-resolution capture, not the
        // tiny fixed pixel-row crop WorldState normally reads, since the marker can be
        // anywhere on screen. Returns its screen position, or null if not found (marker not
        // created yet, target occluded, or no target at all). Shared by TargetMarkerDebugTask
        // and WowMovementTasks.TurnToFaceTargetMarkerTask.
        public Point? FindTargetMarkerOnScreen()
        {
            var resolution = FarmingConfig.ScreenConfiguration.Resolution;
            var fullScreenRect = new Rectangle(0, 0, resolution.Width, resolution.Height);

            using (Bitmap fullBmp = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(fullScreenRect))
            {
                return BitmapDifferenceVisualizer.FindColorCentroid(fullBmp, WowScreenConfiguration.TARGET_MARKER_COLOR);
            }
        }

        // TEMP diagnostic (see the "Approach ranged/caster mobs" plan): verify the
        // sentinel-colored target marker UIFunctions.lua paints onto the current target's
        // nameplate is actually findable via screen-capture pixel search, and that its
        // position relative to screen center matches expectations (this bot is run with the
        // camera pitched straight down, so X < center should mean the target is to the
        // player's left, Y < center should mean in front). Loops once a second until the
        // process is stopped -- wired up to the AdHocTest button so it can be exercised in
        // isolation, without running the full combat loop. Remove once confirmed.
        public async Task<bool> TargetMarkerDebugTask()
        {
            while (true)
            {
                await UpdateWorldStateAsync();

                var resolution = FarmingConfig.ScreenConfiguration.Resolution;
                var marker = FindTargetMarkerOnScreen();
                int centerX = resolution.Width / 2;
                int centerY = resolution.Height / 2;

                if (marker == null)
                {
                    Console.WriteLine("WoWHelper DEBUG: target marker NOT FOUND on screen");
                }
                else
                {
                    string leftRight = marker.Value.X < centerX ? "LEFT" : "RIGHT";
                    string frontBack = marker.Value.Y < centerY ? "FRONT" : "BEHIND";
                    Console.WriteLine($"WoWHelper DEBUG: target marker at {marker.Value} (screen center {centerX},{centerY}) -> {leftRight}/{frontBack}");
                }

                await Task.Delay(1000);
            }
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
                    // Mage/Shaman always pull with a spell regardless of FarmingConfig.EngageMethod
                    // (Charge/Pull only distinguishes Warrior's two options -- see the enum's own
                    // comment on WowLocationConfiguration.cs), so checking CombatConfiguration here
                    // instead of EngageMethod covers both classes without needing to know which
                    // location we're on.
                    if (CurrentPlayerState == PlayerState.CONTINUE_TO_TRY_TO_ENGAGE &&
                        (FarmingConfig.CombatConfiguration == WowCombatConfiguration.Mage ||
                         FarmingConfig.CombatConfiguration == WowCombatConfiguration.Shaman) &&
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
                            PlayerState.RESOLVE_FARMING_CONFIGURATION,
                            PlayerState.WAITING_TO_FOCUS_ON_WINDOW);
                        break;
                    case PlayerState.RESOLVE_FARMING_CONFIGURATION:
                        Console.WriteLine("Auto-detecting combat/location config from live game state");
                        CurrentPlayerState = await ChangeStateBasedOnTaskResult(ResolveFarmingConfigurationTask(),
                            PlayerState.CHECK_FOR_LOGOUT,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
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
                        LootX = FarmingConfig.ScreenConfiguration.LootDefaultX;
                        LootY = FarmingConfig.ScreenConfiguration.LootDefaultY;
                        CurrentPlayerState = await ChangeStateBasedOnTaskResult(LootTask(),
                            PlayerState.SKIN_ATTEMPT,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.SKIN_ATTEMPT:
                        Console.WriteLine("Trying to skin");
                        CurrentPlayerState = await ChangeStateBasedOnTaskResult(SkinTask(),
                            PlayerState.LOOT_ATTEMPT_TWO,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.LOOT_ATTEMPT_TWO:
                        Console.WriteLine("Trying to loot a second time, in case the dying anim is slow");
                        await CreateHeatmapForLooting();
                        CurrentPlayerState = await ChangeStateBasedOnTaskResult(LootTask(),
                            PlayerState.SKIN_ATTEMPT_TWO,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.SKIN_ATTEMPT_TWO:
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
