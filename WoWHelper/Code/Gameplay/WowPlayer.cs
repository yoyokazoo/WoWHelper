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
        public long FarmStartTime { get; private set; }
        public long LastFindTargetTime { get; private set; }
        public long LastLineOfSightBailoutTime { get; private set; }
        public long LastJumpTime { get; private set; }
        public long DynamiteTime { get; private set; }
        public long HealthPotionTime { get; private set; }
        public long HealingTrinketTime { get; private set; }
        public long BerserkerRageTime { get; private set; }
        public long ImmolateCastTime { get; private set; }
        public long CorruptionCastTime { get; private set; }
        public long NextUpdateTime { get; private set; }
        public bool FullBagsAlertSent { get; private set; }
        public int EngageAttempts { get; private set; }
        public int LootX { get; private set; }
        public int LootY { get; private set; }
        public int? MostRecentTargetMarkerX { get; private set; }
        public int? MostRecentTargetMarkerY { get; private set; }

        public WowWorldState PreviousWorldState { get; private set; }
        public WowWorldState WorldState { get; private set; }

        public WowClassState ClassState { get; private set; }

        public PlayerMetaState CurrentPlayerMetaState { get; private set; }
        public PlayerState CurrentPlayerState { get; private set; }
        public PathfindingState CurrentPathfindingState { get; private set; }

        public int CurrentWaypointIndex { get; private set; }
        public int WaypointTraversalDirection { get; private set; }

        public bool IsOnMerchantRun { get; private set; }
        public MerchantRunPhase CurrentMerchantRunPhase { get; private set; }
        public int CurrentMerchantWaypointIndex { get; private set; }
        public long MerchantRunStartTime { get; private set; }

        public bool LogoutTriggered { get; private set; }
        public string LogoutReason { get; private set; }
        public Bitmap LogoutBitmap { get; private set; }

        public WowLocationConfiguration LocationConfiguration { get; private set; }
        public WowCombatConfiguration CombatConfiguration { get; private set; }

        public WowScreenConfiguration ScreenConfiguration { get; private set; }

        public WowPlayer() : this(WowScreenConfigs.GetForPrimaryScreen()) { }
        public WowPlayer(WowScreenConfiguration screenConfiguration)
        {
            CurrentPlayerMetaState = PlayerMetaState.WAITING_TO_FOCUS_ON_WINDOW;
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
            WowInput.ReleaseAllInputs();
            Environment.Exit(0);
        }

        private static void EnableEscToQuit()
        {
            KeyPoller.EscPressed += ReleaseInputsAndExit;
            KeyPoller.Start();
        }

        public void KickOffCoreLoop()
        {
            EnableEscToQuit();

            _ = CoreLoopTask().ContinueWith(t =>
            {
                Console.WriteLine($"CoreLoopTask crashed: {t.Exception}");
                SlackHelper.SendMessageToChannel($"WoWHelper crashed: {t.Exception?.GetBaseException().Message}");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public void KickOffAdHocTest()
        {
            EnableEscToQuit();

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

        public async Task<bool> CoreLoopTask()
        {
            while (CurrentPlayerMetaState != PlayerMetaState.EXITING)
            {
                await UpdateWorldStateAsync();

                switch (CurrentPlayerMetaState)
                {
                    case PlayerMetaState.WAITING_TO_FOCUS_ON_WINDOW:
                        Console.WriteLine("Focusing on window");
                        CurrentPlayerMetaState = await GeneralHelpers.ChangeStateBasedOnTaskResult(FocusOnWindowTask(),
                            PlayerMetaState.RESOLVE_FARMING_CONFIGURATION,
                            PlayerMetaState.WAITING_TO_FOCUS_ON_WINDOW);
                        break;
                    case PlayerMetaState.RESOLVE_FARMING_CONFIGURATION:
                        Console.WriteLine("Auto-detecting combat/location config from live game state");
                        CurrentPlayerMetaState = await GeneralHelpers.ChangeStateBasedOnTaskResult(ResolveFarmingConfigurationTask(),
                            PlayerMetaState.RUNNING,
                            PlayerMetaState.EXITING);
                        break;
                    case PlayerMetaState.RUNNING:
                        Console.WriteLine("RUNNING");
                        CurrentPlayerMetaState = PlayerMetaState.EXITING;
                        break;
                    case PlayerMetaState.EXITING:
                        Console.WriteLine("Surprised we haven't exited yet...");
                        break;
                }
            }

            Environment.Exit(0);
                return true;
        }

            /*
            public async Task<bool> CoreLoopTask()
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
            */
        }
}
