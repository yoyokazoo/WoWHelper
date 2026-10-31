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
        public bool EmergencyCombatActionTaken { get; private set; }

        public WowWorldState PreviousWorldState { get; private set; }
        public WowWorldState WorldState { get; private set; }

        public WowClassState ClassState { get; private set; }

        public PlayerMetaState CurrentPlayerMetaState { get; private set; }
        public PlayerState CurrentPlayerState { get; private set; }
        public PathfindingState CurrentPathfindingState { get; private set; }
        public PlayerGoal CurrentPlayerGoal { get; private set; }
        public LogoutState CurrentLogoutState { get; private set; }
        public FindEnemyTargetState CurrentFindEnemyTargetState { get; private set; }
        public BattleRecoveryState CurrentBattleRecoveryState { get; private set; }
        public InteractWithNPCState CurrentInteractWithNPCState { get; private set; }
        public NavigateToEndOfCurrentRouteState CurrentNavigateToEndOfCurrentRouteState { get; private set; }

        public WowNPCConfiguration CurrentNPCTarget {  get; private set; }

        public WowRoute CurrentRoute {  get; private set; }
        public int CurrentWaypointIndex { get; private set; }
        public Vector2 CurrentWaypoint => CurrentRoute.Waypoints[CurrentWaypointIndex]; 
        public int WaypointTraversalDirection { get; private set; }

        public bool IsOnMerchantRun { get; private set; }
        public MerchantRunPhase CurrentMerchantRunPhase { get; private set; }
        public int CurrentMerchantWaypointIndex { get; private set; }
        public long MerchantRunStartTime { get; private set; }

        public bool LogoutTriggered { get; private set; }
        public string LogoutReason { get; private set; }
        public Bitmap LogoutBitmap { get; private set; }

        public WowLocationConfiguration FarmingLocationConfiguration { get; private set; }
        public WowCombatConfiguration CombatConfiguration { get; private set; }
        public WowScreenConfiguration ScreenConfiguration { get; private set; }

        public WowPlayer() : this(WowScreenConfigs.GetForPrimaryScreen()) { }
        public WowPlayer(WowScreenConfiguration screenConfiguration)
        {
            CurrentPlayerMetaState = PlayerMetaState.WAITING_TO_FOCUS_ON_WINDOW;
            CurrentPlayerState = PlayerState.WAITING_TO_FOCUS_ON_WINDOW;
            CurrentPathfindingState = PathfindingState.PICKING_NEXT_WAYPOINT;
            CurrentPlayerGoal = PlayerGoal.NONE;
            CurrentBattleRecoveryState = BattleRecoveryState.STARTING_RECOVERY;

            CurrentWaypointIndex = -1;
            WaypointTraversalDirection = 1;

            IsOnMerchantRun = false;
            CurrentMerchantRunPhase = MerchantRunPhase.WALKING_TO_MERCHANT;
            CurrentMerchantWaypointIndex = 0;

            FarmingLocationConfiguration = null;
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
            await Task.Delay(4000);
            //await MouseTurnRateSweepTask(startPixels: 1, stepPixels: 1);
            //var asdf = WowPathfinding.GetWaypointsToTarget(this);
            //var asdf = WowPathfinding.GetRoutesToTarget(this, WowNPCConfigs.VALLEY_OF_TRIALS_MERCHANT.Route);
            //Console.WriteLine(String.Join(",", asdf));
            return true;
        }

        public async Task<bool> CoreLoopTask()
        {
            while (CurrentPlayerMetaState != PlayerMetaState.EXITING)
            {
                await UpdateWorldStateAsync();
                await EveryWorldStateUpdateTasks();
                await UpdatePlayerGoalTask();

                switch (CurrentPlayerMetaState)
                {
                    case PlayerMetaState.WAITING_TO_FOCUS_ON_WINDOW:
                        Console.WriteLine("Focusing on window");
                        CurrentPlayerMetaState = await GeneralHelpers.ChangeStateBasedOnTaskResult(FocusOnWindowTask(),
                            PlayerMetaState.EXECUTING_GOAL,
                            PlayerMetaState.WAITING_TO_FOCUS_ON_WINDOW);
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

        public async Task UpdatePlayerGoalTask()
        {
            if (!WorldState.IsBotInAValidState)
            {
                return;
            }

            if (WorldState.IsInCombat)
            {
                await SetPlayerGoalTask(PlayerGoal.FIGHT);
                return;
            }

            if (LogoutTriggered)
            {
                await SetPlayerGoalTask(PlayerGoal.LOG_OUT);
                return;
            }

            if (!PlayerIsBattleReady())
            {
                await SetPlayerGoalTask(PlayerGoal.RECOVER_FROM_BATTLE);
                return;
            }

            //if ()

            //if ()
            //{

            //}

            if (!WorldState.AllSkillsKnownForThisLevel && WorldState.CanAffordToTrainAllSkills)
            {
                await SetPlayerGoalTask(PlayerGoal.TRAIN);
                return;
            }

            if (WorldState.BagsAreFull)
            {
                await SetPlayerGoalTask(PlayerGoal.SELL);
                return;
            }

            // if (needs to travel to a new location)
            // {
            // await SetPlayerGoalTask(PlayerGoal.TRAVEL);
            // return;
            // }

            // if (WorldState.GearNeedsRepair)
            // {
            // await SetPlayerGoalTask(PlayerGoal.REPAIR);
            // return;
            // }

            // if (WorldState.HearthInWrongLocation)
            // {
            // await SetPlayerGoalTask(PlayerGoal.SET_HEARTH);
            // return;
            // }

            await SetPlayerGoalTask(PlayerGoal.FIND_ENEMY_TARGET);
        }

        // The only place CurrentPlayerGoal should change. Re-selecting the current goal (every tick) is a no-op;
        // an actual change runs the old goal's ExitGoalTask cleanup, then the new goal's EnterGoalTask setup.
        public async Task SetPlayerGoalTask(PlayerGoal newGoal)
        {
            if (newGoal == CurrentPlayerGoal)
            {
                return;
            }

            Console.WriteLine($"Goal change: {CurrentPlayerGoal} -> {newGoal}");
            await ExitGoalTask(CurrentPlayerGoal);
            CurrentPlayerGoal = newGoal;
            await EnterGoalTask(newGoal);
        }

        private async Task EnterGoalTask(PlayerGoal goal)
        {
            switch (goal)
            {
                case PlayerGoal.LOG_OUT:
                    CurrentLogoutState = LogoutState.STARTING_LOGOUT;
                    break;
                case PlayerGoal.FIND_ENEMY_TARGET:
                    CurrentFindEnemyTargetState = FindEnemyTargetState.PICK_FARMING_LOCATION_CONFIG;
                    LastJumpTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    break;
                case PlayerGoal.FIGHT:
                    EmergencyCombatActionTaken = false;
                    break;
                case PlayerGoal.RECOVER_FROM_BATTLE:
                    CurrentBattleRecoveryState = BattleRecoveryState.LOOT_ATTEMPT_ONE;
                    break;
            }

            await Task.CompletedTask;
        }

        private async Task ExitGoalTask(PlayerGoal goal)
        {
            await KeyUpMovementKeys();
        }

        public async Task ExecuteGoalTask()
        {
            switch (CurrentPlayerGoal)
            {
                case PlayerGoal.FIGHT:
                    await PlayerSingleCombatTask();
                    break;
                case PlayerGoal.RECOVER_FROM_BATTLE:
                    await PlayerRecoverFromBattleTask();
                    break;
                case PlayerGoal.FIND_ENEMY_TARGET:
                    await PlayerFindEnemyTargetGoalTask();
                    break;
                case PlayerGoal.LOG_OUT:
                    await PlayerLogoutGoalTask();
                    break;
                case PlayerGoal.TRAIN:
                    await PlayerInteractWithNPCTask();
                    break;
                case PlayerGoal.SELL:
                case PlayerGoal.TRAVEL:
                case PlayerGoal.SET_HEARTH:
                case PlayerGoal.REPAIR:
                    string errorMessage = $"ExecuteGoalTask not yet implemented for {CurrentPlayerGoal}";
                    SlackHelper.SendMessageToChannel(errorMessage);
                    await Task.Delay(500);
                    Environment.Exit(0);
                    break;
            }
        }

        public async Task PlayerInteractWithNPCTask()
        {
            Console.WriteLine($"Inside PlayerInteractWithNPCTask, CurrentInteractWithNPCState = {CurrentInteractWithNPCState}");
            switch(CurrentInteractWithNPCState)
            {
                case InteractWithNPCState.PICK_NPC_TO_INTERACT_WITH:
                    CurrentNPCTarget = PickNPCToInteractWith();
                    Console.WriteLine($"Picked NPC to target: {CurrentNPCTarget}");
                    CurrentInteractWithNPCState = InteractWithNPCState.BUILD_ROUTE_TO_NPC;
                    break;
                case InteractWithNPCState.BUILD_ROUTE_TO_NPC:
                    CurrentRoute = WowPathfinding.GetRouteToTarget(this, CurrentNPCTarget.Route);
                    Console.WriteLine($"Picked Route: {String.Join(",", CurrentRoute.Waypoints)}");
                    CurrentInteractWithNPCState = InteractWithNPCState.NAVIGATE_ROUTE_TO_NPC;

                    // Manually set the initial waypoint
                    CurrentWaypointIndex = 0;
                    CurrentNavigateToEndOfCurrentRouteState = NavigateToEndOfCurrentRouteState.FACE_WAYPOINT;
                    break;
                case InteractWithNPCState.NAVIGATE_ROUTE_TO_NPC:
                    await NavigateToEndOfCurrentRouteTask();
                    if (CurrentNavigateToEndOfCurrentRouteState == NavigateToEndOfCurrentRouteState.ARRIVED_AT_END_OF_ROUTE)
                    {
                        CurrentInteractWithNPCState = InteractWithNPCState.INTERACT_WITH_NPC;
                    }
                    break;
                case InteractWithNPCState.INTERACT_WITH_NPC:
                    await InteractWithNPCTask();
                    CurrentInteractWithNPCState = InteractWithNPCState.DONE_INTERACTING_WITH_NPC;
                    break;
                case InteractWithNPCState.DONE_INTERACTING_WITH_NPC:
                    Console.WriteLine($"Shouldn't actually be here maybe? New goal should have been picked");
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

        public async Task PlayerRecoverFromBattleTask()
        {
            switch (CurrentBattleRecoveryState)
            {
                case BattleRecoveryState.LOOT_ATTEMPT_ONE:
                    await WaitUnlessInCombatTask(1500);
                    LootX = ScreenConfiguration.LootDefaultX;
                    LootY = ScreenConfiguration.LootDefaultY;
                    await LootTask();
                    CurrentBattleRecoveryState = BattleRecoveryState.SKIN_ATTEMPT_ONE;
                    break;
                case BattleRecoveryState.SKIN_ATTEMPT_ONE:
                    await SkinTask();
                    CurrentBattleRecoveryState = BattleRecoveryState.LOOT_ATTEMPT_TWO;
                    break;
                case BattleRecoveryState.LOOT_ATTEMPT_TWO:
                    Point lootPoint = await WowScreenCapture.CreateHeatmapForLooting(ScreenConfiguration);
                    LootX = lootPoint.X;
                    LootY = lootPoint.Y;
                    await LootTask();
                    CurrentBattleRecoveryState = BattleRecoveryState.SKIN_ATTEMPT_TWO;
                    break;
                case BattleRecoveryState.SKIN_ATTEMPT_TWO:
                    await SkinTask();
                    CurrentBattleRecoveryState = BattleRecoveryState.STARTING_RECOVERY;
                    break;
                case BattleRecoveryState.STARTING_RECOVERY:
                    await StartBattleReadyTask();
                    CurrentBattleRecoveryState = BattleRecoveryState.WAITING_FOR_RECOVERY;
                    break;
                case BattleRecoveryState.WAITING_FOR_RECOVERY:
                    if (await WaitUntilBattleReadyTask())
                    {
                        await ScootForwardsTask();
                        await SetPlayerGoalTask(PlayerGoal.NONE);
                    }
                    break;
            }
        }

        public async Task NavigateToEndOfCurrentRouteTask()
        {
            Console.WriteLine($"NavigateToEndOfCurrentRouteTask, CurrentNavigateToEndOfCurrentRouteState = {CurrentNavigateToEndOfCurrentRouteState}");

            switch (CurrentNavigateToEndOfCurrentRouteState)
            {
                case NavigateToEndOfCurrentRouteState.PICK_NEXT_WAYPOINT:
                    PickNextWaypoint();
                    CurrentNavigateToEndOfCurrentRouteState = NavigateToEndOfCurrentRouteState.FACE_WAYPOINT;
                    Console.WriteLine($"Picked next waypoint: {CurrentWaypoint}");
                    break;
                case NavigateToEndOfCurrentRouteState.FACE_WAYPOINT:
                    await FaceWaypointTask();
                    CurrentNavigateToEndOfCurrentRouteState = NavigateToEndOfCurrentRouteState.WALK_TO_WAYPOINT;
                    break;
                case NavigateToEndOfCurrentRouteState.WALK_TO_WAYPOINT:
                    await WalkToWaypointTask();

                    float targetDistance = Vector2.Distance(WorldState.PlayerLocation, CurrentWaypoint);
                    bool isFinalWaypointInRoute = CurrentWaypointIndex == CurrentRoute.Waypoints.Count - 1;
                    float distanceTolerance = isFinalWaypointInRoute ? WowPlayerConstants.NPC_FINAL_WAYPOINT_TOLERANCE : CurrentRoute.DistanceTolerance;
                    bool arrived = targetDistance <= distanceTolerance;
                    if (arrived)
                    {
                        if (isFinalWaypointInRoute)
                        {
                            Console.WriteLine($"Arrived at {CurrentWaypoint}, last waypoint in route");
                            CurrentNavigateToEndOfCurrentRouteState = NavigateToEndOfCurrentRouteState.ARRIVED_AT_END_OF_ROUTE;
                            await EndWalkForwardTask();
                        }
                        else
                        {
                            Console.WriteLine($"Arrived at {CurrentWaypoint}, distance away {targetDistance}, picking next waypoint");
                            CurrentNavigateToEndOfCurrentRouteState = NavigateToEndOfCurrentRouteState.PICK_NEXT_WAYPOINT;
                        }
                    }

                    break;
                case NavigateToEndOfCurrentRouteState.ARRIVED_AT_END_OF_ROUTE:
                    break;
            }
        }

        public async Task PlayerFindEnemyTargetGoalTask()
        {
            UpdateCurrentFindEnemyTargetState();
            Console.WriteLine($"PlayerFindEnemyTargetGoalTask, CurrentFindEnemyTargetState = {CurrentFindEnemyTargetState}, Walking? {WalkingForward}");

            switch (CurrentFindEnemyTargetState)
            {
                case FindEnemyTargetState.PICK_FARMING_LOCATION_CONFIG:
                    await PickFarmingLocationConfig();
                    CurrentFindEnemyTargetState = FindEnemyTargetState.NAVIGATE_TO_FARMING_LOCATION;
                    break;
                case FindEnemyTargetState.NAVIGATE_TO_FARMING_LOCATION:
                    //PickNextWaypoint();
                    CurrentFindEnemyTargetState = FindEnemyTargetState.PICK_NEXT_WAYPOINT;
                    //Console.WriteLine($"Picked next waypoint: {CurrentWaypoint}");
                    break;
                case FindEnemyTargetState.PICK_NEXT_WAYPOINT:
                    PickNextWaypoint();
                    CurrentFindEnemyTargetState = FindEnemyTargetState.FACE_WAYPOINT;
                    Console.WriteLine($"Picked next waypoint: {CurrentWaypoint}");
                    break;
                case FindEnemyTargetState.FACE_WAYPOINT:
                    await FaceWaypointTask();
                    CurrentFindEnemyTargetState = FindEnemyTargetState.WALK_TO_WAYPOINT;
                    break;
                case FindEnemyTargetState.WALK_TO_WAYPOINT:
                    await TargetEnemyTask();
                    await WalkToWaypointTask();

                    float targetDistance = Vector2.Distance(WorldState.PlayerLocation, CurrentWaypoint);
                    bool arrived = targetDistance <= CurrentRoute.DistanceTolerance;
                    if (arrived)
                    {
                        Console.WriteLine($"Arrived at {CurrentWaypoint}, distance away {targetDistance}, picking next waypoint");
                        CurrentFindEnemyTargetState = FindEnemyTargetState.PICK_NEXT_WAYPOINT;
                    }

                    break;
                case FindEnemyTargetState.WALK_TO_TARGETED_ENEMY:
                    var canStillSeeTargetMarker = await WalkToTargetMarkerTask();
                    if (!canStillSeeTargetMarker)
                    {
                        CurrentFindEnemyTargetState = FindEnemyTargetState.FACE_WAYPOINT;
                    }

                    if (TargetMarkerIsWithinMeleeRangeFromCenter())
                    {
                        CurrentFindEnemyTargetState = FindEnemyTargetState.ENGAGE_TARGETED_ENEMY;
                    }
                    break;
                case FindEnemyTargetState.ENGAGE_TARGETED_ENEMY:
                    // TODO: actually implement this
                    await EndWalkForwardTask();
                    await StartAttackTask();
                    await FaceTargetMarkerTask(refreshTargetMarker: true);
                    break;
            }
        }

        public bool TryGetTargetMarkerDistanceFromCenter(out double distanceFromCenter)
        {
            if (LastFindTargetMarkerPoint == null)
            {
                distanceFromCenter = 0;
                return false;
            }

            double xComp = LastFindTargetMarkerPoint.X - (ScreenConfiguration.Resolution.Width / 2);
            double yComp = LastFindTargetMarkerPoint.Y - (ScreenConfiguration.Resolution.Height / 2);
            distanceFromCenter = Math.Sqrt((xComp * xComp) + (yComp * yComp));
            Console.WriteLine($"xComp {xComp}, yComp {yComp}, distanceFromCenter {distanceFromCenter}");
            return true;
        }

        public bool TargetMarkerIsWithinMeleeRangeFromCenter()
        {
            if(TryGetTargetMarkerDistanceFromCenter(out double distanceFromCenter))
            {
                return distanceFromCenter <= ScreenConfiguration.MeleeRangeTargetMarkerDistance;
            }

            return false;
        }

        public async Task TargetEnemyTask()
        {
            if (GeneralHelpers.CurrentTimeInsideDuration(LastFindTargetTime, WowPlayerConstants.TIME_BETWEEN_FIND_TARGET_MILLIS))
            {
                return;
            }

            LastFindTargetTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            FindTargetCount++;

            if (FarmingLocationConfiguration.TargetFindMethod == WowLocationConfiguration.WaypointTargetFindMethod.TAB)
            {
                await WowInput.PressKey(WowInput.TAB_TARGET);
            }
            else if (FarmingLocationConfiguration.TargetFindMethod == WowLocationConfiguration.WaypointTargetFindMethod.MACRO)
            {
                await WowInput.PressKey(WowInput.FIND_TARGET_MACRO);
            }
            else if (FarmingLocationConfiguration.TargetFindMethod == WowLocationConfiguration.WaypointTargetFindMethod.ALTERNATE)
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
                TryFindTargetMarkerOnScreen() &&
                CanEngageTarget())
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
    }
}
