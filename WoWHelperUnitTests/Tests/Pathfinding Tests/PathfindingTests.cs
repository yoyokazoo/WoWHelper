using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using WoWHelper.Code;
using WoWHelper.Code.WorldState;

namespace WoWHelperUnitTests
{
    [TestClass]
    public class PathfindingTests : UnitTestBase
    {
        [TestMethod]
        [DataRow(52f, 47f, 55.39f, 54.26f, 215.03d)]
        public void VerifyGetDirectionInDegrees(float x1, float y1, float x2, float y2, double expectedDirection)
        {
            var waypoint1 = new Vector2(x1, y1);
            var waypoint2 = new Vector2(x2, y2);

            var direction = WowPathfinding.GetDesiredDirectionInDegrees(waypoint1, waypoint2);
            AssertExtensions.DoublesAreAlmostEqual(expectedDirection, direction);
        }

        [TestMethod]
        [DataRow(250f, 200f, -50f)]
        [DataRow(9f, 330f, -39f)]
        [DataRow(40f, 107f, 67f)]
        [DataRow(340f, 40f, 60f)]
        public void VerifyGetDirectionToDragMouse(float currentDegrees, float desiredDegrees, float expectedDegrees)
        {
            var degreesToMove = WowPathfinding.GetDegreesToMove(currentDegrees, desiredDegrees);
            AssertExtensions.DoublesAreAlmostEqual(expectedDegrees, degreesToMove);
        }

        [TestMethod]
        [DataRow(97f, 0.12f)]
        [DataRow(83f, -0.12f)]
        public void VerifyLateralDistance(float facingDegrees, float expectedDistance)
        {
            Vector2 startingPoint = new Vector2(68.50f, 34.00f);
            Vector2 endingPoint = new Vector2(67.50f, 34.00f);

            var lateralDistance = WowPathfinding.GetLateralDistance(facingDegrees, startingPoint, endingPoint);
            AssertExtensions.DoublesAreAlmostEqual(expectedDistance, lateralDistance);
        }

        // WowPlayerConstants.MERCHANT_FINAL_WAYPOINT_TOLERANCE is what actually opts a
        // MoveTowardsPointTask call into the precision-approach branch (strafe-led fine aiming,
        // rotation only past PRECISION_APPROACH_ROTATION_TRIGGER_DEGREES) -- confirms it stays
        // under the threshold, and that the intermediate merchant leg and every real route's own
        // DistanceTolerance stay above it (so they keep the normal rotate-to-heading behavior).
        [TestMethod]
        public void VerifyPrecisionApproachThresholdSeparatesFinalFromIntermediateTolerances()
        {
            Assert.IsTrue(WowPlayerConstants.MERCHANT_FINAL_WAYPOINT_TOLERANCE <= WowPathfinding.PRECISION_APPROACH_TOLERANCE_THRESHOLD);
            Assert.IsTrue(WowPlayerConstants.MERCHANT_INTERMEDIATE_WAYPOINT_TOLERANCE > WowPathfinding.PRECISION_APPROACH_TOLERANCE_THRESHOLD);

            foreach (var location in WowLocationConfigs.ALL_LOCATIONS)
            {
                Assert.IsTrue(location.DistanceTolerance > WowPathfinding.PRECISION_APPROACH_TOLERANCE_THRESHOLD,
                    $"{location.Title}'s DistanceTolerance ({location.DistanceTolerance}) would unexpectedly opt into the precision-approach branch.");
            }
        }

        // PRECISION_APPROACH_ROTATION_TRIGGER_DEGREES must stay well under 90 -- it's meant to
        // be small enough that a full-speed strafe-plus-walk can still correct whatever heading
        // error it lets through (strafing is slower than walking forward in WoW, so the
        // achievable diagonal is less than 90 degrees off of straight-ahead).
        [TestMethod]
        public void VerifyPrecisionApproachRotationTriggerLeavesStrafingRoomToCorrect()
        {
            Assert.IsTrue(WowPathfinding.PRECISION_APPROACH_ROTATION_TRIGGER_DEGREES < 45f);
        }

        // The precision strafe tolerance needs to be tighter than the default -- otherwise it
        // wouldn't actually improve on the default for a target as small as
        // WowPlayerConstants.MERCHANT_FINAL_WAYPOINT_TOLERANCE.
        [TestMethod]
        public void VerifyPrecisionApproachStrafeToleranceIsTighterThanDefault()
        {
            Assert.IsTrue(WowPathfinding.PRECISION_APPROACH_STRAFE_TOLERANCE < WowPathfinding.STRAFE_LATERAL_DISTANCE_TOLERANCE);
            Assert.IsTrue(WowPathfinding.PRECISION_APPROACH_STRAFE_TOLERANCE < WowPlayerConstants.MERCHANT_FINAL_WAYPOINT_TOLERANCE);
        }
    }
}
