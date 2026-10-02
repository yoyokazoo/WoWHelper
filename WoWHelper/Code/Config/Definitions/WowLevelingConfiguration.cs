using System.Collections.Generic;
using WoWHelper.Code.Gameplay;

namespace WoWHelper.Code.WorldState
{
    // What a character of a given level should be doing to level up: which
    // LocationConfigs are acceptable places to gain XP (and money, when it's short of
    // what training costs), and which trainers to visit. One entry covers every starting
    // zone at that level -- e.g. level 1 lists both Durotar's and Mulgore's routes
    // -- and the goal logic narrows it down by matching each config's own Zone against
    // WowWorldState.CurrentZone. Whether to train vs. grind comes from
    // WowWorldState.AllSkillsKnownForThisLevel/CanAffordToTrainAllSkills.
    public class WowLevelingConfiguration
    {
        // Null (default) = any class. Set only for a genuinely class-specific plan, which
        // WowLevelingConfigs.GetFor prefers over an any-class one at the same level. Prefer
        // pushing class differences into the addon (e.g. the per-class trainer tables behind
        // AllSkillsKnownForThisLevel) over adding class-specific entries here.
        public WowCombatConfiguration? Class { get; set; }
        public int Level { get; set; }

        public List<WowLocationConfiguration> LocationConfigs { get; set; }

        // TODO: stub -- just a human-readable trainer name for now. Will need at least a
        // zone and a route to the trainer (see WowMerchantConfiguration for the shape the
        // merchant detour uses) once training is actually automated.
        public List<string> TrainerConfigs { get; set; }

        public WowLevelingConfiguration()
        {
            Class = null;
            LocationConfigs = new List<WowLocationConfiguration>();
            TrainerConfigs = new List<string>();
        }
    }
}
