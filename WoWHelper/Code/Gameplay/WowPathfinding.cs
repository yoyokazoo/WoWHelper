using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WoWHelper.Code.WorldState;

namespace WoWHelper.Code
{
    public static class WowPathfinding
    {
        public const float WAYPOINT_DEGREES_TOLERANCE = 20f;

        public const int MIN_ROTATION_SPEED = 3;
        public const int MAX_ROTATION_SPEED = 10;
        public const float MAX_SPEED_ANGLE = 180f;

        // The further away you are, the less tolerance allowed.  The closer you get to the waypoint, the higher the tolerance
        public const float WAYPOINT_DEGREE_TOLERANCE_MAX_DISTANCE = 10.0f;
        public const float WAYPOINT_DEGREE_TOLERANCE_MIN_DISTANCE = 3.0f;

        public const float WAYPOINT_DEGREE_TOLERANCE_MAX_DEGREES = 15.0f;
        public const float WAYPOINT_DEGREE_TOLERANCE_MIN_DEGREES = 05.0f;

        public const int STATIONARY_MILLIS_BEFORE_JUMP = 4 * 1000;
        public const int STATIONARY_MILLIS_BEFORE_WIGGLE = 8 * 1000;
        public const int STATIONARY_MILLIS_BEFORE_SECOND_WIGGLE = 18 * 1000;
        public const int STATIONARY_MILLIS_BEFORE_ALERT = 30 * 1000;

        public const float STRAFE_LATERAL_DISTANCE_TOLERANCE = 0.04f;

        // Found experimentally, https://docs.google.com/spreadsheets/d/1rKDjzZpp7rHOLpsKg3mYy_qlmD5BkHvBZwc6nGFjMVw/edit?gid=0#gid=0
        public const float PLAYER_MOVE_SPEED = 0.1325f;
        // Found experimentally
        public const float FULL_ROTATION_MILLIS = 2000f;


        public static float GetDesiredDirectionInDegrees(Vector2 waypoint1, Vector2 waypoint2)
        {
            float dx = waypoint2.X - waypoint1.X;
            float dy = waypoint2.Y - waypoint1.Y;

            dy *= 0.666f; // Wow Coordinates are normalized from 0-100, so we need to denormalize them to get the correct vector.  May be zone dependent!

            dy *= -1; // y is down in Wow's Coordinate system

            var directionInRadians = Math.Atan2(dy, dx);
            var directionInDegrees = directionInRadians * 180 / Math.PI;

            //Console.WriteLine();

            // Wow radians start at N instead of E, so account for that
            directionInDegrees += 270;
            while (directionInDegrees > 360)
            {
                directionInDegrees -= 360;
            }

            // for debugging
            directionInRadians += Math.PI * 1.5;

            return (float)directionInDegrees;
        }

        // Signed turn (+ = left, - = right) to face `target`, picking the turning-circle math
        // when walkingForward (see GetDegreesToMoveWhileWalking) or a plain turn-in-place
        // otherwise. Null only when walking and the target is inside the turning circle --
        // callers should stop walking and turn in place instead.
        public static float? GetDegreesToMove(Vector2 player, float facingDegrees, Vector2 target, bool walkingForward)
        {
            if (walkingForward)
            {
                return GetDegreesToMoveWhileWalking(player, facingDegrees, target);
            }

            return GetDegreesToMoveWhileStationary(player, facingDegrees, target);
        }

        public static float GetDegreesToMoveWhileStationary(Vector2 player, float facingDegrees, Vector2 target)
        {
            float desiredDegrees = GetDesiredDirectionInDegrees(player, target);
            return GetDegreesToMoveWhileStationary(facingDegrees, desiredDegrees);
        }

