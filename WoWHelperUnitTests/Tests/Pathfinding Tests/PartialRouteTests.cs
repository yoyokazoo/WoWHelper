using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WoWHelper.Code;
using WoWHelper.Code.WorldState;

namespace WoWHelperUnitTests
{
    // Pure-math tests for WowPathfinding.CreatePartialWaypointsFromRoute -- doesn't need a
    // WowPlayer/screen resolution, so this doesn't extend UnitTestBase like PathfindingTests does.
    [TestClass]
    public class PartialRouteTests
    {
        // Odd count on purpose: with 7 waypoints the forward and backward distances around a
        // circular route are never equal, so every circular case has one unambiguous shortest path.
        private const int WAYPOINT_COUNT = 7;

        // Waypoint i sits at (i, i * 10), so indices and positions are easy to map back and forth.
        private static WowRoute CreateRoute(WowRoute.WaypointTraversalMethod traversalMethod)
        {
            return new WowRoute
            {
                TraversalMethod = traversalMethod,
                Waypoints = Enumerable.Range(0, WAYPOINT_COUNT).Select(i => new Vector2(i, i * 10)).ToList()
            };
        }

        private static void AssertPartialRoute(WowRoute.WaypointTraversalMethod traversalMethod, int startIndex, int endIndex, int[] expectedIndices)
        {
            WowRoute route = CreateRoute(traversalMethod);
            List<Vector2> expected = expectedIndices.Select(i => route.Waypoints[i]).ToList();

            List<Vector2> actual = WowPathfinding.CreatePartialWaypointsFromRoute(route, route.Waypoints[startIndex], route.Waypoints[endIndex]);

            CollectionAssert.AreEqual(expected, actual,
                $"{traversalMethod} {startIndex} -> {endIndex}: expected [{string.Join(", ", expected)}], got [{string.Join(", ", actual)}]");
        }

        #region LINEAR

        [TestMethod]
        [DataRow(1, 4, new[] { 1, 2, 3, 4 })]
        [DataRow(0, 6, new[] { 0, 1, 2, 3, 4, 5, 6 })]
        [DataRow(2, 3, new[] { 2, 3 })]
        public void LinearForward(int startIndex, int endIndex, int[] expectedIndices)
        {
            AssertPartialRoute(WowRoute.WaypointTraversalMethod.LINEAR, startIndex, endIndex, expectedIndices);
        }

        [TestMethod]
        [DataRow(4, 1, new[] { 4, 3, 2, 1 })]
        [DataRow(6, 0, new[] { 6, 5, 4, 3, 2, 1, 0 })] // never wraps, even though 6 -> 0 is one step on a circular route
        [DataRow(3, 2, new[] { 3, 2 })]
        public void LinearBackward(int startIndex, int endIndex, int[] expectedIndices)
        {
            AssertPartialRoute(WowRoute.WaypointTraversalMethod.LINEAR, startIndex, endIndex, expectedIndices);
        }

        [TestMethod]
        public void LinearSameStartAndEnd()
        {
            AssertPartialRoute(WowRoute.WaypointTraversalMethod.LINEAR, 3, 3, new[] { 3 });
        }

        #endregion

        #region CIRCULAR

        // endIndex > startIndex, shortest path is straight forward through the list
        [TestMethod]
        [DataRow(1, 3, new[] { 1, 2, 3 })]
        [DataRow(0, 3, new[] { 0, 1, 2, 3 })] // forward 3 vs backward 4
        [DataRow(4, 6, new[] { 4, 5, 6 })]    // ends on the last index without wrapping
        public void CircularEndAfterStartDirect(int startIndex, int endIndex, int[] expectedIndices)
        {
            AssertPartialRoute(WowRoute.WaypointTraversalMethod.CIRCULAR, startIndex, endIndex, expectedIndices);
        }

        // endIndex > startIndex, shortest path is backward through index 0 and wraps to the end
        [TestMethod]
        [DataRow(1, 5, new[] { 1, 0, 6, 5 })] // backward 3 vs forward 4
        [DataRow(0, 6, new[] { 0, 6 })]       // single wrap step
        [DataRow(2, 6, new[] { 2, 1, 0, 6 })]
        public void CircularEndAfterStartWraparound(int startIndex, int endIndex, int[] expectedIndices)
        {
            AssertPartialRoute(WowRoute.WaypointTraversalMethod.CIRCULAR, startIndex, endIndex, expectedIndices);
        }

        // endIndex < startIndex, shortest path is straight backward through the list
        [TestMethod]
        [DataRow(5, 3, new[] { 5, 4, 3 })]
        [DataRow(3, 0, new[] { 3, 2, 1, 0 })] // backward 3 vs forward 4, ends on index 0 without wrapping
        [DataRow(6, 4, new[] { 6, 5, 4 })]
        public void CircularEndBeforeStartDirect(int startIndex, int endIndex, int[] expectedIndices)
        {
            AssertPartialRoute(WowRoute.WaypointTraversalMethod.CIRCULAR, startIndex, endIndex, expectedIndices);
        }

        // endIndex < startIndex, shortest path is forward past the last index and wraps to 0
        [TestMethod]
        [DataRow(5, 1, new[] { 5, 6, 0, 1 })] // forward 3 vs backward 4
        [DataRow(6, 0, new[] { 6, 0 })]       // single wrap step
        [DataRow(4, 0, new[] { 4, 5, 6, 0 })]
        public void CircularEndBeforeStartWraparound(int startIndex, int endIndex, int[] expectedIndices)
        {
            AssertPartialRoute(WowRoute.WaypointTraversalMethod.CIRCULAR, startIndex, endIndex, expectedIndices);
        }

        [TestMethod]
        public void CircularSameStartAndEnd()
        {
            AssertPartialRoute(WowRoute.WaypointTraversalMethod.CIRCULAR, 3, 3, new[] { 3 });
        }

        #endregion

        [TestMethod]
        [DataRow(WowRoute.WaypointTraversalMethod.LINEAR)]
        [DataRow(WowRoute.WaypointTraversalMethod.CIRCULAR)]
        public void WaypointNotOnRouteThrows(WowRoute.WaypointTraversalMethod traversalMethod)
        {
            WowRoute route = CreateRoute(traversalMethod);
            Vector2 offRoute = new Vector2(-100, -100);

            Assert.ThrowsExactly<Exception>(() => WowPathfinding.CreatePartialWaypointsFromRoute(route, offRoute, route.Waypoints[0]));
            Assert.ThrowsExactly<Exception>(() => WowPathfinding.CreatePartialWaypointsFromRoute(route, route.Waypoints[0], offRoute));
        }
    }
}
