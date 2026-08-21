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
        // Runs once, right after the window is focused and before anything else touches
        // FarmingConfig.CombatConfiguration/LocationConfiguration or ClassState -- replaces
        // what used to be hardcoded in WowFarmingConfigs.CURRENT_CONFIG with values read
        // from live game state via the addon:
        //   - CombatConfiguration comes straight from WowWorldState.PlayerClass (the
        //     player's actual class, as reported by the addon).
        //   - LocationConfiguration is picked from WowLocationConfigs.ALL_LOCATIONS by
        //     filtering to configs the player currently satisfies (level, zone, and close
        //     enough to one of the route's own waypoints -- the same three checks
        //     SetLogoutVariablesTask uses to keep validating an already-chosen route). If
        //     exactly one config matches, that's unambiguous and we use it. If zero or more
        //     than one match, there's no safe automatic choice -- alert on Slack and let the
        //     caller fail the state transition (CoreGameplayLoopTask sends it straight to
        //     EXITING_CORE_GAMEPLAY_LOOP, which exits the process) rather than guessing.
        public async Task<bool> ResolveFarmingConfigurationTask()
        {
            await Task.Delay(0);

            if (WorldState.PlayerClass == null)
            {
                string reason = "Could not determine player's class from the addon (unsupported class, or the addon isn't loaded/rendering yet)";
                Console.WriteLine($"Aborting startup: {reason}");
                SlackHelper.SendMessageToChannel($"WoWHelper startup aborted: {reason}");
                return false;
            }

            FarmingConfig.CombatConfiguration = WorldState.PlayerClass.Value;
            ClassState = CreateClassState(FarmingConfig.CombatConfiguration);
            ClassState.UpdateFromBitmap(WorldState.Bmp, FarmingConfig.ScreenConfiguration);

            var matchingConfigs = WowLocationConfigs.ALL_LOCATIONS.Where(config =>
                (config.MinimumLevel <= 0 || WorldState.PlayerLevel >= config.MinimumLevel) &&
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

            FarmingConfig.LocationConfiguration = matchingConfigs[0];

            Console.WriteLine($"Auto-detected combat config {FarmingConfig.CombatConfiguration} and location config \"{FarmingConfig.LocationConfiguration.Title}\"");

            return true;
        }
    }
}
