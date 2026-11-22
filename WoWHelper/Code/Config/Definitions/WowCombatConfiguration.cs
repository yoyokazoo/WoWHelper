namespace WoWHelper.Code.Gameplay
{
    // Values are Blizzard's own class IDs (UnitClass's third return), sent as-is over the
    // pixel row -- see GetPlayerClassId() in WoWFunctions.lua.
    public enum WowCombatConfiguration
    {
        Unknown = -1,
        Warrior = 1,
        Paladin = 2,
        Hunter = 3,
        Rogue = 4,
        Priest = 5,
        Shaman = 7,
        Mage = 8,
        Warlock = 9,
        Druid = 11,
    }
}
