using Tesseract;

namespace WoWHelper.Shared
{
    // TODO: Dependency Injection
    // from: https://csharpindepth.com/articles/singleton
    public sealed class TesseractEngineSingleton
    {
        private static readonly TesseractEngine instance = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static TesseractEngineSingleton()
        {
        }

        private TesseractEngineSingleton()
        {
        }

        public static TesseractEngine Instance
        {
            get
            {
                return instance;
            }
        }
    }
}
