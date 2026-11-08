using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WoWHelper.Code;
using WoWHelper.Code.WorldState;

namespace WoWHelperUnitTests
{
    // Covers the zone-level search, plus WowZoneCrossing authoring mistakes in
    // WowZoneCrossings.cs -- doesn't need a WowPlayer/screen resolution, so this doesn't extend
    // UnitTestBase like PathfindingTests does.
    [TestClass]
    public class ZoneCrossingTests
    {
        private static WowZoneCrossing Crossing(WowZone from, WowZone to, int minimumLevel = 0)
        {
            return new WowZoneCrossing { FromZone = from, ToZone = to, MinimumLevel = minimumLevel };
        }

        private static readonly WowZoneCrossing DUROTAR_TO_BARRENS = Crossing(WowZone.Durotar, WowZone.TheBarrens);
        private static readonly WowZoneCrossing BARRENS_TO_ASHENVALE = Crossing(WowZone.TheBarrens, WowZone.Ashenvale);
        private static readonly WowZoneCrossing DUROTAR_TO_ASHENVALE = Crossing(WowZone.Durotar, WowZone.Ashenvale);

        [TestMethod]
        public void SameZoneNeedsNoCrossings()
        {
            var path = WowPathfinding.GetZoneCrossingsBetween(WowZone.Durotar, WowZone.Durotar, 1, new List<WowZoneCrossing>());

            Assert.IsNotNull(path);
            Assert.AreEqual(0, path.Count);
        }

        [TestMethod]
        public void ChainsCrossingsInTravelOrder()
        {
            var crossings = new List<WowZoneCrossing> { BARRENS_TO_ASHENVALE, DUROTAR_TO_BARRENS };

            var path = WowPathfinding.GetZoneCrossingsBetween(WowZone.Durotar, WowZone.Ashenvale, 1, crossings);

            CollectionAssert.AreEqual(new List<WowZoneCrossing> { DUROTAR_TO_BARRENS, BARRENS_TO_ASHENVALE }, path);
        }

        [TestMethod]
        public void PicksFewestCrossings()
        {
            var crossings = new List<WowZoneCrossing> { DUROTAR_TO_BARRENS, BARRENS_TO_ASHENVALE, DUROTAR_TO_ASHENVALE };

            var path = WowPathfinding.GetZoneCrossingsBetween(WowZone.Durotar, WowZone.Ashenvale, 1, crossings);

            CollectionAssert.AreEqual(new List<WowZoneCrossing> { DUROTAR_TO_ASHENVALE }, path);
        }

        [TestMethod]
        public void CrossingsAreDirected()
        {
            var crossings = new List<WowZoneCrossing> { DUROTAR_TO_BARRENS };

            Assert.IsNull(WowPathfinding.GetZoneCrossingsBetween(WowZone.TheBarrens, WowZone.Durotar, 1, crossings));
        }

        [TestMethod]
        public void UnderLeveledCrossingsAreSkipped()
        {
            var crossings = new List<WowZoneCrossing> { Crossing(WowZone.Durotar, WowZone.TheBarrens, minimumLevel: 10) };

            Assert.IsNull(WowPathfinding.GetZoneCrossingsBetween(WowZone.Durotar, WowZone.TheBarrens, 9, crossings));
            Assert.AreEqual(1, WowPathfinding.GetZoneCrossingsBetween(WowZone.Durotar, WowZone.TheBarrens, 10, crossings).Count);
        }

        [TestMethod]
        public void ReversedSwapsEndsAndTurnsAround()
        {
            var crossing = new WowZoneCrossing
            {
                FromZone = WowZone.Durotar,
                FromPoint = new Vector2(1f, 2f),
                ToZone = WowZone.Orgrimmar,
                ToPoint = new Vector2(3f, 4f),
                HeadingDegrees = 270f,
                MinimumLevel = 5,
            };

            var reversed = crossing.Reversed();

            Assert.AreEqual(WowZone.Orgrimmar, reversed.FromZone);
            Assert.AreEqual(new Vector2(3f, 4f), reversed.FromPoint);
            Assert.AreEqual(WowZone.Durotar, reversed.ToZone);
            Assert.AreEqual(new Vector2(1f, 2f), reversed.ToPoint);
            Assert.AreEqual(90f, reversed.HeadingDegrees);
            Assert.AreEqual(5, reversed.MinimumLevel);
        }

        [TestMethod]
        public void ReturnHeadingOverridesTurningAround()
        {
            var crossing = new WowZoneCrossing { HeadingDegrees = 90f, ReturnHeadingDegrees = 200f };

            Assert.AreEqual(200f, crossing.Reversed().HeadingDegrees);
            Assert.AreEqual(90f, crossing.Reversed().Reversed().HeadingDegrees);
        }

        [TestMethod]
        public void EveryDefinedCrossingWorksBothWays()
        {
            foreach (var crossing in WowZoneCrossings.ALL_ZONE_CROSSINGS)
            {
                Assert.IsNotNull(WowPathfinding.GetZoneCrossingsBetween(crossing.ToZone, crossing.FromZone, 60),
                    $"{crossing} has no way back from {crossing.ToZone} to {crossing.FromZone}.");
            }
        }

        [TestMethod]
        public void EveryCrossingStartsOnAWalkableRoute()
        {
            foreach (var crossing in WowZoneCrossings.ALL_ZONE_CROSSINGS)
            {
                Assert.IsTrue(HasRouteThrough(crossing.FromZone, crossing.FromPoint),
                    $"{crossing}'s FromPoint ({crossing.FromPoint}) isn't a waypoint on a route in {crossing.FromZone}, so the bot can't path to it.");
            }
        }

        [TestMethod]
        public void EveryCrossingEndsOnAWalkableRoute()
        {
            foreach (var crossing in WowZoneCrossings.ALL_ZONE_CROSSINGS)
            {
                Assert.IsTrue(HasRouteThrough(crossing.ToZone, crossing.ToPoint),
                    $"{crossing}'s ToPoint ({crossing.ToPoint}) isn't a waypoint on a route in {crossing.ToZone}, so the bot can't carry on from there.");
            }
        }

        private static bool HasRouteThrough(WowZone zone, Vector2 point)
        {
            return WowRoutes.ALL_WALKING_ROUTES
                .Concat(WowLocationConfigs.ALL_LOCATIONS.Select(location => location.Route))
                .Concat(WowNPCConfigs.ALL_NPCS.Select(npc => npc.Route))
                .Any(route => route.Zone == zone && route.Waypoints.Contains(point));
        }
    }
}
