using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.IO;

namespace WoWHelperUnitTests
{
    [TestClass]
    public class WoWWorldstateTests : UnitTestBase
    {
        [DataTestMethod]
        [DataRow(100, "..\\..\\Source Images\\HpPercent100.bmp")]
        [DataRow(98, "..\\..\\Source Images\\HpPercent98.bmp")]
        public void VerifyHealthPercentage(int expected, string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            Player.UpdateFromBitmap(new Bitmap(filePath));

            Assert.AreEqual(expected, Player.WorldState.HpPercent);
        }
    }
}
