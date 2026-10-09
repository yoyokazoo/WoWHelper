using System.Collections.Generic;
using System.Numerics;

namespace WoWHelper.Code.WorldState
{
    public static class WowNPCConfigs
    {
        public static readonly WowNPCConfiguration VALLEY_OF_TRIALS_MERCHANT = new WowNPCConfiguration
        {
            Name = "Duokna",
            Route = new WowRoute {
                Zone = WowZone.Durotar,
                Waypoints = new List<Vector2> { new Vector2(42.58f, 67.34f) } 
            },
            Roles = NPCRoles.Sell
        };
    }
}
