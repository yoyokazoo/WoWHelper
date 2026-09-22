using InputManager;
using System;
using System.Drawing;
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
        private const float MEANINGFUL_LOCATION_CHANGE_AMOUNT = 0.15f;

        public async Task<bool> PathfindingLoopTask()
        {
            Console.WriteLine("Kicking off core pathfinding loop");

            bool stationaryJumpAttemptedOnce = false;
            bool stationaryWiggleAttemptedOnce = false;
            bool stationaryWiggleAttemptedTwice = false;
            bool stationaryAlertSent = false;
            long lastLocationChangeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            long lastTargetMarkerScanTime = 0;
            LastJumpTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            // Anchor point for stuck detection -- see the MEANINGFUL_LOCATION_CHANGE_AMOUNT
            // check below.
            float stuckX = WorldState.MapX;
            float stuckY = WorldState.MapY;

            await FocusOnWindowTask();

            // Count loops of the waypoints, if we haven't found a target in N loops, error out
            int maxTargetChecks = 1000;
            int targetChecks = 0;
            //bool lookingForDangerousTarget = false;
            while (targetChecks < maxTargetChecks)
            {
                await UpdateWorldStateAsync();

                await EveryWorldStateUpdateTasks();

                // Suppressed for a bit after an engage attempt bailed out due to
                // WorldState.TargetUnreachable (see AbandonUnreachableEngageTarget in
                // WowCommonCombatTasks.cs) -- otherwise we'd immediately TAB/macro right back
                // onto the same unreachable target we just cleared. Keep walking the route
                // during the suppression window instead of standing still trying to retarget.
                bool suppressedAfterLineOfSightBailout = CurrentTimeInsideDuration(
                    LastLineOfSightBailoutTime, WowPlayerConstants.LINE_OF_SIGHT_RETARGET_SUPPRESS_MILLIS);

                if (!IsOnMerchantRun && !suppressedAfterLineOfSightBailout && !CurrentTimeInsideDuration(LastFindTargetTime, WowPlayerConstants.TIME_BETWEEN_FIND_TARGET_MILLIS))
                {
                    LastFindTargetTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    if (FarmingConfig.LocationConfiguration.TargetFindMethod == WowLocationConfiguration.WaypointTargetFindMethod.TAB)
                    {
                        await WowInput.PressKey(WowInput.TAB_TARGET);
                    }
                    else if (FarmingConfig.LocationConfiguration.TargetFindMethod == WowLocationConfiguration.WaypointTargetFindMethod.MACRO)
                    {
                        await WowInput.PressKey(WowInput.FIND_TARGET_MACRO);
                    }
                    else if (FarmingConfig.LocationConfiguration.TargetFindMethod == WowLocationConfiguration.WaypointTargetFindMethod.ALTERNATE)
                    {
                        if (targetChecks % 2 == 0)
                        {
                            await WowInput.PressKey(WowInput.TAB_TARGET);
                        }
                        else
                        {
                            await WowInput.PressKey(WowInput.FIND_TARGET_MACRO);
                        }
                    }

                    targetChecks++;
                }

                if (!IsOnMerchantRun && !CurrentTimeInsideDuration(LastJumpTime, WowPlayerConstants.TIME_BETWEEN_JUMPS_MILLIS))
                {
                    LastJumpTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    await WowInput.PressKey(WowInput.JUMP);
                }

                if (WorldState.IsInCombat)
                {
                    await EndWalkForwardTask();
                    Console.WriteLine($"Entered combat during pathfinding, clearing target");
                    await WowInput.PressKey(WowInput.CLEAR_TARGET_MACRO); // we may have an errant target that's not attacking us

                    return false;
                }

                // Skipped during a merchant run -- a residual/leftover target becoming
                // engageable shouldn't hijack a sell trip into combat prep. Actual combat
                // (WorldState.IsInCombat, checked above) still interrupts it normally.
                if (!IsOnMerchantRun && CanEngageTarget())
                {
                    await EndWalkForwardTask();
                    return true;
                }

                // If we haven't moved a MEANINGFUL amount away from where we last confirmed
                // progress, keep treating ourselves as stationary. Comparing MapX/MapY
                // tick-to-tick for ANY change (the old behavior) reset the stuck clock on
                // every trivial jitter -- a jump or wiggle nudging us by a fraction of a unit
                // was enough to make us think we'd recovered, so the escalation below
                // (jump -> wiggle -> wiggle -> give up) kept getting reset before it ever had
                // enough uninterrupted stationary time to reach its next step. Requiring a
                // real MEANINGFUL_LOCATION_CHANGE_AMOUNT of total distance from the anchor
                // point means small back-and-forth wiggling while stuck doesn't count as
                // having escaped.
                // Standing still during these two merchant-run phases is intentional (talking
                // to the vendor / waiting for the addon's auto-sell to finish, not stuck on
                // terrain) -- keep resetting the stuck anchor/clock instead of letting the
                // escalation below fire (which would jump/back-off/strafe away from the vendor
                // mid-interaction) or letting it go stale so WALKING_BACK_TO_ROUTE immediately
                // thinks it's been stuck for however long the interaction took.
                bool merchantRunIsStationaryByDesign = IsOnMerchantRun &&
                    (CurrentMerchantRunPhase == MerchantRunPhase.INTERACTING_WITH_MERCHANT ||
                     CurrentMerchantRunPhase == MerchantRunPhase.WAITING_FOR_AUTO_SELL);

                if (merchantRunIsStationaryByDesign)
                {
                    stuckX = WorldState.MapX;
                    stuckY = WorldState.MapY;
                    lastLocationChangeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    stationaryJumpAttemptedOnce = false;
                    stationaryWiggleAttemptedOnce = false;
                    stationaryWiggleAttemptedTwice = false;
                    stationaryAlertSent = false;
                }
                else
                {
                    float distanceFromStuckAnchor = Vector2.Distance(new Vector2(WorldState.MapX, WorldState.MapY), new Vector2(stuckX, stuckY));
                    if (distanceFromStuckAnchor >= MEANINGFUL_LOCATION_CHANGE_AMOUNT)
                    {
                        stuckX = WorldState.MapX;
                        stuckY = WorldState.MapY;
                        lastLocationChangeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                        // We made real progress -- if we get stuck again later in this same
                        // PathfindingLoopTask call (a different obstacle further along the
                        // route), give it the full jump -> wiggle -> wiggle -> alert escalation
                        // again instead of leaving every step permanently "already attempted"
                        // from this episode.
                        stationaryJumpAttemptedOnce = false;
                        stationaryWiggleAttemptedOnce = false;
                        stationaryWiggleAttemptedTwice = false;
                        stationaryAlertSent = false;
                    }

                    if (!stationaryJumpAttemptedOnce && !CurrentTimeInsideDuration(lastLocationChangeTime, WowPathfinding.STATIONARY_MILLIS_BEFORE_JUMP))
                    {
                        //Console.WriteLine($"Haven't moved in a while, stuck at {PreviousWorldState.MapX},{PreviousWorldState.MapY} headed to {FarmingConfig.LocationConfiguration.Waypoints[CurrentWaypointIndex].X},{FarmingConfig.LocationConfiguration.Waypoints[CurrentWaypointIndex].Y}");
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
                        await EndWalkForwardTask();
                        return true;
                    }
                }

                if ((!IsOnMerchantRun && CanEngageTarget()) || WorldState.IsInCombat || LogoutTriggered)
                {
                    await EndWalkForwardTask();
                    // return true if we can charge/shoot, false if we're already in combat
                    return !WorldState.IsInCombat;
                }

                // A merchant run in progress hijacks the rest of this loop body -- no
                // target-finding/out-of-range-chase/waypoint-route logic below applies while
                // walking to/from the vendor. Placed after the stuck-detection block above (not
                // right after the combat check) so a merchant run stuck on terrain -- a fence,
                // same as normal route-walking can get stuck on -- gets the same
                // jump/wiggle/wiggle/give-up escalation instead of spinning in place forever.
                // See MerchantRunStepTask for the phase state machine;
                // IsOnMerchantRun/CurrentMerchantRunPhase/CurrentMerchantWaypointIndex are plain
                // WowPlayer fields, so the combat check above already gives this the same
                // "return false, resume later" interruption behavior as normal pathfinding, with
                // no extra plumbing needed.
                if (IsOnMerchantRun)
                {
                    await MerchantRunStepTask();
                    continue;
                }

                // We've got a target (its marker is on screen) but CanEngageTarget() just said
                // no -- most likely it's simply out of range of whatever our pull ability is.
                // Rather than keep walking the waypoint route and hope it wanders closer, face
                // it and walk towards it for a bit. Throttled to a full-screen capture every
                // PATHFINDING_TARGET_MARKER_SCAN_INTERVAL_MILLIS since FindTargetMarkerOnScreen
                // is far more expensive than the pixel-row read WorldState does each tick.
                // Skipped entirely (scan included) on routes that opt out via
                // ChaseOutOfRangeTargets -- see that property's comment for why -- and on
                // water routes (IsWaterZone): with the camera pitched forward there, the
                // marker's vertical position is depth/distance, not in-front-vs-behind, so
                // "walk straight at it" has no idea whether it's swimming towards the target
                // or away from it.
                if (FarmingConfig.LocationConfiguration.ChaseOutOfRangeTargets &&
                    !FarmingConfig.IsWaterZone &&
                    !CurrentTimeInsideDuration(lastTargetMarkerScanTime, PATHFINDING_TARGET_MARKER_SCAN_INTERVAL_MILLIS))
                {
                    lastTargetMarkerScanTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    if (FindTargetMarkerOnScreen() != null)
                    {
                        await WalkTowardsTargetMarkerTask();

                        // Same early exit as above -- WalkTowardsTargetMarkerTask stops on any of
                        // these, but it's the outer loop's job to actually return on them.
                        if (CanEngageTarget() || WorldState.IsInCombat || LogoutTriggered)
                        {
                            await EndWalkForwardTask();
                            return !WorldState.IsInCombat;
                        }

                        // Facing/position have changed out from under the waypoint logic; go
                        // back to the top so it re-evaluates against a fresh WorldState rather
                        // than acting on the one from before we walked.
                        continue;
                    }
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
                        bool arrivedAtWaypoint = await MoveTowardsWaypointTask();
                        if (arrivedAtWaypoint)
                        {
                            // Branch off to sell if bags are full and we just arrived at the
                            // shared point between this route and its MerchantConfig. Matched
                            // by position (not waypoint index), since which index in
                            // Waypoints reaches that point -- and from which direction --
                            // doesn't matter. See WowMerchantConfiguration.
                            var merchant = FarmingConfig.LocationConfiguration.MerchantConfig;
                            if (!IsOnMerchantRun && merchant != null && WorldState.BagsAreFull &&
                                Vector2.Distance(FarmingConfig.LocationConfiguration.Waypoints[CurrentWaypointIndex], merchant.Waypoints[0])
                                    <= WowPlayerConstants.MERCHANT_BRANCH_POINT_EPSILON)
                            {
                                Console.WriteLine("Bags full at merchant branch point, starting merchant run");
                                IsOnMerchantRun = true;
                                CurrentMerchantRunPhase = MerchantRunPhase.WALKING_TO_MERCHANT;
                                CurrentMerchantWaypointIndex = 1; // index 0 is where we're already standing
                            }

                            CurrentPathfindingState = PathfindingState.PICKING_NEXT_WAYPOINT;
                        }
                        else
                        {
                            CurrentPathfindingState = PathfindingState.MOVING_TOWARDS_WAYPOINT;
                        }
                        break;
                }
            }

            SlackHelper.SendMessageToChannel($"Haven't found a target in ~4 minutes.  Something wrong?");
            Console.WriteLine("Exited Pathfinding loop.  Too many loops without a successful target find.");
            LogoutTriggered = true;
            LogoutReason = "4 minutes without finding a target";
            await EndWalkForwardTask();

            return true;
        }

        // Returns true if we've reached the waypoint
        // Returns false if we haven't yet reached the waypoint
        // Rotates towards the waypoint or walks towards the waypoint, depending
        public async Task<bool> MoveTowardsWaypointTask()
        {
            return await MoveTowardsPointTask(
                FarmingConfig.LocationConfiguration.Waypoints[CurrentWaypointIndex],
                FarmingConfig.LocationConfiguration.DistanceTolerance);
        }

        // Same rotate-or-strafe-and-walk-or-arrive logic MoveTowardsWaypointTask uses, against
        // an arbitrary point/tolerance instead of always reading the route's own
        // Waypoints[CurrentWaypointIndex]/DistanceTolerance -- so MerchantRunStepTask can reuse
        // it for the merchant-run legs, which walk a completely different waypoint list.
        public async Task<bool> MoveTowardsPointTask(Vector2 target, float tolerance)
        {
            float targetDistance = Vector2.Distance(WorldState.PlayerLocation, target);
            float desiredDegrees = WowPathfinding.GetDesiredDirectionInDegrees(WorldState.PlayerLocation, target);
            float degreesDifference = WowPathfinding.GetDegreesToMove(WorldState.FacingDegrees, desiredDegrees);

            Console.WriteLine($"Heading towards {target}. At {WorldState.MapX},{WorldState.MapY}.  DesiredDegrees: {desiredDegrees}, facing degrees: {WorldState.FacingDegrees}.  DegreesDifference: {degreesDifference}");

            if (targetDistance <= tolerance)
            {
                Console.WriteLine($"Arrived at {target} ({WorldState.MapX},{WorldState.MapY})");
                await EndWalkForwardTask();
                return true;
            }

            // A tolerance-aware heading requirement was tried here first and still oscillated:
            // the bearing to a target this close swings wildly for tiny positional noise, so
            // repeatedly re-aiming the whole body at it (RotateToDirectionTask, which halts
            // forward motion) just reproduced the same spin-and-circle failure. Instead, a
            // precision approach only rotates at all once heading error exceeds a much larger
            // fixed threshold (PRECISION_APPROACH_ROTATION_TRIGGER_DEGREES) -- once roughly
            // pointed at the target, it leans on strafing (below) to do the real aiming, since
            // strafing doesn't touch facing and so can't feed back into a spin. Every real
            // route's own DistanceTolerance stays on the unchanged distance-only formula.
            bool isPrecisionApproach = tolerance <= WowPathfinding.PRECISION_APPROACH_TOLERANCE_THRESHOLD;
            float headingTolerance = isPrecisionApproach
                ? WowPathfinding.PRECISION_APPROACH_ROTATION_TRIGGER_DEGREES
                : WowPathfinding.GetWaypointDegreesTolerance(targetDistance);

            if (Math.Abs(degreesDifference) > headingTolerance)
            {
                Console.WriteLine($"degreesDifference too large, rotating to heading");

                // A precision approach stops forward movement before rotating -- turning while
                // still walking traces an arc that can carry the character past the target
                // instead of pivoting cleanly on top of it. Normal route-following keeps its
                // existing behavior of turning without breaking stride.
                if (isPrecisionApproach)
                {
                    await EndWalkForwardTask();
                    await RotateToDirectionTask(desiredDegrees, targetDistance, WowPathfinding.PRECISION_APPROACH_ROTATION_TRIGGER_DEGREES);
                }
                else
                {
                    await RotateToDirectionTask(desiredDegrees, targetDistance);
                }

                return false;
            }

            // if we're already walking, ignore this
            Console.WriteLine($"Start walking forward");
            await StartWalkForwardTask();

            var lateralDistance = WowPathfinding.GetLateralDistance(WorldState.FacingDegrees, WorldState.PlayerLocation, target);

            // A precision approach uses a tighter strafe tolerance -- the default (0.04) is
            // already looser than a target like WowPlayerConstants.MERCHANT_FINAL_WAYPOINT_TOLERANCE
            // (0.02), and strafing is the primary fine-aiming mechanism here (heading is only
            // held roughly on target, see above), not just a minor nudge alongside rotation.
            float strafeTolerance = isPrecisionApproach
                ? WowPathfinding.PRECISION_APPROACH_STRAFE_TOLERANCE
                : WowPathfinding.STRAFE_LATERAL_DISTANCE_TOLERANCE;

            if (Math.Abs(lateralDistance) > strafeTolerance)
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

        // Drives one tick of an in-progress merchant run (WowPlayer.IsOnMerchantRun) -- called
        // from PathfindingLoopTask's loop instead of the normal target-finding/waypoint logic.
        // See WowPlayerStates.MerchantRunPhase and WowMerchantConfiguration.
        public async Task MerchantRunStepTask()
        {
            var merchant = FarmingConfig.LocationConfiguration.MerchantConfig;

            switch (CurrentMerchantRunPhase)
            {
                case MerchantRunPhase.WALKING_TO_MERCHANT:
                    bool isFinalLeg = CurrentMerchantWaypointIndex == merchant.Waypoints.Count - 1;
                    float approachTolerance = isFinalLeg
                        ? WowPlayerConstants.MERCHANT_FINAL_WAYPOINT_TOLERANCE
                        : WowPlayerConstants.MERCHANT_INTERMEDIATE_WAYPOINT_TOLERANCE;

                    if (await MoveTowardsPointTask(merchant.Waypoints[CurrentMerchantWaypointIndex], approachTolerance))
                    {
                        if (isFinalLeg)
                        {
                            CurrentMerchantRunPhase = MerchantRunPhase.INTERACTING_WITH_MERCHANT;
                        }
                        else
                        {
                            CurrentMerchantWaypointIndex++;
                        }
                    }
                    break;

                case MerchantRunPhase.INTERACTING_WITH_MERCHANT:
                    await EndWalkForwardTask();
                    await WowInput.PressKeyWithControl(WowInput.CTRL_TARGET_MERCHANT);
                    await Task.Delay(300); // let the client register the target before clicking
                    Mouse.Move(FarmingConfig.ScreenConfiguration.Resolution.Width/2, FarmingConfig.ScreenConfiguration.Resolution.Height/2);
                    Mouse.PressButton(Mouse.MouseKeys.Right);
                    CurrentMerchantRunPhase = MerchantRunPhase.WAITING_FOR_AUTO_SELL;
                    break;

                case MerchantRunPhase.WAITING_FOR_AUTO_SELL:
                    // Reuses the existing interruptible-wait helper (WowCommonCombatTasks.cs) --
                    // if combat starts mid-wait this returns early, but we still move on to
                    // walking back either way: if the vendor window got interrupted before
                    // selling finished, bags stay full and the branch-off check in
                    // PathfindingLoopTask will simply retry on the next lap.
                    await WaitUnlessInCombatTask(WowPlayerConstants.MERCHANT_AUTO_SELL_WAIT_MILLIS);
                    CurrentMerchantWaypointIndex = Math.Max(merchant.Waypoints.Count - 2, 0);
                    CurrentMerchantRunPhase = MerchantRunPhase.WALKING_BACK_TO_ROUTE;
                    break;

                case MerchantRunPhase.WALKING_BACK_TO_ROUTE:
                    if (await MoveTowardsPointTask(merchant.Waypoints[CurrentMerchantWaypointIndex], WowPlayerConstants.MERCHANT_INTERMEDIATE_WAYPOINT_TOLERANCE))
                    {
                        if (CurrentMerchantWaypointIndex == 0)
                        {
                            IsOnMerchantRun = false;
                            CurrentMerchantRunPhase = MerchantRunPhase.WALKING_TO_MERCHANT; // reset for next trip
                        }
                        else
                        {
                            CurrentMerchantWaypointIndex--;
                        }
                    }
                    break;
            }
        }

        // headingToleranceDegreesOverride, when given, is used directly as the break threshold
        // instead of the distance-based WowPathfinding.GetWaypointDegreesTolerance curve -- used
        // by MoveTowardsPointTask's precision-approach branch so this stops rotating at the same
        // fixed PRECISION_APPROACH_ROTATION_TRIGGER_DEGREES it decided to rotate at, instead of
        // the (much tighter at short range) normal-route formula immediately re-triggering
        // another rotate next tick.
        public async Task<bool> RotateToDirectionTask(float desiredDegrees, float distance, float? headingToleranceDegreesOverride = null)
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

                    float headingTolerance = headingToleranceDegreesOverride ?? WowPathfinding.GetWaypointDegreesTolerance(distance);
                    if (absDegreesToMove <= headingTolerance)
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

        // Measured empirically: how long holding a single turn key takes to spin the
        // character a full 360 degrees. TurnToFaceTargetMarkerTask below uses this to convert
        // a computed bearing directly into a turn-key hold duration.
        private const float FULL_ROTATION_MILLIS = 2000f;

        // How wide a facing cone counts as "roughly facing the target" -- matches the
        // frontal-cone requirement Classic already enforces for a targeted spell cast (see
        // the "You are facing the wrong way!"/TargetNeedsToBeInFront red-error-text handling
        // elsewhere in this codebase). TurnToFaceTargetMarkerTask's final verification passes
        // once within half this, i.e. +/-15 degrees of dead-ahead.
        private const float TARGET_FACING_CONE_DEGREES = 30f;

        // Signed bearing in degrees from the player's own screen position to a target marker
        // position (see WowPlayer.FindTargetMarkerOnScreen / UIFunctions.lua's target-marker
        // section): 0 = dead ahead, positive = turn right by that many degrees, negative =
        // turn left. There is no addon-legal way to read a target's actual position/bearing
        // in this client (UnitPosition, C_Map.GetPlayerMapPosition, and even nameplate frame
        // measurement are all blocked), so this is derived purely from the marker's on-screen
        // position relative to screen center: since the player is run with the camera pitched
        // straight down, screen "up" (Y above center) is forward, and the vector from screen
        // center to the marker IS the bearing, via atan2 -- X left/right of center becomes
        // left/right of facing, Y above/below center becomes in-front-of/behind. Pure function
        // of an already-known marker position -- takes a Point rather than scanning itself, so
        // callers that already have a fresh marker (e.g. WalkIntoMeleeRangeTask, mid-loop)
        // don't need a second full-screen capture just to also get a bearing out of it.
        private float GetBearingDegreesFromMarkerPosition(Point markerPosition)
        {
            var resolution = FarmingConfig.ScreenConfiguration.Resolution;
            float dx = markerPosition.X - (resolution.Width / 2f);
            float dy = markerPosition.Y - (resolution.Height / 2f);

            // atan2(dx, -dy): 0 degrees when dx=0 and the marker is above center (straight
            // ahead); increases toward +90 as the marker moves right of center, toward +/-180
            // as it approaches directly behind, and toward -90 as it moves left of center.
            return (float)(Math.Atan2(dx, -dy) * 180.0 / Math.PI);
        }

        // Scans for the target marker and returns its bearing (see
        // GetBearingDegreesFromMarkerPosition), or null if the marker isn't currently visible
        // on screen.
        private float? GetTargetMarkerBearingDegrees()
        {
            var marker = FindTargetMarkerOnScreen();
            return marker == null ? (float?)null : GetBearingDegreesFromMarkerPosition(marker.Value);
        }

        // Turns to (roughly) face the current target: scans once for the target marker,
        // calculates the bearing and the turn-key hold duration that bearing corresponds to
        // (via FULL_ROTATION_MILLIS), does that one turn, then a final verification scan.
        // Returns true only if that verification lands within TARGET_FACING_CONE_DEGREES/2 of
        // dead-ahead -- false otherwise (marker not visible at all, or still outside the cone
        // after the turn, e.g. the target moved during it), leaving retries to the caller
        // (ShamanFaceCorrectDirectionToEngageTask's own retry loop).
        //
        // Not yet tuned against live testing -- see the "Approach ranged/caster mobs" plan.
        //
        // On water routes (FarmingConfig.IsWaterZone) none of the bearing math above holds,
        // so this hands off to TurnToFaceTargetMarkerInWaterTask instead -- see the
        // water-zone section below.
        public async Task<bool> TurnToFaceTargetMarkerTask()
        {
            if (FarmingConfig.IsWaterZone)
            {
                return await TurnToFaceTargetMarkerInWaterTask();
            }

            float? bearing = GetTargetMarkerBearingDegrees();
            if (bearing == null)
            {
                Console.WriteLine("DEBUG TurnToFaceTargetMarkerTask: no marker found, can't turn -- returning false");
                return false;
            }

            int turnMillis = (int)((Math.Abs(bearing.Value) / 360f) * FULL_ROTATION_MILLIS);
            Keys turnKey = bearing.Value > 0 ? WowInput.TURN_RIGHT : WowInput.TURN_LEFT;

            Console.WriteLine($"DEBUG TurnToFaceTargetMarkerTask: bearing {bearing.Value:0.0} degrees -> holding {turnKey} for {turnMillis}ms");

            Keyboard.KeyDown(turnKey);
            await Task.Delay(turnMillis);
            Keyboard.KeyUp(turnKey);

            float? verifyBearing = GetTargetMarkerBearingDegrees();
            bool success = verifyBearing != null && Math.Abs(verifyBearing.Value) <= TARGET_FACING_CONE_DEGREES / 2f;
            Console.WriteLine(verifyBearing == null
                ? "DEBUG TurnToFaceTargetMarkerTask: post-turn verification found no marker -- returning false"
                : $"DEBUG TurnToFaceTargetMarkerTask: post-turn bearing {verifyBearing.Value:0.0} degrees (cone +/-{TARGET_FACING_CONE_DEGREES / 2f:0.0}) -> success={success}");

            return success;
        }

        // ---- Water-zone facing (WowLocationConfiguration.IsWaterZone) ----
        //
        // Everything above assumes the camera is pitched straight down -- that's what makes
        // the marker's screen position a top-down map of bearing. That isn't an option while
        // swimming: "walk forward" swims in the direction the camera points, so straight down
        // would swim the character straight to the bottom. Water routes run with the camera
        // pitched (mostly) forward instead, which changes what the marker's position means:
        //   - Only the HORIZONTAL offset from screen center still says anything about facing
        //     (left of center = turn left, right = turn right). The vertical offset is now
        //     distance/depth -- a marker below center is a close target, not one behind us.
        //   - Anything behind us, or too far off to the side, is simply off screen. So "no
        //     marker" no longer means "no target"; it can just mean we're pointed the wrong
        //     way, hence the sweep in TurnToFaceTargetMarkerInWaterTask.
        //   - The pixel-offset -> degrees mapping is a perspective projection whose scale
        //     depends on the camera FOV and pitch, neither of which we can read (and the
        //     pitch drifts every time the camera gets bumped), so converting an offset into a
        //     single timed turn-key hold the way TurnToFaceTargetMarkerTask does isn't
        //     reliable here. The water version turns in small steps, re-scanning after each,
        //     until the marker sits inside the tolerance band -- guess and check, not one
        //     computed turn.
        // None of the constants below are tuned against live testing yet.

        // Half-width of the "facing" band, as a fraction of screen HEIGHT (not width): WoW
        // keeps the vertical FOV fixed and widens the horizontal one with aspect ratio, so a
        // given angle off dead-ahead covers the same fraction of screen height on any
        // resolution, but a different fraction of width on an ultrawide vs 16:9. 0.3 * 1440
        // = +/-432px at 3440x1440, very roughly +/-20 degrees. Deliberately wider than
        // TARGET_FACING_CONE_DEGREES' +/-15: Classic's frontal-cone check for casts/charge is
        // more forgiving than that, and every step tighter costs another turn+scan round trip.
        private const float WATER_FACING_TOLERANCE_FRACTION_OF_HEIGHT = 0.3f;

        // Turn-step sizing for the guess-and-check loop: each step holds the turn key for the
        // marker's horizontal offset (as a fraction of half the screen height) times the
        // scale, clamped to the min/max -- so a marker out near the screen edge gets a big
        // step and one just outside the band a small nudge. The scale is a rough "half a
        // screen-height of offset is worth about this much hold" guess; overshoot is bounded
        // by the max and self-corrects on the next step anyway.
        private const float WATER_TURN_STEP_SCALE_MILLIS = 200f;
        private const int WATER_TURN_STEP_MIN_MILLIS = 40;
        private const int WATER_TURN_STEP_MAX_MILLIS = 200;

        // Cap on turn+scan steps per call before giving up (returning false, like the
        // on-foot version does when its one verification scan misses the cone).
        private const int WATER_TURN_MAX_STEPS = 8;

        // Pause between releasing the turn key and re-scanning, so the capture reflects where
        // the turn actually ended rather than a frame from mid-turn.
        private const int WATER_TURN_SETTLE_MILLIS = 100;

        // When the marker isn't on screen at all in a water zone: turn right this long, scan,
        // repeat, up to one full rotation (FULL_ROTATION_MILLIS / this = 8 steps). ~45 degrees
        // per step -- coarse enough to get all the way around quickly, fine enough that the
        // target can't slip past between scans, since the camera's horizontal FOV is well
        // over 45 degrees.
        private const int WATER_SWEEP_STEP_MILLIS = 250;

        // Signed horizontal offset of the marker from screen center, in pixels: negative =
        // left of center, positive = right.
        private float GetMarkerHorizontalOffsetPixels(Point markerPosition)
        {
            return markerPosition.X - (FarmingConfig.ScreenConfiguration.Resolution.Width / 2f);
        }

        private float WaterFacingTolerancePixels =>
            FarmingConfig.ScreenConfiguration.Resolution.Height * WATER_FACING_TOLERANCE_FRACTION_OF_HEIGHT;

        // Mode-aware "are we facing the marker" check, so callers that already have a fresh
        // marker position (WalkIntoMeleeRangeTask) don't need to know which camera setup is
        // in use: on foot it's the +/-TARGET_FACING_CONE_DEGREES/2 bearing cone, in water
        // it's the horizontal band above.
        private bool IsFacingTargetMarker(Point markerPosition)
        {
            if (FarmingConfig.IsWaterZone)
            {
                return Math.Abs(GetMarkerHorizontalOffsetPixels(markerPosition)) <= WaterFacingTolerancePixels;
            }

            return Math.Abs(GetBearingDegreesFromMarkerPosition(markerPosition)) <= TARGET_FACING_CONE_DEGREES / 2f;
        }

        // Holds a turn key for the given duration, then waits WATER_TURN_SETTLE_MILLIS so the
        // next scan sees where the turn actually ended.
        private async Task TurnStepTask(Keys turnKey, int millis)
        {
            Keyboard.KeyDown(turnKey);
            await Task.Delay(millis);
            Keyboard.KeyUp(turnKey);
            await Task.Delay(WATER_TURN_SETTLE_MILLIS);
        }

        // Water-zone counterpart to TurnToFaceTargetMarkerTask (which dispatches here when
        // FarmingConfig.IsWaterZone). Two phases:
        //   1. Sweep: if the marker isn't on screen, turn right in WATER_SWEEP_STEP_MILLIS
        //      chunks, scanning after each, until it shows up -- at most one full rotation.
        //      Still no marker after that means there's genuinely nothing to face -> false.
        //   2. Align: turn towards the marker in proportional steps (see the WATER_TURN_STEP_*
        //      constants), re-scanning after each, until its horizontal offset is inside the
        //      WATER_FACING_TOLERANCE band -> true. Losing the marker mid-align, or running
        //      out of steps, -> false, leaving retries to the caller like the on-foot version.
        public async Task<bool> TurnToFaceTargetMarkerInWaterTask()
        {
            Point? marker = FindTargetMarkerOnScreen();

            if (marker == null)
            {
                int maxSweepSteps = (int)Math.Ceiling(FULL_ROTATION_MILLIS / WATER_SWEEP_STEP_MILLIS);
                Console.WriteLine($"DEBUG TurnToFaceTargetMarkerInWaterTask: no marker on screen, sweeping right in {WATER_SWEEP_STEP_MILLIS}ms steps (up to {maxSweepSteps})");

                for (int sweepStep = 1; sweepStep <= maxSweepSteps && marker == null; sweepStep++)
                {
                    await TurnStepTask(WowInput.TURN_RIGHT, WATER_SWEEP_STEP_MILLIS);
                    marker = FindTargetMarkerOnScreen();
                    Console.WriteLine($"DEBUG TurnToFaceTargetMarkerInWaterTask: sweep step {sweepStep}/{maxSweepSteps} -> marker={(marker == null ? "none" : marker.Value.ToString())}");
                }

                if (marker == null)
                {
                    Console.WriteLine("DEBUG TurnToFaceTargetMarkerInWaterTask: full sweep found no marker -- returning false");
                    return false;
                }
            }

            float halfHeight = FarmingConfig.ScreenConfiguration.Resolution.Height / 2f;

            for (int step = 1; step <= WATER_TURN_MAX_STEPS; step++)
            {
                float offset = GetMarkerHorizontalOffsetPixels(marker.Value);

                if (Math.Abs(offset) <= WaterFacingTolerancePixels)
                {
                    Console.WriteLine($"DEBUG TurnToFaceTargetMarkerInWaterTask: marker={marker.Value} horizontal offset {offset:0}px within +/-{WaterFacingTolerancePixels:0}px after {step - 1} step(s) -> success");
                    return true;
                }

                int turnMillis = (int)Math.Min(WATER_TURN_STEP_MAX_MILLIS,
                    Math.Max(WATER_TURN_STEP_MIN_MILLIS, (Math.Abs(offset) / halfHeight) * WATER_TURN_STEP_SCALE_MILLIS));
                Keys turnKey = offset > 0 ? WowInput.TURN_RIGHT : WowInput.TURN_LEFT;

                Console.WriteLine($"DEBUG TurnToFaceTargetMarkerInWaterTask: [{step}/{WATER_TURN_MAX_STEPS}] marker={marker.Value} horizontal offset {offset:0}px (band +/-{WaterFacingTolerancePixels:0}px) -> holding {turnKey} for {turnMillis}ms");

                await TurnStepTask(turnKey, turnMillis);

                marker = FindTargetMarkerOnScreen();
                if (marker == null)
                {
                    // Off screen after a turn towards it? Most likely it moved (or the step
                    // overshot by far more than expected). Don't sweep again from here -- the
                    // caller's retry will, if it calls back in.
                    Console.WriteLine("DEBUG TurnToFaceTargetMarkerInWaterTask: lost the marker mid-align -- returning false");
                    return false;
                }
            }

            Console.WriteLine($"DEBUG TurnToFaceTargetMarkerInWaterTask: still outside the band after {WATER_TURN_MAX_STEPS} steps -- returning false");
            return false;
        }

        // How often WalkIntoMeleeRangeTask re-scans for the target marker while walking --
        // a full-screen capture (FindTargetMarkerOnScreen), so deliberately slower than
        // WorldState's own throttled pixel-row capture cadence.
        private const int TARGET_MARKER_SCAN_INTERVAL_MILLIS = 200;

        // How many consecutive scans are allowed to come back empty (marker briefly occluded
        // by terrain/other mobs, or a frame UIFunctions.lua hasn't repainted yet) before
        // WalkIntoMeleeRangeTask gives up, rather than aborting on the very first miss.
        private const int MAX_CONSECUTIVE_MARKER_MISSES = 1;

        // Overall safety cap so a target that's unreachable (stuck on terrain, behind an
        // obstacle we can't see around) doesn't walk forever -- same class of guard as
        // PathfindingLoopTask's maxTargetChecks above.
        private const int WALK_INTO_MELEE_RANGE_TIMEOUT_MILLIS = 15000;

        // Walks straight forward until WorldState.IsInCombat says we've engaged (starting
        // auto-attack is the only reliable "we made it" signal now -- the addon's
        // CheckInteractDistance-based IsInMeleeRange was tried and removed, it didn't
        // reliably reflect actual melee range). Caller must already be facing the target on
        // entry (see TurnToFaceTargetMarkerTask). No per-resolution calibration is needed here
        // (contrast the old, now-removed WowScreenConfiguration.DistanceFromTarget, which
        // needed a measured TargetMarkerNearY/FarY pixel pair per resolution):
        // GetBearingDegreesFromMarkerPosition already gives us the line from screen center to
        // the marker, and that's all we need -- |bearing| <= 90 means the marker is above
        // center (in front of us, camera pitched straight down), |bearing| > 90 means it's
        // below center (behind us). Its shrinking magnitude as we close the gap is a "getting
        // closer" signal (logged for now, not gated on).
        //
        // Since every loop iteration already re-scans for the marker anyway (for the bearing),
        // that same scan doubles as two drift checks, cheapest/coarsest first:
        //   1. Did the marker just flip from in-front to behind since the last scan? At close
        //      range the marker's angular speed around us is huge for a small step forward, so
        //      a single 200ms scan gap can jump straight over the fine cone check below without
        //      ever registering as "drifted" by it. When that happens we've walked past/into
        //      the target -- stop walking, re-face it dead-on, then resume, rather than
        //      continuing to walk forward on a heading that's now backwards.
        //   2. Otherwise, has it drifted outside the finer TARGET_FACING_CONE_DEGREES cone?
        //      If so, re-turn via TurnToFaceTargetMarkerTask (without stopping the walk --
        //      this is normal gradual drift, not an overshoot) before continuing.
        // Returns false (and stops walking) if the marker is lost for too long, or if we time
        // out without ever entering combat.
        //
        // On water routes (FarmingConfig.IsWaterZone -- see the water-zone section above)
        // check 1 is skipped entirely, since with the camera pitched forward the marker's
        // vertical position means distance, not in-front-vs-behind; check 2 goes through the
        // mode-aware IsFacingTargetMarker, so it uses the wider horizontal band there.
        public async Task<bool> WalkIntoMeleeRangeTask(WowClassState classState)
        {
            int consecutiveMisses = 0;
            int iteration = 0;
            long deadline = DateTimeOffset.Now.ToUnixTimeMilliseconds() + WALK_INTO_MELEE_RANGE_TIMEOUT_MILLIS;
            bool? wasInFrontOfPlayer = null;

            Console.WriteLine($"DEBUG WalkIntoMeleeRangeTask: starting, timeout {WALK_INTO_MELEE_RANGE_TIMEOUT_MILLIS}ms");

            await StartWalkForwardTask();

            try
            {
                while (DateTimeOffset.Now.ToUnixTimeMilliseconds() < deadline)
                {
                    iteration++;
                    await UpdateWorldStateAsync();

                    if (WorldState.IsInCombat)
                    {
                        Console.WriteLine($"DEBUG WalkIntoMeleeRangeTask: [{iteration}] WorldState.IsInCombat -> success");
                        return true;
                    }

                    if (ClassState is WowWarriorClassState warr && !warr.CanChargeTarget)
                    {
                        Console.WriteLine($"DEBUG WalkIntoMeleeRangeTask: [{iteration}] !ClassState.CanChargeTarget -> success");
                        return false;
                    }

                    var marker = FindTargetMarkerOnScreen();
                    if (marker == null)
                    {
                        consecutiveMisses++;
                        Console.WriteLine($"DEBUG WalkIntoMeleeRangeTask: [{iteration}] no marker this scan (consecutiveMisses={consecutiveMisses}/{MAX_CONSECUTIVE_MARKER_MISSES})");
                        if (consecutiveMisses >= MAX_CONSECUTIVE_MARKER_MISSES)
                        {
                            Console.WriteLine("DEBUG WalkIntoMeleeRangeTask: lost the target marker, giving up");
                            return false;
                        }
                    }
                    else
                    {
                        consecutiveMisses = 0;
                        MostRecentTargetMarkerX = marker.Value.X;
                        MostRecentTargetMarkerY = marker.Value.Y;

                        float bearing = GetBearingDegreesFromMarkerPosition(marker.Value);
                        float verticalOffset = marker.Value.Y - (FarmingConfig.ScreenConfiguration.Resolution.Height / 2f);
                        bool isInFrontOfPlayer = Math.Abs(bearing) <= 90f;
                        Console.WriteLine($"DEBUG WalkIntoMeleeRangeTask: [{iteration}] marker={marker.Value} bearing={bearing:0.0} deg (cone +/-{TARGET_FACING_CONE_DEGREES / 2f:0.0}) verticalOffset={verticalOffset:0.0}px (shrinking magnitude = closer) inFront={isInFrontOfPlayer} waterZone={FarmingConfig.IsWaterZone}");

                        if (!FarmingConfig.IsWaterZone && wasInFrontOfPlayer == true && !isInFrontOfPlayer)
                        {
                            Console.WriteLine($"DEBUG WalkIntoMeleeRangeTask: [{iteration}] target went from in front to behind, stopping to re-face");
                            await EndWalkForwardTask();
                            await TurnToFaceTargetMarkerTask();
                            await StartWalkForwardTask();
                            wasInFrontOfPlayer = null;
                            await Task.Delay(TARGET_MARKER_SCAN_INTERVAL_MILLIS);
                            continue;
                        }

                        wasInFrontOfPlayer = isInFrontOfPlayer;

                        if (!IsFacingTargetMarker(marker.Value))
                        {
                            // Target's drifted out of the fine facing cone but not all the way
                            // behind us -- ordinary gradual drift, so just re-turn without
                            // interrupting the walk.
                            Console.WriteLine($"DEBUG WalkIntoMeleeRangeTask: [{iteration}] target drifted out of front (bearing {bearing:0.0} degrees), re-facing");
                            await TurnToFaceTargetMarkerTask();
                            continue;
                        }
                    }

                    await Task.Delay(TARGET_MARKER_SCAN_INTERVAL_MILLIS);
                }
            }
            finally
            {
                await EndWalkForwardTask();
            }

            Console.WriteLine("DEBUG WalkIntoMeleeRangeTask: timed out before getting close enough");
            return false;
        }

        // How often PathfindingLoopTask checks whether the target marker is on screen while
        // walking the waypoint route. Much slower than TARGET_MARKER_SCAN_INTERVAL_MILLIS
        // above: that one runs while we're already committed to closing on a target, this
        // one runs on every ordinary pathfinding tick where there usually isn't a target at
        // all, and each scan is a full-screen capture.
        private const int PATHFINDING_TARGET_MARKER_SCAN_INTERVAL_MILLIS = 2000;

        // Longest WalkTowardsTargetMarkerTask keeps walking before handing control back to
        // PathfindingLoopTask. Deliberately short -- this isn't WalkIntoMeleeRangeTask's
        // "commit until we're in combat" walk, just a nudge to get an out-of-range target
        // into range. If that isn't enough, the next periodic scan will try again.
        private const int WALK_TOWARDS_TARGET_MARKER_MAX_MILLIS = 4000;

        // PathfindingLoopTask's "we have a target on screen but can't engage it yet" step:
        // stop route-walking, turn to face the target marker, then walk straight at it for up
        // to WALK_TOWARDS_TARGET_MARKER_MAX_MILLIS, or until any of PathfindingLoopTask's own
        // exit conditions (CanEngageTarget / IsInCombat / LogoutTriggered) comes true --
        // whichever is first. Doesn't return on those itself; the caller re-checks them and
        // decides what to return. Always releases the movement keys before returning.
        public async Task<bool> WalkTowardsTargetMarkerTask()
        {
            // Stop the route walk (and any strafing) so the turn below is clean.
            await EndWalkForwardTask();

            if (!await TurnToFaceTargetMarkerTask())
            {
                // Marker vanished, or we're still not facing it after the turn -- walking
                // forward now would just be walking in some random direction.
                Console.WriteLine("DEBUG WalkTowardsTargetMarkerTask: couldn't face the target marker, not walking");
                return false;
            }

            long deadline = DateTimeOffset.Now.ToUnixTimeMilliseconds() + WALK_TOWARDS_TARGET_MARKER_MAX_MILLIS;
            Console.WriteLine($"DEBUG WalkTowardsTargetMarkerTask: facing target, walking towards it for up to {WALK_TOWARDS_TARGET_MARKER_MAX_MILLIS}ms");

            await StartWalkForwardTask();

            try
            {
                while (DateTimeOffset.Now.ToUnixTimeMilliseconds() < deadline)
                {
                    await UpdateWorldStateAsync();
                    await EveryWorldStateUpdateTasks();

                    if (CanEngageTarget() || WorldState.IsInCombat || LogoutTriggered)
                    {
                        Console.WriteLine($"DEBUG WalkTowardsTargetMarkerTask: stopping early (CanEngageTarget={CanEngageTarget()}, IsInCombat={WorldState.IsInCombat}, LogoutTriggered={LogoutTriggered})");
                        return true;
                    }
                }
            }
            finally
            {
                await EndWalkForwardTask();
            }

            Console.WriteLine("DEBUG WalkTowardsTargetMarkerTask: hit the time limit, handing back to pathfinding");
            return false;
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
            await WaitUnlessInCombatTask(3000);
            Keyboard.KeyUp(strafeKey);

            return true;
        }

        public async Task<bool> AvoidObstacleByJumping()
        {
            await WowInput.PressKey(WowInput.JUMP);
            await Task.Delay(1000);
            await WowInput.PressKey(WowInput.JUMP);

            return true;
        }

        public async Task<bool> KeyUpMovementKeys()
        {
            Keyboard.KeyUp(WowInput.MOVE_FORWARD);
            Keyboard.KeyUp(WowInput.MOVE_BACK);
            Keyboard.KeyUp(WowInput.TURN_LEFT);
            Keyboard.KeyUp(WowInput.TURN_RIGHT);
            Keyboard.KeyUp(WowInput.JUMP);
            Keyboard.KeyUp(WowInput.STRAFE_LEFT);
            Keyboard.KeyUp(WowInput.STRAFE_RIGHT);
            Keyboard.KeyUp(WowInput.LatestShiftKey);
            Keyboard.KeyUp(Keys.LShiftKey);
            await Task.Delay(0);

            return true;
        }
    }
}
