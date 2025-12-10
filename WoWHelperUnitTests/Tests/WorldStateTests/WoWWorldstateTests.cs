using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.IO;

namespace WoWHelperUnitTests
{
    [TestClass]
    public class WoWWorldstateTests : UnitTestBase
    {
        [TestMethod]
        [DataRow("..\\..\\Source Images\\numbersAsColors3.bmp")]
        public void VerifyWowWorldState(string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            Player.UpdateFromBitmap(new Bitmap(filePath));

            Assert.AreEqual(100, Player.WorldState.PlayerHpPercent);
            Assert.AreEqual(13, Player.WorldState.ResourcePercent);
            Assert.AreEqual(100, Player.WorldState.TargetHpPercent);
            AssertExtensions.AssertFloatApproximately(44.05f, Player.WorldState.MapX);
            AssertExtensions.AssertFloatApproximately(64.61f, Player.WorldState.MapY);
            AssertExtensions.AssertFloatApproximately(128.56f, Player.WorldState.FacingDegrees);
            Assert.IsFalse(Player.WorldState.IsInRange);
            Assert.IsFalse(Player.WorldState.IsInCombat);
            Assert.IsTrue(Player.WorldState.CanChargeTarget);
        }

        [TestMethod]
        [DataRow(false, "..\\..\\Source Images\\numbersAsColors3.bmp")]
        [DataRow(true, "..\\..\\Source Images\\FacingWrongWay.bmp")]
        public void VerifyFacingWrongWay(bool expected, string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            Player.UpdateFromBitmap(new Bitmap(filePath));

            Assert.AreEqual(expected, Player.WorldState.FacingWrongWay);
        }
    }
}
