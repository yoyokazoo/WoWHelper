using System.Drawing;

namespace WoWHelper
{
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

        public bool IsInEarthShockRange { get; private set; }
        public bool HasClearcasting { get; private set; }
        public bool CanCastFrostShock { get; private set; }

        public override void UpdateFromBitmap(Bitmap bmp)
        {
            Initialized = true;

            PixelRowBits one = WowPixelRows.ReadBits(bmp, PixelRow.ClassBoolOne);

            ShouldCastRockbiterWeapon = one[1];
            ShouldCastLightningShield = one[2];
            CanCastEarthShock = one[3];
            ShouldCastFlameShock = one[4];
            TargetHasFlameShock = one[5];
            CanSpellcastPullTarget = one[6];
            CanCurePoison = one[7];
            CanCureDisease = one[8];

            PixelRowBits two = WowPixelRows.ReadBits(bmp, PixelRow.ClassBoolTwo);

            IsInEarthShockRange = two[1];
            HasClearcasting = two[2];
            CanCastFrostShock = two[3];
        }
    }
}
