using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WoWHelper.Code.WorldState;

namespace WoWHelper
{
    // Ad-hoc route recorder: walk the route in-game, press Space at each waypoint, press Enter
    // to finish. Prints a copy-pasteable WowRoute declaration in a single Console.WriteLine.
    // Not wired into the core loop -- call from AdHocTestTask().
    public partial class WowPlayer
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int VK_SPACE = 0x20;
        private const int VK_RETURN = 0x0D;
        private const int ROUTE_RECORDER_POLL_MILLIS = 15;

        // First and last points closer than this (map-coordinate units) => CIRCULAR route.
        private const float ROUTE_RECORDER_CIRCULAR_DISTANCE = WowRoute.DEFAULT_DISTANCE_TOLERANCE;

        private static bool IsKeyDown(int vKey) => (GetAsyncKeyState(vKey) & 0x8000) != 0;

        public async Task<bool> RecordRouteTask()
        {
            await UpdateWorldStateAsync();
            if (WorldState.CurrentZone == WowZone.Unknown)
            {
                throw new InvalidOperationException("Route recorder: current zone is Unknown -- add it to WowZone and ZONE_NAME_TO_ID before recording a route here.");
            }

            var waypoints = new List<Vector2>();

            Console.WriteLine("Route recorder started: press Space to record a waypoint, Enter to finish.");

            // Seed with the current key state so a key already held when we start doesn't count.
            bool spaceWasDown = IsKeyDown(VK_SPACE);
            bool enterWasDown = IsKeyDown(VK_RETURN);

            while (true)
            {
                bool spaceDown = IsKeyDown(VK_SPACE);
                bool enterDown = IsKeyDown(VK_RETURN);

                // Only fire on the up -> down transition, so holding a key records once.
                if (enterDown && !enterWasDown)
                {
                    break;
                }

                if (spaceDown && !spaceWasDown)
                {
                    await UpdateWorldStateAsync();
                    if (!WorldState.IsBotInAValidState)
                    {
                        Console.WriteLine("Space pressed, but WoW isn't focused / addon not loaded -- waypoint NOT recorded.");
                    }
                    else
                    {
                        var point = new Vector2(WorldState.MapX, WorldState.MapY);
                        waypoints.Add(point);
                        Console.WriteLine($"Recorded waypoint {waypoints.Count}: {FormatRouteFloat(point.X)}, {FormatRouteFloat(point.Y)}");
                    }
                }

                spaceWasDown = spaceDown;
                enterWasDown = enterDown;

                await Task.Delay(ROUTE_RECORDER_POLL_MILLIS);
            }

            // Grab zone/faction fresh at the end of the run.
            await UpdateWorldStateAsync();

            var traversal = WowRoute.WaypointTraversalMethod.LINEAR;
            if (waypoints.Count >= 3 &&
                Vector2.Distance(waypoints[0], waypoints[waypoints.Count - 1]) <= ROUTE_RECORDER_CIRCULAR_DISTANCE)
            {
                traversal = WowRoute.WaypointTraversalMethod.CIRCULAR;
                waypoints.RemoveAt(waypoints.Count - 1);
            }

            long endMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var faction = WorldState.PlayerIsHorde ? WowFaction.HORDE : WowFaction.ALLIANCE;

            var sb = new StringBuilder();
            sb.AppendLine();
            AppendRouteDeclaration(sb, $"AD_HOC_ROUTE_{endMillis}", $"Ad Hoc {endMillis}", WorldState.CurrentZone, faction, traversal, waypoints);

            // A linear route only gets walked start -> end -> start, so also print the reversed
            // copy, for when the route's natural starting point is the other end.
            if (traversal == WowRoute.WaypointTraversalMethod.LINEAR)
            {
                var reversed = new List<Vector2>(waypoints);
                reversed.Reverse();
                sb.AppendLine();
                sb.AppendLine();
                AppendRouteDeclaration(sb, $"AD_HOC_ROUTE_{endMillis}_REVERSED", $"Ad Hoc {endMillis} Reversed", WorldState.CurrentZone, faction, traversal, reversed);
            }

            // One write for the whole thing, so it copy-pastes without per-line timestamps.
            Console.WriteLine(sb.ToString());
            return true;
        }

        private static void AppendRouteDeclaration(StringBuilder sb, string fieldName, string name, WowZone zone,
            WowFaction faction, WowRoute.WaypointTraversalMethod traversal, List<Vector2> waypoints)
        {
            sb.AppendLine($"public static readonly WowRoute {fieldName} = new WowRoute");
            sb.AppendLine("{");
            sb.AppendLine($"    Name = \"{name}\",");
            sb.AppendLine($"    Zone = WowZone.{zone},");
            sb.AppendLine($"    Faction = WowFaction.{faction},");
            sb.AppendLine($"    TraversalMethod = WowRoute.WaypointTraversalMethod.{traversal},");
            sb.AppendLine();
            sb.AppendLine("    Waypoints = new List<Vector2>");
            sb.AppendLine("    {");
            foreach (var point in waypoints)
            {
                sb.AppendLine($"        new Vector2({FormatRouteFloat(point.X)}f, {FormatRouteFloat(point.Y)}f),");
            }
            sb.AppendLine("    },");
            sb.Append("};");
        }

        private static string FormatRouteFloat(float value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
