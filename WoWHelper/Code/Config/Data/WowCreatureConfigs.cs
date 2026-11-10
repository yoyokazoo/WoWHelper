using System.Collections.Generic;

namespace WoWHelper.Code.WorldState
{
    // Lua handles target-specific immunities.  This is used for non target-specific knowledge.  
    public static class WowCreatureConfigs
    {
        public static readonly HashSet<string> NATURE_IMMUNE_MOB_NAMES = new HashSet<string>
        {
            "Swirling Vortex",
            "Rotting Worm",
            "Desert Rumbler",
            "Whirling Invader",
            "Dust Stormer",
        };
    }
}
