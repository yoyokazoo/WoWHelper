using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WoWHelper.Code
{
    public static class WoWPathfinding
    {
        public const float WAYPOINT_DEGREES_TOLERANCE = 20f;

        public const int MIN_ROTATION_SPEED = 3;
        public const int MAX_ROTATION_SPEED = 10;
        public const float MAX_SPEED_ANGLE = 180f;

        // The further away you are, the less tolerance allowed.  The closer you get to the waypoint, the higher the tolerance
        public const float WAYPOINT_DEGREE_TOLERANCE_MAX_DISTANCE = 10.0f;
        public const float WAYPOINT_DEGREE_TOLERANCE_MIN_DISTANCE = 3.0f;

        public const float WAYPOINT_DEGREE_TOLERANCE_MAX_DEGREES = 25.0f;
        public const float WAYPOINT_DEGREE_TOLERANCE_MIN_DEGREES = 10.0f;

        public const int STATIONARY_MILLIS_BEFORE_WIGGLE = 5 * 1000;
        public const int STATIONARY_MILLIS_BEFORE_SECOND_WIGGLE = 15 * 1000;
        public const int STATIONARY_MILLIS_BEFORE_ALERT = 30 * 1000;


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
    }
}
