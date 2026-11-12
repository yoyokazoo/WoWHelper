using System.Drawing;

namespace WoWHelper
{
    // Shaman's slice of the Class pixel rows. Box numbers here MUST match
    // SHAMAN_CLASS_BOOL_ONE/TWO_FIELDS in ShamanFunctions.lua.
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

        // ClassBoolTwo boxes 1/2 (see SHAMAN_CLASS_BOOL_TWO_FIELDS in ShamanFunctions.lua).
        // IsInEarthShockRange is a pure range check for Earth Shock,
        // independent of CanCastEarthShock's cooldown/usability check above. HasClearcasting
        // is Elemental Focus's proc buff (next two spells cost less mana).
        public bool IsInEarthShockRange { get; private set; }
        public bool HasClearcasting { get; private set; }

        // Frost Shock -- same damage as Earth Shock but doesn't interrupt a cast, so it's
        // only used as Earth Shock's substitute against nature-immune targets (see
        // WowShamanHandler.ShouldCastFrostShock). Shares Earth Shock's range and
        // cooldown category, so IsInEarthShockRange above covers range for both.
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