        // Signed difference between two headings, wrapped to +/-180 (+ = left, - = right).
        // Heading-only overload, for callers that don't have a target position.
        public static float GetDegreesToMoveWhileStationary(float currentDegrees, float desiredDegrees)
        {
            float deltaDegrees = desiredDegrees - currentDegrees;

            if (deltaDegrees <= 0 && deltaDegrees >= -180)
            {
                return deltaDegrees;
            }
            else if (deltaDegrees >= 0 && deltaDegrees >= 180)
            {
                return deltaDegrees - 360;
            }
            else if (deltaDegrees <= 0 && deltaDegrees <= -180)
            {
                return deltaDegrees + 360;
            }
            else//if (deltaDegrees >= 0 && deltaDegrees >= 180)
            {
                return deltaDegrees;
            }
        }

        // Returns the signed degrees to turn (GetDegreesToMove convention: + = left, - = right)
        // so that, while walking forward at PLAYER_MOVE_SPEED during the turn, you end up
        // facing `target`. Returns null if the target is inside the turning circle (unreachable).
        // Turn direction follows the stationary turn's sign, but the result is NOT wrapped to
        // +/-180 -- once committed to a direction, the needed turn can exceed 180.
        public static float? GetDegreesToMoveWhileWalking(Vector2 player, float facingDegrees, Vector2 target)
        {
            float naiveDegreesToMove = GetDegreesToMoveWhileStationary(player, facingDegrees, target);

            double omega = 2 * Math.PI / (FULL_ROTATION_MILLIS / 1000.0);   // rad/s
            double r = PLAYER_MOVE_SPEED / omega;

            // Target relative to player, in the same corrected space GetDesiredDirectionInDegrees uses
            double tx = target.X - player.X;
            double ty = -(target.Y - player.Y) * 0.666;

            double phi0 = (facingDegrees + 90) * Math.PI / 180;   // WoW deg -> math angle

            return GetDegreesToTurnAlongTurningCircle(tx, ty, phi0, r, naiveDegreesToMove);
        }

        // Screen-space counterpart to GetDegreesToMoveWhileWalking, for the target marker
        // (camera pitched straight down, so the screen is the player's own top-down frame:
        // player at screen center, screen up = forward). rightPixels/forwardPixels are the
        // marker's offset from screen center (forward = screen up, i.e. -Y), and
        // turningRadiusPixels is the walking turning circle's radius in screen pixels at the
        // current resolution/zoom. Same return convention: signed degrees (+ = left), or
        // null if the marker is inside the turning circle -- stop walking and turn in place.
        public static float? GetDegreesToMoveTowardsScreenOffsetWhileWalking(float rightPixels, float forwardPixels, float turningRadiusPixels)
        {
            float naiveDegreesToMove = (float)(Math.Atan2(-rightPixels, forwardPixels) * 180.0 / Math.PI);

            // Local frame is already a standard math frame (x right, y forward), heading straight up.
            return GetDegreesToTurnAlongTurningCircle(rightPixels, forwardPixels, Math.PI / 2, turningRadiusPixels, naiveDegreesToMove);
        }

        // Shared turning-circle math for the two walking-turn helpers above. (tx, ty) is the
        // target relative to the player in a standard math frame (CCW = left), heading is the
        // player's current heading in that frame (radians), r is the turning circle's radius in
        // the same units as tx/ty, and naiveDegreesToMove is the plain turn-in-place answer,
        // used only to pick the turn direction.
        private static float? GetDegreesToTurnAlongTurningCircle(double tx, double ty, double phi0, double r, float naiveDegreesToMove)
        {
            int s = naiveDegreesToMove >= 0 ? 1 : -1;               // +1 left (CCW), -1 right (CW)

            // Turn-circle center: r along the heading's left normal (or right, for s = -1)
            double cx = s * r * -Math.Sin(phi0);
            double cy = s * r * Math.Cos(phi0);

            double dx = tx - cx, dy = ty - cy;
            double d = Math.Sqrt(dx * dx + dy * dy);
            if (d <= r) return null;

            double cp = Math.Atan2(dy, dx) - s * Math.Acos(r / d);  // tangent point's angle around C
            double phiEnd = cp + s * Math.PI / 2;                   // heading at tangent point

            double turn = s * (phiEnd - phi0);                      // magnitude in turn direction
            turn = ((turn % (2 * Math.PI)) + 2 * Math.PI) % (2 * Math.PI);
            return (float)(s * turn * 180 / Math.PI);
        }

