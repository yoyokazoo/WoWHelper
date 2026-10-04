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
        public int FindTargetCount { get; private set; }
        public long LastFindTargetMarkerTime { get; private set; }
        public Point LastFindTargetMarkerPoint { get; private set; }
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
        public PlayerGoal CurrentPlayerGoal { get; private set; }
        public LogoutState CurrentLogoutState { get; private set; }
        public FindEnemyTargetState CurrentFindEnemyTargetState { get; private set; }

        public int CurrentWaypointIndex { get; private set; }
        public Vector2 CurrentWaypoint => LocationConfiguration.Waypoints[CurrentWaypointIndex]; 
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
            UpdateClassState();

            NextUpdateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() + WowPlayerConstants.TIME_BETWEEN_WORLDSTATE_UPDATES;
        }

        private void UpdateClassState()
        {
            if (ClassState == null && WorldState.IsBotInAValidState)
            {
                CombatConfiguration = WorldState.PlayerClass.Value;
                ClassState = WowClassState.Create(CombatConfiguration);
                Console.WriteLine($"Auto-detected combat config {CombatConfiguration}");
            }

            ClassState?.UpdateFromBitmap(WorldState.Bmp, ScreenConfiguration);
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
            await MouseTurnRateSweepTask(startPixels: 1, stepPixels: 1);
            return true;
        }

        public async Task<bool> CoreLoopTask()
        {
            while (CurrentPlayerMetaState != PlayerMetaState.EXITING)
            {
                await UpdateWorldStateAsync();
                await EveryWorldStateUpdateTasks();
                UpdatePlayerGoal();

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
                            PlayerMetaState.EXECUTING_GOAL,
                            PlayerMetaState.EXITING);
                        break;
                    case PlayerMetaState.EXECUTING_GOAL:
                        Console.WriteLine($"EXECUTING_GOAL, current goal: {CurrentPlayerGoal}");
                        await ExecuteGoalTask();
                        // Right now once we're on the goal execution part, we stay in here forever.
                        break;
                    case PlayerMetaState.EXITING:
                        Console.WriteLine("Surprised we haven't exited yet...");
                        break;
                }
            }

            Environment.Exit(0);
                return true;
        }

        public void UpdatePlayerGoal()
        {
            // TODO refactor this method: each state transition should happen in a more structured manner with an Initialize and a Cleanup or some such
            // (set starting state, key up movement keys, etc.

            if (WorldState.IsInCombat)
            {
                CurrentPlayerGoal = PlayerGoal.FIGHT;
                return;
            }

            if (LogoutTriggered)
            {
                if (CurrentPlayerGoal != PlayerGoal.LOG_OUT)
                {
                    CurrentLogoutState = LogoutState.STARTING_LOGOUT;
                }

                CurrentPlayerGoal = PlayerGoal.LOG_OUT;
                return;
            }

            if (!WorldState.AllSkillsKnownForThisLevel && WorldState.CanAffordToTrainAllSkills)
            {
                CurrentPlayerGoal = PlayerGoal.TRAIN;
                return;
            }

            if (WorldState.BagsAreFull)
            {
                CurrentPlayerGoal = PlayerGoal.SELL;
                return;
            }

            // if (needs to travel to a new location)
            // {
            // CurrentPlayerGoal = PlayerGoal.TRAVEL;
            // return;
            // }

            // if (WorldState.GearNeedsRepair)
            // {
            // CurrentPlayerGoal = PlayerGoal.REPAIR;
            // return;
            // }

            // if (WorldState.HearthInWrongLocation)
            // {
            // CurrentPlayerGoal = PlayerGoal.SET_HEARTH;
            // return;
            // }

            if (CurrentPlayerGoal != PlayerGoal.FIND_ENEMY_TARGET)
            {
                CurrentWaypointIndex = -1;
                WaypointTraversalDirection = 1;

                CurrentFindEnemyTargetState = FindEnemyTargetState.PICK_NEXT_WAYPOINT;
            }

            CurrentPlayerGoal = PlayerGoal.FIND_ENEMY_TARGET;
        }

        public async Task ExecuteGoalTask()
        {
            switch (CurrentPlayerGoal)
            {
                case PlayerGoal.FIGHT:
                    Console.WriteLine($"ExecuteGoalTask not yet implemented for {CurrentPlayerGoal}");
                    break;
                case PlayerGoal.FIND_ENEMY_TARGET:
                    await PlayerFindEnemyTargetGoalTask();
                    break;
                case PlayerGoal.SELL:
                    Console.WriteLine($"ExecuteGoalTask not yet implemented for {CurrentPlayerGoal}");
                    break;
                case PlayerGoal.LOG_OUT:
                    await PlayerLogoutGoalTask();
                    break;
                case PlayerGoal.TRAVEL:
                case PlayerGoal.SET_HEARTH:
                case PlayerGoal.REPAIR:
                case PlayerGoal.TRAIN:
                    Console.WriteLine($"ExecuteGoalTask not yet implemented for {CurrentPlayerGoal}");
                    break;
            }
        }

        public async Task PlayerLogoutGoalTask()
        {
            switch(CurrentLogoutState)
            {
                case LogoutState.STARTING_LOGOUT:
                    await StartLogoutTask();
                    CurrentLogoutState = LogoutState.WAITING_FOR_LOGOUT;
                    break;
                case LogoutState.WAITING_FOR_LOGOUT:
                    // nothing to do here but wait.  EveryWorldStateUpdate will handle seeing
                    // that we've logged out before we'd get back in here
                    break;
            }
        }

        public async Task PlayerFindEnemyTargetGoalTask()
        {
            await TargetEnemyTask();
            UpdateCurrentFindEnemyTargetState();

            switch (CurrentFindEnemyTargetState)
            {
                case FindEnemyTargetState.PICK_NEXT_WAYPOINT:
                    // TODO: May need to rethink this a bit since we'll be re-entering this
                    // method so I think it's going to advance us through the waypoints incorrectly
                    PickNextWaypoint();
                    CurrentFindEnemyTargetState = FindEnemyTargetState.FACE_WAYPOINT;
                    Console.WriteLine($"Picked next waypoint: {CurrentWaypoint}");
                    break;
                case FindEnemyTargetState.FACE_WAYPOINT:
                    // TODO: debugging, remove this
                    float desiredDegrees = WowPathfinding.GetDesiredDirectionInDegrees(WorldState.PlayerLocation, CurrentWaypoint);
                    float degreesDifference = WowPathfinding.GetDegreesToMove(WorldState.FacingDegrees, desiredDegrees);
                    Console.WriteLine($"Before FaceWaypointTask facing {WorldState.FacingDegrees}, aiming towards {desiredDegrees}, need to move {degreesDifference} degrees.");

                    await FaceWaypointTask();
                    CurrentFindEnemyTargetState = FindEnemyTargetState.WALK_TO_WAYPOINT;

                    // TODO: Debugging, remove this
                    await Task.Delay(200);
                    await UpdateWorldStateAsync();
                    desiredDegrees = WowPathfinding.GetDesiredDirectionInDegrees(WorldState.PlayerLocation, CurrentWaypoint);
                    degreesDifference = WowPathfinding.GetDegreesToMove(WorldState.FacingDegrees, desiredDegrees);
                    Console.WriteLine($"After FaceWaypointTask facing {WorldState.FacingDegrees}, error of {degreesDifference} degrees");

                    break;
                case FindEnemyTargetState.WALK_TO_WAYPOINT:
                    // TODO: return true if we can intersect with the waypoint by holding down forward.
                    // if we can't, AKA we're close to the WP, but not close enough to be on it and it's inside our
                    // 0.42 radius circle, return false so we can stop walking forward, re-face, then re-walk forward.
                    // In general our navigation threshold should be set such that this is impossible, and our NPC waypoints
                    // we'll set tight thresholds
                    await WalkToWaypointTask();

                    float targetDistance = Vector2.Distance(WorldState.PlayerLocation, CurrentWaypoint);
                    bool arrived = targetDistance <= LocationConfiguration.DistanceTolerance;
                    if (arrived)
                    {
                        Console.WriteLine($"Arrived at {CurrentWaypoint}, distance away {targetDistance}, picking next waypoint");
                        CurrentFindEnemyTargetState = FindEnemyTargetState.PICK_NEXT_WAYPOINT;
                    }
                    else
                    {
                        // haven't arrived yet, do nothing, we'll keep walking till the next 
                    }

                    break;
                case FindEnemyTargetState.WALK_TO_TARGETED_ENEMY:
                    Console.WriteLine($"Not yet implemented {CurrentFindEnemyTargetState}");
                    Environment.Exit(0);
                    break;
                case FindEnemyTargetState.ENGAGE_TARGETED_ENEMY:
                    Console.WriteLine($"Not yet implemented {CurrentFindEnemyTargetState}");
                    Environment.Exit(0);
                    break;
            }
        }

        public async Task TargetEnemyTask()
        {
            if (GeneralHelpers.CurrentTimeInsideDuration(LastFindTargetTime, WowPlayerConstants.TIME_BETWEEN_FIND_TARGET_MILLIS))
            {
                return;
            }

            LastFindTargetTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            FindTargetCount++;

            if (LocationConfiguration.TargetFindMethod == WowLocationConfiguration.WaypointTargetFindMethod.TAB)
            {
                await WowInput.PressKey(WowInput.TAB_TARGET);
            }
            else if (LocationConfiguration.TargetFindMethod == WowLocationConfiguration.WaypointTargetFindMethod.MACRO)
            {
                await WowInput.PressKey(WowInput.FIND_TARGET_MACRO);
            }
            else if (LocationConfiguration.TargetFindMethod == WowLocationConfiguration.WaypointTargetFindMethod.ALTERNATE)
            {
                if (FindTargetCount % 2 == 0)
                {
                    await WowInput.PressKey(WowInput.TAB_TARGET);
                }
                else
                {
                    await WowInput.PressKey(WowInput.FIND_TARGET_MACRO);
                }
            }
        }

        public void UpdateCurrentFindEnemyTargetState()
        {
            if (!GeneralHelpers.CurrentTimeInsideDuration(LastFindTargetMarkerTime, PATHFINDING_TARGET_MARKER_SCAN_INTERVAL_MILLIS) &&
                TryFindTargetMarkerOnScreen())
            {
                CurrentFindEnemyTargetState = FindEnemyTargetState.WALK_TO_TARGETED_ENEMY;
                return;
            }

            /*
            if (CanEngageTarget())
            {
                CurrentFindEnemyTargetState = FindEnemyTargetState.ENGAGE_TARGETED_ENEMY;
                return;
            }
            */
        }

        public bool TryFindTargetMarkerOnScreen()
        {
            LastFindTargetMarkerTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var targetMarker = WowScreenCapture.FindTargetMarkerOnScreen(ScreenConfiguration);
            if (targetMarker == null)
            {
                return false;
            }

            LastFindTargetMarkerPoint = targetMarker.Value;
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
                if (WorldState.IsBotInAValidState && WorldState.IsInCombat)
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
