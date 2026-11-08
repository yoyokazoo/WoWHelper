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
        public async Task PickFarmingLocationConfig()
        {
            var levelingConfig = WowLevelingConfigs.GetFor(CombatConfiguration, WorldState.PlayerLevel);

            // Any config we can reach, fewest zone crossings first, so a config in the current
            // zone always wins. Ties keep the order in LocationConfigs.
            var newLocationConfig = levelingConfig.LocationConfigs
                .Select(locationConfig => new
                {
                    Config = locationConfig,
                    Crossings = WowPathfinding.GetZoneCrossingsBetween(WorldState.CurrentZone, locationConfig.Route.Zone, WorldState.PlayerLevel)
                })
                .Where(x => x.Crossings != null)
                .OrderBy(x => x.Crossings.Count)
                .Select(x => x.Config)
                .FirstOrDefault();

            if (newLocationConfig == null)
            {
                SetLogoutVariablesAndSlack($"Couldn't find a suitable farming location config!");
                return;
            }

            if (newLocationConfig != FarmingLocationConfiguration)
            {
                if (!string.IsNullOrEmpty(newLocationConfig.TargetMacroMobNames))
                {
                    await WowInput.TypeChatLineTask($"/yytarget {newLocationConfig.TargetMacroMobNames}");
                }
                else
                {
                    SetLogoutVariablesAndSlack($"Location Config isn't set up properly, TargetMacroMobNames must be filled out!");
                    return;
                }
            }

            FarmingLocationConfiguration = newLocationConfig;
            CurrentRoute = FarmingLocationConfiguration.Route;
            CurrentWaypointIndex = -1;

            Console.WriteLine($"Picked farming location config \"{CurrentRoute.Name}\"");
        }
    }
}
