namespace WoWHelper.Code.Gameplay
{
    public enum WowCombatConfiguration
    {
        // Not yet resolved -- see WowPlayer.ResolveCombatConfiguration (WowConfigResolutionTasks.cs), which sets
        // this from the player's class (WowWorldState.PlayerClass) at startup instead of
        // it being hardcoded. Default so an un-resolved config fails loudly (every
        // Wow*CombatConfig.cs dispatcher's switch has a "default: throw" case) rather than
        // silently behaving like Warrior.
        Unknown = -1,
        Warrior = 0,
        Mage = 1,
        Shaman = 2,
        Warlock = 3,
    }
}
