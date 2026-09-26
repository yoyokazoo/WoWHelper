using InputManager;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsGameAutomationTools.Slack;
using WoWHelper.Code;
using WoWHelper.Code.Config;
using WoWHelper.Code.Gameplay;
using WoWHelper.Code.WorldState;
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
        // Set whenever an engage attempt bails because WorldState.TargetUnreachable was true
        // (the "not in line of sight" or "no path available" toast)
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
        // Warlock DoTs -- see WarlockShouldCastImmolate/WarlockShouldCastCorruption in
        // WowWarlockTasks.cs, which suppress re-casting a DoT within
        // WowGameplayConstants.WARLOCK_DOT_RECAST_SUPPRESS_MILLIS of these.
        public long ImmolateCastTime { get; private set; }
        public long CorruptionCastTime { get; private set; }
        public long NextUpdateTime { get; private set; }

        public bool FullBagsAlertSent { get; private set; }

        public int EngageAttempts { get; private set; }

        public int LootX { get; private set; }
        public int LootY { get; private set; }

        // Last position WowScreenCapture.FindTargetMarkerOnScreen() actually found the target marker at --
        // set by WowMovementTasks.WalkIntoMeleeRangeTask() each time a scan succeeds. Null
        // until the first successful scan (or if none has succeeded yet this attempt).
        // Nullable rather than defaulting to 0,0 like LootX/Y above -- unlike loot's "default
        // to screen center" fallback, there's no sane default screen position for "target not
        // found," so callers need to be able to tell the difference.
        public int? MostRecentTargetMarkerX { get; private set; }
        public int? MostRecentTargetMarkerY { get; private set; }

        public WowWorldState PreviousWorldState { get; private set; }
        public WowWorldState WorldState { get; private set; }

        // Class-specific counterpart to WorldState -- see WowClassState. Null until
        // ResolveCombatConfiguration (WowConfigResolutionTasks.cs, called every tick from
        // EveryWorldStateUpdateTasks) picks CombatConfiguration from the
        // player's live-detected class (WowWorldState.PlayerClass) and builds the matching
        // concrete type; never changes again afterward. The class-specific Wow*Tasks.cs
        // methods receive it pre-cast to their own class's type (see
        // WowPlayerCombatConfig.cs), not read directly off this property.
        public WowClassState ClassState { get; private set; }

        public PlayerState CurrentPlayerState { get; private set; }
        public PathfindingState CurrentPathfindingState { get; private set; }

        public int CurrentWaypointIndex { get; private set; }
        public int WaypointTraversalDirection { get; private set; }

        // Merchant-run detour state -- see WowMerchantConfiguration and
        // WowMovementTasks.MerchantRunStepTask. Plain fields, same as the pathfinding state
        // above, so a combat interruption mid-trip leaves them untouched and the trip resumes
        // exactly where it left off once PathfindingLoopTask runs again.
        public bool IsOnMerchantRun { get; private set; }
        public MerchantRunPhase CurrentMerchantRunPhase { get; private set; }
        public int CurrentMerchantWaypointIndex { get; private set; }
        // Unix millis when the current merchant run branched off the route -- see the
        // MERCHANT_RUN_TIMEOUT_MILLIS check in PathfindingLoopTask.
        public long MerchantRunStartTime { get; private set; }

        public bool LogoutTriggered { get; private set; }
        public string LogoutReason { get; private set; }
        public Bitmap LogoutBitmap { get; private set; }

        // Both resolved from live game state rather than hardcoded -- see
        // ResolveCombatConfiguration/ResolveFarmingConfigurationTask
        // (WowConfigResolutionTasks.cs). LocationConfiguration stays null (and
        // CombatConfiguration stays Unknown) until then; LocationConfiguration can stay null
        // for a whole run that started mid-combat, so null-check it where that can happen.
        public WowLocationConfiguration LocationConfiguration { get; private set; }
        public WowCombatConfiguration CombatConfiguration { get; private set; }

        public WowScreenConfiguration ScreenConfiguration { get; private set; }

        public WowPlayer() : this(WowScreenConfigs.GetForPrimaryScreen()) { }
        public WowPlayer(WowScreenConfiguration screenConfiguration)
        {
            CurrentPlayerState = PlayerState.WAITING_TO_FOCUS_ON_WINDOW;
            CurrentPathfindingState = PathfindingState.PICKING_NEXT_WAYPOINT;
            CurrentWaypointIndex = -1;
            WaypointTraversalDirection = 1;

            IsOnMerchantRun = false;
            CurrentMerchantRunPhase = MerchantRunPhase.WALKING_TO_MERCHANT;
            CurrentMerchantWaypointIndex = 0;

            LocationConfiguration = null;
            CombatConfiguration = WowCombatConfiguration.Unknown;
            ScreenConfiguration = screenConfiguration;

            PreviousWorldState = new WowWorldState(screenConfiguration);
            WorldState = new WowWorldState(screenConfiguration);
            ClassState = null;

            NextUpdateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public async Task UpdateWorldStateAsync()
        {
            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            var timeToWait = NextUpdateTime - now;
            int timeToWaitClamped = (int)Math.Max(0, timeToWait);
            await Task.Delay(timeToWaitClamped);
            UpdateWorldState();
        }

        public void UpdateWorldState()
        {
            PreviousWorldState.Bmp?.Dispose();
            PreviousWorldState = WorldState;
            WorldState = WowWorldState.GetWoWWorldState(ScreenConfiguration);
            ClassState?.UpdateFromBitmap(WorldState.Bmp, ScreenConfiguration);

            NextUpdateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() + WowPlayerConstants.TIME_BETWEEN_WORLDSTATE_UPDATES;
        }

        // For Testing only, otherwise use UpdateWorldState
        public void UpdateWorldStateFromBitmap(Bitmap bmp)
        {
            WorldState.UpdateFromBitmap(bmp);
            ClassState?.UpdateFromBitmap(bmp, ScreenConfiguration);
        }

        private static void ReleaseInputsAndExit()
        {
            Console.WriteLine("ESC detected! Performing cleanup then quitting");

            // Make sure we don't have any lingering keys pressed down
            Keyboard.KeyUp(WowInput.MOVE_FORWARD);
            Keyboard.KeyUp(WowInput.MOVE_BACK);
            Keyboard.KeyUp(WowInput.TURN_LEFT);
            Keyboard.KeyUp(WowInput.TURN_RIGHT);
            Keyboard.KeyUp(WowInput.JUMP);
            Keyboard.KeyUp(WowInput.STRAFE_LEFT);
            Keyboard.KeyUp(WowInput.STRAFE_RIGHT);
            Keyboard.KeyUp(WowInput.LatestShiftKey);
            Keyboard.KeyUp(WowInput.LatestControlKey);
            Keyboard.KeyUp(Keys.LShiftKey);
            Mouse.ButtonUp(Mouse.MouseKeys.Right);

            Environment.Exit(0);
        }

        public void KickOffCoreLoop()
        {
            KeyPoller.EscPressed += ReleaseInputsAndExit;
            KeyPoller.Start();

            _ = CoreGameplayLoopTask().ContinueWith(t =>
            {
                Console.WriteLine($"CoreGameplayLoopTask crashed: {t.Exception}");
                SlackHelper.SendMessageToChannel($"WoWHelper crashed: {t.Exception?.GetBaseException().Message}");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public void KickOffAdHocTest()
        {
            KeyPoller.EscPressed += ReleaseInputsAndExit;
            KeyPoller.Start();

            _ = AdHocTestTask().ContinueWith(t =>
            {
                Console.WriteLine($"AdHocTestTask crashed: {t.Exception}");
                SlackHelper.SendMessageToChannel($"WoWHelper crashed: {t.Exception?.GetBaseException().Message}");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public async Task<bool> AdHocTestTask()
        {
            await FocusOnWindowTask();
            await UpdateWorldStateAsync();
            await MouseTurnRateSweepTask(startPixels: 370, stepPixels: 25);
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
                    // Shaman always pulls with a spell regardless of LocationConfiguration.EngageMethod
                    // (Charge/Pull only distinguishes Warrior's two options -- see the enum's own
                    // comment on WowLocationConfiguration.cs), so checking CombatConfiguration here
                    // instead of EngageMethod covers it without needing to know which location
                    // we're on. (Warlock also always pulls with a spell but isn't checked here --
                    // pre-existing gap from before Warlock support was added, not touched by the
                    // Mage removal that dropped the Mage half of this check.)
                    if (CurrentPlayerState == PlayerState.CONTINUE_TO_TRY_TO_ENGAGE &&
                        CombatConfiguration == WowCombatConfiguration.Shaman &&
                        WorldState.ResourcePercent < 100)
                    {
                        // We likely just cast a spell that hasn't yet hit the target.  Wait a little bit so it does,
                        // so we correctly read that our current target is in combat with us, otherwise we get confused
                        Console.WriteLine($"Waiting for spellcast");
                        await Task.Delay(1200);
                    }
                    Console.WriteLine($"In combat unexpectedly ({CurrentPlayerState}), switching to PlayerState.IN_CORE_COMBAT_LOOP");
                    CurrentPlayerState = PlayerState.IN_CORE_COMBAT_LOOP;
                    //await WowInput.PressKey(WowInput.CLEAR_TARGET_MACRO); // we may have an errant target that's not attacking us
                }

                await EveryWorldStateUpdateTasks();

                switch (CurrentPlayerState)
                {
                    case PlayerState.WAITING_TO_FOCUS_ON_WINDOW:
                        Console.WriteLine("Focusing on window");
                        CurrentPlayerState = await GeneralHelpers.ChangeStateBasedOnTaskResult(FocusOnWindowTask(),
                            PlayerState.RESOLVE_FARMING_CONFIGURATION,
                            PlayerState.WAITING_TO_FOCUS_ON_WINDOW);
                        break;
                    case PlayerState.RESOLVE_FARMING_CONFIGURATION:
                        Console.WriteLine("Auto-detecting combat/location config from live game state");
                        CurrentPlayerState = await GeneralHelpers.ChangeStateBasedOnTaskResult(ResolveFarmingConfigurationTask(),
                            PlayerState.CHECK_FOR_LOGOUT,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.CHECK_FOR_LOGOUT:
                        Console.WriteLine("Checking if we should log out");
                        CurrentPlayerState = await GeneralHelpers.ChangeStateBasedOnTaskResult(SetLogoutVariablesTask(),
                            PlayerState.START_LOGGING_OUT,
                            PlayerState.START_BATTLE_READY_RECOVERY);
                        break;
                    case PlayerState.START_LOGGING_OUT:
                        Console.WriteLine($"Started logging out ({LogoutReason})");
                        SlackHelper.SendMessageToChannel($"Logging out: {LogoutReason}");
                        CurrentPlayerState = await GeneralHelpers.ChangeStateBasedOnTaskResult(StartLogoutTask(),
                            PlayerState.WAITING_TO_LOG_OUT,
                            PlayerState.IN_CORE_COMBAT_LOOP);
                        break;
                    case PlayerState.WAITING_TO_LOG_OUT:
                        Console.WriteLine("Waiting to log out");
                        CurrentPlayerState = await GeneralHelpers.ChangeStateBasedOnTaskResult(CheckIfLoggedOutTask(),
                            PlayerState.LOGGED_OUT,
                            PlayerState.WAITING_TO_LOG_OUT);
                        break;
                    case PlayerState.LOGGED_OUT:
                        Console.WriteLine("Logged out");
                        CurrentPlayerState = PlayerState.EXITING_CORE_GAMEPLAY_LOOP;
                        break;
                    case PlayerState.START_BATTLE_READY_RECOVERY:
                        Console.WriteLine("Starting battle ready recovery");
                        CurrentPlayerState = await GeneralHelpers.ChangeStateBasedOnTaskResult(StartBattleReadyTask(),
                            PlayerState.WAIT_UNTIL_BATTLE_READY,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.WAIT_UNTIL_BATTLE_READY:
                        Console.WriteLine("Waiting until battle ready");
                        CurrentPlayerState = await GeneralHelpers.ChangeStateBasedOnTaskResult(WaitUntilBattleReadyTask(),
                            PlayerState.CHECK_FOR_VALID_TARGET,
                            PlayerState.WAIT_UNTIL_BATTLE_READY);
                        break;
                    case PlayerState.CHECK_FOR_VALID_TARGET:
                        Console.WriteLine("Checking for valid target");
                        CurrentPlayerState = await GeneralHelpers.ChangeStateBasedOnTaskResult(PathfindingLoopTask(),
                            PlayerState.INITIATE_ENGAGE_TARGET,
                            PlayerState.IN_CORE_COMBAT_LOOP);
                        break;
                    case PlayerState.INITIATE_ENGAGE_TARGET:
                        Console.WriteLine("Trying to engage target");
                        CurrentPlayerState = await GeneralHelpers.ChangeStateBasedOnTaskResult(StartEngageTask(),
                            PlayerState.CONTINUE_TO_TRY_TO_ENGAGE,
                            PlayerState.CHECK_FOR_LOGOUT);
                        break;
                    case PlayerState.CONTINUE_TO_TRY_TO_ENGAGE:
                        Console.WriteLine("Continuing to engage target");
                        CurrentPlayerState = await GeneralHelpers.ChangeStateBasedOnTaskResult(WaitUntilEngageTask(),
                            PlayerState.CONTINUE_TO_TRY_TO_ENGAGE,
                            PlayerState.CHECK_FOR_LOGOUT);
                        break;
                    case PlayerState.IN_CORE_COMBAT_LOOP:
                        Console.WriteLine("In core combat loop");
                        CurrentPlayerState = await GeneralHelpers.ChangeStateBasedOnTaskResult(CombatLoopTask(),
                            PlayerState.TARGET_DEFEATED,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.TARGET_DEFEATED:
                        Console.WriteLine("Target defeated, trying to loot");
                        // TODO: /canceltarget and /stopcasting and /stopattack here so we don't accidentally attack something
                        await WaitUnlessInCombatTask(1500); // give the dying anim a sec
                        LootX = ScreenConfiguration.LootDefaultX;
                        LootY = ScreenConfiguration.LootDefaultY;
                        CurrentPlayerState = await GeneralHelpers.ChangeStateBasedOnTaskResult(LootTask(),
                            PlayerState.SKIN_ATTEMPT,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.SKIN_ATTEMPT:
                        Console.WriteLine("Trying to skin");
                        CurrentPlayerState = await GeneralHelpers.ChangeStateBasedOnTaskResult(SkinTask(),
                            PlayerState.LOOT_ATTEMPT_TWO,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.LOOT_ATTEMPT_TWO:
                        Console.WriteLine("Trying to loot a second time, in case the dying anim is slow");
                        Point lootPoint = await WowScreenCapture.CreateHeatmapForLooting(ScreenConfiguration);
                        LootX = lootPoint.X;
                        LootY = lootPoint.Y;
                        CurrentPlayerState = await GeneralHelpers.ChangeStateBasedOnTaskResult(LootTask(),
                            PlayerState.SKIN_ATTEMPT_TWO,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.SKIN_ATTEMPT_TWO:
                        Console.WriteLine("Trying to skin");
                        CurrentPlayerState = await GeneralHelpers.ChangeStateBasedOnTaskResult(SkinTask(),
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
