using System.Drawing;

namespace WoWHelper
{
    public class WowWarlockClassState : WowClassState
    {
        public bool CanSpellcastPullTarget { get; private set; }
        public bool ShouldCastDemonArmor { get; private set; }
        public bool ShouldSummonPet { get; private set; }
        public bool ShouldCastImmolate { get; private set; }
        public bool ShouldCastCorruption { get; private set; }

        public override void UpdateFromBitmap(Bitmap bmp)
        {
            Initialized = true;

            PixelRowBits bits = WowPixelRows.ReadBits(bmp, PixelRow.ClassBoolOne);

            CanSpellcastPullTarget = bits[1];
            ShouldCastDemonArmor = bits[2];
            ShouldSummonPet = bits[3];
            ShouldCastImmolate = bits[4];
            ShouldCastCorruption = bits[5];
        }
    }
}
