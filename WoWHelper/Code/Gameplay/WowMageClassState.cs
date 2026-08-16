using System.Drawing;

namespace WoWHelper
{
    // Mage's slice of ClassBoolOne/Two/ClassIntOne. Bit layout here MUST
    // match MageFunctions.lua's GetMageClassBoolOne/Two/GetMageClassIntOne.
    public class WowMageClassState : WowClassState
    {
        public bool MageArmorActive { get; private set; }
        public bool ArcaneIntellectActive { get; private set; }
        public bool ShouldSummonWater { get; private set; }
        public bool ShouldSummonFood { get; private set; }
        public bool IsFireblastCooledDown { get; private set; }
        public bool CanSpellcastPullTarget { get; private set; }

        public override void UpdateFromBitmap(Bitmap bmp, WowScreenConfiguration screenConfig)
        {
            Initialized = true;

            Color color = bmp.GetPixel(screenConfig.ClassBoolOnePosition.X, screenConfig.ClassBoolOnePosition.Y);
            WowWorldState.DecodeByte(color.R, out var r1, out var r2, out var r3, out var r4, out var r5, out var r6, out var r7, out var r8);

            MageArmorActive = r1;
            ArcaneIntellectActive = r2;
            ShouldSummonWater = r3;
            ShouldSummonFood = r4;
            IsFireblastCooledDown = r5;
            CanSpellcastPullTarget = r6;
            // r7, r8, ClassBoolTwo, and ClassIntOne currently reserved/unused for Mage.
        }
    }
}