        // Right-click-drag turning rate is resolution-dependent -- confirmed by
        // WowPlayer.MouseTurnRateSweepTask (WowTurnCalibrationTasks.cs) measuring markedly
        // different results on 1920x1080 vs 3440x1440 -- so the ratio/offset live per
        // resolution on WowScreenConfiguration.MouseDragPixelsPerDegree/
        // MouseDragOffsetDegrees/MouseDragMinEffectivePixels rather than as constants here.
        // See those properties for the measured values and per-resolution caveats.

        // Signed drag distance for a signed turn, using GetDegreesToMove's convention
        // (positive = turn left, negative = turn right), so its result can be passed straight
        // in. Returns Mouse.MoveRelative's X convention: positive = drag right, negative =
        // drag left. pixelsPerDegree/offsetDegrees should come from the current
        // WowScreenConfiguration (MouseDragPixelsPerDegree/MouseDragOffsetDegrees) -- offsetDegrees
        // compensates for a resolution whose measured degrees-vs-pixels line doesn't pass through
        // the origin (see WowScreenConfiguration.MouseDragOffsetDegrees), and is 0 for one that
        // does. A zero-degree request always returns a zero drag, regardless of offsetDegrees --
        // there's nothing to compensate for if no turn was asked for. Turns under ~1 degree
        // return a drag the game ignores (see WowScreenConfiguration.MouseDragMinEffectivePixels).
        public static int GetMouseDragPixelsForDegrees(float degreesToMove, float pixelsPerDegree, float offsetDegrees)
        {
            float magnitude = Math.Abs(degreesToMove);
            if (magnitude <= 0f)
            {
                return 0;
            }

            int pixelsMagnitude = (int)Math.Round((magnitude + offsetDegrees) * pixelsPerDegree);
            return degreesToMove > 0 ? -pixelsMagnitude : pixelsMagnitude;
        }

        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static float Lerp(float normalizedValue, float min, float max)
        {
            return (min + (max - min)) * normalizedValue;
        }

        public static float GetWaypointDegreesTolerance(float distance)
        {
            float clampedDistance = Clamp(distance, WAYPOINT_DEGREE_TOLERANCE_MIN_DISTANCE, WAYPOINT_DEGREE_TOLERANCE_MAX_DISTANCE);
            float zeroBasedDistance = clampedDistance - WAYPOINT_DEGREE_TOLERANCE_MIN_DISTANCE;
            float normalizedDistance = zeroBasedDistance / WAYPOINT_DEGREE_TOLERANCE_MAX_DISTANCE;

            float lerpedDegrees = Lerp(normalizedDistance, WAYPOINT_DEGREE_TOLERANCE_MIN_DEGREES, WAYPOINT_DEGREE_TOLERANCE_MAX_DEGREES);
            float inverseDegrees = WAYPOINT_DEGREE_TOLERANCE_MAX_DEGREES - lerpedDegrees;

            //Console.WriteLine($"GetWaypointDegreesTolerance for distance {distance} = {inverseDegrees}");

            return inverseDegrees;
        }

