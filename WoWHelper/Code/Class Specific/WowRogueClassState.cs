using System.Drawing;

namespace WoWHelper
{
    // Stub -- ROGUE_CLASS_BOOL_ONE/TWO_FIELDS (RogueFunctions.lua) are both still empty.
    public class WowRogueClassState : WowClassState
    {
        public override void UpdateFromBitmap(Bitmap bmp)
        {
            Initialized = true;
        }
    }
}
