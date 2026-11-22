using System.Drawing;

namespace WoWHelper
{
    // Stub -- HUNTER_CLASS_BOOL_ONE/TWO_FIELDS (HunterFunctions.lua) are both still empty.
    public class WowHunterClassState : WowClassState
    {
        public override void UpdateFromBitmap(Bitmap bmp)
        {
            Initialized = true;
        }
    }
}
