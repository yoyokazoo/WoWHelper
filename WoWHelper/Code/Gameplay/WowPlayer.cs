using InputManager;
using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using WindowsGameAutomationTools.Slack;
using WoWHelper.Code;
using WoWHelper.Code.WorldState;
using static WoWHelper.Code.WowPlayerStates;

namespace WoWHelper
{
    public partial class WowPlayer
    {
        
        private long lastFindTargetTime = 0;
        // TODO: player needs to be refactored
        public long lastDangerousFindTargetTime { get; set; }

        // TODO: write custom getters/setters for these so we can keep checking the time until they're off cooldown,
        // then use the cached value until they get dirtied again
        public long FarmStartTime { get; private set; }
        public long LastDynamiteTime { get; private set; }
        public long LastHealthPotionTime { get; private set; }

        // TODO: Switch to a system where we use cancellation tokens to exit normal operation and go into "oh shit I got aggroed by something?"
        //private CancellationTokenSource CancellationTokenSource {  get; set; }


        public WowWorldState WorldState { get; private set; }
        public PlayerState CurrentPlayerState { get; private set; }
        public PathfindingState CurrentPathfindingState { get; private set; }

        public int CurrentWaypointIndex { get; private set; }
        public WowWaypointDefinition WaypointDefinition { get; private set; }
        public int WaypointTraversalDirection { get; private set; }

        public bool LogoutTriggered { get; set; }
        public string LogoutReason { get; set; }

        public enum EngagementMethod
        {
            Charge,
            Shoot
        }

        public EngagementMethod EngageMethod { get; private set; }
        

