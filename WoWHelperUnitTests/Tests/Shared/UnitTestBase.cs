using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tesseract;
using WoWHelper;
using WoWHelper.Shared;

namespace WoWHelperUnitTests
{
    public class UnitTestBase
    {
        public WoWPlayer Player { get; set; }

        [AssemblyInitialize]
        public void InitializeTesseractEngine()
        {
            TesseractEngine te = TesseractEngineSingleton.Instance;
        }

        [TestInitialize]
        public void Initialize()
        {
            Player = new WoWPlayer();
        }
    }
}
