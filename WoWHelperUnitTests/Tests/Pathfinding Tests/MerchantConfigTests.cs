using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Numerics;
using WoWHelper.Code;
using WoWHelper.Code.WorldState;

namespace WoWHelperUnitTests
{
    // Catches WowMerchantConfiguration authoring mistakes in WowLocationConfigs.cs -- doesn't
    // need a WowPlayer/screen resolution, so this doesn't extend UnitTestBase like
    // PathfindingTests does.
    [TestClass]
    public class MerchantConfigTests
    {
        [TestMethod]
        public void EveryMerchantConfigHasAtLeastTwoWaypoints()
        {
            foreach (var location in WowLocationConfigs.ALL_LOCATIONS.Where(l => l.MerchantConfig != null))
            {
                Assert.IsTrue(location.MerchantConfig.Waypoints.Count >= 2,
                    $"{location.Title}'s MerchantConfig needs at least a branch point and a merchant location.");
            }
        }

        [TestMethod]
        public void EveryMerchantConfigsFirstWaypointMatchesARouteWaypoint()
        {
            foreach (var location in WowLocationConfigs.ALL_LOCATIONS.Where(l => l.MerchantConfig != null))
            {
                Vector2 branchPoint = location.MerchantConfig.Waypoints[0];

                bool matchesRouteWaypoint = location.Waypoints
                    .Any(w => Vector2.Distance(w, branchPoint) <= WowPlayerConstants.MERCHANT_BRANCH_POINT_EPSILON);

                Assert.IsTrue(matchesRouteWaypoint,
                    $"{location.Title}'s MerchantConfig.Waypoints[0] ({branchPoint}) doesn't match any of its own route waypoints -- PathfindingLoopTask will never branch off.");
            }
        }
    }
}
