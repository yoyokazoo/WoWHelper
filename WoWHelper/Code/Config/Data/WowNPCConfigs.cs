using System.Collections.Generic;
using System.Numerics;

namespace WoWHelper.Code.WorldState
{
    public static class WowNPCConfigs
    {
        public static readonly WowNPCConfiguration VALLEY_OF_TRIALS_MERCHANT = new WowNPCConfiguration
        {
            Name = "Duokna",
            Route = new WowRoute {
                Name = "Valley of Trials Merchant",
                Zone = WowZone.Durotar,
                Waypoints = new List<Vector2> { new Vector2(42.58f, 67.34f) } 
            },
            Roles = NPCRoles.Sell
        };

        public static readonly WowNPCConfiguration VALLEY_OF_TRIALS_WARRIOR_TRAINER = new WowNPCConfiguration
        {
            Name = "Frang",
            Class = Gameplay.WowCombatConfiguration.Warrior,
            Route = new WowRoute
            {
                Name = "Valley of Trials Warrior Trainer",
                Zone = WowZone.Durotar,
                Waypoints = new List<Vector2> { new Vector2(42.89f, 69.44f) }
            },
            Roles = NPCRoles.Train
        };

        public static readonly List<WowNPCConfiguration> ALL_NPCS = new List<WowNPCConfiguration>()
        {
            VALLEY_OF_TRIALS_MERCHANT,
            VALLEY_OF_TRIALS_WARRIOR_TRAINER,
        };
    }
}
