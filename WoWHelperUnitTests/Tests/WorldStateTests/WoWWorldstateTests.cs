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
        [DataRow(100, "..\\..\\Source Images\\CustomUI.bmp")]
        public void VerifyHpPercentage(int expected, string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            Player.UpdateFromBitmap(new Bitmap(filePath));

            Assert.AreEqual(expected, Player.WorldState.PlayerHpPercent);
        }

        [TestMethod]
        [DataRow(100, "..\\..\\Source Images\\CustomUI.bmp")]
        public void VerifyResourcePercentage(int expected, string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            Player.UpdateFromBitmap(new Bitmap(filePath));

            Assert.AreEqual(expected, Player.WorldState.ResourcePercent);
        }

        [TestMethod]
        [DataRow(3.42f, "..\\..\\Source Images\\CustomUI.bmp")]
        public void VerifyHeading(float expected, string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            Player.UpdateFromBitmap(new Bitmap(filePath));

            Assert.AreEqual(expected, Player.WorldState.FacingDegrees);
        }
    }
}
