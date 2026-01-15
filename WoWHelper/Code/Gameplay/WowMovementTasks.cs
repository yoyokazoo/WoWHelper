using InputManager;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsGameAutomationTools.Slack;
using WoWHelper.Code;
using WoWHelper.Code.WorldState;
using static WoWHelper.Code.WowPlayerStates;

namespace WoWHelper
{
    public partial class WowPlayer
    {
        public async Task<bool> PathfindingLoopTask()
        {
            Console.WriteLine("Kicking off core pathfinding loop");

            bool stationaryJumpAttemptedOnce = false;
            bool stationaryWiggleAttemptedOnce = false;
            bool stationaryWiggleAttemptedTwice = false;
            bool stationaryAlertSent = false;
            long lastLocationChangeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            LastJumpTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            await FocusOnWindowTask();

            // Count loops of the waypoints, if we haven't found a target in N loops, error out
            int maxTargetChecks = 1000;
            int targetChecks = 0;
            //bool lookingForDangerousTarget = false;
            while (targetChecks < maxTargetChecks)
            {
                await UpdateWorldStateAsync();

                await EveryWorldStateUpdateTasks();

                if (!CurrentTimeInsideDuration(LastFindTargetTime, WowPlayerConstants.TIME_BETWEEN_FIND_TARGET_MILLIS))
                {
                    LastFindTargetTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    if (FarmingConfig.LocationConfiguration.TargetFindMethod == WowLocationConfiguration.WaypointTargetFindMethod.TAB)
                    {
                        Keyboard.KeyPress(WowInput.TAB_TARGET);
                    }
                    else if (FarmingConfig.LocationConfiguration.TargetFindMethod == WowLocationConfiguration.WaypointTargetFindMethod.MACRO)
                    {
                        Keyboard.KeyPress(WowInput.FIND_TARGET_MACRO);
                    }
                    else if (FarmingConfig.LocationConfiguration.TargetFindMethod == WowLocationConfiguration.WaypointTargetFindMethod.ALTERNATE)
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

                if (!CurrentTimeInsideDuration(LastJumpTime, WowPlayerConstants.TIME_BETWEEN_JUMPS_MILLIS))
                {
                    LastJumpTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    Keyboard.KeyPress(WowInput.JUMP);
                }

                if (WorldState.IsInCombat)
                {
                    await EndWalkForwardTask();
                    Keyboard.KeyPress(WowInput.CLEAR_TARGET_MACRO); // we may have an errant target that's not attacking us

                    return false;
                }

                if (CanEngageTarget())
                {
                    await EndWalkForwardTask();
                    return true;
                }

                // If we haven't moved in a long time, alert
                if (PreviousWorldState?.MapX != WorldState.MapX || PreviousWorldState?.MapY != WorldState.MapY)
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
                    await AvoidObstacle(left: false);
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

                if (CanEngageTarget() || WorldState.IsInCombat)
                {
                    await EndWalkForwardTask();
                    // return true if we can charge/shoot, false if we're already in combat
                    return !WorldState.IsInCombat;
                }

                switch (CurrentPathfindingState)
                {
                    case PathfindingState.PICKING_NEXT_WAYPOINT:
                        Console.WriteLine($"Picking next waypoint");
                        if (CurrentWaypointIndex == -1)
                        {
                            // we've never picked a waypoint yet, so find the closest one
                            Vector2 playerLocation = new Vector2(WorldState.MapX, WorldState.MapY);
                            CurrentWaypointIndex = FarmingConfig.LocationConfiguration.Waypoints
                                .Select((p, i) => (dist: Vector2.Distance(playerLocation, p), index: i))
                                .OrderBy(t => t.dist)
                                .First()
                                .index;

                            // Circular always goes in the same direction, so if you interrupt and restart, you'll still be going the same direction.
                            // For linear let's do our best guess to pick the best direction
                            if (FarmingConfig.LocationConfiguration.TraversalMethod == WowLocationConfiguration.WaypointTraversalMethod.LINEAR)
                            {
                                if (CurrentWaypointIndex == 0)
                                {
                                    WaypointTraversalDirection = 1;
                                }
                                else if (CurrentWaypointIndex == FarmingConfig.LocationConfiguration.Waypoints.Count - 1)
                                {
                                    WaypointTraversalDirection = -1;
                                }
                                else
                                {
                                    var forwardDegrees = WowPathfinding.GetDesiredDirectionInDegrees(FarmingConfig.LocationConfiguration.Waypoints[CurrentWaypointIndex], FarmingConfig.LocationConfiguration.Waypoints[CurrentWaypointIndex + 1]);
                                    var backwardsDegrees = WowPathfinding.GetDesiredDirectionInDegrees(FarmingConfig.LocationConfiguration.Waypoints[CurrentWaypointIndex], FarmingConfig.LocationConfiguration.Waypoints[CurrentWaypointIndex - 1]);
                                    var facingDegrees = WorldState.FacingDegrees;
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

                            if (CurrentWaypointIndex < 0 || CurrentWaypointIndex >= FarmingConfig.LocationConfiguration.Waypoints.Count)
                            {
                                if (FarmingConfig.LocationConfiguration.TraversalMethod == WowLocationConfiguration.WaypointTraversalMethod.CIRCULAR)
                                {
                                    CurrentWaypointIndex = 0;
                                }
                                else if (FarmingConfig.LocationConfiguration.TraversalMethod == WowLocationConfiguration.WaypointTraversalMethod.LINEAR)
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
                        CurrentPathfindingState = await ChangeStateBasedOnTaskResult(MoveTowardsWaypointTask(),
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

        // Returns true if we've reached the waypoint
        // Returns false if we haven't yet reached the waypoint
        // Rotates towards the waypoint or walks towards the waypoint, depending
        public async Task<bool> MoveTowardsWaypointTask()
        {
            var waypoint = FarmingConfig.LocationConfiguration.Waypoints[CurrentWaypointIndex];
            float waypointDistance = Vector2.Distance(WorldState.PlayerLocation, waypoint);
            float desiredDegrees = WowPathfinding.GetDesiredDirectionInDegrees(WorldState.PlayerLocation, waypoint);
            float degreesDifference = WowPathfinding.GetDegreesToMove(WorldState.FacingDegrees, desiredDegrees);

            Console.WriteLine($"Heading towards waypoint {waypoint}. At {WorldState.MapX},{WorldState.MapY}.  DesiredDegrees: {desiredDegrees}, facing degrees: {WorldState.FacingDegrees}.  DegreesDifference: {degreesDifference}");

            if (waypointDistance <= FarmingConfig.LocationConfiguration.DistanceTolerance)
            {
                Console.WriteLine($"Arrived at {waypoint} ({WorldState.MapX},{WorldState.MapY})");
                await EndWalkForwardTask();
                return true;
            }

            if (Math.Abs(degreesDifference) > WowPathfinding.GetWaypointDegreesTolerance(waypointDistance))
            {
                Console.WriteLine($"degreesDifference too large, rotating to heading");
                //await EndWalkForwardTask();
                await RotateToDirectionTask(desiredDegrees, waypointDistance);
                return false;
            }
            else
            {
                // if we're already walking, ignore this
                Console.WriteLine($"Start walking forward");
                await StartWalkForwardTask();

                var lateralDistance = WowPathfinding.GetLateralDistance(WorldState.FacingDegrees, WorldState.PlayerLocation, waypoint);
                if (Math.Abs(lateralDistance) > WowPathfinding.STRAFE_LATERAL_DISTANCE_TOLERANCE)
                {
                    if (lateralDistance > 0)
                    {
                        Keyboard.KeyDown(WowInput.STRAFE_RIGHT);
                    }
                    else
                    {
                        Keyboard.KeyDown(WowInput.STRAFE_LEFT);
                    }
                }
                else
                {
                    Keyboard.KeyUp(WowInput.STRAFE_LEFT);
                    Keyboard.KeyUp(WowInput.STRAFE_RIGHT);
                }

                return false;
            }
        }

        public async Task<bool> RotateToDirectionTask(float desiredDegrees, float distance)
        {
            await Task.Delay(0);
            try
            {
                while (true)
                {
                    UpdateWorldState();

                    float currentDegrees = WorldState.FacingDegrees;
                    float degreesToMove = WowPathfinding.GetDegreesToMove(currentDegrees, desiredDegrees);
                    float absDegreesToMove = Math.Abs(degreesToMove);

                    //Console.WriteLine($"Desired Degrees: {desiredDegrees} Facing Degrees: {worldState.FacingDegrees} Degrees to Move: {degreesToMove}");

                    if (absDegreesToMove <= WowPathfinding.GetWaypointDegreesTolerance(distance))
                        break;

                    Keys directionKey = degreesToMove <= 0 ? WowInput.TURN_RIGHT : WowInput.TURN_LEFT;

                    if (directionKey == WowInput.TURN_RIGHT)
                    {
                        Keyboard.KeyUp(WowInput.TURN_LEFT);
                    }
                    else
                    {
                        Keyboard.KeyUp(WowInput.TURN_RIGHT);
                    }

                    Keyboard.KeyDown(directionKey);
                }
            }
            finally
            {
                Keyboard.KeyUp(WowInput.TURN_LEFT);
                Keyboard.KeyUp(WowInput.TURN_RIGHT);
            }

            return true;
        }

        public async Task<bool> StartWalkForwardTask()
        {
            await Task.Delay(0);
            Keyboard.KeyDown(WowInput.MOVE_FORWARD);
            return true;
        }

        public async Task<bool> EndWalkForwardTask()
        {
            await Task.Delay(0);
            Keyboard.KeyUp(WowInput.MOVE_FORWARD);
            Keyboard.KeyUp(WowInput.STRAFE_LEFT);
            Keyboard.KeyUp(WowInput.STRAFE_RIGHT);
            return true;
        }

        public async Task<bool> ScootForwardsTask()
        {
            Keyboard.KeyDown(WowInput.MOVE_FORWARD);
            await Task.Delay(100);
            Keyboard.KeyUp(WowInput.MOVE_FORWARD);
            return true;
        }

        public async Task<bool> ScootBackwardsTask()
        {
            Keyboard.KeyDown(WowInput.MOVE_BACK);
            await Task.Delay(1000);
            Keyboard.KeyUp(WowInput.MOVE_BACK);
            return true;
        }

        public async Task<bool> StartOfCombatWiggle()
        {
            // move back a bit to fix camera direction
            Keyboard.KeyDown(WowInput.MOVE_BACK);
            await Task.Delay(400);
            Keyboard.KeyUp(WowInput.MOVE_BACK);

            // scoot forward a tiny bit to get back in range
            Keyboard.KeyDown(WowInput.MOVE_FORWARD);
            await Task.Delay(20);
            Keyboard.KeyUp(WowInput.MOVE_FORWARD);

            return true;
        }

        public async Task<bool> TurnABitToTheLeftTask()
        {
            Keyboard.KeyDown(WowInput.TURN_LEFT);
            await Task.Delay(500);
            Keyboard.KeyUp(WowInput.TURN_LEFT);

            return true;
        }

        public async Task<bool> GetOutOfWater()
        {
            // Holding jump ascends
            Keyboard.KeyDown(WowInput.JUMP);
            await Task.Delay(1000);
            Keyboard.KeyUp(WowInput.JUMP);

            return true;
        }

        public async Task<bool> AvoidObstacle(bool left)
        {
            // stop walking forward
            await EndWalkForwardTask();

            // back off obstruction
            Keyboard.KeyDown(WowInput.MOVE_BACK);
            await Task.Delay(1000);
            Keyboard.KeyUp(WowInput.MOVE_BACK);

            // strafe
            var strafeKey = left ? WowInput.STRAFE_LEFT : WowInput.STRAFE_RIGHT;
            Keyboard.KeyDown(strafeKey);
            await Task.Delay(2000);
            Keyboard.KeyUp(strafeKey);

            return true;
        }

        public async Task<bool> AvoidObstacleByJumping()
        {
            Keyboard.KeyPress(WowInput.JUMP);
            await Task.Delay(1000);
            Keyboard.KeyPress(WowInput.JUMP);

            return true;
        }
    }
}
