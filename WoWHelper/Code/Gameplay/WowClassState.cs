using System.Drawing;
using WoWHelper.Code.Gameplay;

namespace WoWHelper
{
    // Class-specific counterpart to WowWorldState. ClassBoolOne/Two and
    // ClassIntOne are 3 pixels whose MEANING depends on which class is
    // currently playing (e.g. bit R1 of ClassBoolOne is "Battle Shout
    // active" for a Warrior, but "Rockbiter Weapon active" for a Shaman --
    // see Warrior/Shaman/WarlockFunctions.lua's GetXClassBoolOne/Two).
    // Rather than one flat object with every class's fields (where nothing
    // would stop e.g. Shaman combat code from reading a Warrior-only field
    // and silently getting stale/wrong data), each class gets its own
    // concrete subtype exposing ONLY its own fields -- see
    // WowWarriorClassState/WowShamanClassState/WowWarlockClassState.
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

        public static WowClassState Create(WowCombatConfiguration combatConfiguration)
        {
            switch (combatConfiguration)
            {
                case WowCombatConfiguration.Warrior: return new WowWarriorClassState();
                case WowCombatConfiguration.Shaman: return new WowShamanClassState();
                case WowCombatConfiguration.Warlock: return new WowWarlockClassState();
                default: throw new System.NotImplementedException(
                    $"{nameof(WowClassState)}.{nameof(Create)}: no ClassState implemented for CombatConfiguration \"{combatConfiguration}\" -- " +
                    $"this should only be called with a resolved (non-Unknown) CombatConfiguration.");
            }
        }
    }
}
