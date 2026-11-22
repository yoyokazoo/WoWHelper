using System.Drawing;

namespace WoWHelper
{
    // Stub -- decodes PRIEST_CLASS_BOOL_ONE/TWO_FIELDS (PriestFunctions.lua).
    public class WowPriestClassState : WowClassState
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
