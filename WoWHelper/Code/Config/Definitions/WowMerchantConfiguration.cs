using System.Collections.Generic;
using System.Numerics;

namespace WoWHelper.Code.WorldState
{
    // Optional per-route "sell run" -- see WowLocationConfiguration.MerchantConfig. Waypoints[0]
    // MUST equal (within WowPlayerConstants.MERCHANT_BRANCH_POINT_EPSILON) one of the owning
    // WowLocationConfiguration's own Waypoints -- that's the point PathfindingLoopTask branches
    // off from when WorldState.BagsAreFull. Waypoints[^1] is the merchant's exact standing spot,
    // walked to with the much tighter MERCHANT_FINAL_WAYPOINT_TOLERANCE since
    // WowPlayer.MerchantRunStepTask's INTERACTING_WITH_MERCHANT phase (WowMovementTasks.cs)
    // right-clicks screen-center to open the vendor and needs to actually be standing on it.
    public class WowMerchantConfiguration
    {
        // Human-readable label only (like WowLocationConfiguration.Title) -- NOT fed into the
        // CTRL_TARGET_MERCHANT macro. That macro's /target line is a single fixed in-game
        // macro the player configures themselves; it can't be parameterized per-route from C#.
        public string Name { get; set; }

        public List<Vector2> Waypoints { get; set; } = new List<Vector2>();
    }
}
