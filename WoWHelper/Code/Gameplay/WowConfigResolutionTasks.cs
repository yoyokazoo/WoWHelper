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
        // Resolves FarmingConfig.CombatConfiguration + builds ClassState from the
        // player's detected class (WorldState.PlayerClass). Deliberately NOT tied to the
        // RESOLVE_FARMING_CONFIGURATION player state -- called every tick from
        // WowManagementTasks.EveryWorldStateUpdateTasks() instead, so it resolves even if
        // CoreGameplayLoopTask's "already in combat" short-circuit (top of its while loop)
        // jumps straight to IN_CORE_COMBAT_LOOP before RESOLVE_FARMING_CONFIGURATION ever
        // gets to run -- e.g. the bot was (re)started while the character was already
        // mid-fight. Without this split, the combat loop would try to dispatch on
        // CombatConfiguration while it was still Unknown and ClassState was still null,
        // hitting the "no dispatch implemented for CombatConfiguration \"Unknown\""
        // NotImplementedException the moment it tried to act.
        //
        // Early-outs quietly (no logging, no Slack alert) in the two cases that are
        // completely expected on any given tick: already resolved (ClassState != null --
        // nothing to do; this runs every tick forever), or the addon hasn't rendered a
        // real pixel row yet / this is an unsupported class (WorldState.PlayerClass ==
        // null -- normal for the first handful of ticks after focusing the window, and
        // while sitting on the login screen).
        public void ResolveCombatConfiguration()
        {
            if (ClassState != null)
            {
                return;
            }

            if (WorldState.PlayerClass == null)
            {
                return;
            }

            FarmingConfig.CombatConfiguration = WorldState.PlayerClass.Value;
            ClassState = CreateClassState(FarmingConfig.CombatConfiguration);
            ClassState.UpdateFromBitmap(WorldState.Bmp, FarmingConfig.ScreenConfiguration);

            Console.WriteLine($"Auto-detected combat config {FarmingConfig.CombatConfiguration}");
        }

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

            Console.WriteLine($"Using location config \"{FarmingConfig.LocationConfiguration.Title}\" (combat config {FarmingConfig.CombatConfiguration} already resolved)");

            return true;
        }
    }
}
