namespace WoWHelper.Code.Config.Definitions
{
    // LogoutOnFullBags/LogoutOnLowDynamite used to live here too -- moved to being
    // toggled live in-game via the addon's /yyconfig menu instead (see
    // WowWorldState.LogoutOnFullBagsEnabled/LogoutOnLowDynamiteEnabled and
    // YoyokazooUI.lua) so they're run-specific rather than baked into a hardcoded
    // profile. Deliberately not duplicated here anymore -- two competing sources of
    // truth for the same toggle would just raise "which one wins" questions.
    public class WowManagementConfiguration
    {
        public bool AlertOnPotionUsed { get; set; }
        public bool AlertOnFullBags { get; set; }
        public bool AlertOnUnreadWhisper { get; set; }

        public WowManagementConfiguration() { }
    }
}
