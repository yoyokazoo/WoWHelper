using System.Drawing;

namespace WoWHelper
{
    // Warlock's slice of the Class pixel rows. Box numbers here MUST match
    // WARLOCK_CLASS_BOOL_ONE_FIELDS in WarlockFunctions.lua -- add a property
    // here and the matching list entry there together as more Warlock state
    // gets wired up (ClassBoolTwo is unused for Warlock so far).
    public class WowWarlockClassState : WowClassState
    {
        public bool CanSpellcastPullTarget { get; private set; }

        // Demon Skin (low level) and Demon Armor (replaces it at higher level) are
        // two differently-named buffs, but only one is ever active at a time -- see
        // ShouldCastDemonArmor() in WarlockFunctions.lua, which checks for either
        // buff and returns false if either is present.
        public bool ShouldCastDemonArmor { get; private set; }

        // True once the player knows at least Summon Imp (gates whether a pet can be
        // summoned at all yet) and doesn't currently have a living pet out -- see
        // ShouldSummonPet() in WarlockFunctions.lua.
        public bool ShouldSummonPet { get; private set; }

        // True when the player knows Immolate/Corruption and the target doesn't
        // already have that DoT on it -- see ShouldCastImmolate()/
        // ShouldCastCorruption() in WarlockFunctions.lua.
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
