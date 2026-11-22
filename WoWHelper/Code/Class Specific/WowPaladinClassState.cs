using System.Drawing;

namespace WoWHelper
{
    // Stub -- PALADIN_CLASS_BOOL_ONE/TWO_FIELDS (PaladinFunctions.lua) are both still empty.
    public class WowPaladinClassState : WowClassState
    {
        public override void UpdateFromBitmap(Bitmap bmp)
        {
            Initialized = true;
        }
    }
}
