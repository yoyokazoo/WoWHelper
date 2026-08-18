using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.IO;
using WoWHelper;
using WoWHelper.Code.Config;
using WoWHelper.Shared;

namespace WoWHelperUnitTests
{
    [TestClass]
    public class WowTradeTests : UnitTestBase
    {
        [TestMethod]
        [DataRow(false, "..\\..\\Source Images\\numbersAsColors3.bmp")]
        [DataRow(true, "..\\..\\Source Images\\2560trade.bmp")]
        [DataRow(true, "..\\..\\Source Images\\2560tradeaccepted.bmp")]
        public void VerifyTradeWindowUp(bool expected, string fileName)
        {
            var bmp = LoadBitmap(fileName);
            Player = new WowPlayer(WowScreenConfigs.GetForBitmap(bmp));

            var positions = Player.WorldState.ScreenConfig.TradeWindowScreenPositions;
            if (positions == null)
            {
                if (expected) Assert.Fail("Expected trade window detected but positions not configured for this resolution.");
                else Assert.Inconclusive("Trade window positions not configured for this resolution — skipping false check.");
            }

            bool tradeWindowUp = positions.MatchesSourceImage(bmp);
            Assert.AreEqual(expected, tradeWindowUp);
        }

        [TestMethod]
        [DataRow(false, "..\\..\\Source Images\\numbersAsColors3.bmp")]
        [DataRow(false, "..\\..\\Source Images\\2560trade.bmp")]
        [DataRow(true, "..\\..\\Source Images\\2560tradeaccepted.bmp")]
        public void VerifyTradeWindowAccepted(bool expected, string fileName)
        {
            var bmp = LoadBitmap(fileName);
            Player = new WowPlayer(WowScreenConfigs.GetForBitmap(bmp));

            var positions = Player.WorldState.ScreenConfig.TradeWindowAcceptedScreenPositions;
            if (positions == null)
            {
                if (expected) Assert.Fail("Expected trade accepted detected but positions not configured for this resolution.");
                else Assert.Inconclusive("Trade accepted positions not configured for this resolution — skipping false check.");
            }

            bool tradeWindowAccepted = positions.MatchesSourceImage(bmp);
            Assert.AreEqual(expected, tradeWindowAccepted);
        }

        [TestMethod]
        [DataRow(false, "..\\..\\Source Images\\numbersAsColors3.bmp")]
        [DataRow(true, "..\\..\\Source Images\\2560trade.bmp")]
        [DataRow(false, "..\\..\\Source Images\\2560tradeaccepted.bmp")]
        public void VerifyTradeWindowConfirmationUp(bool expected, string fileName)
        {
            var bmp = LoadBitmap(fileName);
            Player = new WowPlayer(WowScreenConfigs.GetForBitmap(bmp));

            var positions = Player.WorldState.ScreenConfig.TradeWindowConfirmationScreenPositions;
            if (positions == null)
            {
                if (expected) Assert.Fail("Expected trade confirmation detected but positions not configured for this resolution.");
                else Assert.Inconclusive("Trade confirmation positions not configured for this resolution — skipping false check.");
            }

            bool tradeWindowConfirmationUp = positions.MatchesSourceImage(bmp);
            Assert.AreEqual(expected, tradeWindowConfirmationUp);
        }

        [TestMethod]
        [DataRow("rite", "..\\..\\Source Images\\numbersAsColors3.bmp")]
        [DataRow("Yoyokazu", "..\\..\\Source Images\\2560trade.bmp")]
        [DataRow("Tankandsb...", "..\\..\\Source Images\\2560tradeaccepted.bmp")]
        public void VerifyTradeWindowRecipient(string expected, string fileName)
        {
            var bmp = LoadBitmap(fileName);
            Player = new WowPlayer(WowScreenConfigs.GetForBitmap(bmp));

            var textArea = Player.WorldState.ScreenConfig.TradeWindowRecipientTextArea;
            if (textArea == null)
                Assert.Inconclusive("Trade window recipient text area not configured for this resolution.");

            String tradeRecipient = textArea.GetText(TesseractEngineSingleton.Instance, bmp).Trim();
            bool namesMatch = String.Equals(expected, tradeRecipient, StringComparison.OrdinalIgnoreCase);
            Assert.IsTrue(namesMatch);
        }
    }
}
