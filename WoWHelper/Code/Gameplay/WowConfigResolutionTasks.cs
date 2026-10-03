using System;
using System.Linq;
using System.Threading.Tasks;
using WindowsGameAutomationTools.Slack;
using WoWHelper.Code;
using WoWHelper.Code.Gameplay;
using WoWHelper.Code.WorldState;

namespace WoWHelper
{
    public partial class WowPlayer
    {
        // Runs once, during the RESOLVE_FARMING_CONFIGURATION player state (after the
        // window is focused). Picks LocationConfiguration from WowLocationConfigs.ALL_LOCATIONS
        // by filtering to configs the player currently satisfies (level, zone, and close
        // enough to one of the route's own waypoints -- the same three checks
        // SetLogoutVariablesTask uses to keep validating an already-chosen route). If
        // exactly one config matches, that's unambiguous and we use it. If zero or more
        // than one match, there's no safe automatic choice -- alert on Slack and let the
        // caller fail the state transition (CoreGameplayLoopTask sends it straight to
        // EXITING_CORE_GAMEPLAY_LOOP, which exits the process) rather than guessing.
        //
        // Combat config resolution (ResolveCombatConfiguration above) runs continuously
        // and independently of this state, so by the time we get here ClassState should
        // already be set -- unless the addon genuinely never rendered a real row
        // (unsupported class, addon not loaded, still on the login screen after focusing
        // the window, etc.), which is still a real reason to abort startup.
        public async Task<bool> ResolveFarmingConfigurationTask()
        {
            await Task.Delay(0);

            if (ClassState == null)
            {
                string reason = "Could not determine player's class from the addon (unsupported class, or the addon isn't loaded/rendering yet)";
                Console.WriteLine($"Aborting startup: {reason}");
                SlackHelper.SendMessageToChannel($"WoWHelper startup aborted: {reason}");
                return false;
            }

            var matchingConfigs = WowLocationConfigs.ALL_LOCATIONS.Where(config =>
                (config.MinimumLevel <= 0 || WorldState.PlayerLevel >= config.MinimumLevel) &&
                (WorldState.PlayerLevel < config.MaximumLevel) && 
                (config.Zone == WowZone.Unknown || WorldState.CurrentZone == config.Zone) &&
                WowPathfinding.GetDistanceToClosestWaypoint(WorldState.PlayerLocation, config.Waypoints) <= WowPlayerConstants.MAX_DISTANCE_FROM_ROUTE_WAYPOINT
            ).ToList();

            if (matchingConfigs.Count != 1)
            {
                string reason = matchingConfigs.Count == 0
                    ? $"No location config matches the player's current level ({WorldState.PlayerLevel}), zone ({WorldState.CurrentZone}), and position -- nowhere close enough to any route's waypoints"
                    : $"{matchingConfigs.Count} location configs all match (level {WorldState.PlayerLevel}, zone {WorldState.CurrentZone}): {string.Join(", ", matchingConfigs.Select(c => c.Title))} -- can't pick automatically";

                Console.WriteLine($"Aborting startup: {reason}");
                SlackHelper.SendMessageToChannel($"WoWHelper startup aborted: {reason}");
                return false;
            }

            LocationConfiguration = matchingConfigs[0];

            Console.WriteLine($"Using location config \"{LocationConfiguration.Title}\" (combat config {CombatConfiguration} already resolved)");

            return true;
        }
    }
}
