using System;
using System.Collections.Generic;
using System.Numerics;
using WoWHelper.Code.Gameplay;

namespace WoWHelper.Code.WorldState
{
    [Flags]
    public enum NPCRoles
    {
        None = 0,
        Sell = 1 << 0,   // 1
        Repair = 1 << 1, // 2
        Train = 1 << 2,  // 4
        Arrows = 1 << 3, // 8
        Bullets = 1 << 4, // 16
    }

    public class WowNPCConfiguration
    {
        public WowCombatConfiguration Class { get; set; } = WowCombatConfiguration.Unknown;
        public WowRoute Route { get; set; }
        public NPCRoles Roles { get; set; }
        public int MaximumLevel { get; set; } = 60;

        public override string ToString()
        {
            return $"WowNPC: {Route.Name}, {Route.Zone}";
        }
    }
}
