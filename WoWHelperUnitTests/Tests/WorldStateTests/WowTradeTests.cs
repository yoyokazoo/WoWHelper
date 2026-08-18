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

            bool tradeWindowUp = Player.WorldState.ScreenConfig.TradeWindowScreenPositions.MatchesSourceImage(bmp);

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

            bool tradeWindowAccepted = Player.WorldState.ScreenConfig.TradeWindowAcceptedScreenPositions.MatchesSourceImage(bmp);

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

            bool tradeWindowConfirmationUp = Player.WorldState.ScreenConfig.TradeWindowConfirmationScreenPositions.MatchesSourceImage(bmp);

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

            String tradeRecipient = Player.WorldState.ScreenConfig.TradeWindowRecipientTextArea.GetText(TesseractEngineSingleton.Instance, bmp).Trim();

            bool namesMatch = String.Equals(expected, tradeRecipient, StringComparison.OrdinalIgnoreCase);
            Assert.IsTrue(namesMatch);
        }
    }
}
