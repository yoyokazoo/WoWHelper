using System.Drawing;

namespace WoWHelper
{
    // Stub -- decodes DRUID_CLASS_BOOL_ONE/TWO_FIELDS (DruidFunctions.lua).
    public class WowDruidClassState : WowClassState
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
