using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.IO;
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
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            Bitmap bmp = new Bitmap(filePath);

            bool tradeWindowUp = Player.WorldState.ScreenConfig.TradeWindowScreenPositions.MatchesSourceImage(bmp);
            //bool tradeWindowAccepted = Player.WorldState.ScreenConfig.TradeWindowAcceptedScreenPositions.MatchesSourceImage(bmp);
            //bool tradeWindowConfirmationUp = Player.WorldState.ScreenConfig.TradeWindowConfirmationScreenPositions.MatchesSourceImage(bmp);
            //String tradeRecipient = Player.WorldState.ScreenConfig.TradeWindowRecipientTextArea.GetText(TesseractEngineSingleton.Instance, bmp);

            Assert.AreEqual(expected, tradeWindowUp);
        }

        [TestMethod]
        [DataRow(false, "..\\..\\Source Images\\numbersAsColors3.bmp")]
        [DataRow(false, "..\\..\\Source Images\\2560trade.bmp")]
        [DataRow(true, "..\\..\\Source Images\\2560tradeaccepted.bmp")]
        public void VerifyTradeWindowAccepted(bool expected, string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            Bitmap bmp = new Bitmap(filePath);

            bool tradeWindowAccepted = Player.WorldState.ScreenConfig.TradeWindowAcceptedScreenPositions.MatchesSourceImage(bmp);

            Assert.AreEqual(expected, tradeWindowAccepted);
        }

        [TestMethod]
        [DataRow(false, "..\\..\\Source Images\\numbersAsColors3.bmp")]
        [DataRow(true, "..\\..\\Source Images\\2560trade.bmp")]
        [DataRow(false, "..\\..\\Source Images\\2560tradeaccepted.bmp")]
        public void VerifyTradeWindowConfirmationUp(bool expected, string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            Bitmap bmp = new Bitmap(filePath);

            bool tradeWindowConfirmationUp = Player.WorldState.ScreenConfig.TradeWindowConfirmationScreenPositions.MatchesSourceImage(bmp);

            Assert.AreEqual(expected, tradeWindowConfirmationUp);
        }

        [TestMethod]
        [DataRow("rite", "..\\..\\Source Images\\numbersAsColors3.bmp")]
        [DataRow("Yoyokazu", "..\\..\\Source Images\\2560trade.bmp")]
        [DataRow("Tankandsb...", "..\\..\\Source Images\\2560tradeaccepted.bmp")]
        public void VerifyTradeWindowRecipient(string expected, string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            Bitmap bmp = new Bitmap(filePath);

            String tradeRecipient = Player.WorldState.ScreenConfig.TradeWindowRecipientTextArea.GetText(TesseractEngineSingleton.Instance, bmp).Trim();

            bool namesMatch = String.Equals(expected, tradeRecipient, StringComparison.OrdinalIgnoreCase);
            Assert.IsTrue(namesMatch);
        }
    }
}
