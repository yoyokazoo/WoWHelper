using System;
using System.Collections.Generic;
using System.Numerics;

namespace WoWHelper.Code.WorldState
{
    [Flags]
    public enum NPCRoles
    {
        None = 0,
        Sell = 1 << 0,   // 1
        Repair = 1 << 1, // 2
    }

    public class WowNPCConfiguration
    {
        public string Name { get; set; }
        public WowRoute Route { get; set; }
        public NPCRoles Roles { get; set; }
    }
}
