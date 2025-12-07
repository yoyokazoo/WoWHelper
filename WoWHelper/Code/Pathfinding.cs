using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WoWHelper.Code
{
    public static class Pathfinding
    {
        public static double GetDirectionInDegrees(Vector2 waypoint1, Vector2 waypoint2)
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

            return directionInDegrees;
        }
    }
}
