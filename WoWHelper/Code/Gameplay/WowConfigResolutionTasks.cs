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

            // For now, only pick configs in the same zone
            var matchingLocationConfigs = levelingConfig.LocationConfigs
                .Where(locationConfig => locationConfig.Route.Zone == WorldState.CurrentZone);

            var newLocationConfig = matchingLocationConfigs.FirstOrDefault();

            if (newLocationConfig == null)
            {
                SetLogoutVariablesAndSlack($"Couldn't find a suitable farming location config!");
                return;
            }

            if (FarmingLocationConfiguration != null && newLocationConfig != FarmingLocationConfiguration)
            {
                if (!string.IsNullOrEmpty(FarmingLocationConfiguration.TargetMacroMobNames))
                {
                    await WowInput.TypeChatLineTask($"/yytarget {FarmingLocationConfiguration.TargetMacroMobNames}");
                }
                else
                {
                    SetLogoutVariablesAndSlack($"Location Config isn't set up properly, TargetMacroMobNames must be filled out!");
                    return;
                }
            }

            FarmingLocationConfiguration = newLocationConfig;
            CurrentRoute = FarmingLocationConfiguration.Route;

            Console.WriteLine($"Picked farming location config \"{CurrentRoute.Name}\"");
        }
    }
}
