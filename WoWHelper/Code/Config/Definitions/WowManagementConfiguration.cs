namespace WoWHelper.Code.Config.Definitions
{
    public class WowManagementConfiguration
    {
        public bool AlertOnPotionUsed { get; set; }
        public bool AlertOnFullBags { get; set; }
        public bool AlertOnUnreadWhisper { get; set; }
        public bool LogoutOnFullBags { get; set; }
        public bool LogoutOnLowDynamite { get; set; }

        public WowManagementConfiguration() { }
    }
}
