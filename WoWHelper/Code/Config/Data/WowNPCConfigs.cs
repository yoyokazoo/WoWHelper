using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace WoWHelper.Code.WorldState
{
    public static class WowNPCConfigs
    {
        public static readonly WowNPCConfiguration VALLEY_OF_TRIALS_MERCHANT = new WowNPCConfiguration
        {
            Route = new WowRoute {
                Name = "VALLEY_OF_TRIALS_MERCHANT",
                Zone = WowZone.Durotar,
                Faction = WowFaction.HORDE,
                Waypoints = new List<Vector2> { new Vector2(42.58f, 67.34f) } 
            },
            Roles = NPCRoles.Sell
        };

        public static readonly WowNPCConfiguration VALLEY_OF_TRIALS_WARRIOR_TRAINER = new WowNPCConfiguration
        {
            Class = Gameplay.WowCombatConfiguration.Warrior,
            Route = new WowRoute
            {
                Name = "VALLEY_OF_TRIALS_WARRIOR_TRAINER",
                Zone = WowZone.Durotar,
                Faction = WowFaction.HORDE,
                Waypoints = new List<Vector2> { new Vector2(42.89f, 69.44f) }
            },
            Roles = NPCRoles.Train,
            MaximumLevel = 6,
        };

        public static readonly WowNPCConfiguration RAZOR_HILL_MERCHANT = new WowNPCConfiguration
        {
            Route = new WowRoute
            {
                Name = "RAZOR_HILL_MERCHANT",
                Zone = WowZone.Durotar,
                Faction = WowFaction.HORDE,
                Waypoints = new List<Vector2> { new Vector2(52.98f, 41.03f) }
            },
            Roles = NPCRoles.Sell | NPCRoles.Repair | NPCRoles.Arrows
        };

        public static readonly WowNPCConfiguration RAZOR_HILL_WARRIOR_TRAINER = new WowNPCConfiguration
        {
            Class = Gameplay.WowCombatConfiguration.Warrior,
            Route = new WowRoute
            {
                Name = "RAZOR_HILL_WARRIOR_TRAINER",
                Zone = WowZone.Durotar,
                Faction = WowFaction.HORDE,
                Waypoints = new List<Vector2> { new Vector2(54.19f, 42.47f), }
            },
            Roles = NPCRoles.Train
        };

        public static readonly WowNPCConfiguration TELDRASSIL_MERCHANT = new WowNPCConfiguration
        {
            Route = new WowRoute
            {
                Name = "TELDRASSIL_MERCHANT",
                Zone = WowZone.Teldrassil,
                Faction = WowFaction.ALLIANCE,
                Waypoints = new List<Vector2> { WowWalkingRoutes.TELDRASSIL_ANIMALS_TO_MERCHANT_ROUTE.Waypoints.Last() }
            },
            Roles = NPCRoles.Sell | NPCRoles.Repair | NPCRoles.Arrows
        };

        public static readonly WowNPCConfiguration TELDRASSIL_WARRIOR_TRAINER = new WowNPCConfiguration
        {
            Class = Gameplay.WowCombatConfiguration.Warrior,
            Route = new WowRoute
            {
                Name = "TELDRASSIL_WARRIOR_TRAINER",
                Zone = WowZone.Teldrassil,
                Faction = WowFaction.ALLIANCE,
                Waypoints = new List<Vector2> { WowWalkingRoutes.TELDRASSIL_TRAINER_TO_WARRIOR_TRAINER_ROUTE.Waypoints.Last() }
            },
            Roles = NPCRoles.Train
        };

        public static readonly WowNPCConfiguration TELDRASSIL_ROGUE_TRAINER = new WowNPCConfiguration
        {
            Class = Gameplay.WowCombatConfiguration.Rogue,
            Route = new WowRoute
            {
                Name = "TELDRASSIL_ROGUE_TRAINER",
                Zone = WowZone.Teldrassil,
                Faction = WowFaction.ALLIANCE,
                Waypoints = new List<Vector2> { WowWalkingRoutes.TELDRASSIL_TRAINER_TO_ROGUE_TRAINER_ROUTE.Waypoints.Last() }
            },
            Roles = NPCRoles.Train
        };

        public static readonly WowNPCConfiguration DUN_MOROGH_MERCHANT = new WowNPCConfiguration
        {
            Route = new WowRoute
            {
                Name = "DUN_MOROGH_MERCHANT",
                Zone = WowZone.DunMorogh,
                Faction = WowFaction.ALLIANCE,
                Waypoints = new List<Vector2> { WowWalkingRoutes.LEVEL_1_DUN_MOROGH_TO_MERCHANT.Waypoints.Last() }
            },
            Roles = NPCRoles.Sell
        };

        // Every NPC above, via reflection.
        private static readonly Lazy<IReadOnlyList<WowNPCConfiguration>> ALL_NPCS_LAZY = StaticConfigCollector.LazyCollect<WowNPCConfiguration>(typeof(WowNPCConfigs));
        public static IReadOnlyList<WowNPCConfiguration> ALL_NPCS => ALL_NPCS_LAZY.Value;
    }
}
