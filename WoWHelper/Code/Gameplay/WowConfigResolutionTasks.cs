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
        public async Task<bool> TryPickFarmingLocationConfig()
        {
            var levelingConfig = WowLevelingConfigs.GetFor(CombatConfiguration, WorldState.PlayerLevel);

            // For now, only pick configs in the same zone
            var matchingLocationConfigs = levelingConfig.LocationConfigs
                .Where(locationConfig => locationConfig.Route.Zone == WorldState.CurrentZone);

            var newLocationConfig = matchingLocationConfigs.FirstOrDefault();

            if (newLocationConfig != null)
            {
                SetLogoutVariablesAndSlack($"Couldn't find a suitable farming location config!");
                return false;
            }

            if (LocationConfiguration != null && newLocationConfig != LocationConfiguration)
            {
                if (!string.IsNullOrEmpty(LocationConfiguration.TargetMacroMobNames))
                {
                    await WowInput.TypeChatLineTask($"/yytarget {LocationConfiguration.TargetMacroMobNames}");
                }
                else
                {
                    SetLogoutVariablesAndSlack($"Couldn't find a suitable farming location config!");
                    return false;
                }
            }

            LocationConfiguration = newLocationConfig;
            CurrentRoute = LocationConfiguration.Route;

            Console.WriteLine($"Picked farming location config \"{CurrentRoute.Name}\"");
            return true;
        }
    }
}
