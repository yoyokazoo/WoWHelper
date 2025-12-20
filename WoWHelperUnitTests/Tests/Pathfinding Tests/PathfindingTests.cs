using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using WoWHelper.Code;

namespace WoWHelperUnitTests
{
    [TestClass]
    public class PathfindingTests : UnitTestBase
    {
        [DataTestMethod]
        [DataRow(52f, 47f, 55.39f, 54.26f, 215.03d)]
        public void VerifyGetDirectionInDegrees(float x1, float y1, float x2, float y2, double expectedDirection)
        {
            var waypoint1 = new Vector2(x1, y1);
            var waypoint2 = new Vector2(x2, y2);

            var direction = WowPathfinding.GetDesiredDirectionInDegrees(waypoint1, waypoint2);
            AssertExtensions.DoublesAreAlmostEqual(expectedDirection, direction);
        }

        [DataTestMethod]
        [DataRow(250f, 200f, -50f)]
        [DataRow(9f, 330f, -39f)]
        [DataRow(40f, 107f, 67f)]
        [DataRow(340f, 40f, 60f)]
        public void VerifyGetDirectionToDragMouse(float currentDegrees, float desiredDegrees, float expectedDegrees)
        {
            var degreesToMove = WowPathfinding.GetDegreesToMove(currentDegrees, desiredDegrees);
            AssertExtensions.DoublesAreAlmostEqual(expectedDegrees, degreesToMove);
        }

        [DataTestMethod]
        [DataRow(97f, 0.12f)]
        [DataRow(83f, -0.12f)]
        public void VerifyLateralDistance(float facingDegrees, float expectedDistance)
        {
            Vector2 startingPoint = new Vector2(68.50f, 34.00f);
            Vector2 endingPoint = new Vector2(67.50f, 34.00f);
            
            var lateralDistance = WowPathfinding.GetLateralDistance(facingDegrees, startingPoint, endingPoint);
            AssertExtensions.DoublesAreAlmostEqual(expectedDistance, lateralDistance);
        }
    }
}
