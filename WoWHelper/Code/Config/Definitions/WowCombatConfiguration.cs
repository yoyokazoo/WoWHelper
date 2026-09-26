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
        // 1 (Mage) retired -- Mage support was removed (never got the rotation working,
        // and a lot has changed elsewhere since). Not reused, to avoid confusing anything
        // that might have logged/persisted the old numeric value.
        Shaman = 2,
        Warlock = 3,
    }
}
