namespace WoWHelper.Code.WorldState
{
    public enum WowFaction
    {
        EITHER,
        HORDE,
        ALLIANCE,
    }

    public static class WowFactionExtensions
    {
        // Whether a route/NPC/zone crossing tagged with this faction can be used by the
        // player (WowWorldState.PlayerIsHorde). EITHER matches both factions.
        public static bool AllowsPlayer(this WowFaction faction, bool playerIsHorde)
        {
            switch (faction)
            {
                case WowFaction.HORDE:
                    return playerIsHorde;
                case WowFaction.ALLIANCE:
                    return !playerIsHorde;
                default:
                    return true;
            }
        }
    }
}
