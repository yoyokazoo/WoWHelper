using System.Drawing;

namespace WoWHelper
{
    // Warlock's slice of ClassBoolOne/Two/ClassIntOne. Bit layout here MUST
    // match WarlockFunctions.lua's GetWarlockClassBoolOne/Two/GetWarlockClassIntOne.
    //
    // STUB: no Warlock-specific fields decoded yet -- ClassBoolOne/Two and
    // ClassIntOne are still fully reserved for this class (same as any other
    // not-yet-decoded pixel -- see "Reserved-but-not-in-the-row" in
    // CLAUDE.md). Add properties here (and the matching bit in
    // WarlockFunctions.lua's GetWarlockClassBoolOne/Two) together as real
    // Warlock state gets wired up -- see WowShamanClassState for the pattern
    // to follow (decode a byte via WowWorldState.DecodeByte, assign named
    // bools in the same order the Lua side packs them).
    public class WowWarlockClassState : WowClassState
    {
        public override void UpdateFromBitmap(Bitmap bmp, WowScreenConfiguration screenConfig)
        {
            Initialized = true;

            // TODO: decode ClassBoolOne/Two (and ClassIntOne, once something
            // needs it) here as fields get defined, same pattern as
            // WowShamanClassState.UpdateFromBitmap.
        }
    }
}
