using System.Drawing;

namespace WoWHelper
{
    // Class-specific counterpart to WowWorldState. ClassBoolOne/Two and
    // ClassIntOne are 3 pixels whose MEANING depends on which class is
    // currently playing (e.g. bit R1 of ClassBoolOne is "Battle Shout
    // active" for a Warrior, but "Frost Armor active" for a Mage -- see
    // Warrior/Mage/ShamanFunctions.lua's GetXClassBoolOne/Two). Rather than
    // one flat object with every class's fields (where nothing would stop
    // e.g. Mage combat code from reading a Warrior-only field and silently
    // getting stale/wrong data), each class gets its own concrete subtype
    // exposing ONLY its own fields -- see WowWarriorClassState/
    // WowMageClassState/WowShamanClassState.
    //
    // WowPlayer builds the right concrete instance once (based on
    // FarmingConfig.CombatConfiguration) and updates it every tick alongside
    // WorldState. The class-specific Wow*Tasks.cs methods receive their
    // class's concrete ClassState as a method parameter (not read off
    // `this`), so a wrong-class field reference is a compile error, not a
    // runtime surprise -- and the cast at each dispatch call site in
    // WowPlayerCombatConfig.cs throws immediately if ClassState and
    // CombatConfiguration ever disagree about which class is active.
    public abstract class WowClassState
    {
        public bool Initialized { get; protected set; }

        public abstract void UpdateFromBitmap(Bitmap bmp, WowScreenConfiguration screenConfig);
    }
}
