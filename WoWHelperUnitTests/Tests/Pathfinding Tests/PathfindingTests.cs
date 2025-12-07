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

            var direction = Pathfinding.GetDirectionInDegrees(waypoint1, waypoint2);
            AssertExtensions.DoublesAreAlmostEqual(expectedDirection, direction);
        }
    }
}
