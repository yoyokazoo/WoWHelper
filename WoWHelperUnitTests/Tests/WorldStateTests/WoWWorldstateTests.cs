using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.IO;

namespace WoWHelperUnitTests
{
    [TestClass]
    public class WoWWorldstateTests : UnitTestBase
    {
        /*
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
            Assert.IsFalse(Player.WorldState.IsInCombat);
            Assert.IsTrue(Player.WorldState.CanChargeTarget);
        }
        */

        [TestMethod]
        [DataRow(false, "..\\..\\Source Images\\numbersAsColors3.bmp")]
        [DataRow(true, "..\\..\\Source Images\\FacingWrongWay.bmp")]
        public void VerifyFacingWrongWay(bool expected, string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            Player.UpdateFromBitmap(new Bitmap(filePath));

            Assert.AreEqual(expected, Player.WorldState.FacingWrongWay);
        }

        [TestMethod]
        [DataRow(false, "..\\..\\Source Images\\FacingWrongWay.bmp")]
        [DataRow(true, "..\\..\\Source Images\\toofaraway.bmp")]
        public void VerifyTooFarAway(bool expected, string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            Player.UpdateFromBitmap(new Bitmap(filePath));

            Assert.AreEqual(expected, Player.WorldState.TooFarAway);
        }

        /*
        [TestMethod]
        [DataRow(0, "..\\..\\Source Images\\numbersAsColors3.bmp")]
        [DataRow(1, "..\\..\\Source Images\\FacingWrongWay.bmp")]
        public void VerifyAttackerCount(int expected, string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            Player.UpdateFromBitmap(new Bitmap(filePath));

            Assert.AreEqual(expected, Player.WorldState.AttackerCount);
        }
        */

        [TestMethod]
        [DataRow(false, false, "..\\..\\Source Images\\NoBattleshout.bmp")]
        [DataRow(true, true, "..\\..\\Source Images\\BattleShout.bmp")]
        public void VerifyMultiBoolEncoding(bool expectedBoolOne, bool expectedBoolTwo, string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            Player.UpdateFromBitmap(new Bitmap(filePath));

            Assert.AreEqual(expectedBoolOne, Player.WorldState.IsAutoAttacking);
            Assert.AreEqual(expectedBoolTwo, Player.WorldState.BattleShoutActive);
        }

        [TestMethod]
        [DataRow(false, "..\\..\\Source Images\\NoBattleshout.bmp")]
        [DataRow(false, "..\\..\\Source Images\\login screen.bmp")]
        [DataRow(true, "..\\..\\Source Images\\new login screen.bmp")]
        public void VerifyOnLoginScreen(bool expected, string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            Player.UpdateFromBitmap(new Bitmap(filePath));

            Assert.AreEqual(expected, Player.WorldState.OnLoginScreen);
        }

        [TestMethod]
        [DataRow(false, "..\\..\\Source Images\\NoBattleshout.bmp")]
        [DataRow(true, "..\\..\\Source Images\\breathbar.bmp")]
        public void VerifyUnderwater(bool expected, string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            Player.UpdateFromBitmap(new Bitmap(filePath));

            Assert.AreEqual(expected, Player.WorldState.Underwater);
        }

        [TestMethod]
        [DataRow(false, "..\\..\\Source Images\\NoBattleshout.bmp")]
        [DataRow(true, "..\\..\\Source Images\\targetneedstobeinfront.bmp")]
        [DataRow(true, "..\\..\\Source Images\\lighttargetneedstobeinfront.bmp")]
        public void VerifyTargetNeedsToBeInFront(bool expected, string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            Player.UpdateFromBitmap(new Bitmap(filePath));

            Assert.AreEqual(expected, Player.WorldState.TargetNeedsToBeInFront);
        }

        [TestMethod]
        [DataRow(false, "..\\..\\Source Images\\targetneedstobeinfront.bmp")]
        [DataRow(true, "..\\..\\Source Images\\invalidtarget.bmp")]
        public void VerifyInvalidTarget(bool expected, string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            Player.UpdateFromBitmap(new Bitmap(filePath));

            Assert.AreEqual(expected, Player.WorldState.InvalidTarget);
        }

        [TestMethod]
        [DataRow(false, "..\\..\\Source Images\\targetneedstobeinfront.bmp")]
        [DataRow(true, "..\\..\\Source Images\\outofrange.bmp")]
        public void VerifyOutOfRange(bool expected, string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            Player.UpdateFromBitmap(new Bitmap(filePath));

            Assert.AreEqual(expected, Player.WorldState.OutOfRange);
        }
    }
}
