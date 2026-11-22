using System.Drawing;

namespace WoWHelper
{
    // Stub -- decodes MAGE_CLASS_BOOL_ONE/TWO_FIELDS (MageFunctions.lua).
    public class WowMageClassState : WowClassState
    {
        public bool CanSpellcastPullTarget { get; private set; }

        public override void UpdateFromBitmap(Bitmap bmp)
        {
            Initialized = true;

            PixelRowBits one = WowPixelRows.ReadBits(bmp, PixelRow.ClassBoolOne);

            CanSpellcastPullTarget = one[1];
        }
    }
}
