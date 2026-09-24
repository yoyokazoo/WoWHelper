using System;
using System.Collections.Generic;
using System.Numerics;

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

        public static float GetDegreesToMove(float currentDegrees, float desiredDegrees)
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

        // Right-click-drag turning, measured by WowPlayer.MouseTurnRateSweepTask
        // (WowTurnCalibrationTasks.cs) at 1px steps from 1-363px, then 25px steps from
        // 370-595px: exactly linear through the origin at 41px per 10 degrees (e.g. 41px ->
        // 10.00, 205px -> 50.00, 328px -> 80.00, 595px -> 145.13), zero spread across repeats,
        // identical left vs right. Drags under 4px didn't turn at all. Measured up to ~145
        // degrees; the last stretch to 180 (~738px) is extrapolated. Measured with the
        // in-game Mouse Look Speed at its default of 5.5, on a 3440-wide screen resolution --
        // this constant may depend on either, so re-run the sweep if the setting or the
        // resolution changes (this may need to become per-resolution, e.g. on
        // WowScreenConfiguration, if other resolutions measure differently).
        public const float MOUSE_DRAG_PIXELS_PER_DEGREE = 4.1f;
        public const int MOUSE_DRAG_MIN_EFFECTIVE_PIXELS = 4;

        // Signed drag distance for a signed turn, using GetDegreesToMove's convention
        // (positive = turn left, negative = turn right), so its result can be passed straight
        // in. Returns Mouse.MoveRelative's X convention: positive = drag right, negative =
        // drag left. Turns under ~1 degree return a drag the game ignores (see
        // MOUSE_DRAG_MIN_EFFECTIVE_PIXELS).
        public static int GetMouseDragPixelsForDegrees(float degreesToMove)
        {
            return -(int)Math.Round(degreesToMove * MOUSE_DRAG_PIXELS_PER_DEGREE);
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
    }
}
