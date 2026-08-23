using System.Drawing;

namespace WoWHelper
{
    // Shaman's slice of ClassBoolOne/Two/ClassIntOne. Bit layout here MUST
    // match ShamanFunctions.lua's GetShamanClassBoolOne/Two/GetShamanClassIntOne.
    public class WowShamanClassState : WowClassState
    {
        public bool ShouldCastRockbiterWeapon { get; private set; }
        public bool ShouldCastLightningShield { get; private set; }
        public bool CanCastEarthShock { get; private set; }
        public bool ShouldCastFlameShock { get; private set; }
        public bool TargetHasFlameShock { get; private set; }
        public bool CanSpellcastPullTarget { get; private set; }
        public bool CanCurePoison { get; private set; }
        public bool CanCureDisease { get; private set; }

        public override void UpdateFromBitmap(Bitmap bmp, WowScreenConfiguration screenConfig)
        {
            Initialized = true;

            Color color = bmp.GetPixel(screenConfig.ClassBoolOnePosition.X, screenConfig.ClassBoolOnePosition.Y);
            WowWorldState.DecodeByte(color.R, out var r1, out var r2, out var r3, out var r4, out var r5, out var r6, out var r7, out var r8);

            ShouldCastRockbiterWeapon = r1;
            ShouldCastLightningShield = r2;
            CanCastEarthShock = r3;
            ShouldCastFlameShock = r4;
            TargetHasFlameShock = r5;
            CanSpellcastPullTarget = r6;
            CanCurePoison = r7;
            CanCureDisease = r8;
            // ClassBoolTwo and ClassIntOne currently reserved/unused for Shaman.
        }
    }
}
