using System.Collections.Generic;

namespace WoWHelper.Code.WorldState
{
    // C# mirror of the name-based creature lists in CreatureConfig.lua (Lua Addon/CreatureConfig.lua).
    // Lua's version drives per-target runtime checks (decoded pixel-side into e.g.
    // WowWorldState.IsTargetNatureImmune); this one exists for config-time checks that have no
    // live target to read a pixel off of -- e.g. WowLocationConfiguration.AllMobsInZoneAreNatureImmune(),
    // which needs to know in advance whether every mob a route can pull is nature-immune, before
    // any of them are ever targeted.
    //
    // Only NATURE_IMMUNE_MOB_NAMES is mirrored so far -- add more of Lua's lists here only once
    // something on the C# side actually needs them, same as CreatureConfig.lua's own "add a list
    // when you need it" pattern. MUST stay in sync with CreatureConfig.lua's
    // NATURE_IMMUNE_MOB_NAMES table -- two independent hardcoded lists that have to agree, same
    // class of coupling as the WowZone enum / ZONE_NAME_TO_ID split (see
    // WowLocationConfiguration.cs).
    public static class CreatureConfig
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
