using System.Drawing;

namespace WoWHelper
{
    // Warlock's slice of ClassBoolOne/Two/ClassIntOne. Bit layout here MUST
    // match WarlockFunctions.lua's GetWarlockClassBoolOne/Two/GetWarlockClassIntOne.
    //
    // R1/R2 of ClassBoolOne are decoded below -- R3-R8, ClassBoolTwo, and
    // ClassIntOne are still fully reserved for this class (same as any other
    // not-yet-decoded pixel -- see "Reserved-but-not-in-the-row" in
    // CLAUDE.md). Add properties here (and the matching bit in
    // WarlockFunctions.lua's GetWarlockClassBoolOne/Two) together as more
    // Warlock state gets wired up -- see WowShamanClassState for the pattern
    // to follow (decode a byte via WowWorldState.DecodeByte, assign named
    // bools in the same order the Lua side packs them).
    public class WowWarlockClassState : WowClassState
    {
        public bool CanSpellcastPullTarget { get; private set; }

        // Demon Skin (low level) and Demon Armor (replaces it at higher level) are
        // two differently-named buffs, but only one is ever active at a time -- see
        // ShouldCastDemonArmor() in WarlockFunctions.lua, which checks for either
        // buff and returns false if either is present.
        public bool ShouldCastDemonArmor { get; private set; }

        public override void UpdateFromBitmap(Bitmap bmp, WowScreenConfiguration screenConfig)
        {
            Initialized = true;

            Color color = bmp.GetPixel(screenConfig.ClassBoolOnePosition.X, screenConfig.ClassBoolOnePosition.Y);
            WowWorldState.DecodeByte(color.R, out var r1, out var r2, out _, out _, out _, out _, out _, out _);

            CanSpellcastPullTarget = r1;
            ShouldCastDemonArmor = r2;
            // R3-R8 still reserved for Warlock -- see GetWarlockClassBoolOne() in
            // WarlockFunctions.lua.
        }
    }
}
