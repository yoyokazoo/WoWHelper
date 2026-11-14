using System.Collections.Generic;
using System.Linq;
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
                Faction = WowFaction.HORDE,
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
                Faction = WowFaction.HORDE,
                Waypoints = new List<Vector2> { new Vector2(42.89f, 69.44f) }
            },
            Roles = NPCRoles.Train,
            MaximumLevel = 6,
        };

        public static readonly WowNPCConfiguration RAZOR_HILL_MERCHANT = new WowNPCConfiguration
        {
            Name = "Ghrawt (Razor Hill)",
            Route = new WowRoute
            {
                Name = "Razor Hill Merchant",
                Zone = WowZone.Durotar,
                Faction = WowFaction.HORDE,
                Waypoints = new List<Vector2> { new Vector2(52.98f, 41.03f) }
            },
            Roles = NPCRoles.Sell | NPCRoles.Repair | NPCRoles.Arrows
        };

        public static readonly WowNPCConfiguration RAZOR_HILL_WARRIOR_TRAINER = new WowNPCConfiguration
        {
            Name = "??? Razor hill warrior trainer",
            Class = Gameplay.WowCombatConfiguration.Warrior,
            Route = new WowRoute
            {
                Name = "Razor Hill Warrior Trainer",
                Zone = WowZone.Durotar,
                Faction = WowFaction.HORDE,
                Waypoints = new List<Vector2> { new Vector2(54.19f, 42.47f), }
            },
            Roles = NPCRoles.Train
        };

        public static readonly WowNPCConfiguration TELDRASSIL_MERCHANT = new WowNPCConfiguration
        {
            Name = "Teldrassil Merchant",
            Route = new WowRoute
            {
                Name = "Teldrassil Merchant",
                Zone = WowZone.Teldrassil,
                Faction = WowFaction.ALLIANCE,
                Waypoints = new List<Vector2> { WowWalkingRoutes.TELDRASSIL_ANIMALS_TO_MERCHANT_ROUTE.Waypoints.Last() }
            },
            Roles = NPCRoles.Sell | NPCRoles.Repair | NPCRoles.Arrows
        };

        public static readonly WowNPCConfiguration TELDRASSIL_WARRIOR_TRAINER = new WowNPCConfiguration
        {
            Name = "Teldrassil Warrior Trainer",
            Class = Gameplay.WowCombatConfiguration.Warrior,
            Route = new WowRoute
            {
                Name = "Teldrassil Warrior Trainer",
                Zone = WowZone.Teldrassil,
                Faction = WowFaction.ALLIANCE,
                Waypoints = new List<Vector2> { WowWalkingRoutes.TELDRASSIL_MERCHANT_TO_WARRIOR_TRAINER_ROUTE.Waypoints.Last() }
            },
            Roles = NPCRoles.Train
        };

        public static readonly List<WowNPCConfiguration> ALL_NPCS = new List<WowNPCConfiguration>()
        {
            VALLEY_OF_TRIALS_MERCHANT,
            VALLEY_OF_TRIALS_WARRIOR_TRAINER,
            RAZOR_HILL_WARRIOR_TRAINER,
            RAZOR_HILL_MERCHANT,
            TELDRASSIL_MERCHANT,
            TELDRASSIL_WARRIOR_TRAINER
        };
    }
}
