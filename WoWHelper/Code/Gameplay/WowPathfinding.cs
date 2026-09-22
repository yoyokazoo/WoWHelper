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

        // Below this arrival tolerance, MoveTowardsPointTask switches from the normal
        // rotate-to-heading behavior to the strafe-led precision approach below (see
        // PRECISION_APPROACH_ROTATION_TRIGGER_DEGREES/PRECISION_APPROACH_STRAFE_TOLERANCE). Set
        // below every real WowLocationConfiguration.DistanceTolerance in use (currently >= 0.1)
        // so ordinary route-following is completely unaffected -- only very tight targets (e.g.
        // WowPlayerConstants.MERCHANT_FINAL_WAYPOINT_TOLERANCE) opt into it.
        public const float PRECISION_APPROACH_TOLERANCE_THRESHOLD = 0.05f;

        // A tolerance/distance-driven heading requirement (tried first) still oscillated near a
        // very tight target: the bearing to a point this close swings wildly for even small
        // positional noise, so repeatedly re-aiming the whole body at it via RotateToDirectionTask
        // (which halts forward motion) produced the same spin-and-circle failure it was meant to
        // fix. Instead, a precision approach only bothers rotating at all once heading error
        // exceeds this much larger, fixed threshold -- once roughly pointed at the target, it
        // just keeps walking and lets the strafe-based lateral correction below do the real
        // aiming, since strafing doesn't touch facing and can't feed back into a spin.
        // Deliberately comfortably under WoW's forward/strafe speed ratio (strafing is slower
        // than walking forward), so the diagonal a full-speed strafe-plus-walk can actually
        // produce is always wide enough to correct whatever heading error this allows through.
        public const float PRECISION_APPROACH_ROTATION_TRIGGER_DEGREES = 20.0f;

        // Strafe-lateral-offset tolerance used instead of STRAFE_LATERAL_DISTANCE_TOLERANCE
        // during a precision approach -- tighter, since that default (0.04) is already looser
        // than WowPlayerConstants.MERCHANT_FINAL_WAYPOINT_TOLERANCE (0.02) and strafing is the
        // primary fine-aiming mechanism here, not just a minor nudge alongside rotation.
        public const float PRECISION_APPROACH_STRAFE_TOLERANCE = 0.01f;


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
