using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tesseract;
using WoWHelper;
using WoWHelper.Code.Config;
using WoWHelper.Shared;

namespace WoWHelperUnitTests
{
    public class UnitTestBase
    {
        public WowPlayer Player { get; set; }

        [AssemblyInitialize]
        public void InitializeTesseractEngine()
        {
            TesseractEngine te = TesseractEngineSingleton.Instance;
        }

        [TestInitialize]
        public void Initialize()
        {
            Player = new WowPlayer();
        }

        protected Bitmap LoadBitmap(string relativePath)
        {
            return new Bitmap(Path.Combine(Directory.GetCurrentDirectory(), relativePath));
        }

        protected void LoadPlayer(string relativePath)
        {
            var bmp = LoadBitmap(relativePath);
            Player = new WowPlayer(WowScreenConfigs.GetForBitmap(bmp));
            Player.UpdateFromBitmap(bmp);
        }
    }
}