        public static float Cross2D(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
        public static float Dot2D(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;

        public static Vector2 ForwardFromFacingDegrees(float facingDegrees)
        {
            // Convert degrees to radians
            float radians = facingDegrees * (float)Math.PI / 180f;

            // WoW:
            // 0° = North
            // 90° = West
            // 180° = South
            // 270° = East
            //
            // X grows East, Y grows South
            //
            // This mapping satisfies all constraints:
            float x = -(float)Math.Sin(radians);
            float y = -(float)Math.Cos(radians);

            return Vector2.Normalize(new Vector2(x, y));
        }

        public static float GetLateralDistance(float facingDegrees, Vector2 playerPos, Vector2 waypoint)
        {
            Vector2 forward = ForwardFromFacingDegrees(facingDegrees); // must be unit
            Vector2 toWaypoint = waypoint - playerPos;

            return Cross2D(forward, toWaypoint);
        }

        // Nearest-waypoint distance, used to validate the player actually started near the
        // route (see WowPlayerConstants.MAX_DISTANCE_FROM_ROUTE_WAYPOINT) rather than to guide
        // in-progress pathing.
        public static float GetDistanceToClosestWaypoint(Vector2 playerPos, IReadOnlyList<Vector2> waypoints)
        {
            float closestDistance = float.MaxValue;
            foreach (var waypoint in waypoints)
            {
                float distance = Vector2.Distance(playerPos, waypoint);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                }
            }

            return closestDistance;
        }

        // TODO: DynamicRoute class?
        /*
        public static void FindRoute(WowPlayer wowPlayer, Vector2 targetLocation)
        {
            WowRoute startingRoute = FindRouteClosestToPlayer(wowPlayer);
            List<WowRoute> safeRoutes = GetAllSafeRoutes(wowPlayer);


        }
        */

        public static List<Vector2> CreatePartialWaypointsFromRoute(WowRoute route, Vector2 startingWaypoint, Vector2 endingWaypoint)
        {
            if (!route.Waypoints.Contains(startingWaypoint) || !route.Waypoints.Contains(endingWaypoint))
            {
                throw new Exception($"Starting Waypoint ({startingWaypoint}) or Ending Waypoint ({endingWaypoint}) missing from route ({route.Waypoints})");
            }

            List<Vector2> partialWaypoints = new List<Vector2>();
            int startingIndex = route.Waypoints.IndexOf(startingWaypoint);
            int endingIndex = route.Waypoints.IndexOf(endingWaypoint);
            int direction = 1;

            if (route.TraversalMethod == WowRoute.WaypointTraversalMethod.LINEAR)
            {
                if (endingIndex < startingIndex) direction = -1;
                for (int i = startingIndex; i != endingIndex + direction; i += direction)
                {
                    partialWaypoints.Add(route.Waypoints[i]);
                }
                return partialWaypoints;
            }
            else if (route.TraversalMethod == WowRoute.WaypointTraversalMethod.CIRCULAR)
            {
                int forwardDiff = (endingIndex - startingIndex) % route.Waypoints.Count;
                int backwardDiff = (startingIndex - endingIndex) % route.Waypoints.Count;

                if (forwardDiff > backwardDiff) direction = -1;
                for (int i = startingIndex; i != endingIndex + direction; i += direction)
                {
                    i %= route.Waypoints.Count;
                    partialWaypoints.Add(route.Waypoints[i]);
                }
                return partialWaypoints;
            }
            
            throw new NotImplementedException($"Unhandled TraversalMethod {route.TraversalMethod}");
        }

        public static List<Vector2> CreateCustomWaypointsFromRoutes(WowPlayer wowPlayer, Vector2 targetLocation)
        {
            WowRoute startingRoute = FindRouteClosestToPlayer(wowPlayer);
            List<WowRoute> safeRoutes = GetAllSafeRoutes(wowPlayer);

            return new List<Vector2>();
        }

        public static WowRoute FindRouteClosestToPlayer(WowPlayer wowPlayer)
        {
            // TODO: implement
            return WowLocationConfigs.LEVEL_1_DUROTAR_BOARS_AND_SCORPS.Route;
        }

        public static List<WowRoute> GetAllSafeRoutes(WowPlayer wowPlayer)
        {
            List<WowRoute> safeRoutes = new List<WowRoute>();
            safeRoutes.AddRange(WowRoutes.ALL_WALKING_ROUTES);
            safeRoutes.AddRange(WowLocationConfigs.ALL_LOCATIONS.Select(config => config.Route));
            safeRoutes.Where(route => route.MinimumLevel <= 0 || wowPlayer.WorldState.PlayerLevel >= route.MinimumLevel);

            return safeRoutes;
        }

        public static WowNPCConfiguration PickNPCToSellTo()
        {
            // TODO: implement!
            return WowNPCConfigs.VALLEY_OF_TRIALS_MERCHANT;
        }
    }
}
