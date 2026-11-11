using System.Drawing;

namespace WoWHelper
{
    // Class-specific counterpart to WowWorldState. The ClassBoolOne/Two pixel
    // rows' MEANING depends on which class is currently playing (e.g. box 1
    // of ClassBoolOne is "Battle Shout active" for a Warrior, but "should cast
    // Rockbiter Weapon" for a Shaman -- see the <CLASS>_CLASS_BOOL_ONE/TWO_FIELDS
    // lists in Warrior/Shaman/WarlockFunctions.lua).
    // Rather than one flat object with every class's fields (where nothing
    // would stop e.g. Shaman combat code from reading a Warrior-only field
    // and silently getting stale/wrong data), each class gets its own
    // concrete subtype exposing ONLY its own fields -- see
    // WowWarriorClassState/WowShamanClassState/WowWarlockClassState.
    //
    // Each instance is owned privately by its class's WowClassHandler (see
    // WowClassHandler<TState>), which builds it once and updates it every tick
    // alongside WorldState. Nothing outside that handler can reach it, so a
    // wrong-class field reference is a compile error, not a runtime surprise.
    public abstract class WowClassState
    {
        public bool Initialized { get; protected set; }

        public abstract void UpdateFromBitmap(Bitmap bmp);
    }
}
