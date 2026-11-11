using SlackAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
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
        // route (see WowConstants.MAX_DISTANCE_FROM_ROUTE_WAYPOINT) rather than to guide
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

        public static WowRoute GetRouteToTarget(WowPlayer wowPlayer, WowRoute targetRoute)
        {
            WowZone currentZone = wowPlayer.WorldState.CurrentZone;
            WowZoneCrossing zoneCrossing = null;
            List<Vector2> waypoints = new List<Vector2>();

            if (targetRoute.Zone == currentZone)
            {
                waypoints = GetWaypointsToTarget(wowPlayer, targetRoute);
            }
            else
            {
                // Only build as far as the next crossing -- map coordinates are per-zone, so the
                // rest of the trip can't be walked as one route. The caller comes back here once
                // it's in the next zone, which also means nothing needs saving if a fight or a
                // restart interrupts the trip.
                List<WowZoneCrossing> crossings = GetZoneCrossingsBetween(currentZone, targetRoute.Zone, wowPlayer.WorldState.PlayerLevel);

                if (crossings == null)
                {
                    wowPlayer.SetLogoutVariablesAndSlack($"Unable to find a chain of zone crossings from {currentZone} to {targetRoute.Zone} for {targetRoute}");
                }
                else
                {
                    zoneCrossing = crossings[0];
                    waypoints = GetWaypointsToPoint(wowPlayer, zoneCrossing.FromPoint);
                }
            }

            Console.WriteLine($"Built route to target: {String.Join(",", waypoints)}");

            return new WowRoute
            {
                Name = "Dynamic Route",
                Waypoints = waypoints,
                Zone = currentZone, // this leg, not the destination, which might be zones away
                ZoneCrossingAtEnd = zoneCrossing,
                TraversalMethod = WowRoute.WaypointTraversalMethod.LINEAR,
                MinimumLevel = 0,
                DistanceTolerance = WowRoute.DEFAULT_DISTANCE_TOLERANCE
            };
        }

        // Breadth-first search over the zones, where each crossing is a one-way edge. Returns the
        // crossings to take in order, using as few as possible -- empty if already in targetZone,
        // null if targetZone can't be reached. crossings defaults to ALL_ZONE_CROSSINGS.
        public static List<WowZoneCrossing> GetZoneCrossingsBetween(WowZone startingZone, WowZone targetZone, int playerLevel, IEnumerable<WowZoneCrossing> crossings = null)
        {
            List<WowZoneCrossing> eligibleCrossings = (crossings ?? WowZoneCrossings.ALL_ZONE_CROSSINGS)
                .Where(crossing => crossing.MinimumLevel <= 0 || playerLevel >= crossing.MinimumLevel)
                .ToList();

            // Each visited zone maps to the crossing we reached it through (null for the start).
            Dictionary<WowZone, WowZoneCrossing> crossingInto = new Dictionary<WowZone, WowZoneCrossing> { { startingZone, null } };
            Queue<WowZone> zonesToVisit = new Queue<WowZone>();
            zonesToVisit.Enqueue(startingZone);

            while (zonesToVisit.Count > 0)
            {
                WowZone currentZone = zonesToVisit.Dequeue();

                if (currentZone == targetZone)
                {
                    List<WowZoneCrossing> path = new List<WowZoneCrossing>();
                    for (WowZoneCrossing crossing = crossingInto[targetZone]; crossing != null; crossing = crossingInto[crossing.FromZone])
                    {
                        path.Add(crossing);
                    }
                    path.Reverse();
                    return path;
                }

                foreach (WowZoneCrossing nextCrossing in eligibleCrossings.Where(c => c.FromZone == currentZone))
                {
                    if (!crossingInto.ContainsKey(nextCrossing.ToZone))
                    {
                        crossingInto[nextCrossing.ToZone] = nextCrossing;
                        zonesToVisit.Enqueue(nextCrossing.ToZone);
                    }
                }
            }

            return null;
        }

        // Top-level method should always take a route as the desired destination
        // Pathfinding helpers will be the ones to figure out which point is intersecting and shoot for that specifically
        private static List<Vector2> GetWaypointsToTarget(WowPlayer wowPlayer, WowRoute targetRoute)
        {
            List<WowRoute> routes = GetRoutesToTarget(wowPlayer, route => route == targetRoute, targetRoute.ToString());
            return GetWaypointsAlongRoutes(wowPlayer, routes, null);
        }

        // Unlike GetWaypointsToTarget, ends exactly on destination, rather than wherever the
        // chain first touches a route containing it.
        private static List<Vector2> GetWaypointsToPoint(WowPlayer wowPlayer, Vector2 destination)
        {
            List<WowRoute> routes = GetRoutesToTarget(wowPlayer, route => route.Waypoints.Contains(destination), destination.ToString());
            return GetWaypointsAlongRoutes(wowPlayer, routes, destination);
        }

        private static List<Vector2> GetWaypointsAlongRoutes(WowPlayer wowPlayer, List<WowRoute> routes, Vector2? finalWaypoint)
        {
            List<Vector2> waypoints = new List<Vector2>();
            Vector2 startingWaypoint = Vector2.Zero;
            Vector2 endingWaypoint = Vector2.Zero;

            if (routes.Count == 0)
            {
                return waypoints;
            }

            startingWaypoint = GetWaypointClosestToPlayer(wowPlayer, routes[0]);

            if (routes.Count == 1 && !finalWaypoint.HasValue)
            {
                return new List<Vector2> { startingWaypoint };
            }

            for (int i = 0; i < routes.Count - 1; i++)
            {
                if (!TryGetInstersectingWaypoint(routes[i], routes[i + 1], out endingWaypoint))
                {
                    wowPlayer.SetLogoutVariablesAndSlack($"Routes expected to have an intersection but don't ({routes[i]}) and ({routes[i + 1]})");
                    return waypoints;
                }

                var partialWaypoints = CreatePartialWaypointsFromRoute(routes[i], startingWaypoint, endingWaypoint);
                waypoints.AddRange(i == 0 ? partialWaypoints : partialWaypoints.Skip(1));

                startingWaypoint = endingWaypoint;
            }

            if (finalWaypoint.HasValue)
            {
                var finalPartialWaypoints = CreatePartialWaypointsFromRoute(routes[routes.Count - 1], startingWaypoint, finalWaypoint.Value);
                waypoints.AddRange(waypoints.Count == 0 ? finalPartialWaypoints : finalPartialWaypoints.Skip(1));
            }

            return waypoints;
        }

        // Breadth-first search over the safe routes in the player's current zone, where two
        // routes are connected if they share a waypoint (TryGetInstersectingWaypoint). Returns
        // the chain with the fewest route hops, starting with the route closest to the player
        // and ending with the first route isDestination accepts, e.g.
        // LEVEL_1_DUROTAR_BOARS_AND_SCORPS -> BOARS_AND_SCORPS_TO_MERCHANT -> VALLEY_OF_TRIALS_MERCHANT.
        private static List<WowRoute> GetRoutesToTarget(WowPlayer wowPlayer, Func<WowRoute, bool> isDestination, string destinationName)
        {
            WowRoute startingRoute = FindRouteClosestToPlayer(wowPlayer);

            if (startingRoute == null)
            {
                return new List<WowRoute>();
            }

            List<WowRoute> eligibleRoutes = GetAllSafeRoutes(wowPlayer)
                .Where(route => route.Zone == wowPlayer.WorldState.CurrentZone)
                .ToList();

            // Each visited route maps to the route we reached it from (null for the start).
            Dictionary<WowRoute, WowRoute> previousRoute = new Dictionary<WowRoute, WowRoute> { { startingRoute, null } };
            Queue<WowRoute> routesToVisit = new Queue<WowRoute>();
            routesToVisit.Enqueue(startingRoute);

            while (routesToVisit.Count > 0)
            {
                WowRoute currentRoute = routesToVisit.Dequeue();

                if (isDestination(currentRoute))
                {
                    List<WowRoute> routes = new List<WowRoute>();
                    for (WowRoute route = currentRoute; route != null; route = previousRoute[route])
                    {
                        routes.Add(route);
                    }
                    routes.Reverse();
                    return routes;
                }

                foreach (WowRoute nextRoute in eligibleRoutes)
                {
                    if (!previousRoute.ContainsKey(nextRoute) && TryGetInstersectingWaypoint(currentRoute, nextRoute, out _))
                    {
                        previousRoute[nextRoute] = currentRoute;
                        routesToVisit.Enqueue(nextRoute);
                    }
                }
            }

            string message = $"Unable to find a chain of intersecting routes from {startingRoute} to {destinationName} in {wowPlayer.WorldState.CurrentZone}";
            wowPlayer.SetLogoutVariablesAndSlack(message);
            return new List<WowRoute>();
        }

        private static WowRoute FindRouteClosestToPlayer(WowPlayer wowPlayer)
        {
            var safeRoutes = GetAllSafeRoutes(wowPlayer);
            var routesInZone = safeRoutes.Where(route => route.Zone == wowPlayer.WorldState.CurrentZone).ToList();
            WowRoute closestRoute = routesInZone
                .Select(route => new { Route = route, Distance = WowPathfinding.GetDistanceToClosestWaypoint(wowPlayer.WorldState.PlayerLocation, route.Waypoints) })
                .Where(x => x.Distance < WowConstants.MAX_DISTANCE_FROM_ROUTE_WAYPOINT)
                .OrderBy(x => x.Distance)
                .Select(x => x.Route)
                .FirstOrDefault();

            if (closestRoute == null)
            {
                string message = $"Unable to find route close enough to player in FindRouteClosestToPlayer, player location {wowPlayer.WorldState.PlayerLocation} in {wowPlayer.WorldState.CurrentZone}";
                wowPlayer.SetLogoutVariablesAndSlack(message);
                return null;
            }

            return closestRoute;
        }

        private static Vector2 GetWaypointClosestToPlayer(WowPlayer wowPlayer, WowRoute route)
        {
            return route.Waypoints
                .Select(waypoint => (dist: Vector2.Distance(wowPlayer.WorldState.PlayerLocation, waypoint), waypoint))
                .OrderBy(t => t.dist)
                .First()
                .waypoint;
        }

        private static List<WowRoute> GetAllSafeRoutes(WowPlayer wowPlayer)
        {
            List<WowRoute> safeRoutes = new List<WowRoute>();
            safeRoutes.AddRange(WowRoutes.ALL_WALKING_ROUTES);
            safeRoutes.AddRange(WowLocationConfigs.ALL_LOCATIONS.Select(config => config.Route));
            safeRoutes.AddRange(WowNPCConfigs.ALL_NPCS.Select(config => config.Route));
            return safeRoutes
                .Where(route => route.MinimumLevel <= 0 || wowPlayer.WorldState.PlayerLevel >= route.MinimumLevel)
                .ToList();
        }

        private static bool TryGetInstersectingWaypoint(WowRoute firstRoute, WowRoute secondRoute, out Vector2 intersectingWaypoint)
        {
            var intersection = firstRoute.Waypoints.Intersect(secondRoute.Waypoints);
            if (intersection.Any())
            {
                intersectingWaypoint = intersection.First();
                return true;
            }

            intersectingWaypoint = Vector2.Zero;
            return false;
        }

        // Brute-force O(n*m) search for the pair of waypoints (one from each route) nearest each
        // other. Waypoint-to-waypoint only -- doesn't consider points along the segments between
        // waypoints. Distance is 0 when the routes share a waypoint (see TryGetInstersectingWaypoint).
        //
        // If startingPoint is given, the pair is instead each route's own waypoint closest to
        // startingPoint (picked independently per route), and distance is still the distance
        // between those two waypoints.
        public static (Vector2 firstWaypoint, Vector2 secondWaypoint, float distance) GetClosestWaypointsBetweenRoutes(WowRoute firstRoute, WowRoute secondRoute, Vector2? startingPoint = null)
        {
            if (firstRoute.Waypoints == null || firstRoute.Waypoints.Count == 0 ||
                secondRoute.Waypoints == null || secondRoute.Waypoints.Count == 0)
            {
                throw new ArgumentException($"Both routes need at least one waypoint ({firstRoute}) and ({secondRoute})");
            }

            if (startingPoint.HasValue)
            {
                Vector2 point = startingPoint.Value;
                Vector2 firstNearPoint = firstRoute.Waypoints.OrderBy(waypoint => Vector2.Distance(point, waypoint)).First();
                Vector2 secondNearPoint = secondRoute.Waypoints.OrderBy(waypoint => Vector2.Distance(point, waypoint)).First();
                return (firstNearPoint, secondNearPoint, Vector2.Distance(firstNearPoint, secondNearPoint));
            }

            Vector2 closestFirst = firstRoute.Waypoints[0];
            Vector2 closestSecond = secondRoute.Waypoints[0];
            float closestDistance = float.MaxValue;

            foreach (Vector2 first in firstRoute.Waypoints)
            {
                foreach (Vector2 second in secondRoute.Waypoints)
                {
                    float distance = Vector2.Distance(first, second);
                    if (distance < closestDistance)
                    {
                        closestFirst = first;
                        closestSecond = second;
                        closestDistance = distance;
                    }
                }
            }

            return (closestFirst, closestSecond, closestDistance);
        }

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
                int forwardDiff = ((endingIndex - startingIndex) + route.Waypoints.Count) % route.Waypoints.Count;
                int backwardDiff = ((startingIndex - endingIndex) + route.Waypoints.Count) % route.Waypoints.Count;

                if (forwardDiff > backwardDiff) direction = -1;
                for (int i = startingIndex; i != endingIndex + direction; i += direction)
                {
                    i = (i + route.Waypoints.Count) % route.Waypoints.Count;
                    partialWaypoints.Add(route.Waypoints[i]);
                }
                return partialWaypoints;
            }
            
            throw new NotImplementedException($"Unhandled TraversalMethod {route.TraversalMethod}");
        }

        /*
        public static WowNPCConfiguration PickNPCToSellTo()
        {
            // TODO: implement!
            return WowNPCConfigs.VALLEY_OF_TRIALS_MERCHANT;
        }
        */
    }
}