        public WowPlayer()
        {
            WorldState = new WowWorldState();
            CurrentPlayerState = PlayerState.WAITING_TO_FOCUS;
            CurrentPathfindingState = PathfindingState.PICKING_NEXT_WAYPOINT;
            CurrentWaypointIndex = -1;
            WaypointDefinition = WowWaypointConstants.LEVEL_42_TANARIS_TURTLES;
            WaypointTraversalDirection = 1;
            EngageMethod = EngagementMethod.Charge;
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

        public bool CanEngageTarget(WowWorldState worldState)
        {
            if (EngageMethod == EngagementMethod.Charge)
            {
                return worldState.CanChargeTarget;
            }
            else if (EngageMethod == EngagementMethod.Shoot)
            {
                return worldState.CanShootTarget;
            }

            return false;
        }

        public void KickOffCoreLoop()
        {
            KeyPoller.EscPressed += async () => {
                Console.WriteLine("ESC detected!");
                // set cancellation flag or perform cleanup
                Keyboard.KeyPress(WowInput.MOVE_BACK);

                await Task.Delay(0);
                Environment.Exit(0);
            };

            KeyPoller.Start();


            _ = CoreGameplayLoopTask();
            //_ = CorePathfindingLoopTask();
        }

        public async Task<bool> CoreGameplayLoopTask()
        {
            FarmStartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            Console.WriteLine("Kicking off core gameplay loop");
            PlayerState currentPlayerState = PlayerState.WAITING_TO_FOCUS;

            while (currentPlayerState != PlayerState.EXITING_CORE_GAMEPLAY_LOOP)
            {
                //UpdateFromBitmap(bm);

                switch (currentPlayerState)
                {
                    case PlayerState.WAITING_TO_FOCUS:
                        Console.WriteLine("Focusing on window");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(FocusOnWindowTask(),
                            PlayerState.FOCUSED_ON_WINDOW,
                            PlayerState.EXITING_CORE_GAMEPLAY_LOOP);
                        break;
                    case PlayerState.FOCUSED_ON_WINDOW:
                        Console.WriteLine("Focused on window");
                        currentPlayerState = PlayerState.CHECK_FOR_LOGOUT;
                        break;
                    case PlayerState.CHECK_FOR_LOGOUT:
                        Console.WriteLine("Checking if we should log out");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(SetLogoutVariablesTask(),
                            PlayerState.LOGGING_OUT,
                            PlayerState.WAIT_UNTIL_BATTLE_READY);
                        break;
                    case PlayerState.LOGGING_OUT:
                        Console.WriteLine("Logging out");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(LogoutTask(),
                            PlayerState.LOGGED_OUT,
                            PlayerState.IN_CORE_COMBAT_LOOP);
                        break;
                    case PlayerState.LOGGED_OUT:
                        Console.WriteLine("Logged out");
                        SlackHelper.SendMessageToChannel($"Logged out: {LogoutReason}");
                        currentPlayerState = PlayerState.EXITING_CORE_GAMEPLAY_LOOP;
                        break;
                    case PlayerState.WAIT_UNTIL_BATTLE_READY:
                        Console.WriteLine("Waiting until battle ready");
                        currentPlayerState = await ChangeStateBasedOnTaskResult(RecoverAfterFightTask(),
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
                        currentPlayerState = await ChangeStateBasedOnTaskResult(TryToEngageTask(),
                            PlayerState.IN_CORE_COMBAT_LOOP,
                            PlayerState.CHECK_FOR_LOGOUT);
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
                        await Task.Delay(1500);

                        // click again in case they die slow
                        Mouse.PressButton(Mouse.MouseKeys.Right);
                        await Task.Delay(1500);

                        // skin
                        Mouse.PressButton(Mouse.MouseKeys.Right);
                        await Task.Delay(3000);

                        currentPlayerState = PlayerState.CHECK_FOR_LOGOUT;
                        break;
                }
            }

            Console.WriteLine("Exited Core Gameplay");
            //await EQTask.CampTask();
            return true;
        }

        public async Task<bool> CoreCombatLoopTask()
        {
            Console.WriteLine("Kicking off core combat loop");
            WowWorldState previousWorldState = null;
            WowWorldState worldState = null;
            bool thrownDynamite = false;
            bool potionUsed = false;
            bool tooManyAttackersActionsTaken = false;

            await StartOfCombatTask();
            
            do
            {
                await Task.Delay(250);

                previousWorldState = worldState;
                worldState = WowWorldState.GetWoWWorldState();

                // don't drown
                if (worldState.Underwater)
                {
                    await GetOutOfWater();
                }

                // First do our "Make sure we're not standing around doing nothing" checks
                if (await MakeSureWeAreAttackingEnemyTask(worldState, previousWorldState))
                {
                    continue;
                }

                // Next, check if we need to pop any big cooldowns
                if (!tooManyAttackersActionsTaken && await TooManyAttackersTask(worldState))
                {
                    tooManyAttackersActionsTaken = true;
                    continue;
                }

                // Just in case, if for some reason things are going really poorly, try to pop retal regardless
                if (!tooManyAttackersActionsTaken && worldState.PlayerHpPercent <= 35)
                {
                    SlackHelper.SendMessageToChannel($"35% Retal popped, not sure what went wrong!");
                    // cast retaliation
                    Keyboard.KeyPress(WowInput.RETALIATION_KEY);
                    // until I get GCD tracking working, just wait a bit and click it again to make sure
                    await Task.Delay(1500);
                    Keyboard.KeyPress(WowInput.RETALIATION_KEY);
                    tooManyAttackersActionsTaken = true;

                    LogoutReason = "Got down to 35% somehow";
                    LogoutTriggered = true;

                    continue;
                }

                if (!thrownDynamite && await ThrowDynamiteTask(worldState))
                {
                    LastDynamiteTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    thrownDynamite = true;
                    continue;
                }

                if (!potionUsed && await UseHealingPotionTask(worldState))
                {
                    LastHealthPotionTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    potionUsed = true;
                    continue;
                }

                // Finally, if we've made it this far, do standard combat actions
                if (!worldState.BattleShoutActive && worldState.ResourcePercent >= WowGameplayConstants.BATTLE_SHOUT_RAGE_COST)
                {
                    Keyboard.KeyPress(WowInput.BATTLE_SHOUT_KEY);
                }
                else if (worldState.OverpowerUsable && worldState.ResourcePercent >= WowGameplayConstants.OVERPOWER_RAGE_COST)
                {
                    Keyboard.KeyPress(WowInput.OVERPOWER_KEY);
                }
                else if (!worldState.TargetHasRend && worldState.ResourcePercent >= WowGameplayConstants.REND_RAGE_COST)
                {
                    Keyboard.KeyPress(WowInput.REND_KEY);
                }
                else if(worldState.AttackerCount > 1)
                {
                    if (worldState.SweepingStrikesCooledDown && worldState.ResourcePercent < WowGameplayConstants.SWEEPING_STRIKES_RAGE_COST)
                    {
                        // Popping SS ASAP is priority
                        continue;
                    }

                    if (worldState.SweepingStrikesCooledDown && worldState.ResourcePercent >= WowGameplayConstants.SWEEPING_STRIKES_RAGE_COST)
                    {
                        await WowInput.PressKeyWithShift(WowInput.SHIFT_SWEEPING_STRIKES_MACRO);
                    }
                    else if (worldState.WhirlwindCooledDown && worldState.ResourcePercent >= WowGameplayConstants.WHIRLWIND_RAGE_COST)
                    {
                        await WowInput.PressKeyWithShift(WowInput.SHIFT_WHIRLWIND_MACRO);
                        await Task.Delay(150);
                        Keyboard.KeyPress(WowInput.HEROIC_STRIKE_KEY);
                        await Task.Delay(150);
                        Keyboard.KeyPress(WowInput.HEROIC_STRIKE_KEY);
                        await Task.Delay(150);
                        Keyboard.KeyPress(WowInput.HEROIC_STRIKE_KEY);
                        await Task.Delay(150);
                        Keyboard.KeyPress(WowInput.HEROIC_STRIKE_KEY);
                    }
                    else if (!worldState.WhirlwindCooledDown && !worldState.HeroicStrikeQueued && worldState.ResourcePercent >= WowGameplayConstants.CLEAVE_RAGE_COST)
                    {
                        // if WW is cooled down, prefer waiting for rage for that over cleaving
                        await WowInput.PressKeyWithShift(WowInput.SHIFT_CLEAVE_MACRO);
                    }
                }
                else if (worldState.AttackerCount == 1)
                {
                    if (worldState.ResourcePercent >= WowGameplayConstants.MORTAL_STRIKE_RAGE_COST)
                    {
                        Keyboard.KeyPress(WowInput.HEROIC_STRIKE_KEY);
                    }
                }
            } while (worldState.IsInCombat);

            return true;
        }

        public async Task<bool> CorePathfindingLoopTask()
        {
            Console.WriteLine("Kicking off core pathfinding loop");

            WowWorldState previousWorldState = null;
            WowWorldState worldState = null;
            bool stationaryJumpAttemptedOnce = false;
            bool stationaryWiggleAttemptedOnce = false;
            bool stationaryWiggleAttemptedTwice = false;
            bool stationaryAlertSent = false;
            long lastLocationChangeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            await FocusOnWindowTask();

            // Count loops of the waypoints, if we haven't found a target in N loops, error out
            int maxTargetChecks = 1000;
            int targetChecks = 0;
            //bool lookingForDangerousTarget = false;
            while (targetChecks < maxTargetChecks)
            {
                if (!CurrentTimeInsideDuration(lastFindTargetTime, WowPlayerConstants.TIME_BETWEEN_FIND_TARGET_MILLIS))
                {
                    //lookingForDangerousTarget = false;
                    lastFindTargetTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    if (WaypointDefinition.TargetFindMethod == WowWaypointDefinition.WaypointTargetFindMethod.TAB)
                    {
                        Keyboard.KeyPress(WowInput.TAB_TARGET);
                    }
                    else if (WaypointDefinition.TargetFindMethod == WowWaypointDefinition.WaypointTargetFindMethod.MACRO)
                    {
                        Keyboard.KeyPress(WowInput.FIND_TARGET_MACRO);
                    }
                    else if (WaypointDefinition.TargetFindMethod == WowWaypointDefinition.WaypointTargetFindMethod.ALTERNATE)
                    {
                        if (targetChecks % 2 == 0)
                        {
                            Keyboard.KeyPress(WowInput.TAB_TARGET);
                        }
                        else
                        {
                            Keyboard.KeyPress(WowInput.FIND_TARGET_MACRO);
                        }  
                    }

                    targetChecks++;
                }
                // TODO: FDSA
                /*
                else if(!CurrentTimeInsideDuration(lastDangerousFindTargetTime, TIME_BETWEEN_FIND_DANGEROUS_TARGET_MILLIS))
                {
                    lookingForDangerousTarget = true;
                    lastDangerousFindTargetTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    await WoWInput.PressKeyWithShift(WoWInput.SHIFT_DANGEROUS_TARGET_MACRO);

                    // give a little extra time
                    await Task.Delay(250);
                }
                */

                // always wait a bit for the UI to update, then grab it?
                await Task.Delay(250);
                previousWorldState = worldState;
                worldState = WowWorldState.GetWoWWorldState();

                // don't drown
                if (worldState.Underwater)
                {
                    await GetOutOfWater();
                }

                if (worldState.IsInCombat)
                {
                    await EndWalkForwardTask();
                    // return true if we can charge, false if we're already in combat
                    return false;
                }

                /*
                if (lookingForDangerousTarget && worldState.TargetHpPercent > 0)
                {
                    await WoWTasks.EndWalkForwardTask();

                    SlackHelper.SendMessageToChannel($"DANGEROUS MOB TARGETED during CorePathfindingLoopTask, {lookingForDangerousTarget}");
                    LogoutReason = "DANGEROUS MOB TARGETED";
                    LogoutTriggered = true;
                    return true;
                }
                */

                if (CanEngageTarget(worldState))
                {
                    await EndWalkForwardTask();
                    return true;
                }

                // If we haven't moved in a long time, alert
                if (previousWorldState?.MapX != worldState.MapX || previousWorldState?.MapY != worldState.MapY)
                {
                    lastLocationChangeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                }

                

                if (!stationaryJumpAttemptedOnce && !CurrentTimeInsideDuration(lastLocationChangeTime, WowPathfinding.STATIONARY_MILLIS_BEFORE_JUMP))
                {
                    await AvoidObstacleByJumping();
                    stationaryJumpAttemptedOnce = true;
                }

                if (!stationaryWiggleAttemptedOnce && !CurrentTimeInsideDuration(lastLocationChangeTime, WowPathfinding.STATIONARY_MILLIS_BEFORE_WIGGLE))
                {
                    // first wiggle try left
                    await AvoidObstacle(left: true);
                    stationaryWiggleAttemptedOnce = true;
                }

                if (!stationaryWiggleAttemptedTwice && !CurrentTimeInsideDuration(lastLocationChangeTime, WowPathfinding.STATIONARY_MILLIS_BEFORE_SECOND_WIGGLE))
                {
                    // second wiggle try right
                    await AvoidObstacle(left: true);
                    stationaryWiggleAttemptedTwice = true;
                }

                if (!stationaryAlertSent && !CurrentTimeInsideDuration(lastLocationChangeTime, WowPathfinding.STATIONARY_MILLIS_BEFORE_ALERT))
                {
                    //SlackHelper.SendMessageToChannel($"Haven't moved in a long time.  Something wrong?");
                    //stationaryAlertSent = true;
                    LogoutTriggered = true;
                    LogoutReason = "Stuck for a long time, couldn't wiggle out";
                    return true;
                }

                if (CanEngageTarget(worldState) || worldState.IsInCombat)
                {
                    await EndWalkForwardTask();
                    // return true if we can charge/shoot, false if we're already in combat
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

                            // Circular always goes in the same direction, so if you interrupt and restart, you'll still be going the same direction.
                            // For linear let's do our best guess to pick the best direction
                            if (WaypointDefinition.TraversalMethod == WowWaypointDefinition.WaypointTraversalMethod.LINEAR)
                            {
                                if (CurrentWaypointIndex == 0)
                                {
                                    WaypointTraversalDirection = 1;
                                }
                                else if (CurrentWaypointIndex == WaypointDefinition.Waypoints.Count - 1)
                                {
                                    WaypointTraversalDirection = -1;
                                }
                                else
                                {
                                    var forwardDegrees = WowPathfinding.GetDesiredDirectionInDegrees(WaypointDefinition.Waypoints[CurrentWaypointIndex], WaypointDefinition.Waypoints[CurrentWaypointIndex + 1]);
                                    var backwardsDegrees = WowPathfinding.GetDesiredDirectionInDegrees(WaypointDefinition.Waypoints[CurrentWaypointIndex], WaypointDefinition.Waypoints[CurrentWaypointIndex - 1]);
                                    var facingDegrees = worldState.FacingDegrees;
                                    var forwardDiff = WowPathfinding.GetDegreesToMove(facingDegrees, forwardDegrees);
                                    var backwardsDiff = WowPathfinding.GetDegreesToMove(facingDegrees, backwardsDegrees);

                                    if (backwardsDiff < forwardDiff)
                                    {
                                        WaypointTraversalDirection = -1;
                                    }
                                    
                                    Console.WriteLine("Forward/Backwards/Facing/AbsFor/AbsBack");
                                    Console.WriteLine(forwardDegrees);
                                    Console.WriteLine(backwardsDegrees);
                                    Console.WriteLine(facingDegrees);
                                    Console.WriteLine(forwardDiff);
                                    Console.WriteLine(backwardsDiff);
                                    
                                }
                            }
                        }
                        else
                        {
                            // otherwise cycle through them
                            CurrentWaypointIndex += WaypointTraversalDirection;

                            if (CurrentWaypointIndex < 0 || CurrentWaypointIndex >= WaypointDefinition.Waypoints.Count)
                            {
                                if (WaypointDefinition.TraversalMethod == WowWaypointDefinition.WaypointTraversalMethod.CIRCULAR)
                                {
                                    CurrentWaypointIndex = 0;
                                }
                                else if (WaypointDefinition.TraversalMethod == WowWaypointDefinition.WaypointTraversalMethod.LINEAR)
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
                        CurrentPathfindingState = await ChangeStateBasedOnTaskResult(MoveTowardsWaypointTask(worldState),
                            PathfindingState.PICKING_NEXT_WAYPOINT,
                            PathfindingState.MOVING_TOWARDS_WAYPOINT);
                        break;
                }
            }

            SlackHelper.SendMessageToChannel($"Haven't found a target in ~4 minutes.  Something wrong?");
            Console.WriteLine("Exited Pathfinding loop.  Too many loops without a successful target find.");
            LogoutTriggered = true;
            LogoutReason = "4 minutes without finding a target";

            return true;
        }
    }
}
