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

                if (!suppressedAfterLineOfSightBailout && !CurrentTimeInsideDuration(LastFindTargetTime, WowPlayerConstants.TIME_BETWEEN_FIND_TARGET_MILLIS))
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

                if (!CurrentTimeInsideDuration(LastJumpTime, WowPlayerConstants.TIME_BETWEEN_JUMPS_MILLIS))
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

                if (CanEngageTarget())
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

                if (CanEngageTarget() || WorldState.IsInCombat || LogoutTriggered)
                {
                    await EndWalkForwardTask();
                    // return true if we can charge/shoot, false if we're already in combat
                    return !WorldState.IsInCombat;
                }

                // We've got a target (its marker is on screen) but CanEngageTarget() just said
                // no -- most likely it's simply out of range of whatever our pull ability is.
                // Rather than keep walking the waypoint route and hope it wanders closer, face
                // it and walk towards it for a bit. Throttled to a full-screen capture every
                // PATHFINDING_TARGET_MARKER_SCAN_INTERVAL_MILLIS since FindTargetMarkerOnScreen
                // is far more expensive than the pixel-row read WorldState does each tick.
                // Skipped entirely (scan included) on routes that opt out via
                // ChaseOutOfRangeTargets -- see that property's comment for why.
                if (FarmingConfig.LocationConfiguration.ChaseOutOfRangeTargets &&
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
            await EndWalkForwardTask();

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

        // Measured empirically: how long holding a single turn key takes to spin the
        // character a full 360 degrees. Shared by RotateToDirectionTask below and
        // TurnToFaceTargetMarkerTask further down, both of which convert a computed
        // degrees-to-turn directly into a turn-key hold duration rather than polling
        // WorldState in a tight hold-and-recheck loop -- the addon's pixel-row update
        // cadence lags real turning, so a loop that re-reads FacingDegrees every
        // iteration and corrects on the fly tends to overshoot/oscillate on the stale
        // reads. A single calculated hold, verified once afterward, sidesteps that.
        private const float FULL_ROTATION_MILLIS = 2000f;

        // Turns to face desiredDegrees: reads the current facing once, computes the
        // signed degrees-to-turn and the turn-key hold duration that corresponds to
        // (via FULL_ROTATION_MILLIS), does that one hold, then re-reads WorldState to
        // verify. Returns true only if the post-turn facing landed within
        // GetWaypointDegreesTolerance(distance) of desiredDegrees -- false otherwise
        // (e.g. movement during the turn, or a stale/late WorldState read), leaving any
        // retry to the caller: MoveTowardsWaypointTask calls this again on its next
        // iteration with a freshly-computed desiredDegrees/distance, so a single
        // imperfect turn self-corrects on the following pass rather than needing an
        // internal retry loop here.
        public async Task<bool> RotateToDirectionTask(float desiredDegrees, float distance)
        {
            UpdateWorldState();

            float currentDegrees = WorldState.FacingDegrees;
            float degreesToMove = WowPathfinding.GetDegreesToMove(currentDegrees, desiredDegrees);
            float absDegreesToMove = Math.Abs(degreesToMove);
            float tolerance = WowPathfinding.GetWaypointDegreesTolerance(distance);

            if (absDegreesToMove <= tolerance)
            {
                return true;
            }

            Keys directionKey = degreesToMove <= 0 ? WowInput.TURN_RIGHT : WowInput.TURN_LEFT;
            int turnMillis = (int)((absDegreesToMove / 360f) * FULL_ROTATION_MILLIS);

            Console.WriteLine($"DEBUG RotateToDirectionTask: currentDegrees {currentDegrees:0.0}, desiredDegrees {desiredDegrees:0.0}, degreesToMove {degreesToMove:0.0} -> holding {directionKey} for {turnMillis}ms");

            try
            {
                Keyboard.KeyDown(directionKey);
                await Task.Delay(turnMillis);
            }
            finally
            {
                Keyboard.KeyUp(WowInput.TURN_LEFT);
                Keyboard.KeyUp(WowInput.TURN_RIGHT);
            }

            UpdateWorldState();

            float verifyDegreesToMove = WowPathfinding.GetDegreesToMove(WorldState.FacingDegrees, desiredDegrees);
            bool success = Math.Abs(verifyDegreesToMove) <= tolerance;

            Console.WriteLine($"DEBUG RotateToDirectionTask: post-turn facing {WorldState.FacingDegrees:0.0}, remaining degreesToMove {verifyDegreesToMove:0.0} (tolerance {tolerance:0.0}) -> success={success}");

            return success;
        }

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
        public async Task<bool> TurnToFaceTargetMarkerTask()
        {
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
                        Console.WriteLine($"DEBUG WalkIntoMeleeRangeTask: [{iteration}] marker={marker.Value} bearing={bearing:0.0} deg (cone +/-{TARGET_FACING_CONE_DEGREES / 2f:0.0}) verticalOffset={verticalOffset:0.0}px (shrinking magnitude = closer) inFront={isInFrontOfPlayer}");

                        if (wasInFrontOfPlayer == true && !isInFrontOfPlayer)
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

                        if (Math.Abs(bearing) > TARGET_FACING_CONE_DEGREES / 2f)
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
